namespace Service.Services.FlightArrivalUpload.Domain
{
    /// <summary>
    /// 班機與派件送達工作表的單筆匯入資料。
    /// </summary>
    public sealed class FlightArrivalUploadArrivalModel
    {
        /// <summary>
        /// 匯入月份，格式為 MM。
        /// </summary>
        public string Month { get; set; } = string.Empty;

        /// <summary>
        /// 客戶名稱或轉換後的客戶代碼。
        /// </summary>
        public string CustId { get; set; } = string.Empty;

        /// <summary>
        /// 主提單號碼。
        /// </summary>
        public string MainNumber { get; set; } = string.Empty;

        /// <summary>
        /// 渠道代碼。
        /// </summary>
        public string LineCode { get; set; } = string.Empty;

        /// <summary>
        /// 航班代號。
        /// </summary>
        public string FlightNumber { get; set; } = string.Empty;

        /// <summary>
        /// 航班應到袋數。
        /// </summary>
        public string FlightCount { get; set; } = string.Empty;

        /// <summary>
        /// 第 1 組班機到達時間，保留原文字供日期驗證。
        /// </summary>
        public string ArriveDate1 { get; set; } = string.Empty;

        /// <summary>
        /// 第 2 組班機到達時間，保留原文字供日期驗證。
        /// </summary>
        public string ArriveDate2 { get; set; } = string.Empty;

        /// <summary>
        /// 第 3 組班機到達時間，保留原文字供日期驗證。
        /// </summary>
        public string ArriveDate3 { get; set; } = string.Empty;

        /// <summary>
        /// 第 4 組班機到達時間，保留原文字供日期驗證。
        /// </summary>
        public string ArriveDate4 { get; set; } = string.Empty;

        /// <summary>
        /// 第 5 組班機到達時間，保留原文字供日期驗證。
        /// </summary>
        public string ArriveDate5 { get; set; } = string.Empty;

        /// <summary>
        /// 派件公司名稱或轉換後的派件公司代碼。
        /// </summary>
        public string TransNo { get; set; } = string.Empty;

        /// <summary>
        /// 第 1 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate1 { get; set; } = string.Empty;

        /// <summary>
        /// 第 2 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate2 { get; set; } = string.Empty;

        /// <summary>
        /// 第 3 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate3 { get; set; } = string.Empty;

        /// <summary>
        /// 第 4 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate4 { get; set; } = string.Empty;

        /// <summary>
        /// 第 5 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate5 { get; set; } = string.Empty;

        /// <summary>
        /// 第 6 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate6 { get; set; } = string.Empty;

        /// <summary>
        /// 第 7 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate7 { get; set; } = string.Empty;

        /// <summary>
        /// 第 8 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate8 { get; set; } = string.Empty;

        /// <summary>
        /// 第 9 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate9 { get; set; } = string.Empty;

        /// <summary>
        /// 第 10 組派件公司送達時間，保留原文字供日期驗證。
        /// </summary>
        public string TransDate10 { get; set; } = string.Empty;

        /// <summary>
        /// 第 1 組派件公司送達袋數。
        /// </summary>
        public string TransCount1 { get; set; } = string.Empty;

        /// <summary>
        /// 第 2 組派件公司送達袋數。
        /// </summary>
        public string TransCount2 { get; set; } = string.Empty;

        /// <summary>
        /// 第 3 組派件公司送達袋數。
        /// </summary>
        public string TransCount3 { get; set; } = string.Empty;

        /// <summary>
        /// 第 4 組派件公司送達袋數。
        /// </summary>
        public string TransCount4 { get; set; } = string.Empty;

        /// <summary>
        /// 第 5 組派件公司送達袋數。
        /// </summary>
        public string TransCount5 { get; set; } = string.Empty;

        /// <summary>
        /// 第 6 組派件公司送達袋數。
        /// </summary>
        public string TransCount6 { get; set; } = string.Empty;

        /// <summary>
        /// 第 7 組派件公司送達袋數。
        /// </summary>
        public string TransCount7 { get; set; } = string.Empty;

        /// <summary>
        /// 第 8 組派件公司送達袋數。
        /// </summary>
        public string TransCount8 { get; set; } = string.Empty;

        /// <summary>
        /// 第 9 組派件公司送達袋數。
        /// </summary>
        public string TransCount9 { get; set; } = string.Empty;

        /// <summary>
        /// 第 10 組派件公司送達袋數。
        /// </summary>
        public string TransCount10 { get; set; } = string.Empty;

        /// <summary>
        /// 起運機場。
        /// </summary>
        public string Ori { get; set; } = string.Empty;

        /// <summary>
        /// 轉運機場。
        /// </summary>
        public string TransitAirport { get; set; } = string.Empty;

        /// <summary>
        /// 目的機場。
        /// </summary>
        public string Dest { get; set; } = string.Empty;
    }
}
