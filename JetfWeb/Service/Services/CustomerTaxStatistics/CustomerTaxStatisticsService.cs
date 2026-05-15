using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Models.CustomerTaxCalculate;
using Service.Services.CustomerTaxStatistics.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CustomerTaxStatistics
{
    public class CustomerTaxStatisticsService : _BaseService
    {
        public CustomerTaxStatisticsService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得客戶列表
        /// </summary>
        /// <returns></returns>
        public List<CustomerTaxStatisticsCustomerModel> GetCustomers()
        {
            string sql = @"
                select a.Cust_Code, b.CUST_NAME 
                from [jetf].[dbo].[CustomerTaxSetting] a
                join DATA_CENTER.dbo.SYS_CUST b on a.Cust_Code = b.CUST_CODE
                order by a.Cust_Code";

            return conn.Query<CustomerTaxStatisticsCustomerModel>(sql).ToList();
        }

        /// <summary>
        /// 取得客戶原始資料稅金
        /// </summary>
        /// <param name="customerCode">客戶代號</param>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <returns></returns>
        public List<CustomerTaxFeeMasterDataModel> GetCustomerTaxFeeMasterData(string customerCode, string startDate, string endDate)
        {
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
                left join DATA_CENTER.dbo.SYS_CUST b on a.CUSTOMER = b.CUST_CODE
                WHERE DATADATE between @StartDate and @EndDate
                and CUSTOMER = @CUSTOMER
                ORDER BY DATADATE, TRACKINGNO";

            return conn.Query<CustomerTaxFeeMasterDataModel>(sql, new
            {
                StartDate = startDate,
                EndDate = endDate,
                CUSTOMER = customerCode
            }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 取得稅金總表
        /// </summary>
        /// <param name="customerCode">客戶代號</param>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <returns></returns>
        public List<CustomerTaxFeeMasterDataModel> GetFeeMasterData(string customerCode, string startDate, string endDate)
        {
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
                DLV_COM
                FROM jetf.[dbo].[Fee_Master] a
                left join DATA_CENTER.dbo.SYS_CUST b on a.CUSTOMER = b.CUST_CODE
                WHERE DATADATE between @StartDate and @EndDate
                and CUSTOMER = @CUSTOMER
                ORDER BY DATADATE, TRACKINGNO";

            return conn.Query<CustomerTaxFeeMasterDataModel>(sql, new
            {
                StartDate = startDate,
                EndDate = endDate,
                CUSTOMER = customerCode
            }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 匯出Excel
        /// </summary>
        /// <param name="customerCode">客戶代號</param>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <returns></returns>
        public CustomerTaxStatisticsExportResult ExportExcel(string customerCode, string startDate, string endDate)
        {
            try
            {
                // 取得客戶原始資料稅金
                var customerTaxData = GetCustomerTaxFeeMasterData(customerCode, startDate, endDate);

                // 取得稅金總表
                var feeMasterData = GetFeeMasterData(customerCode, startDate, endDate);

                if (!customerTaxData.Any() && !feeMasterData.Any())
                {
                    return new CustomerTaxStatisticsExportResult
                    {
                        Success = false,
                        Message = $"客戶 {customerCode} 在 {startDate}~{endDate} 期間查無資料可匯出"
                    };
                }

                // 取得客戶名稱
                var customer = GetCustomers().FirstOrDefault(c => c.Cust_Code == customerCode);
                string customerName = customer?.CUST_NAME ?? customerCode;

                // 建立Excel檔案
                var (fileName, fileData) = CreateTaxStatisticsExcel(customerCode, customerName, startDate, endDate, customerTaxData, feeMasterData);

                return new CustomerTaxStatisticsExportResult
                {
                    Success = true,
                    FileName = fileName,
                    FileData = fileData,
                    RecordCount = customerTaxData.Count + feeMasterData.Count,
                    Message = "匯出成功"
                };
            }
            catch (Exception ex)
            {
                return new CustomerTaxStatisticsExportResult
                {
                    Success = false,
                    Message = $"匯出失敗：{ex.Message}"
                };
            }
        }

        /// <summary>
        /// 建立稅金結算Excel檔案
        /// </summary>
        /// <param name="customerCode">客戶代號</param>
        /// <param name="customerName">客戶名稱</param>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <param name="customerTaxData">客戶原始資料稅金</param>
        /// <param name="feeMasterData">稅金總表資料</param>
        /// <returns></returns>
        private (string FileName, byte[] FileData) CreateTaxStatisticsExcel(string customerCode, string customerName, string startDate, string endDate, 
            List<CustomerTaxFeeMasterDataModel> customerTaxData, List<CustomerTaxFeeMasterDataModel> feeMasterData)
        {
            // 建立Excel檔案
            IWorkbook workbook = new XSSFWorkbook();

            // 建立第一個頁籤 - 客戶原始資料稅金
            CreateTaxStatisticsSheet1(workbook, customerTaxData);

            // 建立第二個頁籤 - 稅金總表
            CreateTaxStatisticsSheet2(workbook, feeMasterData);

            // 建立第三個頁籤 - 稅金差異
            CreateTaxStatisticsSheet3(workbook, customerTaxData, feeMasterData);

            // 產生檔名：客戶名稱_客戶代號_日期區間_稅金結算表.xlsx
            var fileName = $"{customerName}_{customerCode}_{startDate.Replace("-", "")}_{endDate.Replace("-", "")}_稅金結算表.xlsx";

            // 將Excel轉為byte陣列
            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                return (fileName, fileStream.ToArray());
            }
        }

        /// <summary>
        /// 建立第一個頁籤 - 客戶原始資料稅金
        /// </summary>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="customerTaxData">客戶原始資料稅金</param>
        private void CreateTaxStatisticsSheet1(IWorkbook workbook, List<CustomerTaxFeeMasterDataModel> customerTaxData)
        {
            ISheet sheet = workbook.CreateSheet("客戶原始資料稅金");

            // 使用共用樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");
            var datetimeStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);

            // 設定欄位標題（加上作業日為第一欄）
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = {
                "作業日", "資料來源", "報關類別", "客戶", "主號", "清關袋號", "分提單號",
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
            for (int i = 0; i < customerTaxData.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var data = customerTaxData[i];
                var col = 0;

                // 作業日（DATADATE 轉換為日期格式）
                var dataDate = ParseDataDate(data.DATADATE);
                NpoiCell.CreateDateTimeCell(dataRow, col++, dataDate, dateStyle);
                NpoiCell.CreateCell(dataRow, col++, data.SOURCE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DESPATCH_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.MAIN_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TRACKINGNO, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DLV_INV, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.CLEARANCE_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TAX_NUMBER, dataStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.IN_DATETIME, datetimeStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.OUT_DATETIME, datetimeStyle);
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
        /// 建立第二個頁籤 - 稅金總表
        /// </summary>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="feeMasterData">稅金總表資料</param>
        private void CreateTaxStatisticsSheet2(IWorkbook workbook, List<CustomerTaxFeeMasterDataModel> feeMasterData)
        {
            ISheet sheet = workbook.CreateSheet("稅金總表");

            // 使用共用樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");
            var datetimeStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);

            // 設定欄位標題（加上作業日為第一欄）
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = {
                "作業日", "資料來源", "報關類別", "客戶", "主號", "清關袋號", "分提單號",
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
            for (int i = 0; i < feeMasterData.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var data = feeMasterData[i];
                var col = 0;

                // 作業日（DATADATE 轉換為日期格式）
                var dataDate = ParseDataDate(data.DATADATE);
                NpoiCell.CreateDateTimeCell(dataRow, col++, dataDate, dateStyle);
                NpoiCell.CreateCell(dataRow, col++, data.SOURCE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DESPATCH_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.MAIN_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TRACKINGNO, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.DLV_INV, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.CLEARANCE_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.TAX_NUMBER, dataStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.IN_DATETIME, datetimeStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.OUT_DATETIME, datetimeStyle);
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
        /// 建立第三個頁籤 - 稅金差異
        /// </summary>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="customerTaxData">客戶原始資料稅金</param>
        /// <param name="feeMasterData">稅金總表資料</param>
        private void CreateTaxStatisticsSheet3(IWorkbook workbook, List<CustomerTaxFeeMasterDataModel> customerTaxData, List<CustomerTaxFeeMasterDataModel> feeMasterData)
        {
            ISheet sheet = workbook.CreateSheet("稅金差異");

            // 使用共用樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");
            var datetimeStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);
            var differenceStyle = NpoiStyle.CreateColorStyle(workbook, NPOI.HSSF.Util.HSSFColor.Red.Index, true, numberStyle);

            // 設定欄位標題（加上作業日為第一欄，並新增差異金額欄位）
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = {
                "作業日", "資料來源", "報關類別", "客戶", "主號", "清關袋號", "分提單號",
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

            // 計算稅金差異資料
            var differenceData = CalculateTaxDifference(customerTaxData, feeMasterData);

            // 填入差異資料
            for (int i = 0; i < differenceData.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var data = differenceData[i];
                var col = 0;

                // 作業日（DATADATE 轉換為日期格式）
                var dataDate = ParseDataDate(data.FeeMasterData.DATADATE);
                NpoiCell.CreateDateTimeCell(dataRow, col++, dataDate, dateStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.SOURCE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.TYPE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.DESPATCH_NAME, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.MAIN_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.TRACKINGNO, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.DLV_INV, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.CLEARANCE_NUMBER, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.TAX_NUMBER, dataStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.FeeMasterData.IN_DATETIME, datetimeStyle);
                NpoiCell.CreateDateTimeCell(dataRow, col++, data.FeeMasterData.OUT_DATETIME, datetimeStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.FeeMasterData.TAX_BASE, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.FeeMasterData.TAX1, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.FeeMasterData.TAX2, numberStyle);
                NpoiCell.CreateIntCell(dataRow, col++, data.FeeMasterData.TotalTax, numberStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.RECIPIENT, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.RECPHONE, dataStyle);
                NpoiCell.CreateCell(dataRow, col++, data.FeeMasterData.DLV_COM, dataStyle);

                // 差異金額欄位使用特殊樣式
                NpoiCell.CreateIntCell(dataRow, col++, data.DifferenceAmount, differenceStyle);
            }
        }

        /// <summary>
        /// 計算稅金差異資料
        /// </summary>
        /// <param name="customerTaxData">客戶原始資料稅金</param>
        /// <param name="feeMasterData">稅金總表資料</param>
        /// <returns>差異資料列表</returns>
        private List<TaxStatisticsDifferenceItemModel> CalculateTaxDifference(List<CustomerTaxFeeMasterDataModel> customerTaxData, List<CustomerTaxFeeMasterDataModel> feeMasterData)
        {
            var differenceList = new List<TaxStatisticsDifferenceItemModel>();

            // 將客戶原始資料按 TRACKINGNO + DATADATE 分組，並計算稅金合計
            var customerGroups = customerTaxData
                .Where(x => !string.IsNullOrEmpty(x.TRACKINGNO) && !string.IsNullOrEmpty(x.DATADATE))
                .GroupBy(x => new { x.TRACKINGNO, x.DATADATE })
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        TotalTax = g.Sum(x => x.TotalTax),
                        FirstRecord = g.First()
                    }
                );

            // 將稅金總表按 TRACKINGNO + DATADATE 分組，並計算稅金合計
            var feeMasterGroups = feeMasterData
                .Where(x => !string.IsNullOrEmpty(x.TRACKINGNO) && !string.IsNullOrEmpty(x.DATADATE))
                .GroupBy(x => new { x.TRACKINGNO, x.DATADATE })
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        TotalTax = g.Sum(x => x.TotalTax),
                        FirstRecord = g.First()
                    }
                );

            // 以稅金總表為基準進行比對
            foreach (var feeMasterGroup in feeMasterGroups)
            {
                var key = feeMasterGroup.Key;
                var feeMasterTotalTax = feeMasterGroup.Value.TotalTax;
                var feeMasterRecord = feeMasterGroup.Value.FirstRecord;

                // 取得對應的客戶原始資料金額，如果不存在則為0
                var customerTotalTax = customerGroups.ContainsKey(key) ? customerGroups[key].TotalTax : 0;

                // 計算差異金額
                var differenceAmount = feeMasterTotalTax - customerTotalTax;

                // 只有金額不相同時才加入差異清單
                if (differenceAmount != 0)
                {
                    differenceList.Add(new TaxStatisticsDifferenceItemModel
                    {
                        FeeMasterData = feeMasterRecord,
                        CustomerData = customerGroups.ContainsKey(key) ? customerGroups[key].FirstRecord : null,
                        FeeMasterTotalTax = feeMasterTotalTax,
                        CustomerTotalTax = customerTotalTax,
                        DifferenceAmount = differenceAmount
                    });
                }
            }

            // 按差異金額的絕對值排序（差異最大的在前面）
            return differenceList.OrderByDescending(x => Math.Abs(x.DifferenceAmount)).ToList();
        }

        /// <summary>
        /// 解析 DATADATE 字串為日期
        /// </summary>
        /// <param name="dataDate">DATADATE字串（格式：yyyyMMdd）</param>
        /// <returns>日期或null</returns>
        private DateTime? ParseDataDate(string dataDate)
        {
            if (string.IsNullOrEmpty(dataDate) || dataDate.Length != 8)
                return null;

            if (DateTime.TryParseExact(dataDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            return null;
        }
    }
}
