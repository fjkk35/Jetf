using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Services.SjlBilling.Domain;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace Service.Services.SjlBilling
{
    /// <summary>
    /// 捷利帳單服務。
    /// </summary>
    public class SjlBillingService : _BaseService
    {
        public SjlBillingService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        private const decimal BaseFee = 55m;
        private const decimal MinimumAddressCharge = 300m;

        /// <summary>
        /// 產生捷利帳單 Excel。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>Excel 工作簿。</returns>
        public IWorkbook GetWorkbook(SjlBillingQueryRequest request)
        {
            // 先做條件驗證與日期邊界處理，再進入資料查詢。
            var validated = ValidateRequest(request);
            var queryRows = GetQueryRows(validated.Item1, validated.Item2);

            // 先把原始資料轉成可計價的中介模型，再依派件公司輸出對應欄位格式。
            var exportRows = BuildExportRows(queryRows, request.TransName);
            if (!exportRows.Any())
            {
                throw new Exception("查無資料");
            }

            return CreateWorkbook(exportRows, request.TransName);
        }

        /// <summary>
        /// 驗證查詢條件。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>開始日期與結束日期隔日。</returns>
        private Tuple<DateTime, DateTime> ValidateRequest(SjlBillingQueryRequest request)
        {
            if (request == null)
            {
                throw new Exception("查詢條件不可為空");
            }

            if (string.IsNullOrWhiteSpace(request.StartDate) || string.IsNullOrWhiteSpace(request.EndDate) || string.IsNullOrWhiteSpace(request.TransName))
            {
                throw new Exception("請完整輸入日期起、日期迄與派件公司");
            }

            DateTime startDate;
            DateTime endDate;
            if (!DateTime.TryParseExact(request.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate)
                && !DateTime.TryParse(request.StartDate, out startDate))
            {
                throw new Exception("日期起格式不正確");
            }

            if (!DateTime.TryParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate)
                && !DateTime.TryParse(request.EndDate, out endDate))
            {
                throw new Exception("日期迄格式不正確");
            }

            if (startDate.Date > endDate.Date)
            {
                throw new Exception("日期起不可大於日期迄");
            }

            if (request.TransName != "大榮" && request.TransName != "捷通" && request.TransName != "捷穩通")
            {
                throw new Exception("派件公司僅支援大榮、捷通或捷穩通");
            }

            return Tuple.Create(startDate.Date, endDate.Date.AddDays(1));
        }

        /// <summary>
        /// 查詢捷利帳單資料。
        /// </summary>
        /// <param name="startDate">開始日期。</param>
        /// <param name="endDateExclusive">結束日期隔日。</param>
        /// <returns>原始查詢資料。</returns>
        private List<SjlBillingQueryRowModel> GetQueryRows(DateTime startDate, DateTime endDateExclusive)
        {
            var sql = @"
with info as (
    select distinct MAIN_NUMBER, BAG_NUMBER, SIGN_OUT_TIME
    from DATA_CENTER.dbo.CLEARANCE_INFO
      where SIGN_OUT_TIME >= @StartDate
      and SIGN_OUT_TIME < @EndDate
      and DATA_TYPE not in ('FTZ', 'TACT')
),
Original as (
    select MAINNUMBER, BL_NO, JETF_SERIAL,TRANS_NAME
    from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL
    where TRANS_NAME in (N'捷通',N'捷穩通',N'大榮')  and DESPATCH_NAME in ('CN00060','CN00063')
)
select
    b.SIGN_OUT_TIME as SignOutTime,
    a.MAINNUMBER as MainNumber,
    a.BL_NO as BlNo,
    a.JETF_SERIAL as JetfSerial,
	a.TRANS_NAME as OTransName,
	c.CreatedTime,
    c.BagNumber,
    c.Importer,
    c.Cod,
    c.ImporterAddr,
    c.ItemName,
    c.Qty,
    c.Volume,
    c.Gw,
    c.ImporterPhone,
	c.TransName,
	d.TAX1,
	d.TAX2,
	d.FEE,
	e.UploadTime as ScanCargoTime
from Original a
join info b on a.MAINNUMBER = b.MAIN_NUMBER and a.BL_NO = b.BAG_NUMBER
left join jetf.dbo.SjlShippingData c on a.JETF_SERIAL = c.JetfSerial
left join [jetf].[dbo].[FEE_MASTER] d on a.JETF_SERIAL=d.DLV_INV and d.Download=1 and d.INCLUDE_TAX ='N'
left join [jetf].[dbo].[PdtScanCargoUpload] e on a.JETF_SERIAL=e.Data and TransNo in (11,98)
";

            return conn.Query<SjlBillingQueryRowModel>(sql, new
            {
                StartDate = startDate,
                EndDate = endDateExclusive
            },commandTimeout:300).ToList();
        }

        /// <summary>
        /// 建立匯出資料。
        /// </summary>
        /// <param name="queryRows">原始查詢資料。</param>
        /// <param name="transName">派件公司。</param>
        /// <returns>匯出資料列表。</returns>
        private List<SjlBillingExportRowModel> BuildExportRows(List<SjlBillingQueryRowModel> queryRows, string transName)
        {
            // SQL 已一次查出大榮、捷通與捷穩通，匯出前再依派件公司規則做最後篩選。
            var exportRows = queryRows
                .Where(row => ShouldIncludeTransName(GetEffectiveTransName(row), transName))
                .Select(row => new SjlBillingExportRowModel
                {
                    SignOutTime = row.SignOutTime,
                    CreatedTime = row.CreatedTime,
                    ScanCargoTime = row.ScanCargoTime,
                    JetfSerial = row.JetfSerial,
                    BlNo = row.BlNo,
                    Importer = row.Importer,
                    Cod = row.Cod,
                    OtherFee = row.OtherFee,
                    ImporterAddr = row.ImporterAddr,
                    ItemName = row.ItemName,
                    Qty = row.Qty,
                    Volume = row.Volume ?? 0m,
                    Gw = row.Gw ?? 0m,
                    ImporterPhone = row.ImporterPhone,
                    BaseFee = BaseFee,
                    ExtraVolumeFee = CalculateExtraVolumeFee(row.Volume ?? 0m),
                    OverweightFee = CalculateOverweightFee(row.Gw ?? 0m)
                })
                .ToList();

            // 同一個 JetfSerial 只保留排序後的第一筆，避免重複資料重複計價與重複匯出。
            exportRows = exportRows
                .GroupBy(row => row.JetfSerial ?? string.Empty)
                .Select(group => group.First())
                .OrderBy(row => row.SignOutTime.HasValue ? row.SignOutTime.Value.Date : DateTime.MinValue)
                .ThenBy(row => row.ImporterAddr ?? string.Empty)
                .ThenBy(row => row.JetfSerial ?? string.Empty)
                .ToList();

            foreach (var row in exportRows)
            {
                // 總額先以材積計價計算，後續再套用最低收費與捷通擇大值規則。
                row.SubtotalAmount = row.BaseFee + row.ExtraVolumeFee;
                row.TotalAmount = row.SubtotalAmount;
                row.WeightChargeAmount = row.BaseFee + row.OverweightFee;
            }

            // 同日同地址同時套用最低收費，捷通系則於同一輪分組中比較整組重量計費與總額。
            ApplyMinimumCharge(exportRows, IsJtFamilyTransName(transName));

            return exportRows;
        }

        /// <summary>
        /// 取得實際派件公司；若捷利資料未帶值，則回退使用原始派件公司。
        /// </summary>
        private string GetEffectiveTransName(SjlBillingQueryRowModel row)
        {
            if (!string.IsNullOrWhiteSpace(row.TransName))
            {
                return row.TransName.Trim();
            }

            return string.IsNullOrWhiteSpace(row.OTransName) ? string.Empty : row.OTransName.Trim();
        }

        /// <summary>
        /// 判斷查詢條件是否應包含該派件公司資料。
        /// </summary>
        private bool ShouldIncludeTransName(string effectiveTransName, string requestTransName)
        {
            if (string.IsNullOrWhiteSpace(effectiveTransName) || string.IsNullOrWhiteSpace(requestTransName))
            {
                return false;
            }

            var normalizedTransName = effectiveTransName.Trim();
            var normalizedRequest = requestTransName.Trim();

            if (normalizedRequest == "捷通")
            {
                return normalizedTransName == "捷通" || normalizedTransName == "捷穩通";
            }

            return normalizedTransName == normalizedRequest;
        }

        /// <summary>
        /// 判斷是否為捷通系派件公司。
        /// </summary>
        private bool IsJtFamilyTransName(string transName)
        {
            return transName == "捷通" || transName == "捷穩通";
        }

        /// <summary>
        /// 套用同日同地址最低收費。
        /// </summary>
        /// <param name="rows">匯出資料。</param>
        /// <param name="useWeightChargeComparison">是否同時套用捷通系重量計費擇大值。</param>
        private void ApplyMinimumCharge(List<SjlBillingExportRowModel> rows, bool useWeightChargeComparison)
        {
            // 最低收費與捷通系應計價都以「同日同地址」為同一個計價單位。
            var groupedRows = GroupRowsByAddressCharge(rows);

            foreach (var group in groupedRows)
            {
                var groupRows = group.ToList();
                var subtotal = groupRows.Sum(row => row.SubtotalAmount);

                if (subtotal < MinimumAddressCharge && groupRows.Count > 0)
                {
                    // 材積計價整組未滿 300 時，第一筆承接整組最低收費，其餘資料列維持存在但金額歸 0。
                    groupRows[0].TotalAmount = MinimumAddressCharge;

                    for (int i = 1; i < groupRows.Count; i++)
                    {
                        groupRows[i].TotalAmount = 0m;
                    }
                }
                else
                {
                    // 已達最低收費時，維持每筆自己的材積計價結果。
                    foreach (var row in groupRows)
                    {
                        row.TotalAmount = row.SubtotalAmount;
                    }
                }

                // 捷通 / 捷穩通要以整組比較「最低收費後總額」與「重量計費總額」，
                // 若整組重量計費較高，則該組每筆 ChargeAmount 都改採各自的重量計費。
                var useWeightChargeAmount = useWeightChargeComparison
                    && groupRows.Sum(row => row.WeightChargeAmount) > groupRows.Sum(row => row.TotalAmount);

                foreach (var row in groupRows)
                {
                    row.ChargeAmount = useWeightChargeAmount ? row.WeightChargeAmount : row.TotalAmount;
                }
            }
        }

        /// <summary>
        /// 依同日同地址分組。
        /// </summary>
        /// <param name="rows">匯出資料。</param>
        /// <returns>分組資料。</returns>
        private IEnumerable<IGrouping<string, SjlBillingExportRowModel>> GroupRowsByAddressCharge(List<SjlBillingExportRowModel> rows)
        {
            return rows.GroupBy(row =>
                (row.SignOutTime.HasValue ? row.SignOutTime.Value.Date : DateTime.MinValue).ToString("yyyyMMdd")
                + "|"
                + (row.ImporterAddr ?? string.Empty).Trim());
        }

        /// <summary>
        /// 計算超才費。
        /// </summary>
        /// <param name="volume">材積。</param>
        /// <returns>超才費。</returns>
        private decimal CalculateExtraVolumeFee(decimal volume)
        {
            if (volume <= 4m)
            {
                return 0m;
            }

            // 超過 4 才的部分無條件進位，每才加收 20。
            return decimal.Ceiling(volume - 4m) * 20m;
        }

        /// <summary>
        /// 計算超重費。
        /// </summary>
        /// <param name="gw">重量。</param>
        /// <returns>超重費。</returns>
        private decimal CalculateOverweightFee(decimal gw)
        {
            if (gw <= 20m)
            {
                return 0m;
            }

            // 超過 20 公斤的部分無條件進位，每公斤加收 5。
            return decimal.Ceiling(gw - 20m) * 5m;
        }

        /// <summary>
        /// 建立 Excel 工作簿。
        /// </summary>
        /// <param name="rows">匯出資料。</param>
        /// <param name="transName">派件公司。</param>
        /// <returns>Excel 工作簿。</returns>
        private IWorkbook CreateWorkbook(List<SjlBillingExportRowModel> rows, string transName)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var defaultStyle = workbook.CreateCellStyle();

            var mainSheet = workbook.CreateSheet("明細");
            CreateMainSheet(mainSheet, rows, transName, defaultStyle);

            var taxSheet = workbook.CreateSheet("稅金");
            CreateTaxSheet(taxSheet, rows, defaultStyle);

            var summarySheet = workbook.CreateSheet("彙總");
            CreateSummarySheet(summarySheet, rows, defaultStyle);

            return workbook;
        }

        /// <summary>
        /// 建立主表工作表。
        /// </summary>
        private void CreateMainSheet(ISheet sheet, List<SjlBillingExportRowModel> rows, string transName, ICellStyle defaultStyle)
        {
            IRow headerRow = sheet.CreateRow(0);
            var headers = GetHeaders(transName);
            NpoiCell.CreateHeaderCells(headerRow, headers, defaultStyle);

            for (int i = 0; i < rows.Count; i++)
            {
                IRow row = sheet.CreateRow(i + 1);
                WriteRow(row, rows[i], transName, defaultStyle);
            }

            sheet.CreateFreezePane(0, 1);
            SetColumnWidths(sheet, transName);
        }

        /// <summary>
        /// 建立稅金工作表，只顯示稅金大於 0 的資料。
        /// </summary>
        private void CreateTaxSheet(ISheet sheet, List<SjlBillingExportRowModel> rows, ICellStyle defaultStyle)
        {
            NpoiCell.CreateHeaderCells(sheet.CreateRow(0), new List<string> { "運單號", "稅金", "日期" }, defaultStyle);

            var taxRows = rows.Where(row => (row.OtherFee ?? 0m) > 0m).ToList();
            for (int i = 0; i < taxRows.Count; i++)
            {
                var data = taxRows[i];
                var row = sheet.CreateRow(i + 1);
                NpoiCell.CreateCell(row, 0, data.JetfSerial, defaultStyle);
                NpoiCell.CreateDoubleCell(row, 1, ConvertNullableDecimal(data.OtherFee), defaultStyle);
                NpoiCell.CreateCell(row, 2, FormatDate(data.SignOutTime, "yyyy/MM/dd"), defaultStyle);
            }

            sheet.CreateFreezePane(0, 1);
            SetSheetColumnWidths(sheet, new[] { 18, 12, 14 });
        }

        /// <summary>
        /// 建立彙總工作表，依日期加總運費並於最後一列輸出合計。
        /// </summary>
        private void CreateSummarySheet(ISheet sheet, List<SjlBillingExportRowModel> rows, ICellStyle defaultStyle)
        {
            NpoiCell.CreateHeaderCells(sheet.CreateRow(0), new List<string> { "日期", "運費" }, defaultStyle);

            var summaryRows = rows
                .GroupBy(row => row.SignOutTime.HasValue ? row.SignOutTime.Value.Date : DateTime.MinValue)
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    Date = group.Key,
                    Amount = group.Sum(row => row.ChargeAmount)
                })
                .ToList();

            for (int i = 0; i < summaryRows.Count; i++)
            {
                var data = summaryRows[i];
                var row = sheet.CreateRow(i + 1);
                NpoiCell.CreateCell(row, 0, data.Date == DateTime.MinValue ? string.Empty : data.Date.ToString("yyyy/MM/dd"), defaultStyle);
                NpoiCell.CreateDoubleCell(row, 1, Convert.ToDouble(data.Amount), defaultStyle);
            }

            var totalRow = sheet.CreateRow(summaryRows.Count + 1);
            NpoiCell.CreateCell(totalRow, 0, "合計", defaultStyle);
            NpoiCell.CreateDoubleCell(totalRow, 1, Convert.ToDouble(summaryRows.Sum(row => row.Amount)), defaultStyle);

            sheet.CreateFreezePane(0, 1);
            SetSheetColumnWidths(sheet, new[] { 14, 14 });
        }

        /// <summary>
        /// 取得表頭。
        /// </summary>
        /// <param name="transName">派件公司。</param>
        /// <returns>表頭文字。</returns>
        private List<string> GetHeaders(string transName)
        {
            if (transName == "大榮")
            {
                return new List<string>
                {
                    "清關日",
                    "運送編號",
                    "單據編號",
                    "大榮換單號",
                    "收件人",
                    "其他費用(稅金)",
                    "代收",
                    "地址",
                    "品名",
                    "件數",
                    "材積",
                    "重量",
                    "收件人電話",
                    "基本運費",
                    "超才費",
                    "總額"
                };
            }

            return new List<string>
            {
                "資料日期",
                "清關日期",
                "運送編號",
                "單據編號0H4",
                "派送日",
                "收件人",
                "稅金",
                "代收",
                "電話",
                "地址",
                "品名",
                "件數",
                "材積",
                "重量（kg）",
                "基本運費",
                "超才費",
                "總額",
                "基本運費",
                "超重費",
                "重量計費",
                "應計價(擇大值)"
            };
        }

        /// <summary>
        /// 寫入資料列。
        /// </summary>
        /// <param name="row">Excel 列。</param>
        /// <param name="data">匯出資料。</param>
        /// <param name="transName">派件公司。</param>
        /// <param name="defaultStyle">通用樣式。</param>
        private void WriteRow(IRow row, SjlBillingExportRowModel data, string transName, ICellStyle defaultStyle)
        {
            if (transName == "大榮")
            {
                // 大榮只輸出材積計價結果，大榮換單號依規格保留空白。
                NpoiCell.CreateCell(row, 0, FormatDate(data.SignOutTime, "yyyy-MM-dd"), defaultStyle);
                NpoiCell.CreateCell(row, 1, data.JetfSerial, defaultStyle);
                NpoiCell.CreateCell(row, 2, data.BlNo, defaultStyle);
                NpoiCell.CreateCell(row, 3, string.Empty, defaultStyle);
                NpoiCell.CreateCell(row, 4, data.Importer, defaultStyle);
                NpoiCell.CreateDoubleCell(row, 5, ConvertNullableDecimal(data.OtherFee), defaultStyle);
                NpoiCell.CreateDoubleCell(row, 6, ConvertNullableDecimal(data.Cod), defaultStyle);
                NpoiCell.CreateCell(row, 7, data.ImporterAddr, defaultStyle);
                NpoiCell.CreateCell(row, 8, data.ItemName, defaultStyle);
                NpoiCell.CreateIntCell(row, 9, data.Qty, defaultStyle);
                NpoiCell.CreateDoubleCell(row, 10, Convert.ToDouble(data.Volume), defaultStyle);
                NpoiCell.CreateDoubleCell(row, 11, Convert.ToDouble(data.Gw), defaultStyle);
                NpoiCell.CreateCell(row, 12, data.ImporterPhone, defaultStyle);
                NpoiCell.CreateDoubleCell(row, 13, Convert.ToDouble(data.BaseFee), defaultStyle);
                NpoiCell.CreateDoubleCell(row, 14, Convert.ToDouble(data.ExtraVolumeFee), defaultStyle);
                NpoiCell.CreateDoubleCell(row, 15, Convert.ToDouble(data.TotalAmount), defaultStyle);
                return;
            }

            // 捷通除了材積計價欄位外，還需補重量計費與擇大值欄位。
            NpoiCell.CreateCell(row, 0, FormatDate(data.CreatedTime, "yyyy/MM/dd"), defaultStyle);
            NpoiCell.CreateCell(row, 1, FormatDate(data.SignOutTime, "yyyy-MM-dd"), defaultStyle);
            NpoiCell.CreateCell(row, 2, data.JetfSerial, defaultStyle);
            NpoiCell.CreateCell(row, 3, data.BlNo, defaultStyle);
            NpoiCell.CreateCell(row, 4, FormatDateString(data.ScanCargoTime), defaultStyle);
            NpoiCell.CreateCell(row, 5, data.Importer, defaultStyle);
            NpoiCell.CreateDoubleCell(row, 6, ConvertNullableDecimal(data.OtherFee), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 7, ConvertNullableDecimal(data.Cod), defaultStyle);
            NpoiCell.CreateCell(row, 8, data.ImporterPhone, defaultStyle);
            NpoiCell.CreateCell(row, 9, data.ImporterAddr, defaultStyle);
            NpoiCell.CreateCell(row, 10, data.ItemName, defaultStyle);
            NpoiCell.CreateIntCell(row, 11, data.Qty, defaultStyle);
            NpoiCell.CreateDoubleCell(row, 12, Convert.ToDouble(data.Volume), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 13, Convert.ToDouble(data.Gw), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 14, Convert.ToDouble(data.BaseFee), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 15, Convert.ToDouble(data.ExtraVolumeFee), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 16, Convert.ToDouble(data.TotalAmount), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 17, Convert.ToDouble(data.BaseFee), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 18, Convert.ToDouble(data.OverweightFee), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 19, Convert.ToDouble(data.WeightChargeAmount), defaultStyle);
            NpoiCell.CreateDoubleCell(row, 20, Convert.ToDouble(data.ChargeAmount), defaultStyle);
        }

        /// <summary>
        /// 設定欄寬。
        /// </summary>
        /// <param name="sheet">工作表。</param>
        /// <param name="transName">派件公司。</param>
        private void SetColumnWidths(ISheet sheet, string transName)
        {
            var widths = transName == "大榮"
                ? new[] { 14, 18, 18, 16, 16, 10, 16, 32, 22, 10, 12, 12, 18, 12, 12, 12 }
                : new[] { 14, 14, 18, 18, 14, 16, 10, 12, 16, 32, 22, 10, 12, 12, 12, 12, 12, 12, 12, 12, 14 };

            SetSheetColumnWidths(sheet, widths);
        }

        /// <summary>
        /// 統一設定工作表欄寬。
        /// </summary>
        private void SetSheetColumnWidths(ISheet sheet, int[] widths)
        {

            for (int i = 0; i < widths.Length; i++)
            {
                sheet.SetColumnWidth(i, widths[i] * 256);
            }
        }

        /// <summary>
        /// 格式化日期欄位。
        /// </summary>
        private string FormatDate(DateTime? value, string format)
        {
            return value.HasValue ? value.Value.ToString(format) : string.Empty;
        }

        /// <summary>
        /// 格式化字串日期；若可解析則轉為 yyyy/MM/dd，否則保留原值。
        /// </summary>
        private string FormatDateString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            DateTime dateValue;
            return DateTime.TryParse(value, out dateValue) ? dateValue.ToString("yyyy/MM/dd") : value.Trim();
        }

        /// <summary>
        /// 轉換可空 decimal 為可空 double。
        /// </summary>
        /// <param name="value">decimal 值。</param>
        /// <returns>double 值。</returns>
        private double? ConvertNullableDecimal(decimal? value)
        {
            return value.HasValue ? Convert.ToDouble(value.Value) : (double?)null;
        }
    }
}
