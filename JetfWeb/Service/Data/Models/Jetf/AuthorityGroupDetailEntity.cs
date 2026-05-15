using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 權限群組與權限對應明細。
    /// </summary>
    [Table("AuthorityGroupDetail", Schema = "dbo")]
    public sealed class AuthorityGroupDetailEntity
    {
        /// <summary>
        /// 明細流水號。
        /// </summary>
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 權限群組主鍵。
        /// </summary>
        [Key]
        [Column("AuthorityGroupId", Order = 0)]
        public int AuthorityGroupId { get; set; }

        /// <summary>
        /// 權限識別碼。
        /// </summary>
        [Key]
        [Column("AuthorityId", Order = 1)]
        public string AuthorityId { get; set; }
    }
}