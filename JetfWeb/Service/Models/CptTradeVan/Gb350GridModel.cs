namespace Service.Models.CptTradeVan
{
    /// <summary>
    /// GB350 空運進口貨物新艙單主號查詢資料列。
    /// </summary>
    public class Gb350GridModel
    {
        /// <summary>
        /// 總件數。
        /// </summary>
        public int TOT_PACK_QTY { get; set; }

        /// <summary>
        /// 存倉關別。
        /// </summary>
        public string STORE_WARE_CD { get; set; }

        /// <summary>
        /// 傳輸時間。
        /// </summary>
        public string TRANS_DATE { get; set; }

        /// <summary>
        /// 航班號碼。
        /// </summary>
        public string VOYAGE_FLIGHT_NO { get; set; }

        /// <summary>
        /// 傳輸業者統一編號。
        /// </summary>
        public string TRANS_BAN { get; set; }

        /// <summary>
        /// 錯誤訊息。
        /// </summary>
        public object ERROR_MSG { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        public string STATUS { get; set; }

        /// <summary>
        /// 進口日期。
        /// </summary>
        public string IMPORT_DATE { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MAWB { get; set; }

        /// <summary>
        /// 主號明細查詢結果。
        /// </summary>
        public Gb350DetailModel Detail { get; set; }
    }
}
