using Service.EnumTax;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 貨件回倉預先登記處理資料。
    /// </summary>
    [Table("ShipmentInboundProcessStage", Schema = "dbo")]
    public sealed class ShipmentInboundProcessStageEntity
    {
        /// <summary>
        /// 貨件回倉預先登記處理資料主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 貨件追蹤單號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

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
        /// 貨件處理方式代碼。
        /// </summary>
        [Column("ProcessType")]
        public ShipmentInboundProcessType? ProcessType { get; set; }

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
        /// 車牌號碼。
        /// </summary>
        [Column("CarNo")]
        public string CarNo { get; set; }

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
        /// 建立人員。
        /// </summary>
        [Column("CreatedOpe")]
        public string CreatedOpe { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}
