using Service.EnumTax;
using Service.Extensions;
using System;

namespace Service.Services.Authority.Domain
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
    /// 權限編輯用資料傳輸物件
    /// </summary>
    public class AuthorityEditDto
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string PartnerId { get; set; }
        public int Sort { get; set; }
    }

    /// <summary>
    /// 權限分類選項
    /// </summary>
    public class PartnerOptionDto
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public int Sort { get; set; }
    }
}