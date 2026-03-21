using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundWarehouseProcess.Domain;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundWarehouseProcess
{
    public class ShipmentInboundWarehouseProcessService : _BaseService
    {
        /// <summary>
        /// 查詢倉庫處理狀態資料
        /// </summary>
        public List<ShipmentInboundWarehouseProcessModel> GetData(ShipmentInboundWarehouseProcessRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TrackingNo))
            {
                return new List<ShipmentInboundWarehouseProcessModel>();
            }

            string sql = @"
                SELECT Id
                      ,[TrackingNo]
	                  ,[WarehouseProcessType]
	                  ,[WarehouseProcessTime]
	                  ,[WarehouseProcessOpe]
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE TrackingNo = @TrackingNo ";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                var result = connection.Query<ShipmentInboundWarehouseProcessModel>(sql, new { TrackingNo = request.TrackingNo }).ToList();
                return result;
            }
        }

        /// <summary>
        /// 更新處理狀態
        /// </summary>
        public void UpdateProcessType(ShipmentInboundWarehouseProcessUpdateRequest request)
        {
            var getOldValueSql = @"
                SELECT Id, WarehouseProcessType,OutboundTime
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE Id = @Id";

            var userId = GetUserId();

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        var existing = connection.QueryFirstOrDefault<dynamic>(getOldValueSql, new { Id = request.Id }, tx);
                        
                        if (existing == null)
                        {
                            throw new Exception("查無此資料");
                        }

                        if (existing.OutboundTime != null)
                        {
                            throw new Exception("已有出庫日期，更新倉庫處理狀態失敗");
                        }

                        var oldWarehouseProcessType = (byte?)existing.WarehouseProcessType;

                        string updateSql = @"
                UPDATE [jetf].[dbo].[ShipmentInbound]
                SET [WarehouseProcessType] = @WarehouseProcessType,
                    [WarehouseProcessTime] = GETDATE(),
                    [WarehouseProcessOpe] = @WarehouseProcessOpe
                WHERE Id = @Id";

                        connection.Execute(updateSql, new
                        {
                            Id = request.Id,
                            WarehouseProcessType = request.WarehouseProcessType,
                            WarehouseProcessOpe = userId
                        }, tx);

                        InsertWarehouseProcessTypeHistory(
                            connection, 
                            tx, 
                            request.Id, 
                            oldWarehouseProcessType, 
                            (byte)request.WarehouseProcessType, 
                            userId);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 批量上傳更新倉庫處理狀態
        /// Excel 欄位：單號、處理狀態(中文)
        /// 整批驗證：任一筆驗證失敗則整批失敗，不更新任何資料。
        /// </summary>
        public ResopnseModel BatchUpload(string filePath)
        {
            var res = new ResopnseModel { status = Status.success, msg = "上傳成功" };

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
            var getOldValuesSql = @"
SELECT Id, TrackingNo, WarehouseProcessType
FROM [jetf].[dbo].[ShipmentInbound]
WHERE [TrackingNo] IN @TrackingNos";

            const string updateSql = @"
UPDATE [jetf].[dbo].[ShipmentInbound]
SET [WarehouseProcessType] = @WarehouseProcessType,
    [WarehouseProcessTime] = GETDATE(),
    [WarehouseProcessOpe] = @WarehouseProcessOpe
WHERE [TrackingNo] = @TrackingNo";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                connection.Open();

                var existingData = connection.Query<dynamic>(getOldValuesSql, new { TrackingNos = trackingNos })
                    .ToDictionary(x => (string)x.TrackingNo, x => x);

                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var row in rows)
                        {
                            var newProcessType = row.WarehouseProcessTypeText.ToEnumValueByDescription<WarehouseProcessType>();
                            
                            connection.Execute(updateSql, new
                            {
                                TrackingNo = row.TrackingNo,
                                WarehouseProcessType = (byte)newProcessType.Value,
                                WarehouseProcessOpe = userId
                            }, tx);

                            if (existingData.ContainsKey(row.TrackingNo))
                            {
                                var existing = existingData[row.TrackingNo];
                                var oldWarehouseProcessType = (byte?)existing.WarehouseProcessType;
                                var shipmentInboundId = (int)existing.Id;

                                InsertWarehouseProcessTypeHistory(
                                    connection, 
                                    tx, 
                                    shipmentInboundId, 
                                    oldWarehouseProcessType, 
                                    (byte)newProcessType.Value, 
                                    userId);
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

        /// <summary>
        /// 寫入倉庫處理狀態編輯歷史
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="transaction">交易</param>
        /// <param name="shipmentInboundId">ShipmentInbound Id</param>
        /// <param name="oldValue">舊值</param>
        /// <param name="newValue">新值</param>
        /// <param name="userId">使用者Id</param>
        private void InsertWarehouseProcessTypeHistory(
            SqlConnection connection, 
            SqlTransaction transaction, 
            int shipmentInboundId, 
            byte? oldValue, 
            byte newValue, 
            string userId)
        {
            if (oldValue == newValue)
            {
                return;
            }

            var oldValueText = oldValue.HasValue
                ? ((WarehouseProcessType)oldValue.Value).ToDescription()
                : string.Empty;
            var newValueText = ((WarehouseProcessType)newValue).ToDescription();

            var insertHistorySql = @"
INSERT INTO [jetf].[dbo].[ShipmentInboundEditHistory]
([ShipmentInboundId], [FieldName], [OldValue], [NewValue], [EditTime], [EditUser])
VALUES
(@ShipmentInboundId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)";

            connection.Execute(insertHistorySql, new
            {
                ShipmentInboundId = shipmentInboundId,
                FieldName = "倉庫處理狀態",
                OldValue = oldValueText,
                NewValue = newValueText,
                EditTime = DateTime.Now,
                EditUser = userId
            }, transaction);
        }

        private List<ShipmentInboundWarehouseProcessBatchUploadErrorModel> ValidateBatchUploadRows(List<ShipmentInboundWarehouseProcessBatchUploadRowModel> rows)
        {
            var errors = new List<ShipmentInboundWarehouseProcessBatchUploadErrorModel>();

            // 基本欄位檢核 + Enum 檢核
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    errors.Add(new ShipmentInboundWarehouseProcessBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        WarehouseProcessTypeText = row.WarehouseProcessTypeText,
                        Reason = "單號不可為空"
                    });
                }

                if (string.IsNullOrWhiteSpace(row.WarehouseProcessTypeText))
                {
                    errors.Add(new ShipmentInboundWarehouseProcessBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        WarehouseProcessTypeText = row.WarehouseProcessTypeText,
                        Reason = "處理狀態不可為空"
                    });
                }
                else
                {
                    var processType = row.WarehouseProcessTypeText.ToEnumValueByDescription<WarehouseProcessType>();
                    if (!processType.HasValue)
                    {
                        errors.Add(new ShipmentInboundWarehouseProcessBatchUploadErrorModel
                        {
                            RowNo = row.RowNo,
                            TrackingNo = row.TrackingNo,
                            WarehouseProcessTypeText = row.WarehouseProcessTypeText,
                            Reason = $"處理狀態【{row.WarehouseProcessTypeText}】不存在"
                        });
                    }
                }
            }

            // 若基本欄位就已出錯，仍繼續收集錯誤，但避免 DB 查詢用空/重複單號
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
WHERE [TrackingNo] IN @TrackingNos";

                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    var existing = connection.Query<string>(sql, new { TrackingNos = trackingNos }).ToList();
                    var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (string.IsNullOrWhiteSpace(row.TrackingNo))
                        {
                            continue;
                        }

                        if (!existingSet.Contains(row.TrackingNo.Trim()))
                        {
                            errors.Add(new ShipmentInboundWarehouseProcessBatchUploadErrorModel
                            {
                                RowNo = row.RowNo,
                                TrackingNo = row.TrackingNo,
                                WarehouseProcessTypeText = row.WarehouseProcessTypeText,
                                Reason = "單號查無資料"
                            });
                        }
                    }
                }
            }

            return errors;
        }

        private List<ShipmentInboundWarehouseProcessBatchUploadRowModel> ReadBatchUploadExcel(string filePath)
        {
            var result = new List<ShipmentInboundWarehouseProcessBatchUploadRowModel>();

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

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                // 找表頭
                if (!read)
                {
                    for (int c = 0; c < row.LastCellNum; c++)
                    {
                        var header = row.GetCellData(c);
                        if (header == "單號") trackingNoIndex = c;
                        if (header == "處理狀態") processTypeIndex = c;
                    }

                    if (trackingNoIndex >= 0 && processTypeIndex >= 0)
                    {
                        read = true;
                    }
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex);
                var processTypeText = row.GetCellData(processTypeIndex);

                if (string.IsNullOrWhiteSpace(trackingNo) && string.IsNullOrWhiteSpace(processTypeText))
                {
                    continue;
                }

                result.Add(new ShipmentInboundWarehouseProcessBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    WarehouseProcessTypeText = processTypeText
                });
            }

            return result;
        }
    }
}
