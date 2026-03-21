using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ScanCargoArrivalTime
{
    public class ScanCargoArrivalTimeModel
    {
        /// <summary>
        /// 掃貨上車Id
        /// </summary>
        public int PdtScanCargoUploadId { get; set; }

        /// <summary>
        /// 入庫時間
        /// </summary>
        public string ArrivalTime { get; set; }

        /// <summary>
        /// 掃讀時間
        /// </summary>
        public string UploadTime { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 查詢時間
        /// </summary>
        public string SearchTime { get; set; }

        /// <summary>
        /// 查詢人員
        /// </summary>
        public string SearchOpe { get; set; }
    }
}
