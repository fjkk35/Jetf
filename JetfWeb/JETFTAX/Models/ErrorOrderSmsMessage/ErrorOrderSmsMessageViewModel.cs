using Service.Models.ErrorOrderSmsMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.ErrorOrderSmsMessage
{
    public class ErrorOrderSmsMessageViewModel
    {
        public List<ErrorOrderSmsMessageModel> List { get; set; }
    }

    public class ErrorOrderSmsMessageDetailViewModel
    {
        public ErrorOrderSmsMessageModel SmsMessage { get; set; }
    }

}