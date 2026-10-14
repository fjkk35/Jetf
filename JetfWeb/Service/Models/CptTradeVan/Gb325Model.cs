using System.Collections.Generic;

namespace Service.Models.CptTradeVan
{
    /// <summary>
    /// GB325 海運進口艙單表頭及彙整資料查詢結果。
    /// </summary>
    public class Gb325Model
    {
        /// <summary>
        /// 海空運別。
        /// </summary>
        public string TransTypeCd { get; set; }

        /// <summary>
        /// 海關通關號碼（原船隻掛號）。
        /// </summary>
        public string VslRegNo { get; set; }

        /// <summary>
        /// 船公司代碼。
        /// </summary>
        public string ShipCoCd { get; set; }

        /// <summary>
        /// 航次。
        /// </summary>
        public string VoyageNo { get; set; }

        /// <summary>
        /// 受理艙位配置圖註記。
        /// </summary>
        public string BpTransNote { get; set; }

        /// <summary>
        /// 船舶呼號。
        /// </summary>
        public string VslSign { get; set; }

        /// <summary>
        /// 受理關別。
        /// </summary>
        public string ProcessCustCd { get; set; }

        /// <summary>
        /// 聯營船公司代碼資料列。
        /// </summary>
        public List<Gb325GridModel> GridModel { get; set; }

        /// <summary>
        /// 船長姓名。
        /// </summary>
        public string OfficerName { get; set; }

        /// <summary>
        /// 出口艙單船長姓名。
        /// </summary>
        public string OfficerNameExport { get; set; }

        /// <summary>
        /// 裝船結關船長姓名。
        /// </summary>
        public string OfficerNameLoad { get; set; }

        /// <summary>
        /// 到港前一國外港。
        /// </summary>
        public string LastPorCd { get; set; }

        /// <summary>
        /// 停泊港口。
        /// </summary>
        public string AncPortCd { get; set; }

        /// <summary>
        /// 停泊碼頭。
        /// </summary>
        public string WharfCd { get; set; }

        /// <summary>
        /// 前一國外港離港日期，格式為 yyyyMMdd。
        /// </summary>
        public string LastDpDate { get; set; }

        /// <summary>
        /// 預定到港日期，格式為 yyyyMMdd。
        /// </summary>
        public string EstArDate { get; set; }

        /// <summary>
        /// 更新日期時間，格式為 yyyyMMddHHmmss。
        /// </summary>
        public string EntryDate { get; set; }

        /// <summary>
        /// 空櫃總數。
        /// </summary>
        public int MemCount { get; set; }

        /// <summary>
        /// 實櫃總數。
        /// </summary>
        public int MfmCount { get; set; }

        /// <summary>
        /// 艙號總數。
        /// </summary>
        public int MdmCount { get; set; }

        /// <summary>
        /// 查詢狀態。
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 查詢訊息。
        /// </summary>
        public string Msg { get; set; }
    }
}
