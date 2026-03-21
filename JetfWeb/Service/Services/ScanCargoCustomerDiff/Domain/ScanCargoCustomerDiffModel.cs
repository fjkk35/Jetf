using System;

namespace Service.Services.ScanCargoCustomerDiff.Domain
{
    /// <summary>
    /// 刷槍作業差異表Model
    /// </summary>
    public class ScanCargoCustomerDiffModel
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string MAIN_NUMBER { get; set; }

        /// <summary>
        /// 袋號
        /// </summary>
        public string BAG_NUMBER { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string MERGE_NUMBER { get; set; }

        /// <summary>
        /// 查驗時間
        /// </summary>
        public DateTime? SIGN_OUT_TIME { get; set; }

        /// <summary>
        /// 刷槍資料
        /// </summary>
        public string Data { get; set; }
    }
}
