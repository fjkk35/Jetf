using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Job.TactWebClientJob.Model
{
    /// <summary>
    /// 放行未出倉查詢列表
    /// </summary>
    public class TactRelnonoutModel
    {
        /// <summary>
        /// 併袋號
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string DeclarationNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 通關方式
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 放行時間
        /// </summary>
        public string ReleaseTime { get; set; }

        /// <summary>
        /// 申報件數
        /// </summary>
        public int DeclaredQty { get; set; }

        /// <summary>
        /// 進倉件數
        /// </summary>
        public int InboundQty { get; set; }

        /// <summary>
        /// 出倉件數
        /// </summary>
        public int OutboundQty { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public string InboundTime { get; set; }

        /// <summary>
        /// 貨況
        /// </summary>
        public string CargoStatus { get; set; }
    }
}
