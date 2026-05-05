using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentOutboundBatchImport.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
                        WarehouseProcessTypeText = row.GetCellData(3),
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

                if (string.IsNullOrWhiteSpace(shipment.WarehouseProcessTypeText))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "處理狀態為空";
                    continue;
                }

                var warehouseProcessTypeValue = shipment.WarehouseProcessTypeText
                    .Trim()
                    .ToEnumValueByDescription<WarehouseProcessType>();

                if (!warehouseProcessTypeValue.HasValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "處理狀態只允許填入：已出庫、已銷毀、已退運";
                    continue;
                }

                var warehouseProcessType = (WarehouseProcessType)warehouseProcessTypeValue.Value;
                if (warehouseProcessType != WarehouseProcessType.OutBound
                    && warehouseProcessType != WarehouseProcessType.Disposed
                    && warehouseProcessType != WarehouseProcessType.Returned)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "處理狀態只允許填入：已出庫、已銷毀、已退運";
                    continue;
                }

                shipment.WarehouseProcessType = warehouseProcessType;
            }

            var validList = shipmentOutboundList
                .Where(x => x.UploadStatus != "失敗")
                .ToList();

            if (validList.Count == 0)
                return;

            var trackingNos = validList.Select(x => x.TrackingNo).Distinct().ToList();
            Dictionary<string, ShipmentInboundProcessType> existingDict;

            using (var db = CreateJetfDbContext())
            {
                existingDict = db.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => !x.OutboundDate.HasValue && trackingNos.Contains(x.TrackingNo) && x.ProcessType.HasValue)
                    .Select(x => new
                    {
                        x.TrackingNo,
                        ProcessType = (ShipmentInboundProcessType)x.ProcessType.Value
                    })
                    .ToDictionary(x => x.TrackingNo, x => x.ProcessType);
            }

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

            using (var db = CreateJetfDbContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var shipmentInboundIds = db.ShipmentInbounds
                            .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                            .ToDictionary(x => x.TrackingNo, x => x);

                        foreach (var shipment in shipmentOutboundList)
                        {
                            if (!shipmentInboundIds.ContainsKey(shipment.TrackingNo))
                            {
                                continue;
                            }

                            var entity = shipmentInboundIds[shipment.TrackingNo];
                            entity.OutboundDate = shipment.OutboundDate;
                            entity.OutboundTrackingNo = shipment.OutboundTrackingNo;
                            entity.OutboundTime = DateTime.Now;
                            entity.OutboundOpe = shipment.OutboundOpe;
                            entity.WarehouseProcessType = (byte)shipment.WarehouseProcessType.Value;
                            entity.WarehouseProcessTime = DateTime.Now;
                            entity.WarehouseProcessOpe = shipment.OutboundOpe;

                            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = entity.Id,
                                FieldName = "出庫日期",
                                OldValue = string.Empty,
                                NewValue = shipment.OutboundDate.ToString("yyyy/MM/dd"),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
