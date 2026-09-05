using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.CompanyRegistration
{
    public class CompanyRegistrationRequest
    {
        /// <summary>
        /// 帳號
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 統編
        /// </summary>
        public string BusinessNo { get; set; }
    }
}