using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 清關稅金資料。
    /// </summary>
    [Table("CLEARANCE_TAX", Schema = "dbo")]
    public sealed class ClearanceTaxEntity
    {
        /// <summary>
        /// 資料識別碼。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 資料類型。
        /// </summary>
        [Column("DATA_TYPE")]
        public string DataType { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

        /// <summary>
        /// 併袋號。
        /// </summary>
        [Column("MERGE_NUMBER")]
        public string MergeNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        [Column("TAX_NUMBER")]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        [Column("TAX_BASE")]
        public int? TaxBase { get; set; }

        /// <summary>
        /// 稅額。
        /// </summary>
        [Column("TAX_AMOUNT")]
        public int? TaxAmount { get; set; }

        /// <summary>
        /// 頻率註記。
        /// </summary>
        [Column("FREQ_SIGN")]
        public string FreqSign { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 修改序號。
        /// </summary>
        [Column("MODIFY_SEQ")]
        public int? ModifySeq { get; set; }

        /// <summary>
        /// 修改檔名。
        /// </summary>
        [Column("MODIFY_FILE")]
        public string ModifyFile { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFY_TIME")]
        public DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 貨品分類號列。
        /// </summary>
        [Column("CCC_CODE")]
        public string CccCode { get; set; }
    }
}