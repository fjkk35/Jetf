namespace Service.Models.ShortOverUnload
{
    /// <summary>
    /// 短溢卸匯入明細及其艙單、製單補充資料。
    /// </summary>
    public class ShortOverUnloadRow
    {
        /// <summary>來源工作表名稱。</summary>
        public string SourceSheet { get; set; }

        /// <summary>來源 Excel 列號（從 1 起算）。</summary>
        public int SourceRow { get; set; }

        /// <summary>主提單號碼。</summary>
        public string MainNumber { get; set; }

        /// <summary>分提單號碼。</summary>
        public string BlNo { get; set; }

        /// <summary>申報件數。</summary>
        public int DeclaredQuantity { get; set; }

        /// <summary>實到件數。</summary>
        public int ActualQuantity { get; set; }

        /// <summary>艙單裝貨型態。</summary>
        public string Manifest { get; set; }

        /// <summary>第一項貨物說明。</summary>
        public string ItemName { get; set; }

        /// <summary>第一項貨物的櫃號。</summary>
        public string ContainerNo { get; set; }
    }
}
