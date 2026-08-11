namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳紀錄與費用主檔的明細關聯。
    /// </summary>
    public sealed class ReconciliationLogisticsFeeMasterLink
    {
        /// <summary>費用明細識別碼。</summary>
        public int DetailId { get; set; }

        /// <summary>費用主檔識別碼。</summary>
        public int FeeMasterId { get; set; }

        /// <summary>物流銷帳紀錄識別碼。</summary>
        public int? ReconciliationLogisticsId { get; set; }
    }
}
