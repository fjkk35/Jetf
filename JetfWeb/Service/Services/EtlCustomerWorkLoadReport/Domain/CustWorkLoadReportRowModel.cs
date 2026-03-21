using Service.Models.EtlCustWorkLoad;
using System;
using System.Collections.Generic;

namespace Service.Services.EtlCustomerWorkLoadReport.Domain
{
    /// <summary>
    /// 空快客戶作業量報表(博豐格式)每列資料模型
    /// </summary>
    public class CustWorkLoadReportRowModel
    {
        /// <summary>
        /// 派件公司編號
        /// </summary>
        public int? TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string Mainnumber { get; set; }

        /// <summary>
        /// 原單袋數
        /// </summary>
        public int TotalBlNo { get; set; }

        /// <summary>
        /// 原單件數
        /// </summary>
        public int TotalPiece { get; set; }

        /// <summary>
        /// GW (毛重)
        /// </summary>
        public double TotalGW { get; set; }

        /// <summary>
        /// 入倉袋數
        /// </summary>
        public int TotalInBlNo { get; set; }

        /// <summary>
        /// 出倉袋數
        /// </summary>
        public int TotalOutBlNo { get; set; }

        /// <summary>
        /// C3袋數(已入倉 未出倉)
        /// </summary>
        public int TotalC3BlNo { get; set; }

        /// <summary>
        /// 未見袋數(未入倉)
        /// </summary>
        public int TotalNoBlNo { get; set; }

        /// <summary>
        /// A03袋數
        /// </summary>
        public int TotalA03BlNo { get; set; }

        /// <summary>
        /// B6F袋數
        /// </summary>
        public int TotalB6FBlNo { get; set; }

        /// <summary>
        /// 異常袋號字串
        /// </summary>
        public string ErrorBlNo { get; set; }

        /// <summary>
        /// 班次到達資訊
        /// </summary>
        public CustWorkLoadArriveInfo ArriveInfo { get; set; }

        /// <summary>
        /// 出倉日期列表
        /// </summary>
        public List<SignOutTimeModel> SignOutDateList { get; set; }
    }
}
