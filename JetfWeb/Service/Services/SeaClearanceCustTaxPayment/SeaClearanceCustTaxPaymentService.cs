using Dapper;
using Service.Models.ErrorOrderSend;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Models.SeaClearanceCustTaxPayment;
using Service.Extensions;

namespace Service.Services.SeaClearanceCustTaxPayment
{
    public class SeaClearanceCustTaxPaymentService :_BaseService
    {
        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ResopnseModel Create(SeaClearanceCustTaxPaymentModel data)
        {
            try
            {
                var isCustomerExists = IsCustomerExists(data.CustCode);

                if (isCustomerExists)
                    return new ResopnseModel("客戶已存在");

                var sqlQuery = "INSERT INTO [jetf].[dbo].[SeaClearanceCustTaxPayment] (CustCode, TaxPayment) VALUES (@CustCode, @TaxPayment)";

                conn.Execute(sqlQuery, new { CustCode = data.CustCode, TaxPayment = data.TaxPayment.ToString() });

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
                var sqlQuery = "DELETE FROM [jetf].[dbo].[SeaClearanceCustTaxPayment] WHERE Id = @Id";

                conn.Execute(sqlQuery, new { Id = id });

                return new ResopnseModel() { };
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }

        public List<SeaClearanceCustTaxPaymentModel> GetSeaClearanceCustTaxPayment() 
        {
            var sql = @"
                    SELECT [Id],[CustCode],b.Cust_Name as CustName,[TaxPayment] FROM [jetf].[dbo].[SeaClearanceCustTaxPayment] a
                    join DATA_CENTER.dbo.SYS_CUST b on a.CustCode =b.CUST_CODE";

            var list = conn.Query<SeaClearanceCustTaxPaymentModel>(sql).ToList();

            foreach (var item in list)
            {
                item.TaxPaymentDisplay = item.TaxPayment.ToDescription();
            }

            return list;
        }

        private bool IsCustomerExists(string custCode)
        {
            var sqlQuery = "SELECT * FROM [jetf].[dbo].[SeaClearanceCustTaxPayment] WHERE CustCode = @CustCode";

            var result = conn.Query<CustomerPlatformMapping>(sqlQuery, new { CustCode = custCode }).ToList();

            return result.Any();
        }
    }
}
