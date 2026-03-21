using System;
using System.ComponentModel.DataAnnotations;

namespace Service.Models.CustomsBroker
{
    /// <summary>
    /// 報驗公司聯絡人 Model
    /// </summary>
    public class CustomsBrokerContactModel
    {
        /// <summary>
        /// 聯絡人 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 報驗公司 ID
        /// </summary>
        [Required(ErrorMessage = "報驗公司必須選擇")]
        public int CustomsBrokerId { get; set; }

        /// <summary>
        /// 聯絡人
        /// </summary>
        [Required(ErrorMessage = "聯絡人必須填寫")]
        [Display(Name = "聯絡人")]
        public string ContactPerson { get; set; }

        /// <summary>
        /// 電子郵件
        /// </summary>
        [Display(Name = "電子郵件")]
        public string Email { get; set; }

        /// <summary>
        /// 電話
        /// </summary>
        [Display(Name = "電話")]
        public string Phone { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        [Display(Name = "類別")]
        public string Category { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime? UpdateDateTime { get; set; }

        /// <summary>
        /// 更新人員
        /// </summary>
        public string UpdateOperator { get; set; }

        /// <summary>
        /// 公司名稱 (用於顯示)
        /// </summary>
        public string CompanyName { get; set; }
    }

    /// <summary>
    /// 報驗公司查詢結果 ViewModel (含聯絡人資訊)
    /// </summary>
    public class CustomsBrokerWithContactViewModel
    {
        /// <summary>
        /// 報驗公司 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 公司名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 聯絡人
        /// </summary>
        public string ContactPerson { get; set; }

        /// <summary>
        /// 港區
        /// </summary>
        public string PortArea { get; set; }

        /// <summary>
        /// 電子郵件
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 電話
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime? UpdateDateTime { get; set; }

        /// <summary>
        /// 更新人員
        /// </summary>
        public string UpdateOperator { get; set; }

        /// <summary>
        /// 聯絡人 ID (用於編輯/刪除)
        /// </summary>
        public int? ContactId { get; set; }
    }

    /// <summary>
    /// 報驗公司查詢回應 Model
    /// </summary>
    public class CustomsBrokerWithContactResponse
    {
        public int TotalCount { get; set; }
        public System.Collections.Generic.List<CustomsBrokerWithContactViewModel> Data { get; set; }
    }
}
