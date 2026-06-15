using Dapper;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.ScanCargoCustomerDiff.Domain;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.ScanCargoCustomerDiff
{
    public class ScanCargoCustomerDiffService : _BaseService
    {
        public ScanCargoCustomerDiffService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得作業地區下拉選項
        /// </summary>
        /// <returns></returns>
        public List<SelectListModel> GetDataTypeList()
        {
            const string sql = @"
SELECT DataType AS [Value], DataType AS [Text]
FROM [jetf].[dbo].[PdtDataType]" ;

            return conn.Query<SelectListModel>(sql).ToList();
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <param name="dataType">作業地區</param>
        /// <returns></returns>
        public IWorkbook ExportExcel(string startTime, string endTime, string dataType)
        {
            var workbook = new XSSFWorkbook();

            // 取得差異表資料
            var diffData = GetClearanceInfoScanCargoDetails(dataType, startTime, endTime);

            // 取得客戶名稱
            var customerData = GetClearanceInfoScanCargoCustomer(dataType, startTime, endTime);

            // 產生 Excel Sheet
            GetClearanceInfoScanCargoDetailsSheet(workbook, diffData, customerData);

            return workbook;
        }

        /// <summary>
        /// 取得拆袋作業差異表資料
        /// </summary>
        /// <param name="dataType">作業地區</param>
        /// <param name="sDate">開始時間</param>
        /// <param name="eDate">結束時間</param>
        /// <returns></returns>
        private List<ScanCargoCustomerDiffModel> GetClearanceInfoScanCargoDetails(string dataType, string sDate, string eDate)
        {
            const string sql = @"
with cte as 
(    select MAIN_NUMBER,BAG_NUMBER,MERGE_NUMBER,SIGN_OUT_TIME from [DATA_CENTER].[dbo].[CLEARANCE_INFO] 
     where DATA_TYPE=@DataType and SIGN_IN_TIME between @SDate and @EDate 
) 
select a.MAIN_NUMBER,a.BAG_NUMBER,a.MERGE_NUMBER,a.SIGN_OUT_TIME,isnull(b.Data,c.Data) as Data from cte a 
left join (select * from [jetf].[dbo].[PdtScanCargoUpload] where DataType=@DataType and UploadTime between @SDate and @EDate) b on a.BAG_NUMBER =b.data 
left join (select * from [jetf].[dbo].[PdtScanCargoUpload] where DataType=@DataType and UploadTime between @SDate and @EDate) c on a.MERGE_NUMBER =c.data 
group by a.MAIN_NUMBER,a.BAG_NUMBER,a.MERGE_NUMBER,a.SIGN_OUT_TIME,b.Data,c.Data
";

            return conn.Query<ScanCargoCustomerDiffModel>(sql, new
            {
                DataType = dataType,
                SDate = $"{sDate}:00",
                EDate = $"{eDate}:59"
            }).ToList();
        }

        /// <summary>
        /// 取得客戶名稱
        /// </summary>
        /// <param name="dataType">作業地區</param>
        /// <param name="sDate">開始時間</param>
        /// <param name="eDate">結束時間</param>
        /// <returns></returns>
        private List<CustomerNameModel> GetClearanceInfoScanCargoCustomer(string dataType, string sDate, string eDate)
        {
            const string sql = @"
with cte as 
(    select  MAIN_NUMBER from [DATA_CENTER].[dbo].[CLEARANCE_INFO] 
     where DATA_TYPE=@DATA_TYPE and SIGN_IN_TIME between @SDate and @EDate 
     group by  MAIN_NUMBER 
) 
select a.MAIN_NUMBER,c.DESPATCHNAME from cte a 
join [DATA_CENTER].[dbo].[MAINORDERINFO] b on a.MAIN_NUMBER=b.MAINNUMBER 
join [DATA_CENTER].[dbo].[DESPATCHFROM] c on b.DELIVERYFROM=c.DESPATCHNO 
group by a.MAIN_NUMBER,c.DESPATCHNAME";

            return conn.Query<CustomerNameModel>(sql, new
            {
                DATA_TYPE = dataType,
                SDate = $"{sDate}:00",
                EDate = $"{eDate}:59"
            }).ToList();
        }

        /// <summary>
        /// 取得處置說明
        /// </summary>
        /// <param name="list">分提單號清單</param>
        /// <returns></returns>
        private Dictionary<string, string> GetProcess(List<string> list)
        {
            if (list.Count == 0)
                return new Dictionary<string, string>();

            var sql = @"
declare @TrackingNoTable Table
( 
    TrackingNo nvarchar(100)
)

{0}

with Process as 
(
    select DLV_INV,MIN(PROCESS_TYPE) as ProcessType from jetf.dbo.Process
    where PROCESS_TYPE in (3,4) and DEL = 0
    group by DLV_INV
)
select a.TrackingNo,b.ProcessType from @TrackingNoTable a
join Process b on a.TrackingNo = b.DLV_INV" ;

            var sb = new System.Text.StringBuilder();
            foreach (var item in list.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @TrackingNoTable VALUES {string.Join(",",
                    item.Select(r => $"('{r}')"))};");
            }

            sql = string.Format(sql, sb.ToString());

            return conn.Query(sql)
                .ToDictionary(
                    r => (string)r.TrackingNo,
                    r => (string)r.ProcessType);
        }

        /// <summary>
        /// 產生刷槍作業差異表 Excel Sheet
        /// </summary>
        /// <param name="workbook">Workbook</param>
        /// <param name="diffData">差異資料</param>
        /// <param name="customerData">客戶資料</param>
        /// <returns></returns>
        private ISheet GetClearanceInfoScanCargoDetailsSheet(IWorkbook workbook, List<ScanCargoCustomerDiffModel> diffData, List<CustomerNameModel> customerData)
        {
            int irow;

            ISheet sheet = workbook.CreateSheet("刷槍作業差異表");

            // 建立置中樣式
            var cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.Alignment = HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = VerticalAlignment.Center;
            var font = (XSSFFont)workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 12;
            cs_Center.SetFont(font);

            // 表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主號");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("入倉袋數");
            row.CreateCell(3).SetCellValue("現場實際刷貨");
            row.CreateCell(4).SetCellValue("查驗袋數");
            row.CreateCell(5).SetCellValue("查驗的袋號");
            row.CreateCell(6).SetCellValue("差異");
            row.CreateCell(7).SetCellValue("差異的袋號");
            row.CreateCell(8).SetCellValue("差異的分號");
            row.CreateCell(9).SetCellValue("備註");
            row.CreateCell(10).SetCellValue("倉儲漏刷");

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 8000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 10000);
            sheet.AutoSizeColumn(6);
            sheet.SetColumnWidth(7, 10000);
            sheet.SetColumnWidth(8, 10000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 10000);

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;

            // 分組資料
            var dt_Group = (from t in diffData
                            group t by t.MAIN_NUMBER into g
                            orderby g.Key
                            select new
                            {
                                MainNumber = g.Key,
                                TotalCount = g.Count(),
                                ScanCount = g.Where(m => m.Data != null).Count(),
                                CheckCount = g.Where(m => m.Data == null && m.SIGN_OUT_TIME == null).Count(),
                                CheckBagNumber = string.Join(",", g.Where(m => m.Data == null && m.SIGN_OUT_TIME == null)
                                                     .Select(m => m.BAG_NUMBER)),
                                //倉儲漏刷，沒有出艙有掃貨上車
                                CheckTrackingNoList = g.Where(m => m.Data != null && m.SIGN_OUT_TIME == null)
                                                     .Select(m => m.MERGE_NUMBER)
                                                     .Where(m => string.IsNullOrEmpty(m) == false)
                                                     .Distinct()
                                                     .ToList(),
                                DiffCount = g.Where(m => m.Data == null && m.SIGN_OUT_TIME != null).Count(),
                                DiffList = g.Where(m => m.Data == null && m.SIGN_OUT_TIME != null)
                                             .Select(m => new
                                             {
                                                 BagNumber = m.BAG_NUMBER,
                                                 MergeNumber = g.Count(x => x.BAG_NUMBER == m.BAG_NUMBER) > 1
                                                               ? ""
                                                               : m.MERGE_NUMBER
                                             }).Distinct().ToList(),
                            }).ToList();

            // 取得處置說明
            var processTypeRemark = new Dictionary<string, string>
            {
                { "3", "公司名義" },
                { "4", "現場轉出" }
            };

            var list = dt_Group.SelectMany(t =>
                t.DiffList.Where(r => string.IsNullOrEmpty(r.MergeNumber) == false)
                .Select(r => r.MergeNumber))
                .ToList();

            var process = GetProcess(list);

            irow = 1;

            foreach (var item in dt_Group)
            {
                // 客戶
                var customer = (from r in customerData
                                where r.MAIN_NUMBER == item.MainNumber
                                select r.DESPATCHNAME).ToList();

                int startRow = irow;
                int diffCount = item.DiffList.Count;
                int checkTrackingNoCount = item.CheckTrackingNoList.Count;
                int rowCount = Math.Max(Math.Max(diffCount, checkTrackingNoCount), 1);
                int endRow = startRow + rowCount - 1;

                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(item.MainNumber);
                row.CreateCell(1).SetCellValue(string.Join("，", customer));
                row.CreateCell(2).SetCellValue(item.TotalCount);
                row.CreateCell(3).SetCellValue(item.ScanCount);
                row.CreateCell(4).SetCellValue(item.CheckCount);
                row.CreateCell(5).SetCellValue(item.CheckBagNumber);
                row.CreateCell(6).SetCellValue(item.DiffCount);

                for (int index = 0; index < rowCount; index++)
                {
                    int currentRowIndex = startRow + index;
                    row = sheet.GetRow(currentRowIndex) ?? sheet.CreateRow(currentRowIndex);

                    if (index < item.DiffList.Count)
                    {
                        var diff = item.DiffList[index];
                        row.CreateCell(7).SetCellValue(diff.BagNumber);
                        row.CreateCell(8).SetCellValue(diff.MergeNumber);
                        if (process.ContainsKey(diff.MergeNumber))
                        {
                            var processType = process[diff.MergeNumber];
                            if (processTypeRemark.ContainsKey(processType))
                            {
                                row.CreateCell(9).SetCellValue(processTypeRemark[processType]);
                            }
                        }
                    }

                    if (index < item.CheckTrackingNoList.Count)
                    {
                        row.CreateCell(10).SetCellValue(item.CheckTrackingNoList[index]);
                    }
                }

                irow += rowCount;

                // 合併 A~G 欄 (0~6 欄)
                if (rowCount > 1)
                {
                    for (int col = 0; col <= 6; col++)
                    {
                        var mergeRegion = new CellRangeAddress(startRow, endRow, col, col);
                        sheet.AddMergedRegion(mergeRegion);
                    }
                }
            }

            return sheet;
        }
    }
}
