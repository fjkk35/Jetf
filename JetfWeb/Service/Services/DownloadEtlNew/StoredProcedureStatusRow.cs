namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示 stored procedure 執行後回傳的狀態資料。
    /// </summary>
    internal sealed class StoredProcedureStatusRow
    {
        /// <summary>
        /// 取得或設定執行狀態碼。
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 取得或設定執行結果訊息。
        /// </summary>
        public string Message { get; set; }
    }
}