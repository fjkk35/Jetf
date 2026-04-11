using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 使用者主檔實體。
/// </summary>
[Table("USER_MASTER", Schema = "dbo")]
public sealed class UserMasterEntity
{
    /// <summary>
    /// 使用者帳號。
    /// </summary>
    [Key]
    [Column("USER_ID")]
    public string UserId { get; set; } = string.Empty;
}