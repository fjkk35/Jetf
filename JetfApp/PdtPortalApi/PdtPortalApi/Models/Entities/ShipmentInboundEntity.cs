using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 入庫資料實體。
/// </summary>
[PrimaryKey(nameof(TrackingNo), nameof(SeqNo), nameof(InboundDate))]
[Table("ShipmentInbound", Schema = "dbo")]
public sealed class ShipmentInboundEntity
{
	/// <summary>
	/// 資料型態（例如：「海運」、「空運」）。
	/// </summary>
	[Column("DataType")]
	public string DataType { get; set; } = string.Empty;

	/// <summary>
	/// 入庫日期。
	/// </summary>
	[Column("InboundDate")]
	public DateTime InboundDate { get; set; }

    /// <summary>
    /// 入庫資料 Id。
    /// </summary>
    [Column("Id")]
    public int Id { get; set; }

	/// <summary>
	/// 出庫日期。
	/// </summary>
	[Column("OutboundDate")]
	public DateTime? OutboundDate { get; set; }

	/// <summary>
	/// 單號。
	/// </summary>
	[Column("TrackingNo")]
	public string TrackingNo { get; set; } = string.Empty;

	/// <summary>
	/// 流水號。
	/// </summary>
	[Column("SeqNo")]
	public string SeqNo { get; set; } = string.Empty;

	/// <summary>
	/// 儲位。
	/// </summary>
	[Column("LocationCode")]
	public string LocationCode { get; set; } = string.Empty;

	/// <summary>
	/// 貨件來源。
	/// </summary>
	[Column("SourceType")]
	public byte SourceType { get; set; }

	/// <summary>
	/// 退回的追蹤編號（若為退貨或重出時使用）。
	/// </summary>
	[Column("ReturnTrackingNo")]
	public string ReturnTrackingNo { get; set; } = string.Empty;

	/// <summary>
	/// 尺寸。
	/// </summary>
	[Column("Size")]
	public string Size { get; set; } = string.Empty;

	/// <summary>
	/// 客戶代碼（CustCode），用於查詢客戶資料或對應客戶名稱。
	/// </summary>
	[Column("CustCode")]
	public string CustCode { get; set; } = string.Empty;

	/// <summary>
	/// 承運商代號（TransNo）。
	/// </summary>
	[Column("TransNo")]
	public string TransNo { get; set; } = string.Empty;

	/// <summary>
	/// 承運商名稱（可透過 TransNo 反查填入）。
	/// </summary>
	[Column("TransName")]
	public string TransName { get; set; } = string.Empty;

	/// <summary>
	/// 進口人姓名或收件人名稱。
	/// </summary>
	[Column("Importer")]
	public string Importer { get; set; } = string.Empty;

	/// <summary>
	/// 進口人或收件人電話。
	/// </summary>
	[Column("ImporterPhone")]
	public string ImporterPhone { get; set; } = string.Empty;

	/// <summary>
	/// 進口人或收件人地址。
	/// </summary>
	[Column("ImporterAddr")]
	public string ImporterAddr { get; set; } = string.Empty;

    /// <summary>
    /// 出庫時間
    /// </summary>
    [Column("OutboundTime")]
    public DateTime? OutboundTime { get; set; }

    /// <summary>
    /// 是否有原單資料。
    /// </summary>
    [Column("IsOrderOriginal")]
	public bool IsOrderOriginal { get; set; }

	/// <summary>
	/// 上傳操作人員帳號或識別。
	/// </summary>
	[Column("UploadOpe")]
	public string UploadOpe { get; set; } = string.Empty;

	/// <summary>
	/// 建立時間（紀錄匯入或建立的時間）。
	/// </summary>
	[Column("CreatedTime")]
	public DateTime CreatedTime { get; set; }

	/// <summary>
	/// 稅金。
	/// </summary>
	[Column("Tax")]
	public int Tax { get; set; }

	/// <summary>
	/// 報關費。
	/// </summary>
	[Column("Ccfee")]
	public int Ccfee { get; set; }

	/// <summary>
	/// 到付款。
	/// </summary>
	[Column("Cod")]
	public int Cod { get; set; }

	/// <summary>
	/// 手續費。
	/// </summary>
	[Column("Fee")]
	public int Fee { get; set; }
}
