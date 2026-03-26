namespace Service.Services.SjlBatchImport.Domain
{
    /// <summary>
    /// 捷利托運資料查詢條件。
    /// </summary>
    public class SjlBatchImportSearchRequest
    {
        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public string JetfSerial { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}