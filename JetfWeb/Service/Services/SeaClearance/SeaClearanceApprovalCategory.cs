using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance
{
    public partial class SeaClearanceService
    {
        /// <summary>
        /// 取得指定明細的簽審類別
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public List<int> GetDetailApprovalCategories(int seaClearanceDetailId)
        {
            var sql = @"
                SELECT ApprovalCategoryId 
                FROM jetf.dbo.SeaClearanceDetailApprovalCategory 
                WHERE SeaClearanceDetailId = @SeaClearanceDetailId
            ";

            return conn.Query<int>(sql, new { SeaClearanceDetailId = seaClearanceDetailId }).ToList();
        }

        /// <summary>
        /// 取得指定明細的簽審類別名稱
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public Dictionary<int, string> GetDetailApprovalCategories(List<int> seaClearanceDetailIds)
        {
            var sql = @"
                select a.SeaClearanceDetailId,b.CategoryName,b.Sort from jetf.dbo.SeaClearanceDetailApprovalCategory a
                join jetf.dbo.ApprovalCategory b on a.ApprovalCategoryId=b.Id
                WHERE a.SeaClearanceDetailId in @SeaClearanceDetailIds
            ";

            // 查詢結果：每筆包含 DetailId、CategoryName、Sort
            var result = conn.Query<(int SeaClearanceDetailId, string CategoryName, int Sort)>(
                sql,
                new { SeaClearanceDetailIds = seaClearanceDetailIds }
            ).ToList();


            var dic = result
                .GroupBy(r => r.SeaClearanceDetailId)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.OrderBy(x => x.Sort).Select(x => x.CategoryName)));

            return dic;
        }

        /// <summary>
        /// 更新明細的簽審類別
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="categoryIds"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel UpdateDetailApprovalCategories(int seaClearanceDetailId, List<int> categoryIds, string userId)
        {
            // 確保連線開啟
            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }

            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 先取得目前的簽審類別
                    var currentCategories = conn.Query<int>(@"
                            SELECT ApprovalCategoryId 
                            FROM jetf.dbo.SeaClearanceDetailApprovalCategory 
                            WHERE SeaClearanceDetailId = @SeaClearanceDetailId
                        ", new { SeaClearanceDetailId = seaClearanceDetailId }, transaction).ToList();

                    var currentCategoryNames = GetCategoryNamesByIds(currentCategories, transaction);

                    // 刪除現有的簽審類別關聯
                    var deleteSql = @"
                            DELETE FROM jetf.dbo.SeaClearanceDetailApprovalCategory 
                            WHERE SeaClearanceDetailId = @SeaClearanceDetailId
                        ";
                    conn.Execute(deleteSql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction);

                    // 新增新的簽審類別關聯
                    if (categoryIds != null && categoryIds.Any())
                    {
                        var insertSql = @"
                                INSERT INTO jetf.dbo.SeaClearanceDetailApprovalCategory 
                                (SeaClearanceDetailId, ApprovalCategoryId)
                                VALUES (@SeaClearanceDetailId, @ApprovalCategoryId)
                            ";

                        foreach (var categoryId in categoryIds)
                        {
                            conn.Execute(insertSql, new
                            {
                                SeaClearanceDetailId = seaClearanceDetailId,
                                ApprovalCategoryId = categoryId
                            }, transaction);
                        }
                    }

                    // 記錄編輯歷史
                    var newCategoryNames = GetCategoryNamesByIds(categoryIds, transaction);
                    var oldValue = string.Join(", ", currentCategoryNames);
                    var newValue = string.Join(", ", newCategoryNames);

                    // 記錄編輯歷史
                    var editHistorySql = @"
                            INSERT INTO jetf.dbo.SeaClearanceDetailEditHistory 
                            (SeaClearanceDetailId, FieldName, OldValue, NewValue, EditTime, EditUser)
                            VALUES 
                            (@SeaClearanceDetailId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)
                        ";

                    if (oldValue != newValue)
                    {
                        conn.Execute(editHistorySql, new
                        {
                            SeaClearanceDetailId = seaClearanceDetailId,
                            FieldName = "簽審類別",
                            OldValue = oldValue,
                            NewValue = newValue,
                            EditTime = DateTime.Now,
                            EditUser = userId
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
        /// 根據ID取得類別名稱 (支援交易)
        /// </summary>
        /// <param name="categoryIds"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private List<string> GetCategoryNamesByIds(List<int> categoryIds, System.Data.SqlClient.SqlTransaction transaction = null)
        {
            if (categoryIds == null || !categoryIds.Any())
                return new List<string>();

            var sql = @"
                SELECT CategoryName 
                FROM jetf.dbo.ApprovalCategory 
                WHERE Id IN @CategoryIds
                ORDER BY CategoryName
            ";

            return conn.Query<string>(sql, new { CategoryIds = categoryIds }, transaction).ToList();
        }

        /// <summary>
        /// 取得指定明細和類型的授權表單
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="type">1=收到正本選單、2=寄文件選單</param>
        /// <returns></returns>
        public List<int> GetDetailAuthorizationForms(int seaClearanceDetailId, int type)
        {
            var sql = @"
                SELECT d.AuthorizationFormId 
                FROM jetf.dbo.SeaClearanceAuthorizationForm af
                INNER JOIN jetf.dbo.SeaClearanceAuthorizationFormDetail d ON af.Id = d.SeaClearanceAuthorizationFormId
                WHERE af.SeaClearanceDetailId = @SeaClearanceDetailId 
                AND af.Type = @Type
            ";

            return conn.Query<int>(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId,
                Type = type
            }).ToList();
        }

        /// <summary>
        /// 更新明細的授權表單
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="type">1=收到正本選單、2=寄文件選單</param>
        /// <param name="formIds"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel UpdateDetailAuthorizationForms(int seaClearanceDetailId, int type, List<int> formIds)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 如果有選擇表單，則新增記錄
                    if (formIds != null && formIds.Any())
                    {
                        // 新增主記錄
                        var insertFormSql = @"
                            INSERT INTO jetf.dbo.SeaClearanceAuthorizationForm 
                            (DataDate, Type, SeaClearanceDetailId, CrtUser)
                            VALUES (@DataDate, @Type, @SeaClearanceDetailId, @CrtUser);
                            SELECT CAST(SCOPE_IDENTITY() as int);
                        ";

                        var authFormId = conn.QuerySingle<int>(insertFormSql, new
                        {
                            DataDate = DateTime.Now.ToString("yyyy/MM/dd"),
                            Type = type,
                            SeaClearanceDetailId = seaClearanceDetailId,
                            CrtUser = GetUserId(),
                        }, transaction);

                        // 新增明細記錄
                        var insertDetailSql = @"
                            INSERT INTO jetf.dbo.SeaClearanceAuthorizationFormDetail 
                            (SeaClearanceAuthorizationFormId, AuthorizationFormId)
                            VALUES (@SeaClearanceAuthorizationFormId, @AuthorizationFormId)
                        ";

                        foreach (var formId in formIds)
                        {
                            conn.Execute(insertDetailSql, new
                            {
                                SeaClearanceAuthorizationFormId = authFormId,
                                AuthorizationFormId = formId,
                            }, transaction);
                        }

                        var field = type == 1 ? SeaClearanceEditField.ReceiveAuthorizationForm : SeaClearanceEditField.SendAuthorizationForm;
                        //編輯紀錄
                        _editHistoryService.RecordEdit(
                           transaction,
                           conn,
                           seaClearanceDetailId,
                           field,
                           string.Join(",", GetAuthorizationFormNamesByIds(formIds, transaction)),
                           ""
                        );
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
        /// 根據ID取得授權表單名稱 (支援交易)
        /// </summary>
        /// <param name="formIds"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private List<string> GetAuthorizationFormNamesByIds(List<int> formIds, System.Data.SqlClient.SqlTransaction transaction = null)
        {
            if (formIds == null || !formIds.Any())
                return new List<string>();

            var sql = @"
                SELECT FormName 
                FROM jetf.dbo.AuthorizationForm 
                WHERE Id IN @FormIds
                ORDER BY FormName
            ";

            return conn.Query<string>(sql, new { FormIds = formIds }, transaction).ToList();
        }

        /// <summary>
        /// 取得授權表單歷史記錄
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="type">1=收到正本選單、2=寄文件選單</param>
        /// <returns></returns>
        public List<AuthorizationFormHistoryModel> GetAuthorizationFormHistory(int seaClearanceDetailId, int type)
        {
            var sql = @"
                SELECT 
                    af.DataDate,
                    af.Type,
                    af.Id as AuthFormId,
                    af.CrtUser,
                    af.CrtDateTime
                FROM jetf.dbo.SeaClearanceAuthorizationForm af
                WHERE af.SeaClearanceDetailId = @SeaClearanceDetailId AND af.Type = @Type
                ORDER BY af.CrtDateTime DESC";

            var parameters = new { SeaClearanceDetailId = seaClearanceDetailId, Type = type };
            var formGroups = conn.Query(sql, parameters).ToList();

            if (!formGroups.Any())
                return new List<AuthorizationFormHistoryModel>();

            // 取得所有相關的表單名稱，一次查詢完成
            var authFormIds = formGroups.Select(g => (int)g.AuthFormId).ToList();
            var allFormNamesSql = @"
                SELECT 
                    afd.SeaClearanceAuthorizationFormId,
                    auth.FormName
                FROM jetf.dbo.SeaClearanceAuthorizationFormDetail afd
                INNER JOIN jetf.dbo.AuthorizationForm auth ON afd.AuthorizationFormId = auth.Id
                WHERE afd.SeaClearanceAuthorizationFormId IN @AuthFormIds
                ORDER BY afd.SeaClearanceAuthorizationFormId, auth.FormName";

            var allFormNames = conn.Query(allFormNamesSql, new { AuthFormIds = authFormIds })
                .GroupBy(x => (int)x.SeaClearanceAuthorizationFormId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => (string)x.FormName).ToList()
                );

            var results = new List<AuthorizationFormHistoryModel>();

            foreach (var group in formGroups)
            {
                var authFormId = (int)group.AuthFormId;
                var formNames = allFormNames.ContainsKey(authFormId)
                    ? allFormNames[authFormId]
                    : new List<string>();

                results.Add(new AuthorizationFormHistoryModel
                {
                    DataDate = group.DataDate?.ToString("yyyy/MM/dd"),
                    CreateTime = group.CrtDateTime,
                    CreateUser = group.CrtUser,
                    FormNames = formNames.Any() ? string.Join(", ", formNames) : ""
                });
            }

            return results;
        }
    }
}
