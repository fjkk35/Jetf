using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 代收金額人工調整查詢結果。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodQueryResponse
    {
        /// <summary>
        /// 查詢總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當前頁列資料。
        /// </summary>
        public List<SeaShenzhenFeeManualToDlvCodQueryRow> Data { get; set; }
    }

    /// <summary>
    /// 代收金額人工調整查詢列資料。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodQueryRow
    {
        /// <summary>
        /// 主鍵。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 託運單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 代收金額。
        /// </summary>
        public int ToDlvCod { get; set; }

        /// <summary>
        /// 上傳時間。
        /// </summary>
        public string CreatedTimeText { get; set; }

        /// <summary>
        /// 上傳人員。
        /// </summary>
        public string CreatedUser { get; set; }
    }
}