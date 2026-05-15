using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Services.TaxPortalCustomerService.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;

namespace Service.Services.TaxPortalCustomerService
{
    public class TaxPortalCustomerService : _BaseService
    {
        public TaxPortalCustomerService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        private const int PasswordLength = 12;
        private const int BcryptWorkFactor = 12;
        private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string DigitChars = "0123456789";
        private const string SymbolChars = "!@#$%^&*()-_=+[]{}?";

        /// <summary>
        /// 取得可選客戶分組。
        /// </summary>
        /// <returns>客戶分組資料。</returns>
        public ResponseModel GetCustomerGroups()
        {
            try
            {
                var customerOptions = LoadCustomerOptions();
                var result = new TaxPortalCustomerGroupModel
                {
                    SeaCustomers = customerOptions
                        .Where(x => x.CustomerType == "SEA")
                        .OrderBy(x => x.CustCode)
                        .ToList(),
                    AirCustomers = customerOptions
                        .Where(x => x.CustomerType == "AIR")
                        .OrderBy(x => x.CustCode)
                        .ToList()
                };

                return new ResponseModel(result);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"載入客戶資料失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢帳號列表。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>帳號列表。</returns>
        public ResponseModel QueryUsers(TaxPortalUserQueryRequest request)
        {
            try
            {
                string sql = @"
WITH CustomerSource AS (
    SELECT 
        'SEA' AS CustomerType,
        N'海運' AS CustomerTypeName,
        CUST_CODE AS CustCode,
        CUST_NAME AS CustName
    FROM DATA_CENTER.dbo.SYS_CUST
    WHERE CUST_TYPE = 'SEA'

    UNION ALL

    SELECT DISTINCT
        'AIR' AS CustomerType,
        N'空運' AS CustomerTypeName,
        ISNULL(NULLIF(LTRIM(RTRIM(OLD_CODE)), ''), CUST_CODE) AS CustCode,
        CUST_NAME AS CustName
    FROM DATA_CENTER.dbo.SYS_CUST
    WHERE CUST_TYPE = 'AIR'
        AND CUST_CODE IS NOT NULL
)
SELECT 
    tu.Id,
    tu.UserName,
    tu.Memo,
    tc.CustCode,
    cs.CustName,
    cs.CustomerType,
    cs.CustomerTypeName
FROM jetf.dbo.TaxPortalUser tu
LEFT JOIN jetf.dbo.TaxPortalCustomer tc ON tu.Id = tc.TaxPortalUserId
LEFT JOIN CustomerSource cs ON tc.CustCode = cs.CustCode
WHERE (@UserName = '' OR tu.UserName LIKE '%' + @UserName + '%')
ORDER BY tu.UserName, cs.CustomerType, tc.CustCode";

                var rows = conn.Query<TaxPortalUserQueryRow>(sql, new
                {
                    UserName = request?.UserName?.Trim() ?? string.Empty
                }).ToList();

                var result = rows
                    .GroupBy(x => new { x.Id, x.UserName, x.Memo })
                    .Select(group => new TaxPortalUserSummaryModel
                    {
                        Id = group.Key.Id,
                        UserName = group.Key.UserName,
                        Memo = group.Key.Memo,
                        CustomerCount = group.Count(x => !string.IsNullOrWhiteSpace(x.CustCode)),
                        CustomerSummary = BuildCustomerSummary(group)
                    })
                    .OrderBy(x => x.UserName)
                    .ToList();

                return new ResponseModel(result);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 取得帳號明細。
        /// </summary>
        /// <param name="id">帳號流水號。</param>
        /// <returns>帳號明細。</returns>
        public ResponseModel GetUserDetail(int id)
        {
            try
            {
                string sql = @"
WITH CustomerSource AS (
    SELECT 
        'SEA' AS CustomerType,
        N'海運' AS CustomerTypeName,
        CUST_CODE AS CustCode,
        CUST_NAME AS CustName
    FROM DATA_CENTER.dbo.SYS_CUST
    WHERE CUST_TYPE = 'SEA'

    UNION ALL

    SELECT DISTINCT
        'AIR' AS CustomerType,
        N'空運' AS CustomerTypeName,
        ISNULL(NULLIF(LTRIM(RTRIM(OLD_CODE)), ''), CUST_CODE) AS CustCode,
        CUST_NAME AS CustName
    FROM DATA_CENTER.dbo.SYS_CUST
    WHERE CUST_TYPE = 'AIR'
        AND CUST_CODE IS NOT NULL
)
SELECT 
    tu.Id,
    tu.UserName,
    tu.Memo,
    tc.CustCode,
    cs.CustName,
    cs.CustomerType,
    cs.CustomerTypeName
FROM jetf.dbo.TaxPortalUser tu
LEFT JOIN jetf.dbo.TaxPortalCustomer tc ON tu.Id = tc.TaxPortalUserId
LEFT JOIN CustomerSource cs ON tc.CustCode = cs.CustCode
WHERE tu.Id = @Id
ORDER BY cs.CustomerType, tc.CustCode";

                var rows = conn.Query<TaxPortalUserQueryRow>(sql, new { Id = id }).ToList();
                if (!rows.Any())
                {
                    return new ResponseModel("查無帳號資料");
                }

                var first = rows.First();
                var result = new TaxPortalUserDetailModel
                {
                    Id = first.Id,
                    UserName = first.UserName,
                    Memo = first.Memo,
                    SelectedCustomers = rows
                        .Where(x => !string.IsNullOrWhiteSpace(x.CustCode))
                        .Select(x => new TaxPortalCustomerOptionModel
                        {
                            CustomerType = x.CustomerType,
                            CustomerTypeName = x.CustomerTypeName,
                            CustCode = x.CustCode,
                            CustName = x.CustName
                        })
                        .OrderBy(x => x.CustomerType)
                        .ThenBy(x => x.CustCode)
                        .ToList()
                };

                return new ResponseModel(result);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"載入明細失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 產生符合規則的密碼。
        /// </summary>
        /// <returns>明文密碼。</returns>
        public ResponseModel GeneratePassword()
        {
            try
            {
                return new ResponseModel(new TaxPortalPasswordResultModel
                {
                    Password = GeneratePlainPassword()
                });
            }
            catch (Exception ex)
            {
                return new ResponseModel($"產生密碼失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 新增帳號與可查詢客戶。
        /// </summary>
        /// <param name="request">新增資料。</param>
        /// <param name="createOpe">建立人員。</param>
        /// <returns>新增結果。</returns>
        public ResponseModel CreateUser(TaxPortalUserCreateRequest request, string createOpe)
        {
            try
            {
                string userName = request?.UserName?.Trim();
                string memo = request?.Memo?.Trim();
                List<string> selectedCustCodes = NormalizeCustCodes(request?.SelectedCustCodes);

                var validation = ValidateCreateRequest(userName, selectedCustCodes);
                if (validation != null)
                {
                    return validation;
                }

                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        if (IsDuplicateUserName(userName, null, transaction))
                        {
                            transaction.Rollback();
                            return new ResponseModel("帳號已存在，請重新輸入");
                        }

                        EnsureCustomersExist(selectedCustCodes, transaction);

                        string plainPassword = GeneratePlainPassword();
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword, BcryptWorkFactor);

                        string insertUserSql = @"
INSERT INTO jetf.dbo.TaxPortalUser (UserName, Password, Memo)
VALUES (@UserName, @Password, @Memo);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        int userId = conn.QuerySingle<int>(insertUserSql, new
                        {
                            UserName = userName,
                            Password = hashedPassword,
                            Memo = memo
                        }, transaction);

                        InsertCustomers(userId, selectedCustCodes, createOpe, transaction);

                        transaction.Commit();
                        return new ResponseModel
                        {
                            status = Status.success,
                            msg = "新增成功",
                            ReturnObject = new TaxPortalPasswordResultModel
                            {
                                UserName = userName,
                                Password = plainPassword
                            }
                        };
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
                return new ResponseModel($"新增失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 修改帳號可查詢客戶、備註與密碼。
        /// </summary>
        /// <param name="request">修改資料。</param>
        /// <param name="createOpe">建立人員。</param>
        /// <returns>修改結果。</returns>
        public ResponseModel UpdateUser(TaxPortalUserUpdateRequest request, string createOpe)
        {
            try
            {
                List<string> selectedCustCodes = NormalizeCustCodes(request?.SelectedCustCodes);
                string memo = request?.Memo?.Trim();
                string newPassword = request?.NewPassword?.Trim();

                var validation = ValidateUpdateRequest(request, selectedCustCodes);
                if (validation != null)
                {
                    return validation;
                }

                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string userName = conn.QueryFirstOrDefault<string>(
                            "SELECT UserName FROM jetf.dbo.TaxPortalUser WHERE Id = @Id",
                            new { request.Id },
                            transaction);

                        if (string.IsNullOrWhiteSpace(userName))
                        {
                            transaction.Rollback();
                            return new ResponseModel("查無帳號資料");
                        }

                        EnsureCustomersExist(selectedCustCodes, transaction);

                        string updateUserSql = string.IsNullOrWhiteSpace(newPassword)
                            ? @"UPDATE jetf.dbo.TaxPortalUser SET Memo = @Memo WHERE Id = @Id"
                            : @"UPDATE jetf.dbo.TaxPortalUser SET Memo = @Memo, Password = @Password WHERE Id = @Id";

                        conn.Execute(updateUserSql, new
                        {
                            request.Id,
                            Memo = memo,
                            Password = string.IsNullOrWhiteSpace(newPassword)
                                ? null
                                : BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor)
                        }, transaction);

                        conn.Execute(
                            "DELETE FROM jetf.dbo.TaxPortalCustomer WHERE TaxPortalUserId = @TaxPortalUserId",
                            new { TaxPortalUserId = request.Id },
                            transaction);

                        InsertCustomers(request.Id, selectedCustCodes, createOpe, transaction);

                        transaction.Commit();
                        return new ResponseModel
                        {
                            status = Status.success,
                            msg = "修改成功",
                            ReturnObject = string.IsNullOrWhiteSpace(newPassword)
                                ? null
                                : new TaxPortalPasswordResultModel
                                {
                                    UserName = userName,
                                    Password = newPassword
                                }
                        };
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
                return new ResponseModel($"修改失敗：{ex.Message}");
            }
        }

        private List<TaxPortalCustomerOptionModel> LoadCustomerOptions(IDbTransaction transaction = null)
        {
            string sql = @"
SELECT DISTINCT
    'SEA' AS CustomerType,
    N'海運' AS CustomerTypeName,
    CUST_CODE AS CustCode,
    CUST_NAME AS CustName
FROM DATA_CENTER.dbo.SYS_CUST
WHERE CUST_TYPE = 'SEA'

UNION ALL

SELECT DISTINCT
    'AIR' AS CustomerType,
    N'空運' AS CustomerTypeName,
    OLD_CODE AS CustCode,
    CUST_NAME AS CustName
FROM DATA_CENTER.dbo.SYS_CUST
WHERE CUST_TYPE = 'AIR'
    AND OLD_CODE IS NOT NULL";

            return conn.Query<TaxPortalCustomerOptionModel>(sql, transaction: transaction).ToList();
        }

        private ResponseModel ValidateCreateRequest(string userName, List<string> selectedCustCodes)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return new ResponseModel("請輸入帳號");
            }

            if (!selectedCustCodes.Any())
            {
                return new ResponseModel("請至少選擇一位客戶");
            }

            return null;
        }

        private ResponseModel ValidateUpdateRequest(TaxPortalUserUpdateRequest request, List<string> selectedCustCodes)
        {
            if (request == null || request.Id <= 0)
            {
                return new ResponseModel("缺少帳號資料");
            }

            if (!selectedCustCodes.Any())
            {
                return new ResponseModel("請至少選擇一位客戶");
            }

            return null;
        }

        private List<string> NormalizeCustCodes(List<string> custCodes)
        {
            return (custCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool IsDuplicateUserName(string userName, int? excludeId, IDbTransaction transaction)
        {
            string sql = @"
SELECT COUNT(1)
FROM jetf.dbo.TaxPortalUser
WHERE UserName = @UserName
  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";

            int count = conn.QuerySingle<int>(sql, new { UserName = userName, ExcludeId = excludeId }, transaction);
            return count > 0;
        }

        private void EnsureCustomersExist(List<string> selectedCustCodes, IDbTransaction transaction)
        {
            var validCustCodes = new HashSet<string>(
                LoadCustomerOptions(transaction).Select(x => x.CustCode),
                StringComparer.OrdinalIgnoreCase);

            var invalidCustCodes = selectedCustCodes
                .Where(x => !validCustCodes.Contains(x))
                .ToList();

            if (invalidCustCodes.Any())
            {
                throw new Exception($"客戶資料不存在：{string.Join("、", invalidCustCodes)}");
            }
        }

        private void InsertCustomers(int userId, List<string> selectedCustCodes, string createOpe, IDbTransaction transaction)
        {
            string insertCustomerSql = @"
INSERT INTO jetf.dbo.TaxPortalCustomer (TaxPortalUserId, CustCode, CreateOpe)
VALUES (@TaxPortalUserId, @CustCode, @CreateOpe)";

            foreach (string custCode in selectedCustCodes)
            {
                conn.Execute(insertCustomerSql, new
                {
                    TaxPortalUserId = userId,
                    CustCode = custCode,
                    CreateOpe = createOpe
                }, transaction);
            }
        }

        private string BuildCustomerSummary(IEnumerable<TaxPortalUserQueryRow> rows)
        {
            var groups = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.CustCode))
                .GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerTypeName) ? "未分類" : x.CustomerTypeName)
                .Select(group => $"{group.Key}：{string.Join("、", group.Select(x => FormatCustomerDisplay(x.CustCode, x.CustName)))}")
                .ToList();

            return groups.Any() ? string.Join("；", groups) : "";
        }

        private string FormatCustomerDisplay(string custCode, string custName)
        {
            return string.IsNullOrWhiteSpace(custName) ? custCode : $"{custCode}-{custName}";
        }

        private string GeneratePlainPassword()
        {
            List<char> passwordChars = new List<char>
            {
                GetRandomChar(LowerChars),
                GetRandomChar(UpperChars),
                GetRandomChar(DigitChars),
                GetRandomChar(SymbolChars)
            };

            string allChars = LowerChars + UpperChars + DigitChars + SymbolChars;
            while (passwordChars.Count < PasswordLength)
            {
                passwordChars.Add(GetRandomChar(allChars));
            }

            Shuffle(passwordChars);
            return new string(passwordChars.ToArray());
        }

        private char GetRandomChar(string source)
        {
            byte[] randomBytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            int index = (int)(BitConverter.ToUInt32(randomBytes, 0) % source.Length);
            return source[index];
        }

        private void Shuffle(IList<char> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                byte[] randomBytes = new byte[4];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }

                int swapIndex = (int)(BitConverter.ToUInt32(randomBytes, 0) % (uint)(i + 1));
                char temp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = temp;
            }
        }

        private class TaxPortalUserQueryRow
        {
            public int Id { get; set; }

            public string UserName { get; set; }

            public string Memo { get; set; }

            public string CustCode { get; set; }

            public string CustName { get; set; }

            public string CustomerType { get; set; }

            public string CustomerTypeName { get; set; }
        }
    }
}