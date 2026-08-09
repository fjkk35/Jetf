using Service.Services.AirMainComparison.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 主號查詢及上傳比對結果。
    /// </summary>
    public class TactMainQueryViewModel : IAirMainComparisonItem
    {
        /// <summary>
        /// 建立空的查詢結果。
        /// </summary>
        public TactMainQueryViewModel()
        {
            Rows = new List<MainRow>();
            NotGciDetails = new List<TactMainNoDetailModel>();
            TransNameCounts = new Dictionary<string, int>();
            UnreceivedRows = new List<AirMainUploadExcelRow>();
        }

        /// <summary>主號明細。</summary>
        public List<MainRow> Rows { get; set; }

        /// <summary>主號。</summary>
        public string Mwb { get; set; }

        /// <summary>客戶名稱。</summary>
        public string Customer { get; set; }

        /// <summary>申報件數。</summary>
        public int? Piece { get; set; }

        /// <summary>進倉件數。</summary>
        public int? GciPiece { get; set; }

        /// <summary>出倉件數。</summary>
        public int? GcoPiece { get; set; }

        /// <summary>未進倉件數。</summary>
        public int? NotGciPiece { get; set; }

        /// <summary>進倉重量。</summary>
        public double? GciWeight { get; set; }

        /// <summary>分號數量。</summary>
        public int? TrackingNo { get; set; }

        /// <summary>併袋數量。</summary>
        public int? BagNumber { get; set; }

        /// <summary>進倉袋數。</summary>
        public int? GciBagNumber { get; set; }

        /// <summary>出倉袋數。</summary>
        public int? GcoBagNumber { get; set; }

        /// <summary>未進倉袋數。</summary>
        public int? NotGciBagNumber { get; set; }

        /// <summary>未進倉小計。</summary>
        public int NotGciPieceCount { get; set; }

        /// <summary>未進倉明細。</summary>
        public List<TactMainNoDetailModel> NotGciDetails { get; set; }

        /// <summary>錯誤訊息。</summary>
        public string ErrorMessage { get; set; }

        /// <summary>收單件數。</summary>
        public int ReceivedPieceCount { get; set; }

        /// <summary>排除 ZZZA 及無派件公司後的未收單件數。</summary>
        public int UnreceivedCount { get; set; }

        /// <summary>符合 PLINK 錯單代碼的未收單筆數。</summary>
        public int UnreceivedB6FCount { get; set; }

        /// <summary>G 類無 ID 筆數。</summary>
        public int GTypeNoIdCount { get; set; }

        /// <summary>ZZZA 總筆數。</summary>
        public int ZzzaCount { get; set; }

        /// <summary>ZZZA 進倉筆數。</summary>
        public int ZzzaGciCount { get; set; }

        /// <summary>ZZZA 收單筆數。</summary>
        public int ZzzaReceivedCount { get; set; }

        /// <summary>ZZZA 未收單筆數。</summary>
        public int ZzzaUnreceivedCount { get; set; }

        /// <summary>派件公司件數。</summary>
        public Dictionary<string, int> TransNameCounts { get; set; }

        /// <summary>派件公司件數摘要。</summary>
        public string TransNameSummary { get; set; }

        /// <summary>需要補列到明細的未收單資料。</summary>
        public List<AirMainUploadExcelRow> UnreceivedRows { get; set; }

        int IAirMainComparisonItem.DeclaredPiece
        {
            get => Piece ?? 0;
            set => Piece = value;
        }

        int IAirMainComparisonItem.GciPiece
        {
            get => GciPiece ?? 0;
            set => GciPiece = value;
        }

        int IAirMainComparisonItem.BagCount => BagNumber ?? 0;
        int IAirMainComparisonItem.GciBagCount => GciBagNumber ?? 0;

        int IAirMainComparisonItem.NotGciPiece
        {
            get => NotGciPiece ?? 0;
            set => NotGciPiece = value;
        }

        int IAirMainComparisonItem.NotGciBag => NotGciBagNumber ?? 0;

        int IAirMainComparisonItem.NotGciTotal
        {
            get => NotGciPieceCount;
            set => NotGciPieceCount = value;
        }

        IEnumerable<IAirMainQueryRow> IAirMainComparisonItem.QueryRows =>
            Rows?.Cast<IAirMainQueryRow>() ?? Enumerable.Empty<IAirMainQueryRow>();

        IEnumerable<IAirMainDetailRow> IAirMainComparisonItem.NotGciDetails =>
            NotGciDetails?.Cast<IAirMainDetailRow>() ?? Enumerable.Empty<IAirMainDetailRow>();
    }
}
