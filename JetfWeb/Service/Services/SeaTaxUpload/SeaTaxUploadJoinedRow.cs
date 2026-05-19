using System;

namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 海運稅金上傳與清關、原單、客戶設定整併後的資料列。
    /// </summary>
    internal sealed class SeaTaxUploadJoinedRow
    {
        /// <summary>
        /// 分提單號或袋號。
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 報單類別。
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 稅額。
        /// </summary>
        public string Tax { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 進倉時間。
        /// </summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        public string TaxBase { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int? CodFee { get; set; }

        /// <summary>
        /// 包稅代碼。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 物流公司。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 是否為菜鳥 P。
        /// </summary>
        public bool? IsCainiaoP { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string DespatchName { get; set; }

        /// <summary>
        /// 稅金付款方式。
        /// </summary>
        public string TransTaxPayment { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 收件人地址。
        /// </summary>
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 收件人證號。
        /// </summary>
        public string ImporterId { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public decimal? Cod { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 到貨資訊。
        /// </summary>
        public string Arrival { get; set; }
    }
}