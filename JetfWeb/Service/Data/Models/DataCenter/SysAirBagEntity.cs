using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 空運袋號主檔。
    /// </summary>
    [Table("SYS_AIR_BAG", Schema = "dbo")]
    public sealed class SysAirBagEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 袋別代碼。
        /// </summary>
        [Column("BAG_CODE")]
        public string BagCode { get; set; }

        /// <summary>
        /// 規則代碼一。
        /// </summary>
        [Column("CODE_1")]
        public string Code1 { get; set; }

        /// <summary>
        /// 規則代碼二。
        /// </summary>
        [Column("CODE_2")]
        public string Code2 { get; set; }

        /// <summary>
        /// 規則代碼三。
        /// </summary>
        [Column("CODE_3")]
        public string Code3 { get; set; }

        /// <summary>
        /// 規則代碼四。
        /// </summary>
        [Column("CODE_4")]
        public string Code4 { get; set; }

        /// <summary>
        /// 規則代碼五。
        /// </summary>
        [Column("CODE_5")]
        public string Code5 { get; set; }

        /// <summary>
        /// 規則代碼一啟用旗標。
        /// </summary>
        [Column("CODE_1_FLAG")]
        public string Code1Flag { get; set; }

        /// <summary>
        /// 規則代碼二啟用旗標。
        /// </summary>
        [Column("CODE_2_FLAG")]
        public string Code2Flag { get; set; }

        /// <summary>
        /// 規則代碼三啟用旗標。
        /// </summary>
        [Column("CODE_3_FLAG")]
        public string Code3Flag { get; set; }

        /// <summary>
        /// 規則代碼四啟用旗標。
        /// </summary>
        [Column("CODE_4_FLAG")]
        public string Code4Flag { get; set; }

        /// <summary>
        /// 規則代碼五啟用旗標。
        /// </summary>
        [Column("CODE_5_FLAG")]
        public string Code5Flag { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CUST_CODE")]
        public string CustCode { get; set; }

        /// <summary>
        /// 配對批次。
        /// </summary>
        [Column("MATCH_BATCH")]
        public int? MatchBatch { get; set; }

        /// <summary>
        /// 配對時間。
        /// </summary>
        [Column("MATCH_TIME")]
        public DateTime? MatchTime { get; set; }

        /// <summary>
        /// 使用旗標。
        /// </summary>
        [Column("USED_FLAG")]
        public string UsedFlag { get; set; }

        /// <summary>
        /// 配對年度。
        /// </summary>
        [Column("MATCH_YEAR")]
        public string MatchYear { get; set; }

        /// <summary>
        /// 使用狀態。
        /// </summary>
        [Column("USE_STATUS")]
        public string UseStatus { get; set; }
    }
}