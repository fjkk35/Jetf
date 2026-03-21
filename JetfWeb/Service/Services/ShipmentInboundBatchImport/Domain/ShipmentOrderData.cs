namespace Service.Services.ShipmentInboundBatchImport.Domain
{
    /// <summary>
    /// 訂單資料類別
    /// </summary>
    public class ShipmentOrderData
    {
        public string DeliveryNo { get; set; }
        public string TrackingNo { get; set; }
        public string ImporterAddr { get; set; }
        public string ImporterPhone { get; set; }
        public string Importer { get; set; }
        public string CustCode { get; set; }
        public string TransName { get; set; }
        public string TransNo { get; set; }
    }
}
