using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 使用新遞深圳託運資料的物流單號區間設定。
/// </summary>
[Table("SeaShenzhenTrackingNoRange", Schema = "dbo")]
public sealed class SeaShenzhenTrackingNoRangeEntity
{
    /// <summary>
    /// 區間設定識別碼。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 起始物流單號，包含此單號。
    /// </summary>
    public long StartTrackingNo { get; set; }

    /// <summary>
    /// 結束物流單號，包含此單號。
    /// </summary>
    public long EndTrackingNo { get; set; }
}
