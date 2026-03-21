using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JETFTAX.Models.AuthorityGroup
{
    /// <summary>
    /// 新增/修改權限群組請求物件
    /// </summary>
    public class SaveGroupRequest
    {
        /// <summary>
        /// 群組ID（修改時使用）
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 群組名稱
        /// </summary>
        [Required(ErrorMessage = "請輸入群組名稱")]
        [StringLength(50, ErrorMessage = "群組名稱不能超過50個字元")]
        public string GroupName { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [StringLength(200, ErrorMessage = "備註不能超過200個字元")]
        public string Memo { get; set; }

        /// <summary>
        /// 權限ID清單
        /// </summary>
        public List<string> AuthorityIds { get; set; }
    }
}