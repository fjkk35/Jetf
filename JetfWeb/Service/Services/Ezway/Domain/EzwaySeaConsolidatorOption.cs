namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// 海運簡易查詢頁的集運商下拉選項。
    /// </summary>
    public class EzwaySeaConsolidatorOption
    {
        /// <summary>
        /// 下拉選單值。
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 下拉選單顯示文字。
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 集運商對應的 userId。
        /// </summary>
        public string UserId { get; set; }
    }
}