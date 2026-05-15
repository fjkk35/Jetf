using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細與簽審類別對應。
    /// </summary>
    [Table("SeaClearanceDetailApprovalCategory", Schema = "dbo")]
    public sealed class SeaClearanceDetailApprovalCategoryEntity
    {
        /// <summary>
        /// 對應主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// SeaClearance 明細主鍵。
        /// </summary>
        [Column("SeaClearanceDetailId")]
        public int SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 簽審類別主鍵。
        /// </summary>
        [Column("ApprovalCategoryId")]
        public int ApprovalCategoryId { get; set; }
    }
}