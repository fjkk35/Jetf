using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Coupang
{
    public class ManifestModel
    {
        public string SendId { get; set; }
        public string CreateDate { get; set; }
        public string BrokerCode { get; set; }
        public string MawbNo { get; set; }
        public string FlightNo { get; set; }
        public string ImportDate { get; set; }
        public string DeclDate { get; set; }
        public string Currency { get; set; }
        public string OrigPort { get; set; }

        public string DeclType { get; set; }
        public string DeclNo { get; set; }
        public string BagNo { get; set; }
        public string BagWeight { get; set; }

        public string HawbNo { get; set; }
        public string MainHawbNo { get; set; }
        public string DeliveryType { get; set; }
        public string Ctns { get; set; }
        public string CtnUnit { get; set; }
        public string GrossWeight { get; set; }
        public string NetWeight { get; set; }
        public string TermsSales { get; set; }
        public string FreightAmt { get; set; }
        public string DutyExemption { get; set; }

        public string CTaxNo { get; set; }
        public string CName { get; set; }
        public string CAddr { get; set; }
        public string CTel { get; set; }

        public string SName { get; set; }
        public string SAddr { get; set; }

        public string ItemNo { get; set; }
        public string VendorItemId { get; set; }
        public string CategoryName { get; set; }
        public string GoodsDesc { get; set; }
        public string Uprice { get; set; }
        public string Qty { get; set; }
        public string QtyUnit { get; set; }
        public string TotalPrice { get; set; }
        public string MfrCountry { get; set; }
        public string TaxMethod { get; set; }
        public string CCCCode { get; set; }
        public string LicenseNo1 { get; set; }
        public string LicenseNo2 { get; set; }
        public string LicenseNo3 { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Specification { get; set; }
        public string DesignatedCode { get; set; }
    }
}