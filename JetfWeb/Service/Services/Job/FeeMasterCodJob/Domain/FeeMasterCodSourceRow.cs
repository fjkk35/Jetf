using System;

namespace Service.Services.Job.FeeMasterCodJob.Domain
{
    /// <summary>
    /// 表示從空運或海運來源 SQL 查得的到付款資料。
    /// </summary>
    public sealed class FeeMasterCodSourceRow
    {
        /// <summary>
        /// 原始資料類型。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 空運袋號或海運分提單號。
        /// </summary>
        public string BagNo { get; set; }

        /// <summary>
        /// 空運追蹤號；海運與分提單號相同。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 空運配送單號或海運物流貨號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public decimal Cc { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public DateTime SignOutTime { get; set; }
    }
}
