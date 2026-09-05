using CompanyRegistrationLibrary;
using Dapper;
using JETFWebAPI.Models.CompanyRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;

namespace JETFWebAPI.Services
{
    public class CompanyRegistrationService :_BaseService
    {
        private readonly CompanyRegistration _companyRegistration;

        public CompanyRegistrationService() 
        {
            _companyRegistration = new CompanyRegistration();
        }

        /// <summary>
        /// 確認帳號
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        private async Task<int> GetTotalCount(string account)
        {
            var sql = @"select TotalCount from jetf.dbo.CompanyRegistrationUser where Account=@Account ";

            var totalCount = conn.Query<int>(sql, new
            {
                Account = account
            }).FirstOrDefault();

            return totalCount;
        }

        /// <summary>
        /// 確認帳號使用次數
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        private async Task<CompanyRegistrationUserModel> GetCompanyRegistrationUser(string account,string dataDate)
        {
            var sql = @"
select a.TotalCount,b.Id,b.RequestCount from jetf.dbo.CompanyRegistrationUser a
left join jetf.dbo.CompanyRegistrationUsage b on a.Account=b.Account and b.DataDate=@DataDate
where a.Account=@Account  ";

            var result = conn.Query<CompanyRegistrationUserModel>(sql, new
            {
                DataDate = dataDate,
                Account = account
            }).FirstOrDefault();

            return result;
        }


        /// <summary>
        /// 更新公司登記使用次數
        /// </summary>
        /// <param name="account"></param>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        private bool Update(int id)
        {
            var sql = @"
                        update jetf.dbo.CompanyRegistrationUsage set RequestCount = RequestCount+1 where Id=@Id
                      ";

            return conn.Execute(sql, new { Id = id }) > 0;
        }

        /// <summary>
        /// 新增公司登記使用次數
        /// </summary>
        /// <param name="account"></param>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        private bool Insert(string account,string dataDate)
        {
            var sql = @"
                        insert jetf.dbo.CompanyRegistrationUsage(Account,DataDate,RequestCount) Values(@Account,@DataDate,1)
                      ";

            return conn.Execute(sql, 
                new 
                {
                    Account = account,
                    DataDate = dataDate,
                }) > 0;
        }

        /// <summary>
        /// 是否有公司登記
        /// </summary>
        /// <returns></returns>
        public async Task<CompanyRegistrationResponse> IsRegistration(CompanyRegistrationRequest request) 
        {
            //取得當日使用次數
            var dataDate = DateTime.Now.ToString("yyyyMMdd");
            var user = await GetCompanyRegistrationUser(request.Account, dataDate);

            if (user == null)
                return new CompanyRegistrationResponse("查無帳號");

            if (user != null && user.RequestCount > user.TotalCount)
                return new CompanyRegistrationResponse("今日已達使用上限");

            var result = await GetCompanyRegistration(request.BusinessNo);

            if (user.Id == 0)
                Insert(request.Account, dataDate);
            else
                Update(user.Id);

            return result;
        }

        /// <summary>
        /// 是否公司名稱與統一編號符合
        /// </summary>
        /// <param name="account"></param>
        /// <param name="businessNo"></param>
        /// <param name="companyName"></param>
        /// <returns></returns>
        public async Task<CompanyMatchResponse> IsCompanyMatch(CompanyMatchRequest request)
        {
            //取得當日使用次數
            var dataDate = DateTime.Now.ToString("yyyyMMdd");
            var user = await GetCompanyRegistrationUser(request.Account, dataDate);

            if (user == null)
                return new CompanyMatchResponse("查無帳號");

            if (user != null && user.RequestCount > user.TotalCount)
                return new CompanyMatchResponse("今日已達使用上限");

            var result = await GetCompanyMatch(request.BusinessNo, request.CompanyName);

            if (user.Id == 0)
                Insert(request.Account, dataDate);
            else
                Update(user.Id);

            return result;
        }

        private async Task<CompanyMatchResponse> GetCompanyMatch(string businessNo, string companyName)
        {
            try
            {
                var companyResult = await _companyRegistration.GetCompanyRegistration(businessNo);

                //登記、公司名稱是否相同
                if (companyResult.IsRegistration && companyResult.Company_Name == companyName?.Trim())
                    return new CompanyMatchResponse(true);

                var businessResult = await _companyRegistration.GetBusinessRegistryModel(businessNo);

                //登記、公司名稱是否相同
                if (businessResult.IsRegistration && businessResult.Business_Name == companyName?.Trim())
                    return new CompanyMatchResponse(true);

                return new CompanyMatchResponse(false);
            }
            catch (Exception ex)
            {
                return new CompanyMatchResponse(ex.Message);
            }
        }



        private async Task<CompanyRegistrationResponse> GetCompanyRegistration(string businessNo) 
        {
            try
            {
                var companyResult = await _companyRegistration.GetCompanyRegistration(businessNo);

                if (companyResult.IsRegistration)
                    return new CompanyRegistrationResponse(true);

                var businessResult = await _companyRegistration.GetBusinessRegistryModel(businessNo);

                if (businessResult.IsRegistration)
                    return new CompanyRegistrationResponse(true);

                return new CompanyRegistrationResponse(false);
            }
            catch (Exception ex)
            {
                return new CompanyRegistrationResponse(ex.Message);
            }
        }
    }
}