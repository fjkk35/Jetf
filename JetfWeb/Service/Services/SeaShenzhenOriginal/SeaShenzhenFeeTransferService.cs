using Service.Data;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞深圳稅金轉檔服務。
    /// </summary>
    public class SeaShenzhenFeeTransferService : _BaseService
    {
        private const string TargetCustomer = "CN00132";
        private const int FeeWhenTaxPaymentC = 30;

        public SeaShenzhenFeeTransferService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 依資料日期將 FeeMaster 稅金資料轉入深圳轉檔表。
        /// </summary>
        public SeaShenzhenFeeTransferResponse Transfer(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = (request?.DataDate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dataDate))
            {
                throw new Exception("請輸入資料日期");
            }

            // 取得指定日期的新遞 FeeMaster 資料，後續只處理 CUSTOMER = CN00132 的資料。
            var feeMasters = JetfDb.FeeMasters
                .AsNoTracking()
                .Where(x => x.DataDate == dataDate && x.Customer == TargetCustomer)
                .OrderBy(x => x.Id)
                .ToList();

            // 先用分提單號批次查出原始託運資料，避免逐筆查詢造成大量 DB round trip。
            var originalLookup = GetOriginalLookup(feeMasters);
            var now = DateTime.Now;
            var userId = GetUserId();
            var transferRows = new List<ShenzhenFeeMasterEntity>();
            var exceptions = new List<SeaShenzhenFeeTransferExceptionRow>();

            // 逐筆比對 FeeMaster 與原始託運資料，找不到對應資料時列入異常清單。
            foreach (var feeMaster in feeMasters)
            {
                var trackingNo = feeMaster.TrackingNo;
                SeaShenzhenOriginalEntity original;
                if (!originalLookup.TryGetValue(trackingNo, out original))
                {
                    exceptions.Add(CreateExceptionRow(feeMaster, "找不到託運單資料"));
                    continue;
                }

                transferRows.Add(CreateTransferRow(feeMaster, original, dataDate, userId, now));
            }

            int deletedCount;
            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    // 同一資料日期採重轉機制：先刪除既有結果，再整批寫入本次產生的資料。
                    deletedCount = JetfDb.DeleteByColumnValues<ShenzhenFeeMasterEntity, string>(new[] { dataDate }, x => x.DataDate);
                    JetfDb.BulkInsert(transferRows);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return new SeaShenzhenFeeTransferResponse
            {
                DataDate = dataDate,
                SourceCount = feeMasters.Count,
                DeletedCount = deletedCount,
                CreatedCount = transferRows.Count,
                ExceptionCount = exceptions.Count,
                Exceptions = exceptions
            };
        }

        /// <summary>
        /// 使用 EntityFrameworkBulkExtensions 的 WhereBulkContains 批次查詢原始託運資料。
        /// </summary>
        private Dictionary<string, SeaShenzhenOriginalEntity> GetOriginalLookup(IEnumerable<FeeMasterEntity> feeMasters)
        {
            var trackingNos = (feeMasters ?? Enumerable.Empty<FeeMasterEntity>())
                .Select(x => x.TrackingNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (trackingNos.Count == 0)
            {
                return new Dictionary<string, SeaShenzhenOriginalEntity>(StringComparer.OrdinalIgnoreCase);
            }

            var originals = JetfDb.SeaShenzhenOriginals
                .AsNoTracking()
                .WhereBulkContains(JetfDb, trackingNos, x => x.TrackingNo, x => x);

            // 若同一託運單號有多筆原始資料，優先取有託運單號/條碼號的資料排序。
            return originals
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(x => x.JetfSerial)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依 FeeMaster 與原始託運資料建立深圳稅金轉檔資料列。
        /// </summary>
        private static ShenzhenFeeMasterEntity CreateTransferRow(
            FeeMasterEntity feeMaster,
            SeaShenzhenOriginalEntity original,
            string dataDate,
            string userId,
            DateTime now)
        {
            var tax = (feeMaster.Tax1 ?? 0) + (feeMaster.Tax2 ?? 0);
            var cod = ToAmount(original.Cc);
            var fee = string.Equals(original.TaxPayment, "C", StringComparison.OrdinalIgnoreCase)
                ? FeeWhenTaxPaymentC
                : 0;

            return new ShenzhenFeeMasterEntity
            {
                FeeMasterId = feeMaster.Id,
                DataDate = dataDate,
                Customer = feeMaster.Customer,
                TrackingNo = original.TrackingNo,
                DlvInv = original.JetfSerial,
                Tax = tax,
                Cod = cod,
                Fee = fee,
                IncludeTax = original.TaxPayment,
                DlvCom = original.TransName,
                Recipient = original.Importer,
                RecPhone = original.ImporterPhone,
                RecAddress = original.ImporterAddress,
                ToDlvCod = tax + cod + fee,
                CreatedUser = userId,
                CreatedTime = now,
                ModifiedUser = userId,
                ModifiedTime = now
            };
        }

        /// <summary>
        /// 建立無法轉檔的異常資料，回傳給畫面提示使用者。
        /// </summary>
        private static SeaShenzhenFeeTransferExceptionRow CreateExceptionRow(FeeMasterEntity feeMaster, string reason)
        {
            return new SeaShenzhenFeeTransferExceptionRow
            {
                Reason = reason,
                MainNumber = feeMaster.MainNumber,
                TrackingNo = feeMaster.TrackingNo,
                DlvInv = feeMaster.DlvInv,
                Recipient = feeMaster.Recipient,
                RecPhone = feeMaster.RecPhone,
                RecAddress = feeMaster.RecAddress,
                Tax1 = feeMaster.Tax1 ?? 0,
                Tax2 = feeMaster.Tax2 ?? 0
            };
        }

        /// <summary>
        /// 將原始代收金額轉成轉檔表使用的整數金額。
        /// </summary>
        private static int ToAmount(double? value)
        {
            if (!value.HasValue)
            {
                return 0;
            }

            return Convert.ToInt32(Math.Round(value.Value, MidpointRounding.AwayFromZero));
        }
    }
}
