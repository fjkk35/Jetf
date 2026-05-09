using System;

namespace Service.Services.ShipmentInboundLocationTransfer.Domain
{
    /// <summary>
    /// 儲位調撥資料模型
    /// </summary>
    public class LocationTransferModel
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 流水號
        /// </summary>
        public string SeqNo { get; set; }

        /// <summary>
        /// 儲位
        /// </summary>
        public string LocationCode { get; set; }
    }
}
