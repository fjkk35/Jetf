namespace Service.Models.ShortOverUnload
{
    /// <summary>
    /// 溢卸報告選用 GW 大於零的海運製單資料；重複時優先選用溢卸註記 O。
    /// </summary>
    public class ShortOverUnloadCargo
    {
        /// <summary>分提單號碼。</summary>
        public string BlNo { get; set; }

        /// <summary>併票溢卸註記。</summary>
        public string MergeOverFlag { get; set; }

        /// <summary>快遞業者統一編號。</summary>
        public string JetfId { get; set; }

        /// <summary>毛重。</summary>
        public decimal? Gw { get; set; }

        /// <summary>件數。</summary>
        public int? Piece { get; set; }

        /// <summary>件數單位。</summary>
        public string PieceUnit { get; set; }

        /// <summary>標記。</summary>
        public string Marks { get; set; }

        /// <summary>貨物名稱。</summary>
        public string ItemName { get; set; }

        /// <summary>數量。</summary>
        public int? Quantity { get; set; }

        /// <summary>單價金額。</summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>進口人英文名稱。</summary>
        public string Importer { get; set; }

        /// <summary>進口人英文地址。</summary>
        public string ImporterAddress { get; set; }

        /// <summary>貨櫃種類。</summary>
        public string ContainerType { get; set; }

        /// <summary>貨櫃號碼。</summary>
        public string ContainerNo { get; set; }

        /// <summary>封條號碼。</summary>
        public string SealNo { get; set; }
    }
}
