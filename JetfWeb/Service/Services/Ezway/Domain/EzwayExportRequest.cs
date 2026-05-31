using System.Collections.Generic;

namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 查詢結果匯出請求。
    /// </summary>
    public class EzwayExportRequest
    {
        /// <summary>
        /// 要匯出的查詢結果清單。
        /// </summary>
        public List<EzwayQueryResult> Results { get; set; } = new List<EzwayQueryResult>();

        /// <summary>
        /// 當畫面尚未先查詢時，匯出可直接使用的查詢條件。
        /// </summary>
        public EzwayQueryRequest QueryRequest { get; set; } = new EzwayQueryRequest();
    }
}