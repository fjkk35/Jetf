using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SJL 進口人稅金付款人設定。
    /// </summary>
    [Table("SeaClearanceSjlTaxPayment", Schema = "dbo")]
    public sealed class SeaClearanceSjlTaxPaymentEntity
    {
        /// <summary>
        /// 設定主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 進口人名稱。
        /// </summary>
        [Column("Importer")]
        public string Importer { get; set; }

        /// <summary>
        /// 稅金付款方式代碼。
        /// </summary>
        [Column("TaxPayment")]
        public string TaxPayment { get; set; }
    }
}