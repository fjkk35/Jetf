using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// PLINK 錯誤代碼主檔。
    /// </summary>
    [Table("ETL_PLINK_ERROR_CODE", Schema = "dbo")]
    public sealed class EtlPlinkErrorCodeEntity
    {
        /// <summary>
        /// 備註代碼。
        /// </summary>
        [Key]
        [Column("REMARK", Order = 0)]
        public string Remark { get; set; }

        /// <summary>
        /// 錯誤原因代碼。
        /// </summary>
        [Key]
        [Column("REASON", Order = 1)]
        public string Reason { get; set; }
    }
}