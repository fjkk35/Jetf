namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 代收金額人工調整上傳列。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodUploadRow
    {
        public int RowNo { get; set; }

        public string DlvInv { get; set; }

        public string ToDlvCodText { get; set; }

        public int? ToDlvCod { get; set; }

        public string TrackingNo { get; set; }

        public string UploadStatus { get; set; }

        public string FailFieldName { get; set; }

        public string FailReason { get; set; }
    }
}