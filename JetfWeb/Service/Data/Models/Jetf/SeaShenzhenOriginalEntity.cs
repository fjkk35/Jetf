using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Service.EnumTax;

namespace Service.Data
{
    /// <summary>
    /// 新遞深圳原始託運資料。
    /// </summary>
    [Table("SeaShenzhenOriginal", Schema = "dbo")]
    public class SeaShenzhenOriginalEntity
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        [Column("DataDate")]
        public DateTime DataDate { get; set; }

        /// <summary>
        /// 報關行。
        /// </summary>
        [Column("DataType")]
        public SeaShenzhenTaxDataType DataType { get; set; }

        /// <summary>
        /// 報關號碼。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 提單號碼。
        /// </summary>
        [Column("BlNo")]
        public string BlNo { get; set; }

        /// <summary>
        /// 訂單編號。
        /// </summary>
        [Column("OrderNo")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 託運單號或條碼號。
        /// </summary>
        [Column("JetfSerial")]
        public string JetfSerial { get; set; }

        /// <summary>
        /// 廠商交易時間。
        /// </summary>
        [Column("TransTime")]
        public DateTime? TransTime { get; set; }

        /// <summary>
        /// 寄件通路。
        /// </summary>
        [Column("TransName")]
        public string TransName { get; set; }

        /// <summary>
        /// 收件人姓名。
        /// </summary>
        [Column("Importer")]
        public string Importer { get; set; }

        /// <summary>
        /// 收件門市代碼或收件地址，含配送備註。
        /// </summary>
        [Column("ImporterAddress")]
        public string ImporterAddress { get; set; }

        /// <summary>
        /// 收件人手機或電話。
        /// </summary>
        [Column("ImporterPhone")]
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 商品名稱。
        /// </summary>
        [Column("ItemName")]
        public string ItemName { get; set; }

        /// <summary>
        /// 代收金額。
        /// </summary>
        [Column("Cc")]
        public double? Cc { get; set; }

        /// <summary>
        /// 數量。
        /// </summary>
        [Column("Quantity")]
        public int? Quantity { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        [Column("Gw")]
        public decimal Gw { get; set; }

        /// <summary>
        /// 傳給物流重量。
        /// </summary>
        [Column("DlvGw")]
        public decimal DlvGw { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("Memo")]
        public string Memo { get; set; }

        /// <summary>
        /// 認領人。
        /// </summary>
        [Column("Claimant")]
        public string Claimant { get; set; }

        /// <summary>
        /// 稅金付款方式。
        /// </summary>
        [Column("TaxPayment")]
        public string TaxPayment { get; set; }

        /// <summary>
        /// 是否是新竹物流的託運資料。
        /// </summary>
        [Column("IsHct")]
        public bool IsHct { get; set; }

        /// <summary>
        /// 託運單是否傳送成功。
        /// </summary>
        [Column("IsHctSuccess")]
        public bool IsHctSuccess { get; set; }

        /// <summary>
        /// 修改人員。
        /// </summary>
        [Column("ModifiedUser")]
        public string ModifiedUser { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("ModifiedTime")]
        public DateTime? ModifiedTime { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CreatedUser")]
        public string CreatedUser { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}
