using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.InvoiceNew.Domain
{
    /// <summary>
    /// 開立電子發票作業New Model
    /// </summary>
    public class InvoiceWorkNewModel
    {
        /// <summary>
        /// 序號
        /// </summary>
        public string Seq { get; set; }

        /// <summary>
        /// 發票日期
        /// </summary>
        public string InvoiceDate { get; set; }

        /// <summary>
        /// 發票號碼
        /// </summary>
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 追蹤單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 金額（不含稅）
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// 稅額
        /// </summary>
        public string Tax { get; set; }

        /// <summary>
        /// 總金額（含稅）
        /// </summary>
        public string TotalAmount { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 統編抬頭
        /// </summary>
        public string VATTitle { get; set; }

        /// <summary>
        /// 統一編號
        /// </summary>
        public string VATNo { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
    }
}
