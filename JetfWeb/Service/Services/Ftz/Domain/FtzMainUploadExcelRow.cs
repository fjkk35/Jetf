namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// FTZ 主號查詢上傳 Excel 的明細頁籤資料列。
    /// </summary>
    public class FtzMainUploadExcelRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string Mwb { get; set; }

        /// <summary>
        /// 袋號或分號。
        /// </summary>
        public string BagNo { get; set; }

        /// <summary>
        /// 分艙單收單註記。
        /// </summary>
        public string ReceiptMark { get; set; }

        /// <summary>
        /// 一分號多件的分號清單。
        /// </summary>
        public string OneHwbMultiPieceHwb { get; set; }

        /// <summary>
        /// 備註；目前僅保留 ZZZA 註記。
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string TransName { get; set; }
    }
}
