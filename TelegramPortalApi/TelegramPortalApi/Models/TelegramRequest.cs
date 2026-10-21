using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TelegramPortalApi.Models
{
    public class TelegramRequest
    {
        public string GroupId { get; set; }

        public string Message { get; set; }

        public string Jpg { get; set; }

        public SendType SendType { get; set; }
    }

    public enum SendType
    {
        Text = 1,
        Photo = 2,
        Document = 3,
    }
}