using Service.Services.ShipmentInboundProcess.Domain;
using System.Collections.Generic;

namespace Service.Services.ShipmentInboundRecord.Domain
{

    public class ShipmentInboundCustomerModel
    { 
        public string Cust_Type { get; set; }

        public string TypeName => Cust_Type == "SEA" ? "海運" : "空運";

        public string Cust_Code { get; set; }

        public string Cust_Name { get; set; }

    }
}
