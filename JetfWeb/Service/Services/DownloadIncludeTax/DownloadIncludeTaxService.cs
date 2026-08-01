using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.DownloadIncludeTax
{
    /// <summary>
    /// 稅金總表及明細表查詢與 Excel 匯出服務。
    /// </summary>
    public sealed class DownloadIncludeTaxService : _BaseService
    {
        /// <summary>
        /// 建立服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">資料中心資料庫內容。</param>
        public DownloadIncludeTaxService(
            Service.Data.JetfDbContext jetfDbContext,
            Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 查詢資料並建立稅金總表及明細表 Excel。
        /// </summary>
        /// <param name="request">匯出條件。</param>
        /// <returns>Excel 檔案內容與檔名。</returns>
        public DownloadIncludeTaxExportResult Export(DownloadIncludeTaxRequest request)
        {
            DateTime startDate;
            DateTime endDate;
            ValidateRequest(request, out startDate, out endDate);

            var start = startDate.ToString("yyyyMMdd");
            var end = endDate.ToString("yyyyMMdd");
            var rows = GetReportRows(request.Source, start, end);
            var workbook = CreateWorkbook(rows);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return new DownloadIncludeTaxExportResult
                {
                    FileBytes = stream.ToArray(),
                    FileName = string.Format(
                        "{0}~{1}-稅金總表及明細表-{2}.xlsx",
                        start,
                        end,
                        request.Source == "1" ? "海運" : "空運")
                };
            }
        }

        /// <summary>
        /// 驗證日期與資料來源。
        /// </summary>
        /// <param name="request">匯出條件。</param>
        /// <param name="startDate">解析後的開始日期。</param>
        /// <param name="endDate">解析後的結束日期。</param>
        private static void ValidateRequest(
            DownloadIncludeTaxRequest request,
            out DateTime startDate,
            out DateTime endDate)
        {
            if (request == null)
            {
                throw new ArgumentException("查詢條件不可為空。");
            }

            if (!DateTime.TryParseExact(
                request.StartDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out startDate) ||
                !DateTime.TryParseExact(
                    request.EndDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out endDate))
            {
                throw new ArgumentException("日期格式錯誤，請使用 yyyy-MM-dd。");
            }

            if (startDate.Date > endDate.Date)
            {
                throw new ArgumentException("開始日期不可晚於結束日期。");
            }

            if (request.Source != "1" && request.Source != "3")
            {
                throw new ArgumentException("資料來源錯誤。");
            }
        }

        /// <summary>
        /// 使用 Dapper 查詢稅金明細。
        /// </summary>
        /// <param name="source">資料來源。</param>
        /// <param name="startDate">開始日期，yyyyMMdd。</param>
        /// <param name="endDate">結束日期，yyyyMMdd。</param>
        /// <returns>稅金明細。</returns>
        private List<DownloadIncludeTaxReportModel> GetReportRows(
            string source,
            string startDate,
            string endDate)
        {
            string sql;
            if (source == "1")
            {
                sql = @"
SELECT
    a.DATADATE AS DataDate, a.SOURCE AS Source, a.TYPE AS [Type], a.CUSTOMER AS CustId,
    a.DLV_COM AS TransNo, a.ARRIVAL AS Arrival, b.CUST_NAME AS CustName,
    a.CLEARANCE_NUMBER AS ClearanceNumber, a.TRACKINGNO AS BagNumber, a.DLV_INV AS TrackingNo,
    a.MAIN_NUMBER AS MainNumber, a.TAX_NUMBER AS TaxNumber, a.IN_DATETIME AS InDateTime,
    a.OUT_DATETIME AS OutDateTime, a.TAX_BASE AS TaxBase, a.TAX1 AS Tax1, a.TAX2 AS Tax2,
    a.RECIPIENT AS Recipient, a.RECPHONE AS RecPhone, c.TRANS_NAME AS TransName, a.COD AS Cod,
    a.INCLUDE_TAX AS IncludeTax, a.FEE AS Fee, a.DLV_INV AS DlvInv, a.TAX_PAYER AS TaxPayer,
    d.IMPORTER_ID AS ImporterId, d.IMPORTER AS Importer, a.CUSTOMER_COD AS CustomerCod,
    a.TRANS_COD AS TransCod, a.TAX_RECID AS TaxRecId
FROM jetf.dbo.FEE_MASTER a
LEFT JOIN Data_center.dbo.sys_cust b ON a.CUSTOMER = b.CUST_CODE
LEFT JOIN jetf.dbo.customer_master c ON a.CUSTOMER = c.CUST_ID
    AND a.DLV_COM = c.TRANS_NAME AND c.TRAN_TYPE = '海運'
LEFT JOIN DATA_CENTER.dbo.SEA_ORDER_EDIT d ON a.TRACKINGNO = d.BL_NO
    AND a.MAIN_NUMBER = d.MAINNUMBER AND d.ITEM_NO = '1'
WHERE a.DATADATE BETWEEN @sDate AND @eDate AND a.SOURCE_TYPE = '1'";
            }
            else
            {
                sql = @"
SELECT
    a.DATADATE AS DataDate, a.SOURCE AS Source, a.TYPE AS [Type], a.CUSTOMER AS CustId,
    a.DLV_COM AS TransNo, a.ARRIVAL AS Arrival, b.CUSTOMER AS CustName,
    a.CLEARANCE_NUMBER AS ClearanceNumber, a.BAG_NUMBER AS BagNumber, a.DLV_INV AS DlvInv,
    a.TRACKINGNO AS TrackingNo, a.MAIN_NUMBER AS MainNumber, a.TAX_NUMBER AS TaxNumber,
    a.IN_DATETIME AS InDateTime, a.OUT_DATETIME AS OutDateTime, a.TAX_BASE AS TaxBase,
    a.TAX1 AS Tax1, a.TAX2 AS Tax2, a.RECIPIENT AS Recipient, a.RECPHONE AS RecPhone,
    b.TRANS_NAME AS TransName, a.COD AS Cod, a.INCLUDE_TAX AS IncludeTax, a.FEE AS Fee,
    a.TAX_PAYER AS TaxPayer, c.RECID AS ImporterId, c.RECIPIENT AS Importer,
    a.CUSTOMER_COD AS CustomerCod, a.TRANS_COD AS TransCod, a.TAX_RECID AS TaxRecId,
    r.TrackingNo AS ReconciliationAirTrackingNo, r.Recipient AS ReconciliationAirRecipient,
    r.TaxRecId AS ReconciliationAirTaxRecId
FROM jetf.dbo.FEE_MASTER a
LEFT JOIN jetf.dbo.customer_master b ON [jetf].[dbo].[PadLeft]('0', a.CUSTOMER, 5) = b.CUST_ID
    AND a.DLV_COM = b.TRANS_NO AND b.TRAN_TYPE = '空運'
LEFT JOIN (SELECT DISTINCT TRACKINGNO, RECID, RECIPIENT FROM DATA_CENTER.dbo.MAKELIST) c
    ON a.TRACKINGNO = c.TRACKINGNO
LEFT JOIN jetf.dbo.ReconciliationAir r ON a.TRACKINGNO = r.TrackingNo
WHERE a.DATADATE BETWEEN @sDate AND @eDate AND a.SOURCE_TYPE = '3'";
            }

            var reportRows = conn.Query<DownloadIncludeTaxReportModel>(
                    sql,
                    new { sDate = startDate, eDate = endDate })
                .ToList();

            return source == "3"
                ? RemoveAirMakelistDuplicates(reportRows)
                : reportRows;
        }

        /// <summary>
        /// 移除空運報表因 MAKELIST 多筆資料造成的重複列。
        /// </summary>
        /// <param name="reportRows">Dapper 查詢結果。</param>
        /// <returns>去除重複後的報表資料。</returns>
        private static List<DownloadIncludeTaxReportModel> RemoveAirMakelistDuplicates(
            IEnumerable<DownloadIncludeTaxReportModel> reportRows)
        {
            return reportRows
                .GroupBy(x => new
                {
                    x.DataDate,
                    x.Source,
                    x.TrackingNo,
                    x.DlvInv,
                    x.MainNumber,
                    x.TaxNumber,
                    x.Tax1,
                    x.Tax2,
                    x.Cod,
                    x.Fee,
                    x.CustomerCod,
                    x.TransCod
                })
                .Select(group => group.First())
                .ToList();
        }

        /// <summary>
        /// 建立 Excel 工作簿及各類總表、明細頁籤。
        /// </summary>
        /// <param name="reportRows">報表資料。</param>
        /// <returns>Excel 工作簿。</returns>
        private static IWorkbook CreateWorkbook(List<DownloadIncludeTaxReportModel> reportRows)
        {
            var workbook = new XSSFWorkbook();
            if (reportRows == null || reportRows.Count == 0)
            {
                return workbook;
            }

            var customers = reportRows.Select(x => x.CustName).Distinct().OrderBy(x => x).ToList();
            var sources = reportRows.Select(x => x.Source).Distinct().OrderBy(x => x).ToList();
            var sourceSummary = reportRows
                .GroupBy(x => new { x.Source, x.DataDate })
                .Select(group => new DownloadIncludeTaxReportSummaryModel
                {
                    Source = group.Key.Source,
                    DataDate = group.Key.DataDate,
                    Tax1 = group.Sum(x => (long)(x.Tax1 ?? 0)),
                    Tax2 = group.Sum(x => (long)(x.Tax2 ?? 0)),
                    Cod = group.Sum(x => (long)(x.Cod ?? 0))
                })
                .OrderBy(x => x.DataDate)
                .ThenBy(x => x.Source)
                .ToList();
            CreateSourceSummarySheet(workbook, "稅金總表", sourceSummary);

            var customerSummary = reportRows
                .GroupBy(x => new { x.Source, x.CustName, x.DataDate })
                .Select(group => new DownloadIncludeTaxReportSummaryModel
                {
                    Source = group.Key.Source,
                    CustName = group.Key.CustName,
                    DataDate = group.Key.DataDate,
                    Tax1 = group.Sum(x => (long)(x.Tax1 ?? 0)),
                    Tax2 = group.Sum(x => (long)(x.Tax2 ?? 0)),
                    Cod = group.Sum(x => (long)(x.Cod ?? 0))
                })
                .OrderBy(x => x.DataDate)
                .ThenBy(x => x.Source)
                .ToList();

            foreach (var customer in customers)
            {
                CreateCustomerSummarySheet(
                    workbook,
                    string.Format("{0}總表", customer ?? "無客戶"),
                    customerSummary.Where(x => x.CustName == customer).ToList());
            }

            foreach (var source in sources)
            {
                CreateDetailSheet(
                    workbook,
                    string.Format("{0}明細", source ?? "無倉庫"),
                    reportRows.Where(x => x.Source == source).OrderBy(x => x.DataDate).ToList());
            }

            foreach (var customer in customers)
            {
                CreateDetailSheet(
                    workbook,
                    string.Format("{0}明細", customer ?? "無客戶"),
                    reportRows.Where(x => x.CustName == customer).OrderBy(x => x.DataDate).ToList());
            }

            return workbook;
        }

        /// <summary>
        /// 建立來源總表頁籤。
        /// </summary>
        private static void CreateSourceSummarySheet(
            IWorkbook workbook,
            string sheetName,
            List<DownloadIncludeTaxReportSummaryModel> rows)
        {
            var sheet = workbook.CreateSheet(sheetName);
            var headers = new[] { "項次", "日期", "資料來源", "稅金1", "稅金2", "稅金合計", "到付款" };
            WriteHeaders(sheet, headers);
            long allTax1 = 0, allTax2 = 0, allCod = 0, allTotalTax = 0;
            for (var index = 0; index < rows.Count; index++)
            {
                var item = rows[index];
                var totalTax = item.Tax1 + item.Tax2;
                allTax1 += item.Tax1;
                allTax2 += item.Tax2;
                allCod += item.Cod;
                allTotalTax += totalTax;
                var row = sheet.CreateRow(index + 1);
                row.CreateCell(0).SetCellValue(index + 1);
                row.CreateCell(1).SetCellValue(item.DataDate ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.Source ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.Tax1);
                row.CreateCell(4).SetCellValue(item.Tax2);
                row.CreateCell(5).SetCellValue(totalTax);
                row.CreateCell(6).SetCellValue(item.Cod);
            }
            var totalRow = sheet.CreateRow(rows.Count + 1);
            totalRow.CreateCell(2).SetCellValue("合計");
            totalRow.CreateCell(3).SetCellValue(allTax1);
            totalRow.CreateCell(4).SetCellValue(allTax2);
            totalRow.CreateCell(5).SetCellValue(allTotalTax);
            totalRow.CreateCell(6).SetCellValue(allCod);
        }

        /// <summary>
        /// 建立客戶總表頁籤。
        /// </summary>
        private static void CreateCustomerSummarySheet(
            IWorkbook workbook,
            string sheetName,
            List<DownloadIncludeTaxReportSummaryModel> rows)
        {
            var sheet = workbook.CreateSheet(sheetName);
            var headers = new[] { "項次", "日期", "資料來源", "客戶", "稅金1", "稅金2", "稅金合計", "到付款" };
            WriteHeaders(sheet, headers);
            for (var index = 0; index < rows.Count; index++)
            {
                var item = rows[index];
                var totalTax = item.Tax1 + item.Tax2;
                var row = sheet.CreateRow(index + 1);
                row.CreateCell(0).SetCellValue(index + 1);
                row.CreateCell(1).SetCellValue(item.DataDate ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.Source ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.CustName ?? string.Empty);
                row.CreateCell(4).SetCellValue(item.Tax1);
                row.CreateCell(5).SetCellValue(item.Tax2);
                row.CreateCell(6).SetCellValue(totalTax);
                row.CreateCell(7).SetCellValue(item.Cod);
            }
        }

        /// <summary>
        /// 建立明細頁籤。
        /// </summary>
        private static void CreateDetailSheet(
            IWorkbook workbook,
            string sheetName,
            List<DownloadIncludeTaxReportModel> rows)
        {
            var sheet = workbook.CreateSheet(sheetName);
            var headers = new[]
            {
                "項次", "日期", "資料來源", "報關類別", "客戶", "清關袋號", "分提單號", "主號",
                "報單號碼", "稅單號碼", "進倉時間", "出倉時間", "稅基", "稅金1", "稅金2",
                "稅金合計", "跟派件收", "跟廠商收", "納稅義務人", "電話", "派件公司", "到付款",
                "CUST_ID", "TRANS_NO", "是否包稅", "手續費", "物流貨號", "制單納稅義務人",
                "制單統一編號", "菜鳥LP單號", "納稅義務人身分證號"
            };
            WriteHeaders(sheet, headers);
            for (var index = 0; index < rows.Count; index++)
            {
                var item = rows[index];
                var tax1 = item.Tax1 ?? 0;
                var tax2 = item.Tax2 ?? 0;
                var hasReconciliationAir = !string.IsNullOrWhiteSpace(item.ReconciliationAirTrackingNo);
                var recipient = hasReconciliationAir
                    ? item.ReconciliationAirRecipient
                    : string.IsNullOrWhiteSpace(item.TaxPayer) ? item.Recipient : item.TaxPayer;
                var taxRecId = hasReconciliationAir ? item.ReconciliationAirTaxRecId : item.TaxRecId;
                var row = sheet.CreateRow(index + 1);
                row.CreateCell(0).SetCellValue(index + 1);
                row.CreateCell(1).SetCellValue(item.DataDate ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.Source ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.Type ?? string.Empty);
                row.CreateCell(4).SetCellValue(item.CustName ?? string.Empty);
                row.CreateCell(5).SetCellValue(item.BagNumber ?? string.Empty);
                row.CreateCell(6).SetCellValue(item.TrackingNo ?? string.Empty);
                row.CreateCell(7).SetCellValue(item.MainNumber ?? string.Empty);
                row.CreateCell(8).SetCellValue(item.ClearanceNumber ?? string.Empty);
                row.CreateCell(9).SetCellValue(item.TaxNumber ?? string.Empty);
                row.CreateCell(10).SetCellValue(item.InDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty);
                row.CreateCell(11).SetCellValue(item.OutDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty);
                row.CreateCell(12).SetCellValue(item.TaxBase ?? 0);
                row.CreateCell(13).SetCellValue(tax1);
                row.CreateCell(14).SetCellValue(tax2);
                row.CreateCell(15).SetCellValue(tax1 + tax2);
                row.CreateCell(16).SetCellValue(item.TransCod ?? 0);
                row.CreateCell(17).SetCellValue(item.CustomerCod ?? 0);
                row.CreateCell(18).SetCellValue(recipient ?? string.Empty);
                row.CreateCell(19).SetCellValue(item.RecPhone ?? string.Empty);
                row.CreateCell(20).SetCellValue(item.TransName ?? string.Empty);
                row.CreateCell(21).SetCellValue(item.Cod ?? 0);
                row.CreateCell(22).SetCellValue(item.CustId ?? string.Empty);
                row.CreateCell(23).SetCellValue(item.TransNo ?? string.Empty);
                row.CreateCell(24).SetCellValue(item.IncludeTax ?? string.Empty);
                row.CreateCell(25).SetCellValue(item.Fee ?? 0);
                row.CreateCell(26).SetCellValue(item.DlvInv ?? string.Empty);
                row.CreateCell(27).SetCellValue(item.Importer ?? string.Empty);
                row.CreateCell(28).SetCellValue(item.ImporterId ?? string.Empty);
                row.CreateCell(29).SetCellValue(item.Arrival ?? string.Empty);
                row.CreateCell(30).SetCellValue(taxRecId ?? string.Empty);
            }
        }

        /// <summary>
        /// 寫入頁籤表頭與欄寬。
        /// </summary>
        private static void WriteHeaders(ISheet sheet, string[] headers)
        {
            var headerRow = sheet.CreateRow(0);
            for (var index = 0; index < headers.Length; index++)
            {
                headerRow.CreateCell(index).SetCellValue(headers[index]);
                sheet.SetColumnWidth(index, index == 0 ? 3000 : 6000);
            }
        }

    }
}
