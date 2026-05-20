using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundBatchImport.Domain;
using Service.Services.ShipmentInboundCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundBatchImport
{
    public class ShipmentInboundBatchImportService : _BaseService
    {
        private readonly ShipmentInboundTrackingNoService _trackingNoService;

        public ShipmentInboundBatchImportService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, ShipmentInboundTrackingNoService trackingNoService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _trackingNoService = trackingNoService;
        }

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
            _trackingNoService.EnrichShipmentData(shipmentInboundList);
        }

        /// <summary>
        /// 檢查重複資料
        /// </summary>
        /// <param name="shipmentInboundList">貨件入庫資料列表</param>
        private void CheckDuplicateData(List<ShipmentInboundModel> shipmentInboundList)
        {
            _trackingNoService.CheckDuplicateData(shipmentInboundList, validateSeqNo: true);
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
            {
                using (var transaction = JetfDb.Database.BeginTransaction())
                {
                    try
                    {
                        var entities = shipmentInboundList.Select(x => new Data.ShipmentInboundEntity
                        {
                            DataType = x.DataType,
                            InboundDate = x.InboundDate,
                            MainNumber = x.MainNumber,
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
}
