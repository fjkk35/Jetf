using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.AccsShopee.Domain
{
    /// <summary>
    /// Accs 查詢結果
    /// </summary>
    public class AccsQueryResult
    {
        /// <summary>
        /// 序號
        /// </summary>
        public string No { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string MawbNo { get; set; }

        /// <summary>
        /// 主號總件數
        /// </summary>
        public string TotalHwb { get; set; }

        /// <summary>
        /// 本批件數
        /// </summary>
        public string Total { get; set; }

        /// <summary>
        /// 毛重
        /// </summary>
        public string Weight { get; set; }

        /// <summary>
        /// 航機班次
        /// </summary>
        public string FlightNo { get; set; }

        /// <summary>
        /// 進口日期
        /// </summary>
        public string ImportDate { get; set; }

        /// <summary>
        /// 航班號碼（保留欄位，可能從查詢頁面取得）
        /// </summary>
        public string VoyageFlightNo { get; set; }

        /// <summary>
        /// 航班日期（保留欄位，可能從查詢頁面取得）
        /// </summary>
        public string FlightDate { get; set; }

        /// <summary>
        /// 預計到達日期（保留欄位，可能從查詢頁面取得）
        /// </summary>
        public string EstArrivalDate { get; set; }

        /// <summary>
        /// 查詢狀態
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 查詢訊息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 原始 HTML 內容（供除錯用）
        /// </summary>
        public string RawHtml { get; set; }
    }
}
