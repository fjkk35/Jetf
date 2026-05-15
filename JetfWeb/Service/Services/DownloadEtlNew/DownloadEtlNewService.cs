using Microsoft.VisualBasic;
using Service.Data;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Service.Services.DownloadEtlNew
{
    public class DownloadEtlNewService : _BaseService
    {
        private const int AirSourceType = 3;
        private const int BatchSize = 500;

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
        /// 依指定日期區間重算空運代收資料，並回寫 FEE_MASTER。
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

            if (!TryBuildDateRange(date, timeBetween, sTime, eTime, out var startDate, out var endDate, out var dataDate, out var errorMessage))
            {
                responseModel.status = Status.error;
                responseModel.msg = errorMessage;
                return responseModel;
            }

            if (endDate <= startDate)
            {
                responseModel.status = Status.error;
                responseModel.msg = "結束時間大於開始時間，請確認";
                return responseModel;
            }

            try
            {
                // 先同步更新菜鳥稅金調整結果，確保後續組出的代收資料與既有流程一致。
                responseModel = UpdateCainiaoTaxEdit();
                if (responseModel.status != Status.success)
                {
                    return responseModel;
                }

                // 再依清關、稅單、原始單資料組出本次要寫回的 fee master 草稿。
                var feeMasterDrafts = BuildFeeMasterDrafts(startDate, endDate);
                SaveFeeMasters(feeMasterDrafts, dataDate);
                responseModel.status = Status.success;
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return responseModel;
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
                if (!TryBuildDateRange(date, timeBetween, sTime, eTime, out var startDate, out var endDate, out _, out var errorMessage))
                {
                    result.status = Status.error;
                    result.msg = errorMessage;
                    return result;
                }

                // 先以 fee master 當作報表底稿，後續再依代收類型與物流公司條件做篩選。
                var feeMasters = JetfDb.FeeMasters
                    .AsNoTracking()
                    .Where(x =>
                        (x.Source == "tact" || x.Source == "ftz") &&
                        x.SourceType == AirSourceType.ToString() &&
                        x.OutDateTime.HasValue &&
                        x.OutDateTime.Value >= startDate &&
                        x.OutDateTime.Value <= endDate)
                    .Select(x => new DownloadEtlNewReportItem
                    {
                        BagNumber = x.BagNumber,
                        TrackingNo = x.TrackingNo,
                        Tax1 = x.Tax1 ?? 0,
                        Tax2 = x.Tax2 ?? 0,
                        Fee = x.Fee ?? 0,
                        Cod = x.Cod ?? 0,
                        ToDlvCod = x.ToDlvCod ?? 0,
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
            var sourceRows = GetCombinedRows(startDate, endDate);
            var specialPhones = GetSpecialPhoneSet();
            var drafts = new List<FeeMasterDraft>();

            // 舊邏輯以 trackingno 分組，最新出倉的一筆作為主資料，其餘相同 tracking 的稅額累加到 tax2。
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
                var draft = new FeeMasterDraft
                {
                    Source = latestRow.DataType,
                    Type = latestRow.ClearanceType,
                    Customer = latestRow.DespatchNo,
                    MainNumber = latestRow.MainNumber,
                    TrackingNo = latestRow.TrackingNo,
                    ClearanceNumber = latestRow.ClearanceNumber,
                    BagNumber = latestRow.BagNo,
                    TaxNumber = latestRow.TaxNumber,
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

                // 套用舊系統稅金邏輯，計算 customer_cod、trans_cod、to_dlv_cod 等欄位。
                ApplyTaxRule(draft, latestRow, specialPhones);
                drafts.Add(draft);
            }

            return drafts;
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
            rows.AddRange(GetCombinedRowsBySource("tact", startDate, endDate));
            rows.AddRange(GetCombinedRowsBySource("ftz", startDate, endDate));
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
            // step 1: 先抓出這個時間區間內已出倉的清關資料，作為本次處理的主集合。
            var clearances = DataCenterDb.ClearanceInfos
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
                })
                .ToList();

            if (!clearances.Any())
            {
                return new List<CombinedRow>();
            }

            // step 2: 從清關資料整理主提單號與袋號，作為後續稅單、原始單查詢條件。
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

            // step 3: 依來源分開查詢稅單資料，再轉成 dictionary，避免在 SQL 端做過大的 join。
            var taxes = dataType == "tact"
                ? GetTactTaxes(mainNumbers, candidateBagNumbers)
                : GetFtzTaxes(mainNumbers, candidateBagNumbers);

            var taxLookup = taxes
                .GroupBy(x => BuildCompositeKey(x.MainNumber, x.BagNumber))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.TaxNumber).ThenBy(y => y.BagNumber).ToList());

            // step 4: 撈出原始單資料並建立袋號 / tracking_ub 對照，供後續回填提單與收件資訊。
            var originals = GetOriginalLists(mainNumbers, candidateBagNumbers);
            var originalByBag = originals
                .Where(x => !string.IsNullOrWhiteSpace(x.BagNo))
                .GroupBy(x => BuildCompositeKey(x.MainNumber, x.BagNo))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.TrackingNo).ThenBy(y => y.Id).ToList());

            var originalByTrackingUb = originals
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingUb))
                .GroupBy(x => BuildCompositeKey(x.MainNumber, x.TrackingUb))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.TrackingNo).ThenBy(y => y.Id).ToList());

            // step 5: 建立客戶主檔 dictionary，最後逐筆把清關、稅單、原始單組成一筆完整資料。
            var customerLookup = BuildCustomerLookup(originals);
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
                    var customer = FindCustomer(customerLookup, original?.DespatchNo, original?.TransTaxPayment);

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
            return result
                .GroupBy(x => x.TaxNumber)
                .Select(x => x.OrderBy(y => y.TrackingNo ?? y.BagNumber).First())
                .ToList();
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
            var rows = new List<TaxLookupRow>();

            foreach (var batch in Batch(mainNumbers, BatchSize))
            {
                // 分批查詢避免 Contains 清單過大，保留在記憶體端過濾候選袋號。
                var items = DataCenterDb.EtlTactTaxes
                    .AsNoTracking()
                    .Where(x => batch.Contains(x.MainNumber))
                    .Select(x => new TaxLookupRow
                    {
                        MainNumber = x.MainNumber,
                        BagNumber = x.BagNumber,
                        TaxNumber = x.TaxNumber,
                        TaxAmount = x.TaxAmount.HasValue ? x.TaxAmount.Value.ToString() : string.Empty,
                        TaxBase = x.TaxBase
                    })
                    .ToList();

                rows.AddRange(items.Where(x => bagNumberSet.Contains(x.BagNumber ?? string.Empty)));
            }

            return rows;
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
            var rows = new List<TaxLookupRow>();

            foreach (var batch in Batch(mainNumbers, BatchSize))
            {
                var items = DataCenterDb.EtlFtzTaxes
                    .AsNoTracking()
                    .Where(x => batch.Contains(x.MainNumber))
                    .Select(x => new TaxLookupRow
                    {
                        MainNumber = x.MainNumber,
                        BagNumber = x.BagNumber,
                        TaxNumber = x.TaxNumber,
                        TaxAmount = x.TaxAmount,
                        TaxBase = x.TaxBase
                    })
                    .ToList();

                rows.AddRange(items.Where(x => bagNumberSet.Contains(x.BagNumber ?? string.Empty)));
            }

            return rows;
        }

        /// <summary>
        /// 取得對應主提單號的原始單資料。
        /// </summary>
        /// <param name="mainNumbers">主提單號清單。</param>
        /// <param name="bagNumbers">候選袋號清單。</param>
        /// <returns>原始單資料。</returns>
        private List<OriginalListEntity> GetOriginalLists(List<string> mainNumbers, List<string> bagNumbers)
        {
            var bagNumberSet = new HashSet<string>(bagNumbers.Where(x => !string.IsNullOrWhiteSpace(x)));
            var rows = new List<OriginalListEntity>();

            foreach (var batch in Batch(mainNumbers, BatchSize))
            {
                // 原始單同樣先按主提單號批次抓，再用袋號 / tracking_ub 在記憶體端縮小範圍。
                var items = DataCenterDb.OriginalLists
                    .AsNoTracking()
                    .Where(x => batch.Contains(x.MainNumber))
                    .ToList();

                rows.AddRange(items.Where(x =>
                    bagNumberSet.Contains(x.BagNo ?? string.Empty) ||
                    bagNumberSet.Contains(x.TrackingUb ?? string.Empty)));
            }

            return rows;
        }

        /// <summary>
        /// 建立客戶主檔查詢 dictionary。
        /// </summary>
        /// <param name="originals">原始單資料。</param>
        /// <returns>客戶代號與派件公司對照表。</returns>
        private Dictionary<string, CustomerMasterEntity> BuildCustomerLookup(IEnumerable<OriginalListEntity> originals)
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

            var lookup = new Dictionary<string, CustomerMasterEntity>();
            if (!customerCodes.Any() || !transNos.Any())
            {
                return lookup;
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

                    foreach (var item in items)
                    {
                        lookup[BuildCompositeKey(item.CustId, item.TransNo)] = item;
                    }
                }
            }

            return lookup;
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
            return new HashSet<string>(JetfDb.CustomerSpecials
                .AsNoTracking()
                .Where(x => x.TranType == "空運")
                .Select(x => x.Phone)
                .ToList()
                .Select(NormalizeSpecialPhone)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

            /// <summary>
            /// 將草稿資料新增或更新到 FEE_MASTER，並保留舊資料 log。
            /// </summary>
            /// <param name="drafts">待寫入的草稿資料。</param>
            /// <param name="dataDate">資料日期。</param>
        private void SaveFeeMasters(List<FeeMasterDraft> drafts, string dataDate)
        {
            if (drafts == null || drafts.Count == 0)
            {
                return;
            }

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    // 先把現有資料載入成 lookup，後續可直接判斷新增或更新。
                    var existingRows = LoadExistingFeeMasters(drafts);
                    var existingLookup = existingRows.ToDictionary(x => BuildCompositeKey(x.MainNumber, x.TrackingNo));
                    var logEntities = new List<FeeMasterLogEntity>();
                    var updateTime = DateTime.Now;

                    foreach (var draft in drafts)
                    {
                        var key = BuildCompositeKey(draft.MainNumber, draft.TrackingNo);
                        if (existingLookup.TryGetValue(key, out var existingRow))
                        {
                            // 舊系統若已匯款成功(DLV_REMIT_CODE=Y)就不覆寫；其餘情況先寫 log 再更新主檔。
                            if (string.Equals(existingRow.DlvRemitCode, "Y", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            logEntities.Add(CreateFeeMasterLogEntity(existingRow, updateTime));
                            ApplyDraftToEntity(existingRow, draft, dataDate, updateTime);
                            continue;
                        }

                        JetfDb.FeeMasters.Add(CreateFeeMasterEntity(draft, dataDate));
                    }

                    if (logEntities.Count > 0)
                    {
                        // 舊資料先寫入 log，保留異動前快照。
                        JetfDb.FeeMasterLogs.AddRange(logEntities);
                    }

                    JetfDb.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 依本次草稿資料載入既有 FEE_MASTER。
        /// </summary>
        /// <param name="drafts">待比對的草稿資料。</param>
        /// <returns>既有 fee master 清單。</returns>
        private List<FeeMasterEntity> LoadExistingFeeMasters(IEnumerable<FeeMasterDraft> drafts)
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
            var rows = new List<FeeMasterEntity>();

            foreach (var mainBatch in Batch(mainNumbers, BatchSize))
            {
                foreach (var trackingBatch in Batch(trackingNos, BatchSize))
                {
                    var items = JetfDb.FeeMasters
                        .Where(x =>
                            x.SourceType == AirSourceType.ToString() &&
                            mainBatch.Contains(x.MainNumber) &&
                            trackingBatch.Contains(x.TrackingNo))
                        .ToList();

                    rows.AddRange(items.Where(x => keys.Contains(BuildCompositeKey(x.MainNumber, x.TrackingNo))));
                }
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
        /// 依客戶代號與物流代碼查找客戶主檔。
        /// </summary>
        /// <param name="lookup">客戶主檔對照表。</param>
        /// <param name="customerCode">客戶代號。</param>
        /// <param name="transNo">物流代碼。</param>
        /// <returns>符合條件的客戶主檔。</returns>
        private static CustomerMasterEntity FindCustomer(Dictionary<string, CustomerMasterEntity> lookup, string customerCode, string transNo)
        {
            if (lookup == null)
            {
                return null;
            }

            lookup.TryGetValue(BuildCompositeKey(PadCustomerCode(customerCode), transNo), out var customer);
            return customer;
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
        private static OriginalListEntity FindBestOriginal(
            Dictionary<string, List<OriginalListEntity>> originalByBag,
            Dictionary<string, List<OriginalListEntity>> originalByTrackingUb,
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
        /// 將草稿資料轉成新的 FEE_MASTER entity。
        /// </summary>
        /// <param name="draft">待寫入草稿。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <returns>新的 fee master entity。</returns>
        private static FeeMasterEntity CreateFeeMasterEntity(FeeMasterDraft draft, string dataDate)
        {
            return new FeeMasterEntity
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
                DlvInv = NormalizeText(draft.DlvInv),
                InDate = NormalizeText(draft.InDate),
                InDateTime = draft.InDateTime,
                OutDateTime = draft.OutDateTime,
                Combine = NormalizeText(draft.Combine),
                TaxBase = NormalizeText(draft.TaxBase),
                Tax1 = draft.Tax1,
                Tax2 = draft.Tax2,
                Cod = draft.Cod,
                Fee = draft.Fee,
                IncludeTax = NormalizeText(draft.IncludeTax),
                Recipient = NormalizeText(draft.Recipient),
                RecPhone = NormalizeText(draft.RecPhone),
                RecAddress = NormalizeText(draft.RecAddress),
                RecId = NormalizeText(draft.RecId),
                ToDlvCod = draft.ToDlvCod,
                DlvCom = NormalizeText(draft.DlvCom),
                Arrival = NormalizeText(draft.Arrival),
                CustomerCod = draft.CustomerCod,
                TransCod = draft.TransCod
            };
        }

        /// <summary>
        /// 將草稿資料覆寫到既有 FEE_MASTER entity。
        /// </summary>
        /// <param name="entity">既有 fee master。</param>
        /// <param name="draft">最新草稿。</param>
        /// <param name="dataDate">資料日期。</param>
        /// <param name="updateTime">更新時間。</param>
        private static void ApplyDraftToEntity(FeeMasterEntity entity, FeeMasterDraft draft, string dataDate, DateTime updateTime)
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
            entity.TaxBase = NormalizeText(draft.TaxBase);
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
            entity.ToDlvCod = draft.ToDlvCod;
            entity.DlvInv = NormalizeText(draft.DlvInv);
            entity.Arrival = NormalizeText(draft.Arrival);
            entity.CustomerCod = draft.CustomerCod;
            entity.TransCod = draft.TransCod;
            entity.UpdateDate = updateTime;
            entity.RecordFeeMaster = "0";
        }

        /// <summary>
        /// 建立 FEE_MASTER_LOG entity，保留更新前的主檔快照。
        /// </summary>
        /// <param name="row">既有 fee master 資料。</param>
        /// <param name="insTime">log 建立時間。</param>
        /// <returns>fee master log entity。</returns>
        private static FeeMasterLogEntity CreateFeeMasterLogEntity(FeeMasterEntity row, DateTime insTime)
        {
            return new FeeMasterLogEntity
            {
                Id = row.Id,
                InsTime = insTime,
                DataDate = row.DataDate,
                Source = row.Source,
                SourceType = row.SourceType,
                Type = row.Type,
                Customer = row.Customer,
                MainNumber = row.MainNumber,
                TrackingNo = row.TrackingNo,
                ClearanceNumber = row.ClearanceNumber,
                BagNumber = row.BagNumber,
                TaxNumber = row.TaxNumber,
                DlvInv = row.DlvInv,
                InDate = row.InDate,
                InDateTime = row.InDateTime,
                OutDateTime = row.OutDateTime,
                Combine = row.Combine,
                TaxBase = row.TaxBase,
                Tax1 = row.Tax1,
                Tax2 = row.Tax2,
                Ccfee = row.Ccfee,
                Cod = row.Cod,
                Fee = row.Fee,
                IncludeTax = row.IncludeTax,
                Recipient = row.Recipient,
                RecPhone = row.RecPhone,
                RecAddress = row.RecAddress,
                RecId = row.RecId,
                ToDlvCod = row.ToDlvCod,
                DlvCom = row.DlvCom,
                DlvComStn = row.DlvComStn,
                DlvCod = row.DlvCod,
                DlvCodCode = row.DlvCodCode,
                DlvCodTime = row.DlvCodTime,
                DlvCodOpe = row.DlvCodOpe,
                DlvRemitDate = row.DlvRemitDate,
                DlvRemitAmout = row.DlvRemitAmout,
                DlvRemitAmoutFee = row.DlvRemitAmoutFee,
                DlvRemitCode = row.DlvRemitCode,
                DlvRemitTime = row.DlvRemitTime,
                DlvRemitOpe = row.DlvRemitOpe,
                UpdateDate = row.UpdateDate,
                ModiftyDate = row.ModiftyDate,
                Download = row.Download,
                RecordFeeMaster = row.RecordFeeMaster,
                TaxPayer = row.TaxPayer,
                Arrival = row.Arrival,
                CustomerCod = row.CustomerCod,
                TransCod = row.TransCod
            };
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
        /// 判斷是否為舊系統定義的特殊空運客戶。
        /// </summary>
        /// <param name="company">派件公司。</param>
        /// <param name="phone">收件電話。</param>
        /// <param name="specialPhones">特殊客戶電話集合。</param>
        /// <returns>是否為特殊客戶。</returns>
        private static bool IsSpecialEtlCustomer(string company, string phone, HashSet<string> specialPhones)
        {
            var normalizedPhone = NormalizeSpecialPhone(phone);
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

        private sealed class StoredProcedureStatusRow
        {
            public string Status { get; set; }

            public string Message { get; set; }
        }

        private sealed class ClearanceLookupRow
        {
            public string DataType { get; set; }

            public string ClearanceType { get; set; }

            public string ClearanceNumber { get; set; }

            public DateTime? SignInTime { get; set; }

            public DateTime? SignOutTime { get; set; }

            public string MainNumber { get; set; }

            public string BagNumber { get; set; }

            public string MergeNumber { get; set; }
        }

        private sealed class TaxLookupRow
        {
            public string MainNumber { get; set; }

            public string BagNumber { get; set; }

            public string TaxNumber { get; set; }

            public string TaxAmount { get; set; }

            public string TaxBase { get; set; }
        }

        private sealed class CombinedRow
        {
            public string DataType { get; set; }

            public string ClearanceType { get; set; }

            public string ClearanceNumber { get; set; }

            public DateTime? SignInTime { get; set; }

            public DateTime? SignOutTime { get; set; }

            public string TaxNumber { get; set; }

            public string BagNumber { get; set; }

            public string MainNumber { get; set; }

            public string TaxAmount { get; set; }

            public string TaxBase { get; set; }

            public string Ecm { get; set; }

            public string BagNo { get; set; }

            public string DespatchNo { get; set; }

            public string Cc { get; set; }

            public string Recipient { get; set; }

            public string RecPhone { get; set; }

            public string RecAddress { get; set; }

            public string RecId { get; set; }

            public string TrackingNo { get; set; }

            public string DeliveryNo { get; set; }

            public string TransTaxPayment { get; set; }

            public string IncludeTax { get; set; }

            public int? CodFee { get; set; }

            public string Company { get; set; }

            public bool IsCainiaoP { get; set; }
        }

        private sealed class FeeMasterDraft
        {
            public string Source { get; set; }

            public string Type { get; set; }

            public string Customer { get; set; }

            public string MainNumber { get; set; }

            public string TrackingNo { get; set; }

            public string ClearanceNumber { get; set; }

            public string BagNumber { get; set; }

            public string TaxNumber { get; set; }

            public string DlvInv { get; set; }

            public string InDate { get; set; }

            public DateTime? InDateTime { get; set; }

            public DateTime? OutDateTime { get; set; }

            public string Combine { get; set; }

            public string TaxBase { get; set; }

            public int Tax1 { get; set; }

            public int Tax2 { get; set; }

            public int Cod { get; set; }

            public int Fee { get; set; }

            public string IncludeTax { get; set; }

            public string Recipient { get; set; }

            public string RecPhone { get; set; }

            public string RecAddress { get; set; }

            public string RecId { get; set; }

            public int ToDlvCod { get; set; }

            public string DlvCom { get; set; }

            public string Arrival { get; set; }

            public int CustomerCod { get; set; }

            public int TransCod { get; set; }
        }

        private sealed class TaxAmountSet
        {
            public int Tax1 { get; set; }

            public int Tax2 { get; set; }

            public int Cod { get; set; }

            public int Fee { get; set; }
        }

        private sealed class TaxCalculationResult
        {
            public int TransCod { get; set; }

            public int CustomerCod { get; set; }

            public int ToDlvCod { get; set; }
        }

        private sealed class ReportCustomerLookup
        {
            public string CustId { get; set; }

            public string TransNo { get; set; }

            public string TransName { get; set; }

            public string Company { get; set; }
        }
    }

    public sealed class DownloadEtlNewReportResult
    {
        public DownloadEtlNewReportResult()
        {
            status = Status.success;
            Rows = new List<DownloadEtlNewReportItem>();
        }

        public string status { get; set; }

        public string msg { get; set; } = string.Empty;

        public List<DownloadEtlNewReportItem> Rows { get; set; }
    }

    public sealed class DownloadEtlNewReportItem
    {
        public string BagNumber { get; set; }

        public string TrackingNo { get; set; }

        public int Tax1 { get; set; }

        public int Tax2 { get; set; }

        public int Fee { get; set; }

        public int Cod { get; set; }

        public int ToDlvCod { get; set; }

        public string Recipient { get; set; }

        public string RecPhone { get; set; }

        public string TransName { get; set; }

        public DateTime? OutDateTime { get; set; }

        public string IncludeTax { get; set; }

        public string DlvInv { get; set; }

        public string Customer { get; set; }

        public string TransNo { get; set; }

        public string Company { get; set; }
    }

}