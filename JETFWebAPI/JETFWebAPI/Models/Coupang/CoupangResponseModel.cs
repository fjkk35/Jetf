using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Coupang
{
    public class ManifestResponseModel
    {
        public string resultCode { get; set; }
        public string data { get; set; }
        public string resultMessage { get; set; } = "";
        public string resultDetail { get; set; } = "";
    }

    public class CargoManifestResponseModel
    {
        public string resultCode { get; set; }
        public string data { get; set; }
        public string resultMessage { get; set; } = "";
        public string resultDetail { get; set; } = "";
    }


}