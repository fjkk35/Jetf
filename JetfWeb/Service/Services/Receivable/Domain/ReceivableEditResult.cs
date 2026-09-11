using System;

namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細修改結果。
    /// </summary>
    public sealed class ReceivableEditResult
    {
        /// <summary>
        /// 已修改的費用主檔明細識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        public DateTime ModifiedTime { get; set; }
    }
}
