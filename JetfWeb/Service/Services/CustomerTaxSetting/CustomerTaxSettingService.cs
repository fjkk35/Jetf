using Dapper;
using Service.Models;
using Service.Models.CustomerTaxSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CustomerTaxSetting
{
    public class CustomerTaxSettingService : _BaseService
    {
        /// <summary>
        /// 取得SEA客戶列表
        /// </summary>
        /// <returns></returns>
        public List<SeaCustomerModel> GetSeaCustomers()
        {
            string sql = @"
                SELECT 
                    dc.Cust_Code,
                    dc.Cust_Name
                FROM DATA_CENTER.dbo.SYS_CUST dc
                WHERE dc.CUST_TYPE = 'SEA'
                ORDER BY dc.Cust_Code";

            return conn.Query<SeaCustomerModel>(sql).ToList();
        }

        /// <summary>
        /// 取得所有稅金時間
        /// </summary>
        /// <returns></returns>
        public List<TaxTimeModel> GetTaxTimes()
        {
            string sql = @"
                SELECT Id, TaxTime
                FROM jetf.dbo.TaxTime
                ORDER BY TaxTime";

            return conn.Query<TaxTimeModel>(sql).ToList();
        }

        /// <summary>
        /// 取得客戶稅金時間設定列表
        /// </summary>
        /// <returns></returns>
        public List<CustomerTaxSettingDisplayModel> GetCustomerTaxSettings()
        {
            // 先取得所有稅金時間
            var taxTimes = GetTaxTimes();
            
            // 取得所有客戶設定
            string sql = @"
                SELECT 
                    cts.Cust_Code,
                    dc.Cust_Name,
                    tt.TaxTime
                FROM jetf.dbo.CustomerTaxSetting cts
                INNER JOIN DATA_CENTER.dbo.SYS_CUST dc ON cts.Cust_Code = dc.Cust_Code
                INNER JOIN jetf.dbo.CustomerTaxTime ctt ON cts.Id = ctt.CustomerTaxSettingId
                INNER JOIN jetf.dbo.TaxTime tt ON ctt.TaxTimeId = tt.Id
                WHERE dc.CUST_TYPE = 'SEA'
                ORDER BY cts.Cust_Code, tt.TaxTime";

            var customerSettings = conn.Query<dynamic>(sql).ToList();

            // 組合結果
            var result = new List<CustomerTaxSettingDisplayModel>();
            var customerGroups = customerSettings.GroupBy(x => new { CustCode = (string)x.Cust_Code, CustName = (string)x.Cust_Name });

            foreach (var group in customerGroups)
            {
                var setting = new CustomerTaxSettingDisplayModel
                {
                    Cust_Code = group.Key.CustCode,
                    Cust_Name = group.Key.CustName
                };

                // 初始化所有稅金時間為false
                foreach (var taxTime in taxTimes)
                {
                    setting.TaxTimeSettings[taxTime.TaxTime] = false;
                }

                // 設定已選擇的稅金時間為true
                foreach (var item in group)
                {
                    string taxTime = (string)item.TaxTime;
                    setting.TaxTimeSettings[taxTime] = true;
                }

                result.Add(setting);
            }

            return result;
        }

        /// <summary>
        /// 取得特定客戶的稅金時間設定
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <returns></returns>
        public CustomerTaxSettingModel GetCustomerTaxSetting(string custCode)
        {
            string sql = @"
                SELECT 
                    cts.Id,
                    cts.Cust_Code,
                    dc.Cust_Name,
                    tt.Id as TaxTimeId,
                    tt.TaxTime
                FROM jetf.dbo.CustomerTaxSetting cts
                INNER JOIN DATA_CENTER.dbo.SYS_CUST dc ON cts.Cust_Code = dc.Cust_Code
                INNER JOIN jetf.dbo.CustomerTaxTime ctt ON cts.Id = ctt.CustomerTaxSettingId
                INNER JOIN jetf.dbo.TaxTime tt ON ctt.TaxTimeId = tt.Id
                WHERE cts.Cust_Code = @CustCode AND dc.CUST_TYPE = 'SEA'
                ORDER BY tt.TaxTime";

            var results = conn.Query<dynamic>(sql, new { CustCode = custCode }).ToList();

            if (!results.Any())
            {
                // 如果沒有設定，回傳客戶基本資訊
                var customerSql = @"
                    SELECT Cust_Code, Cust_Name
                    FROM DATA_CENTER.dbo.SYS_CUST
                    WHERE Cust_Code = @CustCode AND CUST_TYPE = 'SEA'";

                var customer = conn.QueryFirstOrDefault<dynamic>(customerSql, new { CustCode = custCode });
                
                if (customer != null)
                {
                    return new CustomerTaxSettingModel
                    {
                        Cust_Code = (string)customer.Cust_Code,
                        Cust_Name = (string)customer.Cust_Name
                    };
                }
                
                return null;
            }

            var first = results.First();
            var model = new CustomerTaxSettingModel
            {
                Id = (int)first.Id,
                Cust_Code = (string)first.Cust_Code,
                Cust_Name = (string)first.Cust_Name
            };

            foreach (var item in results)
            {
                model.TaxTimeIds.Add((int)item.TaxTimeId);
                model.TaxTimes.Add((string)item.TaxTime);
            }

            return model;
        }

        /// <summary>
        /// 儲存客戶稅金時間設定
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <param name="taxTimeIds">稅金時間ID列表</param>
        /// <returns></returns>
        public ResopnseModel SaveCustomerTaxSetting(string custCode, List<int> taxTimeIds)
        {
            try
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 檢查客戶是否存在且為SEA類型
                        string checkCustomerSql = @"
                            SELECT COUNT(1)
                            FROM DATA_CENTER.dbo.SYS_CUST
                            WHERE Cust_Code = @CustCode AND CUST_TYPE = 'SEA'";

                        var customerExists = conn.QuerySingle<int>(checkCustomerSql, new { CustCode = custCode }, transaction);
                        if (customerExists == 0)
                        {
                            throw new Exception("客戶不存在或非SEA客戶");
                        }

                        // 檢查是否已有設定
                        string checkSettingSql = @"
                            SELECT Id FROM jetf.dbo.CustomerTaxSetting WHERE Cust_Code = @CustCode";

                        var existingSettingId = conn.QueryFirstOrDefault<int?>(checkSettingSql, new { CustCode = custCode }, transaction);

                        int settingId;
                        if (existingSettingId.HasValue)
                        {
                            // 更新：刪除舊的稅金時間設定
                            settingId = existingSettingId.Value;
                            string deleteTaxTimesSql = @"
                                DELETE FROM jetf.dbo.CustomerTaxTime WHERE CustomerTaxSettingId = @SettingId";

                            conn.Execute(deleteTaxTimesSql, new { SettingId = settingId }, transaction);
                        }
                        else
                        {
                            // 新增：建立新的客戶稅金設定
                            string insertSettingSql = @"
                                INSERT INTO jetf.dbo.CustomerTaxSetting (Cust_Code)
                                VALUES (@CustCode);
                                SELECT CAST(SCOPE_IDENTITY() as int)";

                            settingId = conn.QuerySingle<int>(insertSettingSql, new { CustCode = custCode }, transaction);
                        }

                        // 新增稅金時間設定
                        if (taxTimeIds != null && taxTimeIds.Any())
                        {
                            string insertTaxTimesSql = @"
                                INSERT INTO jetf.dbo.CustomerTaxTime (CustomerTaxSettingId, TaxTimeId)
                                VALUES (@SettingId, @TaxTimeId)";

                            foreach (var taxTimeId in taxTimeIds)
                            {
                                conn.Execute(insertTaxTimesSql, new { SettingId = settingId, TaxTimeId = taxTimeId }, transaction);
                            }
                        }

                        transaction.Commit();
                        return new ResopnseModel { status = Status.success, msg = "設定儲存成功" };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally 
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResopnseModel { status = Status.error, msg = ex.Message };
            }
        }

        /// <summary>
        /// 刪除客戶稅金時間設定
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <returns></returns>
        public ResopnseModel DeleteCustomerTaxSetting(string custCode)
        {
            try
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 取得設定ID
                        string getSettingIdSql = @"
                            SELECT Id FROM jetf.dbo.CustomerTaxSetting WHERE Cust_Code = @CustCode";

                        var settingId = conn.QueryFirstOrDefault<int?>(getSettingIdSql, new { CustCode = custCode }, transaction);

                        if (settingId.HasValue)
                        {
                            // 刪除稅金時間設定
                            string deleteTaxTimesSql = @"
                                DELETE FROM jetf.dbo.CustomerTaxTime WHERE CustomerTaxSettingId = @SettingId";

                            conn.Execute(deleteTaxTimesSql, new { SettingId = settingId.Value }, transaction);

                            // 刪除客戶設定
                            string deleteSettingSql = @"
                                DELETE FROM jetf.dbo.CustomerTaxSetting WHERE Id = @SettingId";

                            conn.Execute(deleteSettingSql, new { SettingId = settingId.Value }, transaction);
                        }

                        transaction.Commit();
                        return new ResopnseModel { status = Status.success, msg = "設定刪除成功" };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally 
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResopnseModel { status = Status.error, msg = ex.Message };
            }
        }
    }
}
