using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 清關進出倉資訊。
    /// </summary>
    [Table("CLEARANCE_INFO", Schema = "dbo")]
    public sealed class ClearanceInfoEntity
    {
        /// <summary>
        /// 資料流水號。
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
        /// 併袋號或合併單號。
        /// </summary>
        [Column("MERGE_NUMBER")]
        public string MergeNumber { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        [Column("CLEARANCE_NUMBER")]
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 清關類型。
        /// </summary>
        [Column("CLEARANCE_TYPE")]
        public string ClearanceType { get; set; }

        /// <summary>
        /// 貨件件數。
        /// </summary>
        [Column("CARGO_PIECE")]
        public int? CargoPiece { get; set; }

        /// <summary>
        /// 貨件重量。
        /// </summary>
        [Column("CARGO_WEIGHT")]
        public decimal? CargoWeight { get; set; }

        /// <summary>
        /// 清關模式。
        /// </summary>
        [Column("CLEARANCE_MODEL")]
        public string ClearanceModel { get; set; }

        /// <summary>
        /// 進倉時間。
        /// </summary>
        [Column("SIGN_IN_TIME")]
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        [Column("SIGN_OUT_TIME")]
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 稅額。
        /// </summary>
        [Column("TAX")]
        public int? Tax { get; set; }

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
        /// 修改來源檔名。
        /// </summary>
        [Column("MODIFY_FILE")]
        public string ModifyFile { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFY_TIME")]
        public DateTime ModifyTime { get; set; }

        /// <summary>
        /// 出倉件數。
        /// </summary>
        [Column("OUT_PIECE")]
        public int? OutPiece { get; set; }

        /// <summary>
        /// 進倉件數。
        /// </summary>
        [Column("SING_IN_PIECE")]
        public int? SignInPiece { get; set; }

        /// <summary>
        /// 記錄代碼。
        /// </summary>
        [Column("RECORD_CODE")]
        public string RecordCode { get; set; }
    }
}