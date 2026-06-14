using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// 匯出轉檔異常明細 Excel。
        /// </summary>
        public byte[] ExportExceptionExcel(SeaShenzhenFeeTransferExceptionExportRequest request)
        {
            var rows = (request?.Exceptions ?? new List<SeaShenzhenFeeTransferExceptionRow>()).ToList();
            if (rows.Count == 0)
            {
                throw new Exception("查無異常明細");
            }

            var workbook = CreateExceptionWorkbook(rows);
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
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
        /// 建立轉檔異常明細 Excel。
        /// </summary>
        private static IWorkbook CreateExceptionWorkbook(IEnumerable<SeaShenzhenFeeTransferExceptionRow> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("轉檔異常明細");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var headers = new[]
            {
                "原因",
                "主號",
                "分提單號",
                "物流貨號",
                "收件人",
                "收件人電話",
                "收件地址",
                "稅金1",
                "稅金2"
            };

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            var rowIndex = 1;
            foreach (var item in rows ?? Enumerable.Empty<SeaShenzhenFeeTransferExceptionRow>())
            {
                var row = sheet.CreateRow(rowIndex++);
                NpoiCell.CreateCell(row, 0, item.Reason, dataStyle);
                NpoiCell.CreateCell(row, 1, item.MainNumber, dataStyle);
                NpoiCell.CreateCell(row, 2, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, 3, item.DlvInv, dataStyle);
                NpoiCell.CreateCell(row, 4, item.Recipient, dataStyle);
                NpoiCell.CreateCell(row, 5, item.RecPhone, dataStyle);
                NpoiCell.CreateCell(row, 6, item.RecAddress, dataStyle);
                NpoiCell.CreateIntCell(row, 7, item.Tax1, dataStyle);
                NpoiCell.CreateIntCell(row, 8, item.Tax2, dataStyle);
            }

            for (var index = 0; index < headers.Length; index++)
            {
                sheet.AutoSizeColumn(index);
                if (sheet.GetColumnWidth(index) < 3000)
                {
                    sheet.SetColumnWidth(index, 3000);
                }
            }

            return workbook;
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
