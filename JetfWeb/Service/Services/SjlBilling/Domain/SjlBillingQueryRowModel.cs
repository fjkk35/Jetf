using System;

namespace Service.Services.SjlBilling.Domain
{
    /// <summary>
    /// 捷利帳單原始查詢資料。
    /// </summary>
    public class SjlBillingQueryRowModel
    {
        /// <summary>
        /// 清關日期時間。
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 單據編號。
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 運送編號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 捷利上傳的單據編號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 其他費用。
        /// </summary>
        public decimal? OtherFee { get; set; }

        /// <summary>
        /// 代收金額。
        /// </summary>
        public decimal? Cod { get; set; }

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
        public decimal? Volume { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        public decimal? Gw { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string ImporterPhone { get; set; }
    }
}