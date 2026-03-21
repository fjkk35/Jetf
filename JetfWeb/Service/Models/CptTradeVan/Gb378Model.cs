using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class Gb378Model
    {
        public string o_rtn_msg { get; set; }
        public string status { get; set; }
        public List<Data> data { get; set; }
        public string o_rtn_code { get; set; }
        public string msg { get; set; }
        public string size { get; set; }
    }

    public class Data
    {
        public string containerNo { get; set; }
        public string imCmRate { get; set; }
        public string storWareCd { get; set; }
        public string vslRegNo { get; set; }
    }
}
