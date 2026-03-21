using System;

namespace Service.Services.PdtScanCargoArrivalTime.Domain
{
    public class PdtScanCargoArrivalTimeQueryRow
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 交倉時間
        /// </summary>
        public DateTime? ArrivalTime { get; set; }

        /// <summary>
        /// 掃貨上傳時間
        /// </summary>
        public DateTime? UploadTime { get; set; }

        /// <summary>
        /// 掃描資料
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 派件公司代碼
        /// </summary>
        public string TransNo { get; set; }

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
