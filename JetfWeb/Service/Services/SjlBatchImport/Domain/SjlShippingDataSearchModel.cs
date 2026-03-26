using System;

namespace Service.Services.SjlBatchImport.Domain
{
    /// <summary>
    /// 捷利托運資料查詢列資料。
    /// </summary>
    public class SjlShippingDataSearchModel
    {
        public int Id { get; set; }

        public string JetfSerial { get; set; }

        public string BagNumber { get; set; }

        public string Seq { get; set; }

        public string Importer { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public decimal? OtherFee { get; set; }

        public decimal? Cod { get; set; }

        public string ImporterAddr { get; set; }

        public string ItemName { get; set; }

        public int? Qty { get; set; }

        public decimal? Volume { get; set; }

        public decimal? Gw { get; set; }

        public string ImporterPhone { get; set; }

        public string TransName { get; set; }

        public DateTime? CreatedTime { get; set; }
    }
}