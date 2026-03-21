using Service.EnumTax;

namespace Service.Services.ShipmentInboundWarehouseProcess.Domain
{
    /// <summary>
    /// 倉庫處理狀態更新請求
    /// </summary>
    public class ShipmentInboundWarehouseProcessUpdateRequest
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 處理狀態
        /// </summary>
        public WarehouseProcessType WarehouseProcessType { get; set; }
    }
}
