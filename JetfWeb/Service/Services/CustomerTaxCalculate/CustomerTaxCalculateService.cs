using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Models.CustomerTaxCalculate;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Service.Services.CustomerTaxCalculate
{
    public class CustomerTaxCalculateService : _BaseService
    {
        /// <summary>
        /// 取得稅金時間列表
        /// </summary>
        /// <returns></returns>
        public List<TaxTimeModel> GetTaxTimes()
        {
            string sql = @"
                SELECT 
                    tt.Id, 
                    tt.TaxTime
                FROM [jetf].[dbo].[TaxTime] tt
                ORDER BY tt.TaxTime";

            return conn.Query<TaxTimeModel>(sql).ToList();
        }

        /// <summary>
        /// 取得指定稅金時間的客戶代號列表
        /// </summary>
        /// <param name="taxTimeId">稅金時間ID</param>
        /// <returns></returns>
        public List<string> GetCustomerCodesByTaxTime(int taxTimeId)
        {
            string sql = @"
                SELECT DISTINCT cts.Cust_Code
                FROM [jetf].[dbo].[CustomerTaxSetting] cts
                INNER JOIN [jetf].[dbo].[CustomerTaxTime] ctt ON cts.Id = ctt.CustomerTaxSettingId
                WHERE ctt.TaxTimeId = @TaxTimeId
                ORDER BY cts.Cust_Code";

            return conn.Query<string>(sql, new { TaxTimeId = taxTimeId }).ToList();
        }

        /// <summary>
        /// 取得客戶列表
        /// </summary>
        /// <returns></returns>
        public List<CustomerTaxCalculateCustomerModel> GetCustomers()
        {
            string sql = @"
                select a.Cust_Code, b.CUST_NAME 
                from [jetf].[dbo].[CustomerTaxSetting] a
                join DATA_CENTER.dbo.SYS_CUST b on a.Cust_Code = b.CUST_CODE
                order by a.Cust_Code";

            return conn.Query<CustomerTaxCalculateCustomerModel>(sql).ToList();
        }

        /// <summary>
        /// 取得資料來源
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetDataType(List<string> list)
        {
            var sql = @"
        select distinct MAIN_NUMBER, CLEARANCE_CP 
        from DATA_CENTER.dbo.CES_MAIN_ORDER
        where MAIN_NUMBER IN @MAIN_NUMBER 
        and TYPE = 'O' and CLEARANCE_CP is not null
    ";

            var result = conn.Query(sql, new { MAIN_NUMBER = list })
                              .ToList();

            var dic = new Dictionary<string, string>();

            foreach (var row in result)
            {
                string mainNumber = row.MAIN_NUMBER;
                string cp = row.CLEARANCE_CP;

                switch (cp)
                {
                    case "CP01":
                        cp = "TPCT";
                        break;
                    case "CP02":
                    case "CP03":
                    case "CP04":
                        cp = "郵聯";
                        break;
                    default:
                        // 保持原值
                        break;
                }

                dic[mainNumber] = cp;
            }

            return dic;
        }

        /// <summary>
        /// 取得稅金計算資料
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <param name="selectedDate">選擇日期</param>
        /// <returns></returns>
        public List<CustomerTaxCalculateDataModel> GetTaxCalculateData(string custCode, DateTime selectedDate)
        {
            DateTime startDate = selectedDate.Date;
            DateTime endDate = selectedDate.Date.AddDays(1).AddSeconds(-1);

            var sql = @"
declare @CustName nvarchar(100);

select @CustName =CUST_NAME from DATA_CENTER.dbo.SYS_CUST
where CUST_CODE=@CustCode

;with SEA_ORDER_ORIGINAL as (
    select MAINNUMBER, BL_NO, TRANS_TAXPAYMENT, IMPORTER, IM_PHONENO, IM_ADD, 
           IMPORTER_ID, JETF_SERIAL, CC, MEMO, ARRIVAL, TRANS_NAME, @CustName as DESPATCH_NAME
    from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL
    where GW > 0 and DESPATCH_NAME = @CustCode
),
ETL_TIPC_TAX as (
    select distinct BAG_NUMBER,TAX_NUMBER,CLEARANCE_NUMBER,TAX_BASE,TAX_AMOUNT
    from DATA_CENTER.dbo.ETL_TIPC_TAX
    where SOURCE_TIME >= @StartDate and SOURCE_TIME < @EndDate
),
CLEARANCE_INFO as (
    select BAG_NUMBER,DATA_TYPE,CLEARANCE_TYPE,SIGN_IN_TIME,SIGN_OUT_TIME from DATA_CENTER.dbo.CLEARANCE_INFO
    where SIGN_OUT_TIME >= @StartDate and SIGN_OUT_TIME < @EndDate
)
select 
    a.MAINNUMBER,
    a.BL_NO,
    a.DESPATCH_NAME,
    a.TRANS_TAXPAYMENT,
    a.IMPORTER,
    a.IM_PHONENO,
    a.IM_ADD,
    a.IMPORTER_ID,
    a.JETF_SERIAL,
    a.CC,
    a.MEMO,
    a.ARRIVAL,
    a.TRANS_NAME,
    b.TAX_NUMBER,
    b.CLEARANCE_NUMBER,
    b.TAX_BASE,
    b.TAX_AMOUNT,
    c.DATA_TYPE,
    c.CLEARANCE_TYPE,
    c.SIGN_IN_TIME,
    c.SIGN_OUT_TIME
from SEA_ORDER_ORIGINAL a
join ETL_TIPC_TAX b on a.BL_NO = b.BAG_NUMBER
left join CLEARANCE_INFO c on a.BL_NO = c.BAG_NUMBER 
where exists (
        select 1 from DATA_CENTER.dbo.CLEARANCE_TAX
        where MODIFY_TIME >= @StartDate 
          and MODIFY_TIME < @EndDate
          and a.MAINNUMBER = MAIN_NUMBER 
          and a.BL_NO = BAG_NUMBER
    )
order by a.MAINNUMBER, a.BL_NO
";

            return conn.Query<CustomerTaxCalculateDataModel>(sql, new
            {
                CustCode = custCode,
                StartDate = startDate,
                EndDate = endDate
            }, commandTimeout: 300).ToList();
        }


        /// <summary>
        /// 取得稅金總表
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <param name="selectedDate">選擇日期</param>
        /// <returns></returns>
        public List<CustomerTaxFeeMasterDataModel> GetFeeMasterData(List<string> custCode, DateTime selectedDate)
        {
            var sql = @"
select
DATADATE,
SOURCE,
TYPE,
CUSTOMER,
b.CUST_NAME as DESPATCH_NAME,
MAIN_NUMBER,
TRACKINGNO,
DLV_INV,
CLEARANCE_NUMBER,
TAX_NUMBER,
IN_DATETIME,
OUT_DATETIME,
TAX_BASE,
TAX1,
TAX2,
RECIPIENT,
RECPHONE,
DLV_COM
from jetf.[dbo].[FEE_MASTER]a
left join DATA_CENTER.dbo.SYS_CUST b on a.CUSTOMER =b.CUST_CODE
where DATADATE=@DATADATE
and CUSTOMER IN @CUSTOMER
";

            return conn.Query<CustomerTaxFeeMasterDataModel>(sql, new
            {
                CUSTOMER = custCode,
                DATADATE = selectedDate.ToString("yyyyMMdd"),
            }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 匯出Excel
        /// </summary>
        /// <param name="taxTimeId">稅金時間ID</param>
        /// <param name="selectedDate">選擇日期</param>
        /// <returns></returns>
        public CustomerTaxCalculateExportResult ExportExcel(int taxTimeId, DateTime selectedDate)
        {
            try
            {
                // 取得該稅金時間的所有客戶代號
                var customerCodes = GetCustomerCodesByTaxTime(taxTimeId);

                if (!customerCodes.Any())
                {
                    return new CustomerTaxCalculateExportResult
                    {
                        Success = false,
                        Message = "該稅金時間區間沒有設定任何客戶"
                    };
                }

                // 取得稅金時間資訊
                var taxTimeInfo = GetTaxTimes().FirstOrDefault(t => t.Id == taxTimeId);
                string taxTimeText = taxTimeInfo?.TaxTime ?? taxTimeId.ToString();

                // 取得客戶資訊字典
                var customerDict = GetCustomers().ToDictionary(c => c.Cust_Code, c => c.CUST_NAME);

                var allDataList = new List<CustomerTaxCalculateDataModel>();

                // 取得所有客戶的資料
                foreach (var custCode in customerCodes)
                {
                    var dataList = GetTaxCalculateData(custCode, selectedDate);
                    allDataList.AddRange(dataList);
                }

                var mainNumbers = allDataList.Select(r => r.MAINNUMBER).Distinct().ToList();

                //資料來源
                var dataTypeDic = GetDataType(mainNumbers);
                allDataList.ForEach(r =>
                {
                    r.DATA_TYPE = dataTypeDic.ContainsKey(r.MAINNUMBER) ? dataTypeDic[r.MAINNUMBER] : r.DATA_TYPE; 
                });

                if (!allDataList.Any())
                {
                    return new CustomerTaxCalculateExportResult
                    {
                        Success = false,
                        Message = $"稅金時間 {taxTimeText} 在 {selectedDate:yyyy-MM-dd} 查無資料可匯出"
                    };
                }

                // 按客戶和資料來源分組
                var groupedData = allDataList
                    .GroupBy(x => new { Customer = x.DESPATCH_NAME, DataType = x.DATA_TYPE ?? "未分類" })
                    .ToList();

                // 建立ZIP壓縮檔
                using (var zipStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var group in groupedData)
                        {
                            var customerCode = group.Key.Customer;
                            var dataSource = group.Key.DataType;
                            var groupDataList = group.ToList();
                            var totalTaxAmount = groupDataList.Sum(x => x.TAX_AMOUNT ?? 0);

                            // 取得客戶名稱，如果沒有就使用客戶代號
                            var customerName = customerDict.ContainsKey(customerCode) 
                                ? customerDict[customerCode] 
                                : customerCode;

                            var (fileName, fileData) = CreateSingleExcel(customerName, dataSource, selectedDate, groupDataList, totalTaxAmount);

                            var entry = archive.CreateEntry(fileName);
                            using (var entryStream = entry.Open())
                            {
                                entryStream.Write(fileData, 0, fileData.Length);
                            }
                        }

                        // 如果稅金時間是20:00，額外產生稅金差異表
                        if (taxTimeText == "20:00")
                        {
                            // 寫入CustomerTaxFeeMaster表格（只有當天日期才寫入）
                            if (selectedDate.Date == DateTime.Today)
                            {
                                //稅金總表
                                var allFeeMasterData = GetFeeMasterData(customerCodes, selectedDate);

                                // 計算差異金額
                                var dataWithDifference = CalculateDifferenceAmounts(allFeeMasterData, allDataList);

                                WriteToCustomerTaxFeeMaster(allFeeMasterData, selectedDate);
                            }

                            //客戶結帳稅金資料
                            var allCustomerTaxFeeMaster = GetCustomerTaxFeeMaster(selectedDate);

                            var (diffFileName, diffFileData) = CreateTaxDifferenceExcel(selectedDate, allDataList, allCustomerTaxFeeMaster);
                            
                            var diffEntry = archive.CreateEntry(diffFileName);
                            using (var diffEntryStream = diffEntry.Open())
                            {
                                diffEntryStream.Write(diffFileData, 0, diffFileData.Length);
                            }
                        }
                    }

                    var zipFileName = $"稅金時間{taxTimeText}_{selectedDate:MMdd}_{DateTime.Now:HHmmss}.zip";

                    return new CustomerTaxCalculateExportResult
                    {
                        Success = true,
                        FileName = zipFileName,
                        FileData = zipStream.ToArray(),
                        RecordCount = allDataList.Count,
                        Message = taxTimeText == "20:00" 
                            ? $"匯出成功，共 {groupedData.Count + 1} 個檔案（含稅金差異表）"
                            : $"匯出成功，共 {groupedData.Count} 個檔案"
                    };
                }
            }
            catch (Exception ex)
            {
                return new CustomerTaxCalculateExportResult
                {
                    Success = false,
                    Message = $"匯出失敗：{ex.Message}"
                };
            }
        }

        /// <summary>
        /// 建立單個Excel檔案
        /// </summary>
        /// <param name="customerName">客戶名稱</param>
        /// <param name="dataSource">資料來源</param>
        /// <param name="selectedDate">選擇日期</param>
        /// <param name="dataList">資料列表</param>
        /// <param name="totalTaxAmount">稅金總額</param>
        /// <returns></returns>
        private (string FileName, byte[] FileData) CreateSingleExcel(string customerName, string dataSource, DateTime selectedDate, List<CustomerTaxCalculateDataModel> dataList, decimal totalTaxAmount)
        {
            // 建立Excel檔案
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(customerName);

            // 使用共用樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook, "#,##0.00");

            // 設定欄位標題
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = {
                "資料來源", "報關類別", "客戶", "清關袋號", "分提單號", "運單號",
                "主號", "報單號碼", "稅單號碼", "進倉時間", "出倉時間",
                "稅基", "稅金", "納稅義務人", "電話", "派件公司",
                "到付款", "備註"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = headerRow.CreateCell(i);
                headerCell.SetCellValue(headers[i]);
                headerCell.CellStyle = headerStyle;
            }

            // 設定欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            // 填入資料
            for (int i = 0; i < dataList.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var data = dataList[i];

                // 使用共用方法建立儲存格
                NpoiCell.CreateCell(dataRow, 0, data.DATA_TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, 1, data.CLEARANCE_TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, 2, data.DESPATCH_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, 3, data.BL_NO, dataStyle);
                NpoiCell.CreateCell(dataRow, 4, data.JETF_SERIAL, dataStyle);
                NpoiCell.CreateCell(dataRow, 5, data.JETF_SERIAL, dataStyle);
                NpoiCell.CreateCell(dataRow, 6, data.MAINNUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, 7, data.CLEARANCE_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, 8, data.TAX_NUMBER, dataStyle);
                NpoiCell.CreateDateTimeCell(dataRow, 9, data.SIGN_IN_TIME, dateStyle);
                NpoiCell.CreateDateTimeCell(dataRow, 10, data.SIGN_OUT_TIME, dateStyle);
                NpoiCell.CreateIntCell(dataRow, 11, data.TAX_BASE, numberStyle);
                NpoiCell.CreateIntCell(dataRow, 12, data.TAX_AMOUNT, numberStyle);
                NpoiCell.CreateCell(dataRow, 13, data.IMPORTER, dataStyle);
                NpoiCell.CreateCell(dataRow, 14, data.IM_PHONENO, dataStyle);
                NpoiCell.CreateCell(dataRow, 15, data.TRANS_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, 16, data.CC, dataStyle);
                NpoiCell.CreateCell(dataRow, 17, data.MEMO, dataStyle);
            }

            // 產生檔名：客戶+(資料來源)+MMdd+筆數(稅金總金額).xlsx
            var count = dataList.GroupBy(r => r.BL_NO).ToList().Count;
            var fileName = $"{customerName}({dataSource}){selectedDate:MMdd}稅金總表{count}({totalTaxAmount:F0}).xlsx";

            // 將Excel轉為byte陣列
            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                return (fileName, fileStream.ToArray());
            }
        }

        /// <summary>
        /// 建立稅金差異表Excel檔案
        /// </summary>
        /// <param name="selectedDate">選擇日期</param>
        /// <param name="allDataList">全部資料列表</param>
        /// <param name="allFeeMasterData">全部稅金總表資料</param>
        /// <returns></returns>
        private (string FileName, byte[] FileData) CreateTaxDifferenceExcel(DateTime selectedDate, List<CustomerTaxCalculateDataModel> allDataList, List<CustomerTaxFeeMasterDataModel> allFeeMasterData)
        {
            // 建立Excel檔案
            IWorkbook workbook = new XSSFWorkbook();

            // 建立第一個頁籤 - allDataList全部資料
            CreateTaxDifferenceSheet1(workbook, allDataList);

            // 建立第二個頁籤 - allFeeMasterData全部資料
            CreateTaxDifferenceSheet2(workbook, allFeeMasterData);

            // 建立第三個頁籤 - 金額差異資料
            CreateTaxDifferenceSheet3(workbook, allFeeMasterData);

            // 產生檔名：MMdd稅金差異表.xlsx
            var fileName = $"{selectedDate:MMdd}稅金差異表.xlsx";

            // 將Excel轉為byte陣列
            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                return (fileName, fileStream.ToArray());
            }
        }

        /// <summary>
        /// 建立稅金差異表第一個頁籤 - allDataList資料
        /// </summary>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="allDataList">全部資料列表</param>
        private void CreateTaxDifferenceSheet1(IWorkbook workbook, List<CustomerTaxCalculateDataModel> allDataList)
        {
            ISheet sheet = workbook.CreateSheet("原始資料");

            // 使用共用樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook, "#,##0.00");

            // 設定欄位標題
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = {
                "資料來源", "報關類別", "客戶", "清關袋號", "分提單號", "運單號",
                "主號", "報單號碼", "稅單號碼", "進倉時間", "出倉時間",
                "稅基", "稅金", "納稅義務人", "電話", "派件公司",
                "到付款", "備註"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = headerRow.CreateCell(i);
                headerCell.SetCellValue(headers[i]);
                headerCell.CellStyle = headerStyle;
            }

            // 設定欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            // 填入資料
            for (int i = 0; i < allDataList.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var data = allDataList[i];

                NpoiCell.CreateCell(dataRow, 0, data.DATA_TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, 1, data.CLEARANCE_TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, 2, data.DESPATCH_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, 3, data.BL_NO, dataStyle);
                NpoiCell.CreateCell(dataRow, 4, data.JETF_SERIAL, dataStyle);
                NpoiCell.CreateCell(dataRow, 5, data.JETF_SERIAL, dataStyle);
                NpoiCell.CreateCell(dataRow, 6, data.MAINNUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, 7, data.CLEARANCE_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, 8, data.TAX_NUMBER, dataStyle);
                NpoiCell.CreateDateTimeCell(dataRow, 9, data.SIGN_IN_TIME, dateStyle);
                NpoiCell.CreateDateTimeCell(dataRow, 10, data.SIGN_OUT_TIME, dateStyle);
                NpoiCell.CreateIntCell(dataRow, 11, data.TAX_BASE, numberStyle);
                NpoiCell.CreateIntCell(dataRow, 12, data.TAX_AMOUNT, numberStyle);
                NpoiCell.CreateCell(dataRow, 13, data.IMPORTER, dataStyle);
                NpoiCell.CreateCell(dataRow, 14, data.IM_PHONENO, dataStyle);
                NpoiCell.CreateCell(dataRow, 15, data.TRANS_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, 16, data.CC, dataStyle);
                NpoiCell.CreateCell(dataRow, 17, data.MEMO, dataStyle);
            }
        }

        /// <summary>
        /// 建立稅金差異表第二個頁籤 - allFeeMasterData資料
        /// </summary>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="allFeeMasterData">全部稅金總表資料</param>
        private void CreateTaxDifferenceSheet2(IWorkbook workbook, List<CustomerTaxFeeMasterDataModel> allFeeMasterData)
        {
            ISheet sheet = workbook.CreateSheet("稅金總表");

            // 使用共用樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook, "#,##0.00");

            // 設定欄位標題
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = {
                "資料來源", "報關類別", "客戶", "主號", "清關袋號", "分提單號",
                "報單號碼", "稅單號碼", "進倉時間", "出倉時間", "稅基", 
                "稅金1", "稅金2","稅金合計", "納稅義務人", "電話", "派件公司"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = headerRow.CreateCell(i);
                headerCell.SetCellValue(headers[i]);
                headerCell.CellStyle = headerStyle;
            }

            // 設定欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            // 填入資料
            for (int i = 0; i < allFeeMasterData.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var data = allFeeMasterData[i];
                var col = 0;
                NpoiCell.CreateCell(dataRow, col++, data.SOURCE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DESPATCH_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.MAIN_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TRACKINGNO, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DLV_INV, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.CLEARANCE_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TAX_NUMBER, dataStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.IN_DATETIME, dateStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.OUT_DATETIME, dateStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TAX_BASE, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TAX1, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TAX2, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TotalTax, numberStyle);
                NpoiCell.CreateCell(dataRow, col++, data.RECIPIENT, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.RECPHONE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DLV_COM, dataStyle);
            }
        }

        /// <summary>
        /// 建立稅金差異表第三個頁籤 - 金額差異資料
        /// </summary>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="allFeeMasterData">稅金總表資料</param>
        private void CreateTaxDifferenceSheet3(IWorkbook workbook, List<CustomerTaxFeeMasterDataModel> allFeeMasterData)
        {
            ISheet sheet = workbook.CreateSheet("金額差異");

            // 使用共用樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook, "#,##0.00");

            // 設定差異金額欄位的特殊樣式（紅色字體）
            var differenceStyle = NpoiStyle.CreateColorStyle(workbook, NPOI.HSSF.Util.HSSFColor.Red.Index, true, numberStyle);

            // 設定欄位標題（與第二個頁籤相同，但新增差異金額欄位）
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = {
                "資料來源", "報關類別", "客戶", "主號", "清關袋號", "分提單號",
                "報單號碼", "稅單號碼", "進倉時間", "出倉時間", "稅基", 
                "稅金1", "稅金2","稅金合計", "納稅義務人", "電話", "派件公司", "差異金額"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = headerRow.CreateCell(i);
                headerCell.SetCellValue(headers[i]);
                headerCell.CellStyle = headerStyle;
            }

            // 設定欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            // 計算金額差異資料
            var differenceData = allFeeMasterData.Where(r => r.DIFF_AMOUNT != 0).ToList();

            // 填入差異資料
            for (int i = 0; i < differenceData.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var data = differenceData[i];
                var col = 0;
                NpoiCell.CreateCell(dataRow, col++, data.SOURCE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DESPATCH_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.MAIN_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TRACKINGNO, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DLV_INV, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.CLEARANCE_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TAX_NUMBER, dataStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.IN_DATETIME, dateStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.OUT_DATETIME, dateStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TAX_BASE, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TAX1, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TAX2, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.TotalTax, numberStyle);
                NpoiCell.CreateCell(dataRow, col++, data.RECIPIENT, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.RECPHONE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DLV_COM, dataStyle);

                // 差異金額欄位使用特殊樣式
                NpoiCell.CreateIntCell(dataRow, col++, data.DIFF_AMOUNT, differenceStyle);
            }
        }

        /// <summary>
        /// 計算稅金差異資料
        /// </summary>
        /// <param name="allDataList">原始資料列表</param>
        /// <param name="allFeeMasterData">稅金總表資料</param>
        /// <returns>差異資料列表</returns>
        private List<TaxDifferenceItemModel> CalculateTaxDifference(List<CustomerTaxCalculateDataModel> allDataList, List<CustomerTaxFeeMasterDataModel> allFeeMasterData)
        {
            var differenceList = new List<TaxDifferenceItemModel>();

            // 將 allFeeMasterData 按 TRACKINGNO 分組，並計算 TAX1 + TAX2 的總和
            var feeMasterGroups = allFeeMasterData
                .Where(x => !string.IsNullOrEmpty(x.TRACKINGNO))
                .GroupBy(x => x.TRACKINGNO)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        TotalTax = g.Sum(x => (x.TAX1 ?? 0) + (x.TAX2 ?? 0)),
                        FirstRecord = g.First() // 用於顯示的代表資料
                    }
                );

            // 將 allDataList 按 BL_NO 分組，並計算 TAX_AMOUNT 的總和
            var dataListGroups = allDataList
                .Where(x => !string.IsNullOrEmpty(x.BL_NO))
                .GroupBy(x => x.BL_NO)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.TAX_AMOUNT ?? 0)
                );

            // 以 feeMasterGroups 為基準進行比對
            foreach (var feeMasterGroup in feeMasterGroups)
            {
                var blNo = feeMasterGroup.Key;
                var feeMasterTotalTax = feeMasterGroup.Value.TotalTax;
                var feeMasterRecord = feeMasterGroup.Value.FirstRecord;

                // 取得對應的 allDataList 金額，如果不存在則為0
                var dataListTotalTax = dataListGroups.ContainsKey(blNo) ? dataListGroups[blNo] : 0;

                // 計算差異金額
                var differenceAmount = feeMasterTotalTax - dataListTotalTax;

                // 只有金額不相同時才加入差異清單
                if (differenceAmount != 0)
                {
                    differenceList.Add(new TaxDifferenceItemModel
                    {
                        FeeMasterData = feeMasterRecord,
                        FeeMasterTotalTax = feeMasterTotalTax,
                        DataListTotalTax = dataListTotalTax,
                        DifferenceAmount = differenceAmount
                    });
                }
            }

            // 按差異金額的絕對值排序（差異最大的在前面）
            return differenceList.OrderByDescending(x => Math.Abs(x.DifferenceAmount)).ToList();
        }

        /// <summary>
        /// 寫入CustomerTaxFeeMaster表格
        /// </summary>
        /// <param name="allFeeMasterData">稅金總表資料</param>
        /// <param name="selectedDate">選擇日期</param>
        private void WriteToCustomerTaxFeeMaster(List<CustomerTaxFeeMasterDataModel> allFeeMasterData, DateTime selectedDate)
        {
            string dataDate = selectedDate.ToString("yyyyMMdd");
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 1. 刪除當天已存在的資料
                    string deleteSql = @"
                        DELETE FROM jetf.[dbo].[CustomerTaxFeeMaster] 
                        WHERE DATADATE = @DataDate";

                    conn.Execute(deleteSql, new { DataDate = dataDate }, transaction);

                    // 2. 如果有資料則插入新資料
                    if (allFeeMasterData.Any())
                    {
                        string insertSql = @"
                            INSERT INTO jetf.[dbo].[CustomerTaxFeeMaster]
                            (DATADATE, SOURCE, TYPE, CUSTOMER, MAIN_NUMBER, TRACKINGNO, 
                             CLEARANCE_NUMBER, TAX_NUMBER, DLV_INV, 
                             IN_DATETIME, OUT_DATETIME, TAX_BASE, TAX1, TAX2, 
                             RECIPIENT, RECPHONE, DLV_COM, DIFF_AMOUNT, CREATEOPE)
                            VALUES 
                            (@DATADATE, @SOURCE, @TYPE, @CUSTOMER, @MAIN_NUMBER, @TRACKINGNO,
                             @CLEARANCE_NUMBER, @TAX_NUMBER, @DLV_INV,
                             @IN_DATETIME, @OUT_DATETIME, @TAX_BASE, @TAX1, @TAX2,
                             @RECIPIENT, @RECPHONE, @DLV_COM, @DIFF_AMOUNT, @CREATEOPE)";

                        // 準備插入的資料
                        var insertData = allFeeMasterData.Select(data => new
                        {
                            DATADATE = dataDate,
                            SOURCE = data.SOURCE,
                            TYPE = data.TYPE,
                            CUSTOMER = data.CUSTOMER,
                            MAIN_NUMBER = data.MAIN_NUMBER,
                            TRACKINGNO = data.TRACKINGNO,
                            CLEARANCE_NUMBER = data.CLEARANCE_NUMBER,
                            TAX_NUMBER = data.TAX_NUMBER,
                            DLV_INV = data.DLV_INV,
                            IN_DATETIME = data.IN_DATETIME,
                            OUT_DATETIME = data.OUT_DATETIME,
                            TAX_BASE = (int?)(data.TAX_BASE ?? 0),
                            TAX1 = (int?)(data.TAX1 ?? 0),
                            TAX2 = (int?)(data.TAX2 ?? 0),
                            RECIPIENT = data.RECIPIENT,
                            RECPHONE = data.RECPHONE,
                            DLV_COM = data.DLV_COM,
                            DIFF_AMOUNT = data.DIFF_AMOUNT,
                            CREATEOPE = GetUserId(),
                        }).ToList();

                        conn.Execute(insertSql, insertData, transaction);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally 
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 取得客戶結帳稅金資料
        /// </summary>
        /// <param name="selectedDate"></param>
        /// <returns></returns>
        private List<CustomerTaxFeeMasterDataModel> GetCustomerTaxFeeMaster(DateTime selectedDate)
        {
            var dataDate = selectedDate.ToString("yyyyMMdd");

            var sql = @"
SELECT DATADATE,
SOURCE,
TYPE,
CUSTOMER,
b.CUST_NAME as DESPATCH_NAME,
MAIN_NUMBER,
TRACKINGNO,
DLV_INV,
CLEARANCE_NUMBER,
TAX_NUMBER,
IN_DATETIME,
OUT_DATETIME,
TAX_BASE,
TAX1,
TAX2,
RECIPIENT,
RECPHONE,
DLV_COM,
DIFF_AMOUNT
FROM jetf.[dbo].[CustomerTaxFeeMaster] a
left join DATA_CENTER.dbo.SYS_CUST b on a.CUSTOMER =b.CUST_CODE
WHERE DATADATE = @DataDate";

            return conn.Query<CustomerTaxFeeMasterDataModel>(sql, new
            {
                DataDate = dataDate
            }).ToList();
        }

        /// <summary>
        /// 計算差異金額並加入到資料中
        /// </summary>
        /// <param name="allFeeMasterData">稅金總表資料</param>
        /// <param name="allDataList">原始資料列表</param>
        /// <returns>包含差異金額的資料</returns>
        private List<CustomerTaxFeeMasterDataModel> CalculateDifferenceAmounts(List<CustomerTaxFeeMasterDataModel> allFeeMasterData, List<CustomerTaxCalculateDataModel> allDataList)
        {
            // 使用 CalculateTaxDifference 計算差異
            var differenceData = CalculateTaxDifference(allDataList, allFeeMasterData);
            var differenceDict = differenceData.ToDictionary(d => d.FeeMasterData.TRACKINGNO, d => d.DifferenceAmount);

            // 為每筆稅金總表資料計算對應的差異金額
            foreach (var data in allFeeMasterData)
            {
                var differenceAmount = 0;
                
                // 如果在差異清單中找到對應的TRACKINGNO，則使用計算出的差異金額
                if (!string.IsNullOrEmpty(data.TRACKINGNO) && differenceDict.ContainsKey(data.TRACKINGNO))
                {
                    differenceAmount = (int)differenceDict[data.TRACKINGNO];
                }
                // 如果不在差異清單中，表示金額相符，差異為0
                data.DIFF_AMOUNT = differenceAmount;
            }

            return allFeeMasterData;
        }
    }
}
