using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ReconciliationCustomerGroup.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace Service.Services.ReconciliationCustomerGroup
{
    /// <summary>
    /// 代收銷帳客戶群組查詢與維護服務。
    /// </summary>
    public sealed class ReconciliationCustomerGroupService : _BaseService
    {
        /// <summary>
        /// 建立代收銷帳客戶群組服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReconciliationCustomerGroupService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 查詢代收銷帳客戶群組。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>客戶群組清單。</returns>
        public List<ReconciliationCustomerGroupListItem> Search(ReconciliationCustomerGroupQueryRequest request)
        {
            var query = JetfDb.ReconciliationCustomerGroups
                .AsNoTracking()
                .WhereIf(!string.IsNullOrWhiteSpace(request?.Type), x => x.Type == request.Type)
                .WhereIf(!string.IsNullOrWhiteSpace(request?.GroupName), x => x.GroupName == request.GroupName);

            var groups = query
                .OrderBy(x => x.Type)
                .ThenBy(x => x.GroupName)
                .ToList();

            if (!groups.Any())
            {
                return new List<ReconciliationCustomerGroupListItem>();
            }

            var groupIds = groups.Select(x => x.Id).ToList();
            var details = JetfDb.ReconciliationCustomerGroupDetails
                .AsNoTracking()
                .Where(x => groupIds.Contains(x.CustomerGroupId))
                .OrderBy(x => x.CustCode)
                .ToList();

            var seaCodes = details
                .Where(x => groups.Any(g =>
                    g.Id == x.CustomerGroupId && g.Type == CustomerType.SEA.ToString()))
                .Select(x => x.CustCode);
            var airCodes = details
                .Where(x => groups.Any(g =>
                    g.Id == x.CustomerGroupId && g.Type == CustomerType.AIR.ToString()))
                .Select(x => x.CustCode);
            var seaCustomerNames = GetSeaCustomerNames(seaCodes);
            var airCustomerNames = GetAirCustomerNames(airCodes);

            return groups.Select(group =>
            {
                var customerNames = group.Type == CustomerType.AIR.ToString()
                    ? airCustomerNames
                    : seaCustomerNames;
                var customerDisplay = details
                    .Where(x => x.CustomerGroupId == group.Id)
                    .Select(x => FormatCustomer(x.CustCode, customerNames));

                return new ReconciliationCustomerGroupListItem
                {
                    Id = group.Id,
                    Type = group.Type,
                    TypeName = GetTypeName(group.Type),
                    GroupName = group.GroupName,
                    CustomerDisplay = string.Join(Environment.NewLine, customerDisplay)
                };
            }).ToList();
        }

        /// <summary>
        /// 取得查詢用客戶群組選項。
        /// </summary>
        /// <param name="type">運送類型代碼。</param>
        /// <returns>客戶群組選項。</returns>
        public List<ReconciliationCustomerGroupOption> GetGroupOptions(string type)
        {
            var normalizedType = string.IsNullOrWhiteSpace(type)
                ? null
                : NormalizeType(type).ToString();
            var query = JetfDb.ReconciliationCustomerGroups.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedType))
            {
                query = query.Where(x => x.Type == normalizedType);
            }

            return query
                .OrderBy(x => x.GroupName)
                .Select(x => new ReconciliationCustomerGroupOption
                {
                    Id = x.Id,
                    GroupName = x.GroupName
                })
                .ToList();
        }

        /// <summary>
        /// 取得指定類型的客戶勾選選項。
        /// </summary>
        /// <param name="type">運送類型代碼。</param>
        /// <param name="currentGroupId">目前編輯的客戶群組識別碼。</param>
        /// <returns>客戶勾選選項。</returns>
        public List<ReconciliationCustomerOption> GetCustomerOptions(string type, int? currentGroupId)
        {
            var normalizedType = NormalizeType(type);
            var normalizedTypeCode = normalizedType.ToString();
            List<ReconciliationCustomerOption> customers;

            if (normalizedType == CustomerType.SEA)
            {
                customers = DataCenterDb.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == normalizedTypeCode)
                    .Select(x => new ReconciliationCustomerOption
                    {
                        CustCode = x.CustCode,
                        CustName = x.CustName
                    })
                    .ToList();
            }
            else
            {
                customers = DataCenterDb.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == normalizedTypeCode && !string.IsNullOrEmpty(x.OldCode))
                    .Select(x => new ReconciliationCustomerOption
                    {
                        CustCode = x.OldCode,
                        CustName = x.CustName
                    })
                    .ToList();
            }

            customers = customers
                .Where(x => !string.IsNullOrWhiteSpace(x.CustCode))
                .GroupBy(x => x.CustCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.CustCode)
                .ToList();

            var assignments = JetfDb.ReconciliationCustomerGroupDetails
                .AsNoTracking()
                .Select(detail => new
                {
                    detail.CustCode,
                    GroupId = detail.CustomerGroup.Id,
                    detail.CustomerGroup.GroupName
                })
                .ToList();

            foreach (var customer in customers)
            {
                var customerAssignments = assignments
                    .Where(x => string.Equals(x.CustCode, customer.CustCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var otherAssignment = customerAssignments
                    .FirstOrDefault(x => !currentGroupId.HasValue || x.GroupId != currentGroupId.Value);

                customer.IsSelected = currentGroupId.HasValue &&
                    customerAssignments.Any(x => x.GroupId == currentGroupId.Value);
                customer.IsDisabled = otherAssignment != null;
                customer.AssignedGroupName = otherAssignment?.GroupName ??
                    customerAssignments.FirstOrDefault()?.GroupName;
            }

            return customers;
        }

        /// <summary>
        /// 取得客戶群組編輯資料。
        /// </summary>
        /// <param name="id">客戶群組識別碼。</param>
        /// <returns>客戶群組編輯資料。</returns>
        public ReconciliationCustomerGroupSaveRequest GetDetail(int id)
        {
            var group = JetfDb.ReconciliationCustomerGroups
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id);

            if (group == null)
            {
                throw new InvalidOperationException("找不到客戶群組");
            }

            return new ReconciliationCustomerGroupSaveRequest
            {
                Id = group.Id,
                Type = group.Type,
                GroupName = group.GroupName,
                CustCodes = JetfDb.ReconciliationCustomerGroupDetails
                    .AsNoTracking()
                    .Where(x => x.CustomerGroupId == group.Id)
                    .OrderBy(x => x.CustCode)
                    .Select(x => x.CustCode)
                    .ToList()
            };
        }

        /// <summary>
        /// 新增或修改客戶群組。
        /// </summary>
        /// <param name="request">客戶群組資料。</param>
        public void Save(ReconciliationCustomerGroupSaveRequest request)
        {
            NormalizeAndValidateRequest(request);
            ValidateCustomerCodes(request.Type, request.CustCodes);

            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var currentGroupId = request.Id ?? 0;
                if (JetfDb.ReconciliationCustomerGroups.Any(x =>
                    x.GroupName == request.GroupName && x.Id != currentGroupId))
                {
                    throw new InvalidOperationException("群組名稱不可重複");
                }

                var selectedCodes = request.CustCodes;
                var conflicts = JetfDb.ReconciliationCustomerGroupDetails
                    .AsNoTracking()
                    .Where(detail =>
                        detail.CustomerGroupId != currentGroupId &&
                        selectedCodes.Contains(detail.CustCode))
                    .Select(detail => new
                    {
                        detail.CustCode,
                        detail.CustomerGroup.GroupName
                    })
                    .ToList();

                if (conflicts.Any())
                {
                    var conflictText = string.Join("、", conflicts
                        .Select(x => $"{x.CustCode}（{x.GroupName}）")
                        .Distinct()
                        .Take(5));
                    throw new InvalidOperationException($"客戶已加入其他群組：{conflictText}");
                }

                ReconciliationCustomerGroupEntity groupEntity;
                if (request.Id.HasValue)
                {
                    groupEntity = JetfDb.ReconciliationCustomerGroups
                        .FirstOrDefault(x => x.Id == request.Id.Value);
                    if (groupEntity == null)
                    {
                        throw new InvalidOperationException("找不到客戶群組");
                    }

                    var oldDetails = JetfDb.ReconciliationCustomerGroupDetails
                        .Where(x => x.CustomerGroupId == groupEntity.Id)
                        .ToList();
                    JetfDb.ReconciliationCustomerGroupDetails.RemoveRange(oldDetails);
                    // 先刪除舊明細，避免未變更的客戶代碼與唯一索引衝突。
                    JetfDb.SaveChanges();
                }
                else
                {
                    var nextId = JetfDb.ReconciliationCustomerGroups.Any()
                        ? JetfDb.ReconciliationCustomerGroups.Max(x => x.Id) + 1
                        : 1;
                    groupEntity = new ReconciliationCustomerGroupEntity
                    {
                        Id = nextId
                    };
                    JetfDb.ReconciliationCustomerGroups.Add(groupEntity);
                }

                groupEntity.Type = request.Type;
                groupEntity.GroupName = request.GroupName;

                var newDetails = request.CustCodes.Select(custCode =>
                    new ReconciliationCustomerGroupDetailEntity
                    {
                        CustomerGroupId = groupEntity.Id,
                        CustCode = custCode
                    });
                JetfDb.ReconciliationCustomerGroupDetails.AddRange(newDetails);

                JetfDb.SaveChanges();
                transaction.Commit();
            }
        }

        /// <summary>
        /// 刪除客戶群組與群組明細。
        /// </summary>
        /// <param name="id">客戶群組識別碼。</param>
        public void Delete(int id)
        {
            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                var group = JetfDb.ReconciliationCustomerGroups.FirstOrDefault(x => x.Id == id);
                if (group == null)
                {
                    throw new InvalidOperationException("找不到客戶群組");
                }

                var details = JetfDb.ReconciliationCustomerGroupDetails
                    .Where(x => x.CustomerGroupId == id)
                    .ToList();
                JetfDb.ReconciliationCustomerGroupDetails.RemoveRange(details);
                JetfDb.ReconciliationCustomerGroups.Remove(group);
                JetfDb.SaveChanges();
                transaction.Commit();
            }
        }

        private static CustomerType NormalizeType(string type)
        {
            var value = (type ?? string.Empty).Trim().ToUpperInvariant();
            if (value == "海運")
            {
                return CustomerType.SEA;
            }

            if (value == "空運")
            {
                return CustomerType.AIR;
            }

            CustomerType customerType;
            if (Enum.TryParse(value, true, out customerType) && Enum.IsDefined(typeof(CustomerType), customerType))
            {
                return customerType;
            }

            throw new ArgumentException("類型僅限海運或空運");
        }

        private static string GetTypeName(string type)
        {
            return NormalizeType(type).ToDescription();
        }

        private static string FormatCustomer(string custCode, IDictionary<string, string> customerNames)
        {
            string customerName;
            return customerNames.TryGetValue(custCode, out customerName) && !string.IsNullOrWhiteSpace(customerName)
                ? $"{custCode} - {customerName}"
                : custCode;
        }

        private static void NormalizeAndValidateRequest(ReconciliationCustomerGroupSaveRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("未提供客戶群組資料");
            }

            request.Type = NormalizeType(request.Type).ToString();
            request.GroupName = (request.GroupName ?? string.Empty).Trim();
            request.CustCodes = (request.CustCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(request.GroupName))
            {
                throw new ArgumentException("請輸入群組名稱");
            }

            if (request.GroupName.Length > 20)
            {
                throw new ArgumentException("群組名稱不可超過 20 個字元");
            }

            if (!request.CustCodes.Any())
            {
                throw new ArgumentException("請至少選擇一個客戶");
            }

            if (request.CustCodes.Any(x => x.Length > 20))
            {
                throw new ArgumentException("客戶代碼不可超過 20 個字元");
            }
        }

        private void ValidateCustomerCodes(string type, ICollection<string> custCodes)
        {
            var customerType = NormalizeType(type);
            var customerTypeCode = customerType.ToString();
            List<string> availableCodes;
            if (customerType == CustomerType.SEA)
            {
                availableCodes = DataCenterDb.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == customerTypeCode && !string.IsNullOrEmpty(x.CustCode))
                    .Select(x => x.CustCode)
                    .ToList();
            }
            else
            {
                availableCodes = DataCenterDb.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == customerTypeCode && !string.IsNullOrEmpty(x.OldCode))
                    .Select(x => x.OldCode)
                    .ToList();
            }

            var availableCodeSet = new HashSet<string>(availableCodes, StringComparer.OrdinalIgnoreCase);
            var invalidCodes = custCodes.Where(x => !availableCodeSet.Contains(x)).Take(5).ToList();
            if (invalidCodes.Any())
            {
                throw new ArgumentException($"客戶不屬於所選類型：{string.Join("、", invalidCodes)}");
            }
        }
    }
}
