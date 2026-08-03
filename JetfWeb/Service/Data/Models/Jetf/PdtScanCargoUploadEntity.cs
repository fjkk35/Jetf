using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 掃貨上傳資料。
    /// </summary>
    [Table("PdtScanCargoUpload", Schema = "dbo")]
    public sealed class PdtScanCargoUploadEntity
    {
        /// <summary>
        /// 主鍵識別碼。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 上傳時間。
        /// </summary>
        [Column("UploadTime")]
        public string UploadTime { get; set; }

        /// <summary>
        /// 客戶單號。
        /// </summary>
        [Column("Data")]
        public string Data { get; set; }

        /// <summary>
        /// 作業地區。
        /// </summary>
        [Column("DataType")]
        public string DataType { get; set; }

        /// <summary>
        /// 派件公司代碼。
        /// </summary>
        [Column("TransNo")]
        public string TransNo { get; set; }
    }
}
