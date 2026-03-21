using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.Cargo
{
    public class ShenzhenCargoViewModel
    {
        public List<ShenzhenCargo> List { get; set; }
    }

    public class ShenzhenCargo
    {
        public string TrackingNo { get; set; }

        public string DeliveryNo { get; set; }
    }
}