using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class CustomerViewModel
    {
        [Display(Name = "運送類型")]
        public string tran_type { get; set; }
        public IEnumerable<SelectListItem> ddlTranTypeList { get; set; }

        [Display(Name = "ID")]
        public string id { get; set; }
        [Display(Name = "CUST_ID")]
        public string cust_id { get; set; }
        [Display(Name = "CUSTOMER")]
        public string customer { get; set; }
        [Display(Name = "TRANS_NO")]
        public string trans_no { get; set; }
        [Display(Name = "TRANS_NAME")]
        public string trans_name { get; set; }
        [Display(Name = "是否包稅")]
        public IEnumerable<SelectListItem> ddlIncludeTaxList { get; set; }

        [Display(Name = "是否包稅")]
        public string include_tax { get; set; }
        [Display(Name = "是否包稅中文")]
        public string include_tax_name { get; set; }
        [Display(Name = "物流公司")]
        public string company_no { get; set; }
        [Display(Name = "物流公司")]
        public string company { get; set; }
        public IEnumerable<SelectListItem> ddlCompanyList { get; set; }
        [Display(Name = "手續費")]
        public string cod_fee { get; set; }

        [Display(Name = "菜鳥尊榮服務")]
        public bool IsCainiaoP { get; set; }
    }
}