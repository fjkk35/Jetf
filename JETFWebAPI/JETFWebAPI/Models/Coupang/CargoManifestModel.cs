using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Coupang
{
    public class CargoManifestModel
    {
        public string To { get; set; }
        public string Broker { get; set; }
        public string Date { get; set; }
        public string BillingCode { get; set; }
        public string Tel { get; set; }
        public string Fax { get; set; }
        public string FlightNo { get; set; }
        public string MawbNo { get; set; }
        public string TotalCnt { get; set; }
        public string TotalGrossWeight { get; set; }
        public string ItemNo { get; set; }
        public string MasterBagNo { get; set; }
        public string Ctn { get; set; }
        public string GrossWeight { get; set; }
        public string Description { get; set; }
        public string DeclaredTo { get; set; }
        public string Remark { get; set; }
    }
}