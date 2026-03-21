using Dapper;
using Service.Models;
using Service.Models.AbnormalState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.AbnormalState
{
    public class AbnormalStateService : _BaseService
    {
        /// <summary>
        /// 取得所有異常狀態（包含異常狀態詳細）
        /// </summary>
        /// <returns></returns>
        public List<AbnormalStateModel> GetAllAbnormalStatesWithDetails()
        {
            var sql = @"
                SELECT a.Id, a.AbnormalStateName, a.Sort,
                       ad.Id as DetailId, ad.AbnormalStateId, ad.AbnormalStateDetailName, ad.Sort as DetailSort
                FROM jetf.dbo.AbnormalState a
                LEFT JOIN jetf.dbo.AbnormalStateDetail ad ON a.Id = ad.AbnormalStateId
                ORDER BY a.Sort, a.Id, ad.Sort, ad.Id
            ";

            var abnormalStateDictionary = new Dictionary<int, AbnormalStateModel>();

            conn.Query<AbnormalStateModel, dynamic, AbnormalStateModel>(sql,
                (abnormalState, abnormalStateDetailData) =>
                {
                    if (!abnormalStateDictionary.TryGetValue(abnormalState.Id, out AbnormalStateModel abnormalStateEntry))
                    {
                        abnormalStateEntry = abnormalState;
                        abnormalStateEntry.AbnormalStateDetails = new List<AbnormalStateDetailModel>();
                        abnormalStateDictionary.Add(abnormalState.Id, abnormalStateEntry);
                    }

                    if (abnormalStateDetailData != null && abnormalStateDetailData.DetailId > 0)
                    {
                        var abnormalStateDetail = new AbnormalStateDetailModel
                        {
                            Id = abnormalStateDetailData.DetailId,
                            AbnormalStateId = abnormalStateDetailData.AbnormalStateId,
                            AbnormalStateDetailName = abnormalStateDetailData.AbnormalStateDetailName,
                            Sort = abnormalStateDetailData.DetailSort
                        };
                        abnormalStateEntry.AbnormalStateDetails.Add(abnormalStateDetail);
                    }

                    return abnormalStateEntry;
                },
                splitOn: "DetailId"
            );

            return abnormalStateDictionary.Values.ToList();
        }

        /// <summary>
        /// 取得所有異常狀態（不包含詳細）
        /// </summary>
        /// <returns></returns>
        public List<AbnormalStateModel> GetAllAbnormalStates()
        {
            var sql = @"
                SELECT Id, AbnormalStateName, Sort 
                FROM jetf.dbo.AbnormalState 
                ORDER BY Sort, Id
            ";

            return conn.Query<AbnormalStateModel>(sql).ToList();
        }

        /// <summary>
        /// 根據ID取得異常狀態
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AbnormalStateModel GetById(int id)
        {
            var sql = @"
                SELECT Id, AbnormalStateName, Sort 
                FROM jetf.dbo.AbnormalState 
                WHERE Id = @Id
            ";

            return conn.QueryFirstOrDefault<AbnormalStateModel>(sql, new { Id = id });
        }

        /// <summary>
        /// 新增異常狀態
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResopnseModel CreateAbnormalState(AbnormalStateModel model)
        {
            // 驗證
            if (string.IsNullOrWhiteSpace(model.AbnormalStateName))
            {
                return new ResopnseModel("異常狀態名稱不可為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查異常狀態名稱是否重複
                    if (IsAbnormalStateNameExists(model.AbnormalStateName, null, transaction))
                    {
                        return new ResopnseModel($"異常狀態名稱「{model.AbnormalStateName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 取得下一個排序號碼
                    var nextSort = GetNextAbnormalStateSort(transaction);

                    var sql = @"
                        INSERT INTO jetf.dbo.AbnormalState (AbnormalStateName, Sort)
                        VALUES (@AbnormalStateName, @Sort);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var id = conn.QuerySingle<int>(sql, new
                    {
                        AbnormalStateName = model.AbnormalStateName.Trim(),
                        Sort = nextSort
                    }, transaction);

                    transaction.Commit();
                    return new ResopnseModel { ReturnObject = id };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 更新異常狀態
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResopnseModel UpdateAbnormalState(AbnormalStateModel model)
        {
            // 驗證
            if (string.IsNullOrWhiteSpace(model.AbnormalStateName))
            {
                return new ResopnseModel("異常狀態名稱不可為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查異常狀態名稱是否重複（排除自己）
                    if (IsAbnormalStateNameExists(model.AbnormalStateName, model.Id, transaction))
                    {
                        return new ResopnseModel($"異常狀態名稱「{model.AbnormalStateName.Trim()}」已存在，請使用其他名稱");
                    }

                    var sql = @"
                        UPDATE jetf.dbo.AbnormalState 
                        SET AbnormalStateName = @AbnormalStateName
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(sql, new
                    {
                        Id = model.Id,
                        AbnormalStateName = model.AbnormalStateName.Trim()
                    }, transaction);

                    if (affected == 0)
                    {
                        transaction.Rollback();
                        return new ResopnseModel("找不到指定的異常狀態");
                    }

                    transaction.Commit();
                    return new ResopnseModel();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 刪除異常狀態
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ResopnseModel DeleteAbnormalState(int id)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查是否有異常狀態詳細
                    var checkSql = @"
                        SELECT COUNT(*) 
                        FROM jetf.dbo.AbnormalStateDetail 
                        WHERE AbnormalStateId = @AbnormalStateId
                    ";

                    var detailCount = conn.QuerySingle<int>(checkSql, new { AbnormalStateId = id }, transaction);

                    if (detailCount > 0)
                    {
                        return new ResopnseModel("此異常狀態下還有異常狀態詳細資料，請先刪除所有異常狀態詳細後，再刪除異常狀態");
                    }

                    // 取得要刪除的資料的排序
                    var getSortSql = @"
                        SELECT Sort FROM jetf.dbo.AbnormalState 
                        WHERE Id = @Id
                    ";
                    var sortToDelete = conn.QueryFirstOrDefault<int?>(getSortSql, new { Id = id }, transaction);

                    // 刪除異常狀態
                    var deleteSql = @"
                        DELETE FROM jetf.dbo.AbnormalState 
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(deleteSql, new { Id = id }, transaction);

                    if (affected == 0)
                    {
                        return new ResopnseModel("找不到指定的異常狀態");
                    }

                    // 調整其他的排序（將大於被刪除的排序的減1）
                    if (sortToDelete.HasValue)
                    {
                        var adjustSql = @"
                            UPDATE jetf.dbo.AbnormalState 
                            SET Sort = Sort - 1 
                            WHERE Sort > @DeletedSort
                        ";
                        conn.Execute(adjustSql, new { DeletedSort = sortToDelete.Value }, transaction);
                    }

                    transaction.Commit();
                    return new ResopnseModel();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 批量更新異常狀態排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        public ResopnseModel UpdateAbnormalStateSorts(List<AbnormalStateSortUpdateModel> sortUpdates)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        UPDATE jetf.dbo.AbnormalState 
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
                    return new ResopnseModel();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 取得異常狀態的所有詳細
        /// </summary>
        /// <param name="abnormalStateId"></param>
        /// <returns></returns>
        public List<AbnormalStateDetailModel> GetAbnormalStateDetails(int abnormalStateId)
        {
            var sql = @"
                SELECT Id, AbnormalStateId, AbnormalStateDetailName, Sort 
                FROM jetf.dbo.AbnormalStateDetail 
                WHERE AbnormalStateId = @AbnormalStateId
                ORDER BY Sort, Id
            ";

            return conn.Query<AbnormalStateDetailModel>(sql, new { AbnormalStateId = abnormalStateId }).ToList();
        }

        /// <summary>
        /// 新增異常狀態詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResopnseModel CreateAbnormalStateDetail(AbnormalStateDetailModel model)
        {
            // 驗證
            if (string.IsNullOrWhiteSpace(model.AbnormalStateDetailName))
            {
                return new ResopnseModel("異常狀態詳細名稱不可為空");
            }

            if (model.AbnormalStateId <= 0)
            {
                return new ResopnseModel("請選擇異常狀態");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查異常狀態是否存在
                    if (!IsAbnormalStateExists(model.AbnormalStateId, transaction))
                    {
                        return new ResopnseModel("指定的異常狀態不存在");
                    }

                    // 檢查異常狀態詳細名稱在同一異常狀態下是否重複
                    if (IsAbnormalStateDetailNameExists(model.AbnormalStateId, model.AbnormalStateDetailName, null, transaction))
                    {
                        return new ResopnseModel($"在此異常狀態下，異常狀態詳細名稱「{model.AbnormalStateDetailName.Trim()}」已存在，請使用其他名稱");
                    }

                    // 取得下一個排序號碼
                    var nextSort = GetNextAbnormalStateDetailSort(model.AbnormalStateId, transaction);

                    var sql = @"
                        INSERT INTO jetf.dbo.AbnormalStateDetail (AbnormalStateId, AbnormalStateDetailName, Sort)
                        VALUES (@AbnormalStateId, @AbnormalStateDetailName, @Sort);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var id = conn.QuerySingle<int>(sql, new
                    {
                        AbnormalStateId = model.AbnormalStateId,
                        AbnormalStateDetailName = model.AbnormalStateDetailName.Trim(),
                        Sort = nextSort
                    }, transaction);

                    transaction.Commit();
                    return new ResopnseModel { ReturnObject = id };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 更新異常狀態詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResopnseModel UpdateAbnormalStateDetail(AbnormalStateDetailModel model)
        {
            // 驗證
            if (string.IsNullOrWhiteSpace(model.AbnormalStateDetailName))
            {
                return new ResopnseModel("異常狀態詳細名稱不可為空");
            }

            if (model.AbnormalStateId <= 0)
            {
                return new ResopnseModel("請選擇異常狀態");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 檢查異常狀態詳細名稱在同一異常狀態下是否重複（排除自己）
                    if (IsAbnormalStateDetailNameExists(model.AbnormalStateId, model.AbnormalStateDetailName, model.Id, transaction))
                    {
                        return new ResopnseModel($"在此異常狀態下，異常狀態詳細名稱「{model.AbnormalStateDetailName.Trim()}」已存在，請使用其他名稱");
                    }

                    var sql = @"
                        UPDATE jetf.dbo.AbnormalStateDetail 
                        SET AbnormalStateId = @AbnormalStateId, AbnormalStateDetailName = @AbnormalStateDetailName
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(sql, new
                    {
                        Id = model.Id,
                        AbnormalStateId = model.AbnormalStateId,
                        AbnormalStateDetailName = model.AbnormalStateDetailName.Trim()
                    }, transaction);

                    if (affected == 0)
                    {
                        transaction.Rollback();
                        return new ResopnseModel("找不到指定的異常狀態詳細");
                    }

                    transaction.Commit();
                    return new ResopnseModel();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 刪除異常狀態詳細
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ResopnseModel DeleteAbnormalStateDetail(int id)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 取得要刪除的資料的排序和異常狀態ID
                    var getInfoSql = @"
                        SELECT AbnormalStateId, Sort FROM jetf.dbo.AbnormalStateDetail 
                        WHERE Id = @Id
                    ";
                    var deleteInfo = conn.QueryFirstOrDefault<dynamic>(getInfoSql, new { Id = id }, transaction);

                    if (deleteInfo == null)
                    {
                        return new ResopnseModel("找不到指定的異常狀態詳細");
                    }

                    // 刪除異常狀態詳細
                    var deleteSql = @"
                        DELETE FROM jetf.dbo.AbnormalStateDetail 
                        WHERE Id = @Id
                    ";

                    var affected = conn.Execute(deleteSql, new { Id = id }, transaction);

                    if (affected == 0)
                    {
                        return new ResopnseModel("找不到指定的異常狀態詳細");
                    }

                    // 調整同一異常狀態下其他的排序
                    var adjustSql = @"
                        UPDATE jetf.dbo.AbnormalStateDetail 
                        SET Sort = Sort - 1 
                        WHERE AbnormalStateId = @AbnormalStateId AND Sort > @DeletedSort
                    ";
                    conn.Execute(adjustSql, new { 
                        AbnormalStateId = deleteInfo.AbnormalStateId, 
                        DeletedSort = deleteInfo.Sort 
                    }, transaction);

                    transaction.Commit();
                    return new ResopnseModel();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 批量更新異常狀態詳細排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        public ResopnseModel UpdateAbnormalStateDetailSorts(List<AbnormalStateDetailSortUpdateModel> sortUpdates)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        UPDATE jetf.dbo.AbnormalStateDetail 
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
                    return new ResopnseModel();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 取得下一個異常狀態排序號碼
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private int GetNextAbnormalStateSort(System.Data.SqlClient.SqlTransaction transaction)
        {
            var sql = @"
                SELECT ISNULL(MAX(Sort), 0) + 1 
                FROM jetf.dbo.AbnormalState
            ";

            return conn.QuerySingle<int>(sql, transaction: transaction);
        }

        /// <summary>
        /// 取得下一個異常狀態詳細排序號碼
        /// </summary>
        /// <param name="abnormalStateId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private int GetNextAbnormalStateDetailSort(int abnormalStateId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var sql = @"
                SELECT ISNULL(MAX(Sort), 0) + 1 
                FROM jetf.dbo.AbnormalStateDetail
                WHERE AbnormalStateId = @AbnormalStateId
            ";

            return conn.QuerySingle<int>(sql, new { AbnormalStateId = abnormalStateId }, transaction);
        }

        /// <summary>
        /// 檢查異常狀態名稱是否重複
        /// </summary>
        /// <param name="abnormalStateName">異常狀態名稱</param>
        /// <param name="excludeId">要排除的ID（更新時使用）</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsAbnormalStateNameExists(string abnormalStateName, int? excludeId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.AbnormalState 
                WHERE AbnormalStateName = @AbnormalStateName";

            if (excludeId.HasValue)
            {
                checkSql += " AND Id != @ExcludeId";
            }

            var count = conn.QuerySingle<int>(checkSql, new
            {
                AbnormalStateName = abnormalStateName.Trim(),
                ExcludeId = excludeId
            }, transaction);

            return count > 0;
        }

        /// <summary>
        /// 檢查異常狀態詳細名稱在同一異常狀態下是否重複
        /// </summary>
        /// <param name="abnormalStateId">異常狀態ID</param>
        /// <param name="abnormalStateDetailName">異常狀態詳細名稱</param>
        /// <param name="excludeId">要排除的ID（更新時使用）</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsAbnormalStateDetailNameExists(int abnormalStateId, string abnormalStateDetailName, int? excludeId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.AbnormalStateDetail 
                WHERE AbnormalStateId = @AbnormalStateId AND AbnormalStateDetailName = @AbnormalStateDetailName";

            if (excludeId.HasValue)
            {
                checkSql += " AND Id != @ExcludeId";
            }

            var count = conn.QuerySingle<int>(checkSql, new
            {
                AbnormalStateId = abnormalStateId,
                AbnormalStateDetailName = abnormalStateDetailName.Trim(),
                ExcludeId = excludeId
            }, transaction);

            return count > 0;
        }

        /// <summary>
        /// 檢查異常狀態是否存在
        /// </summary>
        /// <param name="abnormalStateId">異常狀態ID</param>
        /// <param name="transaction">交易</param>
        /// <returns></returns>
        private bool IsAbnormalStateExists(int abnormalStateId, System.Data.SqlClient.SqlTransaction transaction)
        {
            var checkSql = @"
                SELECT COUNT(*) 
                FROM jetf.dbo.AbnormalState 
                WHERE Id = @AbnormalStateId
            ";

            var count = conn.QuerySingle<int>(checkSql, new { AbnormalStateId = abnormalStateId }, transaction);
            return count > 0;
        }
    }
}
