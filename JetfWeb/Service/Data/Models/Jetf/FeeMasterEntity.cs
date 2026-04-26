using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 費用主檔資料。
    /// </summary>
    [Table("FEE_MASTER", Schema = "dbo")]
    public sealed class FeeMasterEntity
    {
        /// <summary>
        /// 追蹤單號。
        /// </summary>
        [Key]
        [Column("DLV_INV")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 下載註記。
        /// </summary>
        [Column("Download")]
        public string Download { get; set; }

        /// <summary>
        /// 是否含稅註記。
        /// </summary>
        [Column("INCLUDE_TAX")]
        public string IncludeTax { get; set; }

        /// <summary>
        /// 稅金欄位一。
        /// </summary>
        [Column("TAX1")]
        public int? Tax1 { get; set; }

        /// <summary>
        /// 稅金欄位二。
        /// </summary>
        [Column("TAX2")]
        public int? Tax2 { get; set; }

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
    }
}
