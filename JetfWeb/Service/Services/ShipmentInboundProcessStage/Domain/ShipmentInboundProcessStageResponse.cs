using System.Collections.Generic;

namespace Service.Services.ShipmentInboundProcessStage.Domain
{
    /// <summary>
    /// 預先登記處理查詢結果。
    /// </summary>
    public class ShipmentInboundProcessStageResponse
    {
        /// <summary>
        /// 查詢資料。
        /// </summary>
        public List<ShipmentInboundProcessStageModel> Data { get; set; }

        /// <summary>
        /// 符合查詢條件的總筆數。
        /// </summary>
        public int TotalCount { get; set; }
    }
}
