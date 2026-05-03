namespace Service.Services.ShipmentInboundBatchImport.Domain
{
    /// <summary>
    /// 稅金資料模型
    /// </summary>
    public class ShipmentFeeData
    {
        public string TrackingNo { get; set; }
        public int? Tax { get; set; }
        public int? Ccfee { get; set; }
        public int? Cod { get; set; }
        public int? Fee { get; set; }
    }
}
