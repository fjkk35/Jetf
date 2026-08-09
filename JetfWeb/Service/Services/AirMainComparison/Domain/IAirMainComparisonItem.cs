using System.Collections.Generic;

namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 可套用空運主號上傳比對規則的查詢結果。
    /// </summary>
    public interface IAirMainComparisonItem
    {
        /// <summary>
        /// 主號。
        /// </summary>
        string Mwb { get; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        string Customer { get; set; }

        /// <summary>
        /// 查詢錯誤訊息。
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// 申報件數。
        /// </summary>
        int DeclaredPiece { get; set; }

        /// <summary>
        /// 進倉件數。
        /// </summary>
        int GciPiece { get; set; }

        /// <summary>
        /// 併袋數量。
        /// </summary>
        int BagCount { get; }

        /// <summary>
        /// 進倉袋數量。
        /// </summary>
        int GciBagCount { get; }

        /// <summary>
        /// 未進倉件數。
        /// </summary>
        int NotGciPiece { get; set; }

        /// <summary>
        /// 未進倉袋數。
        /// </summary>
        int NotGciBag { get; }

        /// <summary>
        /// 未進倉小計。
        /// </summary>
        int NotGciTotal { get; set; }

        /// <summary>
        /// 完整主號查詢明細。
        /// </summary>
        IEnumerable<IAirMainQueryRow> QueryRows { get; }

        /// <summary>
        /// 未進倉明細。
        /// </summary>
        IEnumerable<IAirMainDetailRow> NotGciDetails { get; }

        /// <summary>
        /// 收單件數。
        /// </summary>
        int ReceivedPieceCount { get; set; }

        /// <summary>
        /// 未收單件數。
        /// </summary>
        int UnreceivedCount { get; set; }

        /// <summary>
        /// 未收單 B6F 件數。
        /// </summary>
        int UnreceivedB6FCount { get; set; }

        /// <summary>
        /// G 類無 ID 件數。
        /// </summary>
        int GTypeNoIdCount { get; set; }

        /// <summary>
        /// ZZZA 總數。
        /// </summary>
        int ZzzaCount { get; set; }

        /// <summary>
        /// ZZZA 進倉數。
        /// </summary>
        int ZzzaGciCount { get; set; }

        /// <summary>
        /// ZZZA 收單數。
        /// </summary>
        int ZzzaReceivedCount { get; set; }

        /// <summary>
        /// ZZZA 未收單數。
        /// </summary>
        int ZzzaUnreceivedCount { get; set; }

        /// <summary>
        /// 派件公司件數。
        /// </summary>
        Dictionary<string, int> TransNameCounts { get; set; }

        /// <summary>
        /// 派件公司件數摘要。
        /// </summary>
        string TransNameSummary { get; set; }

        /// <summary>
        /// 需要補列的未收單資料。
        /// </summary>
        List<AirMainUploadExcelRow> UnreceivedRows { get; set; }
    }
}
