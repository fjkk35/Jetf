using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 關港貿單一窗口稅金資料。
    /// </summary>
    [Table("ETL_TIPC_TAX", Schema = "dbo")]
    public sealed class EtlTipcTaxEntity
    {
        /// <summary>
        /// 資料識別碼。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        [Column("TAX_NUMBER")]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅額。
        /// </summary>
        [Column("TAX_AMOUNT")]
        public string TaxAmount { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        [Column("TAX_BASE")]
        public string TaxBase { get; set; }

        /// <summary>
        /// 稅別代碼。
        /// </summary>
        [Column("TAX_CODE")]
        public string TaxCode { get; set; }

        /// <summary>
        /// 稅費。
        /// </summary>
        [Column("TAX_FEE")]
        public string TaxFee { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATE_TIME")]
        public string CreateTime { get; set; }

        /// <summary>
        /// 稅則號別。
        /// </summary>
        [Column("TARIFF_NO")]
        public string TariffNo { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        [Column("CLEARANCE_NUMBER")]
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        [Column("TAXPAYER_ID")]
        public string TaxpayerId { get; set; }

        /// <summary>
        /// 進口日期。
        /// </summary>
        [Column("IMPORT_DATE")]
        public string ImportDate { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 來源檔名。
        /// </summary>
        [Column("SOURCE_FILE")]
        public string SourceFile { get; set; }

        /// <summary>
        /// 來源時間。
        /// </summary>
        [Column("SOURCE_TIME")]
        public DateTime SourceTime { get; set; }
    }
}