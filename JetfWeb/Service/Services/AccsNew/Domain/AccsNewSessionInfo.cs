using System;

namespace Service.Services.AccsNew.Domain
{
    public class AccsNewSessionInfo
    {
        public string Token { get; set; }

        public bool IsLoggedIn { get; set; }

        public DateTime LoginTime { get; set; }
    }
}