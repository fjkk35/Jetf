using System.Collections.Generic;

namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 空運主號上傳 Excel 的完整解析結果。
    /// </summary>
    public class AirMainUploadExcelData
    {
        /// <summary>
        /// 建立空運主號上傳 Excel 解析結果。
        /// </summary>
        public AirMainUploadExcelData()
        {
            DetailRows = new List<AirMainUploadExcelRow>();
            SummaryRows = new List<AirMainUploadSummaryRow>();
        }

        /// <summary>
        /// 明細頁籤資料。
        /// </summary>
        public List<AirMainUploadExcelRow> DetailRows { get; set; }

        /// <summary>
        /// 主號2頁籤資料。
        /// </summary>
        public List<AirMainUploadSummaryRow> SummaryRows { get; set; }
    }
}
