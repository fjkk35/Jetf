using Service.EnumTax;
using Service.Extensions;
using System;

namespace Service.Services.ShipmentInboundWarehouseProcess.Domain
{
    /// <summary>
    /// 倉庫處理狀態模型
    /// </summary>
    public class ShipmentInboundWarehouseProcessModel
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 處理狀態
        /// </summary>
        public WarehouseProcessType? WarehouseProcessType { get; set; }

        /// <summary>
        /// 處理狀態名稱
        /// </summary>
        public string WarehouseProcessTypeName => WarehouseProcessType?.ToDescription();

        /// <summary>
        /// 處理時間
        /// </summary>
        public DateTime? WarehouseProcessTime { get; set; }

        /// <summary>
        /// 出庫日期
        /// </summary>
        public DateTime? OutboundDate { get; set; }

        /// <summary>
        /// 處理人員
        /// </summary>
        public string WarehouseProcessOpe { get; set; }
    }
}
