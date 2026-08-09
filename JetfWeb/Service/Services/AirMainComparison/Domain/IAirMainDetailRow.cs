namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 空運主號查詢的未進倉明細資料。
    /// </summary>
    public interface IAirMainDetailRow
    {
        /// <summary>
        /// 分提單號。
        /// </summary>
        string Hwb { get; }

        /// <summary>
        /// 併袋號。
        /// </summary>
        string BagNo { get; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        string DeclNo { get; }

        /// <summary>
        /// 申報件數。
        /// </summary>
        int DeclaredPiece { get; }

        /// <summary>
        /// 進倉件數。
        /// </summary>
        int GciPiece { get; }

        /// <summary>
        /// 出倉件數。
        /// </summary>
        int GcoPiece { get; }

        /// <summary>
        /// 報關類別。
        /// </summary>
        string DeclType { get; }

        /// <summary>
        /// 備註。
        /// </summary>
        string Remarks { get; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        string TransName { get; set; }

        /// <summary>
        /// AIR_DETAIN 狀態。
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// ZZZA 註記。
        /// </summary>
        string ZzzaRemark { get; set; }
    }
}
