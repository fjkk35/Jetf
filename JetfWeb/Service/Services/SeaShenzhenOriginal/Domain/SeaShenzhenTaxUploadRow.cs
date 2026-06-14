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
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 分號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅單金額原始文字。
        /// </summary>
        public string TaxText { get; set; }

        /// <summary>
        /// 稅單金額。
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 納稅人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 統編。
        /// </summary>
        public string TaxRecId { get; set; }

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