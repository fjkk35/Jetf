using System;

namespace Service.Models.ShortOverUnload
{
    /// <summary>
    /// 短溢卸匯入明細及其艙單、製單補充資料。
    /// </summary>
    public class ShortOverUnloadRow
    {
        /// <summary>主提單號碼。</summary>
        public string MainNumber { get; set; }

        /// <summary>依主號取得上傳拆櫃紀錄的拆櫃日期，供報告進倉日期與時間使用。</summary>
        public DateTime? UnboxingDate { get; set; }

        /// <summary>分提單號碼。</summary>
        public string BlNo { get; set; }

        /// <summary>申報件數。</summary>
        public int DeclaredQuantity { get; set; }

        /// <summary>實到件數。</summary>
        public int ActualQuantity { get; set; }

        /// <summary>依主號、分號取得 SEA_MANIFEST_UPLOAD.MANIFEST，供裝貨型態及艙單號碼欄位使用。</summary>
        public string Manifest { get; set; }

        /// <summary>第一項貨物說明。</summary>
        public string ItemName { get; set; }

        /// <summary>第一項貨物的櫃號。</summary>
        public string ContainerNo { get; set; }

        /// <summary>GB326 以船掛及主號查詢的艙單號碼，僅供頁籤四 B11 的主艙號欄位使用。</summary>
        public string CorrectionManifestNo { get; set; }

        /// <summary>溢卸報告使用的有效毛重製單資料。</summary>
        public ShortOverUnloadCargo Cargo { get; set; }

        /// <summary>同主號 ER 與 SEO 的 COUNT_RECORD 合計，供 BL C16 使用。</summary>
        public long? BillOfLadingPackageCount { get; set; }

        /// <summary>同主號 ER 毛重捨去小數後，加上 SEO 毛重並四捨五入至小數點後兩位，供 BL F16 使用。</summary>
        public decimal? BillOfLadingGrossWeight { get; set; }
    }
}
