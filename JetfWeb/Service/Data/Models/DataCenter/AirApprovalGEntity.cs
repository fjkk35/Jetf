using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 空運簡易申報資料。
    /// </summary>
    [Table("AIR_APPROVAL_G", Schema = "dbo")]
    public sealed class AirApprovalGEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 建立日期。
        /// </summary>
        [Column("CREAT_DATE")]
        public DateTime? CreatDate { get; set; }

        /// <summary>
        /// 公司名稱。
        /// </summary>
        [Column("COMPANY")]
        public string Company { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 申報號碼。
        /// </summary>
        [Column("DECL_NO")]
        public string DeclNo { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAGNO")]
        public string BagNo { get; set; }

        /// <summary>
        /// 倉庫代碼。
        /// </summary>
        [Column("WAREHOUSE")]
        public string Warehouse { get; set; }

        /// <summary>
        /// 航班號碼。
        /// </summary>
        [Column("FLIGHT_NO")]
        public string FlightNo { get; set; }

        /// <summary>
        /// 稅籍分支代碼。
        /// </summary>
        [Column("TAX_SUB_REG")]
        public string TaxSubReg { get; set; }

        /// <summary>
        /// 幣別。
        /// </summary>
        [Column("CURRENCY")]
        public string Currency { get; set; }

        /// <summary>
        /// 進口日期。
        /// </summary>
        [Column("IMPORT_DATE")]
        public DateTime? ImportDate { get; set; }

        /// <summary>
        /// 申報日期。
        /// </summary>
        [Column("DECL_DATE")]
        public DateTime? DeclDate { get; set; }

        /// <summary>
        /// 結關日期。
        /// </summary>
        [Column("CLOSING_DATE")]
        public DateTime? ClosingDate { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("HAWB_NO")]
        public string HawbNo { get; set; }

        /// <summary>
        /// 件數。
        /// </summary>
        [Column("PIECES")]
        public int? Pieces { get; set; }

        /// <summary>
        /// 箱件單位。
        /// </summary>
        [Column("CTN_UNIT")]
        public string CtnUnit { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        [Column("GW")]
        public decimal? Gw { get; set; }

        /// <summary>
        /// 淨重。
        /// </summary>
        [Column("NW")]
        public decimal? Nw { get; set; }

        /// <summary>
        /// 製造國別。
        /// </summary>
        [Column("MFR_COUNTRY")]
        public string MfrCountry { get; set; }

        /// <summary>
        /// 收件人證號一。
        /// </summary>
        [Column("RECIPIENTID")]
        public string RecipientId { get; set; }

        /// <summary>
        /// 收件人證號二。
        /// </summary>
        [Column("RECIPIENTID2")]
        public string RecipientId2 { get; set; }

        /// <summary>
        /// 收件人名稱。
        /// </summary>
        [Column("RECIPIENT")]
        public string Recipient { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        [Column("RECADDERSS")]
        public string RecAddress { get; set; }

        /// <summary>
        /// 英文收件地址。
        /// </summary>
        [Column("EN_RECADDERSS")]
        public string EnRecAddress { get; set; }

        /// <summary>
        /// 英文收件人名稱。
        /// </summary>
        [Column("EN_RECIPIENT")]
        public string EnRecipient { get; set; }

        /// <summary>
        /// 品名。
        /// </summary>
        [Column("ITEM")]
        public string Item { get; set; }

        /// <summary>
        /// 單價。
        /// </summary>
        [Column("UPRICE")]
        public int? UPrice { get; set; }

        /// <summary>
        /// 數量。
        /// </summary>
        [Column("QTY")]
        public string Qty { get; set; }

        /// <summary>
        /// 數量單位。
        /// </summary>
        [Column("QTY_UNIT")]
        public string QtyUnit { get; set; }

        /// <summary>
        /// 產地。
        /// </summary>
        [Column("MADEIN")]
        public string MadeIn { get; set; }

        /// <summary>
        /// 課稅方式。
        /// </summary>
        [Column("TAX_METHOD")]
        public string TaxMethod { get; set; }

        /// <summary>
        /// JETF 序號。
        /// </summary>
        [Column("JETF_SERIAL")]
        public string JetfSerial { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("RECPHONE")]
        public string RecPhone { get; set; }

        /// <summary>
        /// 貨物稅。
        /// </summary>
        [Column("EXCISE_TAX")]
        public int? ExciseTax { get; set; }

        /// <summary>
        /// 查驗類型。
        /// </summary>
        [Column("CHECK_TYPE")]
        public string CheckType { get; set; }

        /// <summary>
        /// 收件人名稱二。
        /// </summary>
        [Column("RECIPIENT2")]
        public string Recipient2 { get; set; }

        /// <summary>
        /// 金額。
        /// </summary>
        [Column("AMOUNT")]
        public int? Amount { get; set; }

        /// <summary>
        /// 運費。
        /// </summary>
        [Column("FREIGHTAMT")]
        public int? FreightAmt { get; set; }

        /// <summary>
        /// 貨品分類號列。
        /// </summary>
        [Column("CCCCODE")]
        public string CccCode { get; set; }

        /// <summary>
        /// 輸入許可證一。
        /// </summary>
        [Column("LICENSENO1")]
        public string LicenseNo1 { get; set; }

        /// <summary>
        /// 輸入許可證二。
        /// </summary>
        [Column("LICENSENO2")]
        public string LicenseNo2 { get; set; }

        /// <summary>
        /// 輸入許可證三。
        /// </summary>
        [Column("LICENSENO3")]
        public string LicenseNo3 { get; set; }

        /// <summary>
        /// 品牌。
        /// </summary>
        [Column("BRAND")]
        public string Brand { get; set; }

        /// <summary>
        /// 型號。
        /// </summary>
        [Column("MODEL")]
        public string Model { get; set; }

        /// <summary>
        /// 規格。
        /// </summary>
        [Column("SPECIFICATION")]
        public string Specification { get; set; }

        /// <summary>
        /// 指定代碼。
        /// </summary>
        [Column("DESIGNATED_CODE")]
        public string DesignatedCode { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFY_TIME")]
        public DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 項次序號。
        /// </summary>
        [Column("ITEM_NO")]
        public int? ItemNo { get; set; }
    }
}