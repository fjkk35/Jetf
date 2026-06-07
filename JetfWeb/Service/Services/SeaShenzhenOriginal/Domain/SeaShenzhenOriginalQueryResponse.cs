using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞託運資料查詢結果。
    /// </summary>
    public class SeaShenzhenOriginalQueryResponse
    {
        public int TotalCount { get; set; }

        public List<SeaShenzhenOriginalQueryRow> Data { get; set; }
    }

    /// <summary>
    /// 新遞託運資料查詢列資料。
    /// </summary>
    public class SeaShenzhenOriginalQueryRow
    {
        public int Id { get; set; }

        public string DataDateText { get; set; }

        public string TrackingNo { get; set; }

        public string BlNo { get; set; }

        public string OrderNo { get; set; }

        public string JetfSerial { get; set; }

        public string TransTimeText { get; set; }

        public string TransName { get; set; }

        public string Importer { get; set; }

        public string ImporterAddress { get; set; }

        public string ImporterPhone { get; set; }

        public string ItemName { get; set; }

        public string CcText { get; set; }

        public string QuantityText { get; set; }

        public string GwText { get; set; }

        public string Memo { get; set; }

        public string Claimant { get; set; }

        public string TaxPayment { get; set; }
    }
}
