using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        /// 毛重。
        /// </summary>
        [Column("GW")]
        public decimal? Gw { get; set; }
    }
}
