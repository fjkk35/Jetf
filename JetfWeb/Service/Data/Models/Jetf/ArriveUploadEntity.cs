using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 班機派件送達上傳表的批次新增資料。
    /// </summary>
    [Table("ARRIVE_UPLOAD", Schema = "dbo")]
    public sealed class ArriveUploadEntity
    {
        /// <summary>
        /// 匯入月份，格式為 MM。
        /// </summary>
        [Column("MM")]
        public string Month { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CUST_ID")]
        public string CustId { get; set; }

        /// <summary>
        /// 主提單號碼。
        /// </summary>
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 渠道代碼，空白時寫入 NULL。
        /// </summary>
        [Column("LINE_CODE")]
        public string LineCode { get; set; }

        /// <summary>
        /// 航班代號。
        /// </summary>
        [Column("FLIGHTNUMBER")]
        public string FlightNumber { get; set; }

        /// <summary>
        /// 航班應到袋數，空白時寫入零。
        /// </summary>
        [Column("FLIGHT_COUNT")]
        public int FlightCount { get; set; }

        /// <summary>
        /// 第 1 組班機到達時間。
        /// </summary>
        [Column("ARRIVE_DATE1")]
        public DateTime? ArriveDate1 { get; set; }

        /// <summary>
        /// 第 2 組班機到達時間。
        /// </summary>
        [Column("ARRIVE_DATE2")]
        public DateTime? ArriveDate2 { get; set; }

        /// <summary>
        /// 第 3 組班機到達時間。
        /// </summary>
        [Column("ARRIVE_DATE3")]
        public DateTime? ArriveDate3 { get; set; }

        /// <summary>
        /// 第 4 組班機到達時間。
        /// </summary>
        [Column("ARRIVE_DATE4")]
        public DateTime? ArriveDate4 { get; set; }

        /// <summary>
        /// 第 5 組班機到達時間。
        /// </summary>
        [Column("ARRIVE_DATE5")]
        public DateTime? ArriveDate5 { get; set; }

        /// <summary>
        /// 派件公司代碼。
        /// </summary>
        [Column("TRANS_NO")]
        public string TransNo { get; set; }

        /// <summary>
        /// 第 1 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE1")]
        public DateTime? TransDate1 { get; set; }

        /// <summary>
        /// 第 2 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE2")]
        public DateTime? TransDate2 { get; set; }

        /// <summary>
        /// 第 3 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE3")]
        public DateTime? TransDate3 { get; set; }

        /// <summary>
        /// 第 4 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE4")]
        public DateTime? TransDate4 { get; set; }

        /// <summary>
        /// 第 5 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE5")]
        public DateTime? TransDate5 { get; set; }

        /// <summary>
        /// 第 6 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE6")]
        public DateTime? TransDate6 { get; set; }

        /// <summary>
        /// 第 7 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE7")]
        public DateTime? TransDate7 { get; set; }

        /// <summary>
        /// 第 8 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE8")]
        public DateTime? TransDate8 { get; set; }

        /// <summary>
        /// 第 9 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE9")]
        public DateTime? TransDate9 { get; set; }

        /// <summary>
        /// 第 10 組派件公司送達時間。
        /// </summary>
        [Column("TRANS_DATE10")]
        public DateTime? TransDate10 { get; set; }

        /// <summary>
        /// 第 1 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT1")]
        public int TransCount1 { get; set; }

        /// <summary>
        /// 第 2 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT2")]
        public int TransCount2 { get; set; }

        /// <summary>
        /// 第 3 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT3")]
        public int TransCount3 { get; set; }

        /// <summary>
        /// 第 4 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT4")]
        public int TransCount4 { get; set; }

        /// <summary>
        /// 第 5 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT5")]
        public int TransCount5 { get; set; }

        /// <summary>
        /// 第 6 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT6")]
        public int TransCount6 { get; set; }

        /// <summary>
        /// 第 7 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT7")]
        public int TransCount7 { get; set; }

        /// <summary>
        /// 第 8 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT8")]
        public int TransCount8 { get; set; }

        /// <summary>
        /// 第 9 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT9")]
        public int TransCount9 { get; set; }

        /// <summary>
        /// 第 10 組送達袋數，空白時寫入零。
        /// </summary>
        [Column("TRANS_COUNT10")]
        public int TransCount10 { get; set; }

        /// <summary>
        /// 起運機場。
        /// </summary>
        [Column("ORI")]
        public string Ori { get; set; }

        /// <summary>
        /// 轉運機場。
        /// </summary>
        [Column("TRANSIT_AIRPORT")]
        public string TransitAirport { get; set; }

        /// <summary>
        /// 目的機場。
        /// </summary>
        [Column("DEST")]
        public string Dest { get; set; }

        /// <summary>
        /// 上傳人員代碼。
        /// </summary>
        [Column("UPLOAD_OPE")]
        public string UploadOpe { get; set; }
    }
}
