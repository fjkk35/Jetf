namespace Service.Models.SeaMainNumberShippingDetails
{
    /// <summary>
    /// 海運主號託運明細表下載檔案資料。
    /// </summary>
    public sealed class SeaMainNumberShippingDetailsDownloadFile
    {
        /// <summary>
        /// 下載檔案名稱。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 下載檔案內容。
        /// </summary>
        public byte[] FileBytes { get; set; }
    }
}
