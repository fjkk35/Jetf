using System.Collections.Generic;

namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 寫入 FEE_MASTER_TEST 前的海運稅金資料列。
    /// </summary>
    internal sealed class SeaTaxFeeMasterRow
    {
        /// <summary>
        /// 資料來源。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 類型。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 提單號或袋號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 是否併單。
        /// </summary>
        public string Combine { get; set; }

        /// <summary>
        /// 進倉日期。
        /// </summary>
        public string InDate { get; set; }

        /// <summary>
        /// 進倉時間。
        /// </summary>
        public string InDateTime { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public string OutDateTime { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        public string TaxBase { get; set; }

        /// <summary>
        /// 第一段稅額。
        /// </summary>
        public string Tax1 { get; set; }

        /// <summary>
        /// 第二段稅額。
        /// </summary>
        public string Tax2 { get; set; } = string.Empty;

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string DlvCom { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public string Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public string Fee { get; set; }

        /// <summary>
        /// 包稅代碼。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件人地址。
        /// </summary>
        public string RecAddress { get; set; }

        /// <summary>
        /// 收件人證號。
        /// </summary>
        public string RecId { get; set; }

        /// <summary>
        /// 最終代收金額。
        /// </summary>
        public string ToDlvCod { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 到貨資訊。
        /// </summary>
        public string Arrival { get; set; }

        /// <summary>
        /// 派件公司代收稅額。
        /// </summary>
        public string TransCod { get; set; } = string.Empty;

        /// <summary>
        /// 客戶代收稅額。
        /// </summary>
        public string CustomerCod { get; set; } = string.Empty;

        /// <summary>
        /// 同主檔全部筆數的明細資料。
        /// </summary>
        public List<SeaTaxFeeMasterDetailRow> DetailRows { get; set; } = new List<SeaTaxFeeMasterDetailRow>();
    }
}