namespace Service.Models.ShortOverUnload
{
    /// <summary>短溢卸匯出中未取得資料的主分號及欄位，供畫面表格核對。</summary>
    public class ShortOverUnloadMissingData
    {
        /// <summary>主提單號碼。</summary>
        public string MainNumber { get; set; }

        /// <summary>分提單號碼。</summary>
        public string BlNo { get; set; }

        /// <summary>未取得的欄位名稱。</summary>
        public string MissingFields { get; set; }
    }
}
