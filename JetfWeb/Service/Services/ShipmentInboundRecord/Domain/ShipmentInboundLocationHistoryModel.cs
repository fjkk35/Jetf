using System;

namespace Service.Services.ShipmentInboundRecord.Domain
{
    /// <summary>
    /// 儲位歷史紀錄 Model
    /// </summary>
    public class ShipmentInboundLocationHistoryModel
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ShipmentInbound 的 Id
        /// </summary>
        public int ShipmentInboundId { get; set; }

        /// <summary>
        /// 舊儲位
        /// </summary>
        public string OldLocationCode { get; set; }

        /// <summary>
        /// 新儲位
        /// </summary>
        public string NewLocationCode { get; set; }

        /// <summary>
        /// 建立人員
        /// </summary>
        public string CreatedOpe { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedTime { get; set; }
    }
}
