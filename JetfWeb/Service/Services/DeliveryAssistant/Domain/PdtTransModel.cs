namespace Service.Services.DeliveryAssistant.Domain
{
    public class PdtTransModel
    {
        /// <summary>
        /// 派件公司代碼（Key）
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int? Sort { get; set; }
    }
}
