using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 貨物艙單明細。
    /// </summary>
    [Table("ORDER_CARGO_MANIFEST", Schema = "dbo")]
    public sealed class OrderCargoManifestEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 收件對象。
        /// </summary>
        [Column("To")]
        public string To { get; set; }

        /// <summary>
        /// 報關行。
        /// </summary>
        [Column("Broker")]
        public string Broker { get; set; }

        /// <summary>
        /// 日期字串。
        /// </summary>
        [Column("Date")]
        public string Date { get; set; }

        /// <summary>
        /// 帳單代碼。
        /// </summary>
        [Column("BillingCode")]
        public string BillingCode { get; set; }

        /// <summary>
        /// 電話。
        /// </summary>
        [Column("Tel")]
        public string Tel { get; set; }

        /// <summary>
        /// 傳真。
        /// </summary>
        [Column("Fax")]
        public string Fax { get; set; }

        /// <summary>
        /// 航班號碼。
        /// </summary>
        [Column("FlightNo")]
        public string FlightNo { get; set; }

        /// <summary>
        /// 主提單號。
        /// </summary>
        [Column("MawbNo")]
        public string MawbNo { get; set; }

        /// <summary>
        /// 總件數。
        /// </summary>
        [Column("TotalCnt")]
        public string TotalCnt { get; set; }

        /// <summary>
        /// 總毛重。
        /// </summary>
        [Column("TotalGrossWeight")]
        public string TotalGrossWeight { get; set; }

        /// <summary>
        /// 項次。
        /// </summary>
        [Column("ItemNo")]
        public string ItemNo { get; set; }

        /// <summary>
        /// 主袋號。
        /// </summary>
        [Column("MasterBagNo")]
        public string MasterBagNo { get; set; }

        /// <summary>
        /// 箱數。
        /// </summary>
        [Column("Ctn")]
        public string Ctn { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        [Column("GrossWeight")]
        public string GrossWeight { get; set; }

        /// <summary>
        /// 貨品描述。
        /// </summary>
        [Column("Description")]
        public string Description { get; set; }

        /// <summary>
        /// 申報對象。
        /// </summary>
        [Column("DeclaredTo")]
        public string DeclaredTo { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("Remark")]
        public string Remark { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}