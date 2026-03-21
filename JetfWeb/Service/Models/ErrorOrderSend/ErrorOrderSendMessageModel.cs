using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ErrorOrderSend
{
    public class ErrorOrderSendMessageModel
    {
        public ErrorOrderType Type { get; set; }

        public SendType SendType { get; set; }

        public string Message { get; set; }
    }
}
