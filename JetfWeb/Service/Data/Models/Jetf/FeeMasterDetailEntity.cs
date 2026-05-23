using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 費用主檔明細資料。
    /// </summary>
    [Table("FEE_MASTER_DETAIL", Schema = "dbo")]
    public sealed class FeeMasterDetailEntity
    {
        /// <summary>
        /// 主鍵識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 對應費用主檔識別碼。
        /// </summary>
        [Column("FEE_MASTER_ID")]
        public int FeeMasterId { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 提單號。
        /// </summary>
        [Column("TRACKINGNO")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        [Column("CLEARANCE_NUMBER")]
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        [Column("TAX_NUMBER")]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        [Column("TAX_PAYER")]
        public string TaxPayer { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        [Column("TAX_RECID")]
        public string TaxRecId { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        [Column("DLV_INV")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        [Column("TAX_BASE")]
        public int? TaxBase { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        [Column("TAX")]
        public int? Tax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        [Column("CCFEE")]
        public int? Ccfee { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("COD")]
        public int? Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        [Column("FEE")]
        public int? Fee { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        [Column("RECIPIENT")]
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("RECPHONE")]
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        [Column("RECADDRESS")]
        public string RecAddress { get; set; }

        /// <summary>
        /// 應向物流代收金額。
        /// </summary>
        [Column("TO_DLV_COD")]
        public string ToDlvCod { get; set; }

        /// <summary>
        /// 轉由派件公司代收的稅額。
        /// </summary>
        [Column("TRANS_COD")]
        public int? TransCod { get; set; }
    }
}
