using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 海快艙單上傳資料，以主提單及分提單識別。
    /// </summary>
    [Table("SEA_MANIFEST_UPLOAD", Schema = "dbo")]
    public sealed class SeaManifestUploadEntity
    {
        /// <summary>
        /// 主提單號碼。
        /// </summary>
        [Key]
        [Column("MAINNUMBER", Order = 0)]
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號碼。
        /// </summary>
        [Key]
        [Column("BL_NO", Order = 1)]
        public string BlNo { get; set; }

        /// <summary>
        /// 艙單裝貨型態。
        /// </summary>
        [Column("MANIFEST")]
        public string Manifest { get; set; }
    }
}
