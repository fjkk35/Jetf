using System;

namespace Service.Services.SjlBilling.Domain
{
    /// <summary>
    /// 捷利帳單匯出資料。
    /// </summary>
    public class SjlBillingExportRowModel
    {
        /// <summary>
        /// 清關日期時間。
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 運送編號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 單據編號。
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 代收金額。
        /// </summary>
        public decimal? Cod { get; set; }

        /// <summary>
        /// 其他費用。
        /// </summary>
        public decimal? OtherFee { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 品名。
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 件數。
        /// </summary>
        public int? Qty { get; set; }

        /// <summary>
        /// 材積。
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        public decimal Gw { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 基本運費。
        /// </summary>
        public decimal BaseFee { get; set; }

        /// <summary>
        /// 超才費。
        /// </summary>
        public decimal ExtraVolumeFee { get; set; }

        /// <summary>
        /// 最低收費調整前總額。
        /// </summary>
        public decimal SubtotalAmount { get; set; }

        /// <summary>
        /// 最低收費調整後總額。
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 超重費。
        /// </summary>
        public decimal OverweightFee { get; set; }

        /// <summary>
        /// 重量計費。
        /// </summary>
        public decimal WeightChargeAmount { get; set; }

        /// <summary>
        /// 應計價。
        /// </summary>
        public decimal ChargeAmount { get; set; }
    }
}