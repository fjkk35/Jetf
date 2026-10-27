using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// A03、B6F 上傳表的批次新增資料。
    /// </summary>
    [Table("A03_B6F_UPLOAD", Schema = "dbo")]
    public sealed class A03B6FUploadEntity
    {
        /// <summary>
        /// 倉儲類型。
        /// </summary>
        [Column("DATATYPE")]
        public string DataType { get; set; }

        /// <summary>
        /// 主提單號碼。
        /// </summary>
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// A03 袋號或 B6F 報單號碼。
        /// </summary>
        [Column("BL_NO")]
        public string BlNo { get; set; }

        /// <summary>
        /// 資料來源工作表，為 A03 或 B6F。
        /// </summary>
        [Column("REMARK")]
        public string Remark { get; set; }

        /// <summary>
        /// 合併袋數，未達填入條件時為空字串。
        /// </summary>
        [Column("BAGCOUNT")]
        public string BagCount { get; set; }

        /// <summary>
        /// 上傳人員代碼。
        /// </summary>
        [Column("UPLOAD_OPE")]
        public string UploadOpe { get; set; }
    }
}
