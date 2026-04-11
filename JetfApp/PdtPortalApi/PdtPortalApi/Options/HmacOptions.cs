namespace PdtPortalApi.Options;

public sealed class HmacOptions
{
    public const string SectionName = "Hmac";

    public string Secret { get; set; } = string.Empty;

    public int AllowedClockSkewMinutes { get; set; } = 5;
}