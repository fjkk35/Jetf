using System.Collections.Generic;
using System;

namespace Service.Services.PdtScanCargoArrivalTime.Domain
{
    public class PdtScanCargoArrivalTimeGroupModel
    {
        /// <summary>
        /// 派件公司代碼
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 件數(總數)
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 已交倉時間件數(ArrivalTime 有值)
        /// </summary>
        public int ArrivedCount { get; set; }

        /// <summary>
        /// 交倉時間
        /// </summary>
        public DateTime? LastArrivalTime { get; set; }

        /// <summary>
        /// 更新交倉時間
        /// </summary>
        public DateTime? LastUpdateArrivalTime { get; set; }

        /// <summary>
        /// 更新交倉人員
        /// </summary>
        public string LastUpdateArrivalTimeOpe { get; set; }

        /// <summary>
        /// 此派件公司下所有 Id
        /// </summary>
        public List<string> Ids { get; set; }

        /// <summary>
        /// 明細(依 交倉時間/更新交倉時間/更新交倉人員 分組)
        /// </summary>
        public List<PdtScanCargoArrivalTimeDetailModel> Details { get; set; }
    }
}
