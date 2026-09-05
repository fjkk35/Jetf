using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.CompanyRegistration
{
    public class CompanyRegistrationResponse
    {
        public CompanyRegistrationResponse() 
        {
            IsSuccess = true;
        }

        public CompanyRegistrationResponse(bool isRegistration) 
        {
            IsSuccess = true;
            IsRegistration = isRegistration;
        }

        public CompanyRegistrationResponse(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; set; }

        public bool? IsRegistration { get; set; }

        public string ErrorMessage { get; set; }
    }
}