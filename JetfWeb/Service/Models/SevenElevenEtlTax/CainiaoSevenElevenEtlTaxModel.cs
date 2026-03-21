using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SevenElevenEtlTax
{
    public class CainiaoSevenElevenEtlTaxModel
    {
        // 母代號
        public string ParentCode { get; set; }

        // 子代號
        public string ChildCode { get; set; }

        // 配送編號
        public string DLV_INV { get; set; }

        public string DlvInv => DLV_INV;

        // 服務類型
        public string ServiceType { get; set; }

        // 出貨單金額
        public string TO_DLV_COD { get; set; }

        public string ToDlvCod => TO_DLV_COD;
    }
}
