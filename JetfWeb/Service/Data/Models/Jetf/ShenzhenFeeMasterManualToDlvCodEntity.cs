using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 新遞深圳物流代收金額人工調整。
    /// </summary>
    [Table("ShenzhenFeeMasterManualToDlvCod", Schema = "dbo")]
    public class ShenzhenFeeMasterManualToDlvCodEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        [Column("DlvInv")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 應向物流代收金額。
        /// </summary>
        [Column("ToDlvCod")]
        public int ToDlvCod { get; set; }

        /// <summary>
        /// 修改人員。
        /// </summary>
        [Column("ModifiedUser")]
        public string ModifiedUser { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("ModifiedTime")]
        public DateTime? ModifiedTime { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CreatedUser")]
        public string CreatedUser { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}