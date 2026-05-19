using System;

namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 來自 CLEARANCE_TAX 的海運稅金異動資料。
    /// </summary>
    internal sealed class SeaTaxModifyRow
    {
        /// <summary>
        /// 資料識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 資料類型。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 併袋號。
        /// </summary>
        public string MergeNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        public int? TaxBase { get; set; }

        /// <summary>
        /// 稅額。
        /// </summary>
        public int? TaxAmount { get; set; }

        /// <summary>
        /// 頻率註記。
        /// </summary>
        public string FreqSign { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 修改序號。
        /// </summary>
        public int? ModifySeq { get; set; }

        /// <summary>
        /// 修改檔名。
        /// </summary>
        public string ModifyFile { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        public DateTime? ModifyTime { get; set; }
    }
}