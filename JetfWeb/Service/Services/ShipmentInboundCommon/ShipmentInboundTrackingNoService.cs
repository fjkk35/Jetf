using Service.EnumTax;
using Service.Extensions;
using Service.Services.ShipmentInboundBatchImport.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Service.Services.ShipmentInboundCommon
{
    public class ShipmentInboundTrackingNoService : _BaseService
    {
        public void EnrichShipmentData(List<ShipmentInboundModel> shipmentInboundList)
        {
            if (shipmentInboundList == null || shipmentInboundList.Count == 0)
            {
                return;
            }

            var trackingNos = shipmentInboundList
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .Select(x => x.TrackingNo.Trim())
                .Distinct()
                .ToList();

            if (trackingNos.Count == 0)
            {
                return;
            }

            var seaData = QuerySeaOrderData(trackingNos);
            var airData = QueryAirOrderData(trackingNos);
            var airDataByDeliveryNo = QueryAirOrderDataByDeliveryNo(trackingNos);
            var feeData = QueryFeeData(trackingNos);

            foreach (var shipment in shipmentInboundList)
            {
                ResetResolvedFields(shipment);

                if (string.IsNullOrWhiteSpace(shipment.TrackingNo))
                {
                    continue;
                }

                var trackingNo = shipment.TrackingNo.Trim();
                shipment.TrackingNo = trackingNo;

                if (seaData.ContainsKey(trackingNo))
                {
                    var seaOrder = seaData[trackingNo];
                    shipment.DataType = "海運";
                    shipment.IsOrderOriginal = true;
                    shipment.OriginalJetfSerial = seaOrder.OriginalJetfSerial;
                    shipment.OriginalTrackingNo = seaOrder.OriginalTrackingNo;
                    shipment.ImporterAddr = seaOrder.ImporterAddr;
                    shipment.ImporterPhone = seaOrder.ImporterPhone;
                    shipment.Importer = seaOrder.Importer;
                    shipment.CustCode = seaOrder.CustCode;
                    shipment.TransName = seaOrder.TransName;
                }
                else if (airData.ContainsKey(trackingNo))
                {
                    var airOrder = airData[trackingNo];
                    shipment.DataType = "空運";
                    shipment.IsOrderOriginal = true;
                    shipment.OriginalJetfSerial = airOrder.OriginalJetfSerial;
                    shipment.OriginalTrackingNo = airOrder.OriginalTrackingNo;
                    shipment.Importer = airOrder.Importer;
                    shipment.ImporterPhone = airOrder.ImporterPhone;
                    shipment.ImporterAddr = airOrder.ImporterAddr;
                    shipment.CustCode = airOrder.CustCode;
                    shipment.TransNo = airOrder.TransNo;
                }
                else if (airDataByDeliveryNo.ContainsKey(trackingNo))
                {
                    var airOrder = airDataByDeliveryNo[trackingNo];
                    shipment.DataType = "空運";
                    shipment.IsOrderOriginal = true;
                    shipment.OriginalJetfSerial = airOrder.OriginalJetfSerial;
                    shipment.OriginalTrackingNo = airOrder.OriginalTrackingNo;
                    shipment.Importer = airOrder.Importer;
                    shipment.ImporterPhone = airOrder.ImporterPhone;
                    shipment.ImporterAddr = airOrder.ImporterAddr;
                    shipment.CustCode = airOrder.CustCode;
                    shipment.TransNo = airOrder.TransNo;
                }

                if (feeData.ContainsKey(trackingNo))
                {
                    var fee = feeData[trackingNo];
                    shipment.Tax = fee.Tax ?? 0;
                    shipment.Ccfee = fee.Ccfee ?? 0;
                    shipment.Cod = fee.Cod ?? 0;
                    shipment.Fee = 30;
                }
            }
        }

        public void CheckDuplicateData(
            List<ShipmentInboundModel> shipmentInboundList,
            IEnumerable<int> excludedShipmentInboundIds = null,
            bool validateLocationCode = true,
            bool validateSourceType = true)
        {
            if (shipmentInboundList == null || shipmentInboundList.Count == 0)
            {
                return;
            }

            var validSourceTypes = EnumerableExtensions.GetValidDescriptions<ShipmentInboundSourceType>();

            foreach (var shipment in shipmentInboundList)
            {
                if (string.IsNullOrWhiteSpace(shipment.TrackingNo) || shipment.InboundDate == DateTime.MinValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "入庫日期或追蹤單號為空";
                    continue;
                }

                if (validateLocationCode && string.IsNullOrWhiteSpace(shipment.LocationCode))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "儲位為空";
                    continue;
                }

                if (validateSourceType && !string.IsNullOrWhiteSpace(shipment.SourceType))
                {
                    if (!validSourceTypes.Contains(shipment.SourceType))
                    {
                        shipment.UploadStatus = "失敗";
                        shipment.FailReason = string.Format("貨件來源 '{0}' 不在有效範圍內", shipment.SourceType);
                        continue;
                    }

                    shipment.SourceTypeDisplay = shipment.SourceType;
                }
            }

            var validList = shipmentInboundList
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo)
                    && x.InboundDate != DateTime.MinValue
                    && x.UploadStatus != "失敗")
                .ToList();

            if (validList.Count == 0)
            {
                return;
            }

            var trackingNos = validList.Select(x => x.TrackingNo).Distinct().ToList();
            var excludedIdList = (excludedShipmentInboundIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            var threeDaysAgo = DateTime.Now.Date.AddDays(-3);
            Dictionary<string, List<DateTime?>> existingDict;

            using (var db = CreateJetfDbContext())
            {
                var query = db.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.TrackingNo));

                if (excludedIdList.Count > 0)
                {
                    query = query.Where(x => !excludedIdList.Contains(x.Id));
                }

                existingDict = query
                    .Select(x => new
                    {
                        x.TrackingNo,
                        x.OutboundDate
                    })
                    .ToList()
                    .GroupBy(x => x.TrackingNo)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.OutboundDate).ToList());
            }

            foreach (var shipment in validList)
            {
                if (!existingDict.ContainsKey(shipment.TrackingNo))
                {
                    shipment.UploadStatus = "成功";
                    shipment.FailReason = string.Empty;
                    continue;
                }

                var outboundDates = existingDict[shipment.TrackingNo];

                if (outboundDates.Any(d => !d.HasValue))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "單號重複";
                    continue;
                }

                var recentDate = outboundDates.FirstOrDefault(d => d.HasValue && d.Value.Date >= threeDaysAgo);
                if (recentDate.HasValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = string.Format("此單號已出庫且出庫日期 {0:yyyy/MM/dd} 未超過 3 天，無法重新入庫", recentDate.Value);
                    continue;
                }

                shipment.UploadStatus = "成功";
                shipment.FailReason = string.Empty;
            }
        }

        private void ResetResolvedFields(ShipmentInboundModel shipment)
        {
            shipment.DataType = null;
            shipment.OriginalJetfSerial = null;
            shipment.OriginalTrackingNo = null;
            shipment.CustCode = null;
            shipment.TransNo = null;
            shipment.TransName = null;
            shipment.Importer = null;
            shipment.ImporterPhone = null;
            shipment.ImporterAddr = null;
            shipment.IsOrderOriginal = false;
            shipment.Tax = 0;
            shipment.Ccfee = 0;
            shipment.Cod = 0;
            shipment.Fee = 0;
        }

        private Dictionary<string, ShipmentOrderData> QuerySeaOrderData(List<string> trackingNos)
        {
            using (var db = CreateDataCenterDbContext())
            {
                return db.SeaOrderOriginals
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.JetfSerial))
                    .GroupBy(x => x.JetfSerial)
                    .Select(g => g.OrderByDescending(x => x.Gw ?? 0)
                        .Select(x => new ShipmentOrderData
                        {
                            TrackingNo = x.JetfSerial,
                            OriginalJetfSerial = x.JetfSerial,
                            OriginalTrackingNo = x.BlNo,
                            ImporterAddr = x.ImporterAddr,
                            ImporterPhone = x.ImporterPhone,
                            Importer = x.Importer,
                            CustCode = x.CustCode,
                            TransName = x.TransName
                        })
                        .FirstOrDefault())
                    .ToDictionary(x => x.TrackingNo, x => x);
            }
        }

        private Dictionary<string, ShipmentOrderData> QueryAirOrderData(List<string> trackingNos)
        {
            using (var db = CreateDataCenterDbContext())
            {
                return db.OriginalLists
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.TrackingNo))
                    .GroupBy(x => x.TrackingNo)
                    .Select(g => g.Select(x => new ShipmentOrderData
                    {
                        TrackingNo = x.TrackingNo,
                        OriginalJetfSerial = x.DeliveryNo,
                        OriginalTrackingNo = x.TrackingNo,
                        Importer = x.Importer,
                        ImporterPhone = x.ImporterPhone,
                        ImporterAddr = x.ImporterAddr,
                        CustCode = x.CustCode,
                        TransNo = x.TransNo.ToString()
                    }).FirstOrDefault())
                    .ToDictionary(x => x.TrackingNo, x => x);
            }
        }

        private Dictionary<string, ShipmentOrderData> QueryAirOrderDataByDeliveryNo(List<string> trackingNos)
        {
            using (var db = CreateDataCenterDbContext())
            {
                return db.OriginalLists
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.DeliveryNo))
                    .GroupBy(x => x.DeliveryNo)
                    .Select(g => g.Select(x => new ShipmentOrderData
                    {
                        DeliveryNo = x.DeliveryNo,
                        TrackingNo = x.TrackingNo,
                        OriginalJetfSerial = x.DeliveryNo,
                        OriginalTrackingNo = x.TrackingNo,
                        Importer = x.Importer,
                        ImporterPhone = x.ImporterPhone,
                        ImporterAddr = x.ImporterAddr,
                        CustCode = x.CustCode,
                        TransNo = x.TransNo.ToString()
                    }).FirstOrDefault())
                    .ToDictionary(x => x.DeliveryNo, x => x);
            }
        }

        private Dictionary<string, ShipmentFeeData> QueryFeeData(List<string> trackingNos)
        {
            using (var db = CreateJetfDbContext())
            {
                return db.FeeMasters
                    .AsNoTracking()
                    .Where(x => x.Download == "1" && trackingNos.Contains(x.DlvInv) && x.IncludeTax == "N")
                    .Select(x => new ShipmentFeeData
                    {
                        TrackingNo = x.DlvInv,
                        Tax = (x.Tax1 ?? 0) + (x.Tax2 ?? 0),
                        Ccfee = x.Ccfee,
                        Cod = x.Cod,
                        Fee = x.Fee
                    })
                    .ToDictionary(x => x.TrackingNo, x => x);
            }
        }
    }
}
