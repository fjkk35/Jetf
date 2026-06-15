namespace JETFTAX.Models
{
    /// <summary>
    /// 海運主號託運明細表(無稅金)查詢條件。
    /// </summary>
    public class SeaMainNumberShippingDetailsRequest
    {
        /// <summary>
        /// 主號清單，支援換行輸入多筆。
        /// </summary>
        public string MainNumbers { get; set; }
    }
}