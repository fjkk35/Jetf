using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Services.ShipmentInboundProcess
{
    public class ShipmentInboundProcessService : _BaseService
    {
        public ShipmentInboundProcessResponse GetData(ShipmentInboundProcessRequest request)
        {
            var parameters = new DynamicParameters();

            var countSql = new StringBuilder();
            countSql.AppendLine("SELECT COUNT(1) FROM [jetf].[dbo].[ShipmentInbound]");
            BuildWhereConditions(countSql, request, parameters);

            var sql = new StringBuilder();
            sql.AppendLine("SELECT [Id]");
            sql.AppendLine("      ,[DataType]");
            sql.AppendLine("      ,[InboundDate]");
            sql.AppendLine("      ,[TrackingNo]");
            sql.AppendLine("      ,[SourceType]");
            sql.AppendLine("      ,[ReturnTrackingNo]");
            sql.AppendLine("      ,[CustCode]");
            sql.AppendLine("      ,[TransNo]");
            sql.AppendLine("      ,[TransName]");
            sql.AppendLine("      ,[ReturnReason]");
            sql.AppendLine("      ,[ProcessType]");
            sql.AppendLine("      ,[Tax]");
            sql.AppendLine("      ,[Ccfee]");
            sql.AppendLine("      ,[Cod]");
            sql.AppendLine("FROM [jetf].[dbo].[ShipmentInbound]");
            BuildWhereConditions(sql, request, parameters);

            sql.AppendLine("ORDER BY [InboundDate] DESC");
            sql.AppendLine($"OFFSET {(request.Page - 1) * request.PageSize} ROWS");
            sql.AppendLine($"FETCH NEXT {request.PageSize} ROWS ONLY");

            var totalCount = conn.QueryFirstOrDefault<int>(countSql.ToString(), parameters);
            var data = conn.Query<ShipmentInboundProcessModel>(sql.ToString(), parameters).ToList();

            FillCustomerAndTransNames(data);

            return new ShipmentInboundProcessResponse
            {
                Data = data,
                TotalCount = totalCount
            };
        }

        public bool UpdateProcessType(ShipmentInboundProcessUpdateRequest request)
        {
            var checkSql = @"
                SELECT OutboundDate, ProcessType, Tax, Ccfee, Cod, Fee
                FROM [jetf].[dbo].[ShipmentInbound] 
                WHERE [Id] = @Id";

            var existing = conn.QueryFirstOrDefault<dynamic>(checkSql, new { Id = request.Id });

            if (existing == null)
            {
                throw new Exception("查無此資料");
            }

            var outboundDate = (DateTime?)existing.OutboundDate;
            if (outboundDate.HasValue)
            {
                throw new Exception($"重出日期 {outboundDate.Value:yyyy/MM/dd}，無法更新資料");
            }

            var oldProcessType = (int?)existing.ProcessType;
            var oldTax = (int?)existing.Tax;
            var oldCcfee = (int?)existing.Ccfee;
            var oldCod = (int?)existing.Cod;
            var oldFee = (int?)existing.Fee;
            var userId = GetUserId();

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE [jetf].[dbo].[ShipmentInbound]");
            sql.AppendLine("SET [ProcessType] = @ProcessType");
            sql.AppendLine("   ,[ProcessTransNo] = @ProcessTransNo");
            sql.AppendLine("   ,[ProcessImporter] = @ProcessImporter");
            sql.AppendLine("   ,[ProcessImporterPhone] = @ProcessImporterPhone");
            sql.AppendLine("   ,[ProcessImporterAddr] = @ProcessImporterAddr");
            sql.AppendLine("   ,[StoreCode] = @StoreCode");
            sql.AppendLine("   ,[StoreName] = @StoreName");
            sql.AppendLine("   ,[FreightPayerNo] = @FreightPayerNo");
            sql.AppendLine("   ,[FreightFee] = @FreightFee");
            sql.AppendLine("   ,[Fee] = @Fee");
            sql.AppendLine("   ,[CarNo] = @CarNo");
            sql.AppendLine("   ,[PickupTime] = @PickupTime");
            sql.AppendLine("   ,[Remark] = @Remark");
            sql.AppendLine("   ,[Tax] = @Tax");
            sql.AppendLine("   ,[Ccfee] = @Ccfee");
            sql.AppendLine("   ,[Cod] = @Cod");
            sql.AppendLine("   ,[ProcessTime] = GETDATE()");
            sql.AppendLine("   ,[ProcessOpe] = @ProcessOpe");
            sql.AppendLine("WHERE [Id] = @Id");

            var parameters = new DynamicParameters();
            parameters.Add("Id", request.Id);
            parameters.Add("ProcessType", request.ProcessType);
            parameters.Add("ProcessTransNo", request.ProcessTransNo);
            parameters.Add("ProcessImporter", request.ProcessImporter);
            parameters.Add("ProcessImporterPhone", request.ProcessImporterPhone);
            parameters.Add("ProcessImporterAddr", request.ProcessImporterAddr);
            parameters.Add("StoreCode", request.StoreCode);
            parameters.Add("StoreName", request.StoreName);
            parameters.Add("FreightPayerNo", request.FreightPayerNo);
            parameters.Add("FreightFee", request.FreightFee);
            parameters.Add("Fee", request.Fee);
            parameters.Add("CarNo", request.CarNo);
            parameters.Add("PickupTime", request.PickupTime);
            parameters.Add("Remark", request.Remark);
            parameters.Add("Tax", request.Tax);
            parameters.Add("Ccfee", request.Ccfee);
            parameters.Add("Cod", request.Cod);
            parameters.Add("ProcessOpe", userId);

            const string insertHistorySql = @"
INSERT INTO [jetf].[dbo].[ShipmentInboundEditHistory]
([ShipmentInboundId], [FieldName], [OldValue], [NewValue], [EditTime], [EditUser])
VALUES
(@ShipmentInboundId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)";

            using (var connection = new System.Data.SqlClient.SqlConnection(conn.ConnectionString))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        connection.Execute(sql.ToString(), parameters, tx);

                        if (oldProcessType != request.ProcessType)
                        {
                            var oldValueText = oldProcessType.HasValue
                                ? ((ShipmentInboundProcessType)oldProcessType.Value).ToDescription()
                                : string.Empty;
                            var newValueText = ((ShipmentInboundProcessType)request.ProcessType).ToDescription();

                            connection.Execute(insertHistorySql, new
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "處理方式",
                                OldValue = oldValueText,
                                NewValue = newValueText,
                                EditTime = DateTime.Now,
                                EditUser = userId
                            }, tx);
                        }

                        if (oldTax != request.Tax)
                        {
                            connection.Execute(insertHistorySql, new
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "稅金",
                                OldValue = oldTax?.ToString(),
                                NewValue = request.Tax.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            }, tx);
                        }

                        if (oldCcfee != request.Ccfee)
                        {
                            connection.Execute(insertHistorySql, new
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "報關費",
                                OldValue = oldCcfee?.ToString(),
                                NewValue = request.Ccfee.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            }, tx);
                        }

                        if (oldCod != request.Cod)
                        {
                            connection.Execute(insertHistorySql, new
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "到付款",
                                OldValue = oldCod?.ToString(),
                                NewValue = request.Cod.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            }, tx);
                        }

                        if (oldFee != request.Fee)
                        {
                            connection.Execute(insertHistorySql, new
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "手續費",
                                OldValue = oldFee?.ToString(),
                                NewValue = request.Fee.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            }, tx);
                        }

                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public ShipmentInboundProcessDetailModel GetDetailById(int id)
        {
            var sql = @"
                SELECT [Id]
                      ,[ProcessType]
                      ,[ProcessTransNo]
                      ,[ProcessImporter]
                      ,[ProcessImporterPhone]
                      ,[ProcessImporterAddr]
                      ,[StoreCode]
                      ,[StoreName]
                      ,[Tax]
                      ,[Ccfee]
                      ,[Cod]
                      ,[FreightPayerNo]
                      ,[FreightFee]
                      ,[Fee]
                      ,[CarNo]
                      ,[PickupTime]
                      ,[Remark]
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE [Id] = @Id";

            var parameters = new DynamicParameters();
            parameters.Add("Id", id);

            return conn.QueryFirstOrDefault<ShipmentInboundProcessDetailModel>(sql, parameters);
        }

        /// <summary>
        /// 取得貨物來源清單
        /// </summary>
        public List<SelectListModel> GetSourceTypeList()
        {
            return Enum.GetValues(typeof(ShipmentInboundSourceType))
                .Cast<ShipmentInboundSourceType>()
                .Select(item => new SelectListModel
                {
                    Value = ((int)item).ToString(),
                    Text = item.ToDescription()
                }).ToList();
        }

        private void BuildWhereConditions(StringBuilder sql, ShipmentInboundProcessRequest request, DynamicParameters parameters)
        {
            sql.AppendLine("WHERE 1=1");

            sql.WhereIf(
                    !string.IsNullOrWhiteSpace(request.DataType),
                    "[DataType] = @DataType",
                    parameters,
                    p => p.Add("DataType", request.DataType)
                );

            sql.WhereIf(
                DateTime.TryParse(request.InboundDateStart, out var startDate),
                "[InboundDate] >= @InboundDateStart",
                parameters,
                p => p.Add("InboundDateStart", startDate)
            );

            sql.WhereIf(
                DateTime.TryParse(request.InboundDateEnd, out var endDate),
                "[InboundDate] <= @InboundDateEnd",
                parameters,
                p => p.Add("InboundDateEnd", endDate)
            );

            if (request.CustCodes?.Any() == true)
            {
                var custCodes = request.CustCodes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                sql.WhereIf(
                    custCodes.Length > 0,
                    "[CustCode] IN @CustCodes",
                    parameters,
                    p => p.Add("CustCodes", custCodes)
                );
            }

            sql.WhereIf(
                request.SourceType.HasValue,
                "[SourceType] = @SourceType",
                parameters,
                p => p.Add("SourceType", request.SourceType.Value)
            );

            sql.WhereIf(
                !string.IsNullOrWhiteSpace(request.TrackingNo),
                "[TrackingNo] = @TrackingNo",
                parameters,
                p => p.Add("TrackingNo", request.TrackingNo)
            );

            // 結案條件處理，不包含開箱確認內容物狀況、暫存資料
            sql.WhereIf(request.IsClosed == true, "([ProcessType] IS NOT NULL AND [ProcessType] NOT IN (7,8, 9))");
            sql.WhereIf(request.IsClosed == false, "([ProcessTime] IS NULL OR [ProcessType] IN (7,8, 9))");
        }

        private Dictionary<string, string> GetAirCustNames(List<string> custCodes)
        {
            if (!custCodes.Any())
                return new Dictionary<string, string>();

            var sql = "SELECT distinct OLD_CODE as Cust_Code, Cust_Name FROM DATA_CENTER.dbo.SYS_CUST WHERE CUST_TYPE='AIR' AND OLD_CODE > ''";
            var custs = conn.Query<dynamic>(sql).ToList();
            return custs.ToDictionary(
                x => (string)x.Cust_Code,
                x => (string)x.Cust_Name
            );
        }

        private Dictionary<string, string> GetSeaCustNames(List<string> custCodes)
        {
            if (!custCodes.Any())
                return new Dictionary<string, string>();

            var sql = "SELECT distinct Cust_Code, Cust_Name FROM DATA_CENTER.dbo.SYS_CUST WHERE CUST_TYPE='SEA'";
            var custs = conn.Query<dynamic>(sql).ToList();
            return custs.ToDictionary(
                x => (string)x.Cust_Code,
                x => (string)x.Cust_Name
            );
        }

        private Dictionary<string, string> GetAirTransNames(List<string> transNos)
        {
            if (!transNos.Any())
                return new Dictionary<string, string>();

            var sql = "SELECT distinct TRANS_NO, TRANS_NAME FROM [jetf].[dbo].customer_master WHERE TRAN_TYPE='空運'";
            var transList = conn.Query<dynamic>(sql).ToList();
            return transList
                .GroupBy(x => (string)x.TRANS_NO)
                .ToDictionary(
                    g => g.Key,
                    g => (string)g.First().TRANS_NAME
                );
        }

        private void FillCustomerAndTransNames(List<ShipmentInboundProcessModel> data)
        {
            var airCustCodes = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var seaCustCodes = data.Where(x => x.DataType == "海運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var airTransNos = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.TransNo))
                                  .Select(x => x.TransNo)
                                  .Distinct()
                                  .ToList();

            var airCustNames = GetAirCustNames(airCustCodes);
            var seaCustNames = GetSeaCustNames(seaCustCodes);
            var airTransNames = GetAirTransNames(airTransNos);

            foreach (var item in data)
            {
                if (!string.IsNullOrWhiteSpace(item.CustCode))
                {
                    if (item.DataType == "空運" && airCustNames.ContainsKey(item.CustCode))
                    {
                        item.CustName = airCustNames[item.CustCode];
                    }
                    else if (item.DataType == "海運" && seaCustNames.ContainsKey(item.CustCode))
                    {
                        item.CustName = seaCustNames[item.CustCode];
                    }
                }

                if (item.DataType == "空運" && !string.IsNullOrWhiteSpace(item.TransNo) && airTransNames.ContainsKey(item.TransNo))
                {
                    item.TransName = airTransNames[item.TransNo];
                }
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        public byte[] ExportExcel(ShipmentInboundProcessRequest request)
        {
            // 移除分頁限制，取得所有資料
            request.Page = 1;
            request.PageSize = int.MaxValue;

            var response = GetData(request);
            var workbook = CreateExcelWorkbook(response.Data);

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        private IWorkbook CreateExcelWorkbook(List<ShipmentInboundProcessModel> data)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("貨件退件處理");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");

            string[] headers = new string[]
            {
                "序號",
                "入庫日期",
                "進口方式",
                "客戶",
                "派件公司",
                "單號",
                "貨件來源",
                "退件原因",
                "處理方式"
            };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            for (int i = 0; i < data.Count; i++)
            {
                IRow row = sheet.CreateRow(i + 1);
                var item = data[i];

                NpoiCell.CreateCell(row, 0, (i + 1).ToString(), dataStyle);
                NpoiCell.CreateDateTimeCell(row, 1, item.InboundDate, dateStyle);
                NpoiCell.CreateCell(row, 2, item.DataType ?? "", dataStyle);
                NpoiCell.CreateCell(row, 3, item.CustName ?? "", dataStyle);
                NpoiCell.CreateCell(row, 4, item.TransName ?? "", dataStyle);
                NpoiCell.CreateCell(row, 5, item.TrackingNo ?? "", dataStyle);
                NpoiCell.CreateCell(row, 6, item.SourceTypeName ?? "", dataStyle);
                NpoiCell.CreateCell(row, 7, item.ReturnReason ?? "", dataStyle);
                NpoiCell.CreateCell(row, 8, item.ProcessTypeName ?? "", dataStyle);
            }

            sheet.AutoSizeColumns(headers.Length, scale: 1.2, minWidth: 15);

            return workbook;
        }

        /// <summary>
        /// 批量上傳(貨件回倉處理)
        /// Excel 欄位：單號、處理方式(中文)、備註
        /// 整批驗證：任一筆驗證失敗則整批失敗，不更新任何資料。
        /// </summary>
        public ResponseModel BatchUpload(string filePath)
        {
            var res = new ResponseModel { status = Status.success, msg = "上傳成功" };

            var rows = ReadBatchUploadExcel(filePath);
            if (rows.Count == 0)
            {
                res.status = Status.error;
                res.msg = "Excel 無資料";
                return res;
            }

            var validationErrors = ValidateBatchUploadRows(rows);
            if (validationErrors.Any())
            {
                res.status = Status.error;
                res.msg = $"批量上傳失敗，共{validationErrors.Count}筆錯誤。";
                res.ReturnObject = validationErrors;
                return res;
            }

            var userId = GetUserId();

            var trackingNos = rows.Select(x => x.TrackingNo).Distinct().ToList();
            var existingDataSql = @"
SELECT Id, TrackingNo, ProcessType
FROM [jetf].[dbo].[ShipmentInbound]
WHERE [TrackingNo] IN @TrackingNos
  AND [OutboundDate] IS NULL";

            var existingData = conn.Query<dynamic>(existingDataSql, new { TrackingNos = trackingNos })
                .ToDictionary(x => (string)x.TrackingNo, x => x);

            const string updateSql = @"
UPDATE [jetf].[dbo].[ShipmentInbound]
SET [ProcessType] = @ProcessType,
    [Remark] = @Remark,
    [ProcessTime] = GETDATE(),
    [ProcessOpe] = @ProcessOpe
WHERE [TrackingNo] = @TrackingNo
  AND [OutboundDate] IS NULL";

            const string insertHistorySql = @"
INSERT INTO [jetf].[dbo].[ShipmentInboundEditHistory]
([ShipmentInboundId], [FieldName], [OldValue], [NewValue], [EditTime], [EditUser])
VALUES
(@ShipmentInboundId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)";

            using (var connection = new System.Data.SqlClient.SqlConnection(conn.ConnectionString))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var row in rows)
                        {
                            int? newProcessType = row.ProcessTypeText.ToEnumValueByDescription<ShipmentInboundProcessType>();

                            connection.Execute(updateSql, new
                            {
                                TrackingNo = row.TrackingNo,
                                ProcessType = newProcessType.Value,
                                Remark = row.Remark,
                                ProcessOpe = userId
                            }, tx);

                            if (existingData.ContainsKey(row.TrackingNo))
                            {
                                var existing = existingData[row.TrackingNo];
                                var oldProcessType = (int?)existing.ProcessType;
                                var shipmentInboundId = (int)existing.Id;

                                if (oldProcessType != newProcessType.Value)
                                {
                                    var oldValueText = oldProcessType.HasValue
                                        ? ((ShipmentInboundProcessType)oldProcessType.Value).ToDescription()
                                        : string.Empty;
                                    var newValueText = ((ShipmentInboundProcessType)newProcessType.Value).ToDescription();

                                    connection.Execute(insertHistorySql, new
                                    {
                                        ShipmentInboundId = shipmentInboundId,
                                        FieldName = "處理方式",
                                        OldValue = oldValueText,
                                        NewValue = newValueText,
                                        EditTime = DateTime.Now,
                                        EditUser = userId
                                    }, tx);
                                }
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            res.msg = $"成功{rows.Count}筆";
            return res;
        }

        private List<ShipmentInboundProcessBatchUploadErrorModel> ValidateBatchUploadRows(List<ShipmentInboundProcessBatchUploadRowModel> rows)
        {
            var errors = new List<ShipmentInboundProcessBatchUploadErrorModel>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        ProcessTypeText = row.ProcessTypeText,
                        FieldName = "單號",
                        Reason = "不可為空"
                    });
                }

                if (string.IsNullOrWhiteSpace(row.ProcessTypeText))
                {
                    errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        ProcessTypeText = row.ProcessTypeText,
                        FieldName = "處理方式",
                        Reason = "不可為空"
                    });
                }
                else
                {
                    int? processType = row.ProcessTypeText.ToEnumValueByDescription<ShipmentInboundProcessType>();
                    if (!processType.HasValue || !IsAllowedBatchProcessType(processType.Value))
                    {
                        errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                        {
                            RowNo = row.RowNo,
                            TrackingNo = row.TrackingNo,
                            ProcessTypeText = row.ProcessTypeText,
                            FieldName = "處理方式",
                            Reason = "不允許的處理方式"
                        });
                    }
                }
            }

            var trackingNos = rows
                .Select(x => (x.TrackingNo ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (trackingNos.Any())
            {
                const string sql = @"
SELECT [TrackingNo]
FROM [jetf].[dbo].[ShipmentInbound]
WHERE [TrackingNo] IN @TrackingNos
  AND [OutboundDate] IS NULL";

                var existing = conn.Query<string>(sql, new { TrackingNos = trackingNos }).ToList();
                var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.TrackingNo))
                    {
                        continue;
                    }

                    if (!existingSet.Contains(row.TrackingNo.Trim()))
                    {
                        errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                        {
                            RowNo = row.RowNo,
                            TrackingNo = row.TrackingNo,
                            ProcessTypeText = row.ProcessTypeText,
                            FieldName = "單號",
                            Reason = "查無資料或已出庫"
                        });
                    }
                }
            }

            return errors;
        }

        private bool IsAllowedBatchProcessType(int processType)
        {
            // 僅允許：TransferFromOriginal(2)、ReturnToSite(3)、Destroy(5)、AddToReturnShipment(6)、InspectContents(7)、ConfirmOuterLabel(8)、TempData(9)
            var allowedTypes = new[] {
                (int)ShipmentInboundProcessType.TransferFromOriginal,
                (int)ShipmentInboundProcessType.ReturnToSite,
                (int)ShipmentInboundProcessType.Destroy,
                (int)ShipmentInboundProcessType.AddToReturnShipment,
                (int)ShipmentInboundProcessType.InspectContents,
                (int)ShipmentInboundProcessType.ConfirmOuterLabel,
                (int)ShipmentInboundProcessType.TempData
            };

            return allowedTypes.Contains(processType);
        }

        private List<ShipmentInboundProcessBatchUploadRowModel> ReadBatchUploadExcel(string filePath)
        {
            var result = new List<ShipmentInboundProcessBatchUploadRowModel>();

            IWorkbook workBook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            if (sheet == null) return result;

            bool read = false;
            int trackingNoIndex = -1;
            int processTypeIndex = -1;
            int remarkIndex = -1;

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                if (!read)
                {
                    for (int c = 0; c < row.LastCellNum; c++)
                    {
                        var header = row.GetCellData(c);
                        if (header == "單號") trackingNoIndex = c;
                        if (header == "處理方式") processTypeIndex = c;
                        if (header == "備註") remarkIndex = c;
                    }

                    if (trackingNoIndex >= 0 && processTypeIndex >= 0 && remarkIndex >= 0)
                    {
                        read = true;
                    }
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex);
                var processTypeText = row.GetCellData(processTypeIndex);
                var remark = row.GetCellData(remarkIndex);

                if (string.IsNullOrWhiteSpace(trackingNo) && string.IsNullOrWhiteSpace(processTypeText) && string.IsNullOrWhiteSpace(remark))
                {
                    continue;
                }

                result.Add(new ShipmentInboundProcessBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    ProcessTypeText = processTypeText,
                    Remark = remark
                });
            }

            return result;
        }

        /// <summary>
        /// 更新退件原因
        /// </summary>
        public void UpdateReturnReason(int id, string returnReason)
        {
            var checkSql = @"
                SELECT OutboundDate 
                FROM [jetf].[dbo].[ShipmentInbound] 
                WHERE [Id] = @Id";

            var outboundDate = conn.QueryFirstOrDefault<DateTime?>(checkSql, new { Id = id });

            if (outboundDate.HasValue)
            {
                throw new Exception($"出庫日期 {outboundDate.Value:yyyy/MM/dd}，無法更新資料");
            }

            var sql = @"
                UPDATE [jetf].[dbo].[ShipmentInbound]
                SET [ReturnReason] = @ReturnReason
                WHERE [Id] = @Id";

            conn.Execute(sql, new { Id = id, ReturnReason = returnReason });
        }

        /// <summary>
        /// 批量上傳退件原因
        /// Excel 欄位：單號、退件原因
        /// 驗證規則：如果有單號找不到，整批上傳失敗，並回傳失敗原因
        /// </summary>
        public ResponseModel BatchUploadReturnReason(string filePath)
        {
            var res = new ResponseModel { status = Status.success, msg = "上傳成功" };

            var rows = ReadReturnReasonBatchUploadExcel(filePath);
            if (rows.Count == 0)
            {
                res.status = Status.error;
                res.msg = "Excel 無資料";
                return res;
            }

            var validationErrors = ValidateReturnReasonBatchUploadRows(rows);
            if (validationErrors.Any())
            {
                res.status = Status.error;
                res.msg = $"批量上傳失敗，共 {validationErrors.Count} 筆錯誤。";
                res.ReturnObject = validationErrors;
                return res;
            }

            var userId = GetUserId();

            const string updateSql = @"
UPDATE [jetf].[dbo].[ShipmentInbound]
SET [ReturnReason] = @ReturnReason
WHERE [TrackingNo] = @TrackingNo
  AND [OutboundDate] IS NULL";

            using (var connection = new System.Data.SqlClient.SqlConnection(conn.ConnectionString))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var row in rows)
                        {
                            connection.Execute(updateSql, new
                            {
                                TrackingNo = row.TrackingNo,
                                ReturnReason = row.ReturnReason
                            }, tx);
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            res.msg = $"成功更新 {rows.Count} 筆";
            return res;
        }

        private List<ReturnReasonBatchUploadErrorModel> ValidateReturnReasonBatchUploadRows(List<ReturnReasonBatchUploadRowModel> rows)
        {
            var errors = new List<ReturnReasonBatchUploadErrorModel>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    errors.Add(new ReturnReasonBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        FieldName = "單號",
                        Reason = "單號不可為空"
                    });
                }
            }

            var trackingNos = rows
                .Select(x => (x.TrackingNo ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (trackingNos.Any())
            {
                const string sql = @"
SELECT [TrackingNo]
FROM [jetf].[dbo].[ShipmentInbound]
WHERE [TrackingNo] IN @TrackingNos
  AND [OutboundDate] IS NULL";

                var existing = conn.Query<string>(sql, new { TrackingNos = trackingNos }).ToList();
                var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.TrackingNo))
                    {
                        continue;
                    }

                    if (!existingSet.Contains(row.TrackingNo.Trim()))
                    {
                        errors.Add(new ReturnReasonBatchUploadErrorModel
                        {
                            RowNo = row.RowNo,
                            TrackingNo = row.TrackingNo,
                            FieldName = "單號",
                            Reason = "查無資料或已出庫"
                        });
                    }
                }
            }

            return errors;
        }

        private List<ReturnReasonBatchUploadRowModel> ReadReturnReasonBatchUploadExcel(string filePath)
        {
            var result = new List<ReturnReasonBatchUploadRowModel>();

            IWorkbook workBook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            if (sheet == null) return result;

            bool read = false;
            int trackingNoIndex = -1;
            int returnReasonIndex = -1;

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                if (!read)
                {
                    for (int c = 0; c < row.LastCellNum; c++)
                    {
                        var header = row.GetCellData(c);
                        if (header == "單號") trackingNoIndex = c;
                        if (header == "退件原因") returnReasonIndex = c;
                    }

                    if (trackingNoIndex >= 0 && returnReasonIndex >= 0)
                    {
                        read = true;
                    }
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex);
                var returnReason = row.GetCellData(returnReasonIndex);

                if (string.IsNullOrWhiteSpace(trackingNo) && string.IsNullOrWhiteSpace(returnReason))
                {
                    continue;
                }

                result.Add(new ReturnReasonBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    ReturnReason = returnReason
                });
            }

            return result;
        }
    }
}

