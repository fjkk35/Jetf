namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 代收金額人工調整上傳列。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 原始文字代收金額。
        /// </summary>
        public string ToDlvCodText { get; set; }

        /// <summary>
        /// 解析後的代收金額。
        /// </summary>
        public int? ToDlvCod { get; set; }

        /// <summary>
        /// 對應的分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 上傳狀態。
        /// </summary>
        public string UploadStatus { get; set; }

        /// <summary>
        /// 失敗欄位名稱。
        /// </summary>
        public string FailFieldName { get; set; }

        /// <summary>
        /// 失敗原因。
        /// </summary>
        public string FailReason { get; set; }
    }
}