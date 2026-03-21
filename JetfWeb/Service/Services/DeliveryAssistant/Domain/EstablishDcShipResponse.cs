using System.Collections.Generic;

namespace Service.Services.DeliveryAssistant.Domain
{
    /// <summary>
    /// 建立車次 API 回應資料
    /// </summary>
    public class EstablishDcShipResponse
    {
        /// <summary>
        /// 結果代碼
        /// </summary>
        public string resultCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string error { get; set; }

        /// <summary>
        /// 車次資料
        /// </summary>
        public EstablishDcShipRow row { get; set; }

        /// <summary>
        /// 客戶訂單資料清單
        /// </summary>
        public List<EstablishDcShipResponseOrderRow> rows { get; set; }
    }

    /// <summary>
    /// 建立車次 API 車次資料
    /// </summary>
    public class EstablishDcShipRow
    {
        /// <summary>
        /// 車次編號
        /// </summary>
        public string dcShip { get; set; }

        /// <summary>
        /// 車號
        /// </summary>
        public string carId { get; set; }

        /// <summary>
        /// 駕駛代碼
        /// </summary>
        public string driverId { get; set; }

        /// <summary>
        /// 到貨日期
        /// </summary>
        public string arriveDate { get; set; }
    }

    /// <summary>
    /// 建立車次 API 客戶訂單資料
    /// </summary>
    public class EstablishDcShipResponseOrderRow
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
