using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ShipmentInboundBatchImport.Domain;
using Service.Services.ShipmentInboundCommon;
using Service.Services.ShipmentInboundProcess.Domain;
using Service.Services.ShipmentInboundRecord.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundRecord
{
    public class ShipmentInboundRecordService : _BaseService
    {
        private readonly ShipmentInboundTrackingNoService _trackingNoService = new ShipmentInboundTrackingNoService();

        /// <summary>
        /// 根據 Id 取得貨件詳細資料
        /// </summary>
        /// <param name="id">ShipmentInbound 的 Id</param>
        /// <returns>貨件詳細資料</returns>
        public ShipmentInboundRecordModel GetDetailById(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            using (var db = CreateJetfDbContext())
            {
                var data = db.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ShipmentInboundRecordModel
                    {
                        Id = x.Id,
                        DataType = x.DataType,
                        InboundDate = x.InboundDate,
                        CustCode = x.CustCode,
                        TransNo = x.TransNo,
                        TransName = x.TransName,
                        TrackingNo = x.TrackingNo,
                        IsOrderOriginal = x.IsOrderOriginal,
                        SourceType = x.SourceType.HasValue
                            ? (ShipmentInboundSourceType)x.SourceType.Value
                            : default(ShipmentInboundSourceType),
                        SeqNo = x.SeqNo,
                        LocationCode = x.LocationCode,
                        Size = x.Size,
                        ProcessType = x.ProcessType.HasValue ? (ShipmentInboundProcessType?)x.ProcessType.Value : null,
                        ReturnReason = x.ReturnReason,
                        ReturnTrackingNo = x.ReturnTrackingNo,
                        FreightPayerNo = x.FreightPayerNo.HasValue ? (ShipmentInboundFreightPayerNo?)x.FreightPayerNo.Value : null,
                        Tax = x.Tax ?? 0,
                        Fee = x.Fee ?? 0,
                        Ccfee = x.Ccfee ?? 0,
                        Cod = x.Cod ?? 0,
                        FreightFee = (int)(x.FreightFee ?? 0),
                        ProcessTransNo = x.ProcessTransNo.HasValue ? (ShipmentInboundProcessTransNo?)x.ProcessTransNo.Value : null,
                        ProcessTime = x.ProcessTime,
                        ProcessOpe = x.ProcessOpe,
                        Remark = x.Remark,
                        ProcessImporter = x.ProcessImporter,
                        ProcessImporterPhone = x.ProcessImporterPhone,
                        ProcessImporterAddr = x.ProcessImporterAddr,
                        StoreCode = x.StoreCode,
                        StoreName = x.StoreName,
                        CarNo = x.CarNo,
                        PickupTime = x.PickupTime,
                        OutboundDate = x.OutboundDate,
                        OutboundTime = x.OutboundTime,
                        OutboundOpe = x.OutboundOpe,
                        OutboundTrackingNo = x.OutboundTrackingNo,
                        WarehouseProcessType = x.WarehouseProcessType.HasValue ? (WarehouseProcessType?)x.WarehouseProcessType.Value : null,
                        WarehouseProcessTime = x.WarehouseProcessTime,
                        WarehouseProcessOpe = x.WarehouseProcessOpe
                    })
                    .FirstOrDefault();

                if (data == null)
                {
                    return null;
                }

                FillCustomerAndTransNames(new List<ShipmentInboundRecordModel> { data });

                var lazyLoadingEnabled = db.Configuration.LazyLoadingEnabled;
                var proxyCreationEnabled = db.Configuration.ProxyCreationEnabled;

                try
                {
                    db.Configuration.LazyLoadingEnabled = true;
                    db.Configuration.ProxyCreationEnabled = true;

                    var exceptions = db.ShipmentInboundExceptions
                        .Where(x => x.ShipmentInboundId == id)
                        .OrderByDescending(x => x.CreatedTime)
                        .ThenByDescending(x => x.Id)
                        .ToList();

                    var latestExceptionTime = exceptions
                        .Select(x => (DateTime?)x.CreatedTime)
                        .FirstOrDefault();

                    var latestExceptions = exceptions;
                    if (latestExceptionTime.HasValue)
                    {
                        latestExceptions = exceptions
                            .Where(x => x.CreatedTime == latestExceptionTime.Value)
                            .ToList();
                    }
                    else
                    {
                        latestExceptions = exceptions.Take(0).ToList();
                    }

                    data.ExceptionReason = latestExceptions
                        .Select(x => x.ExceptionReason == null ? null : x.ExceptionReason.Reason)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                    data.ExceptionFilePaths = exceptions
                        .Select(x => x.FilePath)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    data.ExceptionImages = exceptions
                        .Where(x => !string.IsNullOrWhiteSpace(x.FilePath))
                        .Select(x => new ShipmentInboundExceptionImageModel
                        {
                            FilePath = x.FilePath,
                            CreatedTime = x.CreatedTime
                        })
                        .ToList();
                }
                finally
                {
                    db.Configuration.LazyLoadingEnabled = lazyLoadingEnabled;
                    db.Configuration.ProxyCreationEnabled = proxyCreationEnabled;
                }

                return data;
            }
        }

        public string GetExceptionImagePath(int shipmentInboundId, int imageIndex)
        {
            if (shipmentInboundId <= 0 || imageIndex < 0)
            {
                return null;
            }

            using (var db = CreateJetfDbContext())
            {
                var filePaths = db.ShipmentInboundExceptions
                    .AsNoTracking()
                    .Where(x => x.ShipmentInboundId == shipmentInboundId)
                    .OrderByDescending(x => x.CreatedTime)
                    .ThenByDescending(x => x.Id)
                    .Select(x => x.FilePath)
                    .ToList()
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (imageIndex >= filePaths.Count)
                {
                    return null;
                }

                return filePaths[imageIndex];
            }
        }

        public ShipmentInboundRecordResponse GetData(ShipmentInboundRecordRequest request)
        {
            using (var db = CreateJetfDbContext())
            {
                var query = BuildWhereConditions(db.ShipmentInbounds.AsNoTracking(), request);
                var totalCount = query.Count();
                var data = query
                    .OrderByDescending(x => x.InboundDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => new ShipmentInboundRecordModel
                    {
                        Id = x.Id,
                        DataType = x.DataType,
                        InboundDate = x.InboundDate,
                        CustCode = x.CustCode,
                        TransNo = x.TransNo,
                        TransName = x.TransName,
                        TrackingNo = x.TrackingNo,
                        IsOrderOriginal = x.IsOrderOriginal,
                        SourceType = x.SourceType.HasValue ? (ShipmentInboundSourceType)x.SourceType.Value : default(ShipmentInboundSourceType),
                        SeqNo = x.SeqNo,
                        LocationCode = x.LocationCode,
                        Size = x.Size,
                        ProcessType = x.ProcessType.HasValue ? (ShipmentInboundProcessType?)x.ProcessType.Value : null,
                        ReturnTrackingNo = x.ReturnTrackingNo,
                        FreightPayerNo = x.FreightPayerNo.HasValue ? (ShipmentInboundFreightPayerNo?)x.FreightPayerNo.Value : null,
                        Tax = x.Tax ?? 0,
                        Fee = x.Fee ?? 0,
                        Ccfee = x.Ccfee ?? 0,
                        Cod = x.Cod ?? 0,
                        FreightFee = (int)(x.FreightFee ?? 0),
                        ProcessTransNo = x.ProcessTransNo.HasValue ? (ShipmentInboundProcessTransNo?)x.ProcessTransNo.Value : null,
                        ProcessTime = x.ProcessTime,
                        ProcessOpe = x.ProcessOpe,
                        Remark = x.Remark,
                        ProcessImporter = x.ProcessImporter,
                        ProcessImporterPhone = x.ProcessImporterPhone,
                        ProcessImporterAddr = x.ProcessImporterAddr,
                        StoreCode = x.StoreCode,
                        StoreName = x.StoreName,
                        CarNo = x.CarNo,
                        PickupTime = x.PickupTime,
                        OutboundDate = x.OutboundDate,
                        OutboundTime = x.OutboundTime,
                        OutboundOpe = x.OutboundOpe,
                        OutboundTrackingNo = x.OutboundTrackingNo,
                        WarehouseProcessType = x.WarehouseProcessType.HasValue ? (WarehouseProcessType?)x.WarehouseProcessType.Value : null,
                        WarehouseProcessTime = x.WarehouseProcessTime,
                        WarehouseProcessOpe = x.WarehouseProcessOpe
                    })
                    .ToList();

                FillCustomerAndTransNames(data);

                return new ShipmentInboundRecordResponse
                {
                    Data = data,
                    TotalCount = totalCount
                };
            }
        }

        private IQueryable<Data.ShipmentInboundEntity> BuildWhereConditions(
            IQueryable<Data.ShipmentInboundEntity> query,
            ShipmentInboundRecordRequest request)
        {
            query = query.WhereIf(DateTime.TryParse(request.InboundDateStart, out var startDate), x => x.InboundDate >= startDate);

            if (DateTime.TryParse(request.InboundDateEnd, out var endDate))
            {
                var inboundDateEnd = endDate.AddDays(1);
                query = query.WhereIf(true, x => x.InboundDate < inboundDateEnd);
            }

            query = query.WhereIf(!string.IsNullOrWhiteSpace(request.DataType), x => x.DataType == request.DataType);

            if (byte.TryParse(request.ProcessType, out var processType))
            {
                var targetProcessType = (ShipmentInboundProcessType)processType;
                query = query.WhereIf(true, x => x.ProcessType == targetProcessType);
            }

            if (!string.IsNullOrWhiteSpace(request.LocationCode))
            {
                query = query.WhereIf(true, x => x.LocationCode.Contains(request.LocationCode));
            }

            if (byte.TryParse(request.WarehouseProcessType, out var warehouseProcessType))
            {
                query = query.WhereIf(true, x => x.WarehouseProcessType == warehouseProcessType);
            }

            if (request.IsOrderOriginal.HasValue)
            {
                query = query.WhereIf(true, x => x.IsOrderOriginal == request.IsOrderOriginal.Value);
            }

            var custCodes = request.CustCodes?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            query = query.WhereIf(custCodes?.Any() == true, x => custCodes.Contains(x.CustCode));

            if (byte.TryParse(request.SourceType, out var sourceType))
            {
                query = query.WhereIf(true, x => x.SourceType == sourceType);
            }

            if (!string.IsNullOrWhiteSpace(request.TrackingNo))
            {
                query = query.WhereIf(true, x => x.TrackingNo.Contains(request.TrackingNo));
            }

            return query;
        }

        private Dictionary<string, string> GetAirCustNames(List<string> custCodes)
        {
            return GetAirCustomerNames(custCodes);
        }

        private Dictionary<string, string> GetSeaCustNames(List<string> custCodes)
        {
            return GetSeaCustomerNames(custCodes);
        }

        private Dictionary<string, string> GetAirTransNames(List<string> transNos)
        {
            return base.GetAirTransNames(transNos);
        }

        private void FillCustomerAndTransNames(List<ShipmentInboundRecordModel> data)
        {
            var airCustCodes = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var seaCustCodes = data.Where(x => x.DataType == "海運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var airTransNos = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.TransNo))
                                  .Select(x => x.TransNo)
                                  .Distinct()
                                  .ToList();

            var airCustNames = GetAirCustNames(airCustCodes);
            var seaCustNames = GetSeaCustNames(seaCustCodes);
            var airTransNames = GetAirTransNames(airTransNos);

            foreach (var item in data)
            {
                if (!string.IsNullOrWhiteSpace(item.CustCode))
                {
                    if (item.DataType == "空運" && airCustNames.ContainsKey(item.CustCode))
                    {
                        item.CustName = airCustNames[item.CustCode];
                    }
                    else if (item.DataType == "海運" && seaCustNames.ContainsKey(item.CustCode))
                    {
                        item.CustName = seaCustNames[item.CustCode];
                    }
                }

                if (item.DataType == "空運" && !string.IsNullOrWhiteSpace(item.TransNo) && airTransNames.ContainsKey(item.TransNo))
                {
                    item.TransName = airTransNames[item.TransNo];
                }
            }
        }

        /// <summary>
        /// 取得客戶清單
        /// </summary>
        public Dictionary<string,List<SelectListModel>> GetCustList()
        {
            using (var db = CreateDataCenterDbContext())
            {
                var seaData = db.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == "SEA")
                    .Select(x => new ShipmentInboundCustomerModel
                    {
                        Cust_Type = x.CustType,
                        Cust_Code = x.CustCode,
                        Cust_Name = x.CustName
                    });

                var airData = db.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == "AIR" && !string.IsNullOrEmpty(x.OldCode))
                    .Select(x => new ShipmentInboundCustomerModel
                    {
                        Cust_Type = x.CustType,
                        Cust_Code = x.OldCode,
                        Cust_Name = x.CustName
                    });

                var data = seaData
                    .Concat(airData)
                    .OrderBy(x => x.Cust_Code)
                    .ToList();

                return data.GroupBy(r => r.TypeName)
                    .ToDictionary(g => g.Key, g => g.Select(x => new SelectListModel
                    {
                        Value = x.Cust_Code,
                        Text = x.Cust_Name
                    }).ToList());
            }
        }

        /// <summary>
        /// 取得貨物來源清單
        /// </summary>
        public List<SelectListModel> GetSourceTypeList()
        {
            return Enum.GetValues(typeof(ShipmentInboundSourceType))
                .Cast<ShipmentInboundSourceType>()
                .Select(item => new SelectListModel
                {
                    Value = ((int)item).ToString(),
                    Text = item.ToDescription()
                }).ToList();
        }

        public ShipmentInboundRecordExportExcelResult GetExportExcel(ShipmentInboundRecordRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // 匯出不分頁：保留原查詢條件，但以大筆數避免被分頁限制
            var exportRequest = new ShipmentInboundRecordRequest
            {
                InboundDateStart = request.InboundDateStart,
                InboundDateEnd = request.InboundDateEnd,
                ProcessType = request.ProcessType,
                LocationCode = request.LocationCode,
                CustCode = request.CustCode,
                CustCodes = request.CustCodes,
                SourceType = request.SourceType,
                TrackingNo = request.TrackingNo,
                DataType = request.DataType,
                WarehouseProcessType = request.WarehouseProcessType,
                IsOrderOriginal = request.IsOrderOriginal,
                Page = 1,
                PageSize = 100000
            };

            var dataResult = GetData(exportRequest);
            var data = dataResult?.Data ?? new List<ShipmentInboundRecordModel>();

            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("報表");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");
            var dateTimeStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd hh:mm:ss");

            var headers = new List<string>
            {
                "序號",
                "入庫日期",
                "進口方式",
                "客戶",
                "單號",
                "貨件來源",
                "處理方式",
                "客服處理日期",
                "客服處理人",
                "出庫日期",
                "出庫操作日",
                "出庫操作人",
                "倉庫狀態",
                "倉庫狀態操作日",
                "倉庫狀態操作人",
                "重出派件公司",
                "收件人",
                "電話",
                "宅配地址",
                "門市店號",
                "門市名稱",
                "運費支付方",
                "代收款總金額",
                "備註",
                "流水號",
                "儲位",
                "重出日期",
                "重出單號",
                "到付款",
                "運費",
                "稅金",
                "報關費",
                "代收手續費"
            };

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var row = sheet.CreateRow(i + 1);

                int c = 0;
                NpoiCell.CreateIntCell(row, c++, i + 1, numberStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.InboundDate, dateStyle);
                NpoiCell.CreateCell(row, c++, item.DataType, dataStyle);
                NpoiCell.CreateCell(row, c++, string.IsNullOrWhiteSpace(item.CustName) ? item.CustCode : item.CustName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, c++, item.SourceTypeName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessTypeName, dataStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.ProcessTime, dateTimeStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessOpe, dataStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.OutboundDate, dateStyle);
                NpoiCell.CreateDateTimeCell(row, c++, item.OutboundTime, dateTimeStyle);
                NpoiCell.CreateCell(row, c++, item.OutboundOpe, dataStyle);

                NpoiCell.CreateCell(row, c++, item.WarehouseProcessName, dataStyle);
                NpoiCell.CreateDateTimeCell(row, c++, item.WarehouseProcessTime, dateTimeStyle);
                NpoiCell.CreateCell(row, c++, item.WarehouseProcessOpe, dataStyle);

                NpoiCell.CreateCell(row, c++, item.ProcessTransName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessImporter, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessImporterPhone, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessImporterAddr, dataStyle);
                NpoiCell.CreateCell(row, c++, item.StoreCode, dataStyle);
                NpoiCell.CreateCell(row, c++, item.StoreName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.FreightPayerName, dataStyle);

                NpoiCell.CreateIntCell(row, c++, item.TotalAmount, numberStyle);
                NpoiCell.CreateCell(row, c++, item.Remark, dataStyle);
                NpoiCell.CreateCell(row, c++, item.SeqNo, dataStyle);
                NpoiCell.CreateCell(row, c++, item.LocationCode, dataStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.OutboundDate, dateStyle);
                NpoiCell.CreateCell(row, c++, item.OutboundTrackingNo, dataStyle);

                NpoiCell.CreateIntCell(row, c++, item.Cod, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.FreightFee, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.Tax, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.Ccfee, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.Fee, numberStyle);
            }

            sheet.AutoSizeColumns(headers.Count, scale: 1.15, minWidth: 10);

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                bytes = ms.ToArray();
            }

            var fileName = $"貨件紀錄查詢_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return new ShipmentInboundRecordExportExcelResult
            {
                FileName = fileName,
                FileBytes = bytes
            };
        }

        /// <summary>
        /// 更新金額欄位
        /// </summary>
        public void UpdateAmount(UpdateAmountRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Id <= 0)
            {
                throw new ArgumentException("Id 不可為空");
            }

            if (string.IsNullOrWhiteSpace(request.FieldName))
            {
                throw new ArgumentException("FieldName 不可為空");
            }

            var allowedFields = new[] { "Cod", "Tax", "Ccfee" };
            if (!allowedFields.Contains(request.FieldName))
            {
                throw new ArgumentException("不允許修改此欄位");
            }

            using (var db = CreateJetfDbContext())
            {
                var entity = db.ShipmentInbounds.FirstOrDefault(x => x.Id == request.Id);
                if (entity == null)
                {
                    throw new ArgumentException("查無資料");
                }

                if (entity.OutboundDate.HasValue)
                {
                    throw new InvalidOperationException("已有出庫日期，稅金、到付款、報關費不可調整");
                }

                int? oldValue;
                switch (request.FieldName)
                {
                    case "Cod":
                        oldValue = entity.Cod;
                        entity.Cod = request.NewValue;
                        break;
                    case "Tax":
                        oldValue = entity.Tax;
                        entity.Tax = request.NewValue;
                        break;
                    case "Ccfee":
                        oldValue = entity.Ccfee;
                        entity.Ccfee = request.NewValue;
                        break;
                    default:
                        throw new ArgumentException("不允許修改此欄位");
                }

                var hasAmountChanged = oldValue != request.NewValue;
                var editTime = DateTime.Now;
                var editUser = GetUserId();

                if (hasAmountChanged)
                {
                    db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                    {
                        ShipmentInboundId = request.Id,
                        FieldName = request.FieldName,
                        OldValue = oldValue?.ToString(),
                        NewValue = request.NewValue.ToString(),
                        EditTime = editTime,
                        EditUser = editUser
                    });
                }

                var hasAnyAmount = (entity.Cod ?? 0) > 0
                    || (entity.Ccfee ?? 0) > 0
                    || (entity.FreightFee ?? 0) > 0
                    || (entity.Tax ?? 0) > 0;
                var targetFee = hasAnyAmount ? 30 : 0;
                var shouldUpdateFee = (entity.Fee ?? 0) != targetFee;

                if (shouldUpdateFee)
                {
                    var oldFee = entity.Fee;
                    entity.Fee = targetFee;

                    db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                    {
                        ShipmentInboundId = request.Id,
                        FieldName = "手續費",
                        OldValue = oldFee?.ToString(),
                        NewValue = targetFee.ToString(),
                        EditTime = editTime,
                        EditUser = editUser
                    });
                }

                if (!hasAmountChanged && !shouldUpdateFee)
                {
                    return;
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// 更新單號，並套用與批量上傳相同的單號補資料與重複檢查規則
        /// </summary>
        public void UpdateTrackingNo(UpdateTrackingNoRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Id <= 0)
            {
                throw new ArgumentException("Id 不可為空");
            }

            if (string.IsNullOrWhiteSpace(request.NewTrackingNo))
            {
                throw new ArgumentException("新單號不可為空");
            }

            using (var db = CreateJetfDbContext())
            {
                var entity = db.ShipmentInbounds.FirstOrDefault(x => x.Id == request.Id);
                if (entity == null)
                {
                    throw new ArgumentException("查無資料");
                }

                if (entity.IsOrderOriginal)
                {
                    throw new InvalidOperationException("只有不明貨件可修改單號");
                }

                var newTrackingNo = request.NewTrackingNo.Trim();
                if (string.Equals(entity.TrackingNo, newTrackingNo, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var shipment = new ShipmentInboundModel
                {
                    InboundDate = entity.InboundDate,
                    TrackingNo = newTrackingNo,
                    LocationCode = entity.LocationCode,
                    SourceType = entity.SourceType.HasValue
                        ? ((ShipmentInboundSourceType)entity.SourceType.Value).ToDescription()
                        : null
                };

                var shipmentInboundList = new List<ShipmentInboundModel> { shipment };
                _trackingNoService.EnrichShipmentData(shipmentInboundList);
                _trackingNoService.CheckDuplicateData(shipmentInboundList, new[] { entity.Id }, false, false);

                if (shipment.UploadStatus != "成功")
                {
                    throw new InvalidOperationException(shipment.FailReason);
                }

                var oldTrackingNo = entity.TrackingNo;
                entity.TrackingNo = shipment.TrackingNo;
                entity.DataType = shipment.DataType;
                entity.MainNumber = shipment.MainNumber;
                entity.OriginalJetfSerial = shipment.OriginalJetfSerial;
                entity.OriginalTrackingNo = shipment.OriginalTrackingNo;
                entity.CustCode = shipment.CustCode;
                entity.TransNo = shipment.TransNo;
                entity.TransName = shipment.TransName;
                entity.Importer = shipment.Importer;
                entity.ImporterPhone = shipment.ImporterPhone;
                entity.ImporterAddr = shipment.ImporterAddr;
                entity.Tax = shipment.Tax;
                entity.Ccfee = shipment.Ccfee;
                entity.Cod = shipment.Cod;
                entity.Fee = shipment.Fee;
                entity.IsOrderOriginal = shipment.IsOrderOriginal;

                db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                {
                    ShipmentInboundId = entity.Id,
                    FieldName = "TrackingNo",
                    OldValue = oldTrackingNo,
                    NewValue = shipment.TrackingNo,
                    EditTime = DateTime.Now,
                    EditUser = GetUserId()
                });

                db.SaveChanges();
            }
        }

        /// <summary>
        /// 取得編輯歷史記錄
        /// </summary>
        public List<ShipmentInboundEditHistoryModel> GetEditHistory(int shipmentInboundId)
        {
            if (shipmentInboundId <= 0)
            {
                return new List<ShipmentInboundEditHistoryModel>();
            }

            using (var db = CreateJetfDbContext())
            {
                return db.ShipmentInboundEditHistories
                    .AsNoTracking()
                    .Where(x => x.ShipmentInboundId == shipmentInboundId)
                    .OrderByDescending(x => x.EditTime)
                    .Select(x => new ShipmentInboundEditHistoryModel
                    {
                        Id = x.Id,
                        ShipmentInboundId = x.ShipmentInboundId,
                        FieldName = x.FieldName,
                        OldValue = x.OldValue,
                        NewValue = x.NewValue,
                        EditTime = x.EditTime,
                        EditUser = x.EditUser
                    })
                    .ToList();
            }
        }
    }
}
