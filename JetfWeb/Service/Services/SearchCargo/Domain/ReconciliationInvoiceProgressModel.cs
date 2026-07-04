using System;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
    /// 回款進度資料模型。
    /// </summary>
    public class ReconciliationInvoiceProgressModel
    {
        /// <summary>
        /// 回款日期。
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// 發票類別。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 發票開立日期。
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// 發票號碼。
        /// </summary>
        public string Invoice { get; set; }
    }
}
