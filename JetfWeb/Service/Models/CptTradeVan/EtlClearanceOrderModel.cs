using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class EtlClearanceOrderModel
    {
        /// <summary>
        /// 主提單號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// GB301結果
        /// </summary>
        public string Gb301Msg { get; set; }

        /// <summary>
        /// GB301收單建檔
        /// </summary>
        public string Gb301ReceiveOrder { get; set; }

        /// <summary>
        /// GB302結果
        /// </summary>
        public string Gb302Msg { get; set; }

        /// <summary>
        /// 不受理原因
        /// </summary>
        public List<Gb302GridModel> GridModel { get; set; }
    }
}
