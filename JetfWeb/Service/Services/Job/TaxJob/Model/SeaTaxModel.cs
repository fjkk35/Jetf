using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.TaxJob.Model
{
    /// <summary>
    /// 捷利海運稅金查詢資料。
    /// </summary>
    public class SeaTaxModel
    {
        public int Id { get; set; }

        /// <summary>
        /// 資料日期
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string Dlv_Com { get; set; }

        /// <summary>
        /// 清關袋號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號
        /// </summary>
        public string Dlv_Inv { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string Clearance_Number { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 稅金編號
        /// </summary>
        public string Tax_Number { get; set; }

        /// <summary>
        /// 稅額
        /// </summary>
        public decimal Tax_Amount { get; set; }
    }
}
