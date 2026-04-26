using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 貨件入庫資料。
    /// </summary>
    [Table("ShipmentInbound", Schema = "dbo")]
    public sealed class ShipmentInboundEntity
    {
        /// <summary>
        /// 貨件入庫資料主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 資料類型，例如海運或空運。
        /// </summary>
        [Column("DataType")]
        public string DataType { get; set; }

        /// <summary>
        /// 入庫日期。
        /// </summary>
        [Column("InboundDate")]
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// 貨件追蹤單號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 原始物流貨號。
        /// </summary>
        [Column("OriginalJetfSerial")]
        public string OriginalJetfSerial { get; set; }

        /// <summary>
        /// 原始追蹤單號。
        /// </summary>
        [Column("OriginalTrackingNo")]
        public string OriginalTrackingNo { get; set; }

        /// <summary>
        /// 流水編號。
        /// </summary>
        [Column("SeqNo")]
        public string SeqNo { get; set; }

        /// <summary>
        /// 目前儲位代碼。
        /// </summary>
        [Column("LocationCode")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 貨件來源代碼。
        /// </summary>
        [Column("SourceType")]
        public byte? SourceType { get; set; }

        /// <summary>
        /// 重出或退回時的新單號。
        /// </summary>
        [Column("ReturnTrackingNo")]
        public string ReturnTrackingNo { get; set; }

        /// <summary>
        /// 貨件尺寸資訊。
        /// </summary>
        [Column("Size")]
        public string Size { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CustCode")]
        public string CustCode { get; set; }

        /// <summary>
        /// 派件公司代碼。
        /// </summary>
        [Column("TransNo")]
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        [Column("TransName")]
        public string TransName { get; set; }

        /// <summary>
        /// 進口人或收件人姓名。
        /// </summary>
        [Column("Importer")]
        public string Importer { get; set; }

        /// <summary>
        /// 進口人或收件人電話。
        /// </summary>
        [Column("ImporterPhone")]
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 進口人或收件人地址。
        /// </summary>
        [Column("ImporterAddr")]
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        [Column("Tax")]
        public int? Tax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        [Column("Ccfee")]
        public int? Ccfee { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("Cod")]
        public int? Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        [Column("Fee")]
        public int? Fee { get; set; }

        /// <summary>
        /// 退件原因。
        /// </summary>
        [Column("ReturnReason")]
        public string ReturnReason { get; set; }

        /// <summary>
        /// 是否為原單轉入資料。
        /// </summary>
        [Column("IsOrderOriginal")]
        public bool IsOrderOriginal { get; set; }

        /// <summary>
        /// 上傳操作人員。
        /// </summary>
        [Column("UploadOpe")]
        public string UploadOpe { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 貨件處理方式代碼。
        /// </summary>
        [Column("ProcessType")]
        public byte? ProcessType { get; set; }

        /// <summary>
        /// 重出派件公司代碼。
        /// </summary>
        [Column("ProcessTransNo")]
        public byte? ProcessTransNo { get; set; }

        /// <summary>
        /// 處理後的收件人姓名。
        /// </summary>
        [Column("ProcessImporter")]
        public string ProcessImporter { get; set; }

        /// <summary>
        /// 處理後的收件人電話。
        /// </summary>
        [Column("ProcessImporterPhone")]
        public string ProcessImporterPhone { get; set; }

        /// <summary>
        /// 處理後的收件人地址。
        /// </summary>
        [Column("ProcessImporterAddr")]
        public string ProcessImporterAddr { get; set; }

        /// <summary>
        /// 門市店號。
        /// </summary>
        [Column("StoreCode")]
        public string StoreCode { get; set; }

        /// <summary>
        /// 門市名稱。
        /// </summary>
        [Column("StoreName")]
        public string StoreName { get; set; }

        /// <summary>
        /// 運費支付方代碼。
        /// </summary>
        [Column("FreightPayerNo")]
        public byte? FreightPayerNo { get; set; }

        /// <summary>
        /// 運費金額。
        /// </summary>
        [Column("FreightFee")]
        public int? FreightFee { get; set; }

        /// <summary>
        /// 處理費用。
        /// </summary>
        [Column("ProcessFee")]
        public int? ProcessFee { get; set; }

        /// <summary>
        /// 客服處理時間。
        /// </summary>
        [Column("ProcessTime")]
        public DateTime? ProcessTime { get; set; }

        /// <summary>
        /// 客服處理人員。
        /// </summary>
        [Column("ProcessOpe")]
        public string ProcessOpe { get; set; }

        /// <summary>
        /// 車牌號碼。
        /// </summary>
        [Column("CarNo")]
        public string CarNo { get; set; }

        /// <summary>
        /// 預計自取日期。
        /// </summary>
        [Column("PickupTime")]
        public DateTime? PickupTime { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("Remark")]
        public string Remark { get; set; }

        /// <summary>
        /// 出庫日期。
        /// </summary>
        [Column("OutboundDate")]
        public DateTime? OutboundDate { get; set; }

        /// <summary>
        /// 出庫操作時間。
        /// </summary>
        [Column("OutboundTime")]
        public DateTime? OutboundTime { get; set; }

        /// <summary>
        /// 出庫操作人員。
        /// </summary>
        [Column("OutboundOpe")]
        public string OutboundOpe { get; set; }

        /// <summary>
        /// 出庫單號。
        /// </summary>
        [Column("OutboundTrackingNo")]
        public string OutboundTrackingNo { get; set; }

        /// <summary>
        /// 倉庫處理狀態代碼。
        /// </summary>
        [Column("WarehouseProcessType")]
        public byte? WarehouseProcessType { get; set; }

        /// <summary>
        /// 倉庫處理時間。
        /// </summary>
        [Column("WarehouseProcessTime")]
        public DateTime? WarehouseProcessTime { get; set; }

        /// <summary>
        /// 倉庫處理人員。
        /// </summary>
        [Column("WarehouseProcessOpe")]
        public string WarehouseProcessOpe { get; set; }
    }
}
