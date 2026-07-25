namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳查詢結果單筆資料。
    /// </summary>
    public sealed class ReconciliationLogisticsListItem
    {
        /// <summary>
        /// 物流銷帳識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 回款日期，格式為 yyyy/MM/dd。
        /// </summary>
        public string RepaymentDate { get; set; }

        /// <summary>
        /// 物流公司名稱。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 客戶代號。
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 回款金額。
        /// </summary>
        public int ReceivedAmount { get; set; }

        /// <summary>
        /// 應收金額減去回款金額的差異。
        /// </summary>
        public int DifferenceAmount { get; set; }

        /// <summary>
        /// 銷帳狀態。
        /// </summary>
        public string Status { get; set; }
    }
}
