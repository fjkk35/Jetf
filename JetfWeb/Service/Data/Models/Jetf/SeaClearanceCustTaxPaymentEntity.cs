using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 客戶稅金付款人設定。
    /// </summary>
    [Table("SeaClearanceCustTaxPayment", Schema = "dbo")]
    public sealed class SeaClearanceCustTaxPaymentEntity
    {
        /// <summary>
        /// 設定主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CustCode")]
        public string CustCode { get; set; }

        /// <summary>
        /// 稅金付款方式代碼。
        /// </summary>
        [Column("TaxPayment")]
        public string TaxPayment { get; set; }
    }
}