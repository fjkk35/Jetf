using Service.EnumTax;
using Service.Extensions;
using System;

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

        public string Remark { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int? Cod { get; set; }

        /// <summary>
        /// 運費。
        /// </summary>
        public int? FreightFee { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int? CcFee { get; set; }

        /// <summary>
        /// 代收手續費。
        /// </summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 代收款總金額。
        /// </summary>
        public int CollectionTotalAmount =>
            (Cod ?? 0) + (FreightFee ?? 0) + (Tax ?? 0) + (CcFee ?? 0) + (Fee ?? 0);

        /// <summary>
        /// 輸入日期。
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 輸入人員。
        /// </summary>
        public string CreatedOpe { get; set; }

        /// <summary>
        /// 匹配日期。
        /// </summary>
        public DateTime? MatchTimie { get; set; }

        /// <summary>
        /// 處理方式。
        /// </summary>
        public ShipmentInboundProcessType? ProcessType { get; set; }

        /// <summary>
        /// 重出派件公司。
        /// </summary>
        public ShipmentInboundProcessTransNo? ProcessTransNo { get; set; }

        /// <summary>
        /// 處理方式名稱。
        /// </summary>
        public string ProcessTypeName => ProcessType?.ToDescription();

        /// <summary>
        /// 重出派件公司名稱。
        /// </summary>
        public string ProcessTransName => ProcessTransNo?.ToDescription();
    }
}
