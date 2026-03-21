using System;

namespace Service.Services.PdtScanCargoArrivalTime.Domain
{
    public class PdtScanCargoArrivalTimeRequest
    {
        /// <summary>
        /// 開始時間
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 結束時間
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 派件公司代碼
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 作業地區
        /// </summary>
        public string DataType { get; set; }
    }
}
