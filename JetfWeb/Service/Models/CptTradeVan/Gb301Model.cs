using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class Gb301Model
    {
        public int Total { get; set; }
        public string StorWareCd { get; set; }
        public string SearchOper { get; set; }
        public string SearchString { get; set; }
        public object ArrangeNo { get; set; }
        public bool Loadonce { get; set; }
        public string VslRegNo { get; set; }
        public string Sord { get; set; }
        public string ShipCoCd { get; set; }
        public int Page { get; set; }
        public string BillNo { get; set; }
        /// <summary>
        /// 放行附帶條件
        /// </summary>
        public string RelCondCd { get; set; }
        public string DeclType { get; set; }
        public object DataObject { get; set; }
        public string BrokerBoxNoName { get; set; }
        public object TaxAcntBsnsCd { get; set; }
        public string TransTypeCd { get; set; }
        public string MftNo { get; set; }
        public string Status { get; set; }
        /// <summary>
        /// 報單號碼
        /// </summary>
        public string DeclNo { get; set; }
        public string Hawb { get; set; }
        public string ExamMethod { get; set; }
        public string Class { get; set; }
        public string SearchField { get; set; }
        public List<Gb301GridModel> GridModel { get; set; }
        public string Sidx { get; set; }
        public string Msg { get; set; }
        public object NoPaperStat { get; set; }
        public int Rows { get; set; }
        public object UcrNo { get; set; }
        public string Message { get; set; }
        public string CustCd { get; set; }
        public int Records { get; set; }
    }

    public class Gb301GridModel
    {
        /// <summary>
        /// 處理日期時間
        /// </summary>
        public string ProDateTime { get; set; }
        /// <summary>
        /// 通關狀態代號
        /// </summary>
        public string ProcEventCodeStr { get; set; }
        /// <summary>
        /// 處理說明
        /// </summary>
        public object ProgDesc { get; set; }
        public string UnbCtrlRefNo { get; set; }
    }
}
