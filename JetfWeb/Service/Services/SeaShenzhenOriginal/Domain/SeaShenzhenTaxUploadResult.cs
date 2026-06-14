using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞深圳稅單上傳結果。
    /// </summary>
    public class SeaShenzhenTaxUploadResult
    {
        /// <summary>
        /// 資料日期。
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 報關行。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 來源筆數。
        /// </summary>
        public int SourceCount { get; set; }

        /// <summary>
        /// 寫入 SeaShenzhenTax 筆數。
        /// </summary>
        public int SavedCount { get; set; }

        /// <summary>
        /// 刪除既有深圳稅金筆數。
        /// </summary>
        public int DeletedCount { get; set; }

        /// <summary>
        /// 新增深圳稅金筆數。
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// 驗證失敗筆數。
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 轉檔異常筆數。
        /// </summary>
        public int ExceptionCount { get; set; }

        /// <summary>
        /// 回傳訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 驗證失敗資料。
        /// </summary>
        public List<SeaShenzhenTaxUploadRow> Data { get; set; } = new List<SeaShenzhenTaxUploadRow>();

        /// <summary>
        /// 轉檔異常資料。
        /// </summary>
        public List<SeaShenzhenTaxTransferExceptionRow> Exceptions { get; set; } = new List<SeaShenzhenTaxTransferExceptionRow>();
    }
}