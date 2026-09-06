using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞託運資料查詢結果。
    /// </summary>
    public class SeaShenzhenOriginalQueryResponse
    {
        /// <summary>
        /// 查詢總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當前頁列資料。
        /// </summary>
        public List<SeaShenzhenOriginalQueryRow> Data { get; set; }
    }

    /// <summary>
    /// 新遞託運資料查詢列資料。
    /// </summary>
    public class SeaShenzhenOriginalQueryRow
    {
        /// <summary>
        /// 主鍵。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        public string DataDateText { get; set; }

        /// <summary>
        /// 報關行。
        /// </summary>
        public string DataTypeDisplay { get; set; }

        /// <summary>
        /// 報關號碼。
        /// </summary>
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

        /// <summary>
        /// 稅金。
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int? Fee { get; set; }

        public string QuantityText { get; set; }

        /// <summary>
        /// 材積。
        /// </summary>
        public decimal? Volume { get; set; }

        public string GwText { get; set; }

        public string Memo { get; set; }

        public string Claimant { get; set; }

        public string TaxPayment { get; set; }
    }
}
