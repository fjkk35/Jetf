using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class CargoSignReceiptViewModel
    {
        public List<UrlItem> UrlList { get; set; }
    }

    public class UrlItem
    {
        public string Url { get; set; }
    }

}