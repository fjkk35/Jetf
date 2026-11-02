using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 上傳拆櫃紀錄的主號拆櫃日期資料。
    /// </summary>
    [Table("SeaUnboxingRecord", Schema = "dbo")]
    public sealed class SeaUnboxingRecordEntity
    {
        /// <summary>
        /// 拆櫃紀錄流水號，由資料庫自動產生。
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 拆櫃日期，保留上傳紀錄的日期文字格式。
        /// </summary>
        [Column("DataDate")]
        [StringLength(20)]
        public string DataDate { get; set; }

        /// <summary>
        /// 主提單號碼。
        /// </summary>
        [Column("MainNumber")]
        [StringLength(50)]
        public string MainNumber { get; set; }

        /// <summary>
        /// 上傳人員。
        /// </summary>
        [Column("UploadOpe")]
        [StringLength(10)]
        public string UploadOpe { get; set; }

        /// <summary>
        /// 上傳時間。
        /// </summary>
        [Column("UploadTime")]
        public DateTime? UploadTime { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}
