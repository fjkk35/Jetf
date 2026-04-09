using Dapper;
using Service.Models;
using Service.Services.UserMaster.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.UserMaster
{
    public class UserMasterService : _BaseService
    {
        /// <summary>
        /// 取得所有會員清單
        /// </summary>
        /// <returns>會員清單</returns>
        public List<UserMasterDto> GetUsers()
        {
            var sql = @"
                SELECT 
                    u.USER_ID as UserId,
                    u.USER_NAME as UserName,
                    u.USER_STATUS as UserStatus,
                    u.UPD_OPE as UpdOpe,
                    u.UPD_TIME as UpdTime,
                    ag.Id as GroupId,
                    ag.GroupName as GroupName
                FROM [jetf].[dbo].[USER_MASTER] u
                LEFT JOIN [dbo].[UserAuthorityGroup] uag ON u.USER_ID = uag.UserId
                LEFT JOIN [dbo].[AuthorityGroup] ag ON uag.AuthorityGroupId = ag.Id
                WHERE u.USER_ID<>'admin'
                ORDER BY u.USER_ID";

            var userDict = new Dictionary<string, UserMasterDto>();

            conn.Query<UserMasterDto, AuthorityGroupDto, UserMasterDto>(sql,
                (user, group) =>
                {
                    if (!userDict.TryGetValue(user.UserId, out UserMasterDto userEntry))
                    {
                        userEntry = user;
                        userEntry.AuthorityGroups = new List<AuthorityGroupDto>();
                        userDict.Add(user.UserId, userEntry);
                    }

                    if (group != null && group.GroupId > 0)
                    {
                        userEntry.AuthorityGroups.Add(group);
                    }

                    return userEntry;
                },
                splitOn: "GroupId"
            );

            return userDict.Values.ToList();
        }

        /// <summary>
        /// 取得單一會員資料
        /// </summary>
        /// <param name="userId">會員ID</param>
        /// <returns>會員資料</returns>
        public UserMasterEditDto GetUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var userSql = @"
                SELECT 
                    USER_ID as UserId,
                    USER_NAME as UserName,
                    USER_STATUS as UserStatus
                FROM [jetf].[dbo].[USER_MASTER]
                WHERE USER_ID = @UserId";

            var user = conn.QueryFirstOrDefault<UserMasterEditDto>(userSql, new { UserId = userId });
            
            if (user != null)
            {
                // 取得該會員的權限群組ID列表
                var groupsSql = @"
                    SELECT AuthorityGroupId 
                    FROM [dbo].[UserAuthorityGroup] 
                    WHERE UserId = @UserId";

                var groupIds = conn.Query<int>(groupsSql, new { UserId = userId }).ToList();
                user.AuthorityGroupIds = groupIds;
            }

            return user;
        }

        /// <summary>
        /// 取得權限群組選項
        /// </summary>
        /// <returns>權限群組選項清單</returns>
        public List<AuthorityGroupOptionDto> GetAuthorityGroupOptions()
        {
            var sql = @"
                SELECT 
                    Id,
                    GroupName
                FROM [dbo].[AuthorityGroup]
                ORDER BY Id";

            return conn.Query<AuthorityGroupOptionDto>(sql).ToList();
        }

        /// <summary>
        /// 新增會員
        /// </summary>
        /// <param name="userId">會員ID</param>
        /// <param name="userName">會員名稱</param>
        /// <param name="password">密碼</param>
        /// <param name="userStatus">狀態</param>
        /// <param name="authorityGroupIds">權限群組ID列表</param>
        /// <returns>處理結果</returns>
        public ResponseModel Create(string userId, string userName, string password, string userStatus, List<int> authorityGroupIds)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new ResponseModel("請輸入會員ID");

            if (string.IsNullOrWhiteSpace(userName))
                return new ResponseModel("請輸入會員名稱");

            if (string.IsNullOrWhiteSpace(password))
                return new ResponseModel("請輸入密碼");

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 檢查會員ID是否重複
                        var existsCount = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [jetf].[dbo].[USER_MASTER] WHERE USER_ID = @UserId",
                            new { UserId = userId },
                            transaction);

                        if (existsCount > 0)
                        {
                            transaction.Rollback();
                            return new ResponseModel("會員ID已存在");
                        }

                        // 加密密碼
                        var encryptedPassword = AesEncrypt(password);
                        var currentUser = GetUserId();
                        var currentTime = DateTime.Now;

                        // 新增會員（移除 AuthorityGroupId 欄位）
                        var insertUserSql = @"
                            INSERT INTO [jetf].[dbo].[USER_MASTER]
                            (USER_ID, USER_PASSWORD, USER_NAME, USER_STATUS, UPD_OPE, UPD_TIME)
                            VALUES (@UserId, @UserPassword, @UserName, @UserStatus, @UpdOpe, @UpdTime)";

                        conn.Execute(insertUserSql, new
                        {
                            UserId = userId,
                            UserPassword = encryptedPassword,
                            UserName = userName,
                            UserStatus = userStatus ?? "1",
                            UpdOpe = currentUser,
                            UpdTime = currentTime
                        }, transaction);

                        // 建立會員與權限群組的關聯
                        if (authorityGroupIds != null && authorityGroupIds.Any())
                        {
                            var insertGroupSql = @"
                                INSERT INTO [dbo].[UserAuthorityGroup] (UserId, AuthorityGroupId)
                                VALUES (@UserId, @AuthorityGroupId)";

                            foreach (var groupId in authorityGroupIds.Distinct())
                            {
                                conn.Execute(insertGroupSql, new
                                {
                                    UserId = userId,
                                    AuthorityGroupId = groupId
                                }, transaction);
                            }
                        }

                        transaction.Commit();
                        return new ResponseModel() { status = Status.success, msg = "新增成功" };
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
                return new ResponseModel(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        /// <summary>
        /// 更新會員
        /// </summary>
        /// <param name="userId">會員ID</param>
        /// <param name="userName">會員名稱</param>
        /// <param name="userStatus">狀態</param>
        /// <param name="authorityGroupIds">權限群組ID列表</param>
        /// <param name="password">密碼（選填，不填則不更新密碼）</param>
        /// <returns>處理結果</returns>
        public ResponseModel Update(string userId, string userName, string userStatus, List<int> authorityGroupIds, string password = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new ResponseModel("會員ID不能為空");

            if (string.IsNullOrWhiteSpace(userName))
                return new ResponseModel("請輸入會員名稱");

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 檢查會員是否存在
                        var existsUser = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [jetf].[dbo].[USER_MASTER] WHERE USER_ID = @UserId",
                            new { UserId = userId },
                            transaction) > 0;

                        if (!existsUser)
                        {
                            transaction.Rollback();
                            return new ResponseModel("會員不存在");
                        }

                        var currentUser = GetUserId();
                        var currentTime = DateTime.Now;

                        string updateSql;
                        object parameters;

                        // 根據是否更新密碼決定 SQL（移除 AuthorityGroupId 欄位）
                        if (!string.IsNullOrWhiteSpace(password))
                        {
                            var encryptedPassword = AesEncrypt(password);
                            updateSql = @"
                                UPDATE [jetf].[dbo].[USER_MASTER] 
                                SET USER_PASSWORD = @UserPassword,
                                    USER_NAME = @UserName, 
                                    USER_STATUS = @UserStatus,
                                    UPD_OPE = @UpdOpe,
                                    UPD_TIME = @UpdTime
                                WHERE USER_ID = @UserId";

                            parameters = new
                            {
                                UserPassword = encryptedPassword,
                                UserName = userName,
                                UserStatus = userStatus,
                                UpdOpe = currentUser,
                                UpdTime = currentTime,
                                UserId = userId
                            };
                        }
                        else
                        {
                            updateSql = @"
                                UPDATE [jetf].[dbo].[USER_MASTER] 
                                SET USER_NAME = @UserName, 
                                    USER_STATUS = @UserStatus,
                                    UPD_OPE = @UpdOpe,
                                    UPD_TIME = @UpdTime
                                WHERE USER_ID = @UserId";

                            parameters = new
                            {
                                UserName = userName,
                                UserStatus = userStatus,
                                UpdOpe = currentUser,
                                UpdTime = currentTime,
                                UserId = userId
                            };
                        }

                        conn.Execute(updateSql, parameters, transaction);

                        // 刪除現有的權限群組關聯
                        conn.Execute("DELETE FROM [dbo].[UserAuthorityGroup] WHERE UserId = @UserId", 
                            new { UserId = userId }, transaction);

                        // 重新建立權限群組關聯
                        if (authorityGroupIds != null && authorityGroupIds.Any())
                        {
                            var insertGroupSql = @"
                                INSERT INTO [dbo].[UserAuthorityGroup] (UserId, AuthorityGroupId)
                                VALUES (@UserId, @AuthorityGroupId)";

                            foreach (var groupId in authorityGroupIds.Distinct())
                            {
                                conn.Execute(insertGroupSql, new
                                {
                                    UserId = userId,
                                    AuthorityGroupId = groupId
                                }, transaction);
                            }
                        }

                        transaction.Commit();
                        return new ResponseModel() { status = Status.success, msg = "修改成功" };
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
                return new ResponseModel(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        // 向下相容的舊方法（已棄用）
        [Obsolete("此方法已棄用，請使用支援多個權限群組的 Create 方法")]
        public ResponseModel Create(string userId, string userName, string password, string userStatus, int? authorityGroupId)
        {
            var groupIds = authorityGroupId.HasValue ? new List<int> { authorityGroupId.Value } : new List<int>();
            return Create(userId, userName, password, userStatus, groupIds);
        }

        [Obsolete("此方法已棄用，請使用支援多個權限群組的 Update 方法")]
        public ResponseModel Update(string userId, string userName, string userStatus, int? authorityGroupId, string password = null)
        {
            var groupIds = authorityGroupId.HasValue ? new List<int> { authorityGroupId.Value } : new List<int>();
            return Update(userId, userName, userStatus, groupIds, password);
        }
    }
}
