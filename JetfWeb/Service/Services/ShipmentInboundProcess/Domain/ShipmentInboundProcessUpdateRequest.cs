namespace Service.Services.ShipmentInboundProcess.Domain
{
    public class ShipmentInboundProcessUpdateRequest
    {
        public int Id { get; set; }
        public byte ProcessType { get; set; }
        public byte? ProcessTransNo { get; set; }
        public string ProcessImporter { get; set; }
        public string ProcessImporterPhone { get; set; }
        public string ProcessImporterAddr { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public byte? FreightPayerNo { get; set; }
        public int? FreightFee { get; set; }
        public int Fee { get; set; }
        public string CarNo { get; set; }
        public string PickupTime { get; set; }
        public string Remark { get; set; }
        public int Tax { get; set; }
        public int Ccfee { get; set; }
        public int Cod { get; set; }
    }
}
