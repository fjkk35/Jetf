using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// DATA_CENTER 原始貨件清單資料。
    /// </summary>
    [Table("ORIGINALLIST", Schema = "dbo")]
    public sealed class OriginalListEntity
    {
        /// <summary>
        /// 資料主鍵。
        /// </summary>
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAGNO")]
        public string BagNo { get; set; }

        /// <summary>
        /// 袋重。
        /// </summary>
        [Column("BAGWEIGHT")]
        public decimal? BagWeight { get; set; }

        /// <summary>
        /// 分提單號或追蹤單號。
        /// </summary>
        [Column("TRACKINGNO")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 件數。
        /// </summary>
        [Column("PIECES")]
        public int? Pieces { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        [Column("WEIGHT")]
        public decimal? Weight { get; set; }

        /// <summary>
        /// 品名。
        /// </summary>
        [Column("ITEMS")]
        public string Items { get; set; }

        /// <summary>
        /// 數量。
        /// </summary>
        [Column("QUANTITY")]
        public string Quantity { get; set; }

        /// <summary>
        /// 數量單位。
        /// </summary>
        [Column("UNIT")]
        public string Unit { get; set; }

        /// <summary>
        /// 原產地。
        /// </summary>
        [Column("ORIGIN")]
        public string Origin { get; set; }

        /// <summary>
        /// 單價。
        /// </summary>
        [Column("UNITPRICE")]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// 寄件公司。
        /// </summary>
        [Column("SENDCOMPANY")]
        public string SendCompany { get; set; }

        /// <summary>
        /// 寄件人。
        /// </summary>
        [Column("SENDER")]
        public string Sender { get; set; }

        /// <summary>
        /// 清關倉別或派件倉別代碼。
        /// </summary>
        [Column("CLEARANCEWAREHOUSING")]
        public int? ClearanceWarehousing { get; set; }

        /// <summary>
        /// 派件商或承攬代碼。
        /// </summary>
        [Column("DISPATCHER")]
        public string Dispatcher { get; set; }

        /// <summary>
        /// 收件人名稱。
        /// </summary>
        [Column("RECIPIENT")]
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("RECPHONE")]
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        [Column("RECADDRESS")]
        public string RecAddress { get; set; }

        /// <summary>
        /// 收件人證號。
        /// </summary>
        [Column("RECID")]
        public string RecId { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("CC")]
        public string Cc { get; set; }

        /// <summary>
        /// 尾段資訊或末端別。
        /// </summary>
        [Column("FINALPART")]
        public string FinalPart { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("REMARK")]
        public string Remark { get; set; }

        /// <summary>
        /// LP 代碼。
        /// </summary>
        [Column("LP_CODE")]
        public string LpCode { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("DESPATCHNO")]
        public string DespatchNo { get; set; }

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
        /// 進倉時間。
        /// </summary>
        [Column("SIGN_IN_TIME")]
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        [Column("SIGN_OUT_TIME")]
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 追蹤上層袋號或追蹤 UB 欄位。
        /// </summary>
        [Column("TRACKINGUB")]
        public string TrackingUb { get; set; }

        /// <summary>
        /// 原始主號。
        /// </summary>
        [Column("ORIGINAL_MAIN")]
        public string OriginalMain { get; set; }

        /// <summary>
        /// 發票金額。
        /// </summary>
        [Column("INVOICE_AMOUNT")]
        public decimal? InvoiceAmount { get; set; }

        /// <summary>
        /// 是否含稅。
        /// </summary>
        [Column("INCLUDE_TAX")]
        public string IncludeTax { get; set; }

        /// <summary>
        /// 派送單號。
        /// </summary>
        [Column("DELIVERYNO")]
        public string DeliveryNo { get; set; }

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
        /// 額外欄位 X。
        /// </summary>
        [Column("FIELD_X")]
        public string FieldX { get; set; }

        /// <summary>
        /// 業務模組。
        /// </summary>
        [Column("BIZ_MODULE")]
        public string BizModule { get; set; }

        /// <summary>
        /// 訂單號。
        /// </summary>
        [Column("ORDER_NO")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 快遞單號。
        /// </summary>
        [Column("EXPRESS_NO")]
        public string ExpressNo { get; set; }

        /// <summary>
        /// LINE 路由代碼。
        /// </summary>
        [Column("LINE_CODE")]
        public string LineCode { get; set; }

        /// <summary>
        /// ECM 來源識別。
        /// </summary>
        [Column("ECM")]
        public string Ecm { get; set; }

        /// <summary>
        /// 資料狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 舊版相容欄位，對應收件人名稱。
        /// </summary>
        [NotMapped]
        public string Importer
        {
            get { return Recipient; }
            set { Recipient = value; }
        }

        /// <summary>
        /// 舊版相容欄位，對應收件人電話。
        /// </summary>
        [NotMapped]
        public string ImporterPhone
        {
            get { return RecPhone; }
            set { RecPhone = value; }
        }

        /// <summary>
        /// 舊版相容欄位，對應收件地址。
        /// </summary>
        [NotMapped]
        public string ImporterAddr
        {
            get { return RecAddress; }
            set { RecAddress = value; }
        }

        /// <summary>
        /// 舊版相容欄位，對應客戶代碼。
        /// </summary>
        [NotMapped]
        public string CustCode
        {
            get { return DespatchNo; }
            set { DespatchNo = value; }
        }

        /// <summary>
        /// 舊版相容欄位，對應清關倉別。
        /// </summary>
        [NotMapped]
        public int TransNo
        {
            get { return ClearanceWarehousing ?? 0; }
            set { ClearanceWarehousing = value; }
        }

        /// <summary>
        /// 舊版相容欄位，對應到付款金額。
        /// </summary>
        [NotMapped]
        public string CC
        {
            get { return Cc; }
            set { Cc = value; }
        }
    }
}
