namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞深圳稅單 Excel 上傳列資料。
    /// </summary>
    public class SeaShenzhenTaxUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 託運單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 到付金額原始文字。
        /// </summary>
        public string CodText { get; set; }

        /// <summary>
        /// 到付金額。
        /// </summary>
        public int? Cod { get; set; }

        /// <summary>
        /// 稅金金額原始文字。
        /// </summary>
        public string TaxText { get; set; }

        /// <summary>
        /// 稅金金額。
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 稅金手續費原始文字。
        /// </summary>
        public string FeeText { get; set; }

        /// <summary>
        /// 稅金手續費。
        /// </summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 上傳結果。
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
