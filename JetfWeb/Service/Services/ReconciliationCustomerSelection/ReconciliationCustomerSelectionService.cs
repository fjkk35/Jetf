using Service.Data;
using Service.EnumTax;
using Service.Services.ReconciliationCustomerSelection.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Service.Services.ReconciliationCustomerSelection
{
    /// <summary>
    /// 提供代收銷帳作業共用的客戶及客戶群組選項。
    /// </summary>
    public sealed class ReconciliationCustomerSelectionService : _BaseService
    {
        /// <summary>
        /// 建立代收銷帳共用客戶選擇服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReconciliationCustomerSelectionService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得海運、空運客戶及客戶群組選項。
        /// </summary>
        /// <returns>共用客戶選擇資料。</returns>
        public ReconciliationCustomerSelectionOptions GetOptions()
        {
            var seaType = CustomerType.SEA.ToString();
            var airType = CustomerType.AIR.ToString();

            var seaCustomers = DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == seaType && !string.IsNullOrEmpty(x.CustCode))
                .Select(x => new ReconciliationCustomerOption
                {
                    Type = seaType,
                    CustCode = x.CustCode,
                    CustName = x.CustName
                })
                .ToList();

            var airCustomers = DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == airType && !string.IsNullOrEmpty(x.OldCode))
                .Select(x => new ReconciliationCustomerOption
                {
                    Type = airType,
                    CustCode = x.OldCode,
                    CustName = x.CustName
                })
                .ToList();

            var groups = JetfDb.ReconciliationCustomerGroups
                .AsNoTracking()
                .Include(x => x.Details)
                .OrderBy(x => x.Type)
                .ThenBy(x => x.GroupName)
                .ToList()
                .Select(x => new ReconciliationCustomerGroupOption
                {
                    Id = x.Id,
                    Type = x.Type,
                    GroupName = x.GroupName,
                    CustCodes = (x.Details ?? Enumerable.Empty<ReconciliationCustomerGroupDetailEntity>())
                        .Where(detail => !string.IsNullOrWhiteSpace(detail.CustCode))
                        .Select(detail => detail.CustCode.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(code => code)
                        .ToList()
                })
                .ToList();

            return new ReconciliationCustomerSelectionOptions
            {
                SeaCustomers = NormalizeCustomerOptions(seaCustomers),
                AirCustomers = NormalizeCustomerOptions(airCustomers),
                Groups = groups
            };
        }

        /// <summary>
        /// 移除無效或重複的客戶選項並依客戶代號排序。
        /// </summary>
        /// <param name="customers">原始客戶選項。</param>
        /// <returns>正規化後的客戶選項。</returns>
        private static List<ReconciliationCustomerOption> NormalizeCustomerOptions(
            IEnumerable<ReconciliationCustomerOption> customers)
        {
            return customers
                .Where(x => !string.IsNullOrWhiteSpace(x.CustCode))
                .GroupBy(x => x.CustCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new ReconciliationCustomerOption
                {
                    Type = group.First().Type,
                    CustCode = group.Key,
                    CustName = group.Select(x => x.CustName)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                })
                .OrderBy(x => x.CustCode)
                .ToList();
        }
    }
}
