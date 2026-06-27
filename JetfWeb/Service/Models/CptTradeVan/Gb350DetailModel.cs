using System.Collections.Generic;

namespace Service.Models.CptTradeVan
{
    /// <summary>
    /// GB350 空運進口貨物新艙單明細查詢結果。
    /// </summary>
    public class Gb350DetailModel
    {
        /// <summary>
        /// 總頁數。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 搜尋操作。
        /// </summary>
        public string SearchOper { get; set; }

        /// <summary>
        /// 搜尋字串。
        /// </summary>
        public string SearchString { get; set; }

        /// <summary>
        /// 查詢狀態。
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 樣式類別名稱。
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// 明細查詢資料列。
        /// </summary>
        public List<Gb350DetailGridModel> GridModel { get; set; }

        /// <summary>
        /// 搜尋欄位。
        /// </summary>
        public string SearchField { get; set; }

        /// <summary>
        /// 排序欄位。
        /// </summary>
        public string Sidx { get; set; }

        /// <summary>
        /// 查詢訊息。
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 是否一次載入。
        /// </summary>
        public bool LoadOnce { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int Rows { get; set; }

        /// <summary>
        /// 系統訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 排序方向。
        /// </summary>
        public string Sord { get; set; }

        /// <summary>
        /// 目前頁碼。
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 總筆數。
        /// </summary>
        public int Records { get; set; }

        /// <summary>
        /// 附加資料物件。
        /// </summary>
        public object DataObject { get; set; }
    }
}
