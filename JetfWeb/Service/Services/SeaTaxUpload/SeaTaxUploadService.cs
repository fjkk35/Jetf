using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NLog;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Models;
using Service.Models.Tax;
using Service.Services.Tax;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.SeaTaxUpload
{
    /// <summary>
    /// 海運稅金資料上傳服務。
    /// </summary>
    public class SeaTaxUploadService : _BaseService
    {
        private const string SeaSourceType = "1";
        private const int CommandTimeoutSeconds = 600;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly DownloadService _downloadService;
        private readonly TaxService _taxService;

        public SeaTaxUploadService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext, DownloadService downloadService, TaxService taxService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _downloadService = downloadService;
            _taxService = taxService;
        }

        /// <summary>
        /// 上傳海運稅金檔案。
        /// </summary>
        /// <param name="dataDate">資料日期，格式 yyyyMMdd。</param>
        /// <param name="filePath">上傳檔案路徑。</param>
        /// <param name="taxType">稅金類型。</param>
        /// <param name="userId">操作人員。</param>
        /// <returns>處理結果。</returns>
        public ResponseModel UploadFile(string dataDate, string filePath, SeaTaxType taxType, string userId)
        {
            ConfigureCommandTimeout();

            Logger.Debug($"step1 開始: 讀取海運稅金 Excel，檔案={filePath}");
            var uploadRows = ReadExcelIpost(filePath);
            Logger.Debug($"step1 結束: 讀取海運稅金 Excel，筆數={uploadRows.Count}");

            var source = taxType.ToString();
            var uploadTime = DateTime.Now;
            List<SeaTaxModifyRow> modifyRows;

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    Logger.Debug($"step2 開始: 寫入 SeaTaxUpload 原始資料，筆數={uploadRows.Count}");
                    InsertSeaTaxUploads(JetfDb, uploadRows, uploadTime, userId);
                    Logger.Debug($"step2 結束: 寫入 SeaTaxUpload 原始資料，筆數={uploadRows.Count}");

                    Logger.Debug($"step3 開始: 查詢缺漏異動資料，資料日期={dataDate}，稅別={source}");
                    modifyRows = GetMissingModifyRows(JetfDb, dataDate, source, uploadTime, userId);
                    Logger.Debug($"step3 結束: 查詢缺漏異動資料，補遺筆數={modifyRows.Count}");

                    //測試中先註解掉，正式上線再移除註解
                    //Logger.Debug($"step4 開始: 重建 FeeMasterModify 快照，補遺筆數={modifyRows.Count}");
                    //RefreshFeeMasterModifySnapshot(JetfDb, DataCenterDb, modifyRows, dataDate);
                    //Logger.Debug($"step4 結束: 重建 FeeMasterModify 快照，補遺筆數={modifyRows.Count}");

                    Logger.Debug($"step5 開始: 將補遺資料回補至上傳集合與 SeaTaxUpload，補遺筆數={modifyRows.Count}");
                    AppendModifyRowsToUpload(JetfDb, uploadRows, modifyRows, uploadTime, userId);
                    Logger.Debug($"step5 結束: 回補完成，目前總上傳筆數={uploadRows.Count}");

                    JetfDb.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Error(ex, "step2-step5 失敗: 海運稅金上傳前置資料處理異常");
                    return CreateErrorResponse(ex.Message);
                }
            }

            //測試中先註解掉，正式上線再移除註解
            //Logger.Debug("step6 開始: 更新菜鳥海空運稅金方式");
            //var updateResponse = _downloadService.UpdateCainiaoTaxEdit();
            //if (updateResponse.status != Status.success)
            //{
            //    Logger.Debug($"step6 結束: 更新菜鳥海空運稅金方式失敗，訊息={updateResponse.msg}");
            //    return updateResponse;
            //}

            //Logger.Debug("step6 結束: 更新菜鳥海空運稅金方式成功");
            Logger.Debug($"step7 開始: 檢查可處理筆數，筆數={uploadRows.Count}");

            if (uploadRows.Count == 0)
            {
                Logger.Debug("step7 結束: 無可處理資料");
                return new ResponseModel
                {
                    status = Status.error,
                    msg = "上傳檔案筆數：0"
                };
            }

            Logger.Debug($"step7 結束: 可處理筆數={uploadRows.Count}");
            Logger.Debug("step8 開始: 組裝 FeeMaster 資料");
            var feeMasterRows = BuildFeeMasterRows(JetfDb, DataCenterDb, uploadRows, source, uploadTime, userId);
            Logger.Debug($"step8 結束: 組裝 FeeMaster 資料完成，筆數={feeMasterRows.Count}");

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    Logger.Debug($"step9 開始: 置換 FEE_MASTER_TEST，資料日期={dataDate}，來源={source}，筆數={feeMasterRows.Count}");
                    ReplaceFeeMaster(JetfDb, feeMasterRows, dataDate, source);
                    JetfDb.SaveChanges();
                    transaction.Commit();
                    Logger.Debug($"step9 結束: 置換 FEE_MASTER_TEST 完成，筆數={feeMasterRows.Count}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Error(ex, "step9 失敗: 置換 FEE_MASTER_TEST 異常");
                    return CreateErrorResponse(ex.Message);
                }
            }

            return new ResponseModel
            {
                status = Status.success,
                msg = $"上傳檔案筆數：{uploadRows.Count}"
            };
        }

        /// <summary>
        /// 建立統一的錯誤回傳格式。
        /// </summary>
        private static ResponseModel CreateErrorResponse(string message)
        {
            return new ResponseModel
            {
                status = Status.error,
                msg = message
            };
        }

        /// <summary>
        /// 統一設定這次上傳流程的資料庫 CommandTimeout。
        /// </summary>
        private void ConfigureCommandTimeout()
        {
            JetfDb.Database.CommandTimeout = CommandTimeoutSeconds;
            DataCenterDb.Database.CommandTimeout = CommandTimeoutSeconds;
        }

        /// <summary>
        /// 將 Excel 讀到的原始海運稅金資料批次寫入 SEA_TAX_UPLOAD。
        /// </summary>
        private void InsertSeaTaxUploads(
            JetfDbContext jetfDb,
            IEnumerable<SeaTaxUploadExcelRow> uploadRows,
            DateTime uploadTime,
            string userId)
        {
            var entities = (uploadRows ?? Enumerable.Empty<SeaTaxUploadExcelRow>())
                .Select(row => CreateSeaTaxUploadEntity(row, uploadTime, userId))
                .ToList();

            if (entities.Count == 0)
            {
                return;
            }

            jetfDb.BulkInsert(entities);
        }

        /// <summary>
        /// 查出當天清關異動但尚未出現在本次 SEA_TAX_UPLOAD 的補遺資料。
        /// </summary>
        private List<SeaTaxModifyRow> GetMissingModifyRows(
            JetfDbContext jetfDb,
            string dataDate,
            string taxType,
            DateTime uploadTime,
            string userId)
        {
            var startDate = DateTime.ParseExact($"{dataDate}000000", "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact($"{dataDate}235959", "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var sql = @"
select
    a.ROW_ID as Id,
    a.DATA_TYPE as DataType,
    a.MAIN_NUMBER as MainNumber,
    a.BAG_NUMBER as BagNumber,
    a.MERGE_NUMBER as MergeNumber,
    a.TAX_NUMBER as TaxNumber,
    a.TAX_BASE as TaxBase,
    a.TAX_AMOUNT as TaxAmount,
    a.FREQ_SIGN as FreqSign,
    a.STATUS as Status,
    a.MODIFY_SEQ as ModifySeq,
    a.MODIFY_FILE as ModifyFile,
    a.MODIFY_TIME as ModifyTime
from DATA_CENTER.dbo.CLEARANCE_TAX a
where a.DATA_TYPE = @DATA_TYPE
and a.MODIFY_TIME between @SDate and @EDate
and not exists (
    select 1
    from jetf.dbo.SEA_TAX_UPLOAD b
    where b.UPLOAD_TIME = @UPLOAD_TIME
    and b.UPLOAD_OPE = @UPLOAD_OPE
    and a.BAG_NUMBER = b.BL_NO
    and a.MAIN_NUMBER = b.MAIN_NUMBER)
";

            return jetfDb.Database.SqlQuery<SeaTaxModifyRow>(
                    sql,
                    new SqlParameter("@DATA_TYPE", taxType),
                    new SqlParameter("@SDate", startDate),
                    new SqlParameter("@EDate", endDate),
                    new SqlParameter("@UPLOAD_TIME", uploadTime),
                    new SqlParameter("@UPLOAD_OPE", userId))
                .ToList()
                .Select(item => new SeaTaxModifyRow
                {
                    Id = item.Id,
                    DataType = NormalizeText(item.DataType),
                    MainNumber = NormalizeText(item.MainNumber),
                    BagNumber = NormalizeText(item.BagNumber),
                    MergeNumber = NormalizeText(item.MergeNumber),
                    TaxNumber = NormalizeText(item.TaxNumber),
                    TaxBase = item.TaxBase,
                    TaxAmount = item.TaxAmount,
                    FreqSign = NormalizeText(item.FreqSign),
                    Status = NormalizeText(item.Status),
                    ModifySeq = item.ModifySeq,
                    ModifyFile = NormalizeText(item.ModifyFile),
                    ModifyTime = item.ModifyTime
                })
                .ToList();
        }

        /// <summary>
        /// 依補遺資料重建 FEE_MASTER_MODIFY 快照，供後續人工追蹤使用。
        /// </summary>
        private void RefreshFeeMasterModifySnapshot(
            JetfDbContext jetfDb,
            DataCenterDbContext dataCenterDb,
            List<SeaTaxModifyRow> modifyRows,
            string dataDate)
        {
            if (modifyRows == null || modifyRows.Count == 0)
            {
                return;
            }

            var dataType = modifyRows[0].DataType;
            var latestOrders = GetLatestSeaOrderLookup(
                dataCenterDb,
                modifyRows.Select(row => new UploadKey(row.MainNumber, row.BagNumber)).ToList());

            var existingRows = jetfDb.FeeMasterModifies
                .AsNoTracking()
                .Where(row => row.ModifyDataDate == dataDate && row.DataType == dataType)
                .ToList();

            if (existingRows.Count > 0)
            {
                jetfDb.BulkDelete(existingRows);
            }

            var snapshotRows = (
                from row in modifyRows
                join order in latestOrders
                    on new
                    {
                        MainNumber = NormalizeText(row.MainNumber),
                        BlNo = NormalizeText(row.BagNumber)
                    }
                    equals new
                    {
                        MainNumber = NormalizeText(order.MainNumber),
                        BlNo = NormalizeText(order.BlNo)
                    }
                    into orderGroup
                from order in orderGroup.DefaultIfEmpty()
                select CreateFeeMasterModifyEntity(row, order, dataDate))
                .ToList();

            if (snapshotRows.Count > 0)
            {
                jetfDb.BulkInsert(snapshotRows);
            }
        }

        /// <summary>
        /// 將補遺資料同步加回本次上傳集合，並補寫至 SEA_TAX_UPLOAD。
        /// </summary>
        private void AppendModifyRowsToUpload(
            JetfDbContext jetfDb,
            List<SeaTaxUploadExcelRow> uploadRows,
            IEnumerable<SeaTaxModifyRow> modifyRows,
            DateTime uploadTime,
            string userId)
        {
            var rows = (modifyRows ?? Enumerable.Empty<SeaTaxModifyRow>())
                .Select(row => new SeaTaxUploadExcelRow
                {
                    MainNumber = NormalizeText(row.MainNumber),
                    BlNo = NormalizeText(row.BagNumber),
                    Tax = row.TaxAmount.HasValue ? row.TaxAmount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    TaxNumber = NormalizeText(row.TaxNumber)
                })
                .ToList();

            if (rows.Count == 0)
            {
                return;
            }

            uploadRows.AddRange(rows);

            var entities = rows.Select(row => CreateSeaTaxUploadEntity(row, uploadTime, userId)).ToList();
            if (entities.Count > 0)
            {
                jetfDb.BulkInsert(entities);
            }
        }

        /// <summary>
        /// 將本次上傳資料轉成 FEE_MASTER_TEST 與 FEE_MASTER_DSTAIL 所需的資料結構。
        /// </summary>
        private List<SeaTaxFeeMasterRow> BuildFeeMasterRows(
            JetfDbContext jetfDb,
            DataCenterDbContext dataCenterDb,
            List<SeaTaxUploadExcelRow> uploadRows,
            string taxType,
            DateTime uploadTime,
            string userId)
        {
            Logger.Debug($"step8-1 開始: 使用 join 查詢清關、稅基與原單資料，來源筆數={uploadRows.Count}");
            var joinedRows = GetJoinedUploadRows(jetfDb, uploadTime, userId);
            Logger.Debug($"step8-1 結束: 完成 join 查詢，整併筆數={joinedRows.Count}");

            Logger.Debug("step8-2 開始: 建立客戶主檔與特殊客戶資料");
            var customerMasters = GetSeaCustomerLookup(jetfDb, joinedRows);
            var customerSpecialPhones = GetSeaSpecialPhoneSet(jetfDb);
            ApplyCustomerMasterValues(joinedRows, customerMasters);
            Logger.Debug($"step8-2 結束: 客戶主檔筆數={customerMasters.Count}，特殊客戶電話筆數={customerSpecialPhones.Count}");

            var feeMasterRows = new List<SeaTaxFeeMasterRow>();
            var groupedRows = (
                from joinedGroup in joinedRows.GroupBy(row => new
                {
                    MainNumber = NormalizeText(row.MainNumber),
                    BlNo = NormalizeText(row.BlNo)
                })
                join uploadGroup in uploadRows.GroupBy(row => new
                {
                    MainNumber = NormalizeText(row.MainNumber),
                    BlNo = NormalizeText(row.BlNo)
                })
                    on new
                    {
                        joinedGroup.Key.MainNumber,
                        joinedGroup.Key.BlNo
                    }
                    equals new
                    {
                        uploadGroup.Key.MainNumber,
                        uploadGroup.Key.BlNo
                    }
                    into uploadGroupMatch
                from uploadGroup in uploadGroupMatch.DefaultIfEmpty()
                select new
                {
                    Rows = joinedGroup.ToList(),
                    UploadCount = uploadGroup == null ? joinedGroup.Count() : uploadGroup.Count()
                })
                .ToList();

            foreach (var group in groupedRows)
            {
                // 同一主號/提單可能有多筆稅單，主表保留最新一筆，其餘稅額併入 Tax2，
                // 但 detail 仍需完整保留每一筆資料。
                var orderedRows = group.Rows
                    .OrderByDescending(row => row.SignOutTime ?? DateTime.MinValue)
                    .ThenByDescending(row => row.SignInTime ?? DateTime.MinValue)
                    .ToList();

                if (orderedRows.Count == 0)
                {
                    continue;
                }

                var groupCount = group.UploadCount;
                var effectiveRowCount = Math.Min(groupCount, orderedRows.Count);
                var detailSourceRows = orderedRows.Take(effectiveRowCount).ToList();
                if (detailSourceRows.Count == 0)
                {
                    continue;
                }

                var latestRow = detailSourceRows[0];
                feeMasterRows.Add(BuildMainFeeMasterRow(latestRow, detailSourceRows, groupCount, taxType, customerSpecialPhones));
            }

            return feeMasterRows;
        }

        /// <summary>
        /// 將客戶主檔資料回填到 joined rows，供主檔與明細計算代收邏輯使用。
        /// </summary>
        private static void ApplyCustomerMasterValues(
            IEnumerable<SeaTaxUploadJoinedRow> joinedRows,
            IEnumerable<CustomerMasterEntity> customerMasters)
        {
            var matchedRows = (
                from row in joinedRows ?? Enumerable.Empty<SeaTaxUploadJoinedRow>()
                join customerMaster in customerMasters ?? Enumerable.Empty<CustomerMasterEntity>()
                    on new
                    {
                        Customer = NormalizeText(row.DespatchName),
                        TransName = NormalizeText(row.TransTaxPayment)
                    }
                    equals new
                    {
                        Customer = NormalizeText(customerMaster.CustId),
                        TransName = NormalizeText(customerMaster.TransName)
                    }
                    into customerGroup
                from customerMaster in customerGroup.DefaultIfEmpty()
                select new
                {
                    Row = row,
                    CustomerMaster = customerMaster
                })
                .ToList();

            foreach (var item in matchedRows)
            {
                if (item.CustomerMaster == null)
                {
                    continue;
                }

                item.Row.CodFee = item.CustomerMaster.CodFee;
                item.Row.IncludeTax = NormalizeText(item.CustomerMaster.IncludeTax);
                item.Row.Company = NormalizeText(item.CustomerMaster.Company);
                item.Row.IsCainiaoP = item.CustomerMaster.IsCainiaoP;
            }
        }

        /// <summary>
        /// 以本次 SEA_TAX_UPLOAD 為起點，串接清關、原單與稅基資料。
        /// </summary>
        private List<SeaTaxUploadJoinedRow> GetJoinedUploadRows(
            JetfDbContext jetfDb,
            DateTime uploadTime,
            string userId)
        {
            var sql = @"
with CTE_SEA_ORDER_ORIGINAL as 
(
        select MAINNUMBER,BL_NO,DESPATCH_NAME as DespatchName,TRANS_TAXPAYMENT,IMPORTER,IM_PHONENO as ImporterPhone,IM_ADD as ImporterAddr,IMPORTER_ID,JETF_SERIAL,CC,MEMO,ARRIVAL
    from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL a
    where GW > 0 and MODIFTYDATE = (
        select MAX(MODIFTYDATE)
        from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL
        where BL_NO = a.BL_NO and MAINNUMBER = a.MAINNUMBER)
),
CTE_ETL_TIPC_TAX as
(
    select ROW_ID,MAIN_NUMBER,BAG_NUMBER,TAX_NUMBER,TAX_BASE,TAX_AMOUNT
    from DATA_CENTER.dbo.ETL_TIPC_TAX a
    where ROW_ID = (
        select MAX(ROW_ID)
        from DATA_CENTER.dbo.ETL_TIPC_TAX
        where MAIN_NUMBER = a.MAIN_NUMBER and BAG_NUMBER = a.BAG_NUMBER)
)
select
    a.BL_NO as BlNo,
    a.CLEARANCE_NUMBER as ClearanceNumber,
    a.CLEARANCE_TYPE as ClearanceType,
    a.TAX as Tax,
    a.TAX_NUMBER as TaxNumber,
    a.MAIN_NUMBER as MainNumber,
    b.SIGN_IN_TIME as SignInTime,
    b.SIGN_OUT_TIME as SignOutTime,
    d.TAX_BASE as TaxBase,
    cast(null as int) as CodFee,
    cast(null as nvarchar(10)) as IncludeTax,
    cast(null as nvarchar(50)) as Company,
    cast(null as bit) as IsCainiaoP,
    a.TAX_PAYER as TaxPayer,
    a.TAX_RECID as TaxRecId,
    c.DespatchName,
    c.TRANS_TAXPAYMENT as TransTaxPayment,
    c.IMPORTER as Importer,
    c.ImporterPhone,
    c.ImporterAddr,
    c.IMPORTER_ID as ImporterId,
    c.JETF_SERIAL as JetfSerial,
    c.CC as Cod,
    c.MEMO as Memo,
    c.ARRIVAL as Arrival
from jetf.dbo.SEA_TAX_UPLOAD a
left join DATA_CENTER.dbo.CLEARANCE_INFO b on a.MAIN_NUMBER = b.MAIN_NUMBER and a.BL_NO = b.BAG_NUMBER
left join CTE_SEA_ORDER_ORIGINAL c on a.MAIN_NUMBER = c.MAINNUMBER and a.BL_NO = c.BL_NO
left join CTE_ETL_TIPC_TAX d on b.MAIN_NUMBER = d.MAIN_NUMBER and b.BAG_NUMBER = d.BAG_NUMBER
where a.UPLOAD_TIME = @UPLOAD_TIME and a.UPLOAD_OPE = @UPLOAD_OPE
";

            return jetfDb.Database.SqlQuery<SeaTaxUploadJoinedRow>(
                    sql,
                    new SqlParameter("@UPLOAD_TIME", uploadTime),
                    new SqlParameter("@UPLOAD_OPE", userId))
                .ToList()
                .Select(row => new SeaTaxUploadJoinedRow
                {
                    BlNo = NormalizeText(row.BlNo),
                    ClearanceNumber = NormalizeText(row.ClearanceNumber),
                    ClearanceType = NormalizeText(row.ClearanceType),
                    Tax = NormalizeText(row.Tax),
                    TaxNumber = NormalizeText(row.TaxNumber),
                    MainNumber = NormalizeText(row.MainNumber),
                    SignInTime = row.SignInTime,
                    SignOutTime = row.SignOutTime,
                    TaxBase = NormalizeText(row.TaxBase),
                    CodFee = row.CodFee,
                    IncludeTax = NormalizeText(row.IncludeTax),
                    Company = NormalizeText(row.Company),
                    IsCainiaoP = row.IsCainiaoP,
                    TaxPayer = NormalizeText(row.TaxPayer),
                    TaxRecId = NormalizeText(row.TaxRecId),
                    DespatchName = NormalizeText(row.DespatchName),
                    TransTaxPayment = NormalizeText(row.TransTaxPayment),
                    Importer = NormalizeText(row.Importer),
                    ImporterPhone = NormalizeText(row.ImporterPhone),
                    ImporterAddr = NormalizeText(row.ImporterAddr),
                    ImporterId = NormalizeText(row.ImporterId),
                    JetfSerial = NormalizeText(row.JetfSerial),
                    Cod = row.Cod,
                    Memo = NormalizeText(row.Memo),
                    Arrival = NormalizeText(row.Arrival)
                })
                .ToList();
        }

        /// <summary>
        /// 依主號與提單抓取最新的海運原單資料。
        /// </summary>
        private static List<SeaOrderOriginalEntity> GetLatestSeaOrderLookup(
            DataCenterDbContext dataCenterDb,
            List<UploadKey> uploadKeys)
        {
            var normalizedKeys = (uploadKeys ?? new List<UploadKey>())
                .Select(row => new
                {
                    MainNumber = NormalizeText(row.MainNumber),
                    BlNo = NormalizeText(row.BlNo)
                })
                .Where(row => !string.IsNullOrWhiteSpace(row.MainNumber) && !string.IsNullOrWhiteSpace(row.BlNo))
                .Distinct()
                .ToList();

            if (normalizedKeys.Count == 0)
            {
                return new List<SeaOrderOriginalEntity>();
            }

            var mainNumbers = normalizedKeys.Select(row => row.MainNumber).Distinct().ToList();
            var bagNumbers = normalizedKeys.Select(row => row.BlNo).Distinct().ToList();

            return (from row in dataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .Where(row => row.Gw.HasValue && row.Gw.Value > 0 && mainNumbers.Contains(row.MainNumber) && bagNumbers.Contains(row.BlNo))
                .ToList()
                    join key in normalizedKeys
                        on new
                        {
                            MainNumber = NormalizeText(row.MainNumber),
                            BlNo = NormalizeText(row.BlNo)
                        }
                        equals new
                        {
                            key.MainNumber,
                            key.BlNo
                        }
                    select row)
                .GroupBy(row => new
                {
                    MainNumber = NormalizeText(row.MainNumber),
                    BlNo = NormalizeText(row.BlNo)
                })
                .Select(group => group
                    .OrderByDescending(row => row.ModifyDate ?? DateTime.MinValue)
                    .ThenByDescending(row => row.Id)
                    .First())
                .ToList();
        }

        /// <summary>
        /// 查出這批海運資料可能用到的客戶主檔。
        /// </summary>
        private static List<CustomerMasterEntity> GetSeaCustomerLookup(
            JetfDbContext jetfDb,
            IEnumerable<SeaTaxUploadJoinedRow> joinedRows)
        {
            var customerCodes = (joinedRows ?? Enumerable.Empty<SeaTaxUploadJoinedRow>())
                .Select(row => NormalizeText(row.DespatchName))
                .Where(row => !string.IsNullOrWhiteSpace(row))
                .Distinct()
                .ToList();
            var transNames = (joinedRows ?? Enumerable.Empty<SeaTaxUploadJoinedRow>())
                .Select(row => NormalizeText(row.TransTaxPayment))
                .Where(row => !string.IsNullOrWhiteSpace(row))
                .Distinct()
                .ToList();

            if (!customerCodes.Any() || !transNames.Any())
            {
                return new List<CustomerMasterEntity>();
            }

            return jetfDb.CustomerMasters
                .AsNoTracking()
                .Where(row => row.TranType == "海運" && customerCodes.Contains(row.CustId) && transNames.Contains(row.TransName))
                .ToList();
        }

        /// <summary>
        /// 依包稅方式與客戶條件計算主表的代收邏輯。
        /// </summary>
        private void ApplyTaxRule(
            SeaTaxFeeMasterRow feeMasterRow,
            SeaTaxUploadJoinedRow latestRow,
            IEnumerable<string> customerSpecialPhones)
        {
            var taxCalculationInput = CreateTaxCalculationInput(feeMasterRow);
            var includeTax = NormalizeText(latestRow.IncludeTax);
            var memo = NormalizeText(feeMasterRow.Memo);
            var company = NormalizeText(latestRow.Company);
            var recPhone = NormalizeText(feeMasterRow.RecPhone).Trim();

            if (includeTax == "Y")
            {
                var taxData = _taxService.GetTaxY(taxCalculationInput);
                feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (latestRow.IsCainiaoP.GetValueOrDefault())
            {
                var taxData = _taxService.GetTaxP(taxCalculationInput);
                ApplyCainiaoPTaxRule(feeMasterRow, taxData);
                return;
            }

            if (includeTax == "D" || _taxService.IsSeaSpecial(customerSpecialPhones, company, recPhone))
            {
                var taxData = _taxService.GetTaxD(taxCalculationInput);
                feeMasterRow.IncludeTax = "D";
                feeMasterRow.Fee = "0";
                feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (includeTax == "C" || memo.IndexOf("DDP", StringComparison.OrdinalIgnoreCase) > -1)
            {
                var taxData = _taxService.GetTaxC(taxCalculationInput);
                feeMasterRow.IncludeTax = "C";
                feeMasterRow.Fee = "0";
                feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                return;
            }

            var defaultTaxData = _taxService.GetTaxN(taxCalculationInput);
            feeMasterRow.TransCod = defaultTaxData.TransCod.ToString(CultureInfo.InvariantCulture);
            feeMasterRow.CustomerCod = defaultTaxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
            feeMasterRow.ToDlvCod = defaultTaxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 建立主檔資料。
        /// step1 先決定主檔要保留哪一筆資料。
        /// step2 再計算主檔的 Tax1、Tax2、Combine 與代收邏輯。
        /// step3 最後再把同組的明細逐筆建立出來。
        /// </summary>
        private SeaTaxFeeMasterRow BuildMainFeeMasterRow(
            SeaTaxUploadJoinedRow latestRow,
            List<SeaTaxUploadJoinedRow> detailRows,
            int groupCount,
            string taxType,
            IEnumerable<string> customerSpecialPhones)
        {
            var feeMasterRow = new SeaTaxFeeMasterRow
            {
                Source = taxType,
                Type = latestRow.ClearanceType,
                Customer = latestRow.DespatchName,
                MainNumber = latestRow.MainNumber,
                TrackingNo = latestRow.BlNo,
                ClearanceNumber = latestRow.ClearanceNumber,
                TaxNumber = latestRow.TaxNumber,
                TaxBase = latestRow.TaxBase,
                TaxRecId = latestRow.TaxRecId,
                TaxPayer = latestRow.TaxPayer,
                Fee = ToNullableIntText(latestRow.CodFee),
                IncludeTax = latestRow.IncludeTax,
                DlvCom = ConvertLanguage(latestRow.TransTaxPayment, "Big5"),
                Recipient = latestRow.Importer,
                RecPhone = latestRow.ImporterPhone,
                RecAddress = latestRow.ImporterAddr,
                RecId = Truncate(latestRow.ImporterId, 20),
                DlvInv = latestRow.JetfSerial,
                Cod = ToNullableIntText(latestRow.Cod),
                Memo = latestRow.Memo,
                Arrival = latestRow.Arrival
            };

            if (latestRow.SignInTime.HasValue)
            {
                feeMasterRow.InDate = latestRow.SignInTime.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                feeMasterRow.InDateTime = latestRow.SignInTime.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            if (latestRow.SignOutTime.HasValue)
            {
                feeMasterRow.OutDateTime = latestRow.SignOutTime.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            feeMasterRow.Tax1 = latestRow.Tax;
            if (groupCount > 1)
            {
                feeMasterRow.Combine = "Y";
                feeMasterRow.Tax2 = detailRows
                    .Skip(1)
                    .Sum(row => ParseNullableInt(row.Tax) ?? 0)
                    .ToString(CultureInfo.InvariantCulture);
            }

            ApplyTaxRule(feeMasterRow, latestRow, customerSpecialPhones);
            feeMasterRow.DetailRows = CreateFeeMasterDetailRows(detailRows, customerSpecialPhones);
            return feeMasterRow;
        }

        /// <summary>
        /// 將目前主表資料轉成稅額計算所需的輸入模型。
        /// </summary>
        private static TaxCalculationInput CreateTaxCalculationInput(SeaTaxFeeMasterRow feeMasterRow)
        {
            return CreateTaxCalculationInput(
                ParseNullableInt(feeMasterRow.Tax1) ?? 0,
                ParseNullableInt(feeMasterRow.Tax2) ?? 0,
                ParseNullableInt(feeMasterRow.Cod) ?? 0,
                ParseNullableInt(feeMasterRow.Fee) ?? 0);
        }

        /// <summary>
        /// 建立稅額計算輸入模型。
        /// </summary>
        private static TaxCalculationInput CreateTaxCalculationInput(int tax1, int tax2, int cod, int fee)
        {
            return new TaxCalculationInput
            {
                Tax1 = tax1,
                Tax2 = tax2,
                Cod = cod,
                Fee = fee
            };
        }

        /// <summary>
        /// 取得海運特殊客戶電話集合，供 D 類稅金判斷使用。
        /// </summary>
        private static HashSet<string> GetSeaSpecialPhoneSet(JetfDbContext jetfDb)
        {
            var phones = jetfDb.CustomerSpecials
                .AsNoTracking()
                .Where(row => row.TranType == "海運")
                .Select(row => row.Phone)
                .ToList();

            return new HashSet<string>(
                phones.Select(NormalizeText).Where(row => !string.IsNullOrWhiteSpace(row)),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 建立同一主單下所有 detail rows。
        /// 這個方法只處理明細資料，不處理主檔資料的組裝與計算。
        /// </summary>
        private List<SeaTaxFeeMasterDetailRow> CreateFeeMasterDetailRows(
            IEnumerable<SeaTaxUploadJoinedRow> detailRows,
            IEnumerable<string> customerSpecialPhones)
        {
            var sourceRows = (detailRows ?? Enumerable.Empty<SeaTaxUploadJoinedRow>()).ToList();
            if (sourceRows.Count == 0)
            {
                return new List<SeaTaxFeeMasterDetailRow>();
            }

            if (sourceRows[0].IsCainiaoP.GetValueOrDefault())
            {
                return CreateCainiaoPDetailRows(sourceRows);
            }

            return sourceRows
                .Select(row => CreateRegularDetailRow(row, customerSpecialPhones))
                .ToList();
        }

        /// <summary>
        /// 建立一般明細資料。
        /// </summary>
        private SeaTaxFeeMasterDetailRow CreateRegularDetailRow(
            SeaTaxUploadJoinedRow row,
            IEnumerable<string> customerSpecialPhones)
        {
            var taxAmount = ParseNullableInt(row.Tax) ?? 0;
            var codAmount = ParseNullableInt(row.Cod) ?? 0;
            var feeAmount = ParseNullableInt(row.CodFee) ?? 0;
            var detailFee = feeAmount;
            var taxCalculationInput = CreateTaxCalculationInput(taxAmount, 0, codAmount, feeAmount);
            var includeTax = NormalizeText(row.IncludeTax);
            var memo = NormalizeText(row.Memo);
            var company = NormalizeText(row.Company);
            var recPhone = NormalizeText(row.ImporterPhone).Trim();
            TaxData taxData;

            if (includeTax == "Y")
            {
                taxData = _taxService.GetTaxY(taxCalculationInput);
            }
            else if (includeTax == "D" || _taxService.IsSeaSpecial(customerSpecialPhones, company, recPhone))
            {
                taxData = _taxService.GetTaxD(taxCalculationInput);
                detailFee = 0;
            }
            else if (includeTax == "C" || memo.IndexOf("DDP", StringComparison.OrdinalIgnoreCase) > -1)
            {
                taxData = _taxService.GetTaxC(taxCalculationInput);
                detailFee = 0;
            }
            else
            {
                taxData = _taxService.GetTaxN(taxCalculationInput);
            }

            return CreateFeeMasterDetailRow(row, detailFee, taxData.ToDlvCod);
        }

        /// <summary>
        /// 建立菜鳥 P 明細資料。
        /// step1 先用客戶可吸收的 1000 額度逐筆往下扣。
        /// step2 額度扣完後，超出的稅額才轉成派件公司代收稅額。
        /// step3 手續費只放在第一筆真的有代收稅額的明細，避免重複帶入。
        /// </summary>
        private List<SeaTaxFeeMasterDetailRow> CreateCainiaoPDetailRows(
            IEnumerable<SeaTaxUploadJoinedRow> detailRows)
        {
            var sourceRows = (detailRows ?? Enumerable.Empty<SeaTaxUploadJoinedRow>()).ToList();
            var result = new List<SeaTaxFeeMasterDetailRow>();
            var remainingCustomerTax = 1000;
            var feeAssigned = false;

            foreach (var row in sourceRows)
            {
                var taxAmount = ParseNullableInt(row.Tax) ?? 0;
                var codAmount = ParseNullableInt(row.Cod) ?? 0;
                var feeAmount = ParseNullableInt(row.CodFee) ?? 0;

                // step1: 客戶可吸收的 1000 額度先從當前稅額扣除。
                var customerTax = Math.Min(Math.Max(remainingCustomerTax, 0), taxAmount);

                // step2: 超過剩餘額度的部分，才轉成派件公司代收稅額。
                var transTax = taxAmount - customerTax;

                remainingCustomerTax -= customerTax;

                var detailFee = 0;
                if (transTax > 0 && !feeAssigned)
                {
                    // step3: 手續費只掛在第一筆實際有代收稅額的明細。
                    detailFee = feeAmount;
                    feeAssigned = true;
                }

                result.Add(CreateFeeMasterDetailRow(row, detailFee, codAmount + transTax + detailFee));
            }

            return result;
        }

        /// <summary>
        /// 將單筆 joined row 與已計算完成的明細結果轉成 detail row。
        /// </summary>
        private static SeaTaxFeeMasterDetailRow CreateFeeMasterDetailRow(
            SeaTaxUploadJoinedRow row,
            int feeAmount,
            int toDlvCod)
        {
            return new SeaTaxFeeMasterDetailRow
            {
                MainNumber = row.MainNumber,
                TrackingNo = row.BlNo,
                ClearanceNumber = row.ClearanceNumber,
                BagNumber = row.BlNo,
                TaxNumber = row.TaxNumber,
                TaxPayer = row.TaxPayer,
                TaxRecId = row.TaxRecId,
                DlvInv = row.JetfSerial,
                TaxBase = row.TaxBase,
                Tax = row.Tax,
                Ccfee = string.Empty,
                Cod = ToNullableIntText(row.Cod),
                Fee = feeAmount.ToString(CultureInfo.InvariantCulture),
                Recipient = row.Importer,
                RecPhone = row.ImporterPhone,
                RecAddress = row.ImporterAddr,
                ToDlvCod = toDlvCod.ToString(CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// 套用菜鳥 P 計算結果到主表或明細暫存列。
        /// </summary>
        private static void ApplyCainiaoPTaxRule(SeaTaxFeeMasterRow feeMasterRow, TaxData taxData)
        {
            feeMasterRow.IncludeTax = taxData.TransCod > 0 ? "N" : feeMasterRow.IncludeTax;
            feeMasterRow.Fee = taxData.TransCod > 0 ? feeMasterRow.Fee : "0";
            feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
            feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
            feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 以同資料日期/來源整批覆蓋 FEE_MASTER_TEST 與 FEE_MASTER_DSTAIL。
        /// </summary>
        private void ReplaceFeeMaster(
            JetfDbContext jetfDb,
            List<SeaTaxFeeMasterRow> feeMasterRows,
            string dataDate,
            string source)
        {
            if (feeMasterRows == null || feeMasterRows.Count == 0)
            {
                return;
            }

            var existingRows = jetfDb.FeeMasterTests
                .AsNoTracking()
                .Where(row => row.DataDate == dataDate && row.Source == source && row.SourceType == SeaSourceType)
                .ToList();

            // step1: 先刪除同資料日期/來源的舊明細與舊主檔，確保這次上傳會整批覆蓋。
            if (existingRows.Count > 0)
            {
                var existingIds = existingRows.Select(row => row.Id).ToList();
                var existingDetailRows = jetfDb.FeeMasterDstails
                    .Where(row => existingIds.Contains(row.FeeMasterId))
                    .ToList();

                // 先刪 detail 再刪 master，避免留下舊關聯資料。
                if (existingDetailRows.Count > 0)
                {
                    jetfDb.BulkDelete(existingDetailRows);
                }

                jetfDb.BulkDelete(existingRows);
            }

            // step2: 寫入新的主檔，取得主檔 Id 後，再把對應明細一併寫入。
            var entities = feeMasterRows.Select(row => CreateFeeMasterEntity(row, dataDate)).ToList();
            if (entities.Count > 0)
            {
                jetfDb.BulkInsert(entities, operation => operation.AutoMapOutputDirection = true);

                var detailEntities = feeMasterRows
                    .SelectMany((row, index) => row.DetailRows.Select(detail => CreateFeeMasterDstailEntity(detail, entities[index].Id)))
                    .ToList();

                if (detailEntities.Count > 0)
                {
                    jetfDb.BulkInsert(detailEntities);
                }
            }
        }

        /// <summary>
        /// 建立 FEE_MASTER_TEST 寫入實體。
        /// </summary>
        private static FeeMasterTestEntity CreateFeeMasterEntity(SeaTaxFeeMasterRow row, string dataDate)
        {
            return new FeeMasterTestEntity
            {
                DataDate = dataDate,
                Source = NormalizeText(row.Source),
                SourceType = SeaSourceType,
                Type = NormalizeText(row.Type),
                Customer = NormalizeText(row.Customer),
                MainNumber = NormalizeText(row.MainNumber),
                TrackingNo = NormalizeText(row.TrackingNo),
                ClearanceNumber = NormalizeText(row.ClearanceNumber),
                Combine = NormalizeText(row.Combine),
                InDate = NormalizeText(row.InDate),
                InDateTime = ParseDateTime(row.InDateTime),
                OutDateTime = ParseDateTime(row.OutDateTime),
                TaxBase = ParseNullableInt(row.TaxBase),
                Tax1 = ParseNullableInt(row.Tax1),
                Tax2 = ParseNullableInt(row.Tax2),
                DlvCom = NormalizeText(row.DlvCom),
                TaxNumber = NormalizeText(row.TaxNumber),
                Fee = ParseNullableInt(row.Fee),
                IncludeTax = NormalizeText(row.IncludeTax),
                Recipient = NormalizeText(row.Recipient),
                RecPhone = NormalizeText(row.RecPhone),
                RecAddress = NormalizeText(row.RecAddress),
                RecId = NormalizeText(row.RecId),
                Cod = ParseNullableInt(row.Cod),
                ToDlvCod = NormalizeText(row.ToDlvCod),
                DlvInv = NormalizeText(row.DlvInv),
                TaxPayer = NormalizeText(row.TaxPayer),
                Arrival = NormalizeText(row.Arrival),
                CustomerCod = ParseNullableInt(row.CustomerCod),
                TransCod = ParseNullableInt(row.TransCod),
                TaxRecId = NormalizeText(row.TaxRecId)
            };
        }

        /// <summary>
        /// 建立 FEE_MASTER_MODIFY 寫入實體。
        /// </summary>
        private static FeeMasterModifyEntity CreateFeeMasterModifyEntity(
            SeaTaxModifyRow row,
            SeaOrderOriginalEntity seaOrder,
            string dataDate)
        {
            return new FeeMasterModifyEntity
            {
                ModifyDataDate = dataDate,
                Id = row.Id,
                DataType = NormalizeText(row.DataType),
                MainNumber = NormalizeText(row.MainNumber),
                BagNumber = NormalizeText(row.BagNumber),
                MergeNumber = NormalizeText(row.MergeNumber),
                TaxNumber = NormalizeText(row.TaxNumber),
                TaxBase = row.TaxBase,
                TaxAmount = row.TaxAmount,
                FreqSign = NormalizeText(row.FreqSign),
                Status = NormalizeText(row.Status),
                ModifySeq = row.ModifySeq,
                ModifyFile = NormalizeText(row.ModifyFile),
                ModifyTime = row.ModifyTime,
                JetfSerial = NormalizeText(seaOrder?.JetfSerial)
            };
        }

        /// <summary>
        /// 建立 FEE_MASTER_DSTAIL 寫入實體。
        /// </summary>
        private static FeeMasterDstailEntity CreateFeeMasterDstailEntity(SeaTaxFeeMasterDetailRow row, int feeMasterId)
        {
            return new FeeMasterDstailEntity
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
                TaxBase = ParseNullableInt(row.TaxBase),
                Tax = ParseNullableInt(row.Tax),
                Ccfee = ParseNullableInt(row.Ccfee),
                Cod = ParseNullableInt(row.Cod),
                Fee = ParseNullableInt(row.Fee),
                Recipient = NormalizeText(row.Recipient),
                RecPhone = NormalizeText(row.RecPhone),
                RecAddress = NormalizeText(row.RecAddress),
                ToDlvCod = NormalizeText(row.ToDlvCod)
            };
        }

        /// <summary>
        /// 建立 SEA_TAX_UPLOAD 寫入實體。
        /// </summary>
        private static SeaTaxUploadEntity CreateSeaTaxUploadEntity(
            SeaTaxUploadExcelRow row,
            DateTime uploadTime,
            string userId)
        {
            return new SeaTaxUploadEntity
            {
                MainNumber = NormalizeText(row.MainNumber),
                ClearanceNumber = NormalizeText(row.ClearanceNumber),
                ClearanceType = NormalizeText(row.ClearanceType),
                BlNo = NormalizeText(row.BlNo),
                RegNo = NormalizeText(row.RegNo),
                Mainfest = NormalizeText(row.Mainfest),
                TaxNumber = NormalizeText(row.TaxNumber),
                Tax = NormalizeText(row.Tax),
                PrtTime = row.PrtTime,
                UploadTime = uploadTime,
                UploadOpe = NormalizeText(userId),
                TaxPayer = NormalizeText(row.TaxPayer),
                TaxRecId = NormalizeText(row.TaxRecId)
            };
        }

        /// <summary>
        /// 讀取上傳 Excel，轉成系統內部使用的海運稅金列資料。
        /// </summary>
        private List<SeaTaxUploadExcelRow> ReadExcelIpost(string filePath)
        {
            var result = new List<SeaTaxUploadExcelRow>();
            var read = false;

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IWorkbook workBook = new XSSFWorkbook(fileStream);
                var sheet = workBook.GetSheetAt(0);

                for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null)
                    {
                        continue;
                    }

                    var clearanceNumber = GetCellText(row, 3);
                    if (clearanceNumber == "報單號碼")
                    {
                        read = true;
                        continue;
                    }

                    if (!read)
                    {
                        continue;
                    }

                    var item = new SeaTaxUploadExcelRow
                    {
                        MainNumber = GetCellText(row, 1),
                        BlNo = GetCellText(row, 2),
                        ClearanceNumber = clearanceNumber,
                        ClearanceType = GetCellText(row, 4),
                        TaxNumber = GetCellText(row, 6),
                        TaxRecId = GetCellText(row, 7),
                        TaxPayer = GetCellText(row, 8),
                        Tax = GetCellText(row, 12)
                    };

                    if (!string.IsNullOrWhiteSpace(item.ClearanceNumber) &&
                        !string.IsNullOrWhiteSpace(item.BlNo) &&
                        !string.IsNullOrWhiteSpace(item.Tax))
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 取得指定欄位的文字內容。
        /// </summary>
        private static string GetCellText(IRow row, int index)
        {
            var cell = row.GetCell(index);
            return cell == null ? string.Empty : cell.ToString().Trim();
        }

        /// <summary>
        /// 一般文字欄位的標準化處理。
        /// </summary>
        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// 將字串安全轉成 nullable int。
        /// </summary>
        private static int? ParseNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Replace(",", string.Empty).Trim();
            if (int.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
            {
                return intValue;
            }

            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            {
                return decimal.ToInt32(decimal.Truncate(decimalValue));
            }

            return null;
        }

        /// <summary>
        /// 將 decimal 安全轉成 nullable int。
        /// </summary>
        private static int? ParseNullableInt(decimal? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return decimal.ToInt32(decimal.Truncate(value.Value));
        }

        /// <summary>
        /// 將字串安全轉成 nullable DateTime。
        /// </summary>
        private static DateTime? ParseDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(value, out var parsed) ? parsed : (DateTime?)null;
        }

        /// <summary>
        /// 將 nullable int 轉回字串表示。
        /// </summary>
        private static string ToNullableIntText(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        /// <summary>
        /// 將 nullable decimal 轉回字串表示。
        /// </summary>
        private static string ToNullableIntText(decimal? value)
        {
            var parsed = ParseNullableInt(value);
            return parsed.HasValue ? parsed.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        /// <summary>
        /// 將字串截斷到指定長度。
        /// </summary>
        private static string Truncate(string value, int maxLength)
        {
            var text = NormalizeText(value);
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }

        /// <summary>
        /// 將文字轉換成指定語系版本。
        /// </summary>
        private static string ConvertLanguage(string sourceString, string language)
        {
            switch (language)
            {
                case "Big5":
                    return ChineseConverter.Convert(sourceString ?? string.Empty, ChineseConversionDirection.SimplifiedToTraditional);
                case "GB2312":
                    return ChineseConverter.Convert(sourceString ?? string.Empty, ChineseConversionDirection.TraditionalToSimplified);
                default:
                    return sourceString ?? string.Empty;
            }
        }

    }
}
