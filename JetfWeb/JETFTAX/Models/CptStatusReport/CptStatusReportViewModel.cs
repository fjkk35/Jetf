using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.CptStatusReport
{
    public class CptStatusReportViewModel
    {
        [Display(Name = "日　　期")]
        public string StartDate { get; set; }

        [Display(Name = "日　　期")]
        public string EndDate { get; set; }

        [Display(Name = "資料來源")]
        public DataTypeEnum DataType { get; set; }

        [Display(Name = "資料來源")]
        public IEnumerable<SelectListItem> DataTypeList { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        [Display(Name = "狀　　態")]
        public CptStatusEnum CptStatus { get; set; }

        [Display(Name = "狀　　態")]
        public IEnumerable<SelectListItem> CptStatusList { get; set; }
    }
}