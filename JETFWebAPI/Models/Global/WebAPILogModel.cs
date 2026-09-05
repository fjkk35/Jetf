using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Global
{
    public class WebAPILogModel
    {
        public string ControlNmae { get; set; }
        public string ActionName { get; set; }
        public string RequestData { get; set; }
        public string ResponseData { get; set; }
        public string Remark { get; set; } = "";
    }
}