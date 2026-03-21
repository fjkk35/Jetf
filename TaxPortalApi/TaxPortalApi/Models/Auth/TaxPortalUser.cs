using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.Auth;

[Table("TaxPortalUser", Schema = "dbo")]
public class TaxPortalUser
{
    /// <summary>
    /// 使用者識別碼。
    /// </summary>
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Required]
    [Column("UserName")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [Column("Password")]
    public string Password { get; set; } = string.Empty;
}