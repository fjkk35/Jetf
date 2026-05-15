using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 掃貨入庫時間查詢紀錄。
    /// </summary>
    [Table("ScanCargoArrivalTime", Schema = "dbo")]
    public sealed class ScanCargoArrivalTimeEntity
    {
        /// <summary>
        /// 查詢紀錄主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 對應掃貨上車資料主鍵。
        /// </summary>
        [Column("PdtScanCargoUploadId")]
        public int? PdtScanCargoUploadId { get; set; }

        /// <summary>
        /// 入庫時間文字。
        /// </summary>
        [Column("ArrivalTime")]
        public string ArrivalTime { get; set; }

        /// <summary>
        /// 掃讀上傳時間文字。
        /// </summary>
        [Column("UploadTime")]
        public string UploadTime { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        [Column("TransName")]
        public string TransName { get; set; }

        /// <summary>
        /// 查詢時間文字。
        /// </summary>
        [Column("SearchTime")]
        public string SearchTime { get; set; }

        /// <summary>
        /// 查詢人員。
        /// </summary>
        [Column("SearchOpe")]
        public string SearchOpe { get; set; }
    }
}