using Service.EnumTax;
using System;

namespace Service.Services.ShipmentInboundProcessStage.Domain
{
    /// <summary>
    /// 預先登記處理明細資料。
    /// </summary>
    public class ShipmentInboundProcessStageDetailModel
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
        /// 重出派件公司。
        /// </summary>
        public ShipmentInboundProcessTransNo? ProcessTransNo { get; set; }

        /// <summary>
        /// 處理收件人。
        /// </summary>
        public string ProcessImporter { get; set; }

        /// <summary>
        /// 處理收件人電話。
        /// </summary>
        public string ProcessImporterPhone { get; set; }

        /// <summary>
        /// 處理收件人地址。
        /// </summary>
        public string ProcessImporterAddr { get; set; }

        /// <summary>
        /// 門市店號。
        /// </summary>
        public string StoreCode { get; set; }

        /// <summary>
        /// 門市名稱。
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int? CcFee { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int? Cod { get; set; }

        /// <summary>
        /// 重出運費支付方。
        /// </summary>
        public ShipmentInboundFreightPayerNo? FreightPayerNo { get; set; }

        /// <summary>
        /// 重出運費。
        /// </summary>
        public int? FreightFee { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 車牌號碼。
        /// </summary>
        public string CarNo { get; set; }

        /// <summary>
        /// 預計自取日期。
        /// </summary>
        public DateTime? PickupTime { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Remark { get; set; }
    }
}
