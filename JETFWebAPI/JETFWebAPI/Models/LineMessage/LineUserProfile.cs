using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.LineMessage
{
    public class LineUserProfile
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string PictureUrl { get; set; }
        public string StatusMessage { get; set; }
    }
}