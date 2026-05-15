using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 袋單明細資料。
    /// </summary>
    [Table("MAKELIST", Schema = "dbo")]
    public sealed class MakeListEntity
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
        /// 追蹤單號。
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
        /// 修改後品名。
        /// </summary>
        [Column("ITEMSMODIFY")]
        public string ItemsModify { get; set; }

        /// <summary>
        /// 數量。
        /// </summary>
        [Column("QUANTITY")]
        public int? Quantity { get; set; }

        /// <summary>
        /// 修改後數量。
        /// </summary>
        [Column("QUANTITYMODIFY")]
        public int? QuantityModify { get; set; }

        /// <summary>
        /// 單位。
        /// </summary>
        [Column("UNIT")]
        public string Unit { get; set; }

        /// <summary>
        /// 產地。
        /// </summary>
        [Column("ORIGIN")]
        public string Origin { get; set; }

        /// <summary>
        /// 清關倉別代碼。
        /// </summary>
        [Column("CLEARANCEWAREHOUSING")]
        public int? ClearanceWarehousing { get; set; }

        /// <summary>
        /// 派件商代碼。
        /// </summary>
        [Column("DISPATCHER")]
        public string Dispatcher { get; set; }

        /// <summary>
        /// 稅別。
        /// </summary>
        [Column("TAXTYPE")]
        public string TaxType { get; set; }

        /// <summary>
        /// 稅籍登記。
        /// </summary>
        [Column("TAXREG")]
        public string TaxReg { get; set; }

        /// <summary>
        /// 稅籍分支登記。
        /// </summary>
        [Column("TAXSUBREG")]
        public string TaxSubReg { get; set; }

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
        /// 收件人 Email。
        /// </summary>
        [Column("RECEMAIL")]
        public string RecEmail { get; set; }

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
        /// 尾段資訊。
        /// </summary>
        [Column("FINALPART")]
        public string FinalPart { get; set; }

        /// <summary>
        /// 品項狀態。
        /// </summary>
        [Column("ITEMSTATUS")]
        public int? ItemStatus { get; set; }

        /// <summary>
        /// 清關狀態。
        /// </summary>
        [Column("CLEARANCESTATUS")]
        public int? ClearanceStatus { get; set; }

        /// <summary>
        /// 儲位代碼。
        /// </summary>
        [Column("LOCID")]
        public string LocId { get; set; }

        /// <summary>
        /// 初始狀態。
        /// </summary>
        [Column("INITIALSTATUS")]
        public int? InitialStatus { get; set; }

        /// <summary>
        /// 配送狀態。
        /// </summary>
        [Column("DELIVERSTATUS")]
        public int? DeliverStatus { get; set; }

        /// <summary>
        /// 貨況條件。
        /// </summary>
        [Column("ITEMCONDITION")]
        public int? ItemCondition { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("DESPATCHNO")]
        public string DespatchNo { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("REMARK")]
        public string Remark { get; set; }

        /// <summary>
        /// 來源系統。
        /// </summary>
        [Column("SOURCEFROM")]
        public string SourceFrom { get; set; }

        /// <summary>
        /// 新主號。
        /// </summary>
        [Column("NEWMAINNUM")]
        public string NewMainNum { get; set; }

        /// <summary>
        /// 新袋號。
        /// </summary>
        [Column("NEWBAGNO")]
        public string NewBagNo { get; set; }

        /// <summary>
        /// 交派日期。
        /// </summary>
        [Column("DELIVERDATE")]
        public DateTime? DeliverDate { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATEDATE")]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CREATEBY")]
        public string CreateBy { get; set; }

        /// <summary>
        /// 更新時間。
        /// </summary>
        [Column("UPDATEDATE")]
        public DateTime? UpdateDate { get; set; }

        /// <summary>
        /// 更新人員。
        /// </summary>
        [Column("UPDATEBY")]
        public string UpdateBy { get; set; }

        /// <summary>
        /// 進倉時間。
        /// </summary>
        [Column("sign_in_time")]
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        [Column("sign_out_time")]
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// GS_TFD 標記。
        /// </summary>
        [Column("GS_TFD")]
        public string GsTfd { get; set; }

        /// <summary>
        /// 申報類型。
        /// </summary>
        [Column("DECL_TYPE")]
        public string DeclType { get; set; }

        /// <summary>
        /// OBC 代碼。
        /// </summary>
        [Column("OBC_CODE")]
        public string ObcCode { get; set; }

        /// <summary>
        /// 資料類型。
        /// </summary>
        [Column("DATA_TYPE")]
        public string DataType { get; set; }
    }
}