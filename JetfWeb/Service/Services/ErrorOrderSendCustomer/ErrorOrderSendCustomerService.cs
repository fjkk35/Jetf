using Dapper;
using Org.BouncyCastle.Asn1.Mozilla;
using Service.Models;
using Service.Models.ErrorOrderSend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.ErrorOrderSendCustomer
{
    public class ErrorOrderSendCustomerService :_BaseService
    {

        /// <summary>
        /// 取得客戶平台對應
        /// </summary>
        /// <returns></returns>
        public List<CustomerPlatformMapping> GetCustomerPlatformMapping()
        {
            var sqlQuery = "SELECT * FROM jetf.dbo.CustomerPlatformMapping";

            return conn.Query<CustomerPlatformMapping>(sqlQuery).ToList();
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ResopnseModel Create(CustomerPlatformMapping data)
        {
            try
            {
                var isCustomerExists = IsCustomerExists(data.Customer);

                if(isCustomerExists)
                    return new ResopnseModel("客戶已存在");

                var sqlQuery = "INSERT INTO jetf.dbo.CustomerPlatformMapping (Customer, Platform) VALUES (@Customer, @Platform)";

                conn.Execute(sqlQuery, new { Customer = data.Customer, Platform = data.Platform });

                return new ResopnseModel() { };
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }

        public ResopnseModel Delete(int id)
        {
            try
            {
                var sqlQuery = "DELETE FROM jetf.dbo.CustomerPlatformMapping WHERE Id = @Id";

                conn.Execute(sqlQuery, new { Id = id });

                return new ResopnseModel() { };
            }
            catch (Exception ex)
            { 
                return new ResopnseModel(ex.Message);
            }
        }

        private bool IsCustomerExists(string customer) 
        { 
            var sqlQuery = "SELECT * FROM jetf.dbo.CustomerPlatformMapping WHERE Customer = @Customer";

            var result = conn.Query<CustomerPlatformMapping>(sqlQuery, new { Customer = customer }).ToList();

            return result.Any();
        }

    }
}
