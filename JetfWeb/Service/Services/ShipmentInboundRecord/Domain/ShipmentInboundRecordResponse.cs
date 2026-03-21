using System.Collections.Generic;

namespace Service.Services.ShipmentInboundRecord.Domain
{
    public class ShipmentInboundRecordResponse
    {
        public List<ShipmentInboundRecordModel> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
