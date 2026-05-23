using System;

namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示清關、稅單、原始單與客戶主檔合併後的中繼資料。
    /// </summary>
    internal sealed class CombinedRow
    {
        /// <summary>
        /// 取得或設定資料來源代碼。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 取得或設定清關類型。
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 取得或設定清關單號。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 取得或設定入倉時間。
        /// </summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 取得或設定出倉時間。
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 取得或設定稅單號。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 取得或設定袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 取得或設定主提單號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 取得或設定稅額字串。
        /// </summary>
        public string TaxAmount { get; set; }

        /// <summary>
        /// 取得或設定完稅價格。
        /// </summary>
        public int? TaxBase { get; set; }

        /// <summary>
        /// 取得或設定納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 取得或設定納稅義務人證號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 取得或設定到站資料。
        /// </summary>
        public string Ecm { get; set; }

        /// <summary>
        /// 取得或設定原始袋號。
        /// </summary>
        public string BagNo { get; set; }

        /// <summary>
        /// 取得或設定客戶代碼。
        /// </summary>
        public string DespatchNo { get; set; }

        /// <summary>
        /// 取得或設定代收金額字串。
        /// </summary>
        public string Cc { get; set; }

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
        /// 取得或設定追蹤單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 取得或設定物流貨號。
        /// </summary>
        public string DeliveryNo { get; set; }

        /// <summary>
        /// 取得或設定稅金支付物流。
        /// </summary>
        public string TransTaxPayment { get; set; }

        /// <summary>
        /// 取得或設定 INCLUDE_TAX 類型。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 取得或設定代收手續費。
        /// </summary>
        public int? CodFee { get; set; }

        /// <summary>
        /// 取得或設定物流公司名稱。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 取得或設定是否為菜鳥 P 類型。
        /// </summary>
        public bool IsCainiaoP { get; set; }
    }
}