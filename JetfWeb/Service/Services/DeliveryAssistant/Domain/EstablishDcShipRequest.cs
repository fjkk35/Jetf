using System.Collections.Generic;

namespace Service.Services.DeliveryAssistant.Domain
{
    /// <summary>
    /// 建立車次 API 請求資料
    /// </summary>
    public class EstablishDcShipRequest
    {
        /// <summary>
        /// 車次編號
        /// </summary>
        public string dcShip { get; set; }

        /// <summary>
        /// 到貨日期
        /// </summary>
        public string arriveDate { get; set; }

        /// <summary>
        /// 駕駛資料
        /// </summary>
        public EstablishDcShipDriverData driverData { get; set; }

        /// <summary>
        /// 客戶訂單資料清單
        /// </summary>
        public List<EstablishDcShipCusOrderInfo> cusOrderInfoList { get; set; }
    }

    /// <summary>
    /// 建立車次 API 駕駛資料
    /// </summary>
    public class EstablishDcShipDriverData
    {
        /// <summary>
        /// 駕駛代碼
        /// </summary>
        public string driverId { get; set; }

        public string driverName { get; set; }
    }

    /// <summary>
    /// 建立車次 API 客戶訂單資料
    /// </summary>
    public class EstablishDcShipCusOrderInfo
    {
        /// <summary>
        /// 客戶單號
        /// </summary>
        public string cusOrder { get; set; }

        /// <summary>
        /// 到貨日期
        /// </summary>
        public string arriveDate { get; set; }
    }
}
