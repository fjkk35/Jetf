namespace Service.Services.SeaTaxGUpload
{
    /// <summary>
    /// G 類海運稅金 Excel 上傳資料列。
    /// </summary>
    public sealed class SeaTaxGUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// 倉儲來源。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 海運客戶名稱。
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 由海運客戶名稱轉換後的客戶代號。
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 派送單號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int ClearanceFee { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 代收手續費。
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 應向物流代收總金額。
        /// </summary>
        public int ToDlvCod { get; set; }

        /// <summary>
        /// 應向派送端收取的金額。
        /// </summary>
        public int TransCod { get; set; }

        /// <summary>
        /// 應向客戶收取的金額。
        /// </summary>
        public int? CustomerCod { get; set; }

        /// <summary>
        /// 稅金類別。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 客戶名欄位內容，寫入收件人。
        /// </summary>
        public string Recipient { get; set; }

    }
}
