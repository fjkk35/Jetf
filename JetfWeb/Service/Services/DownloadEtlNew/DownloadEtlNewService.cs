using Microsoft.VisualBasic;
using NLog;
using Service.Data;
using Service.Models;
using Service.Services.SeaTaxUpload;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace Service.Services.DownloadEtlNew
{
    public class DownloadEtlNewService : _BaseService
    {
        private const string AirSourceType = "3";
        private const string TactSource = "tact";
        private const string FtzSource = "ftz";
        private const int BatchSize = 500;
        private const int CommandTimeoutSeconds = 600;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 初始化物流代收檔下載服務。
        /// </summary>
        /// <param name="jetfDbContext">JETF 主資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public DownloadEtlNewService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 依指定日期區間重算空運代收資料，並回寫 FEE_MASTER_TEST。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="timeBetween">畫面選擇的時間區間代碼。</param>
        /// <param name="sTime">開始時間。</param>
        /// <param name="eTime">結束時間。</param>
        /// <param name="userId">操作人員帳號。</param>
        /// <returns>匯入結果。</returns>
        public ResponseModel UploadEtl(string date, string timeBetween, string sTime, string eTime, string userId)
        {
            var responseModel = new ResponseModel();

            // step1: 先將畫面輸入轉成實際查詢區間。
            // 時間格式錯誤時維持既有回傳內容，避免前端判斷行為被改變。
            if (!TryBuildDateRange(date, timeBetween, sTime, eTime, out var startDate, out var endDate, out var dataDate, out var errorMessage))
            {
                responseModel.status = Status.error;
                responseModel.msg = errorMessage;
                return responseModel;
            }

            // step2: 保留舊流程的時間檢查結果與錯誤訊息。
            if (endDate <= startDate)
            {
                responseModel.status = Status.error;
                responseModel.msg = "結束時間大於開始時間，請確認";
                return responseModel;
            }

            try
            {
                // step3: 本流程會跨資料庫查詢大量清關、稅單與原始單資料，先拉長 timeout。
                ConfigureCommandTimeout();

                // step4: 此段為既有流程保留但目前註解停用；不主動恢復，避免改變目前上線行為。
                // 先同步更新菜鳥稅金調整結果，確保後續組出的代收資料與既有流程一致。
                //responseModel = UpdateCainiaoTaxEdit();
                //if (responseModel.status != Status.success)
                //{
                //    return responseModel;
                //}

                // step5: 依清關、稅單、原始單與客戶設定組出主檔與明細草稿。
                var feeMasterDrafts = BuildFeeMasterDrafts(startDate, endDate);

                // step6: 將草稿寫回 FEE_MASTER_TEST，並同步重建 FEE_MASTER_DETAIL 明細。
                SaveFeeMasters(feeMasterDrafts, dataDate);
                responseModel.status = Status.success;
            }
            catch (Exception ex)
            {
                // step7: 維持既有錯誤處理方式，直接回傳例外訊息給前端。
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return responseModel;
        }

        /// <summary>
        /// 統一設定這次上傳流程會用到的資料庫 CommandTimeout。
        /// </summary>
        private void ConfigureCommandTimeout()
        {
            JetfDb.Database.CommandTimeout = CommandTimeoutSeconds;
            DataCenterDb.Database.CommandTimeout = CommandTimeoutSeconds;
        }

        /// <summary>
        /// 取得空運物流代收下載報表資料。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="timeBetween">畫面選擇的時間區間代碼。</param>
        /// <param name="sTime">開始時間。</param>
        /// <param name="eTime">結束時間。</param>
        /// <param name="company">欲輸出的物流公司。</param>
        /// <param name="includeTax">代收類型。</param>
        /// <returns>報表資料與執行結果。</returns>
        public DownloadEtlNewReportResult GetEtlReport(string date, string timeBetween, string sTime, string eTime, string company, string includeTax)
        {
            var result = new DownloadEtlNewReportResult();

            try
            {
                JetfDb.Database.CommandTimeout = CommandTimeoutSeconds;
                DataCenterDb.Database.CommandTimeout = CommandTimeoutSeconds;
                if (!TryBuildDateRange(date, timeBetween, sTime, eTime, out var startDate, out var endDate, out _, out var errorMessage))
                {
                    result.status = Status.error;
                    result.msg = errorMessage;
                    return result;
                }

                // 先以 fee master 當作報表底稿，後續再依代收類型與物流公司條件做篩選。
                var feeMasters = JetfDb.FeeMasterTests
                    .AsNoTracking()
                    .Where(x =>
                        (x.Source == TactSource || x.Source == FtzSource) &&
                        x.SourceType == AirSourceType &&
                        x.OutDateTime.HasValue &&
                        x.OutDateTime.Value >= startDate &&
                        x.OutDateTime.Value <= endDate)
                    .Select(x => new
                    {
                        x.BagNumber,
                        x.TrackingNo,
                        x.Tax1,
                        x.Tax2,
                        x.Fee,
                        x.Cod,
                        x.ToDlvCod,
                        x.Recipient,
                        x.RecPhone,
                        x.DlvInv,
                        x.IncludeTax,
                        x.Customer,
                        x.DlvCom,
                        x.OutDateTime
                    })
                    .ToList()
                    .Select(x => new DownloadEtlNewReportItem
                    {
                        BagNumber = x.BagNumber,
                        TrackingNo = x.TrackingNo,
                        Tax1 = x.Tax1 ?? 0,
                        Tax2 = x.Tax2 ?? 0,
                        Fee = x.Fee ?? 0,
                        Cod = x.Cod ?? 0,
                        ToDlvCod = ToInt(x.ToDlvCod),
                        Recipient = x.Recipient,
                        RecPhone = x.RecPhone,
                        DlvInv = x.DlvInv,
                        IncludeTax = x.IncludeTax,
                        Customer = x.Customer,
                        TransNo = x.DlvCom,
                        OutDateTime = x.OutDateTime,
                        TransName = x.DlvCom
                    })
                    .ToList();

                IEnumerable<DownloadEtlNewReportItem> filteredRows;

                if (string.IsNullOrEmpty(includeTax))
                {
                    // 無客戶資料檔只保留尚未判定 INCLUDE_TAX 的資料。
                    filteredRows = feeMasters.Where(x => string.IsNullOrEmpty(x.IncludeTax));
                }
                else if (includeTax == "D" || includeTax == "C")
                {
                    // 特殊 D/C 檔不依物流公司拆檔，但仍需回填物流資訊供報表顯示。
                    var customerLookup = BuildReportCustomerLookup(feeMasters.Where(x => x.IncludeTax == includeTax));
                    filteredRows = feeMasters
                        .Where(x => x.IncludeTax == includeTax)
                        .Select(x => ApplyReportCompanyInfo(x, customerLookup));
                }
                else
                {
                    var customerLookup = BuildReportCustomerLookup(feeMasters.Where(x => x.IncludeTax == includeTax));

                    // 舊頁一般檔會依物流公司過濾；21:00-22:00 這個區間另外把新瑞宅配一併併入輸出。
                    filteredRows = feeMasters
                        .Where(x => x.IncludeTax == includeTax)
                        .Select(x => ApplyReportCompanyInfo(x, customerLookup))
                        .Where(x =>
                            x.Company == company ||
                            (timeBetween == "3" && x.Company == "新瑞宅配"));
                }

                result.Rows = filteredRows.ToList();
                result.status = Status.success;
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 執行菜鳥稅金調整 stored procedure。
        /// </summary>
        /// <returns>執行結果。</returns>
        private ResponseModel UpdateCainiaoTaxEdit()
        {
            var response = new ResponseModel();

            // 沿用既有 stored procedure，但直接用 EF 的 typed query 取回結果，不再透過 DataTable。
            var queryResult = JetfDb.Database.SqlQuery<StoredProcedureStatusRow>("EXEC [jetf].[dbo].[USP_Update_CainiaoTaxEdit]").FirstOrDefault();
            if (queryResult == null)
            {
                response.status = Status.error;
                response.msg = "USP_Update_CainiaoTaxEdit 未回傳結果";
                return response;
            }

            response.status = queryResult.Status;
            response.msg = queryResult.Message;
            return response;
        }

        /// <summary>
        /// 依指定時間區間建立待寫入 FEE_MASTER 的草稿資料。
        /// </summary>
        /// <param name="startDate">查詢起始時間。</param>
        /// <param name="endDate">查詢結束時間。</param>
        /// <returns>Fee master 草稿清單。</returns>
        private List<FeeMasterDraft> BuildFeeMasterDrafts(DateTime startDate, DateTime endDate)
        {
            // step1: 先把 tact/ftz 來源整理成同一種 CombinedRow，後續計算才能共用。
            var sourceRows = GetCombinedRows(startDate, endDate);

            // step2: 特殊客戶電話會影響 D 類稅金判斷，先一次載入避免每筆查詢 DB。
            var specialPhones = GetSpecialPhoneSet();
            var drafts = new List<FeeMasterDraft>();

            // step3: 舊邏輯以 trackingno 分組，最新出倉的一筆作為主資料，其餘相同 tracking 的稅額累加到 tax2。
            foreach (var group in sourceRows.GroupBy(x => x.TrackingNo ?? string.Empty))
            {
                var orderedRows = group
                    .OrderByDescending(x => x.SignOutTime ?? DateTime.MinValue)
                    .ToList();

                if (!orderedRows.Any())
                {
                    continue;
                }

                var latestRow = orderedRows[0];
                var draft = CreateFeeMasterDraft(latestRow, orderedRows);

                // step4: 套用舊系統稅金邏輯，計算 customer_cod、trans_cod、to_dlv_cod 等欄位。
                ApplyTaxRule(draft, latestRow, specialPhones);

                // step5: 主檔只保留彙總金額，明細仍要保留同 tracking 下每一張稅單。
                draft.DetailRows = CreateFeeMasterDetailRows(orderedRows, specialPhones);
                drafts.Add(draft);
            }

            return drafts;
        }

        /// <summary>
        /// 將同一 tracking 的來源資料轉成 FEE_MASTER_TEST 草稿。
        /// </summary>
        /// <param name="latestRow">同一 tracking 最新出倉的來源資料。</param>
        /// <param name="orderedRows">同一 tracking 依出倉時間排序後的來源資料。</param>
        /// <returns>主檔草稿。</returns>
        private static FeeMasterDraft CreateFeeMasterDraft(CombinedRow latestRow, List<CombinedRow> orderedRows)
        {
            return new FeeMasterDraft
            {
                Source = latestRow.DataType,
                Type = latestRow.ClearanceType,
                Customer = latestRow.DespatchNo,
                MainNumber = latestRow.MainNumber,
                TrackingNo = latestRow.TrackingNo,
                ClearanceNumber = latestRow.ClearanceNumber,
                BagNumber = latestRow.BagNo,
                TaxNumber = latestRow.TaxNumber,
                TaxPayer = latestRow.TaxPayer,
                TaxRecId = latestRow.TaxRecId,
                DlvInv = latestRow.DeliveryNo,
                InDate = latestRow.SignInTime?.ToString("yyyyMMdd"),
                InDateTime = latestRow.SignInTime,
                OutDateTime = latestRow.SignOutTime,
                TaxBase = latestRow.TaxBase,
                Tax1 = ToInt(latestRow.TaxAmount),
                Tax2 = orderedRows.Skip(1).Sum(x => ToInt(x.TaxAmount)),
                Cod = ToInt(latestRow.Cc),
                Fee = latestRow.CodFee ?? 0,
                IncludeTax = latestRow.IncludeTax,
                Recipient = latestRow.Recipient,
                RecPhone = ToNarrowPhone(latestRow.RecPhone),
                RecAddress = latestRow.RecAddress,
                RecId = latestRow.RecId,
                DlvCom = latestRow.TransTaxPayment,
                Arrival = latestRow.Ecm,
                Combine = orderedRows.Count > 1 ? "Y" : string.Empty
            };
        }

        /// <summary>
        /// 合併 tact 與 ftz 兩種來源資料。
        /// </summary>
        /// <param name="startDate">查詢起始時間。</param>
        /// <param name="endDate">查詢結束時間。</param>
        /// <returns>合併後的來源資料。</returns>
        private List<CombinedRow> GetCombinedRows(DateTime startDate, DateTime endDate)
        {
            var rows = new List<CombinedRow>();
            rows.AddRange(GetCombinedRowsBySource(TactSource, startDate, endDate));
            rows.AddRange(GetCombinedRowsBySource(FtzSource, startDate, endDate));
            return rows;
        }

        /// <summary>
        /// 依指定資料來源組出清關、稅單、原始單與客戶設定的合併資料。
        /// </summary>
        /// <param name="dataType">資料來源代碼。</param>
        /// <param name="startDate">查詢起始時間。</param>
        /// <param name="endDate">查詢結束時間。</param>
        /// <returns>單一來源的合併結果。</returns>
        private List<CombinedRow> GetCombinedRowsBySource(string dataType, DateTime startDate, DateTime endDate)
        {
            void LogStep(string step, string status, string content)
            {
                Logger.Debug($"{step} {status}: {content}");
            }

            // step 1: 先抓出這個時間區間內已出倉的清關資料，作為本次處理的主集合。
            LogStep("step 1", "開始", "取得 ClearanceInfo 資料");
            var clearanceQuery = BuildClearanceQuery(dataType, startDate, endDate);
            var clearances = clearanceQuery.ToList();
            LogStep("step 1", "結束", $"取得 ClearanceInfo 資料，筆數={clearances.Count}");

            if (!clearances.Any())
            {
                return new List<CombinedRow>();
            }

            // step 2: 從清關資料整理主提單號與袋號，作為後續稅單、原始單查詢條件。
            LogStep("step 2", "開始", "整理主提單號與候選袋號");
            var mainNumbers = clearances
                .Select(x => x.MainNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var candidateBagNumbers = clearances
                .SelectMany(x => new[] { x.BagNumber, x.MergeNumber })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            LogStep("step 2", "結束", $"整理主提單號與候選袋號，主號筆數={mainNumbers.Count}，袋號筆數={candidateBagNumbers.Count}");

            // step 3: 依來源分開查詢稅單資料，再轉成 dictionary，避免在 SQL 端做過大的 join。
            LogStep("step 3", "開始", "取得稅單資料並建立 taxLookup");
            var taxes = dataType == TactSource
                ? GetTactTaxes(mainNumbers, candidateBagNumbers)
                : GetFtzTaxes(mainNumbers, candidateBagNumbers);

            var taxLookup = taxes
                .GroupBy(x => BuildCompositeKey(x.MainNumber, x.BagNumber))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.TaxNumber).ThenBy(y => y.BagNumber).ToList());
            LogStep("step 3", "結束", $"取得稅單資料並建立 taxLookup，稅單筆數={taxes.Count}，taxLookup 筆數={taxLookup.Count}");

            // step 4: 直接沿用同一份清關條件撈出原始單，避免重複維護查詢條件。
            LogStep("step 4", "開始", "取得 OriginalList 資料並建立袋號與 TrackingUb 對照");
            var originals = GetOriginalLists(dataType, clearanceQuery);
            var originalByBag = originals
                .Where(x => !string.IsNullOrWhiteSpace(x.BagNo))
                .GroupBy(x => BuildCompositeKey(x.MainNumber, x.BagNo))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.TrackingNo).ThenBy(y => y.Id).ToList());

            var originalByTrackingUb = originals
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingUb))
                .GroupBy(x => BuildCompositeKey(x.MainNumber, x.TrackingUb))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.TrackingNo).ThenBy(y => y.Id).ToList());
            LogStep("step 4", "結束", $"取得 OriginalList 資料並建立對照，原始單筆數={originals.Count}，BagNo 對照筆數={originalByBag.Count}，TrackingUb 對照筆數={originalByTrackingUb.Count}");

            // step 5: 批次查出可能會用到的客戶主檔，後續用 join 對應原始單。
            LogStep("step 5", "開始", "查詢客戶主檔資料");
            var customerMasters = LoadCustomerMasters(originals);
            LogStep("step 5", "結束", $"查詢客戶主檔資料，筆數={customerMasters.Count}");

            // step 6: 逐筆把清關、稅單、原始單組成完整資料，最後依 TAX_NUMBER 去重。
            LogStep("step 6", "開始", "組合 CombinedRow 並依 TAX_NUMBER 去重");
            var result = new List<CombinedRow>();

            foreach (var clearance in clearances)
            {
                // 先以 bag number 比對稅單，找不到時退回 merge number，與舊 SQL 的 join 順序一致。
                var matchedTaxes = GetMatchedTaxes(taxLookup, clearance.MainNumber, clearance.BagNumber, clearance.MergeNumber);
                foreach (var tax in matchedTaxes)
                {
                    if (string.IsNullOrWhiteSpace(tax.TaxNumber))
                    {
                        continue;
                    }

                    var original = FindBestOriginal(originalByBag, originalByTrackingUb, clearance.MainNumber, tax.BagNumber);
                    var customer = FindCustomerMaster(original, customerMasters);

                    // 依舊邏輯把來源資料攤平成 fee master 前置資料，缺值時保留 fallback 欄位。
                    result.Add(new CombinedRow
                    {
                        DataType = clearance.DataType,
                        ClearanceType = clearance.ClearanceType,
                        ClearanceNumber = clearance.ClearanceNumber,
                        SignInTime = clearance.SignInTime,
                        SignOutTime = clearance.SignOutTime,
                        TaxNumber = tax.TaxNumber,
                        BagNumber = tax.BagNumber,
                        MainNumber = tax.MainNumber,
                        TaxAmount = tax.TaxAmount,
                        TaxBase = tax.TaxBase,
                        TaxPayer = tax.TaxPayer,
                        TaxRecId = tax.TaxRecId,
                        Ecm = original?.Ecm,
                        BagNo = original?.BagNo ?? clearance.BagNumber,
                        DespatchNo = original?.DespatchNo,
                        Cc = original?.Cc,
                        Recipient = original?.Recipient,
                        RecPhone = original?.RecPhone,
                        RecAddress = original?.RecAddress,
                        RecId = original?.RecId,
                        TrackingNo = original?.TrackingNo ?? tax.BagNumber,
                        DeliveryNo = original?.DeliveryNo,
                        TransTaxPayment = original?.TransTaxPayment,
                        IncludeTax = customer?.IncludeTax,
                        CodFee = customer?.CodFee,
                        Company = customer?.Company,
                        IsCainiaoP = customer?.IsCainiaoP ?? false
                    });
                }
            }

            // 舊 SQL 透過 ROW_NUMBER 針對 TAX_NUMBER 去重，這裡保留每張稅單排序後的第一筆資料。
            var dedupedRows = result
                .GroupBy(x => x.TaxNumber)
                .Select(x => x.OrderBy(y => y.TrackingNo ?? y.BagNumber).First())
                .ToList();
            LogStep("step 6", "結束", $"組合 CombinedRow 並依 TAX_NUMBER 去重，原始筆數={result.Count}，去重後筆數={dedupedRows.Count}");

            return dedupedRows;
        }

        /// <summary>
        /// 建立清關資料的共用查詢條件。
        /// </summary>
        /// <param name="dataType">資料來源代碼。</param>
        /// <param name="startDate">查詢起始時間。</param>
        /// <param name="endDate">查詢結束時間。</param>
        /// <returns>符合條件的清關資料查詢。</returns>
        private IQueryable<ClearanceLookupRow> BuildClearanceQuery(string dataType, DateTime startDate, DateTime endDate)
        {
            return DataCenterDb.ClearanceInfos
                .AsNoTracking()
                .Where(x =>
                    x.DataType == dataType &&
                    x.SignOutTime.HasValue &&
                    x.SignOutTime.Value >= startDate &&
                    x.SignOutTime.Value <= endDate)
                .Select(x => new ClearanceLookupRow
                {
                    DataType = x.DataType,
                    ClearanceType = x.ClearanceType,
                    ClearanceNumber = x.ClearanceNumber,
                    SignInTime = x.SignInTime,
                    SignOutTime = x.SignOutTime,
                    MainNumber = x.MainNumber,
                    BagNumber = x.BagNumber,
                    MergeNumber = x.MergeNumber
                });
        }

        /// <summary>
        /// 取得 tact 稅單資料。
        /// </summary>
        /// <param name="mainNumbers">主提單號清單。</param>
        /// <param name="bagNumbers">候選袋號清單。</param>
        /// <returns>tact 稅單資料。</returns>
        private List<TaxLookupRow> GetTactTaxes(List<string> mainNumbers, List<string> bagNumbers)
        {
            var bagNumberSet = new HashSet<string>(bagNumbers.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!mainNumbers.Any() || bagNumberSet.Count == 0)
            {
                return new List<TaxLookupRow>();
            }

            // 主提單號筆數不多，直接一次查出後再用候選袋號過濾即可。
            var items = DataCenterDb.EtlTactTaxes
                .AsNoTracking()
                .Where(x => mainNumbers.Contains(x.MainNumber))
                .Select(x => new
                {
                    x.MainNumber,
                    x.BagNumber,
                    x.TaxNumber,
                    x.TaxAmount,
                    x.TaxBase,
                    x.Taxpayer,
                    x.TaxpayerId
                })
                .ToList()
                .Select(x => new TaxLookupRow
                {
                    MainNumber = x.MainNumber,
                    BagNumber = x.BagNumber,
                    TaxNumber = x.TaxNumber,
                    TaxAmount = x.TaxAmount.HasValue ? x.TaxAmount.Value.ToString() : string.Empty,
                    TaxBase = ToNullableInt(x.TaxBase),
                    TaxPayer = x.Taxpayer,
                    TaxRecId = x.TaxpayerId
                })
                .ToList();

            return items.Where(x => bagNumberSet.Contains(x.BagNumber ?? string.Empty)).ToList();
        }

        /// <summary>
        /// 取得 ftz 稅單資料。
        /// </summary>
        /// <param name="mainNumbers">主提單號清單。</param>
        /// <param name="bagNumbers">候選袋號清單。</param>
        /// <returns>ftz 稅單資料。</returns>
        private List<TaxLookupRow> GetFtzTaxes(List<string> mainNumbers, List<string> bagNumbers)
        {
            var bagNumberSet = new HashSet<string>(bagNumbers.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!mainNumbers.Any() || bagNumberSet.Count == 0)
            {
                return new List<TaxLookupRow>();
            }

            var items = DataCenterDb.EtlFtzTaxes
                .AsNoTracking()
                .Where(x => mainNumbers.Contains(x.MainNumber))
                .Select(x => new
                {
                    x.MainNumber,
                    x.BagNumber,
                    x.TaxNumber,
                    x.TaxAmount,
                    x.TaxBase,
                    x.Taxpayer,
                    x.TaxpayerId
                })
                .ToList()
                .Select(x => new TaxLookupRow
                {
                    MainNumber = x.MainNumber,
                    BagNumber = x.BagNumber,
                    TaxNumber = x.TaxNumber,
                    TaxAmount = x.TaxAmount,
                    TaxBase = ToNullableInt(x.TaxBase),
                    TaxPayer = x.Taxpayer,
                    TaxRecId = x.TaxpayerId
                })
                .ToList();

            return items.Where(x => bagNumberSet.Contains(x.BagNumber ?? string.Empty)).ToList();
        }

        /// <summary>
        /// 取得對應主提單號的原始單資料。
        /// </summary>
        /// <param name="dataType">資料來源代碼。</param>
        /// <param name="clearanceQuery">清關資料查詢。</param>
        /// <returns>原始單資料。</returns>
        private List<OriginalListLookupRow> GetOriginalLists(string dataType, IQueryable<ClearanceLookupRow> clearanceQuery)
        {
            if (clearanceQuery == null)
            {
                return new List<OriginalListLookupRow>();
            }

            Logger.Debug("step 4-1 開始: 使用 join 取得 OriginalList 資料");

            var bagJoinRows = new List<OriginalListLookupRow>();
            if (dataType == TactSource)
            {
                bagJoinRows = ProjectOriginalListQuery(
                    from clearance in clearanceQuery
                    join original in DataCenterDb.OriginalLists.AsNoTracking()
                        on new
                        {
                            clearance.MainNumber,
                            BagNumber = clearance.BagNumber
                        }
                        equals new
                        {
                            original.MainNumber,
                            BagNumber = original.BagNo
                        }
                    where !string.IsNullOrEmpty(clearance.MainNumber) && !string.IsNullOrEmpty(clearance.BagNumber)
                    select original)
                    .ToList();
            }

            var trackingJoinRows = ProjectOriginalListQuery(
                from clearance in clearanceQuery
                join original in DataCenterDb.OriginalLists.AsNoTracking()
                    on new
                    {
                        clearance.MainNumber,
                        MergeNumber = clearance.MergeNumber
                    }
                    equals new
                    {
                        original.MainNumber,
                        MergeNumber = original.TrackingUb
                    }
                where !string.IsNullOrEmpty(clearance.MainNumber) && !string.IsNullOrEmpty(clearance.MergeNumber)
                select original)
                .ToList();

            Logger.Debug($"step 4-1 結束: 使用 join 取得 OriginalList 資料，BagNo join 筆數={bagJoinRows.Count}，TrackingUb join 筆數={trackingJoinRows.Count}");

            Logger.Debug("step 4-2 開始: 合併並去除重複的 OriginalList");
            var rows = bagJoinRows
                .Concat(trackingJoinRows)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();
            Logger.Debug($"step 4-2 結束: 合併並去除重複的 OriginalList，BagNo join 筆數={bagJoinRows.Count}，TrackingUb join 筆數={trackingJoinRows.Count}，去重後筆數={rows.Count}");

            return rows;
        }

        /// <summary>
        /// 只投影 UploadEtl 流程實際用到的 ORIGINALLIST 欄位，避免查詢整列大欄位資料。
        /// </summary>
        /// <param name="query">原始 ORIGINALLIST 查詢。</param>
        /// <returns>縮欄位後的查詢。</returns>
        private static IQueryable<OriginalListLookupRow> ProjectOriginalListQuery(IQueryable<OriginalListEntity> query)
        {
            return query.Select(x => new OriginalListLookupRow
            {
                Id = x.Id,
                MainNumber = x.MainNumber,
                BagNo = x.BagNo,
                TrackingNo = x.TrackingNo,
                Recipient = x.Recipient,
                RecPhone = x.RecPhone,
                RecAddress = x.RecAddress,
                RecId = x.RecId,
                Cc = x.Cc,
                DespatchNo = x.DespatchNo,
                TrackingUb = x.TrackingUb,
                DeliveryNo = x.DeliveryNo,
                TransTaxPayment = x.TransTaxPayment,
                Ecm = x.Ecm
            });
        }

        /// <summary>
        /// 批次查出 UploadEtl 可能會用到的空運客戶主檔。
        /// </summary>
        /// <param name="originals">原始單資料。</param>
        /// <returns>客戶主檔清單。</returns>
        private List<CustomerMasterEntity> LoadCustomerMasters(IEnumerable<OriginalListLookupRow> originals)
        {
            var customerCodes = originals
                .Select(x => PadCustomerCode(x.DespatchNo))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var transNos = originals
                .Select(x => x.TransTaxPayment)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var customerMasters = new List<CustomerMasterEntity>();
            if (!customerCodes.Any() || !transNos.Any())
            {
                return customerMasters;
            }

            foreach (var customerBatch in Batch(customerCodes, BatchSize))
            {
                foreach (var transBatch in Batch(transNos, BatchSize))
                {
                    // 客戶代號與物流代碼交叉分批查詢，避免單次 SQL 參數過多。
                    var items = JetfDb.CustomerMasters
                        .AsNoTracking()
                        .Where(x => x.TranType == "空運" && customerBatch.Contains(x.CustId) && transBatch.Contains(x.TransNo))
                        .ToList();

                    customerMasters.AddRange(items);
                }
            }

            // 若主檔存在重複客戶代號與派件公司，保留原本 dictionary 覆蓋後的最後一筆行為。
            return customerMasters
                .GroupBy(x => new { CustId = NormalizeText(x.CustId), TransNo = NormalizeText(x.TransNo) })
                .Select(x => x.Last())
                .ToList();
        }

        /// <summary>
        /// 建立報表輸出用的客戶主檔 dictionary。
        /// </summary>
        /// <param name="rows">待輸出的報表資料。</param>
        /// <returns>報表用物流資訊對照表。</returns>
        private Dictionary<string, ReportCustomerLookup> BuildReportCustomerLookup(IEnumerable<DownloadEtlNewReportItem> rows)
        {
            var customerCodes = rows
                .Select(x => PadCustomerCode(x.Customer))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var transNos = rows
                .Select(x => x.TransNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var lookup = new Dictionary<string, ReportCustomerLookup>();
            if (!customerCodes.Any() || !transNos.Any())
            {
                return lookup;
            }

            foreach (var customerBatch in Batch(customerCodes, BatchSize))
            {
                foreach (var transBatch in Batch(transNos, BatchSize))
                {
                    var items = JetfDb.CustomerMasters
                        .AsNoTracking()
                        .Where(x => x.TranType == "空運" && customerBatch.Contains(x.CustId) && transBatch.Contains(x.TransNo))
                        .Select(x => new ReportCustomerLookup
                        {
                            CustId = x.CustId,
                            TransNo = x.TransNo,
                            TransName = x.TransName,
                            Company = x.Company
                        })
                        .ToList();

                    foreach (var item in items)
                    {
                        lookup[BuildCompositeKey(item.CustId, item.TransNo)] = item;
                    }
                }
            }

            return lookup;
        }

        /// <summary>
        /// 取得空運特殊客戶的電話集合。
        /// </summary>
        /// <returns>正規化後的電話集合。</returns>
        private HashSet<string> GetSpecialPhoneSet()
        {
            var phones = JetfDb.CustomerSpecials
                .AsNoTracking()
                .Where(x => x.TranType == "空運")
                .Select(x => x.Phone)
                .ToList();
            var normalizedPhones = phones
                .Select(NormalizeSpecialPhone)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            return new HashSet<string>(normalizedPhones);
        }

        /// <summary>
        /// 將草稿資料新增或更新到 FEE_MASTER_TEST。
        /// </summary>
        /// <param name="drafts">待寫入的草稿資料。</param>
        /// <param name="dataDate">資料日期。</param>
        private void SaveFeeMasters(List<FeeMasterDraft> drafts, string dataDate)
        {
            if (drafts == null || drafts.Count == 0)
            {
                return;
            }

            // 主檔與明細必須在同一個交易內完成，避免主檔已更新但明細仍停留在舊資料。
            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    // step1: 先把現有資料載入成 lookup，後續可直接判斷新增或更新。
                    var existingRows = LoadExistingFeeMasters(drafts);
                    var existingLookup = existingRows.ToDictionary(x => BuildCompositeKey(x.MainNumber, x.TrackingNo));
                    var updateTime = DateTime.Now;
                    var insertedRows = new List<FeeMasterTestEntity>();
                    var updatedRows = new List<FeeMasterTestEntity>();

                    // step2: 依 main_number + trackingno 判斷是新增或更新，維持既有 upsert 規則。
                    foreach (var draft in drafts)
                    {
                        var key = BuildCompositeKey(draft.MainNumber, draft.TrackingNo);
                        if (existingLookup.TryGetValue(key, out var existingRow))
                        {
                            ApplyDraftToEntity(existingRow, draft, dataDate, updateTime);
                            updatedRows.Add(existingRow);
                            continue;
                        }

                        insertedRows.Add(CreateFeeMasterEntity(draft, dataDate));
                    }

                    if (insertedRows.Count > 0)
                    {
                        // step3: 新增主檔並回填 Id，供後續明細關聯使用。
                        JetfDb.BulkInsert(insertedRows, operation => operation.AutoMapOutputDirection = true);
                    }

                    if (updatedRows.Count > 0)
                    {
                        // step4: 更新既有主檔資料。BulkUpdate 使用 LoadExistingFeeMasters 載入的既有 Id。
                        JetfDb.BulkUpdate(updatedRows);
                    }

                    // step5: 主檔 Id 確定後，重建本次對應的明細資料。
                    SaveFeeMasterDetails(drafts, insertedRows, updatedRows);
                    transaction.Commit();
                }
                catch
                {
                    // 任一段 DB 寫入失敗都回滾，並將原例外往外拋給 UploadEtl 統一轉成 ResponseModel。
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 依本次 upsert 後的主檔 Id 重建 FEE_MASTER_DETAIL 明細。
        /// </summary>
        /// <param name="drafts">本次待寫入的主檔草稿。</param>
        /// <param name="insertedRows">本次新增後的主檔資料。</param>
        /// <param name="updatedRows">本次更新後的主檔資料。</param>
        private void SaveFeeMasterDetails(
            List<FeeMasterDraft> drafts,
            List<FeeMasterTestEntity> insertedRows,
            List<FeeMasterTestEntity> updatedRows)
        {
            var masterRows = insertedRows
                .Concat(updatedRows)
                .GroupBy(x => BuildCompositeKey(x.MainNumber, x.TrackingNo))
                .ToDictionary(x => x.Key, x => x.First());

            if (masterRows.Count == 0)
            {
                return;
            }

            // step1: 更新既有主檔時，先刪除舊明細避免同一主檔留下重複資料。
            var existingMasterIds = updatedRows.Select(x => x.Id).ToList();
            if (existingMasterIds.Count > 0)
            {
                var existingDetails = JetfDb.FeeMasterDetails
                    .Where(x => existingMasterIds.Contains(x.FeeMasterId))
                    .ToList();

                if (existingDetails.Count > 0)
                {
                    JetfDb.BulkDelete(existingDetails);
                }
            }

            // step2: 依本次主檔 Id 建立所有明細 entity。
            var detailEntities = new List<FeeMasterDetailEntity>();
            foreach (var draft in drafts)
            {
                if (!masterRows.TryGetValue(BuildCompositeKey(draft.MainNumber, draft.TrackingNo), out var master))
                {
                    continue;
                }

                detailEntities.AddRange(draft.DetailRows.Select(row => CreateFeeMasterDetailEntity(row, master.Id)));
            }

            // step3: 批次寫入新的明細資料。
            if (detailEntities.Count > 0)
            {
                JetfDb.BulkInsert(detailEntities);
            }
        }

        /// <summary>
        /// 依本次草稿資料載入既有 FEE_MASTER_TEST。
        /// </summary>
        /// <param name="drafts">待比對的草稿資料。</param>
        /// <returns>既有 fee master 清單。</returns>
        private List<FeeMasterTestEntity> LoadExistingFeeMasters(IEnumerable<FeeMasterDraft> drafts)
        {
            var mainNumbers = drafts
                .Select(x => x.MainNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var trackingNos = drafts
                .Select(x => x.TrackingNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var keys = new HashSet<string>(drafts.Select(x => BuildCompositeKey(x.MainNumber, x.TrackingNo)));
            var rows = new List<FeeMasterTestEntity>();

            if (!mainNumbers.Any() || !trackingNos.Any())
            {
                return rows;
            }

            foreach (var trackingBatch in Batch(trackingNos, BatchSize))
            {
                var items = JetfDb.FeeMasterTests
                    .AsNoTracking()
                    .Where(x =>
                        x.SourceType == AirSourceType.ToString() &&
                        mainNumbers.Contains(x.MainNumber) &&
                        trackingBatch.Contains(x.TrackingNo))
                    .ToList();
                var matchedItems = items.Where(x => keys.Contains(BuildCompositeKey(x.MainNumber, x.TrackingNo))).ToList();

                rows.AddRange(matchedItems);
            }

            return rows;
        }

        /// <summary>
        /// 回填報表需要的派件公司與物流名稱。
        /// </summary>
        /// <param name="row">報表資料列。</param>
        /// <param name="lookup">客戶主檔對照表。</param>
        /// <returns>補齊物流資訊的報表資料列。</returns>
        private static DownloadEtlNewReportItem ApplyReportCompanyInfo(DownloadEtlNewReportItem row, Dictionary<string, ReportCustomerLookup> lookup)
        {
            lookup.TryGetValue(BuildCompositeKey(PadCustomerCode(row.Customer), row.TransNo), out var customer);
            row.TransName = customer?.TransName ?? string.Empty;
            row.Company = customer?.Company ?? string.Empty;
            return row;
        }

        /// <summary>
        /// 以原始單上的客戶代號與物流代碼查找客戶主檔。
        /// </summary>
        /// <param name="original">原始單資料。</param>
        /// <param name="customerMasters">候選客戶主檔清單。</param>
        /// <returns>符合條件的客戶主檔。</returns>
        private static CustomerMasterEntity FindCustomerMaster(OriginalListLookupRow original, IEnumerable<CustomerMasterEntity> customerMasters)
        {
            if (original == null || customerMasters == null)
            {
                return null;
            }

            var customerCode = PadCustomerCode(original.DespatchNo);
            var transNo = NormalizeText(original.TransTaxPayment);

            // 每次只會用一筆原始單找對應客戶，直接用條件查詢比 join 更直覺。
            return customerMasters
                .Where(x => NormalizeText(x.CustId) == customerCode && NormalizeText(x.TransNo) == transNo)
                .FirstOrDefault();
        }

        /// <summary>
        /// 依主提單號與袋號取得對應稅單，必要時退回 merge number 比對。
        /// </summary>
        /// <param name="lookup">稅單對照表。</param>
        /// <param name="mainNumber">主提單號。</param>
        /// <param name="bagNumber">袋號。</param>
        /// <param name="mergeNumber">併袋號。</param>
        /// <returns>符合條件的稅單清單。</returns>
        private static List<TaxLookupRow> GetMatchedTaxes(Dictionary<string, List<TaxLookupRow>> lookup, string mainNumber, string bagNumber, string mergeNumber)
        {
            if (lookup.TryGetValue(BuildCompositeKey(mainNumber, bagNumber), out var primaryRows) && primaryRows.Any())
            {
                return primaryRows;
            }

            if (lookup.TryGetValue(BuildCompositeKey(mainNumber, mergeNumber), out var mergeRows) && mergeRows.Any())
            {
                return mergeRows;
            }

            return new List<TaxLookupRow>();
        }

        /// <summary>
        /// 依袋號優先、tracking_ub 次之的順序找出最適合的原始單資料。
        /// </summary>
        /// <param name="originalByBag">袋號對照表。</param>
        /// <param name="originalByTrackingUb">tracking_ub 對照表。</param>
        /// <param name="mainNumber">主提單號。</param>
        /// <param name="bagNumber">袋號。</param>
        /// <returns>最適合的原始單資料。</returns>
        private static OriginalListLookupRow FindBestOriginal(
            Dictionary<string, List<OriginalListLookupRow>> originalByBag,
            Dictionary<string, List<OriginalListLookupRow>> originalByTrackingUb,
            string mainNumber,
            string bagNumber)
        {
            if (originalByBag.TryGetValue(BuildCompositeKey(mainNumber, bagNumber), out var bagRows) && bagRows.Any())
            {
                return bagRows[0];
            }

            if (originalByTrackingUb.TryGetValue(BuildCompositeKey(mainNumber, bagNumber), out var trackingRows) && trackingRows.Any())
            {
                return trackingRows[0];
            }

            return null;
        }

        /// <summary>
        /// 將草稿資料轉成新的 FEE_MASTER_TEST entity。
        /// </summary>
        /// <param name="draft">待寫入草稿。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <returns>新的 fee master entity。</returns>
        private static FeeMasterTestEntity CreateFeeMasterEntity(FeeMasterDraft draft, string dataDate)
        {
            return new FeeMasterTestEntity
            {
                DataDate = dataDate,
                Source = NormalizeText(draft.Source),
                SourceType = AirSourceType.ToString(),
                Type = NormalizeText(draft.Type),
                Customer = NormalizeText(draft.Customer),
                MainNumber = NormalizeText(draft.MainNumber),
                TrackingNo = NormalizeText(draft.TrackingNo),
                ClearanceNumber = NormalizeText(draft.ClearanceNumber),
                BagNumber = NormalizeText(draft.BagNumber),
                TaxNumber = NormalizeText(draft.TaxNumber),
                TaxPayer = NormalizeText(draft.TaxPayer),
                TaxRecId = NormalizeText(draft.TaxRecId),
                DlvInv = NormalizeText(draft.DlvInv),
                InDate = NormalizeText(draft.InDate),
                InDateTime = draft.InDateTime,
                OutDateTime = draft.OutDateTime,
                Combine = NormalizeText(draft.Combine),
                TaxBase = draft.TaxBase,
                Tax1 = draft.Tax1,
                Tax2 = draft.Tax2,
                Cod = draft.Cod,
                Fee = draft.Fee,
                IncludeTax = NormalizeText(draft.IncludeTax),
                Recipient = NormalizeText(draft.Recipient),
                RecPhone = NormalizeText(draft.RecPhone),
                RecAddress = NormalizeText(draft.RecAddress),
                RecId = NormalizeText(draft.RecId),
                ToDlvCod = draft.ToDlvCod.ToString(CultureInfo.InvariantCulture),
                DlvCom = NormalizeText(draft.DlvCom),
                Arrival = NormalizeText(draft.Arrival),
                CustomerCod = draft.CustomerCod,
                TransCod = draft.TransCod
            };
        }

        /// <summary>
        /// 將草稿資料覆寫到既有 FEE_MASTER_TEST entity。
        /// </summary>
        /// <param name="entity">既有 fee master。</param>
        /// <param name="draft">最新草稿。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <param name="updateTime">更新時間。</param>
        private static void ApplyDraftToEntity(FeeMasterTestEntity entity, FeeMasterDraft draft, string dataDate, DateTime updateTime)
        {
            entity.DataDate = dataDate;
            entity.Source = NormalizeText(draft.Source);
            entity.Type = NormalizeText(draft.Type);
            entity.Customer = NormalizeText(draft.Customer);
            entity.MainNumber = NormalizeText(draft.MainNumber);
            entity.TrackingNo = NormalizeText(draft.TrackingNo);
            entity.ClearanceNumber = NormalizeText(draft.ClearanceNumber);
            entity.BagNumber = NormalizeText(draft.BagNumber);
            entity.Combine = NormalizeText(draft.Combine);
            entity.InDate = NormalizeText(draft.InDate);
            entity.InDateTime = draft.InDateTime;
            entity.OutDateTime = draft.OutDateTime;
            entity.TaxBase = draft.TaxBase;
            entity.Tax1 = draft.Tax1;
            entity.Tax2 = draft.Tax2;
            entity.DlvCom = NormalizeText(draft.DlvCom);
            entity.TaxNumber = NormalizeText(draft.TaxNumber);
            entity.Fee = draft.Fee;
            entity.IncludeTax = NormalizeText(draft.IncludeTax);
            entity.Recipient = NormalizeText(draft.Recipient);
            entity.RecPhone = NormalizeText(draft.RecPhone);
            entity.RecAddress = NormalizeText(draft.RecAddress);
            entity.RecId = NormalizeText(draft.RecId);
            entity.Cod = draft.Cod;
            entity.ToDlvCod = draft.ToDlvCod.ToString(CultureInfo.InvariantCulture);
            entity.DlvInv = NormalizeText(draft.DlvInv);
            entity.TaxPayer = NormalizeText(draft.TaxPayer);
            entity.TaxRecId = NormalizeText(draft.TaxRecId);
            entity.Arrival = NormalizeText(draft.Arrival);
            entity.CustomerCod = draft.CustomerCod;
            entity.TransCod = draft.TransCod;
            entity.UpdateDate = updateTime;
            entity.RecordFeeMaster = "0";
        }

        /// <summary>
        /// 依舊系統規則計算代收相關欄位。
        /// </summary>
        /// <param name="draft">待回填的 fee master 草稿。</param>
        /// <param name="latestRow">同 tracking 最新一筆來源資料。</param>
        /// <param name="specialPhones">特殊客戶電話集合。</param>
        private static void ApplyTaxRule(FeeMasterDraft draft, CombinedRow latestRow, HashSet<string> specialPhones)
        {
            var amounts = new TaxAmountSet
            {
                Tax1 = draft.Tax1,
                Tax2 = draft.Tax2,
                Cod = draft.Cod,
                Fee = draft.Fee
            };

            // step 1: 已稅內含(Y)者，整筆稅金視為客戶已吸收。
            if (draft.IncludeTax == "Y")
            {
                ApplyTaxData(draft, CalculateTaxY(amounts));
                return;
            }

            // step 2: 菜鳥 P 客戶先判斷是否超過 1000 元門檻，再決定物流代收與客戶吸收金額。
            if (latestRow.IsCainiaoP)
            {
                var taxData = CalculateTaxP(amounts);
                draft.IncludeTax = taxData.TransCod > 0 ? "N" : draft.IncludeTax;
                draft.Fee = taxData.TransCod > 0 ? draft.Fee : 0;
                ApplyTaxData(draft, taxData);
                return;
            }

            // step 3: 指定 D 類型時，稅金由物流代收，貨到付款金額另外保留。
            if (draft.IncludeTax == "D")
            {
                ApplyTaxData(draft, CalculateTaxD(amounts));
                return;
            }

            // step 4: 特殊客戶條件維持舊版判斷，命中時轉成 D 類型處理。
            if (IsSpecialEtlCustomer(latestRow.Company, draft.RecPhone, specialPhones))
            {
                draft.IncludeTax = "D";
                draft.Fee = 0;
                ApplyTaxData(draft, CalculateTaxD(amounts));
                return;
            }

            // step 5: C 類型代表客戶吸收稅金，只保留 COD。
            if (draft.IncludeTax == "C")
            {
                draft.Fee = 0;
                ApplyTaxData(draft, CalculateTaxC(amounts));
                return;
            }

            // step 6: 其餘情況走一般 N 類型，稅金與手續費都由物流端代收。
            ApplyTaxData(draft, CalculateTaxN(amounts));
        }

        /// <summary>
        /// 建立同一 tracking 下每張稅單的明細資料。
        /// </summary>
        /// <param name="detailRows">同一 tracking 的來源資料。</param>
        /// <param name="specialPhones">特殊客戶電話集合。</param>
        /// <returns>待寫入 FEE_MASTER_DETAIL 的明細草稿。</returns>
        private static List<FeeMasterDetailRow> CreateFeeMasterDetailRows(
            IEnumerable<CombinedRow> detailRows,
            HashSet<string> specialPhones)
        {
            var rows = (detailRows ?? Enumerable.Empty<CombinedRow>()).ToList();
            if (rows.Count == 0)
            {
                return new List<FeeMasterDetailRow>();
            }

            // 菜鳥 P 的稅額分攤規則海空運一致，因此共用 SeaTaxUploadService 的計算。
            // 這裡只做空運 CombinedRow -> 共用來源 row 的欄位對應，不改變任何金額規則。
            if (rows[0].IsCainiaoP)
            {
                return SeaTaxUploadService.CreateCainiaoPDetailRows(
                    rows.Select(row => new FeeMasterDetailSourceRow
                    {
                        MainNumber = row.MainNumber,
                        TrackingNo = row.TrackingNo,
                        ClearanceNumber = row.ClearanceNumber,
                        BagNumber = row.BagNo,
                        TaxNumber = row.TaxNumber,
                        TaxPayer = row.TaxPayer,
                        TaxRecId = row.TaxRecId,
                        DlvInv = row.DeliveryNo,
                        TaxBase = row.TaxBase.HasValue ? row.TaxBase.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        Tax = row.TaxAmount,
                        Cod = row.Cc,
                        Fee = row.CodFee.HasValue ? row.CodFee.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        Recipient = row.Recipient,
                        RecPhone = ToNarrowPhone(row.RecPhone),
                        RecAddress = row.RecAddress,
                        IsCainiaoP = row.IsCainiaoP
                    }));
            }

            // 非菜鳥 P 的一般稅金判斷海運與空運不同，保留空運自己的規則以避免隱性改動。
            return rows.Select(row => CreateRegularDetailRow(row, specialPhones)).ToList();
        }

        /// <summary>
        /// 建立空運一般明細資料。
        /// </summary>
        /// <param name="row">空運來源資料。</param>
        /// <param name="specialPhones">特殊客戶電話集合。</param>
        /// <returns>FEE_MASTER_DETAIL 明細資料。</returns>
        private static FeeMasterDetailRow CreateRegularDetailRow(CombinedRow row, HashSet<string> specialPhones)
        {
            var taxAmount = ToInt(row.TaxAmount);
            var codAmount = ToInt(row.Cc);
            var feeAmount = row.CodFee ?? 0;
            var detailFee = feeAmount;
            var amounts = new TaxAmountSet { Tax1 = taxAmount, Tax2 = 0, Cod = codAmount, Fee = feeAmount };
            var includeTax = NormalizeText(row.IncludeTax);
            TaxCalculationResult taxData;

            // 以下判斷順序需與主檔 ApplyTaxRule 的既有邏輯一致，避免主檔與明細金額不一致。
            if (includeTax == "Y")
            {
                taxData = CalculateTaxY(amounts);
            }
            else if (includeTax == "D" || IsSpecialEtlCustomer(row.Company, ToNarrowPhone(row.RecPhone).Trim(), specialPhones))
            {
                taxData = CalculateTaxD(amounts);
                detailFee = 0;
            }
            else if (includeTax == "C")
            {
                taxData = CalculateTaxC(amounts);
                detailFee = 0;
            }
            else
            {
                taxData = CalculateTaxN(amounts);
            }

            return CreateFeeMasterDetailRow(row, detailFee, taxData.ToDlvCod);
        }

        /// <summary>
        /// 將空運來源資料與已計算完成的明細金額轉成 detail row。
        /// </summary>
        /// <param name="row">空運來源資料。</param>
        /// <param name="feeAmount">明細手續費。</param>
        /// <param name="toDlvCod">應向物流代收金額。</param>
        /// <returns>FEE_MASTER_DETAIL 明細資料。</returns>
        private static FeeMasterDetailRow CreateFeeMasterDetailRow(CombinedRow row, int feeAmount, int toDlvCod)
        {
            return new FeeMasterDetailRow
            {
                MainNumber = row.MainNumber,
                TrackingNo = row.TrackingNo,
                ClearanceNumber = row.ClearanceNumber,
                BagNumber = row.BagNo,
                TaxNumber = row.TaxNumber,
                TaxPayer = row.TaxPayer,
                TaxRecId = row.TaxRecId,
                DlvInv = row.DeliveryNo,
                TaxBase = row.TaxBase.HasValue ? row.TaxBase.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Tax = row.TaxAmount,
                Ccfee = string.Empty,
                Cod = row.Cc,
                Fee = feeAmount.ToString(CultureInfo.InvariantCulture),
                Recipient = row.Recipient,
                RecPhone = ToNarrowPhone(row.RecPhone),
                RecAddress = row.RecAddress,
                ToDlvCod = toDlvCod.ToString(CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// 建立 FEE_MASTER_DETAIL 寫入實體。
        /// </summary>
        /// <param name="row">明細草稿。</param>
        /// <param name="feeMasterId">對應主檔 Id。</param>
        /// <returns>明細 entity。</returns>
        private static FeeMasterDetailEntity CreateFeeMasterDetailEntity(FeeMasterDetailRow row, int feeMasterId)
        {
            return new FeeMasterDetailEntity
            {
                FeeMasterId = feeMasterId,
                MainNumber = NormalizeText(row.MainNumber),
                TrackingNo = NormalizeText(row.TrackingNo),
                ClearanceNumber = NormalizeText(row.ClearanceNumber),
                BagNumber = NormalizeText(row.BagNumber),
                TaxNumber = NormalizeText(row.TaxNumber),
                TaxPayer = NormalizeText(row.TaxPayer),
                TaxRecId = NormalizeText(row.TaxRecId),
                DlvInv = NormalizeText(row.DlvInv),
                TaxBase = ToNullableInt(row.TaxBase),
                Tax = ToNullableInt(row.Tax),
                Ccfee = ToNullableInt(row.Ccfee),
                Cod = ToNullableInt(row.Cod),
                Fee = ToNullableInt(row.Fee),
                Recipient = NormalizeText(row.Recipient),
                RecPhone = NormalizeText(row.RecPhone),
                RecAddress = NormalizeText(row.RecAddress),
                ToDlvCod = NormalizeText(row.ToDlvCod)
            };
        }

        /// <summary>
        /// 判斷是否為舊系統定義的特殊空運客戶。
        /// </summary>
        /// <param name="company">派件公司。</param>
        /// <param name="phone">收件電話。</param>
        /// <param name="specialPhones">特殊客戶電話集合。</param>
        /// <returns>是否為特殊客戶。</returns>
        private static bool IsSpecialEtlCustomer(string company, string phone, HashSet<string> specialPhones)
        {
            var normalizedPhone = NormalizeSpecialPhone(phone);

            // NOTE: 這裡保留舊系統判斷式，不直接修正。
            // company 同時等於三個不同名稱的條件理論上不會成立，後續若要調整需先確認既有報表與帳務結果。
            return !string.IsNullOrWhiteSpace(normalizedPhone)
                && specialPhones.Contains(normalizedPhone)
                && company == "新竹物流"
                && company == "新瑞宅配"
                && company == "捷豐";
        }

        /// <summary>
        /// 計算菜鳥 P 客戶的代收金額。
        /// </summary>
        /// <param name="amounts">本次應計金額。</param>
        /// <returns>計算結果。</returns>
        private static TaxCalculationResult CalculateTaxP(TaxAmountSet amounts)
        {
            if (amounts.Tax1 + amounts.Tax2 > 1000)
            {
                return new TaxCalculationResult
                {
                    TransCod = (amounts.Tax1 + amounts.Tax2) - 1000,
                    CustomerCod = 1000,
                    ToDlvCod = (amounts.Tax1 + amounts.Tax2) - 1000 + amounts.Cod + amounts.Fee
                };
            }

            return new TaxCalculationResult
            {
                TransCod = 0,
                CustomerCod = amounts.Tax1 + amounts.Tax2,
                ToDlvCod = amounts.Cod
            };
        }

        /// <summary>
        /// 計算一般 N 類型的代收金額。
        /// </summary>
        /// <param name="amounts">本次應計金額。</param>
        /// <returns>計算結果。</returns>
        private static TaxCalculationResult CalculateTaxN(TaxAmountSet amounts)
        {
            return new TaxCalculationResult
            {
                TransCod = amounts.Tax1 + amounts.Tax2,
                CustomerCod = 0,
                ToDlvCod = amounts.Tax1 + amounts.Tax2 + amounts.Cod + amounts.Fee
            };
        }

        /// <summary>
        /// 計算 C 類型的代收金額。
        /// </summary>
        /// <param name="amounts">本次應計金額。</param>
        /// <returns>計算結果。</returns>
        private static TaxCalculationResult CalculateTaxC(TaxAmountSet amounts)
        {
            return new TaxCalculationResult
            {
                TransCod = 0,
                CustomerCod = amounts.Tax1 + amounts.Tax2,
                ToDlvCod = amounts.Cod
            };
        }

        /// <summary>
        /// 計算 D 類型的代收金額。
        /// </summary>
        /// <param name="amounts">本次應計金額。</param>
        /// <returns>計算結果。</returns>
        private static TaxCalculationResult CalculateTaxD(TaxAmountSet amounts)
        {
            return new TaxCalculationResult
            {
                TransCod = amounts.Tax1 + amounts.Tax2,
                CustomerCod = 0,
                ToDlvCod = amounts.Cod
            };
        }

        /// <summary>
        /// 計算 Y 類型的代收金額。
        /// </summary>
        /// <param name="amounts">本次應計金額。</param>
        /// <returns>計算結果。</returns>
        private static TaxCalculationResult CalculateTaxY(TaxAmountSet amounts)
        {
            return new TaxCalculationResult
            {
                TransCod = 0,
                CustomerCod = amounts.Tax1 + amounts.Tax2,
                ToDlvCod = amounts.Cod
            };
        }

        /// <summary>
        /// 將計算結果回填到草稿資料。
        /// </summary>
        /// <param name="draft">fee master 草稿。</param>
        /// <param name="taxData">計算結果。</param>
        private static void ApplyTaxData(FeeMasterDraft draft, TaxCalculationResult taxData)
        {
            draft.TransCod = taxData.TransCod;
            draft.CustomerCod = taxData.CustomerCod;
            draft.ToDlvCod = taxData.ToDlvCod;
        }

        /// <summary>
        /// 將畫面日期與時間區間組成實際查詢時間範圍。
        /// </summary>
        /// <param name="date">畫面選擇日期。</param>
        /// <param name="timeBetween">畫面選擇的時間區間代碼。</param>
        /// <param name="sTime">開始時間。</param>
        /// <param name="eTime">結束時間。</param>
        /// <param name="startDate">輸出的開始時間。</param>
        /// <param name="endDate">輸出的結束時間。</param>
        /// <param name="dataDate">輸出的資料日期。</param>
        /// <param name="errorMessage">錯誤訊息。</param>
        /// <returns>是否成功組成時間區間。</returns>
        private static bool TryBuildDateRange(string date, string timeBetween, string sTime, string eTime, out DateTime startDate, out DateTime endDate, out string dataDate, out string errorMessage)
        {
            startDate = DateTime.MinValue;
            endDate = DateTime.MinValue;
            dataDate = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(sTime) || string.IsNullOrWhiteSpace(eTime) || sTime.Length != 4 || eTime.Length != 4)
            {
                errorMessage = "時間區間錯誤，請確認";
                return false;
            }

            if (!DateTime.TryParse(date, out var parsedDate))
            {
                errorMessage = "時間區間錯誤，請確認";
                return false;
            }

            dataDate = parsedDate.ToString("yyyyMMdd");
            var startText = string.Format("{0} {1}:{2}:00", parsedDate.ToString("yyyy-MM-dd"), sTime.Substring(0, 2), sTime.Substring(2, 2));
            var endText = string.Format("{0} {1}:{2}:00", parsedDate.ToString("yyyy-MM-dd"), eTime.Substring(0, 2), eTime.Substring(2, 2));

            if (!DateTime.TryParse(startText, out startDate) || !DateTime.TryParse(endText, out endDate))
            {
                errorMessage = "時間區間錯誤，請確認";
                return false;
            }

            if (timeBetween == "1")
            {
                // 舊畫面第一段區間會跨到前一天晚間，因此起始時間需回退一天。
                startDate = startDate.AddDays(-1);
            }

            return true;
        }

        /// <summary>
        /// 將字串集合切成固定大小的批次。
        /// </summary>
        /// <param name="source">來源集合。</param>
        /// <param name="size">每批大小。</param>
        /// <returns>批次集合。</returns>
        private static IEnumerable<List<string>> Batch(IEnumerable<string> source, int size)
        {
            var current = new List<string>(size);

            foreach (var item in source ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                current.Add(item);
                if (current.Count >= size)
                {
                    yield return current;
                    current = new List<string>(size);
                }
            }

            if (current.Count > 0)
            {
                yield return current;
            }
        }

        /// <summary>
        /// 建立雙欄位組成的 dictionary key。
        /// </summary>
        /// <param name="left">左側值。</param>
        /// <param name="right">右側值。</param>
        /// <returns>組合後的 key。</returns>
        private static string BuildCompositeKey(string left, string right)
        {
            return string.Format("{0}|{1}", left ?? string.Empty, right ?? string.Empty);
        }

        /// <summary>
        /// 將客戶代號補成五碼。
        /// </summary>
        /// <param name="value">原始客戶代號。</param>
        /// <returns>補零後的客戶代號。</returns>
        private static string PadCustomerCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().PadLeft(5, '0');
        }

        /// <summary>
        /// 去除字串前後空白，避免寫入資料時夾帶空值格式差異。
        /// </summary>
        /// <param name="value">原始字串。</param>
        /// <returns>正規化後的字串。</returns>
        private static string NormalizeText(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        /// <summary>
        /// 將字串安全轉成整數。
        /// </summary>
        /// <param name="value">原始字串。</param>
        /// <returns>整數結果，失敗時回傳 0。</returns>
        private static int ToInt(string value)
        {
            return int.TryParse(value, out var number) ? number : 0;
        }

        /// <summary>
        /// 將字串安全轉成可為 null 的整數。
        /// </summary>
        /// <param name="value">原始字串。</param>
        /// <returns>整數結果，失敗時回傳 null。</returns>
        private static int? ToNullableInt(string value)
        {
            return int.TryParse(value, out var number) ? number : (int?)null;
        }

        /// <summary>
        /// 將電話正規化成僅保留末九碼數字，供特殊客戶比對使用。
        /// </summary>
        /// <param name="phone">原始電話。</param>
        /// <returns>正規化後電話。</returns>
        private static string NormalizeSpecialPhone(string phone)
        {
            var numbersOnly = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (numbersOnly.Length > 9)
            {
                numbersOnly = numbersOnly.Substring(numbersOnly.Length - 9, 9);
            }

            return numbersOnly;
        }

        /// <summary>
        /// 將全形電話轉成半形格式。
        /// </summary>
        /// <param name="phone">原始電話。</param>
        /// <returns>半形電話字串。</returns>
        private static string ToNarrowPhone(string phone)
        {
            return Strings.StrConv(phone ?? string.Empty, VbStrConv.Narrow, 1028);
        }
    }

}
