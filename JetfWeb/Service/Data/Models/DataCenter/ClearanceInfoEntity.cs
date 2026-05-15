using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 進出倉資訊。
    /// </summary>
    [Table("CLEARANCE_INFO", Schema = "dbo")]
    public sealed class ClearanceInfoEntity
    {
        /// <summary>
        /// 主號。
        /// </summary>
        [Key]
        [Column("MAIN_NUMBER", Order = 0)]
        public string MainNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Key]
        [Column("BAG_NUMBER", Order = 1)]
        public string BagNumber { get; set; }

        /// <summary>
        /// 資料類型。
        /// </summary>
        [Key]
        [Column("DATA_TYPE", Order = 2)]
        public string DataType { get; set; }

        /// <summary>
        /// 報關類型。
        /// </summary>
        [Key]
        [Column("CLEARANCE_TYPE", Order = 3)]
        public string ClearanceType { get; set; }

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
    }
}