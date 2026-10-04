using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PDTWebAPI.Models.Unpacking
{
    public class SwNatClearanceStatusModel
    {
        public string TransTypeCd { get; set; }
        public string Status { get; set; }
        public List<GridModel> GridModel { get; set; }
        public string Msg { get; set; }
    }

    public class GridModel
    {
        public bool BlankOrNull { get; set; }
        public string BrokerAgentNo { get; set; }
        public string BrokerBoxNo { get; set; }
        public bool Checked { get; set; }
        public string DutyType { get; set; }
        public bool Empty { get; set; }
        public string EvalType { get; set; }
        public string Hawb { get; set; }
        public string InspectType { get; set; }
        public string ItemNo { get; set; }
        public bool KeySensitive { get; set; }
        public List<string> Keys { get; set; }
        public string Mawb { get; set; }
        public string Memo { get; set; }
        public string PrimaryField { get; set; }
        public string ProDate { get; set; }
        public string ProOfficer { get; set; }
        public string ProTime { get; set; }
        public string ProType { get; set; }
        public string TransactionCode { get; set; }
        public string UnbCtrlRefNo { get; set; }
        public List<string> Values { get; set; }
        public string YnEvalType { get; set; }
    }


}