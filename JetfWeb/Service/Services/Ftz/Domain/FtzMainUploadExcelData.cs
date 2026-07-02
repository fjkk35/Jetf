using System.Collections.Generic;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// FTZ 主號查詢上傳 Excel 的解析結果。
    /// </summary>
    public class FtzMainUploadExcelData
    {
        /// <summary>
        /// 建立 FTZ 主號查詢上傳 Excel 的解析結果。
        /// </summary>
        public FtzMainUploadExcelData()
        {
            DetailRows = new List<FtzMainUploadExcelRow>();
            SummaryRows = new List<FtzMainUploadSummaryRow>();
        }

        /// <summary>
        /// 明細頁籤資料列。
        /// </summary>
        public List<FtzMainUploadExcelRow> DetailRows { get; set; }

        /// <summary>
        /// 主號2 頁籤資料列。
        /// </summary>
        public List<FtzMainUploadSummaryRow> SummaryRows { get; set; }
    }
}
