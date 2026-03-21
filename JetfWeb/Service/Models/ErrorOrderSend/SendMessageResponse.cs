using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ErrorOrderSend
{
    public class SendMessageResponse
    {
        public int Id { get; set; }

        public string SendResult { get; set; }

        public string SendResultMessage { get; set; }

        public string SmsRowId { get; set; }

        public string SmsCnt { get; set; }

        public string SmsErrorCode { get; set; }
    }
}
