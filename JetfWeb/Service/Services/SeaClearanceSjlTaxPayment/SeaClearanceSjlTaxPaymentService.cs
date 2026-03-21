using Dapper;
using Service.Models.SeaClearanceCustTaxPayment;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Models.ErrorOrderSend;
using Service.Models.SeaClearanceSjlTaxPayment;
using Service.Extensions;

namespace Service.Services.SeaClearanceSjlTaxPayment
{
    public class SeaClearanceSjlTaxPaymentService : _BaseService
    {
        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ResopnseModel Create(SeaClearanceSjlTaxPaymentModel data)
        {
            try
            {
                var isImporterExists = IsImporterExists(data.Importer);

                if (isImporterExists)
                    return new ResopnseModel("申報人已存在");

                var sqlQuery = "INSERT INTO [jetf].[dbo].[SeaClearanceSjlTaxPayment] (Importer, TaxPayment) VALUES (@Importer, @TaxPayment)";

                conn.Execute(sqlQuery, new { Importer = data.Importer, TaxPayment = data.TaxPayment.ToString() });

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
                var sqlQuery = "DELETE FROM [jetf].[dbo].[SeaClearanceSjlTaxPayment] WHERE Id = @Id";

                conn.Execute(sqlQuery, new { Id = id });

                return new ResopnseModel() { };
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }

        public List<SeaClearanceSjlTaxPaymentModel> GetSeaClearanceSjlTaxPayment()
        {
            var sql = @"
                    SELECT * FROM [jetf].[dbo].[SeaClearanceSjlTaxPayment]";

            var list = conn.Query<SeaClearanceSjlTaxPaymentModel>(sql).ToList();

            foreach (var item in list)
            {
                item.TaxPaymentDisplay = item.TaxPayment.ToDescription();
            }

            return list;
        }

        private bool IsImporterExists(string importer)
        {
            var sqlQuery = "SELECT * FROM [jetf].[dbo].[SeaClearanceSjlTaxPayment] WHERE Importer = @Importer";

            var result = conn.Query<SeaClearanceSjlTaxPaymentModel>(sqlQuery, new { Importer = importer }).ToList();

            return result.Any();
        }
    }
}
