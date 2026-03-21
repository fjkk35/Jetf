using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class DialogLogCargoStatusViewModel
    {
        public List<DialogLogCargoStatus> DialogLogCargoStatusList { get; set; }
    }

    public class DialogLogCargoStatus
    {
        public string Dlv_Inv { get; set; }
        public string Search_Time { get; set; }
        public string User_Id { get; set; }
        public string User_Ip { get; set; }
    }
}