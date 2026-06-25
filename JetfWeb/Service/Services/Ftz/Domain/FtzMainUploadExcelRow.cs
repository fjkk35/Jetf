namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// 主號查詢上傳 Excel 的明細列資料。
    /// </summary>
    public class FtzMainUploadExcelRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string Mwb { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        public string BagNo { get; set; }

        /// <summary>
        /// 分艙單收單註記。
        /// </summary>
        public string ReceiptMark { get; set; }
    }
}