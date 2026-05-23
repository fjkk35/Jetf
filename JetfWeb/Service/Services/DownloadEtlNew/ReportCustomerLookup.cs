namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示報表輸出時使用的客戶與物流對照資料。
    /// </summary>
    internal sealed class ReportCustomerLookup
    {
        /// <summary>
        /// 取得或設定客戶代碼。
        /// </summary>
        public string CustId { get; set; }

        /// <summary>
        /// 取得或設定物流代碼。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 取得或設定物流名稱。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 取得或設定公司名稱。
        /// </summary>
        public string Company { get; set; }
    }
}