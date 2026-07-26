using Service.Data;
using Service.Extensions;
using Service.Services.ReconciliationCustomer.Domain;
using Service.Services.ReconciliationCustomerSelection;
using Service.Services.ReconciliationCustomerSelection.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace Service.Services.ReconciliationCustomer
{
    /// <summary>
    /// 客戶銷帳查詢與確認服務。
    /// </summary>
    public sealed class ReconciliationCustomerService : _BaseService
    {
        private readonly ReconciliationCustomerSelectionService _customerSelectionService;

        /// <summary>
        /// 建立客戶銷帳服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        /// <param name="customerSelectionService">共用客戶選擇服務。</param>
        public ReconciliationCustomerService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext,
            ReconciliationCustomerSelectionService customerSelectionService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _customerSelectionService = customerSelectionService;
        }

        /// <summary>
        /// 查詢符合條件的客戶應收金額合計。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>單筆應收金額合計。</returns>
        public ReconciliationCustomerQueryResult Search(ReconciliationCustomerQueryRequest request)
        {
            var receivableAmount = BuildQuery(request)
                .AsNoTracking()
                .Select(x => (long?)(x.CustomerCod ?? 0))
                .Sum() ?? 0;

            return new ReconciliationCustomerQueryResult
            {
                ReceivableAmount = receivableAmount
            };
        }

        /// <summary>
        /// 取得共用客戶及客戶群組選項。
        /// </summary>
        /// <returns>共用客戶選擇資料。</returns>
        public ReconciliationCustomerSelectionOptions GetCustomerSelectionOptions()
        {
            return _customerSelectionService.GetOptions();
        }

        /// <summary>
        /// 依查詢條件確認客戶銷帳。
        /// </summary>
        /// <param name="request">銷帳條件與輸入金額。</param>
        /// <returns>銷帳執行結果。</returns>
        public ReconciliationCustomerConfirmResult Confirm(ReconciliationCustomerConfirmRequest request)
        {
            if (request?.Query == null)
            {
                throw new ArgumentException("缺少客戶銷帳查詢條件。");
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException("銷帳金額必須大於 0。");
            }

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException("無法取得目前登入人員。");
            }
            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var details = BuildQuery(request.Query)
                    .AsNoTracking()
                    .ToList();
                if (!details.Any())
                {
                    throw new InvalidOperationException("目前查詢條件已無可銷帳資料，請重新查詢。");
                }

                var receivableAmount = details.Sum(x => (long)(x.CustomerCod ?? 0));
                if (request.Amount != receivableAmount)
                {
                    throw new InvalidOperationException(
                        $"銷帳金額必須與應收金額 {receivableAmount:N0} 相同，請重新確認。");
                }

                var receivedTime = DateTime.Now;
                foreach (var detail in details)
                {
                    detail.ReceivedCustomerCod = detail.CustomerCod;
                    detail.ReceivedCustomerCodTime = receivedTime;
                    detail.ReceivedCustomerCodUserId = userId;
                }

                // 使用暫存表一次批次更新銷帳欄位，避免 EF 為每筆明細個別執行 UPDATE。
                JetfDb.BulkUpdate(details);
                transaction.Commit();

                return new ReconciliationCustomerConfirmResult
                {
                    UpdatedCount = details.Count,
                    ReceivedAmount = receivableAmount,
                    ReceivedTime = receivedTime
                };
            }
        }

        /// <summary>
        /// 建立尚未銷帳的客戶應收明細查詢。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>尚未執行的費用明細查詢。</returns>
        private IQueryable<FeeMasterDetailEntity> BuildQuery(ReconciliationCustomerQueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.OutDateStart) ||
                string.IsNullOrWhiteSpace(request.OutDateEnd))
            {
                throw new ArgumentException("日期為必填，請選擇開始日期與結束日期。");
            }

            DateTime startDate;
            DateTime endDate;
            if (!DateTime.TryParse(request.OutDateStart, out startDate) ||
                !DateTime.TryParse(request.OutDateEnd, out endDate))
            {
                throw new ArgumentException("日期格式錯誤。");
            }

            startDate = startDate.Date;
            endDate = endDate.Date;
            if (startDate > endDate)
            {
                throw new ArgumentException("開始日期不可晚於結束日期。");
            }

            var customerCodes = (request.CustomerCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var dlvInvs = ParseDlvInvs(request.DlvInvText);
            var endDateExclusive = endDate.AddDays(1);

            return JetfDb.FeeMasterDetails
                .Where(x =>
                    x.FeeMaster.Download == "1" &&
                    !x.ReceivedCustomerCodTime.HasValue &&
                    (x.CustomerCod ?? 0) > 0 &&
                    x.FeeMaster.OutDateTime.HasValue &&
                    x.FeeMaster.OutDateTime >= startDate &&
                    x.FeeMaster.OutDateTime < endDateExclusive)
                .WhereIf(customerCodes.Any(), x => customerCodes.Contains(x.FeeMaster.Customer))
                .WhereIf(dlvInvs.Any(), x => dlvInvs.Contains(x.DlvInv));
        }

        /// <summary>
        /// 解析以換行分隔的物流貨號。
        /// </summary>
        /// <param name="value">物流貨號多行文字。</param>
        /// <returns>正規化後的物流貨號。</returns>
        private static List<string> ParseDlvInvs(string value)
        {
            return (value ?? string.Empty)
                .Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
