using Dapper;
using Service.Models;
using Service.Models.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Step
{
    public class StepService : _BaseService
    {
        public StepService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得所有步驟（包含步驟詳細）
        /// </summary>
        /// <returns></returns>
        public List<StepModel> GetAllStepsWithDetails()
        {
            var sql = @"
                SELECT s.Id, s.StepName, s.IsMultiple, s.Sort,
                       sd.Id as DetailId, sd.StepId, sd.StepDetailName, sd.Sort as DetailSort
                FROM jetf.dbo.Step s
                LEFT JOIN jetf.dbo.StepDetail sd ON s.Id = sd.StepId
                ORDER BY s.Sort, s.Id, sd.Sort, sd.Id
            ";

            var stepDictionary = new Dictionary<int, StepModel>();

            conn.Query<StepModel, dynamic, StepModel>(sql,
                (step, stepDetailData) =>
                {
                    if (!stepDictionary.TryGetValue(step.Id, out StepModel stepEntry))
                    {
                        stepEntry = step;
                        stepEntry.StepDetails = new List<StepDetailModel>();
                        stepDictionary.Add(step.Id, stepEntry);
                    }

                    if (stepDetailData != null && stepDetailData.DetailId > 0)
                    {
                        var stepDetail = new StepDetailModel
                        {
                            Id = stepDetailData.DetailId,
                            StepId = stepDetailData.StepId,
                            StepDetailName = stepDetailData.StepDetailName,
                            Sort = stepDetailData.DetailSort
                        };
                        stepEntry.StepDetails.Add(stepDetail);
                    }

                    return stepEntry;
                },
                splitOn: "DetailId"
            );

            return stepDictionary.Values.ToList();
        }

        /// <summary>
        /// 取得所有步驟（不包含詳細）
        /// </summary>
        /// <returns></returns>
        public List<StepModel> GetAllSteps()
        {
            var sql = @"
                SELECT Id, StepName,IsMultiple, Sort 
                FROM jetf.dbo.Step 
                ORDER BY Sort, Id
            ";

            return conn.Query<StepModel>(sql).ToList();
        }

        /// <summary>
        /// 根據ID取得步驟
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public StepModel GetById(int id)
        {
            var sql = @"
                SELECT Id, StepName, IsMultiple, Sort 
                FROM jetf.dbo.Step 
                WHERE Id = @Id
            ";

            return conn.QueryFirstOrDefault<StepModel>(sql, new { Id = id });
        }

        /// <summary>
        /// 新增步驟
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel CreateStep(StepModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.StepName))
            {
                return new ResponseModel("步驟名稱不能為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查步驟名稱是否重複
                    if (IsStepNameExists(model.StepName, null, transaction))
                    {
                        return new ResponseModel($"步驟名稱「{model.StepName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 取得下一個排序號碼
                    var nextSort = GetNextStepSort(transaction);

                    var sql = @"
                        INSERT INTO jetf.dbo.Step (StepName, IsMultiple, Sort)
                        VALUES (@StepName, @IsMultiple, @Sort);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var id = conn.QuerySingle<int>(sql, new
                    {
                        StepName = model.StepName.Trim(),
                        IsMultiple = model.IsMultiple,
                        Sort = nextSort
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
        /// 更新步驟
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel UpdateStep(StepModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.StepName))
            {
                return new ResponseModel("步驟名稱不能為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查步驟名稱是否重複（排除自己）
                    if (IsStepNameExists(model.StepName, model.Id, transaction))
                    {
                        return new ResponseModel($"步驟名稱「{model.StepName.Trim()}」已存在，請使用其他名稱");
                    }

                    var sql = @"
                        UPDATE jetf.dbo.Step 
                        SET StepName = @StepName,
                            IsMultiple = @IsMultiple
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(sql, new
                    {
                        Id = model.Id,
                        StepName = model.StepName.Trim(),
                        IsMultiple = model.IsMultiple
                    }, transaction);

                    if (affected == 0)
                    {
                        transaction.Rollback();
                        return new ResponseModel("找不到指定的步驟");
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
        /// 刪除步驟
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ResponseModel DeleteStep(int id)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查是否有步驟詳細
                    var checkSql = @"
                        SELECT COUNT(*) 
                        FROM jetf.dbo.StepDetail 
                        WHERE StepId = @StepId
                    ";

                    var detailCount = conn.QuerySingle<int>(checkSql, new { StepId = id }, transaction);

                    if (detailCount > 0)
                    {
                        return new ResponseModel("此步驟下還有步驟詳細資料，請先刪除所有步驟詳細後再刪除步驟");
                    }

                    // 取得要刪除項目的排序
                    var getSortSql = @"
                        SELECT Sort FROM jetf.dbo.Step 
                        WHERE Id = @Id
                    ";
                    var sortToDelete = conn.QueryFirstOrDefault<int?>(getSortSql, new { Id = id }, transaction);

                    // 刪除步驟
                    var deleteSql = @"
                        DELETE FROM jetf.dbo.Step 
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(deleteSql, new { Id = id }, transaction);

                    if (affected == 0)
                    {
                        return new ResponseModel("找不到指定的步驟");
                    }

                    // 調整其他項目的排序（將大於被刪除項目排序的都減1）
                    if (sortToDelete.HasValue)
                    {
                        var adjustSql = @"
                            UPDATE jetf.dbo.Step 
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
        /// 批量更新步驟排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        public ResponseModel UpdateStepSorts(List<StepSortUpdateModel> sortUpdates)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        UPDATE jetf.dbo.Step 
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

        /// <summary>
        /// 取得步驟的所有詳細
        /// </summary>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<StepDetailModel> GetStepDetails(int stepId)
        {
            var sql = @"
                SELECT Id, StepId, StepDetailName, Sort 
                FROM jetf.dbo.StepDetail 
                WHERE StepId = @StepId
                ORDER BY Sort, Id
            ";

            return conn.Query<StepDetailModel>(sql, new { StepId = stepId }).ToList();
        }

        /// <summary>
        /// 新增步驟詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel CreateStepDetail(StepDetailModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.StepDetailName))
            {
                return new ResponseModel("步驟詳細名稱不能為空");
            }

            if (model.StepId <= 0)
            {
                return new ResponseModel("請選擇步驟");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查步驟是否存在
                    if (!IsStepExists(model.StepId, transaction))
                    {
                        return new ResponseModel("指定的步驟不存在");
                    }

                    // 檢查步驟詳細名稱在同一步驟下是否重複
                    if (IsStepDetailNameExists(model.StepId, model.StepDetailName, null, transaction))
                    {
                        return new ResponseModel($"在此步驟下，步驟詳細名稱「{model.StepDetailName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 取得下一個排序號碼
                    var nextSort = GetNextStepDetailSort(model.StepId, transaction);

                    var sql = @"
                        INSERT INTO jetf.dbo.StepDetail (StepId, StepDetailName, Sort)
                        VALUES (@StepId, @StepDetailName, @Sort);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var id = conn.QuerySingle<int>(sql, new
                    {
                        StepId = model.StepId,
                        StepDetailName = model.StepDetailName.Trim(),
                        Sort = nextSort
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
        /// 更新步驟詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel UpdateStepDetail(StepDetailModel model)
        {
            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.StepDetailName))
            {
                return new ResponseModel("步驟詳細名稱不能為空");
            }

            if (model.StepId <= 0)
            {
                return new ResponseModel("請選擇步驟");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查步驟詳細名稱在同一步驟下是否重複（排除自己）
                    if (IsStepDetailNameExists(model.StepId, model.StepDetailName, model.Id, transaction))
                    {
                        return new ResponseModel($"在此步驟下，步驟詳細名稱「{model.StepDetailName.Trim()}」已存在，請使用其他名稱");
                    }

                    var sql = @"
                        UPDATE jetf.dbo.StepDetail 
                        SET StepId = @StepId, StepDetailName = @StepDetailName
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(sql, new
                    {
                        Id = model.Id,
                        StepId = model.StepId,
                        StepDetailName = model.StepDetailName.Trim()
                    }, transaction);

                    if (affected == 0)
                    {
                        transaction.Rollback();
                        return new ResponseModel("找不到指定的步驟詳細");
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
        /// 刪除步驟詳細
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ResponseModel DeleteStepDetail(int id)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 取得要刪除項目的排序和步驟ID
                    var getInfoSql = @"
                        SELECT StepId, Sort FROM jetf.dbo.StepDetail 
                        WHERE Id = @Id
                    ";
                    var deleteInfo = conn.QueryFirstOrDefault<dynamic>(getInfoSql, new { Id = id }, transaction);

                    if (deleteInfo == null)
                    {
                        return new ResponseModel("找不到指定的步驟詳細");
                    }

                    // 刪除步驟詳細
                    var deleteSql = @"
                        DELETE FROM jetf.dbo.StepDetail 
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(deleteSql, new { Id = id }, transaction);

                    if (affected == 0)
                    {
                        return new ResponseModel("找不到指定的步驟詳細");
                    }

                    // 調整同一步驟下其他項目的排序
                    var adjustSql = @"
                        UPDATE jetf.dbo.StepDetail 
                        SET Sort = Sort - 1 
                        WHERE StepId = @StepId AND Sort > @DeletedSort
                    ";
                    conn.Execute(adjustSql, new { 
                        StepId = deleteInfo.StepId, 
                        DeletedSort = deleteInfo.Sort 
                    }, transaction);

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
        /// 批量更新步驟詳細排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        public ResponseModel UpdateStepDetailSorts(List<StepDetailSortUpdateModel> sortUpdates)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        UPDATE jetf.dbo.StepDetail 
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

        /// <summary>
        /// 取得下一個步驟排序號碼
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private int GetNextStepSort(System.Data.SqlClient.SqlTransaction transaction)
        {
            var sql = @"
                SELECT ISNULL(MAX(Sort), 0) + 1 
                FROM jetf.dbo.Step
            ";

            return conn.QuerySingle<int>(sql, transaction: transaction);
        }

        /// <summary>
        /// 取得下一個步驟詳細排序號碼
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private int GetNextStepDetailSort(int stepId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var sql = @"
                SELECT ISNULL(MAX(Sort), 0) + 1 
                FROM jetf.dbo.StepDetail
                WHERE StepId = @StepId
            ";

            return conn.QuerySingle<int>(sql, new { StepId = stepId }, transaction);
        }

        /// <summary>
        /// 檢查步驟名稱是否重複
        /// </summary>
        /// <param name="stepName">步驟名稱</param>
        /// <param name="excludeId">要排除的ID（更新時使用）</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsStepNameExists(string stepName, int? excludeId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.Step 
                WHERE StepName = @StepName";

            if (excludeId.HasValue)
            {
                checkSql += " AND Id != @ExcludeId";
            }

            var count = conn.QuerySingle<int>(checkSql, new
            {
                StepName = stepName.Trim(),
                ExcludeId = excludeId
            }, transaction);

            return count > 0;
        }

        /// <summary>
        /// 檢查步驟詳細名稱在同一步驟下是否重複
        /// </summary>
        /// <param name="stepId">步驟ID</param>
        /// <param name="stepDetailName">步驟詳細名稱</param>
        /// <param name="excludeId">要排除的ID（更新時使用）</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsStepDetailNameExists(int stepId, string stepDetailName, int? excludeId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.StepDetail 
                WHERE StepId = @StepId AND StepDetailName = @StepDetailName";

            if (excludeId.HasValue)
            {
                checkSql += " AND Id != @ExcludeId";
            }

            var count = conn.QuerySingle<int>(checkSql, new
            {
                StepId = stepId,
                StepDetailName = stepDetailName.Trim(),
                ExcludeId = excludeId
            }, transaction);

            return count > 0;
        }

        /// <summary>
        /// 檢查步驟是否存在
        /// </summary>
        /// <param name="stepId">步驟ID</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsStepExists(int stepId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.Step 
                WHERE Id = @StepId
            ";

            var count = conn.QuerySingle<int>(checkSql, new { StepId = stepId }, transaction);
            return count > 0;
        }
    }
}