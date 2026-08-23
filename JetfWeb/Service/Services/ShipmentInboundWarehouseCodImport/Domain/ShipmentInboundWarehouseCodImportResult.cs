using System.Collections.Generic;

namespace Service.Services.ShipmentInboundWarehouseCodImport.Domain
{
    /// <summary>
    /// 倉庫代收上傳結果。
    /// </summary>
    public sealed class ShipmentInboundWarehouseCodImportResult
    {
        /// <summary>
        /// 建立上傳結果。
        /// </summary>
        public ShipmentInboundWarehouseCodImportResult()
        {
            Data = new List<ShipmentInboundWarehouseCodImportRow>();
        }

        /// <summary>
        /// Excel 資料總筆數。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 成功寫入筆數。
        /// </summary>
        public int InsertedCount { get; set; }

        /// <summary>
        /// 驗證失敗筆數。
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 上傳結果訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 驗證失敗明細。
        /// </summary>
        public List<ShipmentInboundWarehouseCodImportRow> Data { get; set; }
    }
}
