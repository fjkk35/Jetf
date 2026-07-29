namespace Service.Services.ReconciliationAir.Domain
{
    /// <summary>
    /// 空快代收銷帳上傳的單筆列資料。
    /// </summary>
    public sealed class ReconciliationAirUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 類型（FTZ / TACT）。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 納稅義務人統一編號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 營業稅基原始文字。
        /// </summary>
        public string TaxBaseText { get; set; }

        /// <summary>
        /// 營業稅基。
        /// </summary>
        public int TaxBase { get; set; }

        /// <summary>
        /// 稅費金額原始文字。
        /// </summary>
        public string TaxText { get; set; }

        /// <summary>
        /// 稅費金額。
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 稅費項目一。
        /// </summary>
        public string TaxItem1 { get; set; }

        /// <summary>
        /// 單項稅費金額一原始文字。
        /// </summary>
        public string TaxAmount1Text { get; set; }

        /// <summary>
        /// 稅費項目二。
        /// </summary>
        public string TaxItem2 { get; set; }

        /// <summary>
        /// 單項稅費金額二原始文字。
        /// </summary>
        public string TaxAmount2Text { get; set; }

        /// <summary>
        /// 稅費項目三。
        /// </summary>
        public string TaxItem3 { get; set; }

        /// <summary>
        /// 單項稅費金額三原始文字。
        /// </summary>
        public string TaxAmount3Text { get; set; }

        /// <summary>
        /// 稅費項目四。
        /// </summary>
        public string TaxItem4 { get; set; }

        /// <summary>
        /// 單項稅費金額四原始文字。
        /// </summary>
        public string TaxAmount4Text { get; set; }

        /// <summary>
        /// 稅費項目五。
        /// </summary>
        public string TaxItem5 { get; set; }

        /// <summary>
        /// 單項稅費金額五原始文字。
        /// </summary>
        public string TaxAmount5Text { get; set; }

        /// <summary>
        /// 稅費項目六。
        /// </summary>
        public string TaxItem6 { get; set; }

        /// <summary>
        /// 單項稅費金額六原始文字。
        /// </summary>
        public string TaxAmount6Text { get; set; }

        /// <summary>
        /// 進口稅。
        /// </summary>
        public int? ImportTax { get; set; }

        /// <summary>
        /// 營業稅。
        /// </summary>
        public int? BusinessTax { get; set; }

        /// <summary>
        /// 失敗原因。
        /// </summary>
        public string FailReason { get; set; }
    }
}
