using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.Tax
{
    public class TaxData
    {
        /// <summary>
        /// 跟派件收
        /// </summary>
        public int TransCod { get; set; }

        /// <summary>
        /// 跟客戶收
        /// </summary>
        public int CustomerCod { get; set; }

        /// <summary>
        /// 物流代收款
        /// </summary>
        public int ToDlvCod { get; set; }
    }
}
