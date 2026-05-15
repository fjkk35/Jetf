using Dapper;
using Service.Models;
using Service.Models.AuthorizationForm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.AuthorizationForm
{
    public class AuthorizationFormService : _BaseService
    {
        public AuthorizationFormService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得所有文件名稱
        /// </summary>
        /// <returns></returns>
        public List<AuthorizationFormModel> GetAll()
        {
            var sql = @"
                SELECT Id, FormName, Sort 
                FROM jetf.dbo.AuthorizationForm 
                ORDER BY Sort ASC, Id ASC
            ";

            return conn.Query<AuthorizationFormModel>(sql).ToList();
        }

        /// <summary>
        /// 根據ID取得文件名稱
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AuthorizationFormModel GetById(int id)
        {
            var sql = @"
                SELECT Id, FormName, Sort 
                FROM jetf.dbo.AuthorizationForm 
                WHERE Id = @Id
            ";

            return conn.QueryFirstOrDefault<AuthorizationFormModel>(sql, new { Id = id });
        }

        /// <summary>
        /// 檢查文件名稱是否重複
        /// </summary>
        /// <param name="formName">文件名稱</param>
        /// <param name="excludeId">要排除的ID（更新時使用）</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsFormNameExists(string formName, int? excludeId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.AuthorizationForm 
                WHERE FormName = @FormName";

            if (excludeId.HasValue)
            {
                checkSql += " AND Id != @ExcludeId";
            }

            var count = conn.QuerySingle<int>(checkSql, new
            {
                FormName = formName.Trim(),
                ExcludeId = excludeId
            }, transaction);

            return count > 0;
        }

        /// <summary>
        /// 新增文件名稱
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel Create(AuthorizationFormModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.FormName))
            {
                return new ResponseModel("文件名稱不能為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查文件名稱是否重複
                    if (IsFormNameExists(model.FormName, null, transaction))
                    {
                        return new ResponseModel($"文件名稱「{model.FormName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 調整其他項目的排序
                    AdjustSortOrder(model.Sort, null, transaction);

                    var sql = @"
                        INSERT INTO jetf.dbo.AuthorizationForm (FormName, Sort)
                        VALUES (@FormName, @Sort);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var id = conn.QuerySingle<int>(sql, new
                    {
                        FormName = model.FormName.Trim(),
                        Sort = model.Sort
                    }, transaction);

                    transaction.Commit();
                    return new ResponseModel { ReturnObject = id };
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
        /// 更新文件名稱
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel Update(AuthorizationFormModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.FormName))
            {
                return new ResponseModel("文件名稱不能為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查文件名稱是否重複（排除自己）
                    if (IsFormNameExists(model.FormName, model.Id, transaction))
                    {
                        return new ResponseModel($"文件名稱「{model.FormName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 調整其他項目的排序
                    AdjustSortOrder(model.Sort, model.Id, transaction);

                    var sql = @"
                        UPDATE jetf.dbo.AuthorizationForm 
                        SET FormName = @FormName, Sort = @Sort
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(sql, new
                    {
                        Id = model.Id,
                        FormName = model.FormName.Trim(),
                        Sort = model.Sort
                    }, transaction);

                    if (affected == 0)
                    {
                        transaction.Rollback();
                        return new ResponseModel("找不到指定的文件名稱");
                    }

                    transaction.Commit();
                    return new ResponseModel();
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
        /// 調整排序順序，避免重複
        /// </summary>
        /// <param name="newSort">新的排序號碼</param>
        /// <param name="excludeId">要排除的ID（更新時使用）</param>
        /// <param name="transaction">交易</param>
        private void AdjustSortOrder(int newSort, int? excludeId, System.Data.SqlClient.SqlTransaction transaction)
        {
            // 將排序號碼大於等於newSort的項目都加1
            var adjustSql = @"
                UPDATE jetf.dbo.AuthorizationForm 
                SET Sort = Sort + 1 
                WHERE Sort >= @NewSort";

            if (excludeId.HasValue)
            {
                adjustSql += " AND Id != @ExcludeId";
            }

            conn.Execute(adjustSql, new
            {
                NewSort = newSort,
                ExcludeId = excludeId
            }, transaction);
        }

        /// <summary>
        /// 批量更新排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        public ResponseModel UpdateSorts(List<AuthorizationFormModel> sortUpdates)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        UPDATE jetf.dbo.AuthorizationForm 
                        SET Sort = @Sort
                        WHERE Id = @Id
                    ";

                    foreach (var item in sortUpdates)
                    {
                        conn.Execute(sql, new
                        {
                            Id = item.Id,
                            Sort = item.Sort
                        }, transaction);
                    }

                    transaction.Commit();
                    return new ResponseModel();
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
    }
}