using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Service.Data
{
    /// <summary>
    /// DATA_CENTER 海運原始訂單資料。
    /// </summary>
    [Table("SEA_ORDER_ORIGINAL", Schema = "dbo")]
    public sealed class SeaOrderOriginalEntity
    {
        [Key]
        [Column("ROW_ID")]
        public int Id { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        [Column("BL_NO")]
        public string BlNo { get; set; }

        /// <summary>
        /// 物流貨號
        /// </summary>
        [Column("JETF_SERIAL")]
        public string JetfSerial { get; set; }

        /// <summary>
        /// 進口人證號。
        /// </summary>
        [Column("IMPORTER_ID")]
        public string ImporterId { get; set; }

        /// <summary>
        /// 進口人地址。
        /// </summary>
        [Column("IM_ADD")]
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 進口人電話。
        /// </summary>
        [Column("IM_PHONENO")]
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 進口人姓名。
        /// </summary>
        [Column("IMPORTER")]
        public string Importer { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("DESPATCH_NAME")]
        public string CustCode { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        [Column("TRANS_NAME")]
        public string TransName { get; set; }

        /// <summary>
        /// 稅金派件公司。
        /// </summary>
        [Column("TRANS_TAXPAYMENT")]
        public string TransTaxPayment { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        [Column("GW")]
        public decimal? Gw { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        [Column("CC")]
        public decimal? CC { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("MEMO")]
        public string Memo { get; set; }

        /// <summary>
        /// 到貨資訊。
        /// </summary>
        [Column("ARRIVAL")]
        public string Arrival { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFTYDATE")]
        public DateTime? ModifyDate { get; set; }

    }
}
