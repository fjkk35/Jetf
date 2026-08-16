using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
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
        public ShipmentOutboundBatchImportService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

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

            {
                existingDict = JetfDb.ShipmentInbounds
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
                else if (processType != ShipmentInboundProcessType.TransferBySystem)
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
            var userId = GetUserId();

            {
                using (var transaction = JetfDb.Database.BeginTransaction())
                {
                    try
                    {
                        var shipmentInboundIds = JetfDb.ShipmentInbounds
                            .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                            .ToDictionary(x => x.TrackingNo, x => x);
                        var feeMasterCodCandidates = new List<FeeMasterCodEntity>();
                        var feeMasterOutboundCandidates = new List<ShipmentInboundEntity>();

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

                            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = entity.Id,
                                FieldName = "出庫日期",
                                OldValue = string.Empty,
                                NewValue = shipment.OutboundDate.ToString("yyyy/MM/dd"),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });

                            // 僅開新單號重出且符合運費、手續費及稅金／報關費／到付款條件時，才建立 FEE_MASTER_COD。
                            if (IsCreateShipmentInboundFeeMasterCod(
                                entity,
                                shipment.ProcessType))
                            {
                                feeMasterCodCandidates.Add(CreateShipmentInboundFeeMasterCod(entity));
                            }

                            // 僅開新單號重出且稅金、報關費或到付款任一大於 0 時，
                            // 後續才檢查原始序號對應的 FEE_MASTER 是否可以更新出庫單號。
                            if (IsUpdateFeeMasterOutboundTrackingNo(
                                entity,
                                shipment.ProcessType))
                            {
                                feeMasterOutboundCandidates.Add(entity);
                            }
                        }

                        UpdateFeeMasterOutboundTrackingNos(feeMasterOutboundCandidates);
                        AddShipmentInboundFeeMasterCods(feeMasterCodCandidates);
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
        }

        /// <summary>
        /// 判斷出庫資料是否符合新增貨件入庫到付款資料的條件。
        /// </summary>
        /// <param name="entity">貨件入庫資料。</param>
        /// <param name="processType">出庫處理方式。</param>
        /// <returns>是否應新增 FEE_MASTER_COD。</returns>
        private static bool IsCreateShipmentInboundFeeMasterCod(
            ShipmentInboundEntity entity,
            ShipmentInboundProcessType processType)
        {
            return processType == ShipmentInboundProcessType.NewTrackingNo &&
                   entity.FreightFee > 0 &&
                   entity.Fee > 0 &&
                   entity.Tax == 0 &&
                   entity.Ccfee == 0 &&
                   entity.Cod == 0 &&
                   !string.IsNullOrWhiteSpace(entity.OutboundTrackingNo);
        }

        /// <summary>
        /// 判斷出庫資料是否符合更新 FEE_MASTER 出庫單號的條件。
        /// </summary>
        /// <param name="entity">貨件入庫資料。</param>
        /// <param name="processType">出庫處理方式。</param>
        /// <returns>是否應查找並更新 FEE_MASTER。</returns>
        private static bool IsUpdateFeeMasterOutboundTrackingNo(
            ShipmentInboundEntity entity,
            ShipmentInboundProcessType processType)
        {
            return processType == ShipmentInboundProcessType.NewTrackingNo &&
                   (entity.Tax > 0 || entity.Ccfee > 0 || entity.Cod > 0) &&
                   !string.IsNullOrWhiteSpace(entity.OriginalJetfSerial) &&
                   !string.IsNullOrWhiteSpace(entity.OutboundTrackingNo);
        }

        /// <summary>
        /// 依貨件入庫的原始序號批次查找 FEE_MASTER，只有所有明細尚未收回物流代收款時，
        /// 才更新 FEE_MASTER 的出庫單號。
        /// </summary>
        /// <param name="candidates">符合出庫條件的貨件入庫資料。</param>
        private void UpdateFeeMasterOutboundTrackingNos(
            List<ShipmentInboundEntity> candidates)
        {
            // 沒有符合條件的資料時，不需要查詢費用主檔。
            if (candidates == null || candidates.Count == 0)
            {
                return;
            }

            // 以原始序號批次查詢 FEE_MASTER.DLV_INV，避免逐筆查詢資料庫。
            var outboundTrackingBySerial = candidates
                .GroupBy(x => x.OriginalJetfSerial)
                .ToDictionary(x => x.Key, x => x.First().OutboundTrackingNo);

            var originalJetfSerials = outboundTrackingBySerial.Keys.ToList();
            var feeMasters = JetfDb.FeeMasters
                .AsNoTracking()
                .WhereBulkContains(
                    JetfDb,
                    originalJetfSerials,
                    x => x.DlvInv,
                    x => x)
                .ToList();

            if (feeMasters.Count == 0)
            {
                return;
            }

            // 一次取得所有對應明細，僅保留 RECEIVED_TO_DLV_COD 全部為 NULL 的 FEE_MASTER。
            var feeMasterIds = feeMasters.Select(x => x.Id).ToList();
            var feeMasterDetails = JetfDb.FeeMasterDetails
                .AsNoTracking()
                .WhereBulkContains(
                    JetfDb,
                    feeMasterIds,
                    x => x.FeeMasterId,
                    x => x)
                .ToList();
            var eligibleFeeMasterIds = feeMasterDetails
                .GroupBy(x => x.FeeMasterId)
                .Where(x => x.All(detail => !detail.ReceivedToDlvCod.HasValue))
                .Select(x => x.Key)
                .ToHashSet();

            // 同一 DLV_INV 若對應多筆主檔，符合條件的資料全部更新。
            var updates = feeMasters
                .Where(x => eligibleFeeMasterIds.Contains(x.Id) &&
                            outboundTrackingBySerial.ContainsKey(x.DlvInv))
                .ToList();
            foreach (var feeMaster in updates)
            {
                feeMaster.OutboundTrackingNo = outboundTrackingBySerial[feeMaster.DlvInv];
            }

            // 使用 EF 批次更新，並沿用出庫上傳外層交易。
            JetfDb.BulkUpdate(updates);
        }

        /// <summary>
        /// 建立由貨件入庫出庫流程產生的 FEE_MASTER_COD 資料。
        /// </summary>
        /// <param name="entity">已完成出庫欄位更新的貨件入庫資料。</param>
        /// <returns>待新增的 FEE_MASTER_COD 資料。</returns>
        private static FeeMasterCodEntity CreateShipmentInboundFeeMasterCod(
            ShipmentInboundEntity entity)
        {
            var freightFee = entity.FreightFee.GetValueOrDefault();
            var fee = entity.Fee.GetValueOrDefault();

            return new FeeMasterCodEntity
            {
                DataType = entity.DataType ?? string.Empty,
                MainNumber = entity.MainNumber ?? string.Empty,
                Customer = entity.CustCode,
                BagNumber = string.Empty,
                TrackingNo = entity.OriginalTrackingNo,
                DlvInv = entity.OutboundTrackingNo,
                Cc = 0,
                SignOutTime = entity.OutboundDate.Value,
                CreatedTime = DateTime.Now,
                FreightFee = freightFee,
                Fee = fee,
                ToDlvCod = freightFee + fee,
                IsShipmentInbound = true
            };
        }

        /// <summary>
        /// 批次檢查物流貨號後新增貨件入庫產生的 FEE_MASTER_COD。
        /// </summary>
        /// <param name="candidates">符合金額及處理方式條件的候選資料。</param>
        private void AddShipmentInboundFeeMasterCods(
            List<FeeMasterCodEntity> candidates)
        {
            // 沒有符合條件的候選資料時，不需要查詢或新增資料。
            if (candidates == null || candidates.Count == 0)
            {
                return;
            }

            // 整理物流貨號清單，供後續批次查詢使用。
            var dlvInvs = candidates
                .Select(x => x.DlvInv)
                .Distinct()
                .ToList();

            // 批次查詢 FEE_MASTER_COD 已存在的物流貨號，避免重複新增。
            var existingDlvInvs = JetfDb.FeeMasterCods
                .AsNoTracking()
                .Where(x => dlvInvs.Contains(x.DlvInv))
                .Select(x => x.DlvInv)
                .ToHashSet();

            // 排除資料庫已存在及同批次重複的物流貨號，每個物流貨號只保留一筆。
            var entities = candidates
                .Where(x => !existingDlvInvs.Contains(x.DlvInv))
                .GroupBy(x => x.DlvInv)
                .Select(x => x.First())
                .ToList();
            if (entities.Count > 0)
            {
                // 使用 EF Bulk Extensions 一次批次新增資料，並沿用外層交易。
                JetfDb.BulkInsert(entities);
            }
        }
    }
}
