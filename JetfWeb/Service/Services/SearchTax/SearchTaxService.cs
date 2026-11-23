using Service.Data;
using Service.Services.SearchTax.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.SearchTax
{
    /// <summary>
    /// 稅金查詢服務。
    /// </summary>
    public sealed class SearchTaxService : _BaseService
    {
        /// <summary>
        /// 查詢紀錄的備註分類。
        /// </summary>
        private const string CargoStatusLogRemark = "貨況查詢";

        private readonly GlobalService _globalService;

        /// <summary>
        /// 建立稅金查詢服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        /// <param name="globalService">共用服務。</param>
        public SearchTaxService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext,
            GlobalService globalService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _globalService = globalService;
        }

        /// <summary>
        /// 依物流貨號、分提單號(海運)或袋號(空運)查詢費用主檔。
        /// </summary>
        /// <param name="keyword">查詢號碼。</param>
        /// <returns>查詢結果，海運在前、空運在後。</returns>
        public List<SearchTaxRow> Search(string keyword)
        {
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                return new List<SearchTaxRow>();
            }

            // 海運(SOURCE_TYPE 1、2)比對物流貨號或分提單號；空運(SOURCE_TYPE 3)比對物流貨號或袋號。
            var feeMasters = JetfDb.FeeMasters
                .AsNoTracking()
                .Where(x =>
                    ((x.SourceType == "1" || x.SourceType == "2") &&
                     (x.DlvInv == keyword || x.TrackingNo == keyword)) ||
                    (x.SourceType == "3" &&
                     (x.DlvInv == keyword || x.BagNumber == keyword)))
                .Select(x => new
                {
                    x.Id,
                    x.DataDate,
                    x.Source,
                    x.SourceType,
                    x.Type,
                    x.Customer,
                    x.InDateTime,
                    x.OutDateTime,
                    x.MainNumber,
                    x.TrackingNo,
                    x.BagNumber,
                    x.DlvInv,
                    x.TaxNumber,
                    x.DlvCom,
                    x.Recipient,
                    x.RecPhone,
                    x.RecAddress,
                    x.IncludeTax,
                    x.Ccfee,
                    x.Fee,
                    x.Cod,
                    x.Tax1,
                    x.Tax2,
                    x.ToDlvCod
                })
                .ToList();

            if (!feeMasters.Any())
            {
                return new List<SearchTaxRow>();
            }

            var seaRows = feeMasters.Where(x => x.SourceType != "3").ToList();
            var airRows = feeMasters.Where(x => x.SourceType == "3").ToList();

            var seaCustomerNames = GetSysCustNames(seaRows.Select(x => x.Customer));
            var airCustomers = GetAirCustomerMasters(airRows.Select(x => new KeyValuePair<string, string>(x.Customer, x.DlvCom)));

            var result = new List<SearchTaxRow>();

            foreach (var x in seaRows.OrderBy(x => x.Id))
            {
                // 海運：客戶名稱取 SYS_CUST，查無時顯示客戶代號；清關袋號顯示分提單號；派件公司為 DLV_COM。
                var customerName = LookupValue(seaCustomerNames, x.Customer);
                result.Add(CreateRow(
                    x.Id, x.DataDate, x.Source, x.Type,
                    string.IsNullOrEmpty(customerName) ? x.Customer : customerName,
                    x.MainNumber, x.TrackingNo, x.DlvInv, x.TaxNumber, x.DlvCom,
                    x.Recipient, x.RecPhone, x.RecAddress,
                    x.InDateTime, x.OutDateTime, x.IncludeTax,
                    x.Tax1, x.Tax2, x.Ccfee, x.Fee, x.Cod, x.ToDlvCod));
            }

            foreach (var x in airRows.OrderBy(x => x.Id))
            {
                // 空運：客戶名稱、派件公司取 customer_master(空運)，以補零 5 碼客戶代號＋派件代號對應。
                airCustomers.TryGetValue(BuildAirCustomerKey(x.Customer, x.DlvCom), out var customer);
                result.Add(CreateRow(
                    x.Id, x.DataDate, x.Source, x.Type,
                    customer?.Customer,
                    x.MainNumber, x.BagNumber, x.DlvInv, x.TaxNumber, customer?.TransName,
                    x.Recipient, x.RecPhone, x.RecAddress,
                    x.InDateTime, x.OutDateTime, x.IncludeTax,
                    x.Tax1, x.Tax2, x.Ccfee, x.Fee, x.Cod, x.ToDlvCod));
            }

            return result;
        }

        /// <summary>
        /// 取得物流貨號的費用明細與配送進度，並寫入貨況查詢紀錄。
        /// </summary>
        /// <param name="dlvInv">物流貨號。</param>
        /// <returns>明細資料。</returns>
        public SearchTaxDetail GetDetail(string dlvInv)
        {
            dlvInv = dlvInv?.Trim();
            if (string.IsNullOrEmpty(dlvInv))
            {
                return new SearchTaxDetail();
            }

            var detail = new SearchTaxDetail
            {
                Fee = Search(dlvInv).FirstOrDefault(),
                CargoStatuses = DataCenterDb.CargoStatusDetails
                    .AsNoTracking()
                    .Where(x => x.TransNumber == dlvInv)
                    .OrderByDescending(x => x.TransModifyTime)
                    .Select(x => new { x.TransModifyTime, x.TransStatusDesc })
                    .ToList()
                    .Select(x => new SearchTaxCargoStatus
                    {
                        TransModifyTimeText = FormatDateTime(x.TransModifyTime),
                        TransStatusDesc = x.TransStatusDesc ?? string.Empty
                    })
                    .ToList()
            };

            InsertCargoStatusLog(dlvInv);

            return detail;
        }

        /// <summary>
        /// 取得物流貨號的貨況查詢紀錄。
        /// </summary>
        /// <param name="dlvInv">物流貨號。</param>
        /// <returns>查詢紀錄，依查詢時間由新到舊排序。</returns>
        public List<SearchTaxLog> GetSearchLogs(string dlvInv)
        {
            dlvInv = dlvInv?.Trim();
            if (string.IsNullOrEmpty(dlvInv))
            {
                return new List<SearchTaxLog>();
            }

            // LOG_CARGO_STATUS 無對應 Entity，以 DbContext 參數化查詢讀取；查詢時間統一轉成 yyyy-MM-dd HH:mm:ss。
            const string sql = @"
                SELECT DLV_INV AS DlvInv,
                       CONVERT(varchar(19), SEARCH_TIME, 120) AS SearchTime,
                       USER_ID AS UserId,
                       USER_IP AS UserIp
                FROM [jetf].[dbo].[LOG_CARGO_STATUS]
                WHERE DLV_INV = @DlvInv AND REMARK = @Remark
                ORDER BY SEARCH_TIME DESC";

            return JetfDb.Database
                .SqlQuery<SearchTaxLog>(
                    sql,
                    new SqlParameter("@DlvInv", SqlDbType.NVarChar) { Value = dlvInv },
                    new SqlParameter("@Remark", SqlDbType.NVarChar) { Value = CargoStatusLogRemark })
                .ToList();
        }

        /// <summary>
        /// 寫入貨況查詢紀錄；寫入失敗不影響查詢。
        /// </summary>
        /// <param name="dlvInv">物流貨號。</param>
        private void InsertCargoStatusLog(string dlvInv)
        {
            const string sql = @"
                INSERT [jetf].[dbo].[LOG_CARGO_STATUS]([DLV_INV],[SEARCH_TIME],[REMARK],[USER_ID],[USER_IP])
                VALUES(@DlvInv, @SearchTime, @Remark, @UserId, @UserIp)";

            try
            {
                JetfDb.Database.ExecuteSqlCommand(
                    sql,
                    new SqlParameter("@DlvInv", SqlDbType.NVarChar) { Value = dlvInv },
                    new SqlParameter("@SearchTime", SqlDbType.NVarChar) { Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                    new SqlParameter("@Remark", SqlDbType.NVarChar) { Value = CargoStatusLogRemark },
                    new SqlParameter("@UserId", SqlDbType.NVarChar) { Value = (object)GetUserId() ?? DBNull.Value },
                    new SqlParameter("@UserIp", SqlDbType.NVarChar) { Value = (object)_globalService.GetIPAddress() ?? DBNull.Value });
            }
            catch (Exception)
            {
                // 記錄 LOG 失敗不影響查詢
            }
        }

        /// <summary>
        /// 依客戶代號取得 SYS_CUST 客戶名稱。
        /// </summary>
        private Dictionary<string, string> GetSysCustNames(IEnumerable<string> customerCodes)
        {
            var codes = customerCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (!codes.Any())
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => codes.Contains(x.CustCode))
                .Select(x => new { x.CustCode, x.CustName })
                .ToList()
                .GroupBy(x => x.CustCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.CustName).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依空運客戶代號(補零 5 碼)＋派件代號取得 customer_master 資料。
        /// </summary>
        private Dictionary<string, CustomerMasterEntity> GetAirCustomerMasters(
            IEnumerable<KeyValuePair<string, string>> customerAndTransNos)
        {
            var pairs = customerAndTransNos.ToList();
            var custIds = pairs
                .Select(x => PadCustomerCode(x.Key))
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();
            var transNos = pairs
                .Select(x => x.Value?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            if (!custIds.Any() || !transNos.Any())
            {
                return new Dictionary<string, CustomerMasterEntity>(StringComparer.OrdinalIgnoreCase);
            }

            return JetfDb.CustomerMasters
                .AsNoTracking()
                .Where(x => x.TranType == "空運" && custIds.Contains(x.CustId) && transNos.Contains(x.TransNo))
                .ToList()
                .GroupBy(x => BuildAirCustomerKey(x.CustId, x.TransNo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        private SearchTaxRow CreateRow(
            int id, string dataDate, string source, string type, string custName,
            string mainNumber, string bagNumber, string dlvInv, string taxNumber, string transName,
            string recipient, string recPhone, string recAddress,
            DateTime? inDateTime, DateTime? outDateTime, string includeTax,
            int? tax1, int? tax2, int? ccFee, int? fee, int? cod, string toDlvCod)
        {
            return new SearchTaxRow
            {
                Id = id,
                DataDate = dataDate ?? string.Empty,
                Source = source ?? string.Empty,
                Type = type ?? string.Empty,
                CustName = custName ?? string.Empty,
                MainNumber = mainNumber ?? string.Empty,
                BagNumber = bagNumber ?? string.Empty,
                DlvInv = dlvInv?.Trim() ?? string.Empty,
                TaxNumber = taxNumber ?? string.Empty,
                TransName = transName ?? string.Empty,
                Recipient = recipient ?? string.Empty,
                RecPhone = recPhone ?? string.Empty,
                RecAddress = recAddress ?? string.Empty,
                InDateTimeText = FormatDateTime(inDateTime),
                OutDateText = outDateTime.HasValue ? outDateTime.Value.ToString("yyyy-MM-dd") : string.Empty,
                OutDateTimeText = FormatDateTime(outDateTime),
                IncludeTax = _globalService.GetTaxType(includeTax?.Trim()) ?? string.Empty,
                Tax1 = tax1 ?? 0,
                Tax2 = tax2 ?? 0,
                TotalTax = (tax1 ?? 0) + (tax2 ?? 0),
                CcFee = ccFee,
                Fee = fee,
                Cod = cod,
                ToDlvCod = toDlvCod ?? string.Empty
            };
        }

        private static string LookupValue(Dictionary<string, string> map, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return map.TryGetValue(key.Trim(), out var value) ? value : string.Empty;
        }

        private static string BuildAirCustomerKey(string customer, string transNo)
        {
            return PadCustomerCode(customer) + "|" + (transNo?.Trim() ?? string.Empty);
        }

        private static string PadCustomerCode(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().PadLeft(5, '0');
        }

        private static string FormatDateTime(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;
        }
    }
}
