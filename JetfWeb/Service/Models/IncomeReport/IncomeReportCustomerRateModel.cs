using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.IncomeReport
{
    public class IncomeReportCustomerRateModel
    {
        /// <summary>
        /// 分類 海快、空快
        /// </summary>
        public string TranType { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string DespatchName { get; set; }

        /// <summary>
        /// 清關收入
        /// </summary>
        public decimal CC { get; set; }

        /// <summary>
        /// 手續費
        /// </summary>
        public int FEE2 { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public decimal Gw { get; set; }



        /// <summary>
        /// 袋數
        /// </summary>
        public int BagNumberCount { get; set; }

        /// <summary>
        /// 筆數
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 清關收入
        /// </summary>
        public decimal TotalCC { get; set; }


        /// <summary>
        /// 手續費
        /// </summary>
        public int TotalFEE2 { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public decimal TotalGw { get; set; }

        /// <summary>
        /// 袋數
        /// </summary>
        public int TotalBagNumberCount { get; set; }

        /// <summary>
        /// 筆數
        /// </summary>
        public int TotalCount { get; set; }
    }
}
