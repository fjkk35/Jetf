using iTextSharp.text;
using System.Collections.Generic;

namespace Service.Services.ShipmentInboundRecord.Domain
{
    public class ShipmentInboundRecordRequest
    {
        /// <summary>
        /// 入庫日期(起)
        /// </summary>
        public string InboundDateStart { get; set; }

        /// <summary>
        /// 入庫日期(迄)
        /// </summary>
        public string InboundDateEnd { get; set; }

        /// <summary>
        /// 處理方式
        /// </summary>
        public string ProcessType { get; set; }

        /// <summary>
        /// 儲位
        /// </summary>
        public string LocationCode { get; set; }

        /// <summary>
        /// 客戶代碼(單一，保留舊版相容)
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶代碼(多選)
        /// </summary>
        public List<string> CustCodes { get; set; }

        /// <summary>
        /// 貨件來源
        /// </summary>
        public string SourceType { get; set; }

        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 進口方式(海運/空運)
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 倉庫狀態
        /// </summary>
        public string WarehouseProcessType { get; set; }

        /// <summary>
        /// 是否只查倉庫狀態沒有值
        /// </summary>
        public bool WarehouseProcessTypeIsEmpty { get; set; }

        /// <summary>
        /// 是否原始貨件
        /// </summary>
        public bool? IsOrderOriginal { get; set; }

        /// <summary>
        /// 目前頁碼
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PageSize { get; set; }
    }
}
