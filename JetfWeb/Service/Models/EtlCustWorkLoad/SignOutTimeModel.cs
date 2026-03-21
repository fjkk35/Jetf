using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.EtlCustWorkLoad
{
    public class SignOutTimeModel
    {
        public string Mainnumber { get; set; }

        public string TransName { get; set; }

        public DateTime SignOutTime { get; set; }

        public string ArrivalTime { get; set; }

        public int TotalBlNo { get; set; }
     
    }
}
