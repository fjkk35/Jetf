using System.Collections.Generic;

namespace Service.Services.ShipmentInboundExceptionRecord.Domain
{
    /// <summary>
    /// 貨件回倉異常紀錄查詢回應。
    /// </summary>
    public class ShipmentInboundExceptionRecordResponse
    {
        /// <summary>
        /// 查詢結果資料。
        /// </summary>
        public List<ShipmentInboundExceptionRecordModel> Data { get; set; }

        /// <summary>
        /// 符合查詢條件的總筆數。
        /// </summary>
        public int TotalCount { get; set; }
    }
}
