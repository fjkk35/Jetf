namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳單號與費用主檔識別碼的比對資料。
    /// </summary>
    public sealed class ReconciliationLogisticsFeeMasterMatch
    {
        /// <summary>
        /// 費用主檔識別碼。
        /// </summary>
        public int FeeMasterId { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }
    }
}
