using System.ComponentModel.DataAnnotations;

namespace TaxPortalApi.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpireMinutes { get; set; } = 30;
}