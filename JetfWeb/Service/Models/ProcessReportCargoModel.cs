using System;

namespace Service.Models
{
    /// <summary>
    /// 處置報表使用的海空運貨況資料。
    /// </summary>
    public sealed class ProcessReportCargoModel
    {
        /// <summary>來源資料流水號。</summary>
        public int Id { get; set; }

        /// <summary>來源資料建立時間，用於選取同貨號的最新資料。</summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>運別：SEA 為海運，AIR 為空運。</summary>
        public string TransportType { get; set; }

        /// <summary>物流貨號，對應處置紀錄的 DLV_INV。</summary>
        public string DlvInv { get; set; }

        /// <summary>主提單號。</summary>
        public string MainNumber { get; set; }

        /// <summary>清關袋號。</summary>
        public string BagNo { get; set; }

        /// <summary>來源客戶代碼。</summary>
        public string CustomerCode { get; set; }

        /// <summary>到港日期。</summary>
        public DateTime? Eta { get; set; }

        /// <summary>收件人名稱。</summary>
        public string Importer { get; set; }

        /// <summary>收件人電話。</summary>
        public string ImPhoneNo { get; set; }

        /// <summary>倉儲類型。</summary>
        public string DataType { get; set; }

        /// <summary>入倉時間。</summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>出倉時間。</summary>
        public DateTime? SignOutTime { get; set; }
    }
}
