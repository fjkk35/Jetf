using System;

namespace Service.Models.DownloadEtlNew
{
    /// <summary>
    /// 表示空運物流代收報表單筆列資料。
    /// </summary>
    public sealed class DownloadEtlNewReportItem
    {
        /// <summary>
        /// 取得或設定袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 取得或設定追蹤單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 取得或設定稅額一。
        /// </summary>
        public int Tax1 { get; set; }

        /// <summary>
        /// 取得或設定稅額二。
        /// </summary>
        public int Tax2 { get; set; }

        /// <summary>
        /// 取得或設定手續費。
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 取得或設定代收金額。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 取得或設定實際派件代收金額。
        /// </summary>
        public int ToDlvCod { get; set; }

        /// <summary>
        /// 取得或設定收件人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 取得或設定收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 取得或設定物流名稱。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 取得或設定出倉時間。
        /// </summary>
        public DateTime? OutDateTime { get; set; }

        /// <summary>
        /// 取得或設定 INCLUDE_TAX 類型。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 取得或設定物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 取得或設定客戶代碼。
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 取得或設定客戶中文名稱。
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 取得或設定物流代碼。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 取得或設定公司名稱。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 取得或設定主號。
        /// </summary>
        public string MainNumber { get; set; }
    }
}
