using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundWarehouseCodImport.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundWarehouseCodImport
{
    /// <summary>
    /// 倉庫代收 Excel 上傳服務。
    /// </summary>
    public sealed class ShipmentInboundWarehouseCodImportService : _BaseService
    {
        private const string WarehouseDataType = "倉庫";

        private static readonly string[] RequiredHeaders =
        {
            "託運單號",
            "訂單編號",
            "件數",
            "客戶",
            "收件人",
            "地址",
            "電話",
            "類別",
            "客代",
            "狀態",
            "代收款",
            "模式",
            "廠商對應單號",
            "訂單狀態"
        };

        /// <summary>
        /// 建立倉庫代收上傳服務。
        /// </summary>
        /// <param name="jetfDbContext">JETF 資料庫內容。</param>
        /// <param name="dataCenterDbContext">Data Center 資料庫內容。</param>
        public ShipmentInboundWarehouseCodImportService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 讀取 Excel、檢查重複資料並批次寫入 FEE_MASTER_COD。
        /// </summary>
        /// <param name="filePath">已保存的 Excel 檔案路徑。</param>
        /// <returns>上傳結果。</returns>
        public ResponseModel Upload(string filePath)
        {
            try
            {
                var rows = ReadUploadRows(filePath);
                if (rows.Count == 0)
                {
                    return new ResponseModel("Excel 無資料");
                }

                var validationErrors = ValidateRows(rows);
                // 驗證失敗的資料保留於回傳明細，其餘正確資料仍可繼續上傳。
                var entities = rows
                    .Where(x => string.IsNullOrWhiteSpace(x.FailReason))
                    .Select(CreateEntity)
                    .ToList();
                if (entities.Count > 0)
                {
                    using (var transaction = JetfDb.Database.BeginTransaction())
                    {
                        try
                        {
                            // 使用既有 EF bulk 擴充一次寫入，避免逐筆 SaveChanges。
                            JetfDb.BulkInsert(entities);
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                var result = new ShipmentInboundWarehouseCodImportResult
                {
                    Count = rows.Count,
                    InsertedCount = entities.Count,
                    FailCount = validationErrors.Count,
                    Message = $"上傳完成，共 {rows.Count} 筆，成功 {entities.Count} 筆，失敗 {validationErrors.Count} 筆。",
                    Data = validationErrors
                };

                return new ResponseModel
                {
                    IsSuccess = true,
                    status = Status.success,
                    msg = result.Message,
                    ReturnObject = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"倉庫代收上傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 依 Excel 表頭名稱讀取第一個工作表的資料列。
        /// </summary>
        /// <param name="filePath">Excel 檔案路徑。</param>
        /// <returns>上傳資料列。</returns>
        private static List<ShipmentInboundWarehouseCodImportRow> ReadUploadRows(string filePath)
        {
            IWorkbook workbook;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                workbook = new XSSFWorkbook(stream);
            }

            try
            {
                if (workbook.NumberOfSheets == 0)
                {
                    return new List<ShipmentInboundWarehouseCodImportRow>();
                }

                var sheet = workbook.GetSheetAt(0);
                int headerRowIndex;
                Dictionary<string, int> columnIndexes;
                if (!TryFindHeaderRow(sheet, out headerRowIndex, out columnIndexes))
                {
                    throw new InvalidDataException(
                        $"找不到完整表頭，請確認包含：{string.Join("、", RequiredHeaders)}。 ");
                }

                var rows = new List<ShipmentInboundWarehouseCodImportRow>();
                for (var rowIndex = headerRowIndex + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var excelRow = sheet.GetRow(rowIndex);
                    if (excelRow == null)
                    {
                        continue;
                    }

                    if (excelRow.GetCellData(columnIndexes["託運單號"]) == "對應欄位")
                    {
                        // 「對應欄位」以下為檔案格式說明，不屬於實際上傳資料。
                        break;
                    }

                    var row = ReadUploadRow(excelRow, rowIndex + 1, columnIndexes);
                    if (!IsEmptyRow(row))
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
        /// 依欄位索引讀取單筆倉庫代收資料，並套用物流公司特殊單號規則。
        /// </summary>
        /// <param name="excelRow">Excel 資料列。</param>
        /// <param name="rowNumber">Excel 列號。</param>
        /// <param name="columnIndexes">表頭欄位索引。</param>
        /// <returns>解析後的資料列。</returns>
        private static ShipmentInboundWarehouseCodImportRow ReadUploadRow(
            IRow excelRow,
            int rowNumber,
            IDictionary<string, int> columnIndexes)
        {
            var shipmentNo = excelRow.GetCellData(columnIndexes["託運單號"]);
            var orderNo = excelRow.GetCellData(columnIndexes["訂單編號"]);
            var type = excelRow.GetCellData(columnIndexes["類別"]);
            var vendorOrderNo = excelRow.GetCellData(columnIndexes["廠商對應單號"]);
            var ccText = excelRow.GetCellData(columnIndexes["代收款"]);
            var row = new ShipmentInboundWarehouseCodImportRow
            {
                RowNo = rowNumber,
                ShipmentNo = shipmentNo,
                OrderNo = orderNo,
                Customer = excelRow.GetCellData(columnIndexes["客戶"]),
                Type = type,
                VendorOrderNo = vendorOrderNo,
                Cc = ParseCc(ccText),
                CcText = ccText,
                TrackingNo = orderNo,
                DlvInv = shipmentNo
            };

            switch (type)
            {
                case "統一數網":
                    // 統一數網的託運單號從第 4 個字元開始才是實際物流貨號。
                    row.DlvInv = shipmentNo.Length >= 4
                        ? shipmentNo.Substring(3)
                        : string.Empty;
                    break;
                case "日翊物流":
                    // 日翊物流以廠商對應單號作為 DLV_INV，原託運單號作為 TRACKINGNO。
                    row.DlvInv = vendorOrderNo;
                    row.TrackingNo = shipmentNo;
                    break;
            }

            return row;
        }

        /// <summary>
        /// 找出包含所有指定欄位的表頭列。
        /// </summary>
        /// <param name="sheet">Excel 工作表。</param>
        /// <param name="headerRowIndex">表頭列索引。</param>
        /// <param name="columnIndexes">欄位名稱與索引。</param>
        /// <returns>是否找到表頭。</returns>
        private static bool TryFindHeaderRow(
            ISheet sheet,
            out int headerRowIndex,
            out Dictionary<string, int> columnIndexes)
        {
            headerRowIndex = -1;
            columnIndexes = null;

            for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var candidate = new Dictionary<string, int>(StringComparer.Ordinal);
                for (var columnIndex = 0; columnIndex < row.LastCellNum; columnIndex++)
                {
                    var header = row.GetCellData(columnIndex);
                    if (!string.IsNullOrWhiteSpace(header) && !candidate.ContainsKey(header))
                    {
                        candidate.Add(header, columnIndex);
                    }
                }

                if (!RequiredHeaders.All(candidate.ContainsKey))
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
        /// 驗證資料列並以一次批次查詢確認資料庫沒有重複物流貨號。
        /// </summary>
        /// <param name="rows">待驗證資料列。</param>
        /// <returns>驗證失敗資料列。</returns>
        private List<ShipmentInboundWarehouseCodImportRow> ValidateRows(
            List<ShipmentInboundWarehouseCodImportRow> rows)
        {
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ShipmentNo))
                {
                    AppendFailReason(row, "託運單號不可為空");
                }

                if (string.IsNullOrWhiteSpace(row.OrderNo))
                {
                    AppendFailReason(row, "訂單編號不可為空");
                }

                if (string.IsNullOrWhiteSpace(row.Customer))
                {
                    AppendFailReason(row, "客戶不可為空");
                }

                if (string.IsNullOrWhiteSpace(row.Type))
                {
                    AppendFailReason(row, "類別不可為空");
                }

                if (!row.Cc.HasValue)
                {
                    AppendFailReason(row, "代收款不可為空或格式錯誤");
                }
                else if (row.Cc.Value < 0)
                {
                    AppendFailReason(row, "代收款不可小於 0");
                }

                if (row.Type == "統一數網" && row.ShipmentNo.Length < 4)
                {
                    AppendFailReason(row, "統一數網託運單號長度不足，無法從第 4 位開始取號");
                }

                if (row.Type == "日翊物流" && string.IsNullOrWhiteSpace(row.VendorOrderNo))
                {
                    AppendFailReason(row, "日翊物流必須提供廠商對應單號");
                }

                if (string.IsNullOrWhiteSpace(row.DlvInv))
                {
                    AppendFailReason(row, "物流貨號不可為空");
                }
            }

            var duplicateDlvInvs = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.DlvInv))
                .GroupBy(x => x.DlvInv, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.Where(x => duplicateDlvInvs.Contains(x.DlvInv)))
            {
                AppendFailReason(row, "物流貨號在上傳檔案內重複");
            }

            var queryDlvInvs = rows
                .Where(x => string.IsNullOrWhiteSpace(x.FailReason) && !string.IsNullOrWhiteSpace(x.DlvInv))
                .Select(x => x.DlvInv)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (queryDlvInvs.Count > 0)
            {
                var existingDlvInvs = JetfDb.FeeMasterCods
                    .AsNoTracking()
                    .WhereBulkContains(
                        JetfDb,
                        queryDlvInvs,
                        x => x.DlvInv,
                        x => x,
                        x => x.DlvInv)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows.Where(x => existingDlvInvs.Contains(x.DlvInv)))
                {
                    AppendFailReason(row, "物流貨號已存在");
                }
            }

            return rows.Where(x => !string.IsNullOrWhiteSpace(x.FailReason)).ToList();
        }

        /// <summary>
        /// 建立 FEE_MASTER_COD 實體。
        /// </summary>
        /// <param name="row">已通過驗證的資料列。</param>
        /// <returns>待新增實體。</returns>
        private static FeeMasterCodEntity CreateEntity(
            ShipmentInboundWarehouseCodImportRow row)
        {
            var now = DateTime.Now;
            return new FeeMasterCodEntity
            {
                DataType = WarehouseDataType,
                MainNumber = row.OrderNo ?? string.Empty,
                Customer = row.Customer,
                BagNumber = string.Empty,
                TrackingNo = row.TrackingNo,
                DlvInv = row.DlvInv,
                Type = row.Type,
                Cc = row.Cc.GetValueOrDefault(),
                // 倉庫代收款即為應向物流收取的代收金額。
                ToDlvCod = decimal.ToInt32(row.Cc.GetValueOrDefault()),
                IsShipmentInbound = true,
                SignOutTime = now,
                CreatedTime = now
            };
        }

        /// <summary>
        /// 解析代收款欄位。
        /// </summary>
        /// <param name="value">Excel 儲存格文字。</param>
        /// <returns>代收款；空白或格式錯誤時回傳 null。</returns>
        private static decimal? ParseCc(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal amount;
            return decimal.TryParse(value, out amount)
                ? (decimal?)amount
                : null;
        }

        /// <summary>
        /// 判斷資料列是否完全空白。
        /// </summary>
        /// <param name="row">資料列。</param>
        /// <returns>是否為空白列。</returns>
        private static bool IsEmptyRow(ShipmentInboundWarehouseCodImportRow row)
        {
            return string.IsNullOrWhiteSpace(row.ShipmentNo) &&
                   string.IsNullOrWhiteSpace(row.OrderNo) &&
                   string.IsNullOrWhiteSpace(row.Customer) &&
                   string.IsNullOrWhiteSpace(row.Type) &&
                   string.IsNullOrWhiteSpace(row.VendorOrderNo) &&
                   string.IsNullOrWhiteSpace(row.CcText);
        }

        /// <summary>
        /// 累加資料列驗證錯誤。
        /// </summary>
        /// <param name="row">資料列。</param>
        /// <param name="reason">錯誤原因。</param>
        private static void AppendFailReason(
            ShipmentInboundWarehouseCodImportRow row,
            string reason)
        {
            row.FailReason = string.IsNullOrWhiteSpace(row.FailReason)
                ? reason
                : $"{row.FailReason}；{reason}";
        }
    }
}
