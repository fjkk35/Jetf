using System;

namespace Service.Services.EtlErrorWork.Domain
{
    /// <summary>
    /// 空快錯單明細資料模型
    /// </summary>
    public class EtlErrorWorkDetailsModel
    {
        /// <summary>
        /// 客戶
        /// </summary>
        public string CUST { get; set; }

        /// <summary>
        /// 發出時間
        /// </summary>
        public DateTime? OUT_TIME { get; set; }

        /// <summary>
        /// 入倉時間
        /// </summary>
        public DateTime? sign_in_time { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? sign_out_time { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string HAWB { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string RECIPIENT { get; set; }

        /// <summary>
        /// 收件人電話
        /// </summary>
        public string RECPHONE { get; set; }

        /// <summary>
        /// 問題原因
        /// </summary>
        public string REASON { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string MAWB { get; set; }

        /// <summary>
        /// 袋號
        /// </summary>
        public string BAG_NO { get; set; }

        /// <summary>
        /// 送達日期
        /// </summary>
        public DateTime? DELIVERYDATE { get; set; }

        /// <summary>
        /// 欄位X (客戶外箱號)
        /// </summary>
        public string FIELD_X { get; set; }

        /// <summary>
        /// 訂單號
        /// </summary>
        public string ORDER_NO { get; set; }
    }
}
