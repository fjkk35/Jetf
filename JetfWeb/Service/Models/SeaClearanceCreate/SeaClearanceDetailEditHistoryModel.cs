using System;

namespace Service.Models.SeaClearanceCreate
{
    public class SeaClearanceDetailEditHistoryModel
    {
        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 舊值
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// 詳細
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 編輯時間
        /// </summary>
        public DateTime EditTime { get; set; }

        /// <summary>
        /// 編輯人員
        /// </summary>
        public string EditUser { get; set; }
    }
}