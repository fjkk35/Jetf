using Service.Services.AirMainComparison.Domain;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 主號查詢明細資料。
    /// </summary>
    public class MainRow : IAirMainQueryRow
    {
        /// <summary>
        /// 分提單號。
        /// </summary>
        public string Hwb { get; set; }

        /// <summary>
        /// 併袋號。
        /// </summary>
        public string ExpBagNo { get; set; }
    }
}
