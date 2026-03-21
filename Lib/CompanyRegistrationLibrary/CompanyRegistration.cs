using CompanyRegistrationLibrary.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompanyRegistrationLibrary
{
    public class CompanyRegistration
    {
        /// <summary>
        /// 公司登記
        /// </summary>
        /// <param name="businessNo"></param>
        /// <returns></returns>
        public async Task<CompanyRegistrationModel> GetCompanyRegistration(string businessNo) 
        {
            //https://data.gcis.nat.gov.tw/od/detail?oid=8776818F-EB3C-445F-BE95-AE22577CBEBC

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient())
            {
                string url = $@"https://data.gcis.nat.gov.tw/od/data/api/5F64D864-61CB-4D0D-8AD9-492047CC1EA6?$format=json&$filter=Business_Accounting_NO eq {businessNo}&$skip=0&$top=1 ";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string jsonResult = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<List<CompanyRegistrationModel>>(jsonResult)?.FirstOrDefault();
               
                if (result == null)
                    return new CompanyRegistrationModel();

                return result;
            }
        }

        /// <summary>
        /// 商業登記
        /// </summary>
        /// <param name="businessNo"></param>
        /// <returns></returns>
        public async Task<BusinessRegistryModel> GetBusinessRegistryModel(string businessNo)
        {
            //https://data.gcis.nat.gov.tw/od/detail?oid=06BCF9F6-A6D0-4F82-A1F2-EA08144D7057

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient())
            {
                string url = $@"https://data.gcis.nat.gov.tw/od/data/api/426D5542-5F05-43EB-83F9-F1300F14E1F1?$format=json&$filter=President_No eq {businessNo}&$skip=0&$top=50";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string jsonResult = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<List<BusinessRegistryModel>>(jsonResult)?.LastOrDefault();

                if(result == null)
                    return new BusinessRegistryModel();

                return result;
            }
        }
    }
}
