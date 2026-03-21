using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
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
            var sql = @"
                        SELECT 
                            Id,
                            TrackingNo,
                            LocationCode
                        FROM [jetf].[dbo].[ShipmentInbound]
                        WHERE OutboundDate IS NULL
                    ";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(request.LocationCode))
            {
                sql += " AND LocationCode = @LocationCode";
                parameters.Add("LocationCode", request.LocationCode);
            }

            if (!string.IsNullOrWhiteSpace(request.TrackingNo))
            {
                sql += " AND TrackingNo = @TrackingNo";
                parameters.Add("TrackingNo", request.TrackingNo);
            }

            sql += " ORDER BY Id";

            var data = conn.Query<LocationTransferModel>(sql, parameters).ToList();

            return new LocationTransferResponse
            {
                Data = data,
                TotalCount = data.Count
            };
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

            var selectSql = @"
                SELECT 
                    Id,
                    LocationCode
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE Id IN @Ids";

            var existingData = conn.Query<LocationTransferModel>(selectSql, new { Ids = request.Ids }).ToList();

            if (existingData.Count == 0)
            {
                throw new Exception("查無資料");
            }

            var updateSql = @"
                UPDATE [jetf].[dbo].[ShipmentInbound]
                SET LocationCode = @NewLocationCode
                WHERE Id = @Id";

            var insertHistorySql = @"
                INSERT INTO [jetf].[dbo].[ShipmentInboundLocationHistory]
                (ShipmentInboundId, OldLocationCode, NewLocationCode, CreatedOpe, CreatedTime)
                VALUES
                (@ShipmentInboundId, @OldLocationCode, @NewLocationCode, @CreatedOpe, @CreatedTime)";

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var item in existingData)
                    {
                        conn.Execute(updateSql, new
                        {
                            Id = item.Id,
                            NewLocationCode = request.NewLocationCode
                        }, transaction);

                        conn.Execute(insertHistorySql, new
                        {
                            ShipmentInboundId = item.Id,
                            OldLocationCode = item.LocationCode,
                            NewLocationCode = request.NewLocationCode,
                            CreatedOpe = GetUserId(),
                            CreatedTime = DateTime.Now
                        }, transaction);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }
        }
    }
}
