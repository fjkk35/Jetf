using JETFWebAPI.Models.CompanyRegistration;
using JETFWebAPI.Models.Jetf;
using JETFWebAPI.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace JETFWebAPI.Controllers
{
    public class CompanyRegistrationController : ApiController
    {
        private readonly CompanyRegistrationService _companyRegistrationService;

        public CompanyRegistrationController()
        {
            _companyRegistrationService = new CompanyRegistrationService();
        }

        public async Task<IHttpActionResult> IsRegistration(CompanyRegistrationRequest request)
        {
            var response = await _companyRegistrationService.IsRegistration(request);

            return Ok(response);
        }

        public async Task<IHttpActionResult> IsCompanyMatch(CompanyMatchRequest request)
        {
            var response = await _companyRegistrationService.IsCompanyMatch(request);

            return Ok(response);
        }
    }
}
