using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// Ftz 查詢結果
    /// </summary>
    public class FtzQueryResult
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string Mwb { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        public string DeclType { get; set; }

        /// <summary>
        /// 通關方式
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 箱號
        /// </summary>
        public string BoxNo { get; set; }

        /// <summary>
        /// 進出口別
        /// </summary>
        public string IE { get; set; }

        /// <summary>
        /// 放行時間
        /// </summary>
        public string ReleaseTime { get; set; }

        /// <summary>
        /// 公司編號
        /// </summary>
        public string BoxNoExpressId { get; set; }

        /// <summary>
        /// 申報件數
        /// </summary>
        public string Piece { get; set; }

        /// <summary>
        /// 申報重量
        /// </summary>
        public string Weight { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public string GciDate1 { get; set; }

        /// <summary>
        /// 公司名稱
        /// </summary>
        public string BoxNoExpressCName { get; set; }

        /// <summary>
        /// 進倉件數
        /// </summary>
        public string GciPiece { get; set; }

        /// <summary>
        /// 進倉重量
        /// </summary>
        public string GciWeight { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public string GcoDate1 { get; set; }

        /// <summary>
        /// 出倉件數
        /// </summary>
        public string GcoPiece { get; set; }

        /// <summary>
        /// 查詢號碼（原始輸入）
        /// </summary>
        public string Hwbq { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }
    }
}
