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
        private readonly ShipmentInboundTrackingNoService _trackingNoService;

        public ShipmentInboundRecordService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, ShipmentInboundTrackingNoService trackingNoService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _trackingNoService = trackingNoService;
        }

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

            {
                var data = JetfDb.ShipmentInbounds
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
                FillCargoSignReceiptFlags(new List<ShipmentInboundRecordModel> { data });

                var lazyLoadingEnabled = JetfDb.Configuration.LazyLoadingEnabled;
                var proxyCreationEnabled = JetfDb.Configuration.ProxyCreationEnabled;

                try
                {
                    JetfDb.Configuration.LazyLoadingEnabled = true;
                    JetfDb.Configuration.ProxyCreationEnabled = true;

                    var exceptions = JetfDb.ShipmentInboundExceptions
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
                    JetfDb.Configuration.LazyLoadingEnabled = lazyLoadingEnabled;
                    JetfDb.Configuration.ProxyCreationEnabled = proxyCreationEnabled;
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

            {
                var filePaths = JetfDb.ShipmentInboundExceptions
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
            {
                var query = BuildWhereConditions(JetfDb.ShipmentInbounds.AsNoTracking(), request);
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
                        ReturnReason = x.ReturnReason,
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
                FillCargoSignReceiptFlags(data);

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

            if (request.WarehouseProcessTypeIsEmpty)
            {
                query = query.WhereIf(true, x => x.WarehouseProcessType == null);
            }
            else if (byte.TryParse(request.WarehouseProcessType, out var warehouseProcessType))
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

            if (!string.IsNullOrWhiteSpace(request.OutboundTrackingNo))
            {
                query = query.WhereIf(true, x => x.OutboundTrackingNo.Contains(request.OutboundTrackingNo));
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
        /// 一次查詢目前頁面單號與重出單號是否有簽收單，避免逐筆查詢。
        /// </summary>
        private void FillCargoSignReceiptFlags(List<ShipmentInboundRecordModel> data)
        {
            var serials = data
                .SelectMany(x => new[] { x.TrackingNo, x.OutboundTrackingNo })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!serials.Any())
            {
                return;
            }

            var receiptSerials = new HashSet<string>(JetfDb.CargoSignReceipts
                .AsNoTracking()
                .Where(x => serials.Contains(x.JetfSerial))
                .Select(x => x.JetfSerial)
                .Distinct()
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

            foreach (var item in data)
            {
                item.HasTrackingNoSignReceipt =
                    !string.IsNullOrWhiteSpace(item.TrackingNo) &&
                    receiptSerials.Contains(item.TrackingNo.Trim());
                item.HasOutboundTrackingNoSignReceipt =
                    !string.IsNullOrWhiteSpace(item.OutboundTrackingNo) &&
                    receiptSerials.Contains(item.OutboundTrackingNo.Trim());
            }
        }

        /// <summary>
        /// 取得客戶清單
        /// </summary>
        public Dictionary<string,List<SelectListModel>> GetCustList()
        {
            {
                var seaData = DataCenterDb.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == "SEA")
                    .Select(x => new ShipmentInboundCustomerModel
                    {
                        Cust_Type = x.CustType,
                        Cust_Code = x.CustCode,
                        Cust_Name = x.CustName
                    });

                var airData = DataCenterDb.SysCusts
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

        /// <summary>
        /// 取得不明貨件可選客戶清單。
        /// </summary>
        /// <param name="dataType">進口方式。</param>
        /// <returns>客戶下拉選單資料。</returns>
        public List<SelectListModel> GetUnknownShipmentCustList(string dataType)
        {
            {
                return BuildUnknownShipmentCustList(DataCenterDb, dataType);
            }
        }

        /// <summary>
        /// 取得不明貨件可選派件公司清單。
        /// </summary>
        /// <param name="dataType">進口方式。</param>
        /// <returns>派件公司下拉選單資料。</returns>
        public List<ShipmentInboundUnknownShipmentTransOptionModel> GetUnknownShipmentTransList(string dataType)
        {
            {
                return BuildUnknownShipmentTransList(JetfDb, dataType);
            }
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
        /// 更新不明貨件的基本資料。
        /// </summary>
        /// <param name="request">更新請求。</param>
        public void UpdateUnknownShipmentBasicInfo(UpdateUnknownShipmentBasicInfoRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Id <= 0)
            {
                throw new ArgumentException("Id 不可為空");
            }

            {
                var entity = JetfDb.ShipmentInbounds.FirstOrDefault(x => x.Id == request.Id);
                if (entity == null)
                {
                    throw new ArgumentException("查無資料");
                }

                if (entity.IsOrderOriginal)
                {
                    throw new InvalidOperationException("只有不明貨件可修改基本資料");
                }

                var hasDataTypeInput = !string.IsNullOrWhiteSpace(request.DataType);
                var hasCustCodeInput = !string.IsNullOrWhiteSpace(request.CustCode);
                var hasTransNoInput = request.TransNo != null;
                var hasTransNameInput = request.TransName != null;
                var hasSourceTypeInput = request.SourceType.HasValue;

                var dataType = hasDataTypeInput
                    ? NormalizeDataType(request.DataType)
                    : NormalizeDataType(entity.DataType);

                if (hasDataTypeInput && string.IsNullOrWhiteSpace(dataType))
                {
                    throw new ArgumentException("請選擇正確的進口方式");
                }

                var currentDataType = NormalizeDataType(entity.DataType);
                var isDataTypeChanged = !string.Equals(currentDataType, dataType, StringComparison.OrdinalIgnoreCase);

                var targetCustCode = hasCustCodeInput
                    ? (request.CustCode ?? string.Empty).Trim()
                    : (entity.CustCode ?? string.Empty).Trim();

                var targetTransNo = hasTransNoInput
                    ? (request.TransNo ?? string.Empty).Trim()
                    : (entity.TransNo ?? string.Empty).Trim();
                var targetTransName = hasTransNameInput
                    ? (request.TransName ?? string.Empty).Trim()
                    : (entity.TransName ?? string.Empty).Trim();

                var targetSourceType = hasSourceTypeInput
                    ? request.SourceType
                    : entity.SourceType;

                if (isDataTypeChanged && !hasCustCodeInput)
                {
                    targetCustCode = string.Empty;
                }

                if (isDataTypeChanged && !hasTransNoInput && !hasTransNameInput)
                {
                    targetTransNo = string.Empty;
                    targetTransName = string.Empty;
                }

                SelectListModel targetCustomer = null;
                if (hasCustCodeInput && !string.IsNullOrWhiteSpace(targetCustCode))
                {
                    var customerOptions = BuildUnknownShipmentCustList(DataCenterDb, dataType);
                    targetCustomer = customerOptions.FirstOrDefault(x => string.Equals(x.Value, targetCustCode, StringComparison.OrdinalIgnoreCase));
                    if (targetCustomer == null)
                    {
                        throw new ArgumentException("查無對應客戶資料");
                    }
                }

                ShipmentInboundUnknownShipmentTransOptionModel targetTrans = null;
                if (hasTransNoInput || hasTransNameInput)
                {
                    if (string.Equals(dataType, "海運", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(targetTransName))
                        {
                            var transOptions = BuildUnknownShipmentTransList(JetfDb, dataType);
                            targetTrans = transOptions.FirstOrDefault(x =>
                                string.Equals((x.TransName ?? string.Empty).Trim(), targetTransName, StringComparison.OrdinalIgnoreCase));

                            if (targetTrans == null)
                            {
                                throw new ArgumentException("查無對應派件公司資料");
                            }
                        }
                        else
                        {
                            targetTransNo = string.Empty;
                            targetTransName = string.Empty;
                        }
                    }
                    else if (string.Equals(dataType, "空運", StringComparison.OrdinalIgnoreCase))
                    {
                        var transOptions = BuildUnknownShipmentTransList(JetfDb, dataType);
                        if (!string.IsNullOrWhiteSpace(targetTransNo))
                        {
                            targetTrans = transOptions.FirstOrDefault(x =>
                                string.Equals((x.TransNo ?? string.Empty).Trim(), targetTransNo, StringComparison.OrdinalIgnoreCase));
                        }
                        else if (!string.IsNullOrWhiteSpace(targetTransName))
                        {
                            targetTrans = transOptions.FirstOrDefault(x =>
                                string.Equals((x.TransName ?? string.Empty).Trim(), targetTransName, StringComparison.OrdinalIgnoreCase));
                        }
                        else
                        {
                            targetTransNo = string.Empty;
                            targetTransName = string.Empty;
                        }

                        if (targetTrans == null)
                        {
                            throw new ArgumentException("查無對應派件公司資料");
                        }
                    }
                    else
                    {
                        targetTransNo = string.Empty;
                        targetTransName = string.Empty;
                    }
                }

                if (hasSourceTypeInput && (!targetSourceType.HasValue || !Enum.IsDefined(typeof(ShipmentInboundSourceType), targetSourceType.Value)))
                {
                    throw new ArgumentException("請選擇正確的貨件來源");
                }

                var editTime = DateTime.Now;
                var editUser = GetUserId();

                var targetDataType = string.IsNullOrWhiteSpace(dataType) ? entity.DataType : dataType;
                var newCustomerDisplay = (hasCustCodeInput || isDataTypeChanged)
                    ? GetCustomerDisplayText(targetDataType, targetCustCode, targetCustomer?.Text ?? string.Empty)
                    : GetCustomerDisplayText(targetDataType, entity.CustCode, ResolveCustomerName(DataCenterDb, targetDataType, entity.CustCode));

                var newTransDisplay = (hasTransNoInput || hasTransNameInput || isDataTypeChanged)
                    ? GetTransDisplayText(targetTrans?.TransNo ?? targetTransNo, targetTrans?.TransName ?? targetTransName)
                    : GetTransDisplayText(entity.TransNo, entity.TransName);

                var persistedTransNo = string.Equals(targetDataType, "空運", StringComparison.OrdinalIgnoreCase)
                    ? (targetTrans?.TransNo ?? targetTransNo)
                    : string.Empty;
                var persistedTransName = string.Equals(targetDataType, "空運", StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : (targetTrans?.TransName ?? targetTransName);

                var oldSourceTypeText = entity.SourceType.HasValue
                    ? ((ShipmentInboundSourceType)entity.SourceType.Value).ToDescription()
                    : string.Empty;
                var newSourceTypeText = targetSourceType.HasValue
                    ? ((ShipmentInboundSourceType)targetSourceType.Value).ToDescription()
                    : string.Empty;

                AddShipmentInboundEditHistoryIfChanged(
                    JetfDb,
                    entity.Id,
                    "進口方式",
                    entity.DataType,
                    targetDataType,
                    editTime,
                    editUser,
                    !string.Equals(entity.DataType, targetDataType, StringComparison.OrdinalIgnoreCase));

                var oldCustomerDisplay = GetCustomerDisplayText(entity.DataType, entity.CustCode, ResolveCustomerName(DataCenterDb, entity.DataType, entity.CustCode));
                AddShipmentInboundEditHistoryIfChanged(
                    JetfDb,
                    entity.Id,
                    "客戶",
                    oldCustomerDisplay,
                    newCustomerDisplay,
                    editTime,
                    editUser,
                    !string.Equals(oldCustomerDisplay, newCustomerDisplay, StringComparison.OrdinalIgnoreCase));

                var oldTransDisplay = GetTransDisplayText(entity.TransNo, entity.TransName);
                AddShipmentInboundEditHistoryIfChanged(
                    JetfDb,
                    entity.Id,
                    "派件公司",
                    oldTransDisplay,
                    newTransDisplay,
                    editTime,
                    editUser,
                    !string.Equals(oldTransDisplay, newTransDisplay, StringComparison.OrdinalIgnoreCase));

                AddShipmentInboundEditHistoryIfChanged(
                    JetfDb,
                    entity.Id,
                    "貨件來源",
                    oldSourceTypeText,
                    newSourceTypeText,
                    editTime,
                    editUser,
                    entity.SourceType != targetSourceType);

                entity.DataType = targetDataType;

                if (hasCustCodeInput || isDataTypeChanged)
                {
                    entity.CustCode = targetCustCode;
                }

                if (hasTransNoInput || hasTransNameInput || isDataTypeChanged)
                {
                    entity.TransNo = persistedTransNo;
                    entity.TransName = persistedTransName;
                }

                if (hasSourceTypeInput)
                {
                    entity.SourceType = targetSourceType;
                }

                JetfDb.SaveChanges();
            }
        }

        /// <summary>
        /// 更新金額欄位
        /// </summary>
        public void UpdateAmount(UpdateAmountRequest request)
        {
            throw new InvalidOperationException("到付款、稅金、報關費修改功能已停用");
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

            {
                var entity = JetfDb.ShipmentInbounds.FirstOrDefault(x => x.Id == request.Id);
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

                JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                {
                    ShipmentInboundId = entity.Id,
                    FieldName = "TrackingNo",
                    OldValue = oldTrackingNo,
                    NewValue = shipment.TrackingNo,
                    EditTime = DateTime.Now,
                    EditUser = GetUserId()
                });

                JetfDb.SaveChanges();
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

            {
                return JetfDb.ShipmentInboundEditHistories
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

        /// <summary>
        /// 依進口方式建立不明貨件可選客戶清單。
        /// </summary>
        /// <param name="db">DataCenter 資料庫內容。</param>
        /// <param name="dataType">進口方式。</param>
        /// <returns>客戶下拉選單資料。</returns>
        private List<SelectListModel> BuildUnknownShipmentCustList(Data.DataCenterDbContext db, string dataType)
        {
            var custType = GetCustomerTypeCode(dataType);
            if (string.IsNullOrWhiteSpace(custType))
            {
                return new List<SelectListModel>();
            }

            if (custType == "SEA")
            {
                return db.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == custType)
                    .Select(x => new SelectListModel
                    {
                        Value = x.CustCode,
                        Text = x.CustName
                    })
                    .ToList()
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value) && !string.IsNullOrWhiteSpace(x.Text))
                    .OrderBy(x => x.Text)
                    .ThenBy(x => x.Value)
                    .ToList();
            }

            return db.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == custType && !string.IsNullOrEmpty(x.OldCode))
                .Select(x => new SelectListModel
                {
                    Value = x.OldCode,
                    Text = x.CustName
                })
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) && !string.IsNullOrWhiteSpace(x.Text))
                .OrderBy(x => x.Text)
                .ThenBy(x => x.Value)
                .ToList();
        }

        /// <summary>
        /// 依進口方式建立不明貨件可選派件公司清單。
        /// </summary>
        /// <param name="db">Jetf 資料庫內容。</param>
        /// <param name="dataType">進口方式。</param>
        /// <returns>派件公司下拉選單資料。</returns>
        private List<ShipmentInboundUnknownShipmentTransOptionModel> BuildUnknownShipmentTransList(Data.JetfDbContext db, string dataType)
        {
            var normalizedDataType = NormalizeDataType(dataType);
            if (string.IsNullOrWhiteSpace(normalizedDataType))
            {
                return new List<ShipmentInboundUnknownShipmentTransOptionModel>();
            }

            return db.CustomerMasters
                .AsNoTracking()
                .Where(x => x.TranType == normalizedDataType && !string.IsNullOrEmpty(x.TransName))
                .Select(x => new
                {
                    x.TransNo,
                    x.TransName
                })
                .ToList()
                .Select(x => new ShipmentInboundUnknownShipmentTransOptionModel
                {
                    TransNo = normalizedDataType == "海運"
                        ? string.Empty
                        : (x.TransNo ?? string.Empty).Trim(),
                    TransName = (x.TransName ?? string.Empty).Trim()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.TransName))
                .GroupBy(x => new { x.TransNo, x.TransName })
                .Select(g => new ShipmentInboundUnknownShipmentTransOptionModel
                {
                    OptionKey = string.IsNullOrWhiteSpace(g.Key.TransNo)
                        ? $"NAME::{g.Key.TransName}"
                        : $"NO::{g.Key.TransNo}::{g.Key.TransName}",
                    TransNo = g.Key.TransNo,
                    TransName = g.Key.TransName
                })
                .OrderBy(x => x.TransName)
                .ThenBy(x => x.TransNo)
                .ToList();
        }

        /// <summary>
        /// 將進口方式轉為 DataCenter 客戶類型代碼。
        /// </summary>
        /// <param name="dataType">進口方式。</param>
        /// <returns>SEA 或 AIR 類型代碼。</returns>
        private string GetCustomerTypeCode(string dataType)
        {
            var normalizedDataType = NormalizeDataType(dataType);
            if (normalizedDataType == "海運")
            {
                return "SEA";
            }

            if (normalizedDataType == "空運")
            {
                return "AIR";
            }

            return null;
        }

        /// <summary>
        /// 正規化進口方式文字。
        /// </summary>
        /// <param name="dataType">進口方式文字。</param>
        /// <returns>正規化後的進口方式。</returns>
        private string NormalizeDataType(string dataType)
        {
            var normalizedDataType = (dataType ?? string.Empty).Trim();
            return normalizedDataType == "海運" || normalizedDataType == "空運"
                ? normalizedDataType
                : null;
        }

        /// <summary>
        /// 依進口方式與客戶代碼取得客戶名稱。
        /// </summary>
        /// <param name="db">DataCenter 資料庫內容。</param>
        /// <param name="dataType">進口方式。</param>
        /// <param name="custCode">客戶代碼。</param>
        /// <returns>客戶名稱。</returns>
        private string ResolveCustomerName(Data.DataCenterDbContext db, string dataType, string custCode)
        {
            var normalizedCustCode = (custCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedCustCode))
            {
                return string.Empty;
            }

            var custType = GetCustomerTypeCode(dataType);
            if (custType == "SEA")
            {
                return db.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == custType && x.CustCode == normalizedCustCode)
                    .Select(x => x.CustName)
                    .FirstOrDefault() ?? string.Empty;
            }

            if (custType == "AIR")
            {
                return db.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == custType && x.OldCode == normalizedCustCode)
                    .Select(x => x.CustName)
                    .FirstOrDefault() ?? string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// 組合客戶欄位的編輯紀錄顯示文字。
        /// </summary>
        /// <param name="dataType">進口方式。</param>
        /// <param name="custCode">客戶代碼。</param>
        /// <param name="custName">客戶名稱。</param>
        /// <returns>客戶顯示文字。</returns>
        private string GetCustomerDisplayText(string dataType, string custCode, string custName)
        {
            var normalizedCustCode = (custCode ?? string.Empty).Trim();
            var normalizedCustName = (custName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedCustCode) && string.IsNullOrWhiteSpace(normalizedCustName))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(normalizedCustName))
            {
                return normalizedCustCode;
            }

            if (string.IsNullOrWhiteSpace(normalizedCustCode))
            {
                return normalizedCustName;
            }

            return $"{normalizedCustName} ({normalizedCustCode})";
        }

        /// <summary>
        /// 組合派件公司欄位的編輯紀錄顯示文字。
        /// </summary>
        /// <param name="transNo">派件公司代碼。</param>
        /// <param name="transName">派件公司名稱。</param>
        /// <returns>派件公司顯示文字。</returns>
        private string GetTransDisplayText(string transNo, string transName)
        {
            var normalizedTransNo = (transNo ?? string.Empty).Trim();
            var normalizedTransName = (transName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedTransNo) && string.IsNullOrWhiteSpace(normalizedTransName))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(normalizedTransNo))
            {
                return normalizedTransName;
            }

            if (string.IsNullOrWhiteSpace(normalizedTransName))
            {
                return normalizedTransNo;
            }

            return $"{normalizedTransName} ({normalizedTransNo})";
        }

        /// <summary>
        /// 若欄位值有異動，新增貨件編輯紀錄。
        /// </summary>
        /// <param name="db">Jetf 資料庫內容。</param>
        /// <param name="shipmentInboundId">貨件入庫資料 Id。</param>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="oldValue">舊值。</param>
        /// <param name="newValue">新值。</param>
        /// <param name="editTime">編輯時間。</param>
        /// <param name="editUser">編輯人員。</param>
        /// <param name="hasChanged">是否有異動。</param>
        private void AddShipmentInboundEditHistoryIfChanged(
            Data.JetfDbContext db,
            int shipmentInboundId,
            string fieldName,
            string oldValue,
            string newValue,
            DateTime editTime,
            string editUser,
            bool hasChanged)
        {
            if (!hasChanged)
            {
                return;
            }

            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
            {
                ShipmentInboundId = shipmentInboundId,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                EditTime = editTime,
                EditUser = editUser
            });
        }
    }
}
