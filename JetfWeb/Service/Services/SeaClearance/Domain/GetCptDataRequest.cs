using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance.Domain
{
    /// <summary>
    /// 取得關貿GB301、GB321資料請求Model
    /// </summary>
    public class GetCptDataRequest
    {
        /// <summary>
        /// 海運通關明細ID
        /// </summary>
        public int SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }
    }
}
