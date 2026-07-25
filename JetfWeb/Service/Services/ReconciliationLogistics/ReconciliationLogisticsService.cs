using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Microsoft.VisualBasic.FileIO;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ReconciliationLogistics.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Service.Services.ReconciliationLogistics
{
    /// <summary>
    /// 物流銷帳檔案上傳與費用明細更新服務。
    /// </summary>
    public sealed class ReconciliationLogisticsService : _BaseService
    {
        private static readonly string[] HctCollectionHeaders =
        {
            "查貨號碼", "清單編號", "客戶代號", "代收貨款金額"
        };

        private static readonly string[] HctRemittanceHeaders =
        {
            "宅配單號", "出貨單號", "現金金額"
        };

        private static readonly string[] SevenElevenHeaders =
        {
            "訂單號碼", "出貨單號", "訂單金額", "備註"
        };

        private static readonly string[] KeledeHeaders =
        {
            "託運單號", "實收金額"
        };

        private static readonly string[] KtjHeaders =
        {
            "明細單號", "實收金額"
        };

        private static readonly string[] TaixinStarHeaders =
        {
            "订单号", "託運單號", "應收金額"
        };

        private static readonly string[] CashHeaders =
        {
            "運單號", "金額"
        };

        private static readonly string[] YtoHeaders =
        {
            "圆通单号", "合计"
        };

        private static readonly string[] TradeVanHeaders =
        {
            "交易金額", "分提單號碼"
        };

        /// <summary>
        /// 建立物流銷帳服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReconciliationLogisticsService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 分頁查詢物流銷帳紀錄。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>分頁物流銷帳資料。</returns>
        public ReconciliationLogisticsQueryResponse Search(
            ReconciliationLogisticsQueryRequest request)
        {
            var startDate = request?.RepaymentDateStart?.Date;
            var endDate = request?.RepaymentDateEnd?.Date;
            if (!startDate.HasValue || !endDate.HasValue)
            {
                throw new ArgumentException("回款日期為必填，請選擇開始日期與結束日期。");
            }

            if (startDate.Value > endDate.Value)
            {
                throw new ArgumentException("開始日期不可晚於結束日期。");
            }

            var company = request?.Company;
            if (company.HasValue &&
                !Enum.IsDefined(typeof(ReconciliationLogisticsCompany), company.Value))
            {
                throw new ArgumentException("物流公司選項不正確。");
            }

            var status = request?.Status;
            if (status.HasValue &&
                !Enum.IsDefined(typeof(ReconciliationLogisticsResultStatus), status.Value))
            {
                throw new ArgumentException("狀態選項不正確。");
            }

            var page = request != null && request.Page > 0 ? request.Page : 1;
            var pageSize = request != null && request.PageSize > 0 ? request.PageSize : 20;
            pageSize = Math.Min(pageSize, 200);
            var startDateValue = startDate.Value;
            var endDateExclusive = endDate.Value.AddDays(1);
            var trackingNo = request?.TrackingNo?.Trim();
            var dlvInv = request?.DlvInv?.Trim();

            // 日期為必要條件直接篩選；其他條件只有在使用者有輸入時才透過 WhereIf 套用。
            var query = JetfDb.ReconciliationLogistics
                .AsNoTracking()
                .Where(x =>
                    x.RepaymentDate >= startDateValue &&
                    x.RepaymentDate < endDateExclusive)
                .WhereIf(company.HasValue,
                    x => x.Company == company.Value)
                .WhereIf(status.HasValue,
                    x => x.Status == status.Value)
                .WhereIf(!string.IsNullOrWhiteSpace(trackingNo),
                    x => x.TrackingNo == trackingNo)
                .WhereIf(!string.IsNullOrWhiteSpace(dlvInv),
                    x => x.DlvInv == dlvInv);

            var totalCount = query.Count();
            var rows = query
                .OrderByDescending(x => x.RepaymentDate)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new ReconciliationLogisticsQueryResponse
            {
                TotalCount = totalCount,
                Data = rows.Select(x => new ReconciliationLogisticsListItem
                {
                    Id = x.Id,
                    RepaymentDate = x.RepaymentDate.ToString("yyyy/MM/dd"),
                    Company = x.Company.ToDescription(),
                    CustomerCode = x.CustomerCode ?? string.Empty,
                    TrackingNo = x.TrackingNo,
                    DlvInv = x.DlvInv,
                    ReceivedAmount = x.ReceivedAmount,
                    DifferenceAmount = x.DifferenceAmount,
                    Status = x.Status.HasValue
                        ? x.Status.Value.ToDescription()
                        : "未設定"
                }).ToList()
            };
        }

        /// <summary>
        /// 上傳物流銷帳檔案，寫入上傳紀錄並更新費用明細。
        /// </summary>
        /// <param name="stream">上傳檔案串流。</param>
        /// <param name="sourceFileName">原始檔名。</param>
        /// <param name="company">物流公司。</param>
        /// <param name="repaymentDate">回款日期。</param>
        /// <returns>上傳結果。</returns>
        public ResponseModel Upload(
            Stream stream,
            string sourceFileName,
            ReconciliationLogisticsCompany company,
            DateTime repaymentDate)
        {
            try
            {
                if (!Enum.IsDefined(typeof(ReconciliationLogisticsCompany), company))
                {
                    return new ResponseModel("物流公司選項不正確");
                }

                var currentUserId = GetUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ResponseModel("無法取得目前登入人員");
                }

                currentUserId = currentUserId.Trim();
                if (currentUserId.Length > 10)
                {
                    return new ResponseModel("登入人員代號不可超過 10 個字元");
                }

                ReconciliationLogisticsUploadFormat uploadFormat;
                var uploadRows = ReadUploadRows(stream, company, out uploadFormat);
                if (!uploadRows.Any())
                {
                    return new ResponseModel("檔案中沒有資料");
                }

                ValidateFileDuplicates(uploadRows, company);
                using (var transaction = JetfDb.Database.BeginTransaction())
                {
                    try
                    {
                        // 檔案內已驗證失敗的資料不需再查詢資料庫，其餘資料仍可繼續處理。
                        var validRows = uploadRows
                            .Where(x => string.IsNullOrWhiteSpace(x.FailReason))
                            .ToList();
                        if (validRows.Any())
                        {
                            ValidateDatabaseDuplicates(validRows, company);
                        }

                        // 僅將通過欄位、檔案重複及資料庫重複驗證的資料送入銷帳流程。
                        validRows = validRows
                            .Where(x => string.IsNullOrWhiteSpace(x.FailReason))
                            .ToList();
                        if (!validRows.Any())
                        {
                            return CreateValidationFailureResponse(uploadRows);
                        }

                        var result = ReconcileUploadRows(
                            validRows,
                            Path.GetFileName(sourceFileName),
                            company,
                            uploadFormat,
                            repaymentDate.Date,
                            currentUserId);

                        // 驗證失敗資料只回傳畫面，不寫入資料庫；成功資料不受失敗資料影響。
                        var failRows = uploadRows
                            .Where(x => !string.IsNullOrWhiteSpace(x.FailReason))
                            .ToList();
                        result.Count = uploadRows.Count;
                        result.FailCount = failRows.Count;
                        result.Data = failRows;
                        result.Message = "上傳成功";

                        transaction.Commit();

                        return new ResponseModel
                        {
                            IsSuccess = true,
                            status = Status.success,
                            msg = result.Message,
                            ReturnObject = result
                        };
                    }
                    catch
                    {
                        // 銷帳過程有任何錯誤時撤銷本次所有新增紀錄及費用明細修改，確保可以重新上傳。
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.GetBaseException().Message}");
            }
        }

        /// <summary>
        /// 匯出本次物流銷帳結果 Excel。
        /// </summary>
        /// <param name="result">本次物流銷帳完整結果。</param>
        /// <param name="company">物流公司。</param>
        /// <param name="repaymentDate">回款日期。</param>
        /// <returns>Excel 檔案內容。</returns>
        public byte[] ExportExcel(
            ReconciliationLogisticsUploadResult result,
            ReconciliationLogisticsCompany company,
            DateTime repaymentDate)
        {
            var data = result?.Results ?? new List<ReconciliationLogisticsResultItem>();
            var exceptionData = data
                .Where(x => x.IsException)
                .ToList();
            var failData = result?.Data ?? new List<ReconciliationLogisticsUploadRow>();
            var workbook = new XSSFWorkbook();
            try
            {
                var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
                var dataStyle = NpoiStyle.CreateDataStyle(workbook);
                var numberStyle = NpoiStyle.CreateNumberStyle(workbook);

                CreateResultSheet(
                    workbook,
                    "物流銷帳結果",
                    data,
                    headerStyle,
                    dataStyle,
                    numberStyle);

                if (exceptionData.Any())
                {
                    // 比對異常另外建立頁籤，讓超額回款及未比對資料可以集中檢查。
                    CreateResultSheet(
                        workbook,
                        "異常明細",
                        exceptionData,
                        headerStyle,
                        dataStyle,
                        numberStyle);
                }

                if (failData.Any())
                {
                    // 驗證失敗資料不會寫入資料庫，直接附在本次 Excel 才能完整保留重複或格式錯誤明細。
                    var failSheet = workbook.CreateSheet("欄位驗證失敗");
                    var failHeaders = new[]
                    {
                        "檔案列號", "失敗原因", "分提單號", "物流貨號", "回款金額"
                    };
                    NpoiCell.CreateHeaderCells(failSheet.CreateRow(0), failHeaders, headerStyle);
                    for (var index = 0; index < failData.Count; index++)
                    {
                        var item = failData[index];
                        var row = failSheet.CreateRow(index + 1);
                        var column = 0;
                        NpoiCell.CreateIntCell(row, column++, item.RowNo, dataStyle);
                        NpoiCell.CreateCell(row, column++, item.FailReason, dataStyle);
                        NpoiCell.CreateCell(row, column++, item.TrackingNo, dataStyle);
                        NpoiCell.CreateCell(row, column++, item.DlvInv, dataStyle);
                        NpoiCell.CreateCell(row, column, item.ReceivedAmountText, dataStyle);
                    }

                    failSheet.AutoSizeColumns(failHeaders.Length, scale: 1.2, minWidth: 12);
                }

                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    return stream.ToArray();
                }
            }
            finally
            {
                workbook.Close();
            }
        }

        /// <summary>
        /// 建立物流銷帳結果 Excel 頁籤。
        /// </summary>
        /// <param name="workbook">Excel 活頁簿。</param>
        /// <param name="sheetName">頁籤名稱。</param>
        /// <param name="data">物流銷帳結果。</param>
        /// <param name="headerStyle">標題列樣式。</param>
        /// <param name="dataStyle">一般資料樣式。</param>
        /// <param name="numberStyle">金額資料樣式。</param>
        private static void CreateResultSheet(
            IWorkbook workbook,
            string sheetName,
            IList<ReconciliationLogisticsResultItem> data,
            ICellStyle headerStyle,
            ICellStyle dataStyle,
            ICellStyle numberStyle)
        {
            var sheet = workbook.CreateSheet(sheetName);
            var headers = new[]
            {
                "回款日期", "物流公司", "分提單號", "物流貨號",
                "應收金額", "回款金額", "差異", "狀態"
            };

            NpoiCell.CreateHeaderCells(sheet.CreateRow(0), headers, headerStyle);
            for (var index = 0; index < data.Count; index++)
            {
                var item = data[index];
                var row = sheet.CreateRow(index + 1);
                var column = 0;
                NpoiCell.CreateCell(row, column++, item.RepaymentDate, dataStyle);
                NpoiCell.CreateCell(row, column++, item.Company, dataStyle);
                NpoiCell.CreateCell(row, column++, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, column++, item.DlvInv, dataStyle);
                // 金額欄位使用數值格式，讓使用者下載後仍可直接加總或運算。
                NpoiCell.CreateIntCell(row, column++, item.ReceivableAmount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.RepaymentAmount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.Difference, numberStyle);
                NpoiCell.CreateCell(row, column, item.StatusName, dataStyle);
            }

            sheet.AutoSizeColumns(headers.Length, scale: 1.2, minWidth: 12);
        }

        /// <summary>
        /// 依物流公司讀取上傳檔案。
        /// </summary>
        /// <param name="stream">上傳檔案串流。</param>
        /// <param name="company">物流公司。</param>
        /// <param name="uploadFormat">辨識出的物流上傳格式。</param>
        /// <returns>上傳資料。</returns>
        private static List<ReconciliationLogisticsUploadRow> ReadUploadRows(
            Stream stream,
            ReconciliationLogisticsCompany company,
            out ReconciliationLogisticsUploadFormat uploadFormat)
        {
            if (company == ReconciliationLogisticsCompany.Ktj)
            {
                uploadFormat = ReconciliationLogisticsUploadFormat.Ktj;
                return ReadKtjRows(stream);
            }

            var workbook = new XSSFWorkbook(stream);
            try
            {
                if (workbook.NumberOfSheets == 0)
                {
                    uploadFormat = default(ReconciliationLogisticsUploadFormat);
                    return new List<ReconciliationLogisticsUploadRow>();
                }

                // 圓通的第一張工作表是彙總資料，物流銷帳必須讀取「明细」工作表。
                var sheet = company == ReconciliationLogisticsCompany.Yto
                    ? workbook.GetSheet("明细") ?? workbook.GetSheet("明細")
                    : workbook.GetSheetAt(0);
                if (sheet == null)
                {
                    throw new InvalidOperationException(
                        $"找不到{company.ToDescription()}明細工作表，請確認檔案格式");
                }

                int headerRowIndex;
                Dictionary<string, int> columnIndexes;
                uploadFormat = GetUploadFormat(
                    sheet,
                    company,
                    out headerRowIndex,
                    out columnIndexes);

                var rows = new List<ReconciliationLogisticsUploadRow>();
                for (var rowIndex = headerRowIndex + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var excelRow = sheet.GetRow(rowIndex);
                    if (excelRow == null)
                    {
                        continue;
                    }

                    if ((uploadFormat == ReconciliationLogisticsUploadFormat.Kelede ||
                         uploadFormat == ReconciliationLogisticsUploadFormat.TaixinStar) &&
                        string.Equals(
                            excelRow.GetCellData(0).Trim(),
                            "筆數：",
                            StringComparison.Ordinal))
                    {
                        // 客樂得及超峰在明細後方仍有總計或其他區段，遇到筆數列即停止讀取。
                        break;
                    }

                    ReconciliationLogisticsUploadRow row;
                    switch (uploadFormat)
                    {
                        case ReconciliationLogisticsUploadFormat.HctCollection:
                            row = ReadHctRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        case ReconciliationLogisticsUploadFormat.HctRemittance:
                            row = ReadHctRemittanceRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        case ReconciliationLogisticsUploadFormat.SevenEleven:
                            row = ReadSevenElevenRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        case ReconciliationLogisticsUploadFormat.Kelede:
                            row = ReadKeledeRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        case ReconciliationLogisticsUploadFormat.TaixinStar:
                            row = ReadTaixinStarRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        case ReconciliationLogisticsUploadFormat.Cash:
                            row = ReadCashRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        case ReconciliationLogisticsUploadFormat.Yto:
                            row = ReadYtoRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        case ReconciliationLogisticsUploadFormat.TradeVan:
                            row = ReadTradeVanRow(excelRow, rowIndex + 1, columnIndexes);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(uploadFormat),
                                uploadFormat,
                                "不支援的物流上傳格式");
                    }

                    if (row != null && !IsEmptyRow(row))
                    {
                        rows.Add(row);
                    }
                }

                return rows;
            }
            finally
            {
                workbook.Close();
            }
        }

        /// <summary>
        /// 依物流公司及 Excel 表頭辨識上傳格式。
        /// </summary>
        /// <param name="sheet">Excel 工作表。</param>
        /// <param name="company">物流公司。</param>
        /// <param name="headerRowIndex">找到的表頭列索引。</param>
        /// <param name="columnIndexes">欄位名稱與欄位索引對照。</param>
        /// <returns>物流上傳格式。</returns>
        private static ReconciliationLogisticsUploadFormat GetUploadFormat(
            ISheet sheet,
            ReconciliationLogisticsCompany company,
            out int headerRowIndex,
            out Dictionary<string, int> columnIndexes)
        {
            switch (company)
            {
                case ReconciliationLogisticsCompany.Hct:
                    if (TryFindHeaderRow(
                        sheet,
                        HctCollectionHeaders,
                        out headerRowIndex,
                        out columnIndexes))
                    {
                        return ReconciliationLogisticsUploadFormat.HctCollection;
                    }

                    if (TryFindHeaderRow(
                        sheet,
                        HctRemittanceHeaders,
                        out headerRowIndex,
                        out columnIndexes))
                    {
                        return ReconciliationLogisticsUploadFormat.HctRemittance;
                    }

                    break;
                case ReconciliationLogisticsCompany.SevenEleven:
                    if (TryFindHeaderRow(
                        sheet,
                        SevenElevenHeaders,
                        out headerRowIndex,
                        out columnIndexes))
                    {
                        return ReconciliationLogisticsUploadFormat.SevenEleven;
                    }

                    break;
                case ReconciliationLogisticsCompany.Kelede:
                    var sectionRowIndex = FindRowIndex(
                        sheet,
                        "現金結帳明細(客樂得付款)");
                    if (sectionRowIndex >= 0 &&
                        TryFindHeaderRow(
                            sheet,
                            KeledeHeaders,
                            out headerRowIndex,
                            out columnIndexes,
                            sectionRowIndex + 1,
                            sectionRowIndex + 1))
                    {
                        return ReconciliationLogisticsUploadFormat.Kelede;
                    }

                    break;
                case ReconciliationLogisticsCompany.TaixinStar:
                    if (TryFindHeaderRow(
                        sheet,
                        TaixinStarHeaders,
                        out headerRowIndex,
                        out columnIndexes))
                    {
                        return ReconciliationLogisticsUploadFormat.TaixinStar;
                    }

                    break;
                case ReconciliationLogisticsCompany.Cash:
                    if (TryFindHeaderRow(
                        sheet,
                        CashHeaders,
                        out headerRowIndex,
                        out columnIndexes))
                    {
                        return ReconciliationLogisticsUploadFormat.Cash;
                    }

                    break;
                case ReconciliationLogisticsCompany.Yto:
                    if (TryFindHeaderRow(
                        sheet,
                        YtoHeaders,
                        out headerRowIndex,
                        out columnIndexes))
                    {
                        return ReconciliationLogisticsUploadFormat.Yto;
                    }

                    break;
                case ReconciliationLogisticsCompany.TradeVan:
                    if (TryFindHeaderRow(
                        sheet,
                        TradeVanHeaders,
                        out headerRowIndex,
                        out columnIndexes))
                    {
                        return ReconciliationLogisticsUploadFormat.TradeVan;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(company),
                        company,
                        "不支援的物流公司");
            }

            throw new InvalidOperationException(
                $"找不到{company.ToDescription()}完整表頭，請確認檔案格式");
        }

        /// <summary>
        /// 讀取一筆新竹物流清單格式 Excel 資料。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>新竹物流清單格式資料；合計列回傳 null。</returns>
        private static ReconciliationLogisticsUploadRow ReadHctRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            int recipientNameColumnIndex;
            if (columnIndexes.TryGetValue("收貨人名稱", out recipientNameColumnIndex) &&
                string.Equals(
                    excelRow.GetCellData(recipientNameColumnIndex),
                    "合計",
                    StringComparison.Ordinal))
            {
                // 新竹物流檔案最後一列為合計資料，不是物流銷帳明細。
                return null;
            }

            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["代收貨款金額"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                DlvInv = excelRow.GetCellData(columnIndexes["查貨號碼"]),
                TrackingNo = excelRow.GetCellData(columnIndexes["清單編號"]),
                CustomerCode = excelRow.GetCellData(columnIndexes["客戶代號"]),
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "代收貨款金額", failReasons)
            };

            ValidateRequiredKeys(row, "清單編號", "查貨號碼", failReasons);
            if (string.IsNullOrWhiteSpace(row.CustomerCode))
            {
                failReasons.Add("客戶代號必填");
            }

            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 讀取一筆新竹物流匯款明細 Excel 資料。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>新竹物流匯款明細；頁尾彙總列回傳 null。</returns>
        private static ReconciliationLogisticsUploadRow ReadHctRemittanceRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            int recipientNameColumnIndex;
            var recipientName = columnIndexes.TryGetValue(
                "收件人",
                out recipientNameColumnIndex)
                ? excelRow.GetCellData(recipientNameColumnIndex)
                : string.Empty;
            if (string.Equals(recipientName, "總匯款金額", StringComparison.Ordinal) ||
                string.Equals(recipientName, "手續費總金額", StringComparison.Ordinal) ||
                string.Equals(recipientName, "匯款作業費", StringComparison.Ordinal) ||
                string.Equals(recipientName, "實匯金額", StringComparison.Ordinal))
            {
                // 新竹物流匯款明細的頁尾彙總資料不是物流銷帳明細。
                return null;
            }

            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["現金金額"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            int customerCodeColumnIndex;
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                TrackingNo = excelRow.GetCellData(columnIndexes["出貨單號"]),
                DlvInv = excelRow.GetCellData(columnIndexes["宅配單號"]),
                CustomerCode = columnIndexes.TryGetValue(
                    "客戶別",
                    out customerCodeColumnIndex)
                    ? excelRow.GetCellData(customerCodeColumnIndex)
                    : null,
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "現金金額", failReasons)
            };

            ValidateRequiredKeys(row, "出貨單號", "宅配單號", failReasons);
            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 讀取一筆 7-11 Excel 資料。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>7-11 上傳資料。</returns>
        private static ReconciliationLogisticsUploadRow ReadSevenElevenRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["訂單金額"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                TrackingNo = excelRow.GetCellData(columnIndexes["訂單號碼"]),
                DlvInv = excelRow.GetCellData(columnIndexes["出貨單號"]),
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "訂單金額", failReasons),
                Remark = excelRow.GetCellData(columnIndexes["備註"])
            };

            // 7-11 檔案只處理備註為 1 且訂單金額大於 0 的資料。
            if (!string.Equals(row.Remark, "1", StringComparison.OrdinalIgnoreCase) ||
                !row.ReceivedAmount.HasValue ||
                row.ReceivedAmount.Value <= 0)
            {
                return null;
            }

            ValidateRequiredKeys(row, "訂單號碼", "出貨單號", failReasons);
            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 讀取一筆客樂得現金結帳明細。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>客樂得上傳資料。</returns>
        private static ReconciliationLogisticsUploadRow ReadKeledeRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["實收金額"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            int orderNoColumnIndex;
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                TrackingNo = columnIndexes.TryGetValue(
                    "訂單號碼",
                    out orderNoColumnIndex)
                    ? excelRow.GetCellData(orderNoColumnIndex)
                    : string.Empty,
                DlvInv = excelRow.GetCellData(columnIndexes["託運單號"]),
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "實收金額", failReasons)
            };

            ValidateRequiredDlvInv(row, "託運單號", failReasons);
            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 讀取大榮 CSV 上傳資料。
        /// </summary>
        /// <param name="stream">CSV 檔案串流。</param>
        /// <returns>大榮上傳資料。</returns>
        private static List<ReconciliationLogisticsUploadRow> ReadKtjRows(
            Stream stream)
        {
            var rows = new List<ReconciliationLogisticsUploadRow>();
            using (var parser = new TextFieldParser(
                stream,
                Encoding.GetEncoding(950),
                true))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = true;

                if (parser.EndOfData)
                {
                    return rows;
                }

                var headerFields = parser.ReadFields() ?? new string[0];
                var columnIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
                for (var index = 0; index < headerFields.Length; index++)
                {
                    var header = (headerFields[index] ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(header) &&
                        !columnIndexes.ContainsKey(header))
                    {
                        columnIndexes.Add(header, index);
                    }
                }

                if (!KtjHeaders.All(columnIndexes.ContainsKey))
                {
                    throw new InvalidOperationException(
                        "找不到大榮完整表頭，請確認檔案格式");
                }

                var rowNo = 1;
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    rowNo++;
                    if (fields == null)
                    {
                        continue;
                    }

                    var firstColumn = fields.Length > 0
                        ? (fields[0] ?? string.Empty).Trim()
                        : string.Empty;
                    if (firstColumn.EndsWith("總計", StringComparison.Ordinal))
                    {
                        // 大榮總計後方還有其他費用表格，遇到總計列即停止讀取。
                        break;
                    }

                    var detailNoColumnIndex = columnIndexes["明細單號"];
                    var amountColumnIndex = columnIndexes["實收金額"];
                    var detailNo = detailNoColumnIndex < fields.Length
                        ? fields[detailNoColumnIndex]
                        : string.Empty;
                    var amountText = amountColumnIndex < fields.Length
                        ? fields[amountColumnIndex]
                        : string.Empty;
                    int trackingNoColumnIndex;
                    var trackingNo = columnIndexes.TryGetValue(
                        "出貨單號",
                        out trackingNoColumnIndex) &&
                        trackingNoColumnIndex < fields.Length
                        ? fields[trackingNoColumnIndex]
                        : string.Empty;

                    var failReasons = new List<string>();
                    var row = new ReconciliationLogisticsUploadRow
                    {
                        RowNo = rowNo,
                        TrackingNo = trackingNo,
                        // 只移除「空白＋00」後綴，避免破壞沒有該後綴的合法 14 位明細單號。
                        DlvInv = Regex.Replace(
                            (detailNo ?? string.Empty).Trim(),
                            @"\s+00$",
                            string.Empty),
                        ReceivedAmountText = amountText,
                        ReceivedAmount = ParseRequiredAmount(
                            amountText,
                            "實收金額",
                            failReasons)
                    };

                    ValidateRequiredDlvInv(row, "明細單號", failReasons);
                    row.FailReason = string.Join("；", failReasons);
                    if (!IsEmptyRow(row))
                    {
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// 讀取一筆超峰 Excel 資料。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>超峰上傳資料。</returns>
        private static ReconciliationLogisticsUploadRow ReadTaixinStarRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["應收金額"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                TrackingNo = excelRow.GetCellData(columnIndexes["订单号"]),
                DlvInv = excelRow.GetCellData(columnIndexes["託運單號"]),
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "應收金額", failReasons)
            };

            ValidateRequiredKeys(row, "订单号", "託運單號", failReasons);
            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 讀取一筆現金 Excel 資料。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>現金上傳資料。</returns>
        private static ReconciliationLogisticsUploadRow ReadCashRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["金額"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                TrackingNo = string.Empty,
                DlvInv = excelRow.GetCellData(columnIndexes["運單號"]),
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "金額", failReasons)
            };

            ValidateRequiredDlvInv(row, "運單號", failReasons);
            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 讀取一筆圓通 Excel 資料。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>圓通上傳資料。</returns>
        private static ReconciliationLogisticsUploadRow ReadYtoRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["合计"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            int originalTrackingNoColumnIndex;
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                TrackingNo = columnIndexes.TryGetValue(
                    "原單號",
                    out originalTrackingNoColumnIndex)
                    ? excelRow.GetCellData(originalTrackingNoColumnIndex)
                    : string.Empty,
                DlvInv = excelRow.GetCellData(columnIndexes["圆通单号"]),
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "合计", failReasons)
            };

            ValidateRequiredDlvInv(row, "圆通单号", failReasons);
            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 讀取一筆關貿交易明細 Excel 資料。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNo">Excel 顯示列號。</param>
        /// <param name="columnIndexes">欄位索引。</param>
        /// <returns>關貿上傳資料。</returns>
        private static ReconciliationLogisticsUploadRow ReadTradeVanRow(
            IRow excelRow,
            int rowNo,
            IDictionary<string, int> columnIndexes)
        {
            var failReasons = new List<string>();
            var amountCellIndex = columnIndexes["交易金額"];
            var amountCell = excelRow.GetCell(amountCellIndex);
            var amountText = amountCell?.CellType == CellType.Numeric
                ? amountCell.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : excelRow.GetCellData(amountCellIndex);
            var row = new ReconciliationLogisticsUploadRow
            {
                RowNo = rowNo,
                TrackingNo = excelRow.GetCellData(columnIndexes["分提單號碼"]),
                DlvInv = string.Empty,
                ReceivedAmountText = amountText,
                ReceivedAmount = ParseRequiredAmount(amountText, "交易金額", failReasons)
            };

            ValidateRequiredTrackingNo(row, "分提單號碼", failReasons);
            row.FailReason = string.Join("；", failReasons);
            return row;
        }

        /// <summary>
        /// 尋找包含指定文字的 Excel 列。
        /// </summary>
        /// <param name="sheet">Excel 工作表。</param>
        /// <param name="value">要尋找的完整文字。</param>
        /// <returns>符合的零起始列索引；找不到回傳 -1。</returns>
        private static int FindRowIndex(ISheet sheet, string value)
        {
            for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                for (var columnIndex = 0; columnIndex < row.LastCellNum; columnIndex++)
                {
                    if (string.Equals(
                        row.GetCellData(columnIndex).Trim(),
                        value,
                        StringComparison.Ordinal))
                    {
                        return rowIndex;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 尋找同時包含所有必要欄位的表頭列。
        /// </summary>
        /// <param name="sheet">Excel 工作表。</param>
        /// <param name="requiredHeaders">必要欄位名稱。</param>
        /// <param name="headerRowIndex">找到的表頭列索引。</param>
        /// <param name="columnIndexes">欄位名稱與欄位索引對照。</param>
        /// <param name="startRowIndex">開始尋找的零起始列索引。</param>
        /// <param name="endRowIndex">結束尋找的零起始列索引。</param>
        /// <returns>是否找到完整表頭。</returns>
        private static bool TryFindHeaderRow(
            ISheet sheet,
            IEnumerable<string> requiredHeaders,
            out int headerRowIndex,
            out Dictionary<string, int> columnIndexes,
            int startRowIndex = 0,
            int? endRowIndex = null)
        {
            headerRowIndex = -1;
            columnIndexes = null;

            var lastRowIndex = Math.Min(
                sheet.LastRowNum,
                endRowIndex ?? sheet.LastRowNum);
            for (var rowIndex = Math.Max(0, startRowIndex);
                 rowIndex <= lastRowIndex;
                 rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var candidate = new Dictionary<string, int>(StringComparer.Ordinal);
                for (var columnIndex = 0; columnIndex < row.LastCellNum; columnIndex++)
                {
                    var name = row.GetCellData(columnIndex);
                    if (!string.IsNullOrWhiteSpace(name) && !candidate.ContainsKey(name))
                    {
                        candidate.Add(name, columnIndex);
                    }
                }

                if (!requiredHeaders.All(candidate.ContainsKey))
                {
                    continue;
                }

                headerRowIndex = rowIndex;
                columnIndexes = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 解析必要的整數回收金額。
        /// </summary>
        /// <param name="text">上傳檔案金額文字。</param>
        /// <param name="columnName">欄位名稱。</param>
        /// <param name="failReasons">失敗原因集合。</param>
        /// <returns>解析後的整數金額。</returns>
        private static int? ParseRequiredAmount(
            string text,
            string columnName,
            ICollection<string> failReasons)
        {
            decimal value;
            if (string.IsNullOrWhiteSpace(text))
            {
                failReasons.Add($"{columnName}必填");
                return null;
            }

            if (!TryParseDecimal(text, out value) ||
                value < 0 ||
                value != decimal.Truncate(value) ||
                value > int.MaxValue)
            {
                failReasons.Add($"{columnName}必須為 0 以上的整數");
                return null;
            }

            return (int)value;
        }

        /// <summary>
        /// 解析上傳檔案金額文字。
        /// </summary>
        /// <param name="text">金額文字。</param>
        /// <param name="value">解析後金額。</param>
        /// <returns>是否解析成功。</returns>
        private static bool TryParseDecimal(string text, out decimal value)
        {
            const NumberStyles styles = NumberStyles.Number |
                                        NumberStyles.AllowCurrencySymbol |
                                        NumberStyles.AllowLeadingSign;
            return decimal.TryParse(text, styles, CultureInfo.CurrentCulture, out value) ||
                   decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// 驗證費用明細比對鍵必填。
        /// </summary>
        /// <param name="row">上傳資料。</param>
        /// <param name="trackingNoName">分提單號來源欄位名稱。</param>
        /// <param name="dlvInvName">物流貨號來源欄位名稱。</param>
        /// <param name="failReasons">失敗原因集合。</param>
        private static void ValidateRequiredKeys(
            ReconciliationLogisticsUploadRow row,
            string trackingNoName,
            string dlvInvName,
            ICollection<string> failReasons)
        {
            row.TrackingNo = (row.TrackingNo ?? string.Empty).Trim();
            row.DlvInv = (row.DlvInv ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(row.TrackingNo))
            {
                failReasons.Add($"{trackingNoName}必填");
            }

            if (string.IsNullOrWhiteSpace(row.DlvInv))
            {
                failReasons.Add($"{dlvInvName}必填");
            }
        }

        /// <summary>
        /// 驗證只使用物流貨號比對的格式是否已提供必要單號。
        /// </summary>
        /// <param name="row">上傳資料。</param>
        /// <param name="dlvInvName">物流貨號來源欄位名稱。</param>
        /// <param name="failReasons">失敗原因集合。</param>
        private static void ValidateRequiredDlvInv(
            ReconciliationLogisticsUploadRow row,
            string dlvInvName,
            ICollection<string> failReasons)
        {
            row.TrackingNo = (row.TrackingNo ?? string.Empty).Trim();
            row.DlvInv = (row.DlvInv ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(row.DlvInv))
            {
                failReasons.Add($"{dlvInvName}必填");
            }
        }

        /// <summary>
        /// 驗證只使用分提單號比對的格式是否已提供必要單號。
        /// </summary>
        /// <param name="row">上傳資料。</param>
        /// <param name="trackingNoName">分提單號來源欄位名稱。</param>
        /// <param name="failReasons">失敗原因集合。</param>
        private static void ValidateRequiredTrackingNo(
            ReconciliationLogisticsUploadRow row,
            string trackingNoName,
            ICollection<string> failReasons)
        {
            row.TrackingNo = (row.TrackingNo ?? string.Empty).Trim();
            row.DlvInv = (row.DlvInv ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(row.TrackingNo))
            {
                failReasons.Add($"{trackingNoName}必填");
            }
        }

        /// <summary>
        /// 判斷上傳資料是否為空白列。
        /// </summary>
        /// <param name="row">上傳資料。</param>
        /// <returns>是否為空白列。</returns>
        private static bool IsEmptyRow(ReconciliationLogisticsUploadRow row)
        {
            return string.IsNullOrWhiteSpace(row.TrackingNo) &&
                   string.IsNullOrWhiteSpace(row.DlvInv) &&
                   string.IsNullOrWhiteSpace(row.ReceivedAmountText) &&
                   string.IsNullOrWhiteSpace(row.CustomerCode) &&
                   string.IsNullOrWhiteSpace(row.Remark);
        }

        /// <summary>
        /// 驗證同一份上傳檔案不可出現重複鍵。
        /// </summary>
        /// <param name="uploadRows">上傳資料。</param>
        /// <param name="company">物流公司。</param>
        private static void ValidateFileDuplicates(
            IEnumerable<ReconciliationLogisticsUploadRow> uploadRows,
            ReconciliationLogisticsCompany company)
        {
            if (company == ReconciliationLogisticsCompany.TradeVan)
            {
                // 關貿只使用分提單號碼比對，檔案內也以相同欄位檢查重複。
                var duplicateRows = uploadRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                    .GroupBy(
                        x => x.TrackingNo.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .SelectMany(group => group);
                foreach (var row in duplicateRows)
                {
                    AppendFailReason(row, "分提單號碼在檔案內重複");
                }

                return;
            }

            if (company == ReconciliationLogisticsCompany.Hct ||
                company == ReconciliationLogisticsCompany.TaixinStar)
            {
                // 新竹物流及超峰都以「分提單號＋物流貨號」判斷檔案內是否重複。
                var duplicateRows = uploadRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.DlvInv))
                    .GroupBy(x => new
                    {
                        TrackingNo = (x.TrackingNo ?? string.Empty).Trim().ToUpperInvariant(),
                        DlvInv = (x.DlvInv ?? string.Empty).Trim().ToUpperInvariant()
                    })
                    .Where(group => group.Count() > 1)
                    .SelectMany(group => group);
                var duplicateKeyName = company == ReconciliationLogisticsCompany.Hct
                    ? "清單編號及查貨號碼"
                    : "订单号及託運單號";
                foreach (var row in duplicateRows)
                {
                    AppendFailReason(row, $"{duplicateKeyName}在檔案內重複");
                }

                return;
            }

            // 其餘物流公司只需以物流貨號判斷檔案內是否重複。
            var duplicateDlvInvRows = uploadRows
                .Where(x => !string.IsNullOrWhiteSpace(x.DlvInv))
                .GroupBy(x => x.DlvInv.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group);
            var duplicateDlvInvName = company == ReconciliationLogisticsCompany.SevenEleven
                ? "出貨單號"
                : "物流貨號";
            foreach (var row in duplicateDlvInvRows)
            {
                AppendFailReason(row, $"{duplicateDlvInvName}在檔案內重複");
            }
        }

        /// <summary>
        /// 驗證資料庫中是否已有相同物流上傳資料。
        /// </summary>
        /// <param name="uploadRows">上傳資料。</param>
        /// <param name="company">物流公司。</param>
        private void ValidateDatabaseDuplicates(
            List<ReconciliationLogisticsUploadRow> uploadRows,
            ReconciliationLogisticsCompany company)
        {
            if (company == ReconciliationLogisticsCompany.TradeVan)
            {
                // 關貿以分提單號碼識別上傳資料，避免同一筆交易重複銷帳。
                var existingTrackingNos = JetfDb.ReconciliationLogistics
                    .AsNoTracking()
                    .Where(x => x.Company == company)
                    .WhereBulkContains(
                        JetfDb,
                        uploadRows,
                        entity => new { entity.TrackingNo },
                        row => new { row.TrackingNo })
                    .Select(x => x.TrackingNo)
                    .ToList()
                    .Select(x => (x ?? string.Empty).Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in uploadRows.Where(
                    x => existingTrackingNos.Contains(
                        (x.TrackingNo ?? string.Empty).Trim())))
                {
                    AppendFailReason(row, "分提單號碼已上傳過");
                }

                return;
            }

            if (company == ReconciliationLogisticsCompany.Hct ||
                company == ReconciliationLogisticsCompany.TaixinStar)
            {
                var existingKeys = JetfDb.ReconciliationLogistics
                    .AsNoTracking()
                    .Where(x => x.Company == company)
                    .WhereBulkContains(
                        JetfDb,
                        uploadRows,
                        entity => new { entity.TrackingNo, entity.DlvInv },
                        row => new { row.TrackingNo, row.DlvInv })
                    .Select(x => new { x.TrackingNo, x.DlvInv })
                    .ToList()
                    .Select(x => new
                    {
                        TrackingNo = (x.TrackingNo ?? string.Empty).Trim().ToUpperInvariant(),
                        DlvInv = (x.DlvInv ?? string.Empty).Trim().ToUpperInvariant()
                    })
                    .ToHashSet();

                foreach (var row in uploadRows.Where(
                    x => existingKeys.Contains(new
                    {
                        TrackingNo = (x.TrackingNo ?? string.Empty).Trim().ToUpperInvariant(),
                        DlvInv = (x.DlvInv ?? string.Empty).Trim().ToUpperInvariant()
                    })))
                {
                    var duplicateKeyName = company == ReconciliationLogisticsCompany.Hct
                        ? "清單編號及查貨號碼"
                        : "订单号及託運單號";
                    AppendFailReason(row, $"{duplicateKeyName}已上傳過");
                }

                return;
            }

            var existingDlvInvs = JetfDb.ReconciliationLogistics
                .AsNoTracking()
                .Where(x => x.Company == company)
                .WhereBulkContains(
                    JetfDb,
                    uploadRows,
                    entity => new { entity.DlvInv },
                    row => new { row.DlvInv })
                .Select(x => x.DlvInv)
                .ToList()
                .Select(x => (x ?? string.Empty).Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var row in uploadRows.Where(
                x => existingDlvInvs.Contains((x.DlvInv ?? string.Empty).Trim())))
            {
                var duplicateDlvInvName =
                    company == ReconciliationLogisticsCompany.SevenEleven
                        ? "出貨單號"
                        : "物流貨號";
                AppendFailReason(row, $"{duplicateDlvInvName}已上傳過");
            }
        }

        /// <summary>
        /// 寫入物流銷帳紀錄並更新符合比對鍵的費用明細。
        /// </summary>
        /// <param name="uploadRows">已通過驗證的上傳資料。</param>
        /// <param name="sourceFileName">原始檔名。</param>
        /// <param name="company">物流公司。</param>
        /// <param name="uploadFormat">物流上傳格式。</param>
        /// <param name="repaymentDate">回款日期。</param>
        /// <param name="currentUserId">操作人員。</param>
        /// <returns>上傳結果。</returns>
        private ReconciliationLogisticsUploadResult ReconcileUploadRows(
            List<ReconciliationLogisticsUploadRow> uploadRows,
            string sourceFileName,
            ReconciliationLogisticsCompany company,
            ReconciliationLogisticsUploadFormat uploadFormat,
            DateTime repaymentDate,
            string currentUserId)
        {
            var currentTime = DateTime.Now;
            var entities = uploadRows
                .Select(row => CreateEntity(
                    row,
                    sourceFileName,
                    company,
                    repaymentDate,
                    currentUserId,
                    currentTime))
                .ToList();

            var result = MatchAndApplyReceivedAmounts(
                entities,
                uploadFormat,
                repaymentDate,
                currentUserId);

            // IsFeeMaster 代表該筆上傳資料至少成功更新一筆費用明細。
            var updatedCount = entities.Count(x => x.IsFeeMaster);
            var unmatchedCount = entities.Count - updatedCount;
            result.UpdatedCount = updatedCount;
            result.UnmatchedCount = unmatchedCount;
            result.ExceptionCount = result.Results.Count(
                x => x.IsSuccess && x.IsException);
            return result;
        }

        /// <summary>
        /// 比對費用明細、分配回款並加入需保留追蹤的物流銷帳紀錄。
        /// </summary>
        /// <param name="entities">尚未寫入的物流銷帳資料。</param>
        /// <param name="uploadFormat">物流上傳格式。</param>
        /// <param name="repaymentDate">回款日期。</param>
        /// <param name="currentUserId">操作人員。</param>
        /// <returns>物流銷帳比對及更新結果。</returns>
        private ReconciliationLogisticsUploadResult MatchAndApplyReceivedAmounts(
            List<ReconciliationLogisticsEntity> entities,
            ReconciliationLogisticsUploadFormat uploadFormat,
            DateTime repaymentDate,
            string currentUserId)
        {
            // Step 1：先建立每筆上傳資料的預設結果，讓失敗資料也能顯示於畫面及 Excel。
            var resultByEntity = entities.ToDictionary(
                x => x,
                CreateResultItem);

            // Step 2：依上傳格式從費用主檔或明細取得單號對應的 FEE_MASTER_ID。
            var matchedFeeMasterIds = GetMatchedFeeMasterIds(
                entities,
                uploadFormat);
            var feeMasterIds = matchedFeeMasterIds
                .Select(x => x.FeeMasterId)
                .Distinct()
                .ToList();
            if (!feeMasterIds.Any())
            {
                // 整批皆查無物流貨號時仍保留客戶回款紀錄，但不更新任何費用明細。
                foreach (var entity in entities)
                {
                    entity.Status = resultByEntity[entity].Status;
                }

                JetfDb.BulkInsert(entities);
                return new ReconciliationLogisticsUploadResult
                {
                    Results = entities.Select(x => resultByEntity[x]).ToList()
                };
            }

            // Step 3：依 FEE_MASTER_ID 一次取得費用主檔及同主檔底下的全部費用明細。
            var feeMasters = GetFeeMasters(feeMasterIds);
            var feeMasterDetails = GetFeeMasterDetails(feeMasterIds);

            // Step 4：每筆上傳資料直接取第一個符合單號的 FEE_MASTER_ID。
            Func<ReconciliationLogisticsFeeMasterMatch, string> matchedKeySelector;
            Func<ReconciliationLogisticsEntity, string> entityKeySelector;
            switch (uploadFormat)
            {
                // 關貿只使用分提單號比對。
                case ReconciliationLogisticsUploadFormat.TradeVan:
                    matchedKeySelector =
                        x => (x.TrackingNo ?? string.Empty).Trim();
                    entityKeySelector =
                        x => (x.TrackingNo ?? string.Empty).Trim();
                    break;
                // 客樂得、大榮、現金及圓通只使用物流貨號比對。
                case ReconciliationLogisticsUploadFormat.Kelede:
                case ReconciliationLogisticsUploadFormat.Ktj:
                case ReconciliationLogisticsUploadFormat.Cash:
                case ReconciliationLogisticsUploadFormat.Yto:
                    matchedKeySelector =
                        x => (x.DlvInv ?? string.Empty).Trim();
                    entityKeySelector =
                        x => (x.DlvInv ?? string.Empty).Trim();
                    break;
                // 新竹物流、7-11 及超峰使用分提單號與物流貨號共同比對。
                case ReconciliationLogisticsUploadFormat.HctCollection:
                case ReconciliationLogisticsUploadFormat.HctRemittance:
                case ReconciliationLogisticsUploadFormat.SevenEleven:
                case ReconciliationLogisticsUploadFormat.TaixinStar:
                    matchedKeySelector = x =>
                        $"{(x.TrackingNo ?? string.Empty).Trim()}|{(x.DlvInv ?? string.Empty).Trim()}";
                    entityKeySelector = x =>
                        $"{(x.TrackingNo ?? string.Empty).Trim()}|{(x.DlvInv ?? string.Empty).Trim()}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(uploadFormat),
                        uploadFormat,
                        "不支援的物流上傳格式");
            }

            var feeMasterIdDictionary = matchedFeeMasterIds
                .GroupBy(
                    matchedKeySelector,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().FeeMasterId,
                    StringComparer.OrdinalIgnoreCase);

            var feeMasterById = feeMasters.ToDictionary(x => x.Id);
            var entityByFeeMasterId = new Dictionary<int, ReconciliationLogisticsEntity>();
            foreach (var entity in entities)
            {
                int feeMasterId;
                if (!feeMasterIdDictionary.TryGetValue(
                    entityKeySelector(entity),
                    out feeMasterId))
                {
                    continue;
                }

                resultByEntity[entity].Status = ReconciliationLogisticsResultStatus.Matched;
                entityByFeeMasterId.Add(feeMasterId, entity);
            }

            // Step 5：依費用主檔分組計算應收金額，並將回款依序分配至各筆明細。
            var updatedDetails = new List<FeeMasterDetailEntity>();
            foreach (var detailGroup in feeMasterDetails.GroupBy(x => x.FeeMasterId))
            {
                ReconciliationLogisticsEntity entity;
                if (!entityByFeeMasterId.TryGetValue(detailGroup.Key, out entity))
                {
                    continue;
                }

                var resultItem = resultByEntity[entity];
                var feeMaster = feeMasterById[detailGroup.Key];
                // 畫面應收金額直接使用費用主檔的 TO_DLV_COD。
                resultItem.ReceivableAmount = feeMaster.ToDlvCod.ToInt();
                resultItem.Difference = resultItem.ReceivableAmount - resultItem.RepaymentAmount;
                entity.DifferenceAmount = resultItem.Difference;

                // 先分配可銷帳金額並收集異動明細，再依差異判斷金額不足或超額。
                var appliedDetails = ApplyReceivedAmount(
                    entity,
                    detailGroup,
                    repaymentDate,
                    currentUserId);
                updatedDetails.AddRange(appliedDetails);

                if (entity.IsFeeMaster && resultItem.Difference < 0)
                {
                    resultItem.Status =
                        ReconciliationLogisticsResultStatus.RepaymentExceedsReceivable;
                }
                else if (entity.IsFeeMaster && resultItem.Difference > 0)
                {
                    resultItem.Status =
                        ReconciliationLogisticsResultStatus.RepaymentLessThanReceivable;
                }
            }

            // Step 6：已命中主檔但沒有可銷帳金額的資料標記為失敗，並保留紀錄供後續追蹤。
            foreach (var entity in entityByFeeMasterId.Values
                .Where(x =>
                    !x.IsFeeMaster &&
                    resultByEntity[x].Status == ReconciliationLogisticsResultStatus.Matched)
                .ToList())
            {
                resultByEntity[entity].Status = ReconciliationLogisticsResultStatus.NoReceivableAmount;
            }

            // Step 7：將比對狀態寫入所有通過欄位驗證的回款紀錄。
            foreach (var entity in entities)
            {
                entity.Status = resultByEntity[entity].Status;
            }

            JetfDb.BulkInsert(
                entities,
                options => options.AutoMapOutputDirection = true);

            // Step 8：將批次新增後取得的物流銷帳 Id 回填至實際分配回款的費用明細。
            foreach (var detail in updatedDetails)
            {
                detail.ReconciliationLogisticsId =
                    detail.ReconciliationLogistics.Id;
            }

            // Step 9：批次更新費用明細，避免 EF 逐筆產生 UPDATE。
            JetfDb.BulkUpdate(updatedDetails);

            return new ReconciliationLogisticsUploadResult
            {
                UpdatedDetailCount = updatedDetails.Count,
                Results = entities.Select(x => resultByEntity[x]).ToList()
            };
        }

        /// <summary>
        /// 依上傳格式取得單號與已下載費用主檔識別碼的對應資料。
        /// </summary>
        /// <param name="entities">待比對的物流銷帳資料。</param>
        /// <param name="uploadFormat">物流上傳格式。</param>
        /// <returns>符合單號的費用主檔識別碼。</returns>
        private List<ReconciliationLogisticsFeeMasterMatch> GetMatchedFeeMasterIds(
            List<ReconciliationLogisticsEntity> entities,
            ReconciliationLogisticsUploadFormat uploadFormat)
        {
            switch (uploadFormat)
            {
                case ReconciliationLogisticsUploadFormat.HctRemittance:
                case ReconciliationLogisticsUploadFormat.TaixinStar:
                    // 新竹物流匯款明細及超峰都以兩個單號直接比對費用明細。
                    return JetfDb.FeeMasterDetails
                        .AsNoTracking()
                        .Where(detail => detail.FeeMaster.Download == "1")
                        .WhereBulkContains(
                            JetfDb,
                            entities,
                            detail => new { detail.TrackingNo, detail.DlvInv },
                            entity => new { entity.TrackingNo, entity.DlvInv })
                        .Select(detail => new ReconciliationLogisticsFeeMasterMatch
                        {
                            FeeMasterId = detail.FeeMasterId,
                            TrackingNo = detail.TrackingNo,
                            DlvInv = detail.DlvInv
                        })
                        .ToList();
                case ReconciliationLogisticsUploadFormat.Kelede:
                case ReconciliationLogisticsUploadFormat.Ktj:
                case ReconciliationLogisticsUploadFormat.Cash:
                case ReconciliationLogisticsUploadFormat.Yto:
                    // 客樂得、大榮、現金及圓通只使用物流貨號直接比對費用明細。
                    return JetfDb.FeeMasterDetails
                        .AsNoTracking()
                        .Where(detail => detail.FeeMaster.Download == "1")
                        .WhereBulkContains(
                            JetfDb,
                            entities,
                            detail => detail.DlvInv,
                            entity => entity.DlvInv)
                        .Select(detail => new ReconciliationLogisticsFeeMasterMatch
                        {
                            FeeMasterId = detail.FeeMasterId,
                            TrackingNo = detail.TrackingNo,
                            DlvInv = detail.DlvInv
                        })
                        .ToList();
                case ReconciliationLogisticsUploadFormat.TradeVan:
                    // 關貿只使用分提單號碼直接比對費用明細。
                    return JetfDb.FeeMasterDetails
                        .AsNoTracking()
                        .Where(detail => detail.FeeMaster.Download == "1")
                        .WhereBulkContains(
                            JetfDb,
                            entities,
                            detail => detail.TrackingNo,
                            entity => entity.TrackingNo)
                        .Select(detail => new ReconciliationLogisticsFeeMasterMatch
                        {
                            FeeMasterId = detail.FeeMasterId,
                            TrackingNo = detail.TrackingNo,
                            DlvInv = detail.DlvInv
                        })
                        .ToList();
                case ReconciliationLogisticsUploadFormat.HctCollection:
                case ReconciliationLogisticsUploadFormat.SevenEleven:
                    // 原有格式先依主檔物流貨號取得候選資料，再於後續使用兩個單號確認。
                    return JetfDb.FeeMasters
                        .AsNoTracking()
                        .Where(feeMaster => feeMaster.Download == "1")
                        .WhereBulkContains(
                            JetfDb,
                            entities,
                            feeMaster => feeMaster.DlvInv,
                            entity => entity.DlvInv)
                        .Select(feeMaster => new ReconciliationLogisticsFeeMasterMatch
                        {
                            FeeMasterId = feeMaster.Id,
                            TrackingNo = feeMaster.TrackingNo,
                            DlvInv = feeMaster.DlvInv
                        })
                        .ToList();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(uploadFormat),
                        uploadFormat,
                        "不支援的物流上傳格式");
            }
        }

        /// <summary>
        /// 取得指定且已下載的費用主檔。
        /// </summary>
        /// <param name="feeMasterIds">費用主檔識別碼。</param>
        /// <returns>費用主檔。</returns>
        private List<FeeMasterEntity> GetFeeMasters(IEnumerable<int> feeMasterIds)
        {
            return JetfDb.FeeMasters
                .AsNoTracking()
                .Where(feeMaster => feeMaster.Download == "1")
                .WhereBulkContains(
                    JetfDb,
                    feeMasterIds,
                    feeMaster => feeMaster.Id,
                    feeMasterId => feeMasterId);
        }

        /// <summary>
        /// 建立一筆預設為查無物流貨號的畫面結果。
        /// </summary>
        /// <param name="entity">物流銷帳資料。</param>
        /// <returns>畫面結果。</returns>
        private static ReconciliationLogisticsResultItem CreateResultItem(
            ReconciliationLogisticsEntity entity)
        {
            // 預設為查無資料；後續成功命中費用主檔時再更新狀態。
            return new ReconciliationLogisticsResultItem
            {
                RepaymentDate = entity.RepaymentDate.ToString("yyyy/MM/dd"),
                Company = entity.Company.ToDescription(),
                TrackingNo = entity.TrackingNo,
                DlvInv = entity.DlvInv,
                RepaymentAmount = entity.ReceivedAmount,
                Difference = 0,
                Status = ReconciliationLogisticsResultStatus.FeeMasterNotFound
            };
        }

        /// <summary>
        /// 取得指定費用主檔底下尚未物流銷帳的明細。
        /// </summary>
        /// <param name="feeMasterIds">費用主檔識別碼。</param>
        /// <returns>費用主檔明細。</returns>
        private List<FeeMasterDetailEntity> GetFeeMasterDetails(IEnumerable<int> feeMasterIds)
        {
            return JetfDb.FeeMasterDetails
                .Where(detail => !detail.ReconciliationLogisticsId.HasValue)
                .WhereBulkContains(
                    JetfDb,
                    feeMasterIds,
                    detail => detail.FeeMasterId,
                    feeMasterId => feeMasterId);
        }

        /// <summary>
        /// 將一筆物流回款依序分配至同一費用主檔的明細。
        /// </summary>
        /// <param name="entity">物流銷帳資料。</param>
        /// <param name="details">同一費用主檔尚未物流銷帳的明細。</param>
        /// <param name="repaymentDate">回款日期。</param>
        /// <param name="currentUserId">操作人員。</param>
        /// <returns>實際分配回款的費用明細。</returns>
        private static List<FeeMasterDetailEntity> ApplyReceivedAmount(
            ReconciliationLogisticsEntity entity,
            IEnumerable<FeeMasterDetailEntity> details,
            DateTime repaymentDate,
            string currentUserId)
        {
            // 尚未分配至費用明細的回款金額。
            var remainingReceivedAmount = entity.ReceivedAmount;
            var updatedDetails = new List<FeeMasterDetailEntity>();

            // Step 1：優先處理有手續費的明細，再依 Id 固定其餘明細的分配順序。
            var pendingDetails = details
                .OrderByDescending(x => (x.Fee ?? 0) > 0)
                .ThenBy(x => x.Id);

            // Step 2：依序取得每筆明細的應收金額，並分配物流公司的回款金額。
            foreach (var detail in pendingDetails)
            {
                if (remainingReceivedAmount <= 0)
                {
                    break;
                }

                // 目前這筆費用明細的應收金額直接使用 TO_DLV_COD。
                var detailReceivableAmount =
                    (long)detail.ToDlvCod.ToInt();
                if (detailReceivableAmount <= 0)
                {
                    continue;
                }

                // 本筆實際分配的銷帳金額，取剩餘回款與明細應收金額中的較小值。
                var allocatedAmount = (int)Math.Min(
                    remainingReceivedAmount,
                    detailReceivableAmount);
                detail.ReceivedToDlvCod = allocatedAmount;
                detail.ReceivedToDlvCodTime = repaymentDate;
                detail.ReceivedToDlvCodUserId = currentUserId;
                detail.ReconciliationLogistics = entity;

                remainingReceivedAmount -= allocatedAmount;
                updatedDetails.Add(detail);
            }

            // Step 3：只要實際更新一筆明細，即標記此筆物流資料已完成費用比對。
            entity.IsFeeMaster = updatedDetails.Count > 0;
            return updatedDetails;
        }

        /// <summary>
        /// 將上傳資料轉換為物流銷帳資料庫實體。
        /// </summary>
        /// <param name="row">上傳資料。</param>
        /// <param name="sourceFileName">原始檔名。</param>
        /// <param name="company">物流公司。</param>
        /// <param name="repaymentDate">回款日期。</param>
        /// <param name="currentUserId">操作人員。</param>
        /// <param name="currentTime">上傳時間。</param>
        /// <returns>物流銷帳資料庫實體。</returns>
        private static ReconciliationLogisticsEntity CreateEntity(
            ReconciliationLogisticsUploadRow row,
            string sourceFileName,
            ReconciliationLogisticsCompany company,
            DateTime repaymentDate,
            string currentUserId,
            DateTime currentTime)
        {
            return new ReconciliationLogisticsEntity
            {
                Company = company,
                RepaymentDate = repaymentDate,
                TrackingNo = row.TrackingNo ?? string.Empty,
                DlvInv = row.DlvInv,
                ReceivedAmount = row.ReceivedAmount.Value,
                CustomerCode = row.CustomerCode,
                SourceFileName = string.IsNullOrWhiteSpace(sourceFileName)
                    ? "未命名"
                    : sourceFileName,
                CreatedUserId = currentUserId,
                CreatedTime = currentTime
            };
        }

        /// <summary>
        /// 全部資料皆驗證失敗時建立錯誤回應。
        /// </summary>
        /// <param name="uploadRows">上傳資料。</param>
        /// <returns>錯誤回應。</returns>
        private static ResponseModel CreateValidationFailureResponse(
            IEnumerable<ReconciliationLogisticsUploadRow> uploadRows)
        {
            var rows = uploadRows.ToList();
            var failRows = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.FailReason))
                .ToList();

            var result = new ReconciliationLogisticsUploadResult
            {
                Count = rows.Count,
                FailCount = failRows.Count,
                Message = $"檔案共有 {rows.Count} 筆資料，失敗 {failRows.Count} 筆，沒有可寫入的資料",
                Data = failRows
            };

            return new ResponseModel
            {
                IsSuccess = false,
                status = Status.error,
                msg = result.Message,
                ReturnObject = result
            };
        }

        /// <summary>
        /// 附加單筆上傳資料的失敗原因。
        /// </summary>
        /// <param name="row">上傳資料。</param>
        /// <param name="reason">失敗原因。</param>
        private static void AppendFailReason(
            ReconciliationLogisticsUploadRow row,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(row.FailReason))
            {
                row.FailReason = reason;
                return;
            }

            if (row.FailReason.IndexOf(reason, StringComparison.OrdinalIgnoreCase) < 0)
            {
                row.FailReason += "；" + reason;
            }
        }

        /// <summary>
        /// 物流上傳檔案格式。
        /// </summary>
        private enum ReconciliationLogisticsUploadFormat
        {
            /// <summary>
            /// 新竹物流清單編號及查貨號碼格式。
            /// </summary>
            HctCollection,

            /// <summary>
            /// 新竹物流出貨單號及宅配單號匯款明細格式。
            /// </summary>
            HctRemittance,

            /// <summary>
            /// 7-11 訂單格式。
            /// </summary>
            SevenEleven,

            /// <summary>
            /// 客樂得現金結帳明細格式。
            /// </summary>
            Kelede,

            /// <summary>
            /// 大榮 CSV 格式。
            /// </summary>
            Ktj,

            /// <summary>
            /// 超峰現金結帳明細格式。
            /// </summary>
            TaixinStar,

            /// <summary>
            /// 現金運單格式。
            /// </summary>
            Cash,

            /// <summary>
            /// 圓通 COD 帳單格式。
            /// </summary>
            Yto,

            /// <summary>
            /// 關貿交易明細格式。
            /// </summary>
            TradeVan
        }

    }
}
