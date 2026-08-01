using System;

namespace Service.Models
{
    /// <summary>
    /// 稅金總表及明細表的資料列。
    /// </summary>
    public sealed class DownloadIncludeTaxReportModel
    {
        /// <summary>
        /// 資料日期。
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 資料來源。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 報關類別。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶代號。
        /// </summary>
        public string CustId { get; set; }

        /// <summary>
        /// 派件公司代號。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 菜鳥 LP 單號。
        /// </summary>
        public string Arrival { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 清關袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 進倉時間。
        /// </summary>
        public DateTime? InDateTime { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public DateTime? OutDateTime { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        public int? TaxBase { get; set; }

        /// <summary>
        /// 稅金一。
        /// </summary>
        public int? Tax1 { get; set; }

        /// <summary>
        /// 稅金二。
        /// </summary>
        public int? Tax2 { get; set; }

        /// <summary>
        /// 納稅義務人原始名稱。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int? Cod { get; set; }

        /// <summary>
        /// 是否包稅。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 費用主檔納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 制單統一編號。
        /// </summary>
        public string ImporterId { get; set; }

        /// <summary>
        /// 制單納稅義務人。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 跟廠商收。
        /// </summary>
        public int? CustomerCod { get; set; }

        /// <summary>
        /// 跟派件收。
        /// </summary>
        public int? TransCod { get; set; }

        /// <summary>
        /// 稅單回傳識別碼。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 對應的代收銷帳資料分提單號。
        /// </summary>
        public string ReconciliationAirTrackingNo { get; set; }

        /// <summary>
        /// 對應的代收銷帳納稅義務人。
        /// </summary>
        public string ReconciliationAirRecipient { get; set; }

        /// <summary>
        /// 對應的代收銷帳稅單回傳識別碼。
        /// </summary>
        public string ReconciliationAirTaxRecId { get; set; }
    }
}
