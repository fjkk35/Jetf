using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞稅金資料查詢結果。
    /// </summary>
    public class SeaShenzhenFeeQueryResponse
    {
        /// <summary>
        /// 查詢總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當前頁列資料。
        /// </summary>
        public List<SeaShenzhenFeeQueryRow> Data { get; set; }
    }

    /// <summary>
    /// 新遞稅金資料查詢列資料。
    /// </summary>
    public class SeaShenzhenFeeQueryRow
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
        /// 客戶名稱。
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 報關行。
        /// </summary>
        public string DataTypeDisplay { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string DlvCom { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅金支付方式顯示文字。
        /// </summary>
        public string IncludeTaxDisplay { get; set; }

        /// <summary>
        /// 稅金金額。
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 稅金手續費。
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 物流代收金額。
        /// </summary>
        public int ToDlvCod { get; set; }
    }
}
