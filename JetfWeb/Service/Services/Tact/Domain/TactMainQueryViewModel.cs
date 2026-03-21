using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 主號查詢結果
    /// </summary>
    public class TactMainQueryViewModel
    {
        public TactMainQueryViewModel()
        {
            NotGciDetails = new List<TactMainNoDetailModel>();
        }

        /// <summary>
        /// 主號
        /// </summary>
        public string Mwb { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 航班
        /// </summary>
        public string FlightNumber { get; set; }

        /// <summary>
        /// 申報件數（以分號申報）
        /// </summary>
        public int? Piece { get; set; }

        /// <summary>
        /// 進倉件數
        /// </summary>
        public int? GciPiece { get; set; }

        /// <summary>
        /// 出倉件數
        /// </summary>
        public int? GcoPiece { get; set; }

        /// <summary>
        /// 未進倉件數
        /// </summary>
        public int? NotGciPiece { get; set; }

        /// <summary>
        /// 進倉重量
        /// </summary>
        public double? GciWeight { get; set; }

        /// <summary>
        /// 以分號申報數量
        /// </summary>
        public int? TrackingNo { get; set; }

        /// <summary>
        /// 併袋數量
        /// </summary>
        public int? BagNumber { get; set; }

        /// <summary>
        /// 進倉袋數
        /// </summary>
        public int? GciBagNumber { get; set; }

        /// <summary>
        /// 出倉袋數
        /// </summary>
        public int? GcoBagNumber { get; set; }

        /// <summary>
        /// 未進倉袋數
        /// </summary>
        public int? NotGciBagNumber { get; set; }

        /// <summary>
        /// 未進倉小計
        /// </summary>
        public int NotGciPieceCount { get; set; }

        /// <summary>
        /// B6F 數量
        /// </summary>
        public int B6FCount { get; set; }

        /// <summary>
        /// B6F 分號（逗號分隔）
        /// </summary>
        public string B6FTrackingNo { get; set; }

        /// <summary>
        /// 未進倉件不含B6F分號（逗號分隔）
        /// </summary>
        public string NotGciPieceNotB6F { get; set; }

        /// <summary>
        /// 未進倉申報袋號（逗號分隔）
        /// </summary>
        public string NotGciPieceBagNumber { get; set; }

        /// <summary>
        /// 未進倉明細
        /// </summary>
        public List<TactMainNoDetailModel> NotGciDetails { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
