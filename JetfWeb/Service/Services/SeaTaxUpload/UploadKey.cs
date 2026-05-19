namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 海運稅金上傳流程使用的主號與分提單鍵值。
    /// </summary>
    internal sealed class UploadKey
    {
        /// <summary>
        /// 初始化上傳鍵值。
        /// </summary>
        /// <param name="mainNumber">主號。</param>
        /// <param name="blNo">分提單號或袋號。</param>
        public UploadKey(string mainNumber, string blNo)
        {
            MainNumber = (mainNumber ?? string.Empty).Trim();
            BlNo = (blNo ?? string.Empty).Trim();
        }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; }

        /// <summary>
        /// 分提單號或袋號。
        /// </summary>
        public string BlNo { get; }
    }
}