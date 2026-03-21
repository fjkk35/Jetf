namespace Service.Services.DeliveryAssistant.Domain
{
    public class DeliveryAssistantRequest
    {
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
