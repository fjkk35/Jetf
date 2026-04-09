using Dapper;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.Authority.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.Authority
{
    public class AuthorityService : _BaseService
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
        /// 取得單一權限資料
        /// </summary>
        /// <param name="id">權限ID</param>
        /// <returns>權限資料</returns>
        public AuthorityEditDto GetAuthority(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var sql = @"SELECT Id, [Text], PartnerId, ISNULL(Sort,0) as Sort 
                       FROM [dbo].[Authority] 
                       WHERE Id = @Id";

            return conn.QueryFirstOrDefault<AuthorityEditDto>(sql, new { Id = id });
        }

        /// <summary>
        /// 新增權限
        /// </summary>
        /// <param name="id">權限ID</param>
        /// <param name="text">權限說明</param>
        /// <param name="partnerId">權限分類</param>
        /// <param name="sort">排序</param>
        /// <returns>處理結果</returns>
        public ResponseModel Create(string id, string text, string partnerId, int sort)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new ResponseModel("請輸入權限ID");

            if (string.IsNullOrWhiteSpace(text))
                return new ResponseModel("請輸入權限說明");

            if (string.IsNullOrWhiteSpace(partnerId))
                return new ResponseModel("請選擇權限分類");

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 檢查權限ID是否重複
                        var existsIdCount = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [dbo].[Authority] WHERE Id = @Id",
                            new { Id = id },
                            transaction);

                        if (existsIdCount > 0)
                        {
                            transaction.Rollback();
                            return new ResponseModel("權限ID已存在");
                        }

                        // 檢查權限說明是否重複
                        var existsTextCount = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [dbo].[Authority] WHERE [Text] = @Text",
                            new { Text = text },
                            transaction);

                        if (existsTextCount > 0)
                        {
                            transaction.Rollback();
                            return new ResponseModel("權限說明已存在");
                        }

                        // 新增權限
                        var insertSql = @"INSERT INTO [dbo].[Authority](Id, [Text], PartnerId, Sort) 
                                         VALUES(@Id, @Text, @PartnerId, @Sort)";

                        conn.Execute(insertSql,
                            new { Id = id, Text = text, PartnerId = partnerId, Sort = sort },
                            transaction);

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
        /// 更新權限
        /// </summary>
        /// <param name="id">權限ID</param>
        /// <param name="text">權限說明</param>
        /// <param name="partnerId">權限分類</param>
        /// <param name="sort">排序</param>
        /// <returns>處理結果</returns>
        public ResponseModel Update(string id, string text, string partnerId, int sort)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new ResponseModel("權限ID不能為空");

            if (string.IsNullOrWhiteSpace(text))
                return new ResponseModel("請輸入權限說明");

            if (string.IsNullOrWhiteSpace(partnerId))
                return new ResponseModel("請選擇權限分類");

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 檢查權限是否存在
                        var existsAuthority = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [dbo].[Authority] WHERE Id = @Id",
                            new { Id = id },
                            transaction) > 0;

                        if (!existsAuthority)
                        {
                            transaction.Rollback();
                            return new ResponseModel("權限不存在");
                        }

                        // 檢查權限說明是否與其他權限重複 (排除自己)
                        var duplicateCount = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM [dbo].[Authority] WHERE [Text] = @Text AND Id <> @Id",
                            new { Text = text, Id = id },
                            transaction);

                        if (duplicateCount > 0)
                        {
                            transaction.Rollback();
                            return new ResponseModel("權限說明已存在");
                        }

                        // 更新權限
                        var updateSql = @"UPDATE [dbo].[Authority] 
                                         SET [Text] = @Text, PartnerId = @PartnerId, Sort = @Sort 
                                         WHERE Id = @Id";

                        conn.Execute(updateSql,
                            new { Text = text, PartnerId = partnerId, Sort = sort, Id = id },
                            transaction);

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

        /// <summary>
        /// 取得權限分類清單
        /// </summary>
        /// <returns>權限分類清單</returns>
        public List<PartnerOptionDto> GetPartnerOptions()
        {
            var partners = Enum.GetValues(typeof(AuthorityPartner))
                              .Cast<AuthorityPartner>()
                              .Select(p => new PartnerOptionDto
                              {
                                  Value = p.ToString(),
                                  Text = p.ToDescription(),
                                  Sort = p.GetSort() ?? 0
                              })
                              .OrderBy(p => p.Sort)
                              .ToList();

            return partners;
        }
    }
}
