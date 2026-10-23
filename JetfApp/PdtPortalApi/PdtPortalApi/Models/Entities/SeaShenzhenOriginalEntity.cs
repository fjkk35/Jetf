using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 新遞深圳原始託運資料。
/// </summary>
[Table("SeaShenzhenOriginal", Schema = "dbo")]
public sealed class SeaShenzhenOriginalEntity
{
    /// <summary>
    /// 託運資料識別碼。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 資料日期。
    /// </summary>
    public DateTime DataDate { get; set; }

    /// <summary>
    /// 託運單號或條碼號。
    /// </summary>
    public string? JetfSerial { get; set; }

    /// <summary>
    /// 報關號碼。
    /// </summary>
    public string? TrackingNo { get; set; }

    /// <summary>
    /// 提單號碼。
    /// </summary>
    public string? BlNo { get; set; }

    /// <summary>
    /// 收件人姓名。
    /// </summary>
    public string? Importer { get; set; }

    /// <summary>
    /// 收件人地址。
    /// </summary>
    public string? ImporterAddress { get; set; }

    /// <summary>
    /// 收件人手機或電話。
    /// </summary>
    public string? ImporterPhone { get; set; }

    /// <summary>
    /// 寄件通路。
    /// </summary>
    public string? TransName { get; set; }
}
