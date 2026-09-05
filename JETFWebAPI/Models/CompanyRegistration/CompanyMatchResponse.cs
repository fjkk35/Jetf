using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.CompanyRegistration
{
    public class CompanyMatchResponse
    {
        public CompanyMatchResponse()
        {
            IsSuccess = true;
        }

        public CompanyMatchResponse(bool isMatch)
        {
            IsSuccess = true;
            IsMatch = isMatch;
        }

        public CompanyMatchResponse(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; set; }

        public bool? IsMatch { get; set; }

        public string ErrorMessage { get; set; }
    }
}