using System.Collections.Generic;

namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 空運主號上傳 Excel 的明細資料列。
    /// </summary>
    public class AirMainUploadExcelRow
    {
        /// <summary>
        /// 建立空運主號上傳明細。
        /// </summary>
        public AirMainUploadExcelRow()
        {
            PlinkErrors = new List<AirMainPlinkErrorRow>();
        }

        /// <summary>
        /// 主號。
        /// </summary>
        public string Mwb { get; set; }

        /// <summary>
        /// 上傳檔袋號。
        /// </summary>
        public string BagNo { get; set; }

        /// <summary>
        /// 分艙單收單註記。
        /// </summary>
        public string ReceiptMark { get; set; }

        /// <summary>
        /// 一分號多件之分號。
        /// </summary>
        public string OneHwbMultiPieceHwb { get; set; }

        /// <summary>
        /// ZZZA 備註。
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// AIR_DETAIN 狀態。
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 對應的 PLINK 錯單資料。
        /// </summary>
        public List<AirMainPlinkErrorRow> PlinkErrors { get; set; }
    }
}
