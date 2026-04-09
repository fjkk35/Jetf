using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentOutboundBatchImport.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentOutboundBatchImport
{
    public class ShipmentOutboundBatchImportService : _BaseService
    {
        /// <summary>
        /// 批量上傳貨件出庫資料
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="userId">上傳人員ID</param>
        /// <returns></returns>
        public ResponseModel UploadShipmentOutbound(string filePath)
        {
            try
            {
                var shipmentOutboundList = ReadExcelFile(filePath);

                if (shipmentOutboundList.Count == 0)
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                ValidateData(shipmentOutboundList);

                var failList = shipmentOutboundList.FindAll(x => x.UploadStatus == "失敗");

                if (failList.Count > 0)
                {
                    return new ResponseModel(new
                    {
                        count = 0,
                        failCount = failList.Count,
                        data = shipmentOutboundList,
                        message = $"上傳失敗，共 {failList.Count} 筆資料有錯誤，請修正後重新上傳"
                    });
                }

                var successList = shipmentOutboundList.FindAll(x => x.UploadStatus == "成功");

                UpdateShipmentOutbound(successList);

                return new ResponseModel(new
                {
                    count = successList.Count,
                    failCount = 0,
                    data = shipmentOutboundList,
                    message = $"成功上傳 {successList.Count} 筆資料"
                });
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 讀取 Excel 檔案
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="userId">上傳人員ID</param>
        /// <returns></returns>
        private List<ShipmentOutboundModel> ReadExcelFile(string filePath)
        {
            var shipmentOutboundList = new List<ShipmentOutboundModel>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var model = new ShipmentOutboundModel
                    {
                        TrackingNo = row.GetCellData(0),
                        OutboundDate = ParseDate(row.GetCellData(1)),
                        OutboundTrackingNo = row.GetCellData(2),
                        OutboundOpe = GetUserId()
                    };

                    if (string.IsNullOrWhiteSpace(model.TrackingNo) && model.OutboundDate == DateTime.MinValue)
                        continue;

                    shipmentOutboundList.Add(model);
                }
            }

            return shipmentOutboundList;
        }

        /// <summary>
        /// 解析日期
        /// </summary>
        /// <param name="dateString">日期字串</param>
        /// <returns></returns>
        private DateTime ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return DateTime.MinValue;

            if (DateTime.TryParse(dateString, out DateTime result))
                return result;

            return DateTime.MinValue;
        }

        /// <summary>
        /// 驗證資料
        /// </summary>
        /// <param name="shipmentOutboundList">貨件出庫資料列表</param>
        private void ValidateData(List<ShipmentOutboundModel> shipmentOutboundList)
        {
            foreach (var shipment in shipmentOutboundList)
            {
                if (string.IsNullOrWhiteSpace(shipment.TrackingNo))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "原單號為空";
                    continue;
                }

                if (shipment.OutboundDate == DateTime.MinValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "轉出日期為空";
                    continue;
                }
            }

            var validList = shipmentOutboundList
                .Where(x => x.UploadStatus != "失敗")
                .ToList();

            if (validList.Count == 0)
                return;

            var trackingNos = validList.Select(x => x.TrackingNo).Distinct().ToList();

            var sql = @"
                SELECT TrackingNo, ProcessType 
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE OutboundDate IS NULL AND TrackingNo IN @TrackingNos";

            var existingData = conn.Query<ShipmentOutboundModel>(sql, new { TrackingNos = trackingNos }).ToList();

            var existingDict = existingData.ToDictionary(x => x.TrackingNo, x => x.ProcessType);

            foreach (var shipment in validList)
            {
                if (!existingDict.ContainsKey(shipment.TrackingNo))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "找不到該原單號或該單號已出庫";
                    continue;
                }

                var processType = existingDict[shipment.TrackingNo];
                shipment.ProcessType = processType;

                if (processType == ShipmentInboundProcessType.NewTrackingNo)
                {
                    if (string.IsNullOrWhiteSpace(shipment.OutboundTrackingNo))
                    {
                        shipment.UploadStatus = "失敗";
                        shipment.FailReason = $"{processType.ToDescription()}，新物流單號為必填";
                        continue;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(shipment.OutboundTrackingNo))
                    {
                        shipment.UploadStatus = "失敗";
                        shipment.FailReason = $"{processType.ToDescription()} 時，新物流單號不可填值";
                        continue;
                    }
                }

                shipment.UploadStatus = "成功";
                shipment.FailReason = string.Empty;
            }
        }

        /// <summary>
        /// 批量更新資料庫
        /// </summary>
        /// <param name="shipmentOutboundList">貨件出庫資料列表</param>
        private void UpdateShipmentOutbound(List<ShipmentOutboundModel> shipmentOutboundList)
        {
            var trackingNos = shipmentOutboundList.Select(x => x.TrackingNo).Distinct().ToList();

            var getIdSql = @"
                SELECT Id, TrackingNo
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE TrackingNo IN @TrackingNos AND OutboundDate IS NULL";

            var shipmentInboundIds = conn.Query<dynamic>(getIdSql, new { TrackingNos = trackingNos })
                .ToDictionary(x => (string)x.TrackingNo, x => (int)x.Id);

            var updateSql = @"
                UPDATE [jetf].[dbo].[ShipmentInbound]
                SET OutboundDate = @OutboundDate,
                    OutboundTrackingNo = @OutboundTrackingNo,
                    OutboundTime = GETDATE(),
                    OutboundOpe = @OutboundOpe,
                    WarehouseProcessType = '1',
                    WarehouseProcessTime = GETDATE(),
                    WarehouseProcessOpe = @OutboundOpe
                WHERE TrackingNo = @TrackingNo AND OutboundDate IS NULL";

            var insertHistorySql = @"
                INSERT INTO [jetf].[dbo].[ShipmentInboundEditHistory]
                ([ShipmentInboundId], [FieldName], [OldValue], [NewValue], [EditTime], [EditUser])
                VALUES
                (@ShipmentInboundId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)";

            var userId = GetUserId();

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    conn.Execute(updateSql, shipmentOutboundList, transaction);

                    foreach (var shipment in shipmentOutboundList)
                    {
                        if (shipmentInboundIds.ContainsKey(shipment.TrackingNo))
                        {
                            var shipmentInboundId = shipmentInboundIds[shipment.TrackingNo];

                            conn.Execute(insertHistorySql, new
                            {
                                ShipmentInboundId = shipmentInboundId,
                                FieldName = "出庫日期",
                                OldValue = string.Empty,
                                NewValue = shipment.OutboundDate.ToString("yyyy/MM/dd"),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            }, transaction);
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            conn.Close();
        }
    }
}
