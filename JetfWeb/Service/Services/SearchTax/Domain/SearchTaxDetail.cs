using System.Collections.Generic;

namespace Service.Services.SearchTax.Domain
{
    /// <summary>
    /// 稅金查詢明細（費用資料與配送進度）。
    /// </summary>
    public sealed class SearchTaxDetail
    {
        /// <summary>
        /// 費用資料；查無資料時為 null。
        /// </summary>
        public SearchTaxRow Fee { get; set; }

        /// <summary>
        /// 配送進度，依作業時間由新到舊排序。
        /// </summary>
        public List<SearchTaxCargoStatus> CargoStatuses { get; set; } = new List<SearchTaxCargoStatus>();
    }
}
