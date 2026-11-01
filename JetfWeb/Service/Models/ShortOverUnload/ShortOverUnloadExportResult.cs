using System.Collections.Generic;

namespace Service.Models.ShortOverUnload
{
    /// <summary>短溢卸匯出檔案與畫面核對使用的未取得資料清單。</summary>
    public class ShortOverUnloadExportResult
    {
        /// <summary>可供下載的 Excel 位元組。</summary>
        public byte[] Content { get; set; }

        /// <summary>依主號、分號合併的未取得欄位，不影響檔案匯出。</summary>
        public List<ShortOverUnloadMissingData> MissingData { get; set; }
    }
}
