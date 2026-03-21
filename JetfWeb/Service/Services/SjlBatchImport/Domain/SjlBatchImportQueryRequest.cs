namespace Service.Services.SjlBatchImport.Domain
{
    /// <summary>
    /// 捷利托運資料查詢條件。
    /// </summary>
    public class SjlBatchImportQueryRequest
    {
        /// <summary>
        /// 運送編號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 單據編號。
        /// </summary>
        public string BagNumber { get; set; }
    }
}
