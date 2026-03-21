using System.Collections.Generic;

namespace Service.Services.ShipmentInboundLocationTransfer.Domain
{
    /// <summary>
    /// 儲位調撥更新請求模型
    /// </summary>
    public class LocationTransferUpdateRequest
    {
        /// <summary>
        /// 需要更新的 Id 列表
        /// </summary>
        public List<int> Ids { get; set; }

        /// <summary>
        /// 新儲位
        /// </summary>
        public string NewLocationCode { get; set; }
    }
}
