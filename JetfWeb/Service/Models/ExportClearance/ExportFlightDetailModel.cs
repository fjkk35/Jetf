using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ExportClearance
{
    public class ExportFlightDetailModel
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string MawbNo { get; set; }
        /// <summary>
        /// 航班號
        /// </summary>
        public string FltNo { get; set; }
        /// <summary>
        /// 航班日期
        /// </summary>
        public string FltDate { get; set; }
        /// <summary>
        /// 起飛時間
        /// </summary>
        public string DepartureTime { get; set; }
        /// <summary>
        /// 到達時間
        /// </summary>
        public string ArrivalTime { get; set; }
    }
}
