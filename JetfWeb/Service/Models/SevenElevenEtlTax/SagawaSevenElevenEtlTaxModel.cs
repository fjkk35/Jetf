using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SevenElevenEtlTax
{
    public class SagawaSevenElevenEtlTaxModel
    {
        // 廠商代號
        public string ChildCode { get; set; }

        // 配送編號
        public string DlvInv { get; set; }

        // 出貨日期
        public string DataDate { get; set; }

        // 金額
        public string ToDlvCod { get; set; }

        // 廠商訂單編號
        public string OrderNo { get; set; }

        // 是否為最後一次出貨(Y:是 N:否)
        public string LastShipment { get; set; }

        // serviceType
        public string ServiceType { get; set; }

        // 門市店號
        public string EshopNo { get; set; }

        // eshopType
        public string EshopType { get; set; }
    }
}
