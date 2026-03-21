using Service.EnumTax;
using Service.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.AuthorityGroup.Domain
{
    /// <summary>
    /// 權限資料傳輸物件
    /// </summary>
    public class AuthorityDto
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string PartnerId { get; set; }
        public int Sort { get; set; }

        /// <summary>
        /// 權限分類名稱
        /// </summary>
        public string PartnerName => PartnerId.ToEnum<AuthorityPartner>().ToDescription();

        /// <summary>
        /// 權限分類排序
        /// </summary>
        public int PartnerSort
        {
            get
            {
                var partner = PartnerId.ToEnum<AuthorityPartner>();
                return partner.GetSort() ?? 0;
            }
        }
    }

    /// <summary>
    /// 權限群組資料傳輸物件
    /// </summary>
    public class AuthorityGroupDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string Memo { get; set; }

        /// <summary>
        /// 群組擁有的權限清單
        /// </summary>
        public List<AuthorityDto> Authorities { get; set; } = new List<AuthorityDto>();

        /// <summary>
        /// 權限數量
        /// </summary>
        public int AuthorityCount => Authorities?.Count ?? 0;

        /// <summary>
        /// 權限名稱列表（用於顯示）
        /// </summary>
        public string AuthorityNames => string.Join("、", Authorities?.Select(a => a.Text) ?? new List<string>());
    }

    /// <summary>
    /// 權限群組編輯用資料傳輸物件
    /// </summary>
    public class AuthorityGroupEditDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string Memo { get; set; }
        public List<string> AuthorityIds { get; set; }
    }
}