using System;

namespace Service.Services.ShipmentInboundReturnImport.Domain
{
    public class ShipmentInboundReturnImportModel
    {
        public string DataType { get; set; }

        public string InboundDateText { get; set; }

        public DateTime InboundDate { get; set; }

        public string VendorName { get; set; }

        public string DispatchName { get; set; }

        public string TrackingNo { get; set; }

        public string SeqNo { get; set; }

        public string LocationCode { get; set; }

        public string SourceType { get; set; }

        public string SourceTypeDisplay { get; set; }

        public byte? SourceTypeCode { get; set; }

        public string WarehouseProcessTypeText { get; set; }

        public byte? WarehouseProcessType { get; set; }

        public string OutboundDateText { get; set; }

        public DateTime? OutboundDate { get; set; }

        public string Remark { get; set; }

        public string ReturnReason { get; set; }

        public string ReturnTrackingNo { get; set; }

        public string Size { get; set; }

        public string OutboundTrackingNo { get; set; }

        public string UnknownShipmentFlag { get; set; }

        public bool IsOrderOriginal { get; set; }

        public string CustCode { get; set; }

        public string TransNo { get; set; }

        public string TransName { get; set; }

        public string UploadOpe { get; set; }

        public string UploadStatus { get; set; }

        public string FailReason { get; set; }
    }
}