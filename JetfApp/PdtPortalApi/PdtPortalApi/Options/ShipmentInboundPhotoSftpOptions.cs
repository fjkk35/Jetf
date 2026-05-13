namespace PdtPortalApi.Options;

public sealed class ShipmentInboundPhotoSftpOptions
{
    public const string SectionName = "ShipmentInboundPhotoSftp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 22;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string RootDirectory { get; set; } = "/SOURCE_DATA/ShipmentInbound";
}
