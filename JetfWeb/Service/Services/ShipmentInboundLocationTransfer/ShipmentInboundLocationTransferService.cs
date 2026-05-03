using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Service.Extensions;
using Service.Services.ShipmentInboundLocationTransfer.Domain;

namespace Service.Services.ShipmentInboundLocationTransfer
{
    public class ShipmentInboundLocationTransferService : _BaseService
    {
        /// <summary>
        /// 查詢儲位資料
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>儲位資料列表</returns>
        public LocationTransferResponse GetData(LocationTransferRequest request)
        {
            using (var db = CreateJetfDbContext())
            {
                var query = db.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => !x.OutboundDate.HasValue);

                query = query.WhereIf(!string.IsNullOrWhiteSpace(request.LocationCode), x => x.LocationCode == request.LocationCode);
                query = query.WhereIf(!string.IsNullOrWhiteSpace(request.TrackingNo), x => x.TrackingNo == request.TrackingNo);
                query = query.WhereIf(!string.IsNullOrWhiteSpace(request.SeqNo), x => x.SeqNo == request.SeqNo);

                var data = query
                    .OrderBy(x => x.Id)
                    .Select(x => new LocationTransferModel
                    {
                        Id = x.Id,
                        TrackingNo = x.TrackingNo,
                        LocationCode = x.LocationCode
                    })
                    .ToList();

                return new LocationTransferResponse
                {
                    Data = data,
                    TotalCount = data.Count
                };
            }
        }

        /// <summary>
        /// 更新儲位並記錄歷史
        /// </summary>
        /// <param name="request">更新請求</param>
        public void UpdateLocation(LocationTransferUpdateRequest request)
        {
            if (request.Ids == null || request.Ids.Count == 0)
            {
                throw new Exception("未選擇任何資料");
            }

            if (string.IsNullOrWhiteSpace(request.NewLocationCode))
            {
                throw new Exception("新儲位不可為空");
            }

            var userId = GetUserId();

            using (var db = CreateJetfDbContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var existingData = db.ShipmentInbounds
                            .Where(x => request.Ids.Contains(x.Id))
                            .ToList();

                        if (existingData.Count == 0)
                        {
                            throw new Exception("查無資料");
                        }

                        foreach (var item in existingData)
                        {
                            db.ShipmentInboundLocationHistories.Add(new Data.ShipmentInboundLocationHistoryEntity
                            {
                                ShipmentInboundId = item.Id,
                                OldLocationCode = item.LocationCode,
                                NewLocationCode = request.NewLocationCode,
                                CreatedOpe = userId,
                                CreatedTime = DateTime.Now
                            });

                            item.LocationCode = request.NewLocationCode;
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
