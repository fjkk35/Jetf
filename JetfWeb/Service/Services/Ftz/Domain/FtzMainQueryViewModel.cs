using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// Ftz 主號查詢結果（用於前端展示）
    /// </summary>
    public class FtzMainQueryViewModel
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string Mwb { get; set; }

        /// <summary>
        /// 分號
        /// </summary>
        public string HwbCount { get; set; }

        /// <summary>
        /// 申報
        /// </summary>
        public string HwbPiece { get; set; }

        /// <summary>
        /// 進倉
        /// </summary>
        public string HwbGciPiece { get; set; }

        /// <summary>
        /// 出倉
        /// </summary>
        public string HwbGcoPiece { get; set; }

        /// <summary>
        /// 進倉重量
        /// </summary>
        public string GciWeight { get; set; }

        /// <summary>
        /// 未進倉 (申報 - 進倉)
        /// </summary>
        public int NotGciPiece { get; set; }

        /// <summary>
        /// 併袋
        /// </summary>
        public string ExpBagCount { get; set; }

        /// <summary>
        /// 進倉袋
        /// </summary>
        public string ExpBagGciCount { get; set; }

        /// <summary>
        /// 出倉袋
        /// </summary>
        public string ExpBagGcoCount { get; set; }

        /// <summary>
        /// 未進倉袋 (併袋 - 進倉袋)
        /// </summary>
        public int NotGciBag { get; set; }

        /// <summary>
        /// 未進倉小計 (未進倉 + 未進倉袋)
        /// </summary>
        public int NotGciTotal { get; set; }

        /// <summary>
        /// 收單件數。
        /// </summary>
        public int ReceivedPieceCount { get; set; }

        /// <summary>
        /// 排除 ZZZA 及無派件公司後的未收單件數。
        /// </summary>
        public int UnreceivedCount { get; set; }

        /// <summary>
        /// 未收單且狀態為 G類無ID 的筆數。
        /// </summary>
        public int GTypeNoIdCount { get; set; }

        /// <summary>
        /// ZZZA 總筆數。
        /// </summary>
        public int ZzzaCount { get; set; }

        /// <summary>
        /// ZZZA 進倉筆數。
        /// </summary>
        public int ZzzaGciCount { get; set; }

        /// <summary>
        /// ZZZA 收單筆數。
        /// </summary>
        public int ZzzaReceivedCount { get; set; }

        /// <summary>
        /// ZZZA 未收單筆數。
        /// </summary>
        public int ZzzaUnreceivedCount { get; set; }

        /// <summary>
        /// 排除 ZZZA 後的派件公司件數。
        /// </summary>
        public Dictionary<string, int> TransNameCounts { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 需要補列到未進倉明細的未收單資料。
        /// </summary>
        public List<FtzMainUploadExcelRow> UnreceivedRows { get; set; } = new List<FtzMainUploadExcelRow>();

        /// <summary>
        /// 未收單B6F筆數
        /// </summary>
        public int UnreceivedB6FCount { get; set; }

        /// <summary>
        /// 未進倉申報袋號
        /// </summary>
        public string NotGciPieceExpBagNo { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 航班
        /// </summary>
        public string FlightNumber { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }

        public List<Row> NotGciDetails { get; set; }

        /// <summary>
        /// 原始 API 回應資料
        /// </summary>
        public FtzMainQueryResult RawData { get; set; }
    }
}
