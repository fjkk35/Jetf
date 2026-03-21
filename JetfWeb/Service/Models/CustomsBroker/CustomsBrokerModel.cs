using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CustomsBroker
{
    /// <summary>
    /// 報驗公司 Model
    /// </summary>
    public class CustomsBrokerModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 報驗公司名稱
        /// </summary>
        [Required(ErrorMessage = "報驗公司名稱為必填欄位")]
        [Display(Name = "報驗公司名稱")]
        public string Name { get; set; }

        /// <summary>
        /// 港區
        /// </summary>
        [Display(Name = "港區")]
        public string PortArea { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Display(Name = "更新時間")]
        public DateTime UpdateDateTime { get; set; }

        /// <summary>
        /// 更新人員
        /// </summary>
        [Display(Name = "更新人員")]
        public string UpdateOperator { get; set; }
    }

    /// <summary>
    /// 報驗公司聯絡人 Model
    /// </summary>
    public class CustomsBrokerContactModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 報驗公司 ID
        /// </summary>
        [Required(ErrorMessage = "請選擇報驗公司")]
        public int CustomsBrokerId { get; set; }

        /// <summary>
        /// 聯絡人
        /// </summary>
        [Required(ErrorMessage = "聯絡人為必填欄位")]
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
        [Display(Name = "更新時間")]
        public DateTime? UpdateDateTime { get; set; }

        /// <summary>
        /// 更新人員
        /// </summary>
        [Display(Name = "更新人員")]
        public string UpdateOperator { get; set; }
    }

    /// <summary>
    /// 報驗公司查詢結果 (含聯絡人)
    /// </summary>
    public class CustomsBrokerWithContactModel
    {
        /// <summary>
        /// 報驗公司 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 報驗公司名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 港區
        /// </summary>
        public string PortArea { get; set; }

        /// <summary>
        /// 聯絡人 ID
        /// </summary>
        public int? ContactId { get; set; }

        /// <summary>
        /// 聯絡人
        /// </summary>
        public string ContactPerson { get; set; }

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
        public DateTime UpdateDateTime { get; set; }

        /// <summary>
        /// 更新人員
        /// </summary>
        public string UpdateOperator { get; set; }
    }

    /// <summary>
    /// 報驗公司請求 Model
    /// </summary>
    public class CustomsBrokerRequest
    {
        public string Name { get; set; }
        public string ContactPerson { get; set; }
        public string PortArea { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Category { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// 報驗公司回應 Model
    /// </summary>
    public class CustomsBrokerResponse
    {
        public List<CustomsBrokerWithContactModel> Data { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 報驗公司下拉選單 Model
    /// </summary>
    public class CustomsBrokerDropdownModel
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
}