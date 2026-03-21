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
        /// B6F件數
        /// </summary>
        public int B6FCount { get; set; }

        /// <summary>
        /// B6F分號
        /// </summary>
        public string B6FHwb { get; set; }

        /// <summary>
        /// 未進倉件不含B6F分號
        /// </summary>
        public string NotGciPieceNotB6F { get; set; }

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
