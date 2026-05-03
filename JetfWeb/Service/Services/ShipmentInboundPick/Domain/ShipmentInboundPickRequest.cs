using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.ShipmentInboundPick.Domain
{
    public class ShipmentInboundPickRequest
    {
        public string ProcessTimeStart { get; set; }
        public string ProcessTimeEnd { get; set; }
        public ShipmentInboundProcessType? ProcessType { get; set; }
        
        /// <summary>
        /// 客戶
        /// </summary>
        public List<string> CustCodes { get; set; }
    }
}
