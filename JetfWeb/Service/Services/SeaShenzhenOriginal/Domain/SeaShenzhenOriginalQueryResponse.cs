using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞託運資料查詢結果。
    /// </summary>
    public class SeaShenzhenOriginalQueryResponse
    {
        /// <summary>
        /// 查詢總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當前頁列資料。
        /// </summary>
        public List<SeaShenzhenOriginalQueryRow> Data { get; set; }
    }
}
