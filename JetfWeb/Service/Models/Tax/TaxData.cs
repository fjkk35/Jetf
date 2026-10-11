using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.Tax
{
    /// <summary>
    /// 稅金與代收金額的分攤結果。
    /// </summary>
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
        /// 向客戶收取的稅金合計，不含報關費。
        /// </summary>
        public int CustomerTax { get; set; }

        /// <summary>
        /// 物流代收款
        /// </summary>
        public int ToDlvCod { get; set; }
    }
}
