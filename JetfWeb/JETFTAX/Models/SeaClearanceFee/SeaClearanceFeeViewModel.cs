using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.SeaClearanceFee
{
    public class SeaClearanceFeeViewModel
    {
        public string CustCode { get; set; }

        public IEnumerable<SelectListItem> CustList { get; set; }

        /// <summary>
        /// G1費用
        /// </summary>
        public int? G1Fee { get; set; }

        /// <summary>
        /// 移倉費用
        /// </summary>
        public int? MoveWarehouseFee { get; set; }

        /// <summary>
        /// 轉G1
        /// </summary>
        public int? TransferG1Fee { get; set; }

        /// <summary>
        /// 轉移倉
        /// </summary>
        public int? TransferWarehouseFee { get; set; }

        /// <summary>
        /// X2X3費用
        /// </summary>
        public int? X2Fee { get; set; }

        public List<SeaClearanceFeeModel> List { get; set; }
    }
}