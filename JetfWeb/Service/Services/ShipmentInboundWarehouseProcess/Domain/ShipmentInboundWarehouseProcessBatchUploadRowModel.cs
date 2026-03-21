using System;

namespace Service.Services.ShipmentInboundWarehouseProcess.Domain
{
    public class ShipmentInboundWarehouseProcessBatchUploadRowModel
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 處理狀態(中文)
        /// </summary>
        public string WarehouseProcessTypeText { get; set; }

        /// <summary>
        /// 處理狀態(Enum 值)
        /// </summary>
        public byte? WarehouseProcessType { get; set; }

        /// <summary>
        /// 第幾列(Excel Row)
        /// </summary>
        public int RowNo { get; set; }
    }
}
