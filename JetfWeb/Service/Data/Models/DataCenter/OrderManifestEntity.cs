using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 訂單艙單資料。
    /// </summary>
    [Table("ORDER_MANIFEST", Schema = "dbo")]
    public sealed class OrderManifestEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 發送識別碼。
        /// </summary>
        [Column("SendId")]
        public string SendId { get; set; }

        /// <summary>
        /// 建立日期字串。
        /// </summary>
        [Column("CreateDate")]
        public string CreateDate { get; set; }

        /// <summary>
        /// 報關行代碼。
        /// </summary>
        [Column("BrokerCode")]
        public string BrokerCode { get; set; }

        /// <summary>
        /// 主提單號。
        /// </summary>
        [Column("MawbNo")]
        public string MawbNo { get; set; }

        /// <summary>
        /// 航班號碼。
        /// </summary>
        [Column("FlightNo")]
        public string FlightNo { get; set; }

        /// <summary>
        /// 進口日期字串。
        /// </summary>
        [Column("ImportDate")]
        public string ImportDate { get; set; }

        /// <summary>
        /// 申報日期字串。
        /// </summary>
        [Column("DeclDate")]
        public string DeclDate { get; set; }

        /// <summary>
        /// 幣別。
        /// </summary>
        [Column("Currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 起運港。
        /// </summary>
        [Column("OrigPort")]
        public string OrigPort { get; set; }

        /// <summary>
        /// 申報類型。
        /// </summary>
        [Column("DeclType")]
        public string DeclType { get; set; }

        /// <summary>
        /// 申報號碼。
        /// </summary>
        [Column("DeclNo")]
        public string DeclNo { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BagNo")]
        public string BagNo { get; set; }

        /// <summary>
        /// 袋重。
        /// </summary>
        [Column("BagWeight")]
        public string BagWeight { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("HawbNo")]
        public string HawbNo { get; set; }

        /// <summary>
        /// 交貨類型。
        /// </summary>
        [Column("DeliveryType")]
        public string DeliveryType { get; set; }

        /// <summary>
        /// 箱數。
        /// </summary>
        [Column("Ctns")]
        public string Ctns { get; set; }

        /// <summary>
        /// 箱件單位。
        /// </summary>
        [Column("CtnUnit")]
        public string CtnUnit { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        [Column("GrossWeight")]
        public string GrossWeight { get; set; }

        /// <summary>
        /// 淨重。
        /// </summary>
        [Column("NetWeight")]
        public string NetWeight { get; set; }

        /// <summary>
        /// 貿易條件。
        /// </summary>
        [Column("TermsSales")]
        public string TermsSales { get; set; }

        /// <summary>
        /// 運費。
        /// </summary>
        [Column("FreightAmt")]
        public string FreightAmt { get; set; }

        /// <summary>
        /// 免稅註記。
        /// </summary>
        [Column("DutyExemption")]
        public string DutyExemption { get; set; }

        /// <summary>
        /// 收件人稅號。
        /// </summary>
        [Column("CTaxNo")]
        public string CTaxNo { get; set; }

        /// <summary>
        /// 收件人名稱。
        /// </summary>
        [Column("CName")]
        public string CName { get; set; }

        /// <summary>
        /// 收件人地址。
        /// </summary>
        [Column("CAddr")]
        public string CAddr { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("CTel")]
        public string CTel { get; set; }

        /// <summary>
        /// 寄件人名稱。
        /// </summary>
        [Column("SName")]
        public string SName { get; set; }

        /// <summary>
        /// 寄件人地址。
        /// </summary>
        [Column("SAddr")]
        public string SAddr { get; set; }

        /// <summary>
        /// 項次。
        /// </summary>
        [Column("ItemNo")]
        public string ItemNo { get; set; }

        /// <summary>
        /// 賣家商品編號。
        /// </summary>
        [Column("VendorItemId")]
        public string VendorItemId { get; set; }

        /// <summary>
        /// 類別名稱。
        /// </summary>
        [Column("CategoryName")]
        public string CategoryName { get; set; }

        /// <summary>
        /// 貨品描述。
        /// </summary>
        [Column("GoodsDesc")]
        public string GoodsDesc { get; set; }

        /// <summary>
        /// 單價。
        /// </summary>
        [Column("Uprice")]
        public string UPrice { get; set; }

        /// <summary>
        /// 數量。
        /// </summary>
        [Column("Qty")]
        public string Qty { get; set; }

        /// <summary>
        /// 數量單位。
        /// </summary>
        [Column("QtyUnit")]
        public string QtyUnit { get; set; }

        /// <summary>
        /// 總價。
        /// </summary>
        [Column("TotalPrice")]
        public string TotalPrice { get; set; }

        /// <summary>
        /// 製造國別。
        /// </summary>
        [Column("MfrCountry")]
        public string MfrCountry { get; set; }

        /// <summary>
        /// 課稅方式。
        /// </summary>
        [Column("TaxMethod")]
        public string TaxMethod { get; set; }

        /// <summary>
        /// 貨品分類號列。
        /// </summary>
        [Column("CCCCode")]
        public string CccCode { get; set; }

        /// <summary>
        /// 輸入許可證一。
        /// </summary>
        [Column("LicenseNo1")]
        public string LicenseNo1 { get; set; }

        /// <summary>
        /// 輸入許可證二。
        /// </summary>
        [Column("LicenseNo2")]
        public string LicenseNo2 { get; set; }

        /// <summary>
        /// 輸入許可證三。
        /// </summary>
        [Column("LicenseNo3")]
        public string LicenseNo3 { get; set; }

        /// <summary>
        /// 品牌。
        /// </summary>
        [Column("Brand")]
        public string Brand { get; set; }

        /// <summary>
        /// 型號。
        /// </summary>
        [Column("Model")]
        public string Model { get; set; }

        /// <summary>
        /// 規格。
        /// </summary>
        [Column("Specification")]
        public string Specification { get; set; }

        /// <summary>
        /// 指定代碼。
        /// </summary>
        [Column("DesignatedCode")]
        public string DesignatedCode { get; set; }

        /// <summary>
        /// 編修時間。
        /// </summary>
        [Column("EditDateTime")]
        public DateTime? EditDateTime { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 母子併列分提單號。
        /// </summary>
        [Column("MainHawbNo")]
        public string MainHawbNo { get; set; }
    }
}