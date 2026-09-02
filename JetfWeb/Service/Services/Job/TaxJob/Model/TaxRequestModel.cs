using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.TaxJob.Model
{
    /// <summary>
    /// 捷利稅金 API 的請求資料。
    /// </summary>
    public class TaxRequestModel
    {
        public int FeeMasterId { get; set; }
        /// <summary>
        /// 作業日
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 稅單號碼
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string DeclarationNumber { get; set; }

        /// <summary>
        /// 清關袋號
        /// </summary>
        public string Bigbagid { get; set; }

        /// <summary>
        /// 運單號
        /// </summary>
        public string Edelno { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string ConsigneeName { get; set; }

        /// <summary>
        /// 稅金金額
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 支付對象
        /// </summary>
        public string PayObject { get; set; }

        /// <summary>
        /// 空运/海运
        /// </summary>
        public string Type { get; set; }
    }
}
