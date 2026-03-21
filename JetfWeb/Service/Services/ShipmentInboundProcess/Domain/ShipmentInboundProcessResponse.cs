using System.Collections.Generic;

namespace Service.Services.ShipmentInboundProcess.Domain
{
    public class ShipmentInboundProcessResponse
    {
        public List<ShipmentInboundProcessModel> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
