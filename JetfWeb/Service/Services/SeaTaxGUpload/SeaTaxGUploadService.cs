using NLog;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.TaxTransferGuard;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Services.SeaTaxGUpload
{
    /// <summary>
    /// G 類海運稅金資料上傳服務。
    /// </summary>
    public class SeaTaxGUploadService : _BaseService
    {
        private const int CommandTimeoutSeconds = 600;
        private const string GSourceType = "2";
        private const string GClearanceType = "G";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly TaxTransferGuardService _taxTransferGuardService;

        private static readonly string[] RequiredHeaders =
        {
            "倉儲",
            "分提單號",
            "派送單號",
            "稅金",
            "報關費",
            "到付款",
            "代收手續",
            "稅金類別",
            "客戶名",
            "客戶"
        };

        /// <summary>
        /// 建立 G 類海運稅金資料上傳服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">Data Center 資料庫內容。</param>
        /// <param name="taxTransferGuardService">稅金轉檔作業日銷帳檢查服務。</param>
        public SeaTaxGUploadService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext,
            TaxTransferGuardService taxTransferGuardService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _taxTransferGuardService = taxTransferGuardService;
        }

        /// <summary>
        /// 上傳 G 類海運稅金 Excel。
        /// </summary>
        /// <param name="dataDate">資料日期，格式為 yyyyMMdd。</param>
        /// <param name="excelStream">Excel 檔案串流。</param>
        /// <returns>上傳結果。</returns>
        public ResponseModel Upload(string dataDate, Stream excelStream)
        {
            try
            {
                DateTime outDate;
                if (!DateTime.TryParseExact(
                    dataDate,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out outDate))
                {
                    return new ResponseModel("資料日期格式錯誤");
                }

                if (excelStream == null)
                {
                    return new ResponseModel("未選擇檔案");
                }

                var transferValidation = _taxTransferGuardService.ValidateCanTransfer(dataDate);
                if (!transferValidation.IsSuccess)
                {
                    return transferValidation;
                }

                JetfDb.Database.CommandTimeout = CommandTimeoutSeconds;
                DataCenterDb.Database.CommandTimeout = CommandTimeoutSeconds;
                var uploadRows = ReadUploadRows(excelStream);
                ValidateRows(uploadRows);
                ResolveSeaCustomerCodes(uploadRows);
                var response = InsertFeeMasterG(uploadRows, dataDate, outDate);
                if (response.status == Status.success)
                {
                    response.msg = $"上傳檔案筆數：{uploadRows.Count}";
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "G 類海運稅金資料上傳失敗");
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 以既有 NPOI 儲存格擴充功能讀取上傳資料。
        /// </summary>
        private static List<SeaTaxGUploadRow> ReadUploadRows(Stream excelStream)
        {
            IWorkbook workbook = new XSSFWorkbook(excelStream);
            try
            {
                if (workbook.NumberOfSheets == 0)
                {
                    return new List<SeaTaxGUploadRow>();
                }

                var sheet = workbook.GetSheetAt(0);
                int headerRowIndex;
                Dictionary<string, int> columnIndexes;
                if (!TryFindHeaderRow(
                    sheet,
                    RequiredHeaders,
                    out headerRowIndex,
                    out columnIndexes))
                {
                    throw new InvalidDataException(
                        $"找不到完整表頭，請確認包含：{string.Join("、", RequiredHeaders)}。");
                }

                var uploadRows = new List<SeaTaxGUploadRow>();

                for (var rowIndex = headerRowIndex + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var excelRow = sheet.GetRow(rowIndex);
                    if (excelRow == null)
                    {
                        continue;
                    }

                    var uploadRow = ReadUploadRow(
                        excelRow,
                        rowIndex + 1,
                        columnIndexes);
                    if (!IsEmptyRow(uploadRow))
                    {
                        uploadRows.Add(uploadRow);
                    }
                }

                return uploadRows;
            }
            finally
            {
                workbook.Close();
            }
        }

        /// <summary>
        /// 依欄位名稱讀取一筆 G 類海運稅金資料。
        /// </summary>
        private static SeaTaxGUploadRow ReadUploadRow(
            IRow excelRow,
            int rowNumber,
            IDictionary<string, int> columnIndexes)
        {
            int taxPayerColumnIndex;
            var taxPayer = columnIndexes.TryGetValue(
                "收件人",
                out taxPayerColumnIndex)
                ? excelRow.GetCellData(taxPayerColumnIndex)
                : string.Empty;

            var uploadRow = new SeaTaxGUploadRow
            {
                RowNumber = rowNumber,
                Source = NormalizeSource(excelRow.GetCellData(columnIndexes["倉儲"])),
                TrackingNo = excelRow.GetCellData(columnIndexes["分提單號"]),
                DlvInv = excelRow.GetCellData(columnIndexes["派送單號"]),
                Tax = ParseAmount(excelRow.GetCellData(columnIndexes["稅金"]), rowNumber, "稅金"),
                ClearanceFee = ParseAmount(excelRow.GetCellData(columnIndexes["報關費"]), rowNumber, "報關費"),
                Cod = ParseAmount(excelRow.GetCellData(columnIndexes["到付款"]), rowNumber, "到付款"),
                Fee = ParseAmount(excelRow.GetCellData(columnIndexes["代收手續"]), rowNumber, "代收手續"),
                IncludeTax = excelRow.GetCellData(columnIndexes["稅金類別"]),
                Recipient = excelRow.GetCellData(columnIndexes["客戶名"]),
                TaxPayer = taxPayer,
                CustomerName = excelRow.GetCellData(columnIndexes["客戶"])
            };

            if (uploadRow.IncludeTax == "C")
            {
                // C 類別：TO_DLV_COD 為到付款加手續費，CUSTOMER_COD 為稅金加報關費，TRANS_COD 為 0。
                uploadRow.ToDlvCod = uploadRow.Cod + uploadRow.Fee;
                uploadRow.CustomerCod = uploadRow.Tax + uploadRow.ClearanceFee;
            }
            else
            {
                // 非 C 類別由物流及派送端代收稅金，客戶代收金額維持為空值。
                uploadRow.ToDlvCod =
                    uploadRow.ClearanceFee +
                    uploadRow.Cod +
                    uploadRow.Fee +
                    uploadRow.Tax;
                uploadRow.TransCod = uploadRow.Tax;
            }

            return uploadRow;
        }

        /// <summary>
        /// 尋找同時包含所有必要欄位的表頭列，並建立欄位名稱索引。
        /// </summary>
        private static bool TryFindHeaderRow(
            ISheet sheet,
            IEnumerable<string> requiredHeaders,
            out int headerRowIndex,
            out Dictionary<string, int> columnIndexes)
        {
            headerRowIndex = -1;
            columnIndexes = null;

            for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var candidate = new Dictionary<string, int>(StringComparer.Ordinal);
                for (var columnIndex = 0; columnIndex < row.LastCellNum; columnIndex++)
                {
                    var header = row.GetCellData(columnIndex);
                    if (!string.IsNullOrWhiteSpace(header) &&
                        !candidate.ContainsKey(header))
                    {
                        // 同名表頭取第一個欄位；目前兩個「倉儲」會沿用原上傳格式的第一欄。
                        candidate.Add(header, columnIndex);
                    }
                }

                if (!requiredHeaders.All(candidate.ContainsKey))
                {
                    continue;
                }

                headerRowIndex = rowIndex;
                columnIndexes = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判斷解析後的資料列是否完全空白。
        /// </summary>
        private static bool IsEmptyRow(SeaTaxGUploadRow row)
        {
            return string.IsNullOrWhiteSpace(row.Source) &&
                   string.IsNullOrWhiteSpace(row.TrackingNo) &&
                   string.IsNullOrWhiteSpace(row.DlvInv) &&
                   string.IsNullOrWhiteSpace(row.IncludeTax) &&
                   string.IsNullOrWhiteSpace(row.Recipient) &&
                   string.IsNullOrWhiteSpace(row.TaxPayer) &&
                   string.IsNullOrWhiteSpace(row.CustomerName) &&
                   row.Tax == 0 &&
                   row.ClearanceFee == 0 &&
                   row.Cod == 0 &&
                   row.Fee == 0;
        }

        private static string NormalizeSource(string source)
        {
            var value = (source ?? string.Empty).Trim();
            return string.Equals(value, "G類", StringComparison.OrdinalIgnoreCase)
                ? "TPCT"
                : value;
        }

        private static int ParseAmount(string value, int rowNumber, string header)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            decimal amount;
            var normalizedValue = value.Trim();
            var parsed = decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out amount) || decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out amount);

            if (!parsed || amount != decimal.Truncate(amount) || amount > int.MaxValue || amount < int.MinValue)
            {
                throw new InvalidDataException($"第 {rowNumber} 列「{header}」必須是整數");
            }

            return (int)amount;
        }

        private static void ValidateRows(List<SeaTaxGUploadRow> uploadRows)
        {
            if (uploadRows.Count == 0)
            {
                throw new InvalidDataException("上傳檔案筆數：0");
            }

            foreach (var row in uploadRows)
            {
                var missingFields = new List<string>();
                if (string.IsNullOrWhiteSpace(row.Source))
                {
                    missingFields.Add("倉儲");
                }

                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    missingFields.Add("分提單號");
                }

                if (string.IsNullOrWhiteSpace(row.DlvInv))
                {
                    missingFields.Add("派送單號");
                }

                if (string.IsNullOrWhiteSpace(row.IncludeTax))
                {
                    missingFields.Add("稅金類別");
                }

                if (string.IsNullOrWhiteSpace(row.CustomerName))
                {
                    missingFields.Add("客戶");
                }

                if (missingFields.Any())
                {
                    throw new InvalidDataException(
                        $"第 {row.RowNumber} 列必填欄位不可空白：{string.Join("、", missingFields)}");
                }
            }

            var duplicateDlvInvs = uploadRows
                .GroupBy(x => x.DlvInv)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .Take(10)
                .ToList();

            if (duplicateDlvInvs.Any())
            {
                throw new InvalidDataException(
                    $"派送單號重複：{string.Join("、", duplicateDlvInvs)}");
            }
        }

        private void ResolveSeaCustomerCodes(List<SeaTaxGUploadRow> uploadRows)
        {
            var customerCodeDic = DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(customer => customer.CustType == "SEA")
                .ToDictionary(
                    customer => customer.CustName,
                    customer => customer.CustCode);

            foreach (var row in uploadRows)
            {
                if (!customerCodeDic.TryGetValue(
                    row.CustomerName, out var customerCode))
                {
                    throw new InvalidDataException(
                        $"第 {row.RowNumber} 列查無海運客戶代號：{row.CustomerName}");
                }

                row.CustomerCode = customerCode;
            }
        }

        /// <summary>
        /// 依原 G 類寫入流程將上傳 Model 寫入費用主檔。
        /// </summary>
        /// <param name="uploadRows">已完成驗證及客戶代號轉換的上傳資料。</param>
        /// <param name="dataDate">資料日期，格式為 yyyyMMdd。</param>
        /// <param name="outDate">費用主檔的出倉日期。</param>
        /// <returns>資料寫入結果。</returns>
        private ResponseModel InsertFeeMasterG(
            List<SeaTaxGUploadRow> uploadRows,
            string dataDate,
            DateTime outDate)
        {
            var response = new ResponseModel();
            var shouldCloseConnection = conn.State == ConnectionState.Closed;
            if (shouldCloseConnection)
            {
                conn.Open();
            }

            var sql = new StringBuilder()
                .Append("declare @Select_DLV_REMIT_CODE nvarchar(2) ")
                .Append("declare @Select_SOURCE_TYPE nvarchar(2) ")
                .Append("declare @Select_DATADATE nvarchar(8) ")
                .Append("select * from [jetf].[dbo].[FEE_MASTER] where SOURCE_TYPE='2' and DLV_INV=@DLV_INV ")
                .Append("if @@ROWCOUNT>0 ")
                .Append("begin ")
                .Append("    delete detail from [jetf].[dbo].[FEE_MASTER_DETAIL] detail ")
                .Append("    inner join [jetf].[dbo].[FEE_MASTER] master on master.ID=detail.FEE_MASTER_ID ")
                .Append("    where master.SOURCE_TYPE='2' and master.DLV_INV=@DLV_INV ")
                .Append("    delete from [jetf].[dbo].[FEE_MASTER] where SOURCE_TYPE='2' and DLV_INV=@DLV_INV ")
                .Append("end ")
                .Append("select @Select_DATADATE=DATADATE,@Select_SOURCE_TYPE=SOURCE_TYPE,@Select_DLV_REMIT_CODE=DLV_REMIT_CODE ")
                .Append("from [jetf].[dbo].[FEE_MASTER] where SOURCE_TYPE='1' and DLV_INV=@DLV_INV ")
                .Append("if @@ROWCOUNT>0 ")
                .Append("begin ")
                .Append("    insert FEE_MASTER_MODIFY_G([MODIFY_DATADATE], [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [MEMO], [INS_TIME], [ARRIVAL], [CUSTOMER_COD], [TRANS_COD]) ")
                .Append("    select @MODIFY_DATADATE, [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], '刪除', getdate(), [ARRIVAL], [CUSTOMER_COD], [TRANS_COD] ")
                .Append("    from jetf.dbo.FEE_MASTER where SOURCE_TYPE='1' and DLV_INV=@DLV_INV ")
                .Append("    update [jetf].[dbo].[FEE_MASTER] set Download='0' where SOURCE_TYPE='1' and DLV_INV=@DLV_INV ")
                .Append("end ")
                .Append("insert [jetf].[dbo].[FEE_MASTER](DATADATE, SOURCE, SOURCE_TYPE, CUSTOMER, TRACKINGNO, TYPE, DLV_INV, OUT_DATETIME, TAX1, FEE, CCFEE, COD, INCLUDE_TAX, TO_DLV_COD, RECIPIENT, TAX_PAYER, TRANS_COD, CUSTOMER_COD) ")
                .Append("values(@DATADATE, @SOURCE, @SOURCE_TYPE, @CUSTOMER, @TRACKINGNO, @TYPE, @DLV_INV, @OUT_DATETIME, @TAX1, @FEE, @CCFEE, @COD, @INCLUDE_TAX, @TO_DLV_COD, @RECIPIENT, @TAX_PAYER, @TRANS_COD, @CUSTOMER_COD) ")
                .Append("declare @FeeMasterId int=cast(scope_identity() as int) ")
                .Append("insert [jetf].[dbo].[FEE_MASTER_DETAIL](FEE_MASTER_ID, MAIN_NUMBER, TRACKINGNO, CLEARANCE_NUMBER, BAG_NUMBER, TAX_NUMBER, TAX_PAYER, TAX_RECID, DLV_INV, TAX_BASE, TAX, CCFEE, COD, FEE, RECIPIENT, RECPHONE, RECADDRESS, TO_DLV_COD, TRANS_COD, CUSTOMER_COD) ")
                .Append("select ID, MAIN_NUMBER, TRACKINGNO, CLEARANCE_NUMBER, BAG_NUMBER, TAX_NUMBER, TAX_PAYER, TAX_RECID, DLV_INV, TAX_BASE, TAX1, CCFEE, COD, FEE, RECIPIENT, RECPHONE, RECADDRESS, TO_DLV_COD, TRANS_COD, CUSTOMER_COD ")
                .Append("from [jetf].[dbo].[FEE_MASTER] where ID=@FeeMasterId ")
                .Append("insert FEE_MASTER_MODIFY_G([MODIFY_DATADATE], [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [MEMO], [INS_TIME], [ARRIVAL], [CUSTOMER_COD], [TRANS_COD]) ")
                .Append("select @MODIFY_DATADATE, [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], '新增', getdate(), [ARRIVAL], [CUSTOMER_COD], [TRANS_COD] ")
                .Append("from jetf.dbo.FEE_MASTER where SOURCE_TYPE='2' and DLV_INV=@DLV_INV ")
                .ToString();

            try
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        if (uploadRows.Count > 0)
                        {
                            using (var deleteCommand = new SqlCommand(
                                "delete from [jetf].[dbo].[FEE_MASTER_MODIFY_G] where MODIFY_DATADATE=@MODIFY_DATADATE",
                                conn,
                                transaction))
                            {
                                deleteCommand.CommandTimeout = CommandTimeoutSeconds;
                                deleteCommand.Parameters.Add("@MODIFY_DATADATE", SqlDbType.NVarChar, 8).Value = dataDate;
                                deleteCommand.ExecuteNonQuery();
                            }
                        }

                        using (var command = new SqlCommand(sql, conn, transaction))
                        {
                            command.CommandTimeout = CommandTimeoutSeconds;
                            foreach (var row in uploadRows)
                            {
                                command.Parameters.Clear();
                                command.Parameters.Add("@MODIFY_DATADATE", SqlDbType.NVarChar, 8).Value = dataDate;
                                command.Parameters.Add("@DATADATE", SqlDbType.NVarChar, 8).Value = dataDate;
                                command.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = row.Source;
                                command.Parameters.Add("@SOURCE_TYPE", SqlDbType.NVarChar, 2).Value = GSourceType;
                                command.Parameters.Add("@CUSTOMER", SqlDbType.NVarChar).Value = row.CustomerCode;
                                command.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = row.TrackingNo;
                                command.Parameters.Add("@TYPE", SqlDbType.NVarChar, 2).Value = GClearanceType;
                                command.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = row.DlvInv;
                                command.Parameters.Add("@OUT_DATETIME", SqlDbType.DateTime).Value = outDate;
                                command.Parameters.Add("@TAX1", SqlDbType.Int).Value = row.Tax;
                                command.Parameters.Add("@FEE", SqlDbType.Int).Value = row.Fee;
                                command.Parameters.Add("@CCFEE", SqlDbType.Int).Value = row.ClearanceFee;
                                command.Parameters.Add("@COD", SqlDbType.Int).Value = row.Cod;
                                command.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = row.IncludeTax;
                                command.Parameters.Add("@TO_DLV_COD", SqlDbType.NVarChar).Value = row.ToDlvCod.ToString(CultureInfo.InvariantCulture);
                                command.Parameters.Add("@RECIPIENT", SqlDbType.NVarChar).Value = row.Recipient;
                                command.Parameters.Add("@TAX_PAYER", SqlDbType.NVarChar).Value = row.TaxPayer;
                                command.Parameters.Add("@TRANS_COD", SqlDbType.Int).Value = row.TransCod;
                                command.Parameters.Add("@CUSTOMER_COD", SqlDbType.Int).Value =
                                    row.CustomerCod.HasValue
                                        ? (object)row.CustomerCod.Value
                                        : DBNull.Value;
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        response.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        response.status = Status.error;
                        response.msg = ex.Message;
                    }
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    conn.Close();
                }
            }

            return response;
        }
    }
}
