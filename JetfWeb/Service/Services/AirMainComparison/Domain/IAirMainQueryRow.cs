namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 空運主號查詢的完整明細識別資料。
    /// </summary>
    public interface IAirMainQueryRow
    {
        /// <summary>
        /// 分提單號。
        /// </summary>
        string Hwb { get; }

        /// <summary>
        /// 併袋號。
        /// </summary>
        string ExpBagNo { get; }
    }
}
