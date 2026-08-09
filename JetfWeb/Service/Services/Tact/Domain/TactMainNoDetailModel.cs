using Service.Services.AirMainComparison.Domain;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 主號未進倉明細。
    /// </summary>
    public class TactMainNoDetailModel : IAirMainDetailRow
    {
        /// <summary>分提單號。</summary>
        public string TrackingNo { get; set; }

        /// <summary>報關類別。</summary>
        public string DeclType { get; set; }

        /// <summary>併袋號。</summary>
        public string BagNumber { get; set; }

        /// <summary>報單號碼。</summary>
        public string DeclNo { get; set; }

        /// <summary>通關方式。</summary>
        public string ClearanceType { get; set; }

        /// <summary>申報件數。</summary>
        public int Piece { get; set; }

        /// <summary>進倉件數。</summary>
        public int GciPiece { get; set; }

        /// <summary>出倉件數。</summary>
        public int GcoPiece { get; set; }

        /// <summary>申報重量。</summary>
        public string Weight { get; set; }

        /// <summary>進倉重量。</summary>
        public string GciWeight { get; set; }

        /// <summary>進倉時間或狀態。</summary>
        public string GciDate1 { get; set; }

        /// <summary>出倉時間。</summary>
        public string GcoDate1 { get; set; }

        /// <summary>航機班次。</summary>
        public string FlightNo { get; set; }

        /// <summary>更改後報單號。</summary>
        public string UpdateDecl { get; set; }

        /// <summary>稅費金額。</summary>
        public string Amount { get; set; }

        /// <summary>派件公司。</summary>
        public string TransName { get; set; }

        /// <summary>AIR_DETAIN 狀態。</summary>
        public string Status { get; set; }

        /// <summary>上傳明細的 ZZZA 註記。</summary>
        public string ZzzaRemark { get; set; }

        string IAirMainDetailRow.Hwb => TrackingNo;
        string IAirMainDetailRow.BagNo => BagNumber;
        string IAirMainDetailRow.DeclNo => DeclNo;
        int IAirMainDetailRow.DeclaredPiece => Piece;
        int IAirMainDetailRow.GciPiece => GciPiece;
        int IAirMainDetailRow.GcoPiece => GcoPiece;
        string IAirMainDetailRow.DeclType => DeclType;
        string IAirMainDetailRow.Remarks => string.Empty;
    }
}
