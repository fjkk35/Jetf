using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.LineLogin
{
    public class LineUserProfileViewModel
    {
        public string UserId { get; set; }

        public string DisplayName { get; set; }

        public string Phone { get; set; }

        public bool IsBind { get; set; }

    }
}