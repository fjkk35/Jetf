using System;

namespace Service.Services.ShipmentOutboundBatchImportRevoke.Domain
{
    /// <summary>
    /// 貨件出庫取消上傳結果
    /// </summary>
    public class ShipmentOutboundRevokeModel
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 出庫日期
        /// </summary>
        public DateTime? OutboundDate { get; set; }

        /// <summary>
        /// 新物流單號
        /// </summary>
        public string OutboundTrackingNo { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 失敗原因
        /// </summary>
        public string FailReason { get; set; }

        /// <summary>
        /// ShipmentInbound Id
        /// </summary>
        public int? ShipmentInboundId { get; set; }
    }
}
