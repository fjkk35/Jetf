using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundBatchImport.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Service.Services.ShipmentInboundBatchImport
{
    public class ShipmentInboundBatchImportService : _BaseService
    {
        /// <summary>
        /// 批量上傳貨件入庫資料
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <returns></returns>
        public ResponseModel UploadShipmentInbound(string filePath)
        {
            try
            {
                var shipmentInboundList = ReadExcelFile(filePath);

                if (shipmentInboundList.Count == 0)
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                EnrichShipmentData(shipmentInboundList);

                CheckDuplicateData(shipmentInboundList);

                var failList = shipmentInboundList.FindAll(x => x.UploadStatus == "失敗");
                var successList = shipmentInboundList.FindAll(x => x.UploadStatus == "成功");

                if (successList.Count > 0)
                {
                    ConvertSourceTypeToEnumValue(successList);
                    InsertShipmentInbound(successList);
                }

                var responseMessage = $"成功 {successList.Count} 筆，失敗 {failList.Count} 筆";

                var response = new ResponseModel
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

                return response;
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
        private List<ShipmentInboundModel> ReadExcelFile(string filePath)
        {
            var shipmentInboundList = new List<ShipmentInboundModel>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var model = new ShipmentInboundModel
                    {
                        InboundDate = ParseDate(row.GetCellData(0)),
                        TrackingNo = row.GetCellData(1),
                        SeqNo = row.GetCellData(2),
                        LocationCode = row.GetCellData(3),
                        SourceType = row.GetCellData(4),
                        ReturnTrackingNo = row.GetCellData(5),
                        Size = row.GetCellData(6),
                        ReturnReason = row.GetCellData(7),
                        UploadOpe = GetUserId()
                    };

                    if (model.InboundDate == DateTime.MinValue)
                        continue;

                    shipmentInboundList.Add(model);
                }
            }

            return shipmentInboundList;
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
        /// 補充貨件資料
        /// </summary>
        /// <param name="shipmentInboundList">貨件入庫資料列表</param>
        private void EnrichShipmentData(List<ShipmentInboundModel> shipmentInboundList)
        {
            var trackingNos = shipmentInboundList
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .Select(x => x.TrackingNo)
                .Distinct()
                .ToList();

            if (trackingNos.Count == 0)
                return;

            var seaData = QuerySeaOrderData(trackingNos);
            var etlData = QueryEtlOrderData(trackingNos);
            var etlData2 = QueryEtlOrderDataByDeliveryNo(trackingNos);
            var feeData = QueryFeeData(trackingNos);

            foreach (var shipment in shipmentInboundList)
            {
                if (string.IsNullOrWhiteSpace(shipment.TrackingNo))
                    continue;

                if (seaData.ContainsKey(shipment.TrackingNo))
                {
                    var seaOrder = seaData[shipment.TrackingNo];
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
                else if (etlData.ContainsKey(shipment.TrackingNo))
                {
                    var etlOrder = etlData[shipment.TrackingNo];
                    shipment.DataType = "空運";
                    shipment.IsOrderOriginal = true;
                    shipment.OriginalJetfSerial = etlOrder.OriginalJetfSerial;
                    shipment.OriginalTrackingNo = etlOrder.OriginalTrackingNo;
                    shipment.Importer = etlOrder.Importer;
                    shipment.ImporterPhone = etlOrder.ImporterPhone;
                    shipment.ImporterAddr = etlOrder.ImporterAddr;
                    shipment.CustCode = etlOrder.CustCode;
                    shipment.TransNo = etlOrder.TransNo;
                }
                else if (etlData2.ContainsKey(shipment.TrackingNo))
                {
                    var etlOrder = etlData2[shipment.TrackingNo];
                    shipment.DataType = "空運";
                    shipment.IsOrderOriginal = true;
                    shipment.OriginalJetfSerial = etlOrder.OriginalJetfSerial;
                    shipment.OriginalTrackingNo = etlOrder.OriginalTrackingNo;
                    shipment.Importer = etlOrder.Importer;
                    shipment.ImporterPhone = etlOrder.ImporterPhone;
                    shipment.ImporterAddr = etlOrder.ImporterAddr;
                    shipment.CustCode = etlOrder.CustCode;
                    shipment.TransNo = etlOrder.TransNo;
                }

                if (feeData.ContainsKey(shipment.TrackingNo))
                {
                    var fee = feeData[shipment.TrackingNo];
                    shipment.Tax = fee.Tax ?? 0;
                    shipment.Ccfee = fee.Ccfee ?? 0;
                    shipment.Cod = fee.Cod ?? 0;
                    //手續費固定30
                    shipment.Fee = 30;
                }
            }
        }

        /// <summary>
        /// 查詢海運訂單資料
        /// </summary>
        /// <param name="trackingNos">追蹤單號列表</param>
        /// <returns></returns>
        private Dictionary<string, ShipmentOrderData> QuerySeaOrderData(List<string> trackingNos)
        {
            if (trackingNos.Count == 0)
                return new Dictionary<string, ShipmentOrderData>();

            using (var db = CreateDataCenterDbContext())
            {
                return db.SeaOrderOriginals
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.JetfSerial))
                    .GroupBy(x => x.JetfSerial)
                    .Select(g => g
                        .OrderByDescending(x => x.Gw ?? 0)
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

        /// <summary>
        /// 查詢空運訂單資料
        /// </summary>
        /// <param name="trackingNos">追蹤單號列表</param>
        /// <returns></returns>
        private Dictionary<string, ShipmentOrderData> QueryEtlOrderData(List<string> trackingNos)
        {
            if (trackingNos.Count == 0)
                return new Dictionary<string, ShipmentOrderData>();

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

        /// <summary>
        /// 查詢空運訂單資料
        /// </summary>
        /// <param name="trackingNos">追蹤單號列表</param>
        /// <returns></returns>
        private Dictionary<string, ShipmentOrderData> QueryEtlOrderDataByDeliveryNo(List<string> trackingNos)
        {
            if (trackingNos.Count == 0)
                return new Dictionary<string, ShipmentOrderData>();

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

        /// <summary>
        /// 查詢稅金資料
        /// </summary>
        /// <param name="trackingNos">追蹤單號列表</param>
        /// <returns></returns>
        private Dictionary<string, ShipmentFeeData> QueryFeeData(List<string> trackingNos)
        {
            if (trackingNos.Count == 0)
                return new Dictionary<string, ShipmentFeeData>();

            using (var db = CreateJetfDbContext())
            {
                return db.FeeMasters
                    .AsNoTracking()
                    .Where(x => x.Download == "1" && trackingNos.Contains(x.TrackingNo) && x.IncludeTax =="N")
                    .Select(x => new ShipmentFeeData
                    {
                        TrackingNo = x.TrackingNo,
                        Tax = (x.Tax1 ?? 0) + (x.Tax2 ?? 0),
                        Ccfee = x.Ccfee,
                        Cod = x.Cod,
                        Fee = x.Fee
                    })
                    .ToDictionary(x => x.TrackingNo, x => x);
            }
        }

        /// <summary>
        /// 檢查重複資料
        /// </summary>
        /// <param name="shipmentInboundList">貨件入庫資料列表</param>
        private void CheckDuplicateData(List<ShipmentInboundModel> shipmentInboundList)
        {
            var validSourceTypes = EnumerableExtensions.GetValidDescriptions<ShipmentInboundSourceType>();

            foreach (var shipment in shipmentInboundList)
            {
                if (string.IsNullOrWhiteSpace(shipment.TrackingNo) || shipment.InboundDate == DateTime.MinValue)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "入庫日期或追蹤單號為空";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(shipment.LocationCode))
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "儲位為空";
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(shipment.SourceType))
                {
                    if (!validSourceTypes.Contains(shipment.SourceType))
                    {
                        shipment.UploadStatus = "失敗";
                        shipment.FailReason = $"貨件來源 '{shipment.SourceType}' 不在有效範圍內";
                        continue;
                    }
                    
                    shipment.SourceTypeDisplay = shipment.SourceType;
                }
            }

            var validList = shipmentInboundList
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo) && 
                           x.InboundDate != DateTime.MinValue &&
                           x.UploadStatus != "失敗")
                .ToList();

            if (validList.Count == 0)
                return;

            var trackingNos = validList.Select(x => x.TrackingNo).Distinct().ToList();
            var threeDaysAgo = DateTime.Now.Date.AddDays(-3);
            Dictionary<string, List<DateTime?>> existingDict;

            using (var db = CreateJetfDbContext())
            {
                existingDict = db.ShipmentInbounds
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

                var hasUnoutbound = outboundDates.Any(d => !d.HasValue);
                if (hasUnoutbound)
                {
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = "單號重複";
                    continue;
                }

                var hasRecentOutbound = outboundDates.Any(d => d.HasValue && d.Value.Date >= threeDaysAgo);
                if (hasRecentOutbound)
                {
                    var recentDate = outboundDates.First(d => d.HasValue && d.Value.Date >= threeDaysAgo);
                    shipment.UploadStatus = "失敗";
                    shipment.FailReason = $"此單號已出庫且出庫日期 {recentDate.Value:yyyy/MM/dd} 未超過 3 天，無法重新入庫";
                    continue;
                }

                shipment.UploadStatus = "成功";
                shipment.FailReason = string.Empty;
            }
        }

        /// <summary>
        /// 將貨件來源轉換為 Enum 數值
        /// </summary>
        /// <param name="shipmentInboundList">貨件入庫資料列表</param>
        private void ConvertSourceTypeToEnumValue(List<ShipmentInboundModel> shipmentInboundList)
        {
            foreach (var shipment in shipmentInboundList)
            {
                if (!string.IsNullOrWhiteSpace(shipment.SourceType))
                {
                    var enumValue = shipment.SourceType.ToEnumValueByDescription<ShipmentInboundSourceType>();
                    if (enumValue.HasValue)
                    {
                        shipment.SourceType = enumValue.Value.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// 批量寫入資料庫
        /// </summary>
        /// <param name="shipmentInboundList">貨件入庫資料列表</param>
        private void InsertShipmentInbound(List<ShipmentInboundModel> shipmentInboundList)
        {
            using (var db = CreateJetfDbContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var entities = shipmentInboundList.Select(x => new Data.ShipmentInboundEntity
                        {
                            DataType = x.DataType,
                            InboundDate = x.InboundDate,
                            TrackingNo = x.TrackingNo,
                            OriginalJetfSerial = x.OriginalJetfSerial,
                            OriginalTrackingNo = x.OriginalTrackingNo,
                            SeqNo = x.SeqNo,
                            LocationCode = x.LocationCode,
                            SourceType = byte.TryParse(x.SourceType, out var sourceType) ? (byte?)sourceType : null,
                            ReturnTrackingNo = x.ReturnTrackingNo,
                            Size = x.Size,
                            CustCode = x.CustCode,
                            TransNo = x.TransNo,
                            TransName = x.TransName,
                            Importer = x.Importer,
                            ImporterPhone = x.ImporterPhone,
                            ImporterAddr = x.ImporterAddr,
                            Tax = x.Tax,
                            Ccfee = x.Ccfee,
                            Cod = x.Cod,
                            Fee = x.Fee,
                            ReturnReason = x.ReturnReason,
                            IsOrderOriginal = x.IsOrderOriginal,
                            UploadOpe = x.UploadOpe,
                            CreatedTime = DateTime.Now
                        }).ToList();

                        db.ShipmentInbounds.AddRange(entities);
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
