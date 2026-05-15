using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 訂單匯入資訊。
    /// </summary>
    [Table("ETL_ORDER_INFO", Schema = "dbo")]
    public sealed class EtlOrderInfoEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 資料模式。
        /// </summary>
        [Column("MODEL")]
        public string Model { get; set; }

        /// <summary>
        /// 資料類型。
        /// </summary>
        [Column("TYPE")]
        public string Type { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CUST_CODE")]
        public string CustCode { get; set; }

        /// <summary>
        /// 訂單代碼。
        /// </summary>
        [Column("ORDER_CODE")]
        public string OrderCode { get; set; }

        /// <summary>
        /// 主提單號。
        /// </summary>
        [Column("MAWB_NO")]
        public string MawbNo { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("HAWB_NO")]
        public string HawbNo { get; set; }

        /// <summary>
        /// 申報類型。
        /// </summary>
        [Column("DECL_TYPE")]
        public string DeclType { get; set; }

        /// <summary>
        /// 配送類型。
        /// </summary>
        [Column("DELIVER_TYPE")]
        public string DeliverType { get; set; }

        /// <summary>
        /// 配送單號。
        /// </summary>
        [Column("DELIVER_NO")]
        public string DeliverNo { get; set; }

        /// <summary>
        /// 起運港。
        /// </summary>
        [Column("FROM_PORT")]
        public string FromPort { get; set; }

        /// <summary>
        /// 目的港。
        /// </summary>
        [Column("TO_PORT")]
        public string ToPort { get; set; }

        /// <summary>
        /// 稅金付款方式。
        /// </summary>
        [Column("TAX_PAYMENT")]
        public string TaxPayment { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAG_NO")]
        public string BagNo { get; set; }

        /// <summary>
        /// 件數。
        /// </summary>
        [Column("PIECE")]
        public int? Piece { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        [Column("WEIGHT")]
        public int? Weight { get; set; }

        /// <summary>
        /// 價格。
        /// </summary>
        [Column("PRICE")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("CC")]
        public int? Cc { get; set; }

        /// <summary>
        /// 出庫日期。
        /// </summary>
        [Column("OUTBOUND_DATE")]
        public DateTime? OutboundDate { get; set; }

        /// <summary>
        /// 到港日期。
        /// </summary>
        [Column("ARRIVE_DATE")]
        public DateTime? ArriveDate { get; set; }

        /// <summary>
        /// 航空公司或航班識別。
        /// </summary>
        [Column("AIR_ID")]
        public string AirId { get; set; }

        /// <summary>
        /// 航次。
        /// </summary>
        [Column("VOYAGE_NO")]
        public string VoyageNo { get; set; }

        /// <summary>
        /// 船舶呼號。
        /// </summary>
        [Column("VESSEL_CALLSIGN")]
        public string VesselCallsign { get; set; }

        /// <summary>
        /// 船公司。
        /// </summary>
        [Column("SHIPPING_COMPANY")]
        public string ShippingCompany { get; set; }

        /// <summary>
        /// 裝貨港。
        /// </summary>
        [Column("POL")]
        public string Pol { get; set; }

        /// <summary>
        /// IMO 編號。
        /// </summary>
        [Column("IMO")]
        public string Imo { get; set; }

        /// <summary>
        /// 貨櫃型式。
        /// </summary>
        [Column("CONT_TYPE")]
        public string ContType { get; set; }

        /// <summary>
        /// 貨櫃號。
        /// </summary>
        [Column("CONT_NO")]
        public string ContNo { get; set; }

        /// <summary>
        /// 貨櫃運輸模式。
        /// </summary>
        [Column("CONT_TRANS")]
        public string ContTrans { get; set; }

        /// <summary>
        /// 封條號碼。
        /// </summary>
        [Column("SEAL_NO")]
        public string SealNo { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFY_TIME")]
        public DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 業務類型。
        /// </summary>
        [Column("BIZ_TYPE")]
        public string BizType { get; set; }

        /// <summary>
        /// 門市代碼。
        /// </summary>
        [Column("STORE_ID")]
        public string StoreId { get; set; }

        /// <summary>
        /// 門市名稱。
        /// </summary>
        [Column("STORE_NAME")]
        public string StoreName { get; set; }
    }
}