using System;

namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 海運稅金 Excel 原始列資料。
    /// </summary>
    internal sealed class SeaTaxUploadExcelRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 報單類別。
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 分提單號或袋號。
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 登記號碼。
        /// </summary>
        public string RegNo { get; set; }

        /// <summary>
        /// 艙單號碼。
        /// </summary>
        public string Mainfest { get; set; }

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
        /// 稅額。
        /// </summary>
        public string Tax { get; set; }

        /// <summary>
        /// 列印時間。
        /// </summary>
        public DateTime? PrtTime { get; set; }
    }
}