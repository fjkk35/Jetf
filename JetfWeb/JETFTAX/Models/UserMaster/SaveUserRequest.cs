using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JETFTAX.Models.UserMaster
{
    /// <summary>
    /// 新增/修改會員請求物件
    /// </summary>
    public class SaveUserRequest
    {
        /// <summary>
        /// 會員ID
        /// </summary>
        [Required(ErrorMessage = "請輸入會員ID")]
        [StringLength(50, ErrorMessage = "會員ID不能超過50個字元")]
        public string UserId { get; set; }

        /// <summary>
        /// 會員名稱
        /// </summary>
        [Required(ErrorMessage = "請輸入會員名稱")]
        [StringLength(100, ErrorMessage = "會員名稱不能超過100個字元")]
        public string UserName { get; set; }

        /// <summary>
        /// 密碼（新增時必填，修改時選填）
        /// </summary>
        [StringLength(100, ErrorMessage = "密碼不能超過100個字元")]
        public string Password { get; set; }

        /// <summary>
        /// 狀態（0停用，1啟用）
        /// </summary>
        [Required(ErrorMessage = "請選擇狀態")]
        [RegularExpression("^[01]$", ErrorMessage = "狀態只能是0或1")]
        public string UserStatus { get; set; }

        /// <summary>
        /// 權限群組ID列表（可多選）
        /// </summary>
        public List<int> AuthorityGroupIds { get; set; } = new List<int>();

        /// <summary>
        /// 是否為修改模式（用於判斷密碼是否必填）
        /// </summary>
        public bool IsEdit { get; set; }

        // 向下相容舊版本的單一權限群組ID（已棄用，但保留以避免現有代碼錯誤）
        [System.Obsolete("此屬性已棄用，請使用 AuthorityGroupIds")]
        public int? AuthorityGroupId { get; set; }
    }
}