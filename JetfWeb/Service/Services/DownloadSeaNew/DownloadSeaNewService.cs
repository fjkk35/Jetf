using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.DownloadSeaNew
{
    public class DownloadSeaNewService : _BaseService
    {
        private const string SeaSourceType = "2";
        private const int BatchSize = 500;
        private readonly GlobalService _globalService;

        /// <summary>
        /// 初始化海運物流代收下載服務。
        /// </summary>
        /// <param name="globalService">共用服務。</param>
        /// <param name="jetfDbContext">JETF 主資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public DownloadSeaNewService(GlobalService globalService, JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _globalService = globalService;
        }

        /// <summary>
        /// 取得海運一般代收檔匯出資料。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <returns>匯出結果。</returns>
        public DownloadSeaNewExportResult GetNormalExport(string date, string taxType)
        {
            return BuildExportResult(date, taxType, "N", SeaExportFileType.Normal);
        }

        /// <summary>
        /// 取得海運無客戶代收檔匯出資料。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <returns>匯出結果。</returns>
        public DownloadSeaNewExportResult GetErrorExport(string date, string taxType)
        {
            return BuildExportResult(date, taxType, string.Empty, SeaExportFileType.Error);
        }

        /// <summary>
        /// 取得海運特殊客戶 D 檔匯出資料。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <returns>匯出結果。</returns>
        public DownloadSeaNewExportResult GetSpecialDExport(string date, string taxType)
        {
            return BuildExportResult(date, taxType, "D", SeaExportFileType.SpecialD);
        }

        /// <summary>
        /// 取得海運特殊客戶 C 檔匯出資料。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <returns>匯出結果。</returns>
        public DownloadSeaNewExportResult GetSpecialCExport(string date, string taxType)
        {
            return BuildExportResult(date, taxType, "C", SeaExportFileType.SpecialC);
        }

        /// <summary>
        /// 依指定條件建立海運匯出結果。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="includeTax">稅金類型。</param>
        /// <param name="fileType">匯出檔案類型。</param>
        /// <returns>匯出結果。</returns>
        private DownloadSeaNewExportResult BuildExportResult(string date, string taxType, string includeTax, SeaExportFileType fileType)
        {
            var exportResult = new DownloadSeaNewExportResult();
            var reportResult = GetSeaReport(date, taxType, includeTax);

            exportResult.status = reportResult.status;
            exportResult.msg = reportResult.msg;
            exportResult.Rows = reportResult.Rows;

            if (reportResult.status != Status.success)
            {
                return exportResult;
            }

            // step 1: 先依舊版查詢條件取出報表資料。
            // step 2: 再依匯出類型與稅金種類組出舊系統一致的檔名。
            exportResult.FileName = BuildFileName(fileType, taxType, date, reportResult.Rows.Count);

            // step 3: 最後依匯出類型建立對應的 workbook，controller 不再處理報表格式細節。
            exportResult.FileBytes = CreateWorkbookBytes(fileType, reportResult.Rows);
            return exportResult;
        }

        /// <summary>
        /// 取得海運下載報表資料。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="includeTax">稅金類型。</param>
        /// <returns>報表資料。</returns>
        public DownloadSeaNewReportResult GetSeaReport(string date, string taxType, string includeTax)
        {
            var result = new DownloadSeaNewReportResult();

            try
            {
                if (!TryGetDataDate(date, out var dataDate))
                {
                    result.status = Status.error;
                    result.msg = "日期格式錯誤，請確認";
                    return result;
                }

                // step 1: 先從 FEE_MASTER 抓出這一天、這個稅金種類的原始資料。
                var feeMasters = LoadFeeMasters(dataDate, taxType, includeTax);
                var customerNames = GetSeaCustomerNames(feeMasters.Select(x => x.Customer));
                IEnumerable<FeeMasterEntity> filteredRows = feeMasters;

                if (!string.IsNullOrEmpty(includeTax) && includeTax != "D" && includeTax != "C")
                {
                    // step 2: 一般檔需補上客戶派件公司資訊，重現舊 SQL 的 company 條件與 union all 行為。
                    var companyLookup = BuildSeaCompanyLookup(feeMasters);

                    // 舊 SQL 透過 UNION ALL 保留兩段結果，這裡維持同樣的拼接方式，不主動去重。
                    filteredRows = feeMasters
                        .Where(x => x.Download == "1" && GetCompany(companyLookup, x.Customer, x.DlvCom) == "新竹物流")
                        .Concat(feeMasters.Where(x => x.SourceType == SeaSourceType));
                }

                    // step 3: 最後把 entity 轉成報表 DTO，避免 controller 再碰資料表欄位。
                result.Rows = filteredRows
                    .Select(x => MapReportItem(x, customerNames))
                    .ToList();
                result.status = Status.success;
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 依匯出類型建立 workbook bytes。
        /// </summary>
        /// <param name="fileType">匯出檔案類型。</param>
        /// <param name="rows">報表資料列。</param>
        /// <returns>workbook bytes。</returns>
        private byte[] CreateWorkbookBytes(SeaExportFileType fileType, IReadOnlyList<DownloadSeaNewReportItem> rows)
        {
            var workbook = fileType == SeaExportFileType.SpecialD || fileType == SeaExportFileType.SpecialC
                ? GetSeaSpecialWorkbook(rows)
                : GetSeaWorkbook(rows);

            using (var fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                return fileStream.ToArray();
            }
        }

        /// <summary>
        /// 建立海運一般檔 workbook。
        /// </summary>
        /// <param name="rows">報表資料列。</param>
        /// <returns>workbook。</returns>
        private IWorkbook GetSeaWorkbook(IReadOnlyList<DownloadSeaNewReportItem> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("報表");
            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("清關袋號");
            row.CreateCell(3).SetCellValue("運單號");
            row.CreateCell(4).SetCellValue("稅金");
            row.CreateCell(5).SetCellValue("納稅義務人");
            row.CreateCell(6).SetCellValue("電話");
            row.CreateCell(7).SetCellValue("備註");
            row.CreateCell(8).SetCellValue("派件公司");
            row.CreateCell(9).SetCellValue("稅金類別");

            for (var column = 0; column <= 9; column++)
            {
                sheet.SetColumnWidth(column, 6000);
            }
            sheet.SetColumnWidth(0, 3000);

            for (var i = 0; i < rows.Count; i++)
            {
                // 逐列填入一般檔格式，欄位與舊 DownloadSea workbook 保持一致。
                var item = rows[i];
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(item.CustomerName ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.TrackingNo ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.DlvInv ?? string.Empty);
                row.CreateCell(4).SetCellValue(item.ToDlvCod);
                row.CreateCell(5).SetCellValue(item.Recipient ?? string.Empty);
                row.CreateCell(6).SetCellValue(item.RecPhone ?? string.Empty);
                row.CreateCell(7).SetCellValue(GetRemark(item));
                row.CreateCell(8).SetCellValue(item.DlvCom ?? string.Empty);
                row.CreateCell(9).SetCellValue(_globalService.GetTaxType(item.IncludeTax ?? string.Empty));
            }

            return workbook;
        }

        /// <summary>
        /// 建立海運特殊客戶檔 workbook。
        /// </summary>
        /// <param name="rows">報表資料列。</param>
        /// <returns>workbook。</returns>
        private IWorkbook GetSeaSpecialWorkbook(IReadOnlyList<DownloadSeaNewReportItem> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("報表");
            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("清關袋號");
            row.CreateCell(3).SetCellValue("運單號");
            row.CreateCell(4).SetCellValue("稅金1");
            row.CreateCell(5).SetCellValue("稅金2");
            row.CreateCell(6).SetCellValue("納稅義務人");
            row.CreateCell(7).SetCellValue("電話");
            row.CreateCell(8).SetCellValue("備註");
            row.CreateCell(9).SetCellValue("稅金類別");

            for (var column = 0; column <= 9; column++)
            {
                sheet.SetColumnWidth(column, 6000);
            }
            sheet.SetColumnWidth(0, 3000);

            for (var i = 0; i < rows.Count; i++)
            {
                // 特殊客戶檔需拆出 TAX1 / TAX2，其他欄位維持舊格式。
                var item = rows[i];
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(item.CustomerName ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.TrackingNo ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.DlvInv ?? string.Empty);
                row.CreateCell(4).SetCellValue(item.Tax1);
                row.CreateCell(5).SetCellValue(item.Tax2);
                row.CreateCell(6).SetCellValue(item.Recipient ?? string.Empty);
                row.CreateCell(7).SetCellValue(item.RecPhone ?? string.Empty);
                row.CreateCell(8).SetCellValue(GetRemark(item));
                row.CreateCell(9).SetCellValue(_globalService.GetTaxType(item.IncludeTax ?? string.Empty));
            }

            return workbook;
        }

        /// <summary>
        /// 依日期、稅金種類與包稅條件載入 FEE_MASTER。
        /// </summary>
        /// <param name="dataDate">資料日期。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="includeTax">稅金類型。</param>
        /// <returns>符合條件的 fee master 清單。</returns>
        private List<FeeMasterEntity> LoadFeeMasters(string dataDate, string taxType, string includeTax)
        {
            var query = JetfDb.FeeMasters
                .AsNoTracking()
                .Where(x => x.DataDate == dataDate && x.Source == taxType)
                .OrderBy(x => x.Id);

            if (string.IsNullOrEmpty(includeTax))
            {
                // 無客戶檔只取 INCLUDE_TAX 為空的資料。
                return query
                    .Where(x => x.IncludeTax == null || x.IncludeTax == string.Empty)
                    .ToList();
            }

            return query
                .Where(x => x.IncludeTax == includeTax)
                .ToList();
        }

        /// <summary>
        /// 建立海運客戶與派件公司對照表。
        /// </summary>
        /// <param name="feeMasters">本次匯出的 fee master 資料。</param>
        /// <returns>客戶與派件公司對照表。</returns>
        private Dictionary<string, string> BuildSeaCompanyLookup(IEnumerable<FeeMasterEntity> feeMasters)
        {
            var customers = feeMasters
                .Select(x => x.Customer)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var transNames = feeMasters
                .Select(x => x.DlvCom)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var lookup = new Dictionary<string, string>();
            if (!customers.Any() || !transNames.Any())
            {
                return lookup;
            }

            foreach (var customerBatch in Batch(customers, BatchSize))
            {
                foreach (var transBatch in Batch(transNames, BatchSize))
                {
                    // 分批查 customer_master，避免單次 Contains 參數太大。
                    var items = JetfDb.CustomerMasters
                        .AsNoTracking()
                        .Where(x => x.TranType == "海運" && customerBatch.Contains(x.CustId) && transBatch.Contains(x.TransName))
                        .Select(x => new
                        {
                            x.CustId,
                            x.TransName,
                            x.Company
                        })
                        .ToList();

                    foreach (var item in items)
                    {
                        lookup[BuildCompositeKey(item.CustId, item.TransName)] = item.Company ?? string.Empty;
                    }
                }
            }

            return lookup;
        }

        /// <summary>
        /// 將 FEE_MASTER entity 轉成海運下載報表資料列。
        /// </summary>
        /// <param name="entity">fee master 資料。</param>
        /// <param name="customerNames">客戶名稱對照表。</param>
        /// <returns>報表資料列。</returns>
        private static DownloadSeaNewReportItem MapReportItem(FeeMasterEntity entity, IReadOnlyDictionary<string, string> customerNames)
        {
            customerNames.TryGetValue((entity.Customer ?? string.Empty).Trim(), out var customerName);

            return new DownloadSeaNewReportItem
            {
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? entity.Customer ?? string.Empty : customerName,
                TrackingNo = entity.TrackingNo ?? string.Empty,
                DlvInv = entity.DlvInv ?? string.Empty,
                Tax1 = entity.Tax1 ?? 0,
                Tax2 = entity.Tax2 ?? 0,
                ToDlvCod = ParseInt(entity.ToDlvCod),
                Recipient = entity.Recipient ?? string.Empty,
                RecPhone = entity.RecPhone ?? string.Empty,
                IncludeTax = entity.IncludeTax ?? string.Empty,
                Combine = entity.Combine ?? string.Empty,
                Type = entity.Type ?? string.Empty,
                DlvCom = entity.DlvCom ?? string.Empty
            };
        }

        /// <summary>
        /// 將 fee master 的文字金額安全轉成整數。
        /// </summary>
        /// <param name="value">原始文字。</param>
        /// <returns>整數結果，失敗時回傳 0。</returns>
        private static int ParseInt(string value)
        {
            return int.TryParse(value, out var number) ? number : 0;
        }

        /// <summary>
        /// 依客戶代號與派件公司名稱取得物流公司名稱。
        /// </summary>
        /// <param name="companyLookup">公司對照表。</param>
        /// <param name="customer">客戶代號。</param>
        /// <param name="transName">派件公司名稱。</param>
        /// <returns>物流公司名稱。</returns>
        private static string GetCompany(IReadOnlyDictionary<string, string> companyLookup, string customer, string transName)
        {
            companyLookup.TryGetValue(BuildCompositeKey(customer, transName), out var company);
            return company ?? string.Empty;
        }

        /// <summary>
        /// 依舊版規則回填備註欄位。
        /// </summary>
        /// <param name="item">報表資料列。</param>
        /// <returns>備註文字。</returns>
        private static string GetRemark(DownloadSeaNewReportItem item)
        {
            if (item.Combine == "Y")
            {
                return "併單";
            }

            if (item.Type == "G")
            {
                return "G類";
            }

            return "單";
        }

        /// <summary>
        /// 依匯出類型與稅金種類建立檔名。
        /// </summary>
        /// <param name="fileType">匯出檔案類型。</param>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="count">資料筆數。</param>
        /// <returns>檔名。</returns>
        private static string BuildFileName(SeaExportFileType fileType, string taxType, string date, int count)
        {
            var dataDate = Convert.ToDateTime(date).ToString("yyyyMMdd");
            var fileName = string.Empty;

            switch (fileType)
            {
                case SeaExportFileType.Normal:
                    fileName = BuildNormalFileName(taxType, dataDate, count);
                    break;
                case SeaExportFileType.Error:
                    fileName = BuildErrorFileName(taxType, dataDate, count);
                    break;
                case SeaExportFileType.SpecialD:
                    fileName = BuildSpecialDFileName(taxType, dataDate, count);
                    break;
                case SeaExportFileType.SpecialC:
                    fileName = BuildSpecialCFileName(taxType, dataDate, count);
                    break;
                default:
                    return string.Empty;
            }

            return fileName;
        }

        /// <summary>
        /// 建立一般檔檔名。
        /// </summary>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <param name="count">資料筆數。</param>
        /// <returns>檔名。</returns>
        private static string BuildNormalFileName(string taxType, string dataDate, int count)
        {
            switch (taxType)
            {
                case "TPCT":
                    return string.Format("{0}-tpct-新竹-{1}票.xlsx", dataDate, count);
                case "TIPC":
                    return string.Format("{0}-港務新竹-{1}票.xlsx", dataDate, count);
                case "IPOST":
                    return string.Format("{0}-高雄新竹(億興)-{1}票.xlsx", dataDate, count);
                case "CHWN":
                    return string.Format("{0}-高雄新竹(全旺)-{1}票.xlsx", dataDate, count);
                case "JFKH":
                    return string.Format("{0}-高雄新竹(捷豐)-{1}票.xlsx", dataDate, count);
                case "WAHA":
                    return string.Format("{0}-萬海新竹-{1}票.xlsx", dataDate, count);
                case "UNIJ":
                    return string.Format("{0}-連捷-{1}票.xlsx", dataDate, count);
                case "JFKL":
                    return string.Format("{0}-基隆港務(捷豐)-{1}票.xlsx", dataDate, count);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 建立無客戶檔檔名。
        /// </summary>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <param name="count">資料筆數。</param>
        /// <returns>檔名。</returns>
        private static string BuildErrorFileName(string taxType, string dataDate, int count)
        {
            switch (taxType)
            {
                case "TPCT":
                    return string.Format("{0}-tpct-新竹-無客戶{1}-票.xlsx", dataDate, count);
                case "TIPC":
                    return string.Format("{0}-港務新竹-無客戶{1}-票.xlsx", dataDate, count);
                case "IPOST":
                    return string.Format("{0}-高雄新竹(億興)-無客戶{1}-票.xlsx", dataDate, count);
                case "CHWN":
                    return string.Format("{0}-高雄新竹(全旺)-無客戶{1}票.xlsx", dataDate, count);
                case "JFKH":
                    return string.Format("{0}-高雄新竹(捷豐)-無客戶{1}票.xlsx", dataDate, count);
                case "WAHA":
                    return string.Format("{0}-萬海新竹-無客戶{1}票.xlsx", dataDate, count);
                case "UNIJ":
                    return string.Format("{0}-連捷-無客戶{1}票.xlsx", dataDate, count);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 建立特殊客戶 D 檔檔名。
        /// </summary>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <param name="count">資料筆數。</param>
        /// <returns>檔名。</returns>
        private static string BuildSpecialDFileName(string taxType, string dataDate, int count)
        {
            switch (taxType)
            {
                case "TPCT":
                    return string.Format("{0}-tpct-新竹-特殊客戶(收客匯款){1}-票.xlsx", dataDate, count);
                case "TIPC":
                    return string.Format("{0}-港務新竹-特殊客戶(收客匯款){1}-票.xlsx", dataDate, count);
                case "IPOST":
                    return string.Format("{0}-高雄新竹(億興)-特殊客戶(收客匯款){1}-票.xlsx", dataDate, count);
                case "CHWN":
                    return string.Format("{0}-高雄新竹(全旺)-特殊客戶(收客匯款){1}票.xlsx", dataDate, count);
                case "JFKH":
                    return string.Format("{0}-高雄新竹(捷豐)-特殊客戶(收客匯款){1}票.xlsx", dataDate, count);
                case "WAHA":
                    return string.Format("{0}-萬海新竹-特殊客戶(收客匯款){1}票.xlsx", dataDate, count);
                case "UNIJ":
                    return string.Format("{0}-連捷-特殊客戶(收客匯款){1}票.xlsx", dataDate, count);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 建立特殊客戶 C 檔檔名。
        /// </summary>
        /// <param name="taxType">海運稅金種類。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <param name="count">資料筆數。</param>
        /// <returns>檔名。</returns>
        private static string BuildSpecialCFileName(string taxType, string dataDate, int count)
        {
            switch (taxType)
            {
                case "TPCT":
                    return string.Format("{0}-tpct-新竹-特殊客戶(客戶付款){1}-票.xlsx", dataDate, count);
                case "TIPC":
                    return string.Format("{0}-港務新竹-特殊客戶(客戶付款){1}-票.xlsx", dataDate, count);
                case "IPOST":
                    return string.Format("{0}-高雄新竹-特殊客戶(客戶付款){1}-票.xlsx", dataDate, count);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 將畫面日期轉成資料日期格式。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="dataDate">輸出的資料日期。</param>
        /// <returns>是否轉換成功。</returns>
        private static bool TryGetDataDate(string date, out string dataDate)
        {
            dataDate = string.Empty;
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return false;
            }

            dataDate = parsedDate.ToString("yyyyMMdd");
            return true;
        }

        /// <summary>
        /// 將字串集合切成固定大小的批次。
        /// </summary>
        /// <param name="source">來源集合。</param>
        /// <param name="size">每批大小。</param>
        /// <returns>批次集合。</returns>
        private static IEnumerable<List<string>> Batch(IEnumerable<string> source, int size)
        {
            var current = new List<string>(size);

            foreach (var item in source ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                current.Add(item);
                if (current.Count >= size)
                {
                    yield return current;
                    current = new List<string>(size);
                }
            }

            if (current.Count > 0)
            {
                yield return current;
            }
        }

        /// <summary>
        /// 建立雙欄位組成的 dictionary key。
        /// </summary>
        /// <param name="left">左側值。</param>
        /// <param name="right">右側值。</param>
        /// <returns>組合後的 key。</returns>
        private static string BuildCompositeKey(string left, string right)
        {
            return string.Format("{0}|{1}", left ?? string.Empty, right ?? string.Empty);
        }

        private enum SeaExportFileType
        {
            Normal,
            Error,
            SpecialD,
            SpecialC
        }
    }

    /// <summary>
    /// 海運下載匯出結果。
    /// </summary>
    
    public sealed class DownloadSeaNewExportResult
    {
        /// <summary>
        /// 初始化海運下載匯出結果。
        /// </summary>
        public DownloadSeaNewExportResult()
        {
            status = Status.success;
            FileName = string.Empty;
            Rows = new List<DownloadSeaNewReportItem>();
        }

        /// <summary>
        /// 執行狀態。
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// 執行訊息。
        /// </summary>
        public string msg { get; set; } = string.Empty;

        /// <summary>
        /// 匯出檔名。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 匯出檔案內容。
        /// </summary>
        public byte[] FileBytes { get; set; }

        /// <summary>
        /// 匯出資料列。
        /// </summary>
        public List<DownloadSeaNewReportItem> Rows { get; set; }
    }

    /// <summary>
    /// 海運下載報表結果。
    /// </summary>
    public sealed class DownloadSeaNewReportResult
    {
        /// <summary>
        /// 初始化海運下載報表結果。
        /// </summary>
        public DownloadSeaNewReportResult()
        {
            status = Status.success;
            Rows = new List<DownloadSeaNewReportItem>();
        }

        /// <summary>
        /// 執行狀態。
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// 執行訊息。
        /// </summary>
        public string msg { get; set; } = string.Empty;

        /// <summary>
        /// 報表資料列。
        /// </summary>
        public List<DownloadSeaNewReportItem> Rows { get; set; }
    }

    /// <summary>
    /// 海運下載報表資料列。
    /// </summary>
    public sealed class DownloadSeaNewReportItem
    {
        public string CustomerName { get; set; }

        public string TrackingNo { get; set; }

        public string DlvInv { get; set; }

        public int Tax1 { get; set; }

        public int Tax2 { get; set; }

        public int ToDlvCod { get; set; }

        public string Recipient { get; set; }

        public string RecPhone { get; set; }

        public string IncludeTax { get; set; }

        public string Combine { get; set; }

        public string Type { get; set; }

        public string DlvCom { get; set; }
    }
}
