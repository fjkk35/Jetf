using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 測試用費用主檔資料。
    /// </summary>
    [Table("FEE_MASTER_TEST", Schema = "dbo")]
    public sealed class FeeMasterTestEntity
    {
        /// <summary>
        /// 主鍵識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        [Column("DATADATE")]
        public string DataDate { get; set; }

        /// <summary>
        /// 資料來源。
        /// </summary>
        [Column("SOURCE")]
        public string Source { get; set; }

        /// <summary>
        /// 來源類型。
        /// </summary>
        [Column("SOURCE_TYPE")]
        public string SourceType { get; set; }

        /// <summary>
        /// 報關類型。
        /// </summary>
        [Column("TYPE")]
        public string Type { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Column("CUSTOMER")]
        public string Customer { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 提單號。
        /// </summary>
        [Column("TRACKINGNO")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        [Column("CLEARANCE_NUMBER")]
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        [Column("TAX_NUMBER")]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        [Column("DLV_INV")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 進倉日。
        /// </summary>
        [Column("IN_DATE")]
        public string InDate { get; set; }

        /// <summary>
        /// 進倉時間。
        /// </summary>
        [Column("IN_DATETIME")]
        public DateTime? InDateTime { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        [Column("OUT_DATETIME")]
        public DateTime? OutDateTime { get; set; }

        /// <summary>
        /// 是否併單。
        /// </summary>
        [Column("COMBINE")]
        public string Combine { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        [Column("TAX_BASE")]
        public int? TaxBase { get; set; }

        /// <summary>
        /// 第一筆稅金。
        /// </summary>
        [Column("TAX1")]
        public int? Tax1 { get; set; }

        /// <summary>
        /// 第二筆稅金。
        /// </summary>
        [Column("TAX2")]
        public int? Tax2 { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        [Column("CCFEE")]
        public int? Ccfee { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("COD")]
        public int? Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        [Column("FEE")]
        public int? Fee { get; set; }

        /// <summary>
        /// 是否包稅。
        /// </summary>
        [Column("INCLUDE_TAX")]
        public string IncludeTax { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        [Column("RECIPIENT")]
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("RECPHONE")]
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        [Column("RECADDRESS")]
        public string RecAddress { get; set; }

        /// <summary>
        /// 收件人證號。
        /// </summary>
        [Column("RECID")]
        public string RecId { get; set; }

        /// <summary>
        /// 應向物流代收金額。
        /// </summary>
        [Column("TO_DLV_COD")]
        public string ToDlvCod { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        [Column("DLV_COM")]
        public string DlvCom { get; set; }

        /// <summary>
        /// 派件站所。
        /// </summary>
        [Column("DLV_COM_STN")]
        public string DlvComStn { get; set; }

        /// <summary>
        /// 物流代收金額。
        /// </summary>
        [Column("DLV_COD")]
        public string DlvCod { get; set; }

        /// <summary>
        /// 物流代收代碼。
        /// </summary>
        [Column("DLV_COD_CODE")]
        public string DlvCodCode { get; set; }

        /// <summary>
        /// 物流代收時間。
        /// </summary>
        [Column("DLV_COD_TIME")]
        public DateTime? DlvCodTime { get; set; }

        /// <summary>
        /// 物流代收操作人。
        /// </summary>
        [Column("DLV_COD_OPE")]
        public string DlvCodOpe { get; set; }

        /// <summary>
        /// 匯款日期。
        /// </summary>
        [Column("DLV_REMIT_DATE")]
        public string DlvRemitDate { get; set; }

        /// <summary>
        /// 匯款金額。
        /// </summary>
        [Column("DLV_REMIT_AMOUT")]
        public string DlvRemitAmout { get; set; }

        /// <summary>
        /// 匯款手續費。
        /// </summary>
        [Column("DLV_REMIT_AMOUT_FEE")]
        public string DlvRemitAmoutFee { get; set; }

        /// <summary>
        /// 匯款代碼。
        /// </summary>
        [Column("DLV_REMIT_CODE")]
        public string DlvRemitCode { get; set; }

        /// <summary>
        /// 匯款時間。
        /// </summary>
        [Column("DLV_REMIT_TIME")]
        public DateTime? DlvRemitTime { get; set; }

        /// <summary>
        /// 匯款操作人。
        /// </summary>
        [Column("DLV_REMIT_OPE")]
        public string DlvRemitOpe { get; set; }

        /// <summary>
        /// 更新時間。
        /// </summary>
        [Column("UPDATEDATE")]
        public DateTime? UpdateDate { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFTYDATE")]
        public DateTime? ModiftyDate { get; set; }

        /// <summary>
        /// 下載註記。
        /// </summary>
        [Column("Download")]
        public string Download { get; set; }

        /// <summary>
        /// 記錄來源註記。
        /// </summary>
        [Column("RECORD_FEE_MASTER")]
        public string RecordFeeMaster { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        [Column("TAX_PAYER")]
        public string TaxPayer { get; set; }

        /// <summary>
        /// 到貨資訊。
        /// </summary>
        [Column("ARRIVAL")]
        public string Arrival { get; set; }

        /// <summary>
        /// 客戶代收金額。
        /// </summary>
        [Column("CUSTOMER_COD")]
        public int? CustomerCod { get; set; }

        /// <summary>
        /// 派件公司代收金額。
        /// </summary>
        [Column("TRANS_COD")]
        public int? TransCod { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        [Column("TAX_RECID")]
        public string TaxRecId { get; set; }
    }
}