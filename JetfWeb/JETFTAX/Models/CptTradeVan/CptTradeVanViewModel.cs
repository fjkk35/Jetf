using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.CptTradeVan
{
    public class CptTradeVanViewModel
    {
        [Display(Name = "資料來源")]
        public CptTradeVanEnum source { get; set; }

        [Display(Name = "資料來源")]
        public IEnumerable<SelectListItem> ddlSourceList { get; set; }

        /// <summary>
        /// 海運主號查詢(海快作業)、海運-收單查詢(海快作業)-使用
        /// </summary>
        public string Data { get; set; }
    }
}