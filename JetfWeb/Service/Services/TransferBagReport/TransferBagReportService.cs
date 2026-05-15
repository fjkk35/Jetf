using Dapper;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Renci.SshNet;
using Service.Extensions;
using Service.Models.TransferBagReport;
using Service.Services.ScanCargoCustomer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.TransferBagReport
{
    public class TransferBagReportService : _BaseService
    {
        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle;
        iTextSharp.text.Font font8, font9, font10, font11, font12, font14, font16, font18, font20, fontB18;

        public TransferBagReportService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext) 
            : base(jetfDbContext, dataCenterDbContext)
        {

        }

        public IWorkbook GetWorkbook(string startDate, string endDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var list = GetList(startDate, endDate);
            //取得Excel樣式
            GetWorkbookStyle(workbook);

            //日期統計表
            GetDateReportSheet(workbook, list);

            var dates = list.Select(r => r.UploadTime)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            var sheetName = $"{startDate.ToDateTimeString("MMdd")}-{endDate.ToDateTimeString("MMdd")}";

            //取得交接單統計表(總表)Sheet
            GetCustomerReportSheet(workbook, sheetName, list);

            foreach (var date in dates)
            {
                sheetName = date.ToString("MMdd");
                var data = list.Where(r => r.UploadTime == date).ToList();
                //取得交接單統計表Sheet
                GetCustomerReportSheet(workbook, sheetName, data);
            }
           
            return workbook;
        }

        ///// <summary>
        ///// 取得掃貨上車PDT資料(客戶)
        ///// </summary>
        ///// <param name="trans"></param>
        ///// <param name="dataType"></param>
        ///// <param name="sDate"></param>
        ///// <param name="eDate"></param>
        ///// <returns></returns>
        //public List<TransferBagReportModel> GetScanCargoCustomerDetailsPdf(string sDate, string eDate)
        //{
        //    return GetList(sDate, eDate);
        //    //DataTable dt_Exclude = new DataTable();
        //    ////空快回艙資料
        //    //var dt_Exclude = _scanCargoCustomerService.GetScanCargoCustomerDetails("74", dataType, sDate, eDate);

        //    ////空快回艙資料，掃貨上車有掃到的資料
        //    //var filterExclude = from r in dt.AsEnumerable()
        //    //                    where dt_Exclude.AsEnumerable()
        //    //                                  .Any(x => r.Field<string>("Data") == x.Field<string>("Data"))
        //    //                    select r;

        //    //    if (filterExclude.Any())
        //    //    {
        //    //        dt_Exclude = filterExclude.CopyToDataTable();
        //    //    }
        //    //    else
        //    //    {
        //    //        dt_Exclude = new DataTable();
        //    //    }

        //    //    //排除空快回艙資料
        //    //    //dt = GetScanCargoCustomerDetailsExclude(dt, dt_Exclude);
        //    //}

        //    //return (dt, dt_Exclude);
        //}

        /// <summary>
        /// 日期統計表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public ISheet GetDateReportSheet(IWorkbook workbook, List<TransferBagReportModel> list)
        {
            var dateList = list.GroupBy(r => r.UploadTime)
                .Select(g => new 
                {
                    UploadTime = g.Key,
                    MorningTotal = g.Where(r => r.ScanTransNo == "63").Sum(r => r.Total),
                    NightTotal = g.Where(r => r.ScanTransNo == "76").Sum(r => r.Total),
                    TotalCount = g.Sum(r => r.Total)
                })
                .OrderBy(r => r.UploadTime)
                .ToList();

            ISheet sheet = workbook.CreateSheet("日期統計表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("日期");
            row.CreateCell(1).SetCellValue("早班");
            row.CreateCell(2).SetCellValue("晚班");
            row.CreateCell(3).SetCellValue("合計");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 4000);
            sheet.SetColumnWidth(2, 4000);
            sheet.SetColumnWidth(3, 4000);

            var irow = 1;
            foreach (var item in dateList)
            {
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(item.UploadTime.ToString("MM月dd日"));
                row.CreateCell(1).SetCellValue(item.MorningTotal);
                row.CreateCell(2).SetCellValue(item.NightTotal);
                row.CreateCell(3).SetCellValue(item.TotalCount);
                row.GetCell(0).CellStyle = cs_Title_Left;
                row.GetCell(1).CellStyle = cs_Int;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                irow++;
            }
            return sheet;
        }

        /// <summary>
        /// 客戶統計表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public ISheet GetCustomerReportSheet(IWorkbook workbook,string sheetName, List<TransferBagReportModel> list)
        {
            int irow, subTotal;
            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("交接單統計表");
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 2));
            row.GetCell(0).CellStyle = cs_Center;

            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("派件公司");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("件數");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 4000);

            irow = 2;
            var transNameGroup = list.GroupBy(t => t.TransName);
            foreach (var item in transNameGroup)
            {
                var dt_Group = from t in list
                               where t.TransName == item.Key
                               group t by new { TransName = t.TransName, DespatchName = t.DespatchName } into g
                               orderby g.Key.TransName, g.Key.DespatchName
                               select new
                               {
                                   TransName = g.Key.TransName,
                                   DespatchName = g.Key.DespatchName,
                                   TotalCount = g.Sum(x => x.Total),
                               };
                subTotal = 0;
                foreach (var item2 in dt_Group)
                {
                    row = sheet.CreateRow(irow);
                    row.CreateCell(0).SetCellValue(item2.TransName);
                    row.CreateCell(1).SetCellValue(item2.DespatchName);
                    row.CreateCell(2).SetCellValue(item2.TotalCount);
                    row.GetCell(0).CellStyle = cs_Title_Left;
                    row.GetCell(1).CellStyle = cs_Title_Left;
                    row.GetCell(2).CellStyle = cs_Int;
                    subTotal += item2.TotalCount;
                    irow++;
                }
                //小計
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue("小計");
                row.CreateCell(2).SetCellValue(subTotal);
                row.GetCell(0).CellStyle = cs_Center_Blue;
                row.GetCell(2).CellStyle = cs_Int_Blue;
                irow++;
            }
            return sheet;


        }

        public ISheet GetHandoverReportSheet(IWorkbook workbook, DataTable dt)
        {
            int irow, subTotal;
            ISheet sheet = workbook.CreateSheet("交接單統計表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("交接單統計表");
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 2));
            row.GetCell(0).CellStyle = cs_Center;

            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("派件公司");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("件數");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 4000);

            irow = 2;
            var transNameGroup = dt.AsEnumerable().GroupBy(t => new { TransName = t.Field<string>("TransName") });
            foreach (var item in transNameGroup)
            {
                var dt_Group = from t in dt.AsEnumerable()
                               where t.Field<string>("TransName") == item.Key.TransName
                               group t by new { TransName = t.Field<string>("TransName"), DESPATCHNAME = t.Field<string>("DESPATCHNAME") } into g
                               orderby g.Key.TransName, g.Key.DESPATCHNAME
                               select new
                               {
                                   TransName = g.Key.TransName,
                                   DESPATCHNAME = g.Key.DESPATCHNAME,
                                   TotalCount = g.Count()
                               };
                subTotal = 0;
                foreach (var item2 in dt_Group)
                {
                    row = sheet.CreateRow(irow);
                    row.CreateCell(0).SetCellValue(item2.TransName);
                    row.CreateCell(1).SetCellValue(item2.DESPATCHNAME);
                    row.CreateCell(2).SetCellValue(item2.TotalCount);
                    row.GetCell(0).CellStyle = cs_Title_Left;
                    row.GetCell(1).CellStyle = cs_Title_Left;
                    row.GetCell(2).CellStyle = cs_Int;
                    subTotal += item2.TotalCount;
                    irow++;
                }
                //小計
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue("小計");
                row.CreateCell(2).SetCellValue(subTotal);
                row.GetCell(0).CellStyle = cs_Center_Blue;
                row.GetCell(2).CellStyle = cs_Int_Blue;
                irow++;
            }
            return sheet;
        }

        /// <summary>
        /// 取得掃貨上車PDT資料(客戶)
        /// </summary>
        /// <returns></returns>
        public List<TransferBagReportModel> GetList(string startDate, string endDate)
        {
            //63早班清關、76晚班清關
            var sql = @"
with PdtScanCargoUpload as (
    select a.TransNo as ScanTransNo,a.Data,CAST(DATEADD(HOUR, 5, a.UploadTime) AS DATE) as UploadTime,b.DESPATCHNO,b.CLEARANCEWAREHOUSING from [jetf].[dbo].[PdtScanCargoUpload] a
    join DATA_CENTER.[dbo].[ORIGINALLIST] b on a.Data=b.BAGNO 
    where a.TransNo in('63','76') and a.DataType in('TACT','FTZ') 
    and a.UploadTime between @startDate and @endDate
    union
    select a.TransNo as ScanTransNo,a.Data,CAST(DATEADD(HOUR, 5, a.UploadTime) AS DATE) as UploadTime,c.DESPATCHNO,c.CLEARANCEWAREHOUSING from [jetf].[dbo].[PdtScanCargoUpload] a
    join DATA_CENTER.[dbo].[ORIGINALLIST] c on a.Data=c.TRACKINGNO 
    where a.TransNo in('63','76') and a.DataType in('TACT','FTZ') 
    and a.UploadTime between @startDate and @endDate
),
Warehouse as(
	select Data from [jetf].[dbo].[PdtScanCargoUpload] a
	where a.TransNo ='74' and a.DataType in('TACT','FTZ') 
	and a.UploadTime between @startDate and @endDate
),
Report as (
	select * from PdtScanCargoUpload a
	where a.DESPATCHNO in (
	'00026', -- 韓國蝦皮
	'00028', -- 中國蝦皮(MCT)
	'00035', -- 中國蝦皮(JIELI)
	'00036', -- 中國蝦皮(Bofeng)
	'00038', -- 中國蝦皮(Vinflair)
	'00051', -- 東南亞蝦皮(印尼)
	'00053'  -- 東南亞蝦皮(越南)
	) and not exists (
	select Data from Warehouse
	where a.Data = Data
	)
)
select ScanTransNo ,UploadTime,[jetf].[dbo].[GetTRANS_NAME](CLEARANCEWAREHOUSING) as TransName,b.DespatchName,count(1) as Total from Report a
left join [DATA_CENTER].[dbo].[DESPATCHFROM] b on  a.DESPATCHNO=b.DESPATCHNO
group by ScanTransNo,UploadTime,CLEARANCEWAREHOUSING,a.DESPATCHNO,b.DespatchName
";

            return conn.Query<TransferBagReportModel>(sql,new 
            {
                startDate = startDate,
                endDate = endDate
            }, commandTimeout: 600).ToList();
        }

        /// <summary>
        /// 取得掃貨上車PDT資料(客戶)-排除空快回艙資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetScanCargoCustomerDetailsExclude(DataTable dt, DataTable dt_Exclude)
        {
            //掃貨上車資料，空快回艙有掃到需要移除
            if (dt.Rows.Count > 0 && dt_Exclude.Rows.Count > 0)
            {
                var filter = from r in dt.AsEnumerable()
                             where !dt_Exclude.AsEnumerable()
                                              .Any(x => r.Field<string>("Data") == x.Field<string>("Data"))
                             select r;

                dt = filter.CopyToDataTable();
            }
            return dt;
        }

        void GetWorkbookStyle(IWorkbook workbook)
        {
            //藍色的Style
            fontB = workbook.CreateFont();
            fontB.Color = NPOI.SS.UserModel.IndexedColors.Blue.Index;
            fontB.FontName = "微軟正黑體";
            fontB.FontHeightInPoints = 12;
            font1 = (XSSFFont)workbook.CreateFont();
            font1.FontName = "微軟正黑體";
            font1.FontHeightInPoints = 12;
            //標題
            cs_Title = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //標題
            cs_Title_Left = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title_Left.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Title_Left.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Title.BorderTop = BorderStyle.Thin;
            //cs_Title.BorderBottom = BorderStyle.Thin;
            //cs_Title.BorderLeft = BorderStyle.Thin;
            //cs_Title.BorderRight = BorderStyle.Thin;
            cs_Title_Left.SetFont(font1);

            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center.SetFont(font1);
            //cs_Center.BorderTop = BorderStyle.Thin;
            //cs_Center.BorderBottom = BorderStyle.Thin;
            //cs_Center.BorderLeft = BorderStyle.Thin;
            //cs_Center.BorderRight = BorderStyle.Thin;

            cs_Center_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center_Blue.BorderTop = BorderStyle.Thin;
            //cs_Center_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Center_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Center_Blue.BorderRight = BorderStyle.Thin;
            cs_Center_Blue.SetFont(fontB);

            format = (XSSFDataFormat)workbook.CreateDataFormat();
            cs_Int = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int.BorderTop = BorderStyle.Thin;
            //cs_Int.BorderBottom = BorderStyle.Thin;
            //cs_Int.BorderLeft = BorderStyle.Thin;
            //cs_Int.BorderRight = BorderStyle.Thin;
            cs_Int.DataFormat = format.GetFormat("#,##0");
            cs_Int.SetFont(font1);

            cs_Int_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int_Blue.BorderTop = BorderStyle.Thin;
            //cs_Int_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Int_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Int_Blue.BorderRight = BorderStyle.Thin;
            cs_Int_Blue.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue.SetFont(fontB);

            cs_Double = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Double.BorderTop = BorderStyle.Thin;
            //cs_Double.BorderBottom = BorderStyle.Thin;
            //cs_Double.BorderLeft = BorderStyle.Thin;
            //cs_Double.BorderRight = BorderStyle.Thin;
            cs_Double.DataFormat = format.GetFormat("#,##0.000");

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0.000%");
            cs_Percent2.SetFont(font1);

            dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            dateStyle.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm:ss");

        }
    }
}
