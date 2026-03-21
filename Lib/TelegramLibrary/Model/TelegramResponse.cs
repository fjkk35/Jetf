using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibrary.Model
{
    public class TelegramResponse
    {
        public bool Ok { get; set; }
        public int Error_code { get; set; }  // 解析 error_code
        public string Description { get; set; }  // 解析 description
        public Parameters Parameters { get; set; }  // 解析 parameters
        public ResultData Result { get; set; } // 仍然保留，以適應成功回應
    }

    public class Parameters
    {
        public long Migrate_to_chat_id { get; set; }  // 解析 migrate_to_chat_id
    }


    public class ResultData
    {
        public int MessageId { get; set; }
        public User From { get; set; }
        public Chat Chat { get; set; }
        public long Date { get; set; }
        public string Text { get; set; }
    }

    public class User
    {
        public long Id { get; set; }
        public bool IsBot { get; set; }
        public string FirstName { get; set; }
        public string Username { get; set; }
    }

    public class Chat
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public bool AllMembersAreAdministrators { get; set; }
    }
}
