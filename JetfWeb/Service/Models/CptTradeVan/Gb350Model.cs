using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class Gb350Model
    {
        public int Total { get; set; }
        public string SearchOper { get; set; }
        public string SearchString { get; set; }
        public string Status { get; set; }
        public string Class { get; set; }
        public List<GB350GridModel> GridModel { get; set; }
        public string SearchField { get; set; }
        public string Sidx { get; set; }
        public string Msg { get; set; }
        public bool LoadOnce { get; set; }
        public int Rows { get; set; }
        public string Message { get; set; }
        public string Sord { get; set; }
        public int Page { get; set; }
        public int Records { get; set; }
        public object DataObject { get; set; }
    }

    public class GB350GridModel
    {
        public int TOT_PACK_QTY { get; set; }
        public string STORE_WARE_CD { get; set; }
        public string TRANS_DATE { get; set; }
        public string VOYAGE_FLIGHT_NO { get; set; }
        public string TRANS_BAN { get; set; }
        public string ERROR_MSG { get; set; }
        public string STATUS { get; set; }
        public string IMPORT_DATE { get; set; }
        public string MAWB { get; set; }
    }
}
