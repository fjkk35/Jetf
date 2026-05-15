using Dapper;
using Service.Models;
using Service.Models.SeaClearanceCustomer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearanceCustomer
{
    public class SeaClearanceCustomerService : _BaseService
    {
        public SeaClearanceCustomerService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得所有已選擇的客戶
        /// </summary>
        /// <returns></returns>
        public List<SeaClearanceCustomerModel> GetSelectedCustomers()
        {
            var sql = @"
                SELECT Cust_Code, Cust_Name
                FROM jetf.dbo.SeaClearanceCustomer
                ORDER BY Cust_Code
            ";

            return conn.Query<SeaClearanceCustomerModel>(sql).ToList();
        }

        /// <summary>
        /// 取得所有可用的客戶（來源自 DATA_CENTER）
        /// </summary>
        /// <returns></returns>
        public List<AvailableCustomerModel> GetAvailableCustomers()
        {
            var sql = @"
                SELECT 
                    dc.Cust_Code,
                    dc.Cust_Name,
                    CASE WHEN sc.Cust_Code IS NOT NULL THEN 1 ELSE 0 END as IsSelected
                FROM DATA_CENTER.dbo.SYS_CUST dc
                LEFT JOIN jetf.dbo.SeaClearanceCustomer sc ON dc.Cust_Code = sc.Cust_Code
                WHERE dc.CUST_TYPE = 'SEA'
                ORDER BY dc.Cust_Code
            ";

            return conn.Query<AvailableCustomerModel>(sql).ToList();
        }

        /// <summary>
        /// 批量新增客戶
        /// </summary>
        /// <param name="customerCodes">客戶代碼列表</param>
        /// <returns></returns>
        public ResponseModel AddCustomers(List<string> customerCodes)
        {
            if (customerCodes == null || !customerCodes.Any())
            {
                return new ResponseModel("請選擇要新增的客戶");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var addedCount = 0;
                    var skippedCount = 0;

                    foreach (var custCode in customerCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        // 檢查客戶是否已存在
                        var existsSql = @"
                            SELECT COUNT(*) 
                            FROM jetf.dbo.SeaClearanceCustomer 
                            WHERE Cust_Code = @Cust_Code
                        ";
                        var exists = conn.QuerySingle<int>(existsSql, new { Cust_Code = custCode }, transaction) > 0;

                        if (exists)
                        {
                            skippedCount++;
                            continue;
                        }

                        // 從 DATA_CENTER 取得客戶資料
                        var customerSql = @"
                            SELECT Cust_Code, Cust_Name
                            FROM DATA_CENTER.dbo.SYS_CUST
                            WHERE Cust_Code = @Cust_Code AND CUST_TYPE = 'SEA'
                        ";
                        var customer = conn.QueryFirstOrDefault<SeaClearanceCustomerModel>(customerSql, new { Cust_Code = custCode }, transaction);

                        if (customer == null)
                        {
                            return new ResponseModel($"客戶代碼 {custCode} 不存在或不是海運客戶");
                        }

                        // 新增客戶
                        var insertSql = @"
                            INSERT INTO jetf.dbo.SeaClearanceCustomer (Cust_Code, Cust_Name)
                            VALUES (@Cust_Code, @Cust_Name)
                        ";
                        conn.Execute(insertSql, customer, transaction);
                        addedCount++;
                    }

                    transaction.Commit();

                    var message = $"操作完成：新增 {addedCount} 個客戶";
                    if (skippedCount > 0)
                    {
                        message += $"，跳過 {skippedCount} 個已存在的客戶";
                    }

                    return new ResponseModel { ReturnObject = new { AddedCount = addedCount, SkippedCount = skippedCount, Message = message } };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResponseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 批量刪除客戶
        /// </summary>
        /// <param name="customerCodes">客戶代碼列表</param>
        /// <returns></returns>
        public ResponseModel DeleteCustomers(List<string> customerCodes)
        {
            if (customerCodes == null || !customerCodes.Any())
            {
                return new ResponseModel("請選擇要刪除的客戶");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查是否有客戶正在使用中（這裡可以根據實際業務需求添加檢查邏輯）
                    // 例如：檢查是否有相關的報關記錄等

                    var deletedCount = 0;
                    foreach (var custCode in customerCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        var deleteSql = @"
                            DELETE FROM jetf.dbo.SeaClearanceCustomer 
                            WHERE Cust_Code = @Cust_Code
                        ";
                        var affected = conn.Execute(deleteSql, new { Cust_Code = custCode }, transaction);
                        deletedCount += affected;
                    }

                    transaction.Commit();

                    var message = $"成功刪除 {deletedCount} 個客戶";
                    return new ResponseModel { ReturnObject = new { DeletedCount = deletedCount, Message = message } };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResponseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 檢查客戶是否存在於 DATA_CENTER
        /// </summary>
        /// <param name="custCode">客戶代碼</param>
        /// <returns></returns>
        public bool IsValidCustomer(string custCode)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM DATA_CENTER.dbo.SYS_CUST 
                WHERE Cust_Code = @Cust_Code AND CUST_TYPE = 'SEA'
            ";

            return conn.QuerySingle<int>(sql, new { Cust_Code = custCode }) > 0;
        }

        /// <summary>
        /// 根據客戶代碼取得客戶名稱
        /// </summary>
        /// <param name="custCode">客戶代碼</param>
        /// <returns></returns>
        public string GetCustomerName(string custCode)
        {
            var sql = @"
                SELECT Cust_Name 
                FROM DATA_CENTER.dbo.SYS_CUST 
                WHERE Cust_Code = @Cust_Code AND CUST_TYPE = 'SEA'
            ";

            return conn.QueryFirstOrDefault<string>(sql, new { Cust_Code = custCode });
        }
    }
}