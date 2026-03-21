using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.HctEtlTax
{
    public class HctEtlTaxDetail
    {
        public string MainNumber { get; set; }
        public string BagNumber { get; set; }
        public string TrackingNo { get; set; }
        public string Recipient { get; set; }
        public string RecPhone { get; set; }
        public string RecAddress { get; set; }
        public string Remark { get; set; }
        public int Quantity { get; set; }
        public double Weight { get; set; }
        public string DeliveryNo { get; set; }
        public string DespatchNo { get; set; }
        public double To_Dlv_Cod { get; set; }

        public string Trans_Name { get; set; }

        public string Company { get; set; }

        public int CeilingWeight => (int)Math.Ceiling(Weight);
    }

     
}
