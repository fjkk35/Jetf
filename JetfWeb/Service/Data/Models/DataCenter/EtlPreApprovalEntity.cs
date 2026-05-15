using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 海關預審資料。
    /// </summary>
    [Table("ETL_PRE_APPROVAL", Schema = "dbo")]
    public sealed class EtlPreApprovalEntity
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
        /// 海關代碼。
        /// </summary>
        [Column("CUSTOMS_CODE")]
        public string CustomsCode { get; set; }

        /// <summary>
        /// 報關箱號。
        /// </summary>
        [Column("BROKER_BOX_NO")]
        public string BrokerBoxNo { get; set; }

        /// <summary>
        /// 申報類型。
        /// </summary>
        [Column("DECL_TYPE")]
        public string DeclType { get; set; }

        /// <summary>
        /// 申報號碼。
        /// </summary>
        [Column("DECL_NO")]
        public string DeclNo { get; set; }

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
        /// 進口人證號。
        /// </summary>
        [Column("ID")]
        public string IdNo { get; set; }

        /// <summary>
        /// 電話。
        /// </summary>
        [Column("TEL")]
        public string Tel { get; set; }

        /// <summary>
        /// 單項課稅金額。
        /// </summary>
        [Column("ITEM_CHARGE_AMOUNT")]
        public string ItemChargeAmount { get; set; }

        /// <summary>
        /// 進口日期字串。
        /// </summary>
        [Column("IMPORT_DATE")]
        public string ImportDate { get; set; }

        /// <summary>
        /// 流水序號。
        /// </summary>
        [Column("SEQUENCE_NUMERIC")]
        public string SequenceNumeric { get; set; }

        /// <summary>
        /// 貨物描述。
        /// </summary>
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        /// <summary>
        /// 回覆代碼。
        /// </summary>
        [Column("REPLY_CODE")]
        public string ReplyCode { get; set; }

        /// <summary>
        /// 海關核准號碼。
        /// </summary>
        [Column("CUSTOMS_APPROVAL_NUMBER")]
        public string CustomsApprovalNumber { get; set; }

        /// <summary>
        /// 海關核准時間字串。
        /// </summary>
        [Column("CUSTOMS_APPROVAL_DATETIME")]
        public string CustomsApprovalDateTime { get; set; }

        /// <summary>
        /// 修改序號。
        /// </summary>
        [Column("MODIFY_SEQ")]
        public int? ModifySeq { get; set; }

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
        /// 核准時間。
        /// </summary>
        [Column("APPROVAL_TIME")]
        public DateTime? ApprovalTime { get; set; }

        /// <summary>
        /// 訊息識別碼。
        /// </summary>
        [Column("MESSAGE_IDENTIFIER")]
        public string MessageIdentifier { get; set; }

        /// <summary>
        /// 順序號碼。
        /// </summary>
        [Column("SEQ_NO")]
        public int? SeqNo { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CUST_CODE")]
        public string CustCode { get; set; }
    }
}