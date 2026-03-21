using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ErrorOrderSend
{
    public class ErrorOrderSendDetailModel
    {
        public int Id { get; set; }

        public string Customer { get; set; }

        public string Platform { get; set; }

        public string Phone { get; set; }

        public string TrackingNo { get; set; }

        public string LineUserId { get; set; }

        public SendType SendType { get; set; }

        public string IsSend { get; set; }

        public string SmsName { get; set; }

        public string Message { get; set; }

        public string SendResult { get; set; }

        public string SendResultMessage { get; set; }

        public string SmsRowId { get; set; }

        public string SmsCnt { get; set; }

        public string SmsErrorCode { get; set; }

        public DateTime? SendDateTime { get; set; }
    }
}
