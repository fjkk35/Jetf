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
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        [Column("DATADATE")]
        public string DataDate { get; set; }

        [Column("SOURCE")]
        public string Source { get; set; }

        [Column("SOURCE_TYPE")]
        public string SourceType { get; set; }

        [Column("TYPE")]
        public string Type { get; set; }

        [Column("CUSTOMER")]
        public string Customer { get; set; }

        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        [Column("TRACKINGNO")]
        public string TrackingNo { get; set; }

        [Column("CLEARANCE_NUMBER")]
        public string ClearanceNumber { get; set; }

        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

        [Column("TAX_NUMBER")]
        public string TaxNumber { get; set; }

        [Column("DLV_INV")]
        public string DlvInv { get; set; }

        [Column("IN_DATE")]
        public string InDate { get; set; }

        [Column("IN_DATETIME")]
        public DateTime? InDateTime { get; set; }

        [Column("OUT_DATETIME")]
        public DateTime? OutDateTime { get; set; }

        [Column("COMBINE")]
        public string Combine { get; set; }

        [Column("TAX_BASE")]
        public string TaxBase { get; set; }

        [Column("TAX1")]
        public int? Tax1 { get; set; }

        [Column("TAX2")]
        public int? Tax2 { get; set; }

        [Column("CCFEE")]
        public int? Ccfee { get; set; }

        [Column("COD")]
        public int? Cod { get; set; }

        [Column("FEE")]
        public int? Fee { get; set; }

        [Column("INCLUDE_TAX")]
        public string IncludeTax { get; set; }

        [Column("RECIPIENT")]
        public string Recipient { get; set; }

        [Column("RECPHONE")]
        public string RecPhone { get; set; }

        [Column("RECADDRESS")]
        public string RecAddress { get; set; }

        [Column("RECID")]
        public string RecId { get; set; }

        [Column("TO_DLV_COD")]
        public int? ToDlvCod { get; set; }

        [Column("DLV_COM")]
        public string DlvCom { get; set; }

        [Column("DLV_COM_STN")]
        public string DlvComStn { get; set; }

        [Column("DLV_COD")]
        public int? DlvCod { get; set; }

        [Column("DLV_COD_CODE")]
        public string DlvCodCode { get; set; }

        [Column("DLV_COD_TIME")]
        public DateTime? DlvCodTime { get; set; }

        [Column("DLV_COD_OPE")]
        public string DlvCodOpe { get; set; }

        [Column("DLV_REMIT_DATE")]
        public string DlvRemitDate { get; set; }

        [Column("DLV_REMIT_AMOUT")]
        public decimal? DlvRemitAmout { get; set; }

        [Column("DLV_REMIT_AMOUT_FEE")]
        public decimal? DlvRemitAmoutFee { get; set; }

        [Column("DLV_REMIT_CODE")]
        public string DlvRemitCode { get; set; }

        [Column("DLV_REMIT_TIME")]
        public DateTime? DlvRemitTime { get; set; }

        [Column("DLV_REMIT_OPE")]
        public string DlvRemitOpe { get; set; }

        [Column("UPDATEDATE")]
        public DateTime? UpdateDate { get; set; }

        [Column("MODIFTYDATE")]
        public DateTime? ModiftyDate { get; set; }

        [Column("Download")]
        public string Download { get; set; }

        [Column("RECORD_FEE_MASTER")]
        public string RecordFeeMaster { get; set; }

        [Column("TAX_PAYER")]
        public string TaxPayer { get; set; }

        [Column("ARRIVAL")]
        public string Arrival { get; set; }

        [Column("CUSTOMER_COD")]
        public int? CustomerCod { get; set; }

        [Column("TRANS_COD")]
        public int? TransCod { get; set; }

        [Column("TAX_RECID")]
        public string TaxRecId { get; set; }
    }
}