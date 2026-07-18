using Dapper;
using NLog;
using Service.Data;
using Service.Services.Job.FeeMasterCodJob.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services.Job.FeeMasterCodJob
{
    /// <summary>
    /// 將前兩個完整日的空運及海運到付款資料寫入 FEE_MASTER_COD。
    /// </summary>
    public sealed class FeeMasterCodJobService : _BaseService
    {
        private const string JobName = "稅金到付款資料";
        private const string AirSourceType = "AIR";
        private const string SeaSourceType = "SEA";
        private const int CommandTimeoutSeconds = 600;
        private const int BatchSize = 500;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string AirQuerySql = @"
SELECT LOWER(c.DATA_TYPE) AS DataType,
       o.MAINNUMBER AS MainNumber,
       o.BAGNO AS BagNo,
       o.TRACKINGNO AS TrackingNo,
       o.DELIVERYNO AS JetfSerial,
       o.CC AS Cc,
       c.SIGN_OUT_TIME AS SignOutTime
FROM DATA_CENTER.dbo.CLEARANCE_INFO AS c
INNER JOIN DATA_CENTER.dbo.ORIGINALLIST AS o
    ON c.MAIN_NUMBER = o.MAINNUMBER
   AND c.BAG_NUMBER = o.BAGNO
WHERE c.DATA_TYPE IN ('tact', 'ftz')
  AND c.SIGN_OUT_TIME >= @START_TIME
  AND c.SIGN_OUT_TIME < @END_TIME
  AND o.CC > 0

UNION ALL

SELECT LOWER(c.DATA_TYPE) AS DataType,
       o.MAINNUMBER AS MainNumber,
       o.BAGNO AS BagNo,
       o.TRACKINGNO AS TrackingNo,
       o.DELIVERYNO AS JetfSerial,
       o.CC AS Cc,
       c.SIGN_OUT_TIME AS SignOutTime
FROM DATA_CENTER.dbo.CLEARANCE_INFO AS c
INNER JOIN DATA_CENTER.dbo.ORIGINALLIST AS o
    ON c.MAIN_NUMBER = o.MAINNUMBER
   AND c.MERGE_NUMBER = o.TRACKINGUB
WHERE c.DATA_TYPE IN ('tact', 'ftz')
  AND c.SIGN_OUT_TIME >= @START_TIME
  AND c.SIGN_OUT_TIME < @END_TIME
  AND o.CC > 0;";

        private const string SeaQuerySql = @"
SELECT LOWER(c.DATA_TYPE) AS DataType,
       sea.MAINNUMBER AS MainNumber,
       sea.BL_NO AS BagNo,
       sea.BL_NO AS TrackingNo,
       sea.JETF_SERIAL AS JetfSerial,
       CONVERT(decimal(18, 2), sea.CC) AS Cc,
       c.SIGN_OUT_TIME AS SignOutTime
FROM DATA_CENTER.dbo.CLEARANCE_INFO AS c
INNER JOIN DATA_CENTER.dbo.SEA_ORDER_ORIGINAL AS sea
    ON c.MAIN_NUMBER = sea.MAINNUMBER
   AND c.BAG_NUMBER = sea.BL_NO
WHERE c.DATA_TYPE IS NOT NULL
  AND c.DATA_TYPE NOT IN ('tact', 'ftz')
  AND c.SIGN_OUT_TIME >= @START_TIME
  AND c.SIGN_OUT_TIME < @END_TIME
  AND sea.CC > 0;";

        /// <summary>
        /// 初始化 FEE_MASTER_COD 到付款資料彙整排程。
        /// </summary>
        /// <param name="jetfDbContext">JETF 主資料庫內容。</param>
        /// <param name="dataCenterDbContext">DATA_CENTER 資料庫內容。</param>
        public FeeMasterCodJobService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
            JetfDb.Database.CommandTimeout = CommandTimeoutSeconds;
        }

        /// <summary>
        /// 依系統時間查詢前兩個完整日，每次只處理一天的空運及海運到付款資料。
        /// 空運主號與追蹤號、海運主號與分提單號已存在時不會再次寫入。
        /// </summary>
        /// <returns>非同步排程工作。</returns>
        public async Task RunFeeMasterCodJobAsync()
        {
            var endTime = DateTime.Now.Date;
            var startTime = endTime.AddDays(-2);

            try
            {
                for (var dayOffset = 1; dayOffset <= 2; dayOffset++)
                {
                    var currentStartTime = endTime.AddDays(-dayOffset);
                    var currentEndTime = currentStartTime.AddDays(1);
                    await ProcessOneDayAsync(currentStartTime, currentEndTime);
                }

                Logger.Info(
                    $"{JobName}全部完成，區間={startTime:yyyy-MM-dd HH:mm:ss}~{endTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    ex,
                    $"{JobName}失敗，區間={startTime:yyyy-MM-dd HH:mm:ss}~{endTime:yyyy-MM-dd HH:mm:ss}");
                WriteJobErrorLog(JobName, ex);
                throw;
            }
        }

        /// <summary>
        /// 查詢並寫入單一完整日的空運及海運到付款資料。
        /// </summary>
        /// <param name="startTime">當日起始時間（含）。</param>
        /// <param name="endTime">隔日起始時間（不含）。</param>
        /// <returns>非同步處理工作。</returns>
        private async Task ProcessOneDayAsync(DateTime startTime, DateTime endTime)
        {
            var parameters = new
            {
                START_TIME = startTime,
                END_TIME = endTime
            };

            var airQueryRows = (await conn.QueryAsync<FeeMasterCodSourceRow>(
                AirQuerySql,
                parameters,
                commandTimeout: CommandTimeoutSeconds)).ToList();
            var airRows = DeduplicateAirRows(airQueryRows);
            var airQueryCount = airQueryRows.Count;
            var airDeduplicatedCount = airRows.Count;
            var airInsertedCount = await SaveAirRowsAsync(airRows);
            airQueryRows.Clear();
            airRows.Clear();

            var seaQueryRows = (await conn.QueryAsync<FeeMasterCodSourceRow>(
                SeaQuerySql,
                parameters,
                commandTimeout: CommandTimeoutSeconds)).ToList();
            var seaRows = DeduplicateSeaRows(seaQueryRows);
            var seaQueryCount = seaQueryRows.Count;
            var seaDeduplicatedCount = seaRows.Count;
            var seaInsertedCount = await SaveSeaRowsAsync(seaRows);

            Logger.Info(
                $"{JobName}單日完成，區間={startTime:yyyy-MM-dd HH:mm:ss}~{endTime:yyyy-MM-dd HH:mm:ss}，" +
                $"空運查詢={airQueryCount}，空運去重={airDeduplicatedCount}，空運新增={airInsertedCount}，" +
                $"海運查詢={seaQueryCount}，海運去重={seaDeduplicatedCount}，海運新增={seaInsertedCount}");
        }

        /// <summary>
        /// 依主號與追蹤號去除空運重複資料，保留出倉時間最新的一筆。
        /// </summary>
        /// <param name="rows">空運來源資料。</param>
        /// <returns>去重後的空運資料。</returns>
        private static List<FeeMasterCodSourceRow> DeduplicateAirRows(IEnumerable<FeeMasterCodSourceRow> rows)
        {
            return DeduplicateRows(
                rows,
                IsValidAirRow,
                x => BuildKey(x.MainNumber, x.TrackingNo));
        }

        /// <summary>
        /// 依主號與袋號去除海運重複資料，保留出倉時間最新的一筆。
        /// </summary>
        /// <param name="rows">海運來源資料。</param>
        /// <returns>去重後的海運資料。</returns>
        private static List<FeeMasterCodSourceRow> DeduplicateSeaRows(IEnumerable<FeeMasterCodSourceRow> rows)
        {
            return DeduplicateRows(
                rows,
                IsValidSeaRow,
                x => BuildKey(x.MainNumber, x.BagNo));
        }

        /// <summary>
        /// 以 Dictionary 單次掃描來源資料，依業務鍵保留較新的資料。
        /// </summary>
        /// <param name="rows">來源資料。</param>
        /// <param name="isValid">資料有效性判斷。</param>
        /// <param name="keySelector">業務鍵選擇器。</param>
        /// <returns>去重後資料。</returns>
        private static List<FeeMasterCodSourceRow> DeduplicateRows(
            IEnumerable<FeeMasterCodSourceRow> rows,
            Func<FeeMasterCodSourceRow, bool> isValid,
            Func<FeeMasterCodSourceRow, string> keySelector)
        {
            var lookup = new Dictionary<string, FeeMasterCodSourceRow>(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                if (!isValid(row))
                {
                    continue;
                }

                var key = keySelector(row);
                if (!lookup.TryGetValue(key, out var currentRow) || IsPreferredRow(row, currentRow))
                {
                    lookup[key] = row;
                }
            }

            return lookup.Values.ToList();
        }

        /// <summary>
        /// 儲存尚未存在的空運資料。
        /// </summary>
        /// <param name="rows">已完成記憶體去重的空運資料。</param>
        /// <returns>實際新增筆數。</returns>
        private async Task<int> SaveAirRowsAsync(List<FeeMasterCodSourceRow> rows)
        {
            var existingKeys = await LoadExistingAirKeysAsync(rows);
            var entities = rows
                .Where(x => !existingKeys.Contains(BuildKey(x.MainNumber, x.TrackingNo)))
                .Select(x => CreateEntity(AirSourceType, x))
                .ToList();

            return SaveEntities(entities);
        }

        /// <summary>
        /// 儲存尚未存在的海運資料。
        /// </summary>
        /// <param name="rows">已完成記憶體去重的海運資料。</param>
        /// <returns>實際新增筆數。</returns>
        private async Task<int> SaveSeaRowsAsync(List<FeeMasterCodSourceRow> rows)
        {
            var existingKeys = await LoadExistingSeaKeysAsync(rows);
            var entities = rows
                .Where(x => !existingKeys.Contains(BuildKey(x.MainNumber, x.BagNo)))
                .Select(x => CreateEntity(SeaSourceType, x))
                .ToList();

            return SaveEntities(entities);
        }

        /// <summary>
        /// 分批查詢目標表既有的空運主號與追蹤號。
        /// </summary>
        /// <param name="rows">待比對的空運資料。</param>
        /// <returns>既有空運防重鍵集合。</returns>
        private async Task<HashSet<string>> LoadExistingAirKeysAsync(IEnumerable<FeeMasterCodSourceRow> rows)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var mainNumbers = rows
                .Select(x => x.MainNumber)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var batch in Batch(mainNumbers, BatchSize))
            {
                var currentBatch = batch.ToList();
                var existingRows = await JetfDb.FeeMasterCods
                    .AsNoTracking()
                    .Where(x => x.SourceType == AirSourceType && currentBatch.Contains(x.MainNumber))
                    .Select(x => new
                    {
                        x.MainNumber,
                        x.TrackingNo
                    })
                    .ToListAsync();

                foreach (var existingRow in existingRows)
                {
                    result.Add(BuildKey(existingRow.MainNumber, existingRow.TrackingNo));
                }
            }

            return result;
        }

        /// <summary>
        /// 分批查詢目標表既有的海運主號與袋號。
        /// </summary>
        /// <param name="rows">待比對的海運資料。</param>
        /// <returns>既有海運防重鍵集合。</returns>
        private async Task<HashSet<string>> LoadExistingSeaKeysAsync(IEnumerable<FeeMasterCodSourceRow> rows)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var mainNumbers = rows
                .Select(x => x.MainNumber)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var batch in Batch(mainNumbers, BatchSize))
            {
                var currentBatch = batch.ToList();
                var existingRows = await JetfDb.FeeMasterCods
                    .AsNoTracking()
                    .Where(x => x.SourceType == SeaSourceType && currentBatch.Contains(x.MainNumber))
                    .Select(x => new
                    {
                        x.MainNumber,
                        x.BagNo
                    })
                    .ToListAsync();

                foreach (var existingRow in existingRows)
                {
                    result.Add(BuildKey(existingRow.MainNumber, existingRow.BagNo));
                }
            }

            return result;
        }

        /// <summary>
        /// 使用 EntityFrameworkBulkExtensions 一次新增 FEE_MASTER_COD。
        /// </summary>
        /// <param name="entities">待新增資料。</param>
        /// <returns>實際新增筆數。</returns>
        private int SaveEntities(List<FeeMasterCodEntity> entities)
        {
            if (!entities.Any())
            {
                return 0;
            }

            JetfDb.BulkInsert(
                entities,
                options =>
                {
                    options.BatchSize = BatchSize;
                    options.TimeoutSeconds = CommandTimeoutSeconds;
                });

            return entities.Count;
        }

        /// <summary>
        /// 建立目標表 Entity。
        /// </summary>
        /// <param name="sourceType">AIR 或 SEA。</param>
        /// <param name="row">來源資料。</param>
        /// <returns>待寫入的 FEE_MASTER_COD Entity。</returns>
        private static FeeMasterCodEntity CreateEntity(string sourceType, FeeMasterCodSourceRow row)
        {
            return new FeeMasterCodEntity
            {
                SourceType = sourceType,
                DataType = row.DataType,
                MainNumber = row.MainNumber,
                BagNo = row.BagNo,
                TrackingNo = row.TrackingNo,
                JetfSerial = row.JetfSerial,
                Cc = row.Cc,
                SignOutTime = row.SignOutTime,
                CreatedTime = DateTime.Now
            };
        }

        /// <summary>
        /// 判斷候選資料是否應取代同一業務鍵目前保留的資料。
        /// </summary>
        /// <param name="candidate">候選資料。</param>
        /// <param name="current">目前保留資料。</param>
        /// <returns>候選資料是否較優先。</returns>
        private static bool IsPreferredRow(FeeMasterCodSourceRow candidate, FeeMasterCodSourceRow current)
        {
            if (candidate.SignOutTime != current.SignOutTime)
            {
                return candidate.SignOutTime > current.SignOutTime;
            }

            var bagComparison = StringComparer.OrdinalIgnoreCase.Compare(candidate.BagNo, current.BagNo);
            if (bagComparison != 0)
            {
                return bagComparison < 0;
            }

            var serialComparison = StringComparer.OrdinalIgnoreCase.Compare(candidate.JetfSerial, current.JetfSerial);
            if (serialComparison != 0)
            {
                return serialComparison < 0;
            }

            return candidate.Cc > current.Cc;
        }

        /// <summary>
        /// 檢查空運來源資料是否具備可寫入欄位。
        /// </summary>
        /// <param name="row">空運來源資料。</param>
        /// <returns>是否為有效資料。</returns>
        private static bool IsValidAirRow(FeeMasterCodSourceRow row)
        {
            return row != null
                && !string.IsNullOrWhiteSpace(row.MainNumber)
                && !string.IsNullOrWhiteSpace(row.BagNo)
                && !string.IsNullOrWhiteSpace(row.TrackingNo)
                && row.Cc > 0;
        }

        /// <summary>
        /// 檢查海運來源資料是否具備可寫入欄位。
        /// </summary>
        /// <param name="row">海運來源資料。</param>
        /// <returns>是否為有效資料。</returns>
        private static bool IsValidSeaRow(FeeMasterCodSourceRow row)
        {
            return row != null
                && !string.IsNullOrWhiteSpace(row.MainNumber)
                && !string.IsNullOrWhiteSpace(row.BagNo)
                && row.Cc > 0;
        }

        /// <summary>
        /// 建立不受大小寫及來源尾端空白影響的複合防重鍵。
        /// </summary>
        /// <param name="first">第一段欄位。</param>
        /// <param name="second">第二段欄位。</param>
        /// <returns>複合防重鍵。</returns>
        private static string BuildKey(string first, string second)
        {
            return NormalizeKeyPart(first) + "\u001F" + NormalizeKeyPart(second);
        }

        /// <summary>
        /// 正規化防重鍵的單一欄位。
        /// </summary>
        /// <param name="value">原始欄位值。</param>
        /// <returns>正規化後的欄位值。</returns>
        private static string NormalizeKeyPart(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        /// <summary>
        /// 將資料分割成固定筆數的批次。
        /// </summary>
        /// <typeparam name="T">資料型別。</typeparam>
        /// <param name="source">來源資料。</param>
        /// <param name="size">每批筆數。</param>
        /// <returns>批次資料。</returns>
        private static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> source, int size)
        {
            var batch = new List<T>(size);

            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count < size)
                {
                    continue;
                }

                yield return batch;
                batch = new List<T>(size);
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}
