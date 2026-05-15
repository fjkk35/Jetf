using Dapper;
using Service.Models;
using Service.Models.ApprovalCategory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.ApprovalCategory
{
    public class ApprovalCategoryService : _BaseService
    {
        public ApprovalCategoryService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得所有簽審類別
        /// </summary>
        /// <returns></returns>
        public List<ApprovalCategoryModel> GetAll()
        {
            var sql = @"
                SELECT Id, CategoryName, Sort 
                FROM jetf.dbo.ApprovalCategory 
                ORDER BY Sort ASC, Id ASC
            ";

            return conn.Query<ApprovalCategoryModel>(sql).ToList();
        }

        /// <summary>
        /// 根據ID取得簽審類別
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ApprovalCategoryModel GetById(int id)
        {
            var sql = @"
                SELECT Id, CategoryName, Sort 
                FROM jetf.dbo.ApprovalCategory 
                WHERE Id = @Id
            ";

            return conn.QueryFirstOrDefault<ApprovalCategoryModel>(sql, new { Id = id });
        }

        /// <summary>
        /// 檢查簽審類別名稱是否重複
        /// </summary>
        /// <param name="categoryName">簽審類別名稱</param>
        /// <param name="excludeId">要排除的ID（更新時使用）</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsCategoryNameExists(string categoryName, int? excludeId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.ApprovalCategory 
                WHERE CategoryName = @CategoryName";

            if (excludeId.HasValue)
            {
                checkSql += " AND Id != @ExcludeId";
            }

            var count = conn.QuerySingle<int>(checkSql, new
            {
                CategoryName = categoryName.Trim(),
                ExcludeId = excludeId
            }, transaction);

            return count > 0;
        }

        /// <summary>
        /// 新增簽審類別
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel Create(ApprovalCategoryModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return new ResponseModel("簽審類別名稱不能為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查簽審類別名稱是否重複
                    if (IsCategoryNameExists(model.CategoryName, null, transaction))
                    {
                        return new ResponseModel($"簽審類別名稱「{model.CategoryName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 調整其他項目的排序
                    AdjustSortOrder(model.Sort, null, transaction);

                    var sql = @"
                        INSERT INTO jetf.dbo.ApprovalCategory (CategoryName, Sort)
                        VALUES (@CategoryName, @Sort);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var id = conn.QuerySingle<int>(sql, new
                    {
                        CategoryName = model.CategoryName.Trim(),
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
        /// 更新簽審類別
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel Update(ApprovalCategoryModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return new ResponseModel("簽審類別名稱不能為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查簽審類別名稱是否重複（排除自己）
                    if (IsCategoryNameExists(model.CategoryName, model.Id, transaction))
                    {
                        return new ResponseModel($"簽審類別名稱「{model.CategoryName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 調整其他項目的排序
                    AdjustSortOrder(model.Sort, model.Id, transaction);

                    var sql = @"
                        UPDATE jetf.dbo.ApprovalCategory 
                        SET CategoryName = @CategoryName, Sort = @Sort
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(sql, new
                    {
                        Id = model.Id,
                        CategoryName = model.CategoryName.Trim(),
                        Sort = model.Sort
                    }, transaction);

                    if (affected == 0)
                    {
                        transaction.Rollback();
                        return new ResponseModel("找不到指定的簽審類別");
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
                UPDATE jetf.dbo.ApprovalCategory 
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
        /// 刪除簽審類別
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ResponseModel Delete(int id)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查是否有被使用
                    var checkSql = @"
                        SELECT COUNT(*) 
                        FROM jetf.dbo.SeaClearanceDetailApprovalCategory 
                        WHERE ApprovalCategoryId = @Id
                    ";

                    var usageCount = conn.QuerySingle<int>(checkSql, new { Id = id }, transaction);

                    if (usageCount > 0)
                    {
                        return new ResponseModel("此簽審類別正在被使用中，無法刪除");
                    }

                    // 取得要刪除項目的排序
                    var getSortSql = @"
                        SELECT Sort FROM jetf.dbo.ApprovalCategory 
                        WHERE Id = @Id
                    ";
                    var sortToDelete = conn.QueryFirstOrDefault<int?>(getSortSql, new { Id = id }, transaction);

                    // 刪除項目
                    var deleteSql = @"
                        DELETE FROM jetf.dbo.ApprovalCategory 
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(deleteSql, new { Id = id }, transaction);

                    if (affected == 0)
                    {
                        return new ResponseModel("找不到指定的簽審類別");
                    }

                    // 調整其他項目的排序（將大於被刪除項目排序的都減1）
                    if (sortToDelete.HasValue)
                    {
                        var adjustSql = @"
                            UPDATE jetf.dbo.ApprovalCategory 
                            SET Sort = Sort - 1 
                            WHERE Sort > @DeletedSort
                        ";
                        conn.Execute(adjustSql, new { DeletedSort = sortToDelete.Value }, transaction);
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
        /// 批量更新排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        public ResponseModel UpdateSorts(List<ApprovalCategoryModel> sortUpdates)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        UPDATE jetf.dbo.ApprovalCategory 
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