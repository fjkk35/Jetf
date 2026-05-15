using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 海運訂單編修資料。
    /// </summary>
    [Table("SEA_ORDER_EDIT", Schema = "dbo")]
    public sealed class SeaOrderEditEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 預計到港日。
        /// </summary>
        [Column("ETA")]
        public DateTime? Eta { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("BL_NO")]
        public string BlNo { get; set; }

        /// <summary>
        /// 艙單號。
        /// </summary>
        [Column("MANIFEST_NO")]
        public string ManifestNo { get; set; }

        /// <summary>
        /// JETF 識別碼。
        /// </summary>
        [Column("JETF_ID")]
        public string JetfId { get; set; }

        /// <summary>
        /// 貿易條件。
        /// </summary>
        [Column("TERMSOFPRICE")]
        public string TermsOfPrice { get; set; }

        /// <summary>
        /// 幣別。
        /// </summary>
        [Column("CURRENCY")]
        public string Currency { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        [Column("GW")]
        public decimal? Gw { get; set; }

        /// <summary>
        /// 件數。
        /// </summary>
        [Column("PIECE")]
        public int? Piece { get; set; }

        /// <summary>
        /// 件數單位。
        /// </summary>
        [Column("PIECE_UNIT")]
        public string PieceUnit { get; set; }

        /// <summary>
        /// 嘜頭。
        /// </summary>
        [Column("MARKS")]
        public string Marks { get; set; }

        /// <summary>
        /// 項次。
        /// </summary>
        [Column("ITEM_NO")]
        public string ItemNo { get; set; }

        /// <summary>
        /// 原始品名。
        /// </summary>
        [Column("ITEM_ONAME")]
        public string ItemOName { get; set; }

        /// <summary>
        /// 編修後品名。
        /// </summary>
        [Column("ITEM_NAME")]
        public string ItemName { get; set; }

        /// <summary>
        /// CCC Code。
        /// </summary>
        [Column("CCC_CODE")]
        public string CccCode { get; set; }

        /// <summary>
        /// 商標。
        /// </summary>
        [Column("TRADEMARK")]
        public string TradeMark { get; set; }

        /// <summary>
        /// 淨重。
        /// </summary>
        [Column("NW")]
        public decimal? Nw { get; set; }

        /// <summary>
        /// 數量。
        /// </summary>
        [Column("QUANTITY")]
        public int? Quantity { get; set; }

        /// <summary>
        /// 數量單位。
        /// </summary>
        [Column("QUANTITY_UNIT")]
        public string QuantityUnit { get; set; }

        /// <summary>
        /// 單價。
        /// </summary>
        [Column("UNIT_PRICE")]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// 發票金額。
        /// </summary>
        [Column("INVOICE_AMOUNT")]
        public decimal? InvoiceAmount { get; set; }

        /// <summary>
        /// 完稅價格。
        /// </summary>
        [Column("DUTY_PAYING")]
        public double? DutyPaying { get; set; }

        /// <summary>
        /// 體積。
        /// </summary>
        [Column("MEASUREMENT")]
        public double? Measurement { get; set; }

        /// <summary>
        /// 材積字串。
        /// </summary>
        [Column("CBM")]
        public string Cbm { get; set; }

        /// <summary>
        /// 產地。
        /// </summary>
        [Column("MADEIN")]
        public string MadeIn { get; set; }

        /// <summary>
        /// 出口商。
        /// </summary>
        [Column("EXPORTER")]
        public string Exporter { get; set; }

        /// <summary>
        /// 出口國別代碼。
        /// </summary>
        [Column("EX_COUNRTYCODE")]
        public string ExCountryCode { get; set; }

        /// <summary>
        /// 出口地址。
        /// </summary>
        [Column("EX_ADD")]
        public string ExAddress { get; set; }

        /// <summary>
        /// 申報對象識別碼。
        /// </summary>
        [Column("PARTY_IDENTIFIER")]
        public string PartyIdentifier { get; set; }

        /// <summary>
        /// 進口人證號。
        /// </summary>
        [Column("IMPORTER_ID")]
        public string ImporterId { get; set; }

        /// <summary>
        /// 進口人名稱。
        /// </summary>
        [Column("IMPORTER")]
        public string Importer { get; set; }

        /// <summary>
        /// 轉運日期。
        /// </summary>
        [Column("TRANS_DATE")]
        public string TransDate { get; set; }

        /// <summary>
        /// 轉運狀態。
        /// </summary>
        [Column("TRANS_STATUS")]
        public string TransStatus { get; set; }

        /// <summary>
        /// 更正證號。
        /// </summary>
        [Column("CORRECT_ID")]
        public string CorrectId { get; set; }

        /// <summary>
        /// 更正姓名。
        /// </summary>
        [Column("CORRECT_Name")]
        public string CorrectName { get; set; }

        /// <summary>
        /// 海關系統狀態。
        /// </summary>
        [Column("CS_STATUS")]
        public string CsStatus { get; set; }

        /// <summary>
        /// 進口人電話。
        /// </summary>
        [Column("IM_PHONENO")]
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 進口人地址。
        /// </summary>
        [Column("IM_ADD")]
        public string ImporterAddress { get; set; }

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
        [Column("CONT_TRANSMODEL")]
        public string ContTransModel { get; set; }

        /// <summary>
        /// 封條號碼。
        /// </summary>
        [Column("SEALNO")]
        public string SealNo { get; set; }

        /// <summary>
        /// 申報欄位一。
        /// </summary>
        [Column("DECLARATION_1")]
        public string Declaration1 { get; set; }

        /// <summary>
        /// 申報欄位二。
        /// </summary>
        [Column("DECLARATION_2")]
        public string Declaration2 { get; set; }

        /// <summary>
        /// 稅費申報內容。
        /// </summary>
        [Column("TAXFEE_DECLARED")]
        public string TaxFeeDeclared { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("DESPATCH_NAME")]
        public string DespatchName { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        [Column("TRANS_NAME")]
        public string TransName { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        [Column("JETF_SERIAL")]
        public string JetfSerial { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("CC")]
        public string Cc { get; set; }

        /// <summary>
        /// 補登資料。
        /// </summary>
        [Column("POST_ENTRY")]
        public string PostEntry { get; set; }

        /// <summary>
        /// JETF 發票金額。
        /// </summary>
        [Column("JETF_INVOICE")]
        public double? JetfInvoice { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("MEMO")]
        public string Memo { get; set; }

        /// <summary>
        /// 尺寸資訊。
        /// </summary>
        [Column("SIZE")]
        public string Size { get; set; }

        /// <summary>
        /// SIHNO。
        /// </summary>
        [Column("SIHNO")]
        public string SihNo { get; set; }

        /// <summary>
        /// LPNO。
        /// </summary>
        [Column("LPNO")]
        public string LpNo { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATEDATE")]
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CREATEBY")]
        public string CreateBy { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFTYDATE")]
        public DateTime? ModifyDate { get; set; }

        /// <summary>
        /// 修改人員。
        /// </summary>
        [Column("MODIFYBY")]
        public string ModifyBy { get; set; }

        /// <summary>
        /// 版本號。
        /// </summary>
        [Column("VERSION")]
        public int? Version { get; set; }

        /// <summary>
        /// 規格補充說明。
        /// </summary>
        [Column("II_SPEC")]
        public string IiSpec { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 資料資訊。
        /// </summary>
        [Column("DATA_INFO")]
        public string DataInfo { get; set; }

        /// <summary>
        /// 稅率。
        /// </summary>
        [Column("TAX_RATE")]
        public string TaxRate { get; set; }

        /// <summary>
        /// 稅則貨品名稱。
        /// </summary>
        [Column("TAX_GOODS")]
        public string TaxGoods { get; set; }

        /// <summary>
        /// 稅別代碼一。
        /// </summary>
        [Column("TAX_CODE1")]
        public string TaxCode1 { get; set; }

        /// <summary>
        /// 稅別代碼二。
        /// </summary>
        [Column("TAX_CODE2")]
        public string TaxCode2 { get; set; }

        /// <summary>
        /// 原始單價。
        /// </summary>
        [Column("UNIT_PRICE_O")]
        public decimal? UnitPriceOriginal { get; set; }

        /// <summary>
        /// 原始發票金額。
        /// </summary>
        [Column("INVOICE_AMOUNT_O")]
        public decimal? InvoiceAmountOriginal { get; set; }

        /// <summary>
        /// 原始完稅價格。
        /// </summary>
        [Column("DUTY_PAYING_O")]
        public decimal? DutyPayingOriginal { get; set; }

        /// <summary>
        /// 稅額一。
        /// </summary>
        [Column("TAX_FEE1")]
        public decimal? TaxFee1 { get; set; }

        /// <summary>
        /// 稅額二。
        /// </summary>
        [Column("TAX_FEE2")]
        public decimal? TaxFee2 { get; set; }

        /// <summary>
        /// 總稅額。
        /// </summary>
        [Column("TOTAL_TAX")]
        public decimal? TotalTax { get; set; }

        /// <summary>
        /// 核驗結果一。
        /// </summary>
        [Column("REAL_RESULT_1")]
        public string RealResult1 { get; set; }

        /// <summary>
        /// 核驗結果二。
        /// </summary>
        [Column("REAL_RESULT_2")]
        public string RealResult2 { get; set; }

        /// <summary>
        /// 核驗結果三。
        /// </summary>
        [Column("REAL_RESULT_3")]
        public string RealResult3 { get; set; }

        /// <summary>
        /// 核驗結果四。
        /// </summary>
        [Column("REAL_RESULT_4")]
        public string RealResult4 { get; set; }

        /// <summary>
        /// 是否已超出併單範圍。
        /// </summary>
        [Column("MERGE_OVER_FLAG")]
        public string MergeOverFlag { get; set; }
    }
}