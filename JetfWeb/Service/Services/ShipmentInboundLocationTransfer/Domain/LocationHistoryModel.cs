using System;

namespace Service.Services.ShipmentInboundLocationTransfer.Domain
{
    /// <summary>
    /// 儲位歷史紀錄模型
    /// </summary>
    public class LocationHistoryModel
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 舊儲位
        /// </summary>
        public string OldLocationCode { get; set; }

        /// <summary>
        /// 新儲位
        /// </summary>
        public string NewLocationCode { get; set; }

        /// <summary>
        /// 修改人員
        /// </summary>
        public string CreatedOpe { get; set; }

        /// <summary>
        /// 修改時間
        /// </summary>
        public DateTime CreatedTime { get; set; }
    }
}
