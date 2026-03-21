using Dapper;
using Service.Models;
using Service.Models.ErrorOrderSend;
using Service.Models.SeaClearanceCustTaxPayment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearanceFee
{
    public class SeaClearanceFeeService : _BaseService
    {
        public List<SeaClearanceFeeModel> GetSeaClearanceFee() 
        {
            var sql = @"
                        SELECT [Id],[CustCode],b.CUST_NAME as CustName,[G1Fee],[MoveWarehouseFee],[TransferG1Fee],[TransferWarehouseFee],[X2Fee] FROM [jetf].[dbo].[SeaClearanceFee] a
                        join  [DATA_CENTER].[dbo].[SYS_CUST] b on a.CustCode=b.Cust_Code
                        order by CustCode
                        ";

            return conn.Query<SeaClearanceFeeModel>(sql).ToList();
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ResopnseModel Create(SeaClearanceFeeModel data)
        {
            try
            {
                var isCustomerExists = IsCustomerExists(data.CustCode);

                if (isCustomerExists)
                    return new ResopnseModel("客戶已存在");

                var sqlQuery = "INSERT INTO [jetf].[dbo].[SeaClearanceFee] (CustCode,G1Fee,MoveWarehouseFee,TransferG1Fee,TransferWarehouseFee,X2Fee) VALUES (@CustCode, @G1Fee,@MoveWarehouseFee,@TransferG1Fee,@TransferWarehouseFee,@X2Fee)";

                conn.Execute(sqlQuery,new 
                {
                    G1Fee = data.G1Fee,
                    MoveWarehouseFee = data.MoveWarehouseFee,
                    TransferG1Fee = data.TransferG1Fee,
                    TransferWarehouseFee = data.TransferWarehouseFee,
                    X2Fee = data.X2Fee,
                    CustCode = data.CustCode
                });


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
                var sqlQuery = "DELETE FROM [jetf].[dbo].[SeaClearanceFee] WHERE Id = @Id";

                conn.Execute(sqlQuery, new { Id = id });

                return new ResopnseModel() { };
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }

       

        private bool IsCustomerExists(string custCode)
        {
            var sqlQuery = "SELECT * FROM [jetf].[dbo].[SeaClearanceFee] WHERE CustCode = @CustCode";

            var result = conn.Query<CustomerPlatformMapping>(sqlQuery, new { CustCode = custCode }).ToList();

            return result.Any();
        }
    }
}
