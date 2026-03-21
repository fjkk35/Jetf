using Service.EnumTax;
using Service.Models.ErrorOrderSend;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.ErrorOrderSend
{
    public class ErrorOrderSendViewModel
    {
        [Display(Name = "罐頭簡訊")]
        public int SmsMessageId { get; set; }

        [Display(Name = "罐頭簡訊")]
        public IEnumerable<SelectListItem> SmsMessageList { get; set; }

        public List<ErrorOrderSendModel> ErrorOrderSendList { get; set; }
    }
}