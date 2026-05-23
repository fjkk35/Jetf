using Service.Services.SeaTaxUpload;
using System;
using System.Collections.Generic;

namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示待寫入 FeeMaster 的草稿資料。
    /// </summary>
    internal sealed class FeeMasterDraft
    {
        /// <summary>
        /// 取得或設定來源系統。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 取得或設定清關類型。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 取得或設定客戶代碼。
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 取得或設定主提單號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 取得或設定追蹤單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 取得或設定清關單號。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 取得或設定袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 取得或設定稅單號。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 取得或設定納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 取得或設定納稅義務人證號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 取得或設定物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 取得或設定入倉日期字串。
        /// </summary>
        public string InDate { get; set; }

        /// <summary>
        /// 取得或設定入倉時間。
        /// </summary>
        public DateTime? InDateTime { get; set; }

        /// <summary>
        /// 取得或設定出倉時間。
        /// </summary>
        public DateTime? OutDateTime { get; set; }

        /// <summary>
        /// 取得或設定是否為併單。
        /// </summary>
        public string Combine { get; set; }

        /// <summary>
        /// 取得或設定完稅價格。
        /// </summary>
        public int? TaxBase { get; set; }

        /// <summary>
        /// 取得或設定主筆稅額。
        /// </summary>
        public int Tax1 { get; set; }

        /// <summary>
        /// 取得或設定累加稅額。
        /// </summary>
        public int Tax2 { get; set; }

        /// <summary>
        /// 取得或設定代收金額。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 取得或設定手續費。
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 取得或設定 INCLUDE_TAX 類型。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 取得或設定收件人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 取得或設定收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 取得或設定收件地址。
        /// </summary>
        public string RecAddress { get; set; }

        /// <summary>
        /// 取得或設定收件人證號。
        /// </summary>
        public string RecId { get; set; }

        /// <summary>
        /// 取得或設定實際派件代收金額。
        /// </summary>
        public int ToDlvCod { get; set; }

        /// <summary>
        /// 取得或設定派件物流代碼。
        /// </summary>
        public string DlvCom { get; set; }

        /// <summary>
        /// 取得或設定到站資料。
        /// </summary>
        public string Arrival { get; set; }

        /// <summary>
        /// 取得或設定客戶代收金額。
        /// </summary>
        public int CustomerCod { get; set; }

        /// <summary>
        /// 取得或設定物流代收金額。
        /// </summary>
        public int TransCod { get; set; }

        /// <summary>
        /// 取得或設定對應 FEE_MASTER_DETAIL 的明細資料。
        /// </summary>
        public List<FeeMasterDetailRow> DetailRows { get; set; } = new List<FeeMasterDetailRow>();
    }
}