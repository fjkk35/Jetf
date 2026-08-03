namespace Service.Services.DeliveryAssistant.Domain
{
    /// <summary>
    /// 派送助理匯出查詢條件。
    /// </summary>
    public class DeliveryAssistantRequest
    {
        /// <summary>
        /// 單號列表（每行一筆）。
        /// </summary>
        public string OrderNoList { get; set; }

        /// <summary>
        /// 作業地區（DataType）
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 派件公司（TransNo）
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 查詢日期（起），格式 yyyy-MM-dd
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// 查詢日期（迄），格式 yyyy-MM-dd
        /// </summary>
        public string EndDate { get; set; }
    }
}
