namespace Service.Models.SeaCustomerShippingDetails
{
    /// <summary>
    /// 海運客戶託運明細表下載檔案資料。
    /// </summary>
    public sealed class SeaCustomerShippingDetailsDownloadFile
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
