using Service.Data;
using Service.EnumTax;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞深圳稅金轉檔服務。
    /// </summary>
    public class SeaShenzhenFeeTransferService : _BaseService
    {
        private const string TargetCustomer = "CN00132";

        public SeaShenzhenFeeTransferService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 依資料日期將 FeeMaster 稅金資料轉入深圳轉檔表。
        /// </summary>
        public SeaShenzhenFeeTransferResponse Transfer(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = GetRequiredDataDate(request);

            // 取得指定日期的新遞 FeeMaster 資料，後續只處理 CUSTOMER = CN00132 的資料。
            var feeMasters = JetfDb.FeeMasters
                .AsNoTracking()
                .Where(x => x.DataDate == dataDate && x.Customer == TargetCustomer)
                .OrderBy(x => x.Id)
                .ToList();

            // 先用分提單號批次查出原始託運資料，避免逐筆查詢造成大量 DB round trip。
            var originalLookup = SeaShenzhenFeeTransferShared.GetOriginalLookup(JetfDb, feeMasters.Select(x => x.TrackingNo));
            var now = DateTime.Now;
            var userId = GetUserId();
            var transferRows = new List<ShenzhenFeeMasterEntity>();
            var exceptions = new List<SeaShenzhenFeeTransferExceptionRow>();

            // 逐筆比對 FeeMaster 與原始託運資料，找不到對應資料時列入異常清單。
            foreach (var feeMaster in feeMasters)
            {
                var trackingNo = feeMaster.TrackingNo;
                if (string.IsNullOrWhiteSpace(trackingNo))
                {
                    exceptions.Add(CreateExceptionRow(feeMaster, "找不到託運單資料：分提單號空白"));
                    continue;
                }

                SeaShenzhenOriginalEntity original;
                if (!originalLookup.TryGetValue(trackingNo, out original))
                {
                    exceptions.Add(CreateExceptionRow(feeMaster, "找不到託運單資料"));
                    continue;
                }

                transferRows.Add(SeaShenzhenFeeTransferShared.CreateTransferRow(
                    original,
                    (feeMaster.Tax1 ?? 0) + (feeMaster.Tax2 ?? 0),
                    dataDate,
                    feeMaster.Customer,
                    SeaShenzhenTaxDataType.Jetf,
                    feeMaster.Id,
                    userId,
                    now));
            }

            int deletedCount;
            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    // 舊轉檔流程只覆蓋未標示報關行的資料，避免誤刪同日不同報關行的上傳結果。
                    deletedCount = DeleteExistingTransferRows(dataDate, SeaShenzhenTaxDataType.Jetf);
                    if (transferRows.Count > 0)
                    {
                        JetfDb.BulkInsert(transferRows);
                    }
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
        /// 依條件建立找不到對應託運資料時的異常列。
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
        /// 以批次刪除移除同資料日期、同資料類型的既有轉檔資料。
        /// </summary>
        private int DeleteExistingTransferRows(string dataDate, SeaShenzhenTaxDataType dataType)
        {
            return JetfDb.DeleteWhere(JetfDb.ShenzhenFeeMasters
                .Where(x => x.DataDate == dataDate && x.DataType == dataType));
        }

        /// <summary>
        /// 驗證並取得必要的資料日期參數。
        /// </summary>
        private static string GetRequiredDataDate(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = (request?.DataDate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dataDate))
            {
                throw new Exception("請輸入資料日期");
            }

            return dataDate;
        }
    }
}
