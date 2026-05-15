using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 授權文件紀錄主檔。
    /// </summary>
    [Table("SeaClearanceAuthorizationForm", Schema = "dbo")]
    public sealed class SeaClearanceAuthorizationFormEntity
    {
        /// <summary>
        /// 授權文件紀錄主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        [Column("DataDate")]
        public DateTime? DataDate { get; set; }

        /// <summary>
        /// 授權文件類型。
        /// </summary>
        [Column("Type")]
        public byte? Type { get; set; }

        /// <summary>
        /// SeaClearance 明細主鍵。
        /// </summary>
        [Column("SeaClearanceDetailId")]
        public int? SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CrtUser")]
        public string CrtUser { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}