using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Models;
using Service.Services.Tax;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
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

        private readonly DownloadService _downloadService;
        private readonly TaxService _taxService;

        public SeaTaxUploadService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, DownloadService downloadService, TaxService taxService)
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
            var uploadRows = ReadExcelIpost(filePath);
            var source = taxType.ToString();
            var uploadTime = DateTime.Now;
            List<SeaTaxModifyRow> modifyRows;

            {
                JetfDb.Database.CommandTimeout = 600;
                DataCenterDb.Database.CommandTimeout = 600;

                using (var transaction = JetfDb.Database.BeginTransaction())
                {
                    try
                    {
                        InsertSeaTaxUploads(JetfDb, uploadRows, uploadTime, userId);

                        modifyRows = GetMissingModifyRows(DataCenterDb, uploadRows, dataDate, source);
                        RefreshFeeMasterModifySnapshot(JetfDb, DataCenterDb, modifyRows, dataDate);
                        AppendModifyRowsToUpload(JetfDb, uploadRows, modifyRows, uploadTime, userId);

                        JetfDb.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return CreateErrorResponse(ex.Message);
                    }
                }
            }

            var updateResponse = _downloadService.UpdateCainiaoTaxEdit();
            if (updateResponse.status != Status.success)
            {
                return updateResponse;
            }

            if (uploadRows.Count == 0)
            {
                return new ResponseModel
                {
                    status = Status.error,
                    msg = "上傳檔案筆數：0"
                };
            }

            List<SeaTaxFeeMasterRow> feeMasterRows;
            {
                JetfDb.Database.CommandTimeout = 600;
                DataCenterDb.Database.CommandTimeout = 600;
                feeMasterRows = BuildFeeMasterRows(JetfDb, DataCenterDb, uploadRows, source);
            }

            {
                JetfDb.Database.CommandTimeout = 600;

                using (var transaction = JetfDb.Database.BeginTransaction())
                {
                    try
                    {
                        ReplaceFeeMaster(JetfDb, feeMasterRows, dataDate, source);
                        JetfDb.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return CreateErrorResponse(ex.Message);
                    }
                }
            }

            return new ResponseModel
            {
                status = Status.success,
                msg = $"上傳檔案筆數：{uploadRows.Count}"
            };
        }

        private static ResponseModel CreateErrorResponse(string message)
        {
            return new ResponseModel
            {
                status = Status.error,
                msg = message
            };
        }

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

            jetfDb.SeaTaxUploads.AddRange(entities);
        }

        private List<SeaTaxModifyRow> GetMissingModifyRows(
            DataCenterDbContext dataCenterDb,
            IEnumerable<SeaTaxUploadExcelRow> uploadRows,
            string dataDate,
            string taxType)
        {
            var startDate = DateTime.ParseExact($"{dataDate}000000", "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact($"{dataDate}235959", "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var uploadKeys = new HashSet<string>(
                (uploadRows ?? Enumerable.Empty<SeaTaxUploadExcelRow>())
                    .Select(row => BuildUploadKey(row.MainNumber, row.BlNo)),
                StringComparer.OrdinalIgnoreCase);

            return dataCenterDb.ClearanceTaxes
                .AsNoTracking()
                .Where(row => row.DataType == taxType && row.ModifyTime >= startDate && row.ModifyTime <= endDate)
                .ToList()
                .Where(row => !uploadKeys.Contains(BuildUploadKey(row.MainNumber, row.BagNumber)))
                .Select(row => new SeaTaxModifyRow
                {
                    Id = row.RowId,
                    DataType = NormalizeText(row.DataType),
                    MainNumber = NormalizeKeyText(row.MainNumber),
                    BagNumber = NormalizeKeyText(row.BagNumber),
                    MergeNumber = NormalizeText(row.MergeNumber),
                    TaxNumber = NormalizeText(row.TaxNumber),
                    TaxBase = row.TaxBase,
                    TaxAmount = row.TaxAmount,
                    FreqSign = NormalizeText(row.FreqSign),
                    Status = NormalizeText(row.Status),
                    ModifySeq = row.ModifySeq,
                    ModifyFile = NormalizeText(row.ModifyFile),
                    ModifyTime = row.ModifyTime
                })
                .ToList();
        }

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
                .Where(row => row.ModifyDataDate == dataDate && row.DataType == dataType)
                .ToList();

            if (existingRows.Count > 0)
            {
                jetfDb.FeeMasterModifies.RemoveRange(existingRows);
            }

            var snapshotRows = modifyRows
                .Select(row => CreateFeeMasterModifyEntity(
                    row,
                    latestOrders.TryGetValue(BuildUploadKey(row.MainNumber, row.BagNumber), out var order) ? order : null,
                    dataDate))
                .ToList();

            jetfDb.FeeMasterModifies.AddRange(snapshotRows);
        }

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
                    MainNumber = NormalizeKeyText(row.MainNumber),
                    BlNo = NormalizeKeyText(row.BagNumber),
                    Tax = row.TaxAmount.HasValue ? row.TaxAmount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    TaxNumber = NormalizeText(row.TaxNumber)
                })
                .ToList();

            if (rows.Count == 0)
            {
                return;
            }

            uploadRows.AddRange(rows);
            jetfDb.SeaTaxUploads.AddRange(rows.Select(row => CreateSeaTaxUploadEntity(row, uploadTime, userId)).ToList());
        }

        private List<SeaTaxFeeMasterRow> BuildFeeMasterRows(
            JetfDbContext jetfDb,
            DataCenterDbContext dataCenterDb,
            List<SeaTaxUploadExcelRow> uploadRows,
            string taxType)
        {
            var uploadKeys = uploadRows
                .Select(row => new UploadKey(row.MainNumber, row.BlNo))
                .ToList();

            var clearanceLookup = GetLatestClearanceInfoLookup(dataCenterDb, uploadKeys);
            var etlTipcTaxLookup = GetLatestEtlTipcTaxLookup(dataCenterDb, uploadKeys);
            var latestOrders = GetLatestSeaOrderLookup(dataCenterDb, uploadKeys);
            var customerLookup = GetSeaCustomerLookup(jetfDb, latestOrders.Values);
            var customerSpecialTable = CreateCustomerSpecialTable(
                jetfDb.CustomerSpecials
                    .AsNoTracking()
                    .Where(row => row.TranType == "海運")
                    .Select(row => row.Phone)
                    .ToList());

            var joinedRows = uploadRows
                .Select(row => BuildJoinedRow(row, clearanceLookup, etlTipcTaxLookup, latestOrders, customerLookup))
                .ToList();

            var uploadGroupCounts = uploadRows
                .GroupBy(row => BuildUploadKey(row.MainNumber, row.BlNo))
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            var feeMasterRows = new List<SeaTaxFeeMasterRow>();
            foreach (var group in joinedRows.GroupBy(row => BuildUploadKey(row.MainNumber, row.BlNo)))
            {
                var orderedRows = group
                    .OrderByDescending(row => row.SignOutTime ?? DateTime.MinValue)
                    .ThenByDescending(row => row.SignInTime ?? DateTime.MinValue)
                    .ToList();

                if (orderedRows.Count == 0)
                {
                    continue;
                }

                var latestRow = orderedRows[0];
                var feeMasterRow = new SeaTaxFeeMasterRow
                {
                    Source = taxType,
                    Type = NormalizeText(latestRow.ClearanceType),
                    Customer = NormalizeText(latestRow.DespatchName),
                    MainNumber = NormalizeKeyText(latestRow.MainNumber),
                    TrackingNo = NormalizeKeyText(latestRow.BlNo),
                    ClearanceNumber = NormalizeText(latestRow.ClearanceNumber),
                    TaxNumber = NormalizeText(latestRow.TaxNumber),
                    TaxBase = NormalizeText(latestRow.TaxBase),
                    TaxRecId = NormalizeText(latestRow.TaxRecId),
                    TaxPayer = NormalizeText(latestRow.TaxPayer),
                    Fee = ToNullableIntText(latestRow.CodFee),
                    IncludeTax = NormalizeText(latestRow.IncludeTax),
                    DlvCom = ConvertLanguage(NormalizeText(latestRow.TransTaxPayment), "Big5"),
                    Recipient = NormalizeText(latestRow.Importer),
                    RecPhone = NormalizeText(latestRow.ImporterPhone),
                    RecAddress = NormalizeText(latestRow.ImporterAddr),
                    RecId = Truncate(NormalizeText(latestRow.ImporterId), 20),
                    DlvInv = NormalizeText(latestRow.JetfSerial),
                    Cod = ToNullableIntText(latestRow.Cod),
                    Memo = NormalizeText(latestRow.Memo),
                    Arrival = NormalizeText(latestRow.Arrival)
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

                var groupCount = uploadGroupCounts.TryGetValue(group.Key, out var count) ? count : 0;
                if (groupCount > 1)
                {
                    feeMasterRow.Combine = "Y";
                    feeMasterRow.Tax1 = NormalizeText(latestRow.Tax);
                    feeMasterRow.Tax2 = orderedRows
                        .Skip(1)
                        .Take(groupCount - 1)
                        .Sum(row => ParseNullableInt(row.Tax) ?? 0)
                        .ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    feeMasterRow.Tax1 = NormalizeText(latestRow.Tax);
                }

                ApplyTaxRule(feeMasterRow, latestRow, customerSpecialTable);
                feeMasterRows.Add(feeMasterRow);
            }

            return feeMasterRows;
        }

        private static SeaTaxUploadJoinedRow BuildJoinedRow(
            SeaTaxUploadExcelRow uploadRow,
            IReadOnlyDictionary<string, ClearanceInfoEntity> clearanceLookup,
            IReadOnlyDictionary<string, EtlTipcTaxEntity> etlTipcTaxLookup,
            IReadOnlyDictionary<string, SeaOrderOriginalEntity> latestOrders,
            IReadOnlyDictionary<string, CustomerMasterEntity> customerLookup)
        {
            var uploadKey = BuildUploadKey(uploadRow.MainNumber, uploadRow.BlNo);
            clearanceLookup.TryGetValue(uploadKey, out var clearanceInfo);
            etlTipcTaxLookup.TryGetValue(uploadKey, out var etlTipcTax);
            latestOrders.TryGetValue(uploadKey, out var seaOrder);

            CustomerMasterEntity customerMaster = null;
            if (seaOrder != null)
            {
                customerLookup.TryGetValue(BuildCustomerLookupKey(seaOrder.CustCode, seaOrder.TransTaxPayment), out customerMaster);
            }

            return new SeaTaxUploadJoinedRow
            {
                BlNo = NormalizeKeyText(uploadRow.BlNo),
                ClearanceNumber = NormalizeText(uploadRow.ClearanceNumber),
                ClearanceType = NormalizeText(uploadRow.ClearanceType),
                Tax = NormalizeText(uploadRow.Tax),
                TaxNumber = NormalizeText(uploadRow.TaxNumber),
                MainNumber = NormalizeKeyText(uploadRow.MainNumber),
                SignInTime = clearanceInfo?.SignInTime,
                SignOutTime = clearanceInfo?.SignOutTime,
                TaxBase = NormalizeText(etlTipcTax?.TaxBase),
                CodFee = customerMaster?.CodFee,
                IncludeTax = NormalizeText(customerMaster?.IncludeTax),
                Company = NormalizeText(customerMaster?.Company),
                IsCainiaoP = customerMaster?.IsCainiaoP,
                TaxPayer = NormalizeText(uploadRow.TaxPayer),
                TaxRecId = NormalizeText(uploadRow.TaxRecId),
                DespatchName = NormalizeText(seaOrder?.CustCode),
                TransTaxPayment = NormalizeText(seaOrder?.TransTaxPayment),
                Importer = NormalizeText(seaOrder?.Importer),
                ImporterPhone = NormalizeText(seaOrder?.ImporterPhone),
                ImporterAddr = NormalizeText(seaOrder?.ImporterAddr),
                ImporterId = NormalizeText(seaOrder?.ImporterId),
                JetfSerial = NormalizeText(seaOrder?.JetfSerial),
                Cod = seaOrder?.CC,
                Memo = NormalizeText(seaOrder?.Memo),
                Arrival = NormalizeText(seaOrder?.Arrival)
            };
        }

        private static IReadOnlyDictionary<string, ClearanceInfoEntity> GetLatestClearanceInfoLookup(
            DataCenterDbContext dataCenterDb,
            List<UploadKey> uploadKeys)
        {
            var mainNumbers = uploadKeys.Select(row => row.MainNumber).Distinct().ToList();
            var bagNumbers = uploadKeys.Select(row => row.BlNo).Distinct().ToList();
            var keySet = new HashSet<string>(uploadKeys.Select(row => BuildUploadKey(row.MainNumber, row.BlNo)), StringComparer.OrdinalIgnoreCase);

            return dataCenterDb.ClearanceInfos
                .AsNoTracking()
                .Where(row => mainNumbers.Contains(row.MainNumber) && bagNumbers.Contains(row.BagNumber))
                .ToList()
                .Where(row => keySet.Contains(BuildUploadKey(row.MainNumber, row.BagNumber)))
                .GroupBy(row => BuildUploadKey(row.MainNumber, row.BagNumber))
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(row => row.SignOutTime ?? DateTime.MinValue)
                        .ThenByDescending(row => row.SignInTime ?? DateTime.MinValue)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, EtlTipcTaxEntity> GetLatestEtlTipcTaxLookup(
            DataCenterDbContext dataCenterDb,
            List<UploadKey> uploadKeys)
        {
            var mainNumbers = uploadKeys.Select(row => row.MainNumber).Distinct().ToList();
            var bagNumbers = uploadKeys.Select(row => row.BlNo).Distinct().ToList();
            var keySet = new HashSet<string>(uploadKeys.Select(row => BuildUploadKey(row.MainNumber, row.BlNo)), StringComparer.OrdinalIgnoreCase);

            return dataCenterDb.EtlTipcTaxes
                .AsNoTracking()
                .Where(row => mainNumbers.Contains(row.MainNumber) && bagNumbers.Contains(row.BagNumber))
                .ToList()
                .Where(row => keySet.Contains(BuildUploadKey(row.MainNumber, row.BagNumber)))
                .GroupBy(row => BuildUploadKey(row.MainNumber, row.BagNumber))
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(row => row.RowId).First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, SeaOrderOriginalEntity> GetLatestSeaOrderLookup(
            DataCenterDbContext dataCenterDb,
            List<UploadKey> uploadKeys)
        {
            var mainNumbers = uploadKeys.Select(row => row.MainNumber).Distinct().ToList();
            var bagNumbers = uploadKeys.Select(row => row.BlNo).Distinct().ToList();
            var keySet = new HashSet<string>(uploadKeys.Select(row => BuildUploadKey(row.MainNumber, row.BlNo)), StringComparer.OrdinalIgnoreCase);

            return dataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .Where(row => row.Gw.HasValue && row.Gw.Value > 0 && mainNumbers.Contains(row.MainNumber) && bagNumbers.Contains(row.BlNo))
                .ToList()
                .Where(row => keySet.Contains(BuildUploadKey(row.MainNumber, row.BlNo)))
                .GroupBy(row => BuildUploadKey(row.MainNumber, row.BlNo))
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(row => row.ModifyDate ?? DateTime.MinValue)
                        .ThenByDescending(row => row.Id)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, CustomerMasterEntity> GetSeaCustomerLookup(
            JetfDbContext jetfDb,
            IEnumerable<SeaOrderOriginalEntity> seaOrders)
        {
            var customerKeys = new HashSet<string>(
                (seaOrders ?? Enumerable.Empty<SeaOrderOriginalEntity>())
                    .Select(row => BuildCustomerLookupKey(row.CustCode, row.TransTaxPayment))
                    .Where(key => !string.IsNullOrWhiteSpace(key)),
                StringComparer.OrdinalIgnoreCase);

            if (customerKeys.Count == 0)
            {
                return new Dictionary<string, CustomerMasterEntity>(StringComparer.OrdinalIgnoreCase);
            }

            return jetfDb.CustomerMasters
                .AsNoTracking()
                .Where(row => row.TranType == "海運")
                .ToList()
                .Where(row => customerKeys.Contains(BuildCustomerLookupKey(row.CustId, row.TransName)))
                .GroupBy(row => BuildCustomerLookupKey(row.CustId, row.TransName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        private void ApplyTaxRule(
            SeaTaxFeeMasterRow feeMasterRow,
            SeaTaxUploadJoinedRow latestRow,
            DataTable customerSpecialTable)
        {
            var taxCalculationRow = CreateTaxCalculationRow(feeMasterRow);
            var includeTax = NormalizeText(latestRow.IncludeTax);
            var memo = NormalizeText(feeMasterRow.Memo);
            var company = NormalizeText(latestRow.Company);
            var recPhone = NormalizeText(feeMasterRow.RecPhone).Trim();

            if (includeTax == "Y")
            {
                var taxData = _taxService.GetTaxY(taxCalculationRow);
                feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (latestRow.IsCainiaoP.GetValueOrDefault())
            {
                var taxData = _taxService.GetTaxP(taxCalculationRow);
                feeMasterRow.IncludeTax = taxData.TransCod > 0 ? "N" : feeMasterRow.IncludeTax;
                feeMasterRow.Fee = taxData.TransCod > 0 ? feeMasterRow.Fee : "0";
                feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (includeTax == "D" || _taxService.IsSeaSpecial(customerSpecialTable, company, recPhone))
            {
                var taxData = _taxService.GetTaxD(taxCalculationRow);
                feeMasterRow.IncludeTax = "D";
                feeMasterRow.Fee = "0";
                feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (includeTax == "C" || memo.IndexOf("DDP", StringComparison.OrdinalIgnoreCase) > -1)
            {
                var taxData = _taxService.GetTaxC(taxCalculationRow);
                feeMasterRow.IncludeTax = "C";
                feeMasterRow.Fee = "0";
                feeMasterRow.TransCod = taxData.TransCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.CustomerCod = taxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
                feeMasterRow.ToDlvCod = taxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                return;
            }

            var defaultTaxData = _taxService.GetTaxN(taxCalculationRow);
            feeMasterRow.TransCod = defaultTaxData.TransCod.ToString(CultureInfo.InvariantCulture);
            feeMasterRow.CustomerCod = defaultTaxData.CustomerCod.ToString(CultureInfo.InvariantCulture);
            feeMasterRow.ToDlvCod = defaultTaxData.ToDlvCod.ToString(CultureInfo.InvariantCulture);
        }

        private static DataRow CreateTaxCalculationRow(SeaTaxFeeMasterRow feeMasterRow)
        {
            var table = new DataTable();
            table.Columns.Add("tax1", typeof(string));
            table.Columns.Add("tax2", typeof(string));
            table.Columns.Add("cod", typeof(string));
            table.Columns.Add("fee", typeof(string));

            var row = table.NewRow();
            row["tax1"] = NormalizeText(feeMasterRow.Tax1);
            row["tax2"] = NormalizeText(feeMasterRow.Tax2);
            row["cod"] = NormalizeText(feeMasterRow.Cod);
            row["fee"] = NormalizeText(feeMasterRow.Fee);
            table.Rows.Add(row);
            return row;
        }

        private static DataTable CreateCustomerSpecialTable(IEnumerable<string> phones)
        {
            var table = new DataTable();
            table.Columns.Add("PHONE", typeof(string));

            foreach (var phone in phones ?? Enumerable.Empty<string>())
            {
                var row = table.NewRow();
                row["PHONE"] = NormalizeText(phone);
                table.Rows.Add(row);
            }

            return table;
        }

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

            var existingRows = jetfDb.FeeMasters
                .Where(row => row.DataDate == dataDate && row.Source == source && row.SourceType == SeaSourceType)
                .ToList();

            if (existingRows.Count > 0)
            {
                var insTime = DateTime.Now;
                jetfDb.FeeMasterLogs.AddRange(existingRows.Select(row => CreateFeeMasterLogEntity(row, insTime)).ToList());
                jetfDb.FeeMasters.RemoveRange(existingRows);
            }

            jetfDb.FeeMasters.AddRange(feeMasterRows.Select(row => CreateFeeMasterEntity(row, dataDate)).ToList());
        }

        private static FeeMasterEntity CreateFeeMasterEntity(SeaTaxFeeMasterRow row, string dataDate)
        {
            return new FeeMasterEntity
            {
                DataDate = dataDate,
                Source = NormalizeText(row.Source),
                SourceType = SeaSourceType,
                Type = NormalizeText(row.Type),
                Customer = NormalizeText(row.Customer),
                MainNumber = NormalizeKeyText(row.MainNumber),
                TrackingNo = NormalizeKeyText(row.TrackingNo),
                ClearanceNumber = NormalizeText(row.ClearanceNumber),
                Combine = NormalizeText(row.Combine),
                InDate = NormalizeText(row.InDate),
                InDateTime = ParseDateTime(row.InDateTime),
                OutDateTime = ParseDateTime(row.OutDateTime),
                TaxBase = NormalizeText(row.TaxBase),
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
                ToDlvCod = ParseNullableInt(row.ToDlvCod),
                DlvInv = NormalizeText(row.DlvInv),
                TaxPayer = NormalizeText(row.TaxPayer),
                Arrival = NormalizeText(row.Arrival),
                CustomerCod = ParseNullableInt(row.CustomerCod),
                TransCod = ParseNullableInt(row.TransCod),
                TaxRecId = NormalizeText(row.TaxRecId)
            };
        }

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
                MainNumber = NormalizeKeyText(row.MainNumber),
                BagNumber = NormalizeKeyText(row.BagNumber),
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

        private static SeaTaxUploadEntity CreateSeaTaxUploadEntity(
            SeaTaxUploadExcelRow row,
            DateTime uploadTime,
            string userId)
        {
            return new SeaTaxUploadEntity
            {
                MainNumber = NormalizeKeyText(row.MainNumber),
                ClearanceNumber = NormalizeKeyText(row.ClearanceNumber),
                ClearanceType = NormalizeText(row.ClearanceType),
                BlNo = NormalizeKeyText(row.BlNo),
                RegNo = NormalizeText(row.RegNo),
                Mainfest = NormalizeText(row.Mainfest),
                TaxNumber = NormalizeKeyText(row.TaxNumber),
                Tax = NormalizeKeyText(row.Tax),
                PrtTime = row.PrtTime,
                UploadTime = uploadTime,
                UploadOpe = NormalizeKeyText(userId),
                TaxPayer = NormalizeText(row.TaxPayer),
                TaxRecId = NormalizeText(row.TaxRecId)
            };
        }

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

        private static string GetCellText(IRow row, int index)
        {
            var cell = row.GetCell(index);
            return cell == null ? string.Empty : cell.ToString().Trim();
        }

        private static string BuildUploadKey(string mainNumber, string blNo)
        {
            return $"{NormalizeKeyText(mainNumber)}__{NormalizeKeyText(blNo)}";
        }

        private static string BuildCustomerLookupKey(string customerCode, string transTaxPayment)
        {
            return $"{NormalizeText(customerCode)}__{NormalizeText(transTaxPayment)}";
        }

        private static string NormalizeKeyText(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

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

        private static int? ParseNullableInt(decimal? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return decimal.ToInt32(decimal.Truncate(value.Value));
        }

        private static DateTime? ParseDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(value, out var parsed) ? parsed : (DateTime?)null;
        }

        private static string ToNullableIntText(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string ToNullableIntText(decimal? value)
        {
            var parsed = ParseNullableInt(value);
            return parsed.HasValue ? parsed.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string Truncate(string value, int maxLength)
        {
            var text = NormalizeText(value);
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }

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

        private sealed class UploadKey
        {
            public UploadKey(string mainNumber, string blNo)
            {
                MainNumber = NormalizeKeyText(mainNumber);
                BlNo = NormalizeKeyText(blNo);
            }

            public string MainNumber { get; }

            public string BlNo { get; }
        }

        private sealed class SeaTaxUploadExcelRow
        {
            public string MainNumber { get; set; }

            public string ClearanceNumber { get; set; }

            public string ClearanceType { get; set; }

            public string BlNo { get; set; }

            public string RegNo { get; set; }

            public string Mainfest { get; set; }

            public string TaxNumber { get; set; }

            public string TaxRecId { get; set; }

            public string TaxPayer { get; set; }

            public string Tax { get; set; }

            public DateTime? PrtTime { get; set; }
        }

        private sealed class SeaTaxModifyRow
        {
            public int Id { get; set; }

            public string DataType { get; set; }

            public string MainNumber { get; set; }

            public string BagNumber { get; set; }

            public string MergeNumber { get; set; }

            public string TaxNumber { get; set; }

            public int? TaxBase { get; set; }

            public int? TaxAmount { get; set; }

            public string FreqSign { get; set; }

            public string Status { get; set; }

            public int? ModifySeq { get; set; }

            public string ModifyFile { get; set; }

            public DateTime? ModifyTime { get; set; }
        }

        private sealed class SeaTaxUploadJoinedRow
        {
            public string BlNo { get; set; }

            public string ClearanceNumber { get; set; }

            public string ClearanceType { get; set; }

            public string Tax { get; set; }

            public string TaxNumber { get; set; }

            public string MainNumber { get; set; }

            public DateTime? SignInTime { get; set; }

            public DateTime? SignOutTime { get; set; }

            public string TaxBase { get; set; }

            public int? CodFee { get; set; }

            public string IncludeTax { get; set; }

            public string Company { get; set; }

            public bool? IsCainiaoP { get; set; }

            public string TaxPayer { get; set; }

            public string TaxRecId { get; set; }

            public string DespatchName { get; set; }

            public string TransTaxPayment { get; set; }

            public string Importer { get; set; }

            public string ImporterPhone { get; set; }

            public string ImporterAddr { get; set; }

            public string ImporterId { get; set; }

            public string JetfSerial { get; set; }

            public decimal? Cod { get; set; }

            public string Memo { get; set; }

            public string Arrival { get; set; }
        }

        private sealed class SeaTaxFeeMasterRow
        {
            public string Source { get; set; }

            public string Type { get; set; }

            public string Customer { get; set; }

            public string MainNumber { get; set; }

            public string TrackingNo { get; set; }

            public string ClearanceNumber { get; set; }

            public string Combine { get; set; }

            public string InDate { get; set; }

            public string InDateTime { get; set; }

            public string OutDateTime { get; set; }

            public string TaxBase { get; set; }

            public string Tax1 { get; set; }

            public string Tax2 { get; set; } = string.Empty;

            public string DlvCom { get; set; }

            public string TaxNumber { get; set; }

            public string TaxRecId { get; set; }

            public string TaxPayer { get; set; }

            public string Cod { get; set; }

            public string Fee { get; set; }

            public string IncludeTax { get; set; }

            public string Recipient { get; set; }

            public string RecPhone { get; set; }

            public string RecAddress { get; set; }

            public string RecId { get; set; }

            public string ToDlvCod { get; set; }

            public string DlvInv { get; set; }

            public string Memo { get; set; }

            public string Arrival { get; set; }

            public string TransCod { get; set; } = string.Empty;

            public string CustomerCod { get; set; } = string.Empty;
        }
    }
}