using System;

namespace Service.Models
{
    /// <summary>
    /// 處置說明報表的一列資料。
    /// </summary>
    public sealed class ProcessReportRowModel
    {
        /// <summary>處理資料日期。</summary>
        public string DataDate { get; set; }
        /// <summary>分提單號。</summary>
        public string DlvInv { get; set; }
        /// <summary>處置說明。</summary>
        public string Remark { get; set; }
        /// <summary>輸入人員代碼。</summary>
        public string UserId { get; set; }
        /// <summary>輸入人員姓名。</summary>
        public string UserName { get; set; }
        /// <summary>輸入時間。</summary>
        public DateTime? CreateDateTime { get; set; }
        /// <summary>刪除狀態。</summary>
        public string Deleted { get; set; }
        /// <summary>結案狀態。</summary>
        public string Finish { get; set; }
        /// <summary>結案人員代碼。</summary>
        public string FinishUserId { get; set; }
        /// <summary>結案人員姓名。</summary>
        public string FinishUserName { get; set; }
        /// <summary>結案時間。</summary>
        public DateTime? FinishDateTime { get; set; }
        /// <summary>處理類型。</summary>
        public string ProcessType { get; set; }
        /// <summary>客戶代碼。</summary>
        public string CusdId { get; set; }
        /// <summary>客戶名稱。</summary>
        public string Customer { get; set; }
        /// <summary>主提單號。</summary>
        public string MainNumber { get; set; }
        /// <summary>清關袋號。</summary>
        public string BlNo { get; set; }
        /// <summary>倉儲類型。</summary>
        public string IDataType { get; set; }
        /// <summary>收件人名稱。</summary>
        public string Importer { get; set; }
        /// <summary>收件人電話。</summary>
        public string ImPhoneNo { get; set; }
        /// <summary>到港日期。</summary>
        public DateTime? Eta { get; set; }
        /// <summary>入倉時間。</summary>
        public DateTime? ISignInTime { get; set; }
        /// <summary>出倉時間。</summary>
        public DateTime? ISignOutTime { get; set; }
    }
}
