using Dapper;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.AuthorityGroup.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.AuthorityGroup
{
    public class AuthorityGroupService : _BaseService
    {
        /// <summary>
        /// 取得所有權限清單 (依 PartnerId, Sort 排序)
        /// </summary>
        /// <returns>權限清單</returns>
        public List<AuthorityDto> GetAuthorities()
        {
            var sql = @"SELECT Id, [Text], PartnerId, ISNULL(Sort,0) as Sort 
                       FROM [dbo].[Authority] 
                       ORDER BY PartnerId, Sort, Id";

            return conn.Query<AuthorityDto>(sql)
                .OrderBy(r => r.PartnerSort)
                .ThenBy(r => r.Sort)
                .ThenBy(r => r.Id)
                .ToList();
        }

        /// <summary>
        /// 取得所有權限群組清單（包含權限資訊）
        /// </summary>
        /// <returns>權限群組清單</returns>
        public List<AuthorityGroupDto> GetGroups()
        {
            // 取得所有群組
            var groupSql = @"SELECT Id, GroupName, Memo 
                           FROM [dbo].[AuthorityGroup] 
                           ORDER BY Id DESC";
            
            var groups = conn.Query<AuthorityGroupDto>(groupSql).ToList();

            // 如果沒有群組，直接回傳
            if (!groups.Any()) return groups;

            // 取得所有群組的權限詳細資料
            var groupIds = groups.Select(g => g.Id).ToList();
            var authoritiesSql = @"
                SELECT 
                    agd.AuthorityGroupId,
                    a.Id,
                    a.[Text],
                    a.PartnerId,
                    ISNULL(a.Sort, 0) as Sort
                FROM [dbo].[AuthorityGroupDetail] agd
                INNER JOIN [dbo].[Authority] a ON agd.AuthorityId = a.Id
                WHERE agd.AuthorityGroupId IN @GroupIds
                ORDER BY agd.AuthorityGroupId, a.PartnerId, a.Sort, a.Id";

            var groupAuthorities = conn.Query<dynamic>(authoritiesSql, new { GroupIds = groupIds }).ToList();

            // 將權限資料分組並填入對應的群組
            var authorityLookup = groupAuthorities
                .GroupBy(x => (int)x.AuthorityGroupId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new AuthorityDto
                    {
                        Id = x.Id,
                        Text = x.Text,
                        PartnerId = x.PartnerId,
                        Sort = x.Sort
                    }).ToList()
                );

            // 填入每個群組的權限資料
            foreach (var group in groups)
            {
                if (authorityLookup.ContainsKey(group.Id))
                {
                    group.Authorities = authorityLookup[group.Id];
                }
            }

            return groups;
        }

        /// <summary>
        /// 取得單一權限群組資料 (包含已選權限)
        /// </summary>
        /// <param name="id">群組ID</param>
        /// <returns>權限群組編輯資料</returns>
        public AuthorityGroupEditDto GetGroup(int id)
        {
            var groupSql = @"SELECT Id, GroupName, Memo 
                            FROM [dbo].[AuthorityGroup] 
                            WHERE Id = @Id";
            
            var detailSql = @"SELECT AuthorityId 
                             FROM [dbo].[AuthorityGroupDetail] 
                             WHERE AuthorityGroupId = @Id";

            var group = conn.QueryFirstOrDefault<AuthorityGroupDto>(groupSql, new { Id = id });
            if (group == null) return null;

            var authIds = conn.Query<string>(detailSql, new { Id = id }).ToList();

            return new AuthorityGroupEditDto
            {
                Id = group.Id,
                GroupName = group.GroupName,
                Memo = group.Memo,
                AuthorityIds = authIds
            };
        }

        /// <summary>
        /// 新增權限群組
        /// </summary>
        /// <param name="groupName">群組名稱</param>
        /// <param name="memo">備註</param>
        /// <param name="authorityIds">權限ID清單</param>
        /// <returns>處理結果</returns>
        public ResopnseModel Create(string groupName, string memo, List<string> authorityIds)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return new ResopnseModel("請輸入群組名稱");

            try
            {
                if (conn.State != ConnectionState.Open) 
                    conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 檢查群組名稱是否重複
                        var existsCount = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [dbo].[AuthorityGroup] WHERE GroupName = @GroupName",
                            new { GroupName = groupName }, 
                            transaction);

                        if (existsCount > 0)
                        {
                            transaction.Rollback();
                            return new ResopnseModel("群組名稱已存在");
                        }

                        // 新增權限群組
                        var insertGroupSql = @"INSERT INTO [dbo].[AuthorityGroup](GroupName, Memo) 
                                              VALUES(@GroupName, @Memo);
                                              SELECT CAST(SCOPE_IDENTITY() as int);";

                        int newGroupId = conn.ExecuteScalar<int>(insertGroupSql, 
                            new { GroupName = groupName, Memo = memo ?? string.Empty }, 
                            transaction);

                        // 新增權限群組明細
                        if (authorityIds != null && authorityIds.Count > 0)
                        {
                            var insertDetailSql = @"INSERT INTO [dbo].[AuthorityGroupDetail](AuthorityGroupId, AuthorityId) 
                                                    VALUES(@AuthorityGroupId, @AuthorityId)";

                            var detailParams = authorityIds.Select(authId => new 
                            { 
                                AuthorityGroupId = newGroupId, 
                                AuthorityId = authId 
                            });

                            conn.Execute(insertDetailSql, detailParams, transaction);
                        }

                        transaction.Commit();
                        return new ResopnseModel() { status = Status.success, msg = "新增成功" };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        /// <summary>
        /// 更新權限群組
        /// </summary>
        /// <param name="id">群組ID</param>
        /// <param name="groupName">群組名稱</param>
        /// <param name="memo">備註</param>
        /// <param name="authorityIds">權限ID清單</param>
        /// <returns>處理結果</returns>
        public ResopnseModel Update(int id, string groupName, string memo, List<string> authorityIds)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return new ResopnseModel("請輸入群組名稱");

            try
            {
                if (conn.State != ConnectionState.Open) 
                    conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 檢查群組是否存在
                        var existsGroup = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [dbo].[AuthorityGroup] WHERE Id = @Id",
                            new { Id = id }, 
                            transaction) > 0;

                        if (!existsGroup)
                        {
                            transaction.Rollback();
                            return new ResopnseModel("群組不存在");
                        }

                        // 檢查群組名稱是否與其他群組重複 (排除自己)
                        var duplicateCount = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [dbo].[AuthorityGroup] WHERE GroupName = @GroupName AND Id <> @Id",
                            new { GroupName = groupName, Id = id }, 
                            transaction);

                        if (duplicateCount > 0)
                        {
                            transaction.Rollback();
                            return new ResopnseModel("群組名稱已存在");
                        }

                        // 更新權限群組
                        var updateGroupSql = @"UPDATE [dbo].[AuthorityGroup] 
                                              SET GroupName = @GroupName, Memo = @Memo 
                                              WHERE Id = @Id";

                        conn.Execute(updateGroupSql, 
                            new { GroupName = groupName, Memo = memo ?? string.Empty, Id = id }, 
                            transaction);

                        // 刪除原有的權限群組明細
                        conn.Execute("DELETE FROM [dbo].[AuthorityGroupDetail] WHERE AuthorityGroupId = @Id", 
                            new { Id = id }, transaction);

                        // 新增新的權限群組明細
                        if (authorityIds != null && authorityIds.Count > 0)
                        {
                            var insertDetailSql = @"INSERT INTO [dbo].[AuthorityGroupDetail](AuthorityGroupId, AuthorityId) 
                                                    VALUES(@AuthorityGroupId, @AuthorityId)";

                            var detailParams = authorityIds.Select(authId => new 
                            { 
                                AuthorityGroupId = id, 
                                AuthorityId = authId 
                            });

                            conn.Execute(insertDetailSql, detailParams, transaction);
                        }

                        transaction.Commit();
                        return new ResopnseModel() { status = Status.success, msg = "修改成功" };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        /// <summary>
        /// 刪除權限群組 (同時刪除相關明細)
        /// </summary>
        /// <param name="id">群組ID</param>
        /// <returns>處理結果</returns>
        public ResopnseModel Delete(int id)
        {
            try
            {
                if (conn.State != ConnectionState.Open) 
                    conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 刪除權限群組明細
                        conn.Execute("DELETE FROM [dbo].[AuthorityGroupDetail] WHERE AuthorityGroupId = @Id", 
                            new { Id = id }, transaction);

                        // 刪除權限群組
                        int affectedRows = conn.Execute("DELETE FROM [dbo].[AuthorityGroup] WHERE Id = @Id", 
                            new { Id = id }, transaction);

                        transaction.Commit();

                        if (affectedRows == 0)
                            return new ResopnseModel("群組不存在");

                        return new ResopnseModel() { status = Status.success, msg = "刪除成功" };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }
    }
}
