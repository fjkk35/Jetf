using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 新遞深圳金額人工調整。
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
        /// 託運單號或條碼號。
        /// </summary>
        [Column("DlvInv")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        [Column("Cod")]
        public int Cod { get; set; }

        /// <summary>
        /// 稅金金額。
        /// </summary>
        [Column("Tax")]
        public int Tax { get; set; }

        /// <summary>
        /// 稅金手續費。
        /// </summary>
        [Column("Fee")]
        public int Fee { get; set; }

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
