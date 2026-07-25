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
        [ForeignKey(nameof(FeeMaster))]
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

        /// <summary>
        /// 客戶吸收稅額。
        /// </summary>
        [Column("CUSTOMER_COD")]
        public int? CustomerCod { get; set; }

        /// <summary>
        /// 已向客戶收回的代收金額。
        /// </summary>
        [Column("RECEIVED_CUSTOMER_COD")]
        public int? ReceivedCustomerCod { get; set; }

        /// <summary>
        /// 向客戶收回代收金額的時間。
        /// </summary>
        [Column("RECEIVED_CUSTOMER_COD_TIME")]
        public System.DateTime? ReceivedCustomerCodTime { get; set; }

        /// <summary>
        /// 客戶銷帳操作人員。
        /// </summary>
        [StringLength(10)]
        [Column("RECEIVED_CUSTOMER_COD_USERID")]
        public string ReceivedCustomerCodUserId { get; set; }

        /// <summary>
        /// 已向物流公司收回的應收金額。
        /// </summary>
        [Column("RECEIVED_TO_DLV_COD")]
        public int? ReceivedToDlvCod { get; set; }

        /// <summary>
        /// 向物流公司收回應收金額的時間。
        /// </summary>
        [Column("RECEIVED_TO_DLV_COD_TIME")]
        public System.DateTime? ReceivedToDlvCodTime { get; set; }

        /// <summary>
        /// 物流銷帳操作人員。
        /// </summary>
        [StringLength(10)]
        [Column("RECEIVED_TO_DLV_COD_USERID")]
        public string ReceivedToDlvCodUserId { get; set; }

        /// <summary>
        /// 對應的物流銷帳紀錄識別碼。
        /// </summary>
        [Column("ReconciliationLogisticsId")]
        [ForeignKey(nameof(ReconciliationLogistics))]
        public int? ReconciliationLogisticsId { get; set; }

        /// <summary>
        /// 對應的費用主檔。
        /// </summary>
        public FeeMasterEntity FeeMaster { get; set; }

        /// <summary>
        /// 對應的物流銷帳紀錄。
        /// </summary>
        public ReconciliationLogisticsEntity ReconciliationLogistics { get; set; }
    }
}
