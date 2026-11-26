using Service.Data;
using Service.Extensions;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞託運資料上傳與物流代收檔下載共用的到付款轉入邏輯。
    /// </summary>
    internal static class SeaShenzhenCodTransfer
    {
        /// <summary>
        /// 依原始託運資料建立物流代收資料，僅收取原始代收金額。
        /// </summary>
        public static SeaShenzhenFeeDownloadRow CreateOriginalRow(SeaShenzhenOriginalEntity original, int cod)
        {
            return new SeaShenzhenFeeDownloadRow
            {
                TrackingNo = original.TrackingNo,
                DlvInv = original.JetfSerial,
                Cod = cod,
                Fee = 0,
                ToDlvCod = cod,
                Recipient = original.Importer,
                RecPhone = original.ImporterPhone,
                DlvCom = original.TransName,
                IncludeTax = original.TaxPayment
            };
        }

        /// <summary>
        /// 在呼叫端的 Serializable 交易內，依物流貨號新增到付款資料或更新尚未銷帳的金額。
        /// </summary>
        public static void SaveCollectibleRows(JetfDbContext db, IEnumerable<SeaShenzhenFeeDownloadRow> feeRows, DateTime dataDate)
        {
            var rows = feeRows.Where(x => !string.IsNullOrWhiteSpace(x.DlvInv)).ToList();
            if (rows.Count == 0)
            {
                return;
            }

            var dlvInvs = rows.Select(x => x.DlvInv).ToList();
            var existingRows = db.FeeMasterCods
                .AsNoTracking()
                .WhereBulkContains(db, dlvInvs, x => x.DlvInv, x => x)
                .ToLookup(x => x.DlvInv.TrimEnd(), StringComparer.OrdinalIgnoreCase);
            var inserts = new List<FeeMasterCodEntity>();
            var updates = new List<FeeMasterCodEntity>();
            var now = DateTime.Now;

            foreach (var row in rows)
            {
                var key = row.DlvInv.TrimEnd();
                if (!existingRows.Contains(key))
                {
                    if (row.ToDlvCod == 0)
                    {
                        continue;
                    }

                    inserts.Add(new FeeMasterCodEntity
                    {
                        DataType = "新遞",
                        MainNumber = string.Empty,
                        Customer = row.Customer,
                        BagNumber = row.TrackingNo,
                        TrackingNo = row.TrackingNo,
                        DlvInv = row.DlvInv,
                        OriginalImporter = row.Recipient,
                        Cc = row.ToDlvCod,
                        ToDlvCod = row.ToDlvCod,
                        SignOutTime = dataDate,
                        CreatedTime = now
                    });
                    continue;
                }

                foreach (var target in existingRows[key])
                {
                    if (target.ReceivedCcTime.HasValue || target.ReconciliationLogisticsId.HasValue ||
                        (target.Cc == row.ToDlvCod && target.ToDlvCod == row.ToDlvCod))
                    {
                        continue;
                    }

                    target.Cc = row.ToDlvCod;
                    target.ToDlvCod = row.ToDlvCod;
                    updates.Add(target);
                }
            }

            db.BulkUpdate(updates);
            db.BulkInsert(inserts);
        }
    }
}
