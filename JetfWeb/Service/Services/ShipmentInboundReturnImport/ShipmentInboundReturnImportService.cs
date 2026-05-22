using Service.EnumTax;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundReturnImport.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundReturnImport
{
    public class ShipmentInboundReturnImportService : _BaseService
    {
        public ShipmentInboundReturnImportService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public ResponseModel UploadShipmentInbound(string filePath, string dataType)
        {
            try
            {
                var normalizedDataType = NormalizeDataType(dataType);
                if (string.IsNullOrWhiteSpace(normalizedDataType))
                {
                    return new ResponseModel("進口方式只允許海運或空運");
                }

                var shipmentInboundList = ReadExcelFile(filePath, normalizedDataType);

                if (shipmentInboundList.Count == 0)
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                ResolveCustomerAndTransCodes(shipmentInboundList);
                ValidateData(shipmentInboundList);
                CheckDuplicateData(shipmentInboundList);

                var failList = shipmentInboundList.FindAll(x => x.UploadStatus == "失敗");
                var successList = shipmentInboundList.FindAll(x => x.UploadStatus == "成功");

                if (failList.Count > 0)
                {
                    var failMessage = $"上傳失敗，共 {failList.Count} 筆資料有錯誤，整批未寫入";

                    return new ResponseModel
                    {
                        IsSuccess = false,
                        status = Status.error,
                        msg = failMessage,
                        ReturnObject = new
                        {
                            count = 0,
                            failCount = failList.Count,
                            data = failList,
                            message = failMessage
                        }
                    };
                }

                if (successList.Count > 0)
                {
                    InsertShipmentInbound(successList);
                }

                var responseMessage = $"成功 {successList.Count} 筆，失敗 {failList.Count} 筆";

                return new ResponseModel
                {
                    IsSuccess = successList.Count > 0,
                    status = successList.Count > 0 ? Status.success : Status.error,
                    msg = responseMessage,
                    ReturnObject = new
                    {
                        count = successList.Count,
                        failCount = failList.Count,
                        data = failList,
                        message = responseMessage
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.Message}");
            }
        }

        private List<ShipmentInboundReturnImportModel> ReadExcelFile(string filePath, string dataType)
        {
            var shipmentInboundList = new List<ShipmentInboundReturnImportModel>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var model = new ShipmentInboundReturnImportModel
                    {
                        DataType = dataType,
                        InboundDateText = row.GetCellData(0),
                        VendorName = row.GetCellData(1),
                        DispatchName = row.GetCellData(2),
                        TrackingNo = row.GetCellData(3),
                        SeqNo = row.GetCellData(4),
                        LocationCode = row.GetCellData(5),
                        SourceType = row.GetCellData(6),
                        WarehouseProcessTypeText = row.GetCellData(7),
                        OutboundDateText = row.GetCellData(8),
                        Remark = row.GetCellData(9),
                        ReturnReason = row.GetCellData(10),
                        ReturnTrackingNo = row.GetCellData(11),
                        Size = row.GetCellData(12),
                        OutboundTrackingNo = row.GetCellData(13),
                        UnknownShipmentFlag = row.GetCellData(14),
                        UploadOpe = GetUserId()
                    };

                    if (IsEmptyRow(model))
                    {
                        continue;
                    }

                    shipmentInboundList.Add(model);
                }
            }

            return shipmentInboundList;
        }

        private string NormalizeDataType(string dataType)
        {
            var normalized = dataType?.Trim();
            if (normalized == "海運" || normalized == "空運")
            {
                return normalized;
            }

            return null;
        }

        private bool IsEmptyRow(ShipmentInboundReturnImportModel model)
        {
            return string.IsNullOrWhiteSpace(model.InboundDateText)
                && string.IsNullOrWhiteSpace(model.VendorName)
                && string.IsNullOrWhiteSpace(model.DispatchName)
                && string.IsNullOrWhiteSpace(model.TrackingNo)
                && string.IsNullOrWhiteSpace(model.SeqNo)
                && string.IsNullOrWhiteSpace(model.LocationCode)
                && string.IsNullOrWhiteSpace(model.SourceType)
                && string.IsNullOrWhiteSpace(model.WarehouseProcessTypeText)
                && string.IsNullOrWhiteSpace(model.OutboundDateText)
                && string.IsNullOrWhiteSpace(model.Remark)
                && string.IsNullOrWhiteSpace(model.ReturnReason)
                && string.IsNullOrWhiteSpace(model.ReturnTrackingNo)
                && string.IsNullOrWhiteSpace(model.Size)
                && string.IsNullOrWhiteSpace(model.OutboundTrackingNo)
                && string.IsNullOrWhiteSpace(model.UnknownShipmentFlag);
        }

        private bool TryParseDate(string dateString, out DateTime result)
        {
            return DateTime.TryParse(dateString, out result);
        }

        private void ResolveCustomerAndTransCodes(List<ShipmentInboundReturnImportModel> shipmentInboundList)
        {
            var seaVendorNames = shipmentInboundList
                .Where(x => x.DataType == "海運" && !string.IsNullOrWhiteSpace(x.VendorName))
                .Select(x => x.VendorName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var airVendorNames = shipmentInboundList
                .Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.VendorName))
                .Select(x => x.VendorName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var airDispatchNames = shipmentInboundList
                .Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.DispatchName))
                .Select(x => x.DispatchName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var seaCustomerCodeMap = GetSeaCustomerCodeMap(seaVendorNames);
            var airCustomerCodeMap = GetAirCustomerCodeMap(airVendorNames);
            var airTransNoMap = GetAirTransNoMap(airDispatchNames);

            foreach (var shipment in shipmentInboundList)
            {
                shipment.VendorName = shipment.VendorName?.Trim();
                shipment.DispatchName = shipment.DispatchName?.Trim();
                shipment.TrackingNo = shipment.TrackingNo?.Trim();
                shipment.SeqNo = shipment.SeqNo?.Trim();
                shipment.LocationCode = shipment.LocationCode?.Trim();
                shipment.SourceType = shipment.SourceType?.Trim();
                shipment.WarehouseProcessTypeText = shipment.WarehouseProcessTypeText?.Trim();
                shipment.ReturnReason = shipment.ReturnReason?.Trim();
                shipment.ReturnTrackingNo = shipment.ReturnTrackingNo?.Trim();
                shipment.Size = shipment.Size?.Trim();
                shipment.Remark = shipment.Remark?.Trim();
                shipment.OutboundTrackingNo = shipment.OutboundTrackingNo?.Trim();
                shipment.UnknownShipmentFlag = shipment.UnknownShipmentFlag?.Trim();
                shipment.TransName = shipment.DispatchName;

                if (!string.IsNullOrWhiteSpace(shipment.UnknownShipmentFlag)
                    && !string.Equals(shipment.UnknownShipmentFlag, "V", StringComparison.OrdinalIgnoreCase))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "是否不明貨件只允許空白或 V";
                    continue;
                }

                shipment.IsOrderOriginal = !string.Equals(shipment.UnknownShipmentFlag, "V", StringComparison.OrdinalIgnoreCase);

                if (!shipment.IsOrderOriginal)
                {
                    continue;
                }

                if (shipment.DataType == "海運")
                {
                    if (!string.IsNullOrWhiteSpace(shipment.VendorName)
                        && seaCustomerCodeMap.ContainsKey(shipment.VendorName))
                    {
                        shipment.CustCode = seaCustomerCodeMap[shipment.VendorName];
                    }
                }
                else if (shipment.DataType == "空運")
                {
                    if (!string.IsNullOrWhiteSpace(shipment.VendorName)
                        && airCustomerCodeMap.ContainsKey(shipment.VendorName))
                    {
                        shipment.CustCode = airCustomerCodeMap[shipment.VendorName];
                    }

                    if (!string.IsNullOrWhiteSpace(shipment.DispatchName)
                        && airTransNoMap.ContainsKey(shipment.DispatchName))
                    {
                        shipment.TransNo = airTransNoMap[shipment.DispatchName];
                    }
                }
            }
        }

        private Dictionary<string, string> GetSeaCustomerCodeMap(IEnumerable<string> vendorNames)
        {
            var names = (vendorNames ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!names.Any())
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == "SEA" && names.Contains(x.CustName))
                .Select(x => new { x.CustName, x.CustCode })
                .ToList()
                .GroupBy(x => x.CustName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.CustCode).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, string> GetAirCustomerCodeMap(IEnumerable<string> vendorNames)
        {
            var names = (vendorNames ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!names.Any())
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == "AIR" && !string.IsNullOrEmpty(x.OldCode) && names.Contains(x.CustName))
                .Select(x => new { x.CustName, x.OldCode })
                .ToList()
                .GroupBy(x => x.CustName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.OldCode).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, string> GetAirTransNoMap(IEnumerable<string> dispatchNames)
        {
            var names = (dispatchNames ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!names.Any())
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return JetfDb.CustomerMasters
                .AsNoTracking()
                .Where(x => x.TranType == "空運" && names.Contains(x.TransName))
                .Select(x => new { x.TransName, x.TransNo })
                .ToList()
                .GroupBy(x => x.TransName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.TransNo).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        private void ValidateData(List<ShipmentInboundReturnImportModel> shipmentInboundList)
        {
            foreach (var shipment in shipmentInboundList)
            {
                if (!TryParseDate(shipment.InboundDateText, out var inboundDate))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "清點日期為空或格式錯誤";
                    continue;
                }

                shipment.InboundDate = inboundDate;

                if (!string.IsNullOrWhiteSpace(shipment.OutboundDateText))
                {
                    if (!TryParseDate(shipment.OutboundDateText, out var outboundDate))
                    {
                        shipment.UploadStatus = "失敗";
                        shipment.FailReason = "出貨日期格式錯誤";
                        continue;
                    }

                    shipment.OutboundDate = outboundDate;
                }

                if (shipment.IsOrderOriginal && string.IsNullOrWhiteSpace(shipment.VendorName))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "廠商為空";
                    continue;
                }

                if (shipment.IsOrderOriginal && string.IsNullOrWhiteSpace(shipment.DispatchName))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "派件為空";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(shipment.TrackingNo))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "單號/袋號為空";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(shipment.SeqNo))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "流水編號為空";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(shipment.LocationCode))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "板號為空";
                    continue;
                }

                if (shipment.IsOrderOriginal && string.IsNullOrWhiteSpace(shipment.CustCode))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = shipment.DataType == "海運"
                        ? $"海運廠商 '{shipment.VendorName}' 找不到對應客戶代號"
                        : $"空運廠商 '{shipment.VendorName}' 找不到對應客戶代號";
                    continue;
                }

                var sourceTypeValue = shipment.SourceType.ToEnumValueByDescription<ShipmentInboundSourceType>();
                if (!sourceTypeValue.HasValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = $"貨物來源 '{shipment.SourceType}' 不在有效範圍內";
                    continue;
                }

                var warehouseProcessTypeValue = shipment.WarehouseProcessTypeText.ToEnumValueByDescription<WarehouseProcessType>();
                if (!warehouseProcessTypeValue.HasValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = $"貨物狀態 '{shipment.WarehouseProcessTypeText}' 不在有效範圍內";
                    continue;
                }

                shipment.SourceTypeDisplay = shipment.SourceType;
                shipment.SourceTypeCode = (byte)sourceTypeValue.Value;
                shipment.WarehouseProcessType = (byte)warehouseProcessTypeValue.Value;
                shipment.UploadStatus = "成功";
                shipment.FailReason = string.Empty;
            }
        }

        private void CheckDuplicateData(List<ShipmentInboundReturnImportModel> shipmentInboundList)
        {
            var validList = shipmentInboundList
                .Where(x => x.UploadStatus != "失敗")
                .ToList();

            if (validList.Count == 0)
            {
                return;
            }

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

            var seqNos = validList.Select(x => x.SeqNo).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingSeqNoSet = JetfDb.ShipmentInbounds
                .AsNoTracking()
                .Where(x => seqNos.Contains(x.SeqNo))
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

            var trackingNos = validList.Select(x => x.TrackingNo).Distinct().ToList();
            var threeDaysAgo = DateTime.Now.Date.AddDays(-3);
            var existingDict = JetfDb.ShipmentInbounds
                .AsNoTracking()
                .Where(x => trackingNos.Contains(x.TrackingNo))
                .Select(x => new
                {
                    x.TrackingNo,
                    x.OutboundDate
                })
                .ToList()
                .GroupBy(x => x.TrackingNo)
                .ToDictionary(g => g.Key, g => g.Select(x => x.OutboundDate).ToList());

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

        private void InsertShipmentInbound(List<ShipmentInboundReturnImportModel> shipmentInboundList)
        {
            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    var entities = shipmentInboundList.Select(x => new Data.ShipmentInboundEntity
                    {
                        DataType = x.DataType,
                        InboundDate = x.InboundDate,
                        TrackingNo = x.TrackingNo,
                        SeqNo = x.SeqNo,
                        LocationCode = x.LocationCode,
                        SourceType = x.SourceTypeCode,
                        CustCode = x.CustCode,
                        TransNo = x.TransNo,
                        TransName = x.TransName,
                        Remark = x.Remark,
                        ReturnReason = x.ReturnReason,
                        ReturnTrackingNo = x.ReturnTrackingNo,
                        Size = x.Size,
                        OutboundDate = x.OutboundDate,
                        OutboundTrackingNo = x.OutboundTrackingNo,
                        WarehouseProcessType = x.WarehouseProcessType,
                        IsOrderOriginal = x.IsOrderOriginal,
                        UploadOpe = x.UploadOpe,
                        CreatedTime = DateTime.Now
                    }).ToList();

                    JetfDb.ShipmentInbounds.AddRange(entities);
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