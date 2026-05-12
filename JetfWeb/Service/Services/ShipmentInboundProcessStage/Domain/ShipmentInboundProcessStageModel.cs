using Service.EnumTax;
using Service.Extensions;

namespace Service.Services.ShipmentInboundProcessStage.Domain
{
    /// <summary>
    /// 預先登記處理列表資料。
    /// </summary>
    public class ShipmentInboundProcessStageModel
    {
        /// <summary>
        /// 預先登記資料 Id。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 退件原因。
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// 處理方式。
        /// </summary>
        public ShipmentInboundProcessType? ProcessType { get; set; }

        /// <summary>
        /// 處理方式名稱。
        /// </summary>
        public string ProcessTypeName => ProcessType?.ToDescription();
    }
}
