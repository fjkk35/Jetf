using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ExportClearance
{
    public class ExportClearanceInfoModel
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string MawbNo { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string CustHawbNo { get; set; }
        
        /// <summary>
        /// 報關類別
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 併袋號
        /// </summary>
        public string MergeNumber { get; set; }
        /// <summary>
        /// 報單號碼
        /// </summary>
        public string ClearanceNumber { get; set; }
        /// <summary>
        /// 通關方式
        /// </summary>
        public string ClearanceModel { get; set; }
        /// <summary>
        /// 申報件數
        /// </summary>
        public string DeclaredPiece { get; set; }
        /// <summary>
        /// 進倉件數
        /// </summary>
        public string InboundPiece { get; set; }
        /// <summary>
        /// 出倉件數
        /// </summary>
        public string OutboundPiece { get; set; }
        /// <summary>
        /// 申報重量
        /// </summary>
        public string DeclaredWeight { get; set; }
        /// <summary>
        /// 進倉重量
        /// </summary>
        public string InboundWeight { get; set; }
        /// <summary>
        /// 進倉時間
        /// </summary>
        public string SignInTime { get; set; }
        /// <summary>
        /// 出倉時間
        /// </summary>
        public string SignOutTime { get; set; }
        /// <summary>
        /// 航機班次
        /// </summary>
        public string FltNo { get; set; }
        /// <summary>
        /// 更改後報單號
        /// </summary>
        public string AmendClearanceNumber { get; set; }
        /// <summary>
        /// 稅費金額
        /// </summary>
        public string Tax { get; set; }
    }
}
