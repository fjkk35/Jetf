using System.Collections.Generic;

namespace Service.Services.ShipmentInboundLocationTransfer.Domain
{
    /// <summary>
    /// 儲位調撥查詢回應模型
    /// </summary>
    public class LocationTransferResponse
    {
        /// <summary>
        /// 資料列表
        /// </summary>
        public List<LocationTransferModel> Data { get; set; }

        /// <summary>
        /// 總筆數
        /// </summary>
        public int TotalCount { get; set; }
    }
}
