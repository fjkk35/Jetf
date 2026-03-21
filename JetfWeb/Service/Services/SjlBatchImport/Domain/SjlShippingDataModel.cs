using System;

namespace Service.Services.SjlBatchImport.Domain
{
    /// <summary>
    /// 捷利托運資料查詢結果。
    /// </summary>
    public class SjlShippingDataModel
    {
        /// <summary>
        /// 運送編號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 單據編號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 編號。
        /// </summary>
        public string Seq { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 派送日。
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// 其他費用。
        /// </summary>
        public decimal? OtherFee { get; set; }

        /// <summary>
        /// 代收。
        /// </summary>
        public decimal? Cod { get; set; }

        /// <summary>
        /// 地址。
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

        /// <summary>
        /// 修改人員。
        /// </summary>
        public string UpdatedOpe { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 上傳人員。
        /// </summary>
        public string CreatedOpe { get; set; }

        /// <summary>
        /// 上傳時間。
        /// </summary>
        public DateTime? CreatedTime { get; set; }
    }
}
