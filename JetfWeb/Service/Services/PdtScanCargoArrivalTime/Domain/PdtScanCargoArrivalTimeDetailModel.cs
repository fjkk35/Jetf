using System;

namespace Service.Services.PdtScanCargoArrivalTime.Domain
{
    public class PdtScanCargoArrivalTimeDetailModel
    {
        /// <summary>
        /// 派件公司
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 交倉時間
        /// </summary>
        public DateTime? ArrivalTime { get; set; }

        /// <summary>
        /// 交倉時間件數(ArrivalTime 有值就算 1)
        /// </summary>
        public int ArrivedCount { get; set; }

        /// <summary>
        /// 更新交倉時間
        /// </summary>
        public DateTime? UpdateArrivalTime { get; set; }

        /// <summary>
        /// 更新交倉人員
        /// </summary>
        public string UpdateArrivalTimeOpe { get; set; }
    }
}
