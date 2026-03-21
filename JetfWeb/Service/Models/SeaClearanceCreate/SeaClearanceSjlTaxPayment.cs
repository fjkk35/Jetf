using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearanceCreate
{
    public class SeaClearanceSjlTaxPayment
    {
        /// <summary>
        /// 原單申報人
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 收費方式
        /// </summary>
        public string TaxPayment { get; set; }
    }
}
