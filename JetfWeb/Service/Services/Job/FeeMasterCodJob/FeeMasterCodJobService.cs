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
    /// 將前三個完整日的空運及海運到付款資料寫入 FEE_MASTER_COD。
    /// </summary>
    public sealed class FeeMasterCodJobService : _BaseService
    {
        private const string JobName = "稅金到付款資料";
        private const string AirSourceType = "AIR";
        private const string SeaSourceType = "SEA";
        private const int CommandTimeoutSeconds = 600;
        private const int BatchSize = 500;
        private const int ProcessingDays = 3;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        /// 依系統時間查詢前三個完整日，每次只處理一天的空運及海運到付款資料。
        /// 已存在 CLEARANCE_TAX 的資料不會寫入。
        /// 空運主號與追蹤號、海運主號與分提單號已存在時不會再次寫入。
        /// </summary>
        /// <returns>非同步排程工作。</returns>
        public async Task RunFeeMasterCodJobAsync()
        {
            var processDate = DateTime.Today.AddDays(-1);

            try
            {
                // 從昨天開始逐日往前處理，每完成一天後再往前一天。
                for (var processedDays = 0; processedDays < ProcessingDays; processedDays++)
                {
                    Logger.Info($"{JobName}開始，處理日期={processDate:yyyy-MM-dd}");
                    await ProcessOneDayAsync(processDate, processDate.AddDays(1));
                    Logger.Info($"{JobName}完成，處理日期={processDate:yyyy-MM-dd}");
                    processDate = processDate.AddDays(-1);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{JobName}失敗，處理日期={processDate:yyyy-MM-dd}");
                WriteJobErrorLog(JobName, ex);
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
            var airQueryRows = await QueryAirRowsAsync(startTime, endTime);
            var airRows = DeduplicateAirRows(airQueryRows);
            SaveAirRows(airRows);
            airQueryRows.Clear();
            airRows.Clear();

            var seaQueryRows = await QuerySeaRowsAsync(startTime, endTime);
            var seaRows = DeduplicateSeaRows(seaQueryRows);
            SaveSeaRows(seaRows);
        }

        /// <summary>
        /// 查詢指定時間區間內尚無稅金資料的空運到付款資料。
        /// </summary>
        /// <param name="startTime">當日起始時間（含）。</param>
        /// <param name="endTime">隔日起始時間（不含）。</param>
        /// <returns>空運來源資料。</returns>
        private async Task<List<FeeMasterCodSourceRow>> QueryAirRowsAsync(DateTime startTime, DateTime endTime)
        {
            const string sql = @"
SELECT c.DATA_TYPE AS DataType,
       o.MAINNUMBER AS MainNumber,
       o.DESPATCHNO AS Customer,
       o.BAGNO AS BagNumber,
       o.TRACKINGNO AS TrackingNo,
       o.DELIVERYNO AS DlvInv,
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
  AND NOT EXISTS
      (
          SELECT 1
          FROM DATA_CENTER.dbo.CLEARANCE_TAX AS tax
          WHERE tax.MAIN_NUMBER = o.MAINNUMBER
            AND tax.BAG_NUMBER = o.BAGNO
      )
  AND NOT EXISTS
      (
          SELECT 1
          FROM DATA_CENTER.dbo.CLEARANCE_TAX AS tax
          WHERE tax.MAIN_NUMBER = o.MAINNUMBER
            AND tax.BAG_NUMBER = o.TRACKINGUB
      )

UNION ALL

SELECT c.DATA_TYPE AS DataType,
       o.MAINNUMBER AS MainNumber,
       o.DESPATCHNO AS Customer,
       o.BAGNO AS BagNumber,
       o.TRACKINGNO AS TrackingNo,
       o.DELIVERYNO AS DlvInv,
       o.CC AS Cc,
       c.SIGN_OUT_TIME AS SignOutTime
FROM DATA_CENTER.dbo.CLEARANCE_INFO AS c
INNER JOIN DATA_CENTER.dbo.ORIGINALLIST AS o
    ON c.MAIN_NUMBER = o.MAINNUMBER
   AND c.MERGE_NUMBER = o.TRACKINGUB
WHERE c.DATA_TYPE IN ('tact', 'ftz')
  AND c.SIGN_OUT_TIME >= @START_TIME
  AND c.SIGN_OUT_TIME < @END_TIME
  AND o.CC > 0
  AND NOT EXISTS
      (
          SELECT 1
          FROM DATA_CENTER.dbo.CLEARANCE_TAX AS tax
          WHERE tax.MAIN_NUMBER = o.MAINNUMBER
            AND tax.BAG_NUMBER = o.BAGNO
      )
  AND NOT EXISTS
      (
          SELECT 1
          FROM DATA_CENTER.dbo.CLEARANCE_TAX AS tax
          WHERE tax.MAIN_NUMBER = o.MAINNUMBER
            AND tax.BAG_NUMBER = o.TRACKINGUB
      );";

            return (await conn.QueryAsync<FeeMasterCodSourceRow>(
                sql,
                new
                {
                    START_TIME = startTime,
                    END_TIME = endTime
                },
                commandTimeout: CommandTimeoutSeconds)).ToList();
        }

        /// <summary>
        /// 查詢指定時間區間內尚無稅金資料的海運到付款資料。
        /// </summary>
        /// <param name="startTime">當日起始時間（含）。</param>
        /// <param name="endTime">隔日起始時間（不含）。</param>
        /// <returns>海運來源資料。</returns>
        private async Task<List<FeeMasterCodSourceRow>> QuerySeaRowsAsync(DateTime startTime, DateTime endTime)
        {
            const string sql = @"
SELECT c.DATA_TYPE AS DataType,
       sea.MAINNUMBER AS MainNumber,
       sea.DESPATCH_NAME AS Customer,
       sea.BL_NO AS BagNumber,
       sea.BL_NO AS TrackingNo,
       sea.JETF_SERIAL AS DlvInv,
       sea.CC AS Cc,
       c.SIGN_OUT_TIME AS SignOutTime
FROM DATA_CENTER.dbo.CLEARANCE_INFO AS c
INNER JOIN DATA_CENTER.dbo.SEA_ORDER_ORIGINAL AS sea
    ON c.MAIN_NUMBER = sea.MAINNUMBER
   AND c.BAG_NUMBER = sea.BL_NO
WHERE c.DATA_TYPE IS NOT NULL
  AND c.DATA_TYPE NOT IN ('tact', 'ftz')
  AND c.SIGN_OUT_TIME >= @START_TIME
  AND c.SIGN_OUT_TIME < @END_TIME
  AND sea.CC > 0
  AND NOT EXISTS
      (
          SELECT 1
          FROM DATA_CENTER.dbo.CLEARANCE_TAX AS tax
          WHERE tax.MAIN_NUMBER = sea.MAINNUMBER
            AND tax.BAG_NUMBER = sea.BL_NO
      );";

            return (await conn.QueryAsync<FeeMasterCodSourceRow>(
                sql,
                new
                {
                    START_TIME = startTime,
                    END_TIME = endTime
                },
                commandTimeout: CommandTimeoutSeconds)).ToList();
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
                x => BuildKey(x.MainNumber, x.BagNumber));
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
        private int SaveAirRows(List<FeeMasterCodSourceRow> rows)
        {
            var existingKeys = LoadExistingAirKeys(rows);
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
        private int SaveSeaRows(List<FeeMasterCodSourceRow> rows)
        {
            var existingKeys = LoadExistingSeaKeys(rows);
            var entities = rows
                .Where(x => !existingKeys.Contains(BuildKey(x.MainNumber, x.BagNumber)))
                .Select(x => CreateEntity(SeaSourceType, x))
                .ToList();

            return SaveEntities(entities);
        }

        /// <summary>
        /// 批次查詢目標表既有的空運主號與追蹤號。
        /// </summary>
        /// <param name="rows">待比對的空運資料。</param>
        /// <returns>既有空運防重鍵集合。</returns>
        private HashSet<string> LoadExistingAirKeys(IEnumerable<FeeMasterCodSourceRow> rows)
        {
            return JetfDb.FeeMasterCods
                .AsNoTracking()
                .Where(x => x.SourceType == AirSourceType)
                .WhereBulkContains(
                    JetfDb,
                    rows,
                    entity => new { entity.MainNumber, entity.TrackingNo },
                    row => new { row.MainNumber, row.TrackingNo })
                .Select(x => BuildKey(x.MainNumber, x.TrackingNo))
                .ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>
        /// 批次查詢目標表既有的海運主號與袋號。
        /// </summary>
        /// <param name="rows">待比對的海運資料。</param>
        /// <returns>既有海運防重鍵集合。</returns>
        private HashSet<string> LoadExistingSeaKeys(IEnumerable<FeeMasterCodSourceRow> rows)
        {
            return JetfDb.FeeMasterCods
                .AsNoTracking()
                .Where(x => x.SourceType == SeaSourceType)
                .WhereBulkContains(
                    JetfDb,
                    rows,
                    entity => new { entity.MainNumber, entity.BagNumber },
                    row => new { row.MainNumber, row.BagNumber })
                .Select(x => BuildKey(x.MainNumber, x.BagNumber))
                .ToHashSet(StringComparer.Ordinal);
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
                Customer = row.Customer,
                BagNumber = row.BagNumber,
                TrackingNo = row.TrackingNo,
                DlvInv = row.DlvInv,
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

            var bagComparison = StringComparer.OrdinalIgnoreCase.Compare(candidate.BagNumber, current.BagNumber);
            if (bagComparison != 0)
            {
                return bagComparison < 0;
            }

            var serialComparison = StringComparer.OrdinalIgnoreCase.Compare(candidate.DlvInv, current.DlvInv);
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
                && !string.IsNullOrWhiteSpace(row.BagNumber)
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
                && !string.IsNullOrWhiteSpace(row.BagNumber)
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

    }
}
