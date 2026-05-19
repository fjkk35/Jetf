namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 寫入 FEE_MASTER_DSTAIL 前的海運稅金明細資料列。
    /// </summary>
    internal sealed class SeaTaxFeeMasterDetailRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 提單號或袋號。
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
    }
}