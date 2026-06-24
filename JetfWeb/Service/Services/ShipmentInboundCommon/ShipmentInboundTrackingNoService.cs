using Service.EnumTax;
using Service.Extensions;
using Service.Services.ShipmentInboundBatchImport.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace Service.Services.ShipmentInboundCommon
{
    public class ShipmentInboundTrackingNoService : _BaseService
    {
        public ShipmentInboundTrackingNoService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

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
            var originalJetfSerials = seaData.Values
                .Concat(airData.Values)
                .Concat(airDataByDeliveryNo.Values)
                .Where(x => !string.IsNullOrWhiteSpace(x.OriginalJetfSerial))
                .Select(x => x.OriginalJetfSerial)
                .Distinct()
                .ToList();

            var feeData = QueryFeeData(originalJetfSerials);

            foreach (var shipment in shipmentInboundList)
            {
                ResetResolvedFields(shipment);

                if (string.IsNullOrWhiteSpace(shipment.TrackingNo))
                {
                    continue;
                }

                var trackingNo = shipment.TrackingNo.Trim();
                shipment.TrackingNo = trackingNo;

                ShipmentOrderData orderData = null;

                if (seaData.ContainsKey(trackingNo))
                {
                    var seaOrder = seaData[trackingNo];
                    orderData = seaOrder;
                    shipment.DataType = "海運";
                    shipment.IsOrderOriginal = true;
                    shipment.MainNumber = seaOrder.MainNumber;
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
                    orderData = airOrder;
                    shipment.DataType = "空運";
                    shipment.IsOrderOriginal = true;
                    shipment.MainNumber = airOrder.MainNumber;
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
                    orderData = airOrder;
                    shipment.DataType = "空運";
                    shipment.IsOrderOriginal = true;
                    shipment.MainNumber = airOrder.MainNumber;
                    shipment.OriginalJetfSerial = airOrder.OriginalJetfSerial;
                    shipment.OriginalTrackingNo = airOrder.OriginalTrackingNo;
                    shipment.Importer = airOrder.Importer;
                    shipment.ImporterPhone = airOrder.ImporterPhone;
                    shipment.ImporterAddr = airOrder.ImporterAddr;
                    shipment.CustCode = airOrder.CustCode;
                    shipment.TransNo = airOrder.TransNo;
                }

                if (!string.IsNullOrWhiteSpace(shipment.OriginalJetfSerial)
                    && feeData.ContainsKey(shipment.OriginalJetfSerial))
                {
                    var fee = feeData[shipment.OriginalJetfSerial];
                    shipment.Tax = fee.Tax ?? 0;
                    shipment.Ccfee = fee.Ccfee ?? 0;
                    shipment.Cod = fee.Cod ?? 0;
                    shipment.Fee = CalculateFee(shipment.Tax, shipment.Ccfee, null);
                }
                else if (orderData != null)
                {
                    shipment.Cod = orderData.FallbackCod;
                    shipment.Fee = 0;
                }
            }
        }

        public void CheckDuplicateData(
            List<ShipmentInboundModel> shipmentInboundList,
            IEnumerable<int> excludedShipmentInboundIds = null,
            bool validateSeqNo = false,
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
                shipment.SeqNo = shipment.SeqNo?.Trim();

                if (string.IsNullOrWhiteSpace(shipment.TrackingNo) || shipment.InboundDate == DateTime.MinValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "入庫日期或追蹤單號為空";
                    continue;
                }

                if (validateSeqNo && string.IsNullOrWhiteSpace(shipment.SeqNo))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "流水編號為空";
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
                    && (!validateSeqNo || !string.IsNullOrWhiteSpace(x.SeqNo))
                    && x.UploadStatus != "失敗")
                .ToList();

            if (validList.Count == 0)
            {
                return;
            }

            if (validateSeqNo)
            {
                var duplicateSeqNos = validList
                    .GroupBy(x => x.SeqNo, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var shipment in validList.Where(x => duplicateSeqNos.Contains(x.SeqNo)))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "流水編號重複";
                }

                validList = validList
                    .Where(x => x.UploadStatus != "失敗")
                    .ToList();

                if (validList.Count == 0)
                {
                    return;
                }
            }

            var trackingNos = validList.Select(x => x.TrackingNo).Distinct().ToList();
            var seqNos = validateSeqNo
                ? validList.Select(x => x.SeqNo).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();
            var excludedIdList = (excludedShipmentInboundIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            var threeDaysAgo = DateTime.Now.Date.AddDays(-3);
            Dictionary<string, List<DateTime?>> existingDict;
            HashSet<string> existingSeqNoSet;

            if (validateSeqNo)
            {
                var seqNoQuery = JetfDb.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => seqNos.Contains(x.SeqNo));

                if (excludedIdList.Count > 0)
                {
                    seqNoQuery = seqNoQuery.Where(x => !excludedIdList.Contains(x.Id));
                }

                existingSeqNoSet = seqNoQuery
                    .Select(x => x.SeqNo)
                    .ToList()
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var shipment in validList.Where(x => existingSeqNoSet.Contains(x.SeqNo)))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "流水編號重複";
                }

                validList = validList
                    .Where(x => x.UploadStatus != "失敗")
                    .ToList();

                if (validList.Count == 0)
                {
                    return;
                }

                trackingNos = validList.Select(x => x.TrackingNo).Distinct().ToList();
            }

            {
                var query = JetfDb.ShipmentInbounds
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
            shipment.MainNumber = null;
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
            {
                var data = DataCenterDb.SeaOrderOriginals
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.JetfSerial))
                    .Select(x => new
                    {
                        x.JetfSerial,
                        x.MainNumber,
                        x.BlNo,
                        x.ImporterAddress,
                        x.ImporterPhone,
                        x.Importer,
                        x.DespatchName,
                        x.TransName,
                        x.Gw,
                        x.Cc
                    })
                    .ToList();

                return data
                    .GroupBy(x => x.JetfSerial)
                    .Select(g => g.OrderByDescending(x => x.Gw ?? 0)
                        .Select(x => new ShipmentOrderData
                        {
                            TrackingNo = x.JetfSerial,
                            MainNumber = x.MainNumber,
                            OriginalJetfSerial = x.JetfSerial,
                            OriginalTrackingNo = x.BlNo,
                            ImporterAddr = x.ImporterAddress,
                            ImporterPhone = x.ImporterPhone,
                            Importer = x.Importer,
                            CustCode = x.DespatchName,
                            TransName = x.TransName,
                            FallbackCod = ParseAmountToInt(x.Cc)
                        })
                        .FirstOrDefault())
                    .ToDictionary(x => x.TrackingNo, x => x);
            }
        }

        private Dictionary<string, ShipmentOrderData> QueryAirOrderData(List<string> trackingNos)
        {
            {
                var data = DataCenterDb.OriginalLists
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.TrackingNo))
                    .Select(x => new
                    {
                        x.MainNumber,
                        x.TrackingNo,
                        x.DeliveryNo,
                        x.Recipient,
                        x.RecPhone,
                        x.RecAddress,
                        x.DespatchNo,
                        x.ClearanceWarehousing,
                        x.Cc
                    })
                    .ToList();

                return data
                    .GroupBy(x => x.TrackingNo)
                    .Select(g => g.Select(x => new ShipmentOrderData
                    {
                        MainNumber = x.MainNumber,
                        TrackingNo = x.TrackingNo,
                        OriginalJetfSerial = x.DeliveryNo,
                        OriginalTrackingNo = x.TrackingNo,
                        Importer = x.Recipient,
                        ImporterPhone = x.RecPhone,
                        ImporterAddr = x.RecAddress,
                        CustCode = x.DespatchNo,
                        TransNo = (x.ClearanceWarehousing ?? 0).ToString(),
                        FallbackCod = ParseAmountToInt(x.Cc)
                    }).FirstOrDefault())
                    .ToDictionary(x => x.TrackingNo, x => x);
            }
        }

        private Dictionary<string, ShipmentOrderData> QueryAirOrderDataByDeliveryNo(List<string> trackingNos)
        {
            {
                var data = DataCenterDb.OriginalLists
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.DeliveryNo))
                    .Select(x => new
                    {
                        x.MainNumber,
                        x.DeliveryNo,
                        x.TrackingNo,
                        x.Recipient,
                        x.RecPhone,
                        x.RecAddress,
                        x.DespatchNo,
                        x.ClearanceWarehousing,
                        x.Cc
                    })
                    .ToList();

                return data
                    .GroupBy(x => x.DeliveryNo)
                    .Select(g => g.Select(x => new ShipmentOrderData
                    {
                        MainNumber = x.MainNumber,
                        DeliveryNo = x.DeliveryNo,
                        TrackingNo = x.TrackingNo,
                        OriginalJetfSerial = x.DeliveryNo,
                        OriginalTrackingNo = x.TrackingNo,
                        Importer = x.Recipient,
                        ImporterPhone = x.RecPhone,
                        ImporterAddr = x.RecAddress,
                        CustCode = x.DespatchNo,
                        TransNo = (x.ClearanceWarehousing ?? 0).ToString(),
                        FallbackCod = ParseAmountToInt(x.Cc)
                    }).FirstOrDefault())
                    .ToDictionary(x => x.DeliveryNo, x => x);
            }
        }

        private Dictionary<string, ShipmentFeeData> QueryFeeData(List<string> originalJetfSerials)
        {
            if (originalJetfSerials == null || originalJetfSerials.Count == 0)
            {
                return new Dictionary<string, ShipmentFeeData>();
            }

            {
                var data = JetfDb.FeeMasters
                    .AsNoTracking()
                    .Where(x => x.Download == "1" && originalJetfSerials.Contains(x.DlvInv) && x.IncludeTax == "N")
                    .Select(x => new ShipmentFeeData
                    {
                        TrackingNo = x.DlvInv,
                        Tax = (x.Tax1 ?? 0) + (x.Tax2 ?? 0) - (x.CustomerCod ?? 0),
                        Ccfee = x.Ccfee,
                        Cod = x.Cod,
                        Fee = x.Fee
                    })
                    .ToList();

                return data
                    .GroupBy(x => x.TrackingNo)
                    .Select(g => g.First())
                    .ToDictionary(x => x.TrackingNo, x => x);
            }
        }

        private int ParseAmountToInt(decimal? amount)
        {
            return amount.HasValue ? decimal.ToInt32(decimal.Truncate(amount.Value)) : 0;
        }

        private int ParseAmountToInt(double? amount)
        {
            return amount.HasValue ? decimal.ToInt32(decimal.Truncate(Convert.ToDecimal(amount.Value))) : 0;
        }

        private int CalculateFee(int? tax, int? ccfee, int? freightFee)
        {
            return (tax ?? 0) > 0
                || (ccfee ?? 0) > 0
                || (freightFee ?? 0) > 0
                ? 30
                : 0;
        }

        private int ParseAmountToInt(string amount)
        {
            if (string.IsNullOrWhiteSpace(amount))
            {
                return 0;
            }

            if (decimal.TryParse(amount, NumberStyles.Any, CultureInfo.CurrentCulture, out var currentCultureValue))
            {
                return decimal.ToInt32(decimal.Truncate(currentCultureValue));
            }

            if (decimal.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantCultureValue))
            {
                return decimal.ToInt32(decimal.Truncate(invariantCultureValue));
            }

            return 0;
        }
    }
}
