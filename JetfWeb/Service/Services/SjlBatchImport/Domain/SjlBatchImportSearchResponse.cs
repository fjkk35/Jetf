using System.Collections.Generic;

namespace Service.Services.SjlBatchImport.Domain
{
    /// <summary>
    /// 捷利托運資料查詢結果。
    /// </summary>
    public class SjlBatchImportSearchResponse
    {
        public int TotalCount { get; set; }

        public List<SjlShippingDataSearchModel> Data { get; set; }
    }
}