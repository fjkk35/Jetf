namespace JETFTAX.Models
{
    /// <summary>
    /// 海運客戶託運明細表查詢條件。
    /// </summary>
    public class SeaCustomerShippingDetailsRequest
    {
        /// <summary>
        /// 出倉日起日。
        /// </summary>
        public string SDate { get; set; }

        /// <summary>
        /// 出倉日迄日。
        /// </summary>
        public string EDate { get; set; }

        /// <summary>
        /// 倉別。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string DespatchName { get; set; }
    }
}
