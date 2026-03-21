using System.ComponentModel.DataAnnotations;

namespace JETFTAX.Models.Authority
{
    /// <summary>
    /// 新增/修改權限請求物件
    /// </summary>
    public class SaveAuthorityRequest
    {
        /// <summary>
        /// 權限ID
        /// </summary>
        [Required(ErrorMessage = "請輸入權限ID")]
        [StringLength(50, ErrorMessage = "權限ID不能超過50個字元")]
        public string Id { get; set; }

        /// <summary>
        /// 權限說明
        /// </summary>
        [Required(ErrorMessage = "請輸入權限說明")]
        [StringLength(100, ErrorMessage = "權限說明不能超過100個字元")]
        public string Text { get; set; }

        /// <summary>
        /// 權限分類
        /// </summary>
        [Required(ErrorMessage = "請選擇權限分類")]
        public string PartnerId { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        [Range(0, 9999, ErrorMessage = "排序必須介於0-9999之間")]
        public int Sort { get; set; }
    }
}