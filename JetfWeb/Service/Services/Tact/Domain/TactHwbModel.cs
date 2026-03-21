using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 查詢結果
    /// </summary>
    public class TactHwbModel
    {
        /// <summary>
        /// 主提單號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 報關類別
        /// </summary>
        public string DeclType { get; set; }

        /// <summary>
        /// 併袋號
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 通關方式
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 申報件數
        /// </summary>
        public string Piece { get; set; }

        /// <summary>
        /// 進倉件數
        /// </summary>
        public string GciPiece { get; set; }

        /// <summary>
        /// 出倉件數
        /// </summary>
        public string GcoPiece { get; set; }

        /// <summary>
        /// 申報重量
        /// </summary>
        public string Weight { get; set; }

        /// <summary>
        /// 進倉重量
        /// </summary>
        public string GciWeight { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public string GciDate1 { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public string GcoDate1 { get; set; }

        /// <summary>
        /// 航機班次
        /// </summary>
        public string FlightNo { get; set; }

        /// <summary>
        /// 更改後報單
        /// </summary>
        public string UpdateDecl { get; set; }

        /// <summary>
        /// 稅費金額
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 備註2
        /// </summary>
        public string Remark2 { get; set; }
    }
}
