using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 異常狀態明細主檔。
    /// </summary>
    [Table("AbnormalStateDetail", Schema = "dbo")]
    public sealed class AbnormalStateDetailEntity
    {
        /// <summary>
        /// 異常狀態明細主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 所屬異常狀態主鍵。
        /// </summary>
        [Column("AbnormalStateId")]
        public int? AbnormalStateId { get; set; }

        /// <summary>
        /// 異常狀態明細名稱。
        /// </summary>
        [Column("AbnormalStateDetailName")]
        public string AbnormalStateDetailName { get; set; }

        /// <summary>
        /// 顯示排序。
        /// </summary>
        [Column("Sort")]
        public int? Sort { get; set; }
    }
}