using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.Cargo
{
    public class FeeMasterModel
    {
        public string IncludeTax { get; set; }
        public int Tax1 { get; set; }
        public int Tax2 { get; set; }
        public int TotalTax { get; set; }
        public int CcFee { get; set; }
        public int Fee { get; set; }
        public int Cod { get; set; }
        public int ToDlvCod { get; set; }
        public int CustomerCod { get; set; }
        public int TransCod { get; set; }
    }
}
