using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 新遞深圳稅金轉檔主檔。
    /// </summary>
    [Table("ShenzhenFeeMaster", Schema = "dbo")]
    public sealed class ShenzhenFeeMasterEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("FeeMasterId")]
        public int FeeMasterId { get; set; }

        [Column("DataDate")]
        public string DataDate { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Column("CUSTOMER")]
        public string Customer { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        [Column("DlvInv")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        [Column("Tax")]
        public int Tax { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("Cod")]
        public int Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        [Column("Fee")]
        public int Fee { get; set; }

        /// <summary>
        /// 稅金支付方式。
        /// </summary>
        [Column("IncludeTax")]
        public string IncludeTax { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        [Column("DlvCom")]
        public string DlvCom { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        [Column("Recipient")]
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("RecPhone")]
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        [Column("RecAddress")]
        public string RecAddress { get; set; }

        /// <summary>
        /// 應向物流代收金額。
        /// </summary>
        [Column("ToDlvCod")]
        public int ToDlvCod { get; set; }

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
