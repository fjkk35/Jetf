using System;
using System.Collections.Generic;
using Service.Models.EtlCustWorkLoad;

namespace Service.Services.EtlCustomerWorkLoadReport.Domain
{
    /// <summary>
    /// 空快客戶作業量報表(蝦皮格式)每列資料模型
    /// </summary>
    public class CustWorkLoadReport2RowModel
    {
        /// <summary>
        /// 派件公司編號
        /// </summary>
        public int? TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱 (尾程)
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 客戶名稱 (头程)
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string Mainnumber { get; set; }

        /// <summary>
        /// 渠道代碼
        /// </summary>
        public string LineCode { get; set; }

        /// <summary>
        /// 渠道箱數 (原單袋數)
        /// </summary>
        public int TotalBlNo { get; set; }

        /// <summary>
        /// 班次到達資訊
        /// </summary>
        public CustWorkLoadArriveInfo ArriveInfo { get; set; }

        /// <summary>
        /// 出倉時間列表 (清关完成时间)
        /// </summary>
        public List<SignOutTimeModel> SignOutTimeList { get; set; }

        /// <summary>
        /// 交倉時間列表
        /// </summary>
        //public List<DateTime> ArrivalTimeList { get; set; }
    }

    /// <summary>
    /// 班次到達資訊模型
    /// </summary>
    public class CustWorkLoadArriveInfo
    {
        /// <summary>
        /// 起飛地
        /// </summary>
        public string Ori { get; set; }

        /// <summary>
        /// 轉運機場
        /// </summary>
        public string TransitAirport { get; set; }

        /// <summary>
        /// 目的地
        /// </summary>
        public string Dest { get; set; }

        /// <summary>
        /// 航班代號
        /// </summary>
        public string FlightNumber { get; set; }

        /// <summary>
        /// 航班袋數
        /// </summary>
        public int? FlightCount { get; set; }

        /// <summary>
        /// 到達時間1
        /// </summary>
        public DateTime? ArriveDate1 { get; set; }

        /// <summary>
        /// 到達時間2
        /// </summary>
        public DateTime? ArriveDate2 { get; set; }

        /// <summary>
        /// 到達時間3
        /// </summary>
        public DateTime? ArriveDate3 { get; set; }

        /// <summary>
        /// 到達時間4
        /// </summary>
        public DateTime? ArriveDate4 { get; set; }

        /// <summary>
        /// 到達時間5
        /// </summary>
        public DateTime? ArriveDate5 { get; set; }

        /// <summary>
        /// 派件公司送達時間1
        /// </summary>
        public DateTime? TransDate1 { get; set; }

        /// <summary>
        /// 派件公司袋數1
        /// </summary>
        public int? TransCount1 { get; set; }

        /// <summary>
        /// 派件公司送達時間2
        /// </summary>
        public DateTime? TransDate2 { get; set; }

        /// <summary>
        /// 派件公司袋數2
        /// </summary>
        public int? TransCount2 { get; set; }

        /// <summary>
        /// 派件公司送達時間3
        /// </summary>
        public DateTime? TransDate3 { get; set; }

        /// <summary>
        /// 派件公司袋數3
        /// </summary>
        public int? TransCount3 { get; set; }

        /// <summary>
        /// 派件公司送達時間4
        /// </summary>
        public DateTime? TransDate4 { get; set; }

        /// <summary>
        /// 派件公司袋數4
        /// </summary>
        public int? TransCount4 { get; set; }

        /// <summary>
        /// 派件公司送達時間5
        /// </summary>
        public DateTime? TransDate5 { get; set; }

        /// <summary>
        /// 派件公司袋數5
        /// </summary>
        public int? TransCount5 { get; set; }

        /// <summary>
        /// 派件公司送達時間6
        /// </summary>
        public DateTime? TransDate6 { get; set; }

        /// <summary>
        /// 派件公司袋數6
        /// </summary>
        public int? TransCount6 { get; set; }

        /// <summary>
        /// 派件公司送達時間7
        /// </summary>
        public DateTime? TransDate7 { get; set; }

        /// <summary>
        /// 派件公司袋數7
        /// </summary>
        public int? TransCount7 { get; set; }

        /// <summary>
        /// 派件公司送達時間8
        /// </summary>
        public DateTime? TransDate8 { get; set; }

        /// <summary>
        /// 派件公司袋數8
        /// </summary>
        public int? TransCount8 { get; set; }

        /// <summary>
        /// 派件公司送達時間9
        /// </summary>
        public DateTime? TransDate9 { get; set; }

        /// <summary>
        /// 派件公司袋數9
        /// </summary>
        public int? TransCount9 { get; set; }

        /// <summary>
        /// 派件公司送達時間10
        /// </summary>
        public DateTime? TransDate10 { get; set; }

        /// <summary>
        /// 派件公司袋數10
        /// </summary>
        public int? TransCount10 { get; set; }
    }
}
