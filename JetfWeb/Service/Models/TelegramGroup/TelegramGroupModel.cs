using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.TelegramGroup
{
    public class TelegramGroupModel
    {
        public string GroupId { get; set; }

        public string GroupName { get; set; }

        public string ChatId { get; set; }

        public string CrtUser { get; set; }

        public DateTime CrtDateTime { get; set; }
    }
}
