namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞稅金資料查詢條件。
    /// </summary>
    public class SeaShenzhenFeeQueryRequest
    {
        public string DataDateStart { get; set; }

        public string DataDateEnd { get; set; }

        public string TrackingNo { get; set; }

        public string DlvInv { get; set; }

        public string IncludeTax { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }
    }
}