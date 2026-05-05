using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.ShipmentOutboundBatchImport.Domain
{
    public class ShipmentOutboundModel
    {
        public string TrackingNo { get; set; }
        public DateTime OutboundDate { get; set; }
        public string OutboundTrackingNo { get; set; }
        public string WarehouseProcessTypeText { get; set; }
        public WarehouseProcessType? WarehouseProcessType { get; set; }
        public string OutboundOpe { get; set; }
        public ShipmentInboundProcessType ProcessType { get; set; }
        public string UploadStatus { get; set; }
        public string FailReason { get; set; }
    }
}
