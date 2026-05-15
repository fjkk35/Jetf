using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 異常訂單發送明細。
    /// </summary>
    [Table("ErrorOrderSendDetail", Schema = "dbo")]
    public sealed class ErrorOrderSendDetailEntity
    {
        /// <summary>
        /// 明細主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 所屬發送批次主鍵。
        /// </summary>
        [Column("ErrorOrderSendId")]
        public int ErrorOrderSendId { get; set; }

        /// <summary>
        /// 異常或通知類型。
        /// </summary>
        [Column("Type")]
        public string Type { get; set; }

        /// <summary>
        /// 聯絡電話。
        /// </summary>
        [Column("Phone")]
        public string Phone { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Column("Customer")]
        public string Customer { get; set; }

        /// <summary>
        /// 平台名稱。
        /// </summary>
        [Column("Platform")]
        public string Platform { get; set; }

        /// <summary>
        /// 追蹤單號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 發送方式。
        /// </summary>
        [Column("SendType")]
        public string SendType { get; set; }

        /// <summary>
        /// LINE 使用者識別碼。
        /// </summary>
        [Column("LineUserId")]
        public string LineUserId { get; set; }

        /// <summary>
        /// 是否已發送。
        /// </summary>
        [Column("IsSend")]
        public string IsSend { get; set; }

        /// <summary>
        /// 發送訊息內容。
        /// </summary>
        [Column("Message")]
        public string Message { get; set; }

        /// <summary>
        /// 簡訊模板名稱。
        /// </summary>
        [Column("SmsName")]
        public string SmsName { get; set; }

        /// <summary>
        /// 發送結果代碼。
        /// </summary>
        [Column("SendResult")]
        public string SendResult { get; set; }

        /// <summary>
        /// 發送結果訊息。
        /// </summary>
        [Column("SendResultMessage")]
        public string SendResultMessage { get; set; }

        /// <summary>
        /// 簡訊平台列號。
        /// </summary>
        [Column("SmsRowId")]
        public string SmsRowId { get; set; }

        /// <summary>
        /// 簡訊拆分筆數。
        /// </summary>
        [Column("SmsCnt")]
        public string SmsCnt { get; set; }

        /// <summary>
        /// 簡訊錯誤代碼。
        /// </summary>
        [Column("SmsErrorCode")]
        public string SmsErrorCode { get; set; }

        /// <summary>
        /// 發送時間。
        /// </summary>
        [Column("SendDateTime")]
        public DateTime? SendDateTime { get; set; }
    }
}