using System;
using System.Collections.Generic;

namespace Service.Services.PdtScanCargoArrivalTime.Domain
{
    public class PdtScanCargoArrivalTimeUpdateRequest
    {
        /// <summary>
        /// 交倉時間(使用者輸入)
        /// </summary>
        public DateTime? ArrivalTime { get; set; }

        /// <summary>
        /// 群組內所有 Id
        /// </summary>
        public List<string> Ids { get; set; }
    }
}
