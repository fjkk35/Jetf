using System.Collections.Generic;

namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 海運簡易查詢頁面所需的下拉選單資料。
    /// </summary>
    public class EzwaySeaQueryOptions
    {
        /// <summary>
        /// 報關業者選取後要回填的查詢欄位名稱。
        /// </summary>
        public string BrokerQueryField { get; set; }

        /// <summary>
        /// 報關業者下拉選單。
        /// </summary>
        public List<EzwaySeaBrokerOption> BrokerOptions { get; set; } = new List<EzwaySeaBrokerOption>();

        /// <summary>
        /// 預設選取的報關業者值。
        /// </summary>
        public string SelectedBrokerValue { get; set; }

        /// <summary>
        /// 集運商下拉選單。
        /// </summary>
        public List<EzwaySeaConsolidatorOption> ConsolidatorOptions { get; set; } = new List<EzwaySeaConsolidatorOption>();

        /// <summary>
        /// 預設選取的集運商值。
        /// </summary>
        public string SelectedConsolidator { get; set; }

        /// <summary>
        /// 預設選取的集運商帳號。
        /// </summary>
        public string SelectedConsolidatorUserId { get; set; }
    }

    /// <summary>
}