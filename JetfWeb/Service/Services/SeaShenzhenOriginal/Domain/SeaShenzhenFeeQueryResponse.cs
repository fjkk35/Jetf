using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞稅金資料查詢結果。
    /// </summary>
    public class SeaShenzhenFeeQueryResponse
    {
        public int TotalCount { get; set; }

        public List<SeaShenzhenFeeQueryRow> Data { get; set; }
    }

    /// <summary>
    /// 新遞稅金資料查詢列資料。
    /// </summary>
    public class SeaShenzhenFeeQueryRow
    {
        public int Id { get; set; }

        public string DataDateText { get; set; }

        public string CustomerName { get; set; }

        public string DlvCom { get; set; }

        public string TrackingNo { get; set; }

        public string DlvInv { get; set; }

        public string IncludeTaxDisplay { get; set; }

        public int Tax { get; set; }

        public int Cod { get; set; }

        public int Fee { get; set; }

        public int ToDlvCod { get; set; }
    }
}