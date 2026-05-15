using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 授權文件紀錄明細。
    /// </summary>
    [Table("SeaClearanceAuthorizationFormDetail", Schema = "dbo")]
    public sealed class SeaClearanceAuthorizationFormDetailEntity
    {
        /// <summary>
        /// 明細主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// SeaClearance 授權文件主鍵。
        /// </summary>
        [Column("SeaClearanceAuthorizationFormId")]
        public int? SeaClearanceAuthorizationFormId { get; set; }

        /// <summary>
        /// 授權文件主鍵。
        /// </summary>
        [Column("AuthorizationFormId")]
        public int? AuthorizationFormId { get; set; }
    }
}