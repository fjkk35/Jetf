using System;
using System.Collections.Generic;

namespace JETFTAX.Models.PdtScanCargoArrivalTime
{
    public class PdtScanCargoArrivalTimeUpdateVm
    {
        /// <summary>
        /// 交倉時間(使用者輸入)
        /// </summary>
        public DateTime? ArrivalTime { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 群組內所有 Id
        /// </summary>
        public List<string> Ids { get; set; }
    }
}
