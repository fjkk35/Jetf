using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class LineGroupModel
    {
        public string GroupId { get; set; } 
        public string GroupName { get; set; }
        public string Token { get; set; }
    }

    public class GetTokenFromCodeResult
    {
        public string status { get; set; }

        public string message { get; set; }

        public string access_token { get; set; }
    }
}
