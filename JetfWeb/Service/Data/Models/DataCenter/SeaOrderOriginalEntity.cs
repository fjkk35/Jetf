using System;
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
        /// 品名。
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
        public string Trademark { get; set; }

        /// <summary>
        /// 規格補充說明。
        /// </summary>
        [Column("II_SPEC")]
        public string IiSpec { get; set; }

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
        public decimal? DutyPaying { get; set; }

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
        /// 出口商地址。
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
        /// 進口人地址簡化版。
        /// </summary>
        [Column("IM_ADD_S")]
        public string ImporterAddressShort { get; set; }

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
        /// 客戶代碼或客戶名稱代碼。
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
        public double? Cc { get; set; }

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
        /// 尺寸說明。
        /// </summary>
        [Column("SIZE")]
        public string Size { get; set; }

        /// <summary>
        /// 合計。
        /// </summary>
        [Column("TOTAL")]
        public double? Total { get; set; }

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
        /// 額外欄位。
        /// </summary>
        [Column("EXTRA1")]
        public string Extra1 { get; set; }

        /// <summary>
        /// 到貨資訊。
        /// </summary>
        [Column("ARRIVAL")]
        public string Arrival { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

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
        /// 進口人名稱簡化版。
        /// </summary>
        [Column("IMPORTER_S")]
        public string ImporterShort { get; set; }

        /// <summary>
        /// 是否轉運。
        /// </summary>
        [Column("IS_TRANSFER")]
        public string IsTransfer { get; set; }

        /// <summary>
        /// 出口商地址簡化版。
        /// </summary>
        [Column("EX_ADD_S")]
        public string ExAddressShort { get; set; }

        /// <summary>
        /// 進口人電話簡化版。
        /// </summary>
        [Column("IM_PHONENO_S")]
        public string ImporterPhoneShort { get; set; }

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
        /// 品名簡化版。
        /// </summary>
        [Column("ITEM_NAME_S")]
        public string ItemNameShort { get; set; }

        /// <summary>
        /// 稅率代碼一。
        /// </summary>
        [Column("TAX1_CODE")]
        public string Tax1Code { get; set; }

        /// <summary>
        /// 稅率代碼二。
        /// </summary>
        [Column("TAX2_CODE")]
        public string Tax2Code { get; set; }

        /// <summary>
        /// 稅率。
        /// </summary>
        [Column("TAX_RATE")]
        public string TaxRate { get; set; }

        /// <summary>
        /// 稅額。
        /// </summary>
        [Column("TAX")]
        public decimal? Tax { get; set; }

        /// <summary>
        /// 是否重複。
        /// </summary>
        [Column("IS_REPEAT")]
        public string IsRepeat { get; set; }

        /// <summary>
        /// 毛重檢核結果。
        /// </summary>
        [Column("GW_CHECK")]
        public string GwCheck { get; set; }

        /// <summary>
        /// 件數檢核結果。
        /// </summary>
        [Column("PIECE_CHECK")]
        public string PieceCheck { get; set; }

        /// <summary>
        /// 淨重檢核結果。
        /// </summary>
        [Column("NW_CHECK")]
        public string NwCheck { get; set; }

        /// <summary>
        /// 數量檢核結果。
        /// </summary>
        [Column("QUANTITY_CHECK")]
        public string QuantityCheck { get; set; }

        /// <summary>
        /// 完稅價格檢核結果。
        /// </summary>
        [Column("PAYING_CHECK")]
        public string PayingCheck { get; set; }

        /// <summary>
        /// 是否含稅。
        /// </summary>
        [Column("INCLUDE_TAX")]
        public string IncludeTax { get; set; }

        /// <summary>
        /// 進口人證號簡化版。
        /// </summary>
        [Column("IMPORTER_ID_S")]
        public string ImporterIdShort { get; set; }

        /// <summary>
        /// 稅則貨品名稱。
        /// </summary>
        [Column("TAX_GOODS")]
        public string TaxGoods { get; set; }

        /// <summary>
        /// 品名中譯版。
        /// </summary>
        [Column("ITEM_NAME_M")]
        public string ItemNameMedium { get; set; }

        /// <summary>
        /// 身分證檢核結果。
        /// </summary>
        [Column("ID_CHECK")]
        public string IdCheck { get; set; }

        /// <summary>
        /// 稅金付款方式。
        /// </summary>
        [Column("TAX_PAYMENT")]
        public string TaxPayment { get; set; }

        /// <summary>
        /// 轉換後稅金付款方式。
        /// </summary>
        [Column("TRANS_TAXPAYMENT")]
        public string TransTaxPayment { get; set; }

        /// <summary>
        /// 業務模組。
        /// </summary>
        [Column("BIZ_MODULE")]
        public string BizModule { get; set; }

        /// <summary>
        /// 是否已超出併單範圍。
        /// </summary>
        [Column("MERGE_OVER_FLAG")]
        public string MergeOverFlag { get; set; }

    }
}
