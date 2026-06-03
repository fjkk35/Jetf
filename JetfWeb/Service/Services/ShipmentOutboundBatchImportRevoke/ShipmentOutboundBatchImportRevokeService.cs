using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentOutboundBatchImportRevoke.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentOutboundBatchImportRevoke
{
    public class ShipmentOutboundBatchImportRevokeService : _BaseService
    {
        public ShipmentOutboundBatchImportRevokeService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 批量取消貨件出庫
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="userName">使用者名稱</param>
        /// <returns></returns>
        public ResponseModel RevokeOutbound(string filePath)
        {
            try
            {
                var revokeList = ReadExcelFile(filePath);

                if (revokeList.Count == 0)
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                ValidateAndFetchData(revokeList);

                var failList = revokeList.Where(x => x.Status == "失敗").ToList();

                if (failList.Count > 0)
                {
                    return new ResponseModel(new
                    {
                        count = 0,
                        failCount = failList.Count,
                        data = revokeList,
                        message = $"上傳失敗，共 {failList.Count} 筆資料有錯誤，請修正後重新上傳"
                    });
                }

                var successList = revokeList.Where(x => x.Status == "成功").ToList();

                RevokeOutboundData(successList);

                return new ResponseModel(new
                {
                    count = successList.Count,
                    failCount = 0,
                    data = revokeList,
                    message = $"成功取消 {successList.Count} 筆出庫資料"
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
        /// <returns></returns>
        private List<ShipmentOutboundRevokeModel> ReadExcelFile(string filePath)
        {
            var revokeList = new List<ShipmentOutboundRevokeModel>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var trackingNo = row.GetCellData(0);

                    if (string.IsNullOrWhiteSpace(trackingNo))
                        continue;

                    var model = new ShipmentOutboundRevokeModel
                    {
                        TrackingNo = trackingNo,
                        Status = "待處理"
                    };

                    revokeList.Add(model);
                }
            }

            return revokeList;
        }

        /// <summary>
        /// 驗證並取得資料
        /// </summary>
        /// <param name="revokeList">取消出庫資料列表</param>
        private void ValidateAndFetchData(List<ShipmentOutboundRevokeModel> revokeList)
        {
            if (revokeList.Count == 0)
                return;

            var trackingNos = revokeList.Select(x => x.TrackingNo).Distinct().ToList();

            var threeDaysAgo = DateTime.Now.Date.AddDays(-3);
            Dictionary<string, Data.ShipmentInboundEntity> dataDict;

            {
                dataDict = JetfDb.ShipmentInbounds
                        .AsNoTracking()
                        .Where(x => trackingNos.Contains(x.TrackingNo) && x.OutboundDate.HasValue)
                        .GroupBy(x => x.TrackingNo)
                        .Select(g => g
                            .OrderByDescending(x => x.OutboundDate)
                            .FirstOrDefault())
                        .ToDictionary(x => x.TrackingNo, x => x);
            }

            foreach (var revoke in revokeList)
            {
                if (string.IsNullOrWhiteSpace(revoke.TrackingNo))
                {
                    revoke.Status = "失敗";
                    revoke.FailReason = "單號為空";
                    continue;
                }

                if (!dataDict.ContainsKey(revoke.TrackingNo))
                {
                    revoke.Status = "失敗";
                    revoke.FailReason = "查無此單號或此單號未出庫";
                    continue;
                }

                var data = dataDict[revoke.TrackingNo];
                revoke.ShipmentInboundId = data.Id;
                revoke.OutboundDate = data.OutboundDate;
                revoke.OutboundTrackingNo = data.OutboundTrackingNo;

                if (revoke.OutboundDate.HasValue && revoke.OutboundDate.Value.Date < threeDaysAgo)
                {
                    revoke.Status = "失敗";
                    revoke.FailReason = $"出庫日期 {revoke.OutboundDate.Value:yyyy/MM/dd} 已超過 3 天，無法取消";
                    continue;
                }

                revoke.Status = "成功";
                revoke.FailReason = string.Empty;
            }
        }

        /// <summary>
        /// 取消出庫資料
        /// </summary>
        /// <param name="successList">成功的資料列表</param>
        /// <param name="userName">使用者名稱</param>
        private void RevokeOutboundData(List<ShipmentOutboundRevokeModel> successList)
        {
            if (successList.Count == 0)
                return;

            var ids = successList
                .Where(x => x.ShipmentInboundId.HasValue)
                .Select(x => x.ShipmentInboundId.Value)
                .ToList();

            {
                using (var transaction = JetfDb.Database.BeginTransaction())
                {
                    try
                    {
                        var entities = JetfDb.ShipmentInbounds
                            .Where(x => ids.Contains(x.Id))
                            .ToDictionary(x => x.Id, x => x);

                        foreach (var item in successList)
                        {
                            if (!item.ShipmentInboundId.HasValue || !entities.ContainsKey(item.ShipmentInboundId.Value))
                            {
                                continue;
                            }

                            var entity = entities[item.ShipmentInboundId.Value];
                            entity.OutboundDate = null;
                            entity.OutboundTrackingNo = null;
                            entity.OutboundTime = null;
                            entity.OutboundOpe = null;
                            entity.WarehouseProcessType = null;
                            entity.WarehouseProcessTime = null;
                            entity.WarehouseProcessOpe = null;

                            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = item.ShipmentInboundId.Value,
                                FieldName = "出庫日期",
                                OldValue = item.OutboundDate.HasValue ? item.OutboundDate.Value.ToString("yyyy/MM/dd") : string.Empty,
                                NewValue = string.Empty,
                                EditTime = DateTime.Now,
                                EditUser = GetUserId()
                            });
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
        }
    }
}
