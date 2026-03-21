using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.SeaUnreceivedOrder
{
    public class SeaUnreceivedOrderViewModel
    {
        [Display(Name = "資料來源")]
        public SeaErrorReportEnum DataType { get; set; }

        [Display(Name = "資料來源")]
        public IEnumerable<SelectListItem> DataTypeList { get; set; }

        /// <summary>
        /// 主號查詢
        /// </summary>
        public string MainNumber { get; set; }
    }
}