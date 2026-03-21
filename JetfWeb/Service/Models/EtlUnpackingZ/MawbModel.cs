using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.EtlUnpackingZ
{
    public class MawbModel
    {
        public string searchMawb { get; set; }
        public int total { get; set; }
        public string searchOper { get; set; }
        public string searchString { get; set; }
        public string status { get; set; }
        public string @class { get; set; }
        public List<GridModel> gridModel { get; set; }
        public string searchField { get; set; }
        public string sidx { get; set; }
        public string msg { get; set; }
        public bool loadonce { get; set; }
        public int rows { get; set; }
        public string message { get; set; }
        public string sord { get; set; }
        public int page { get; set; }
        public int records { get; set; }
        public Dictionary<string, object> dataObject { get; set; }
    }

    public class GridModel
    {
        public int TOT_PACK_QTY { get; set; }
        public string STORE_WARE_CD { get; set; }
        public string TRANS_DATE { get; set; }
        public string VOYAGE_FLIGHT_NO { get; set; }
        public string TRANS_BAN { get; set; }
        public object ERROR_MSG { get; set; }
        public string STATUS { get; set; }
        public string IMPORT_DATE { get; set; }
        public string MAWB { get; set; }

        public DetailModel Detail { get; set; }
    }

    public class DetailModel
    {
        public int total { get; set; }
        public string searchOper { get; set; }
        public string searchString { get; set; }
        public string status { get; set; }
        public string className { get; set; }
        public List<GridDetailModel> gridModel { get; set; }
        public string searchField { get; set; }
        public string sidx { get; set; }
        public string msg { get; set; }
        public bool loadonce { get; set; }
        public int rows { get; set; }
        public string message { get; set; }
        public string sord { get; set; }
        public int page { get; set; }
        public int records { get; set; }
        public Object dataObject { get; set; }
    }

    public class GridDetailModel
    {
        public string HAWB { get; set; }
        public double WEIGHT { get; set; }
        public int QTY { get; set; }
        public string REMARK { get; set; }
        public string SOURCE_NOTE { get; set; }
        public string POUCH_NO { get; set; }
    }
}
