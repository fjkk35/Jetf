using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
   public class CustomerModel
    {
        public string tran_type { get; set; }
        public string id { get; set; }
        public string cust_id { get; set; }
        public string customer { get; set; }
        public string trans_no { get; set; }
        public string trans_name { get; set; }
        public string include_tax { get; set; }
        public string include_tax_name { get; set; }
        public string company_no { get; set; }
        public string company { get; set; }
        public string cod_fee { get; set; }

        public bool IsCainiaoP { get; set; }
    }
}
