using System.Collections.Generic;

namespace Service.Services.FlightArrivalUpload.Domain
{
    /// <summary>
    /// A03、B6F 與班機派件送達活頁簿的解析結果。
    /// </summary>
    public sealed class FlightArrivalUploadData
    {
        /// <summary>
        /// A03 與 B6F 的原有匯入資料。
        /// </summary>
        public List<FlightArrivalUploadClearanceModel> ClearanceRows { get; set; }

        /// <summary>
        /// 班機與派件送達的原有匯入資料。
        /// </summary>
        public List<FlightArrivalUploadArrivalModel> ArrivalRows { get; set; }

        /// <summary>
        /// 依分提單及處理類型去重的待新增處理紀錄。
        /// </summary>
        public List<FlightArrivalUploadProcessModel> Processes { get; set; }
    }
}
