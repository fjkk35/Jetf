namespace Service.Models.CptTradeVan
{
    /// <summary>
    /// GB350 空運進口貨物新艙單明細資料列。
    /// </summary>
    public class Gb350DetailGridModel
    {
        /// <summary>
        /// 分號。
        /// </summary>
        public string HAWB { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        public double WEIGHT { get; set; }

        /// <summary>
        /// 袋數。
        /// </summary>
        public int QTY { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string REMARK { get; set; }

        /// <summary>
        /// 分艙單收單註記。
        /// </summary>
        public string SOURCE_NOTE { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        public string POUCH_NO { get; set; }
    }
}
