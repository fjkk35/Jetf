using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class Gb326Model
    {
        /// <summary>
        /// 總數量
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 分運單存倉代碼
        /// </summary>
        public string HawbStorWareCd { get; set; }

        /// <summary>
        /// 搜尋操作
        /// </summary>
        public string SearchOper { get; set; }

        /// <summary>
        /// 搜尋字串
        /// </summary>
        public string SearchString { get; set; }

        /// <summary>
        /// 國際海事組織編號
        /// </summary>
        public string ImoNo { get; set; }

        /// <summary>
        /// 船名
        /// </summary>
        public string ShipName { get; set; }

        /// <summary>
        /// 包裝單位
        /// </summary>
        public string PackUnit { get; set; }

        /// <summary>
        /// 包裝描述
        /// </summary>
        public string PackDesc { get; set; }

        /// <summary>
        /// 是否一次加載
        /// </summary>
        public bool Loadonce { get; set; }

        /// <summary>
        /// 船舶登記號
        /// </summary>
        public string VslRegNo { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public string Sord { get; set; }

        /// <summary>
        /// 頁碼
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 提單號
        /// </summary>
        public string BillNo { get; set; }

        /// <summary>
        /// 集裝箱號列表
        /// </summary>
        public List<object> ContainerNoList { get; set; }

        /// <summary>
        /// 申報類型
        /// </summary>
        public string DeclType { get; set; }

        /// <summary>
        /// 製造商編號列表
        /// </summary>
        public List<string> MftNoList { get; set; }

        /// <summary>
        /// 數據對象
        /// </summary>
        public object DataObject { get; set; }

        /// <summary>
        /// 放行日期
        /// </summary>
        public string RelDate { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string Mawb { get; set; }

        /// <summary>
        /// 運輸類型代碼
        /// </summary>
        public string TransTypeCd { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 製造商編號
        /// </summary>
        public string MftNo { get; set; }

        /// <summary>
        /// 申報號
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 分運單號
        /// </summary>
        public string Hawb { get; set; }

        /// <summary>
        /// 航次號
        /// </summary>
        public string VoyagNo { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        public string Class { get; set; }

        /// <summary>
        /// 船舶標誌
        /// </summary>
        public string VslSign { get; set; }

        /// <summary>
        /// 搜尋欄位
        /// </summary>
        public string SearchField { get; set; }

        /// <summary>
        /// 網格模型
        /// </summary>
        public List<object> GridModel { get; set; }

        /// <summary>
        /// 主提單存倉代碼
        /// </summary>
        public string MawbStorWareCd { get; set; }

        /// <summary>
        /// 排序索引
        /// </summary>
        public string Sidx { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 對應標誌
        /// </summary>
        public string MhCorrespMark { get; set; }

        /// <summary>
        /// 進口類型
        /// </summary>
        public string ImdType { get; set; }

        /// <summary>
        /// 行數
        /// </summary>
        public int Rows { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 進口日期標記
        /// </summary>
        public string ImportDateMarkOut { get; set; }

        /// <summary>
        /// 進口日期
        /// </summary>
        public string ImportDate { get; set; }

        /// <summary>
        /// 申報日期
        /// </summary>
        public string DeclDate { get; set; }

        /// <summary>
        /// 經紀人箱號
        /// </summary>
        public string BrokerBoxNo { get; set; }

        /// <summary>
        /// 記錄數
        /// </summary>
        public int Records { get; set; }

        /// <summary>
        /// 追蹤位置代碼
        /// </summary>
        public string TrackLocationCd { get; set; }

        /// <summary>
        /// 預計到達日期
        /// </summary>
        public string EstArDate { get; set; }
    }
}
