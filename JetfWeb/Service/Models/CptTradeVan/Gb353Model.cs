using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class Gb353Model
    {
        public string Status { get; set; }
        public List<Item> Data { get; set; }
        public string Msg { get; set; }

        public class Item
        {
            public bool BlankOrNull { get; set; }
            public bool Checked { get; set; }
            public object DeclNo { get; set; }
            public bool Empty { get; set; }
            public string Hawb { get; set; }
            public string IssueDate { get; set; }
            public string IssueTime { get; set; }
            public bool KeySensitive { get; set; }
            public List<string> Keys { get; set; }
            public string Mawb { get; set; }
            public object PrimaryField { get; set; }
            public string RejReasonCode { get; set; }
            public string RejReasonDesc { get; set; }
            public List<string> Values { get; set; }
        }
    }
}
