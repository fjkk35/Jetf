namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 寫入 FEE_MASTER_DETAIL 的明細資料。
    /// </summary>
    internal sealed class FeeMasterDetailRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        public string TaxBase { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public string Tax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public string Ccfee { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public string Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public string Fee { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        public string RecAddress { get; set; }

        /// <summary>
        /// 應向物流代收金額。
        /// </summary>
        public string ToDlvCod { get; set; }

        /// <summary>
        /// 轉由派件公司代收的稅額。
        /// </summary>
        public string TransCod { get; set; }

        /// <summary>
        /// 客戶稅額
        /// </summary>
        public string CustomerCod { get; set; }
    }

    /// <summary>
    /// 建立 FEE_MASTER_DETAIL 明細前的共用來源資料。
    /// </summary>
    internal sealed class FeeMasterDetailSourceRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        public string TaxBase { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public string Tax { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public string Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public string Fee { get; set; }

        /// <summary>
        /// 稅金方式。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 來源備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        public string RecAddress { get; set; }

        /// <summary>
        /// 是否為菜鳥 P 客戶。
        /// </summary>
        public bool IsCainiaoP { get; set; }
    }
}
