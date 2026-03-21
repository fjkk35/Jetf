using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.TpctContainer
{
    public class QueryCntrStatusModel
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public bool success { get; set; }
        public string msg { get; set; }
        public List<DataModel> data { get; set; }
    }

    public class DataModel
    {
        public string rownum { get; set; }
        public string msg { get; set; }
        public string cntrType { get; set; }
        public string vslvoy { get; set; }
        public string carrier { get; set; }
        public string ecarrier { get; set; }
        public string estatus { get; set; }
        public string opTime { get; set; }
        public string action { get; set; }
        public string opDate { get; set; }
        public string status { get; set; }
    }
}
