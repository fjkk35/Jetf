using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundProcess
{
    public class ShipmentInboundProcessService : _BaseService
    {
        public ShipmentInboundProcessResponse GetData(ShipmentInboundProcessRequest request)
        {
            using (var db = CreateJetfDbContext())
            {
                var query = BuildWhereConditions(db.ShipmentInbounds.AsNoTracking(), request);
                var totalCount = query.Count();
                var data = query
                    .OrderByDescending(x => x.InboundDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => new ShipmentInboundProcessModel
                    {
                        Id = x.Id,
                        DataType = x.DataType,
                        InboundDate = x.InboundDate,
                        TrackingNo = x.TrackingNo,
                        SourceType = x.SourceType.HasValue
                            ? (ShipmentInboundSourceType)x.SourceType.Value
                            : default(ShipmentInboundSourceType),
                        ReturnTrackingNo = x.ReturnTrackingNo,
                        CustCode = x.CustCode,
                        TransNo = x.TransNo,
                        TransName = x.TransName,
                        ReturnReason = x.ReturnReason,
                        ProcessType = x.ProcessType.HasValue
                            ? (ShipmentInboundProcessType?)x.ProcessType.Value
                            : null,
                        Tax = x.Tax,
                        Ccfee = x.Ccfee,
                        Cod = x.Cod
                    })
                    .ToList();

                FillCustomerAndTransNames(data);

                return new ShipmentInboundProcessResponse
                {
                    Data = data,
                    TotalCount = totalCount
                };
            }
        }

        public bool UpdateProcessType(ShipmentInboundProcessUpdateRequest request)
        {
            var userId = GetUserId();

            using (var db = CreateJetfDbContext())
            {
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        var existing = db.ShipmentInbounds.FirstOrDefault(x => x.Id == request.Id);

                        if (existing == null)
                        {
                            throw new Exception("查無此資料");
                        }

                        if (existing.OutboundDate.HasValue)
                        {
                            throw new Exception($"重出日期 {existing.OutboundDate.Value:yyyy/MM/dd}，無法更新資料");
                        }

                        var oldProcessType = existing.ProcessType;
                        var oldTax = existing.Tax;
                        var oldCcfee = existing.Ccfee;
                        var oldCod = existing.Cod;
                        var oldFee = existing.Fee;

                        existing.ProcessType = request.ProcessType;
                        existing.ProcessTransNo = request.ProcessTransNo;
                        existing.ProcessImporter = request.ProcessImporter;
                        existing.ProcessImporterPhone = request.ProcessImporterPhone;
                        existing.ProcessImporterAddr = request.ProcessImporterAddr;
                        existing.StoreCode = request.StoreCode;
                        existing.StoreName = request.StoreName;
                        existing.FreightPayerNo = request.FreightPayerNo;
                        existing.FreightFee = request.FreightFee;
                        existing.Fee = request.Fee;
                        existing.CarNo = request.CarNo;
                        existing.PickupTime = DateTime.TryParse(request.PickupTime, out var pickupTime)
                            ? pickupTime
                            : (DateTime?)null;
                        existing.Remark = request.Remark;
                        existing.Tax = request.Tax;
                        existing.Ccfee = request.Ccfee;
                        existing.Cod = request.Cod;
                        existing.ProcessTime = DateTime.Now;
                        existing.ProcessOpe = userId;

                        if (oldProcessType != request.ProcessType)
                        {
                            var oldValueText = oldProcessType.HasValue
                                ? ((ShipmentInboundProcessType)oldProcessType.Value).ToDescription()
                                : string.Empty;
                            var newValueText = ((ShipmentInboundProcessType)request.ProcessType).ToDescription();

                            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "處理方式",
                                OldValue = oldValueText,
                                NewValue = newValueText,
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        if (oldTax != request.Tax)
                        {
                            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "稅金",
                                OldValue = oldTax?.ToString(),
                                NewValue = request.Tax.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        if (oldCcfee != request.Ccfee)
                        {
                            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "報關費",
                                OldValue = oldCcfee?.ToString(),
                                NewValue = request.Ccfee.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        if (oldCod != request.Cod)
                        {
                            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "到付款",
                                OldValue = oldCod?.ToString(),
                                NewValue = request.Cod.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        if (oldFee != request.Fee)
                        {
                            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "手續費",
                                OldValue = oldFee?.ToString(),
                                NewValue = request.Fee.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        db.SaveChanges();
                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public ShipmentInboundProcessDetailModel GetDetailById(int id)
        {
            using (var db = CreateJetfDbContext())
            {
                return db.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ShipmentInboundProcessDetailModel
                    {
                        Id = x.Id,
                        ProcessType = x.ProcessType.HasValue ? (ShipmentInboundProcessType?)x.ProcessType.Value : null,
                        ProcessTransNo = x.ProcessTransNo.HasValue ? (ShipmentInboundProcessTransNo?)x.ProcessTransNo.Value : null,
                        ProcessImporter = x.ProcessImporter,
                        ProcessImporterPhone = x.ProcessImporterPhone,
                        ProcessImporterAddr = x.ProcessImporterAddr,
                        StoreCode = x.StoreCode,
                        StoreName = x.StoreName,
                        Tax = x.Tax,
                        Ccfee = x.Ccfee,
                        Cod = x.Cod,
                        FreightPayerNo = x.FreightPayerNo.HasValue ? (ShipmentInboundFreightPayerNo?)x.FreightPayerNo.Value : null,
                        FreightFee = x.FreightFee,
                        Fee = x.Fee,
                        CarNo = x.CarNo,
                        PickupTime = x.PickupTime,
                        Remark = x.Remark
                    })
                    .FirstOrDefault();
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

        private IQueryable<Data.ShipmentInboundEntity> BuildWhereConditions(
            IQueryable<Data.ShipmentInboundEntity> query,
            ShipmentInboundProcessRequest request)
        {
            query = query.WhereIf(!string.IsNullOrWhiteSpace(request.DataType), x => x.DataType == request.DataType);
            query = query.WhereIf(
                DateTime.TryParse(request.InboundDateStart, out var startDate),
                x => x.InboundDate >= startDate);
            query = query.WhereIf(
                DateTime.TryParse(request.InboundDateEnd, out var endDate),
                x => x.InboundDate <= endDate);

            if (request.CustCodes?.Any() == true)
            {
                var custCodes = request.CustCodes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                query = query.WhereIf(custCodes.Length > 0, x => custCodes.Contains(x.CustCode));
            }

            query = query.WhereIf(request.SourceType.HasValue, x => x.SourceType == request.SourceType.Value);
            query = query.WhereIf(!string.IsNullOrWhiteSpace(request.TrackingNo), x => x.TrackingNo == request.TrackingNo);

            // 結案條件處理，不包含開箱確認內容物狀況、暫存資料
            query = query.WhereIf(
                request.IsClosed == true,
                x => x.ProcessType.HasValue && x.ProcessType != 7 && x.ProcessType != 8 && x.ProcessType != 9);
            query = query.WhereIf(
                request.IsClosed == false,
                x => !x.ProcessTime.HasValue || x.ProcessType == 7 || x.ProcessType == 8 || x.ProcessType == 9);

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

        private void FillCustomerAndTransNames(List<ShipmentInboundProcessModel> data)
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
        /// 匯出 Excel
        /// </summary>
        public byte[] ExportExcel(ShipmentInboundProcessRequest request)
        {
            // 移除分頁限制，取得所有資料
            request.Page = 1;
            request.PageSize = int.MaxValue;

            var response = GetData(request);
            var workbook = CreateExcelWorkbook(response.Data);

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        private IWorkbook CreateExcelWorkbook(List<ShipmentInboundProcessModel> data)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("貨件退件處理");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");

            string[] headers = new string[]
            {
                "序號",
                "入庫日期",
                "進口方式",
                "客戶",
                "派件公司",
                "單號",
                "貨件來源",
                "退件原因",
                "處理方式"
            };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            for (int i = 0; i < data.Count; i++)
            {
                IRow row = sheet.CreateRow(i + 1);
                var item = data[i];

                NpoiCell.CreateCell(row, 0, (i + 1).ToString(), dataStyle);
                NpoiCell.CreateDateTimeCell(row, 1, item.InboundDate, dateStyle);
                NpoiCell.CreateCell(row, 2, item.DataType ?? "", dataStyle);
                NpoiCell.CreateCell(row, 3, item.CustName ?? "", dataStyle);
                NpoiCell.CreateCell(row, 4, item.TransName ?? "", dataStyle);
                NpoiCell.CreateCell(row, 5, item.TrackingNo ?? "", dataStyle);
                NpoiCell.CreateCell(row, 6, item.SourceTypeName ?? "", dataStyle);
                NpoiCell.CreateCell(row, 7, item.ReturnReason ?? "", dataStyle);
                NpoiCell.CreateCell(row, 8, item.ProcessTypeName ?? "", dataStyle);
            }

            sheet.AutoSizeColumns(headers.Length, scale: 1.2, minWidth: 15);

            return workbook;
        }

        /// <summary>
        /// 批量上傳(貨件回倉處理)
        /// Excel 欄位：單號、處理方式(中文)、備註
        /// 整批驗證：任一筆驗證失敗則整批失敗，不更新任何資料。
        /// </summary>
        public ResponseModel BatchUpload(string filePath)
        {
            var res = new ResponseModel { status = Status.success, msg = "上傳成功" };

            var rows = ReadBatchUploadExcel(filePath);
            if (rows.Count == 0)
            {
                res.status = Status.error;
                res.msg = "Excel 無資料";
                return res;
            }

            var validationErrors = ValidateBatchUploadRows(rows);
            if (validationErrors.Any())
            {
                res.status = Status.error;
                res.msg = $"批量上傳失敗，共{validationErrors.Count}筆錯誤。";
                res.ReturnObject = validationErrors;
                return res;
            }

            var userId = GetUserId();

            var trackingNos = rows.Select(x => x.TrackingNo).Distinct().ToList();
            using (var db = CreateJetfDbContext())
            {
                var existingData = db.ShipmentInbounds
                    .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                    .ToDictionary(x => x.TrackingNo, x => x);

                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var row in rows)
                        {
                            int? newProcessType = row.ProcessTypeText.ToEnumValueByDescription<ShipmentInboundProcessType>();

                            if (existingData.ContainsKey(row.TrackingNo))
                            {
                                var existing = existingData[row.TrackingNo];
                                var oldProcessType = existing.ProcessType;
                                var shipmentInboundId = existing.Id;

                                existing.ProcessType = (byte)newProcessType.Value;
                                existing.Remark = row.Remark;
                                existing.ProcessTime = DateTime.Now;
                                existing.ProcessOpe = userId;

                                if (oldProcessType != newProcessType.Value)
                                {
                                    var oldValueText = oldProcessType.HasValue
                                        ? ((ShipmentInboundProcessType)oldProcessType.Value).ToDescription()
                                        : string.Empty;
                                    var newValueText = ((ShipmentInboundProcessType)newProcessType.Value).ToDescription();

                                    db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                                    {
                                        ShipmentInboundId = shipmentInboundId,
                                        FieldName = "處理方式",
                                        OldValue = oldValueText,
                                        NewValue = newValueText,
                                        EditTime = DateTime.Now,
                                        EditUser = userId
                                    });
                                }
                            }
                        }

                        db.SaveChanges();
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            res.msg = $"成功{rows.Count}筆";
            return res;
        }

        private List<ShipmentInboundProcessBatchUploadErrorModel> ValidateBatchUploadRows(List<ShipmentInboundProcessBatchUploadRowModel> rows)
        {
            var errors = new List<ShipmentInboundProcessBatchUploadErrorModel>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        ProcessTypeText = row.ProcessTypeText,
                        FieldName = "單號",
                        Reason = "不可為空"
                    });
                }

                if (string.IsNullOrWhiteSpace(row.ProcessTypeText))
                {
                    errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        ProcessTypeText = row.ProcessTypeText,
                        FieldName = "處理方式",
                        Reason = "不可為空"
                    });
                }
                else
                {
                    int? processType = row.ProcessTypeText.ToEnumValueByDescription<ShipmentInboundProcessType>();
                    if (!processType.HasValue || !IsAllowedBatchProcessType(processType.Value))
                    {
                        errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                        {
                            RowNo = row.RowNo,
                            TrackingNo = row.TrackingNo,
                            ProcessTypeText = row.ProcessTypeText,
                            FieldName = "處理方式",
                            Reason = "不允許的處理方式"
                        });
                    }
                }
            }

            var trackingNos = rows
                .Select(x => (x.TrackingNo ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (trackingNos.Any())
            {
                List<string> existing;
                using (var db = CreateJetfDbContext())
                {
                    existing = db.ShipmentInbounds
                        .AsNoTracking()
                        .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                        .Select(x => x.TrackingNo)
                        .ToList();
                }
                var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.TrackingNo))
                    {
                        continue;
                    }

                    if (!existingSet.Contains(row.TrackingNo.Trim()))
                    {
                        errors.Add(new ShipmentInboundProcessBatchUploadErrorModel
                        {
                            RowNo = row.RowNo,
                            TrackingNo = row.TrackingNo,
                            ProcessTypeText = row.ProcessTypeText,
                            FieldName = "單號",
                            Reason = "查無資料或已出庫"
                        });
                    }
                }
            }

            return errors;
        }

        private bool IsAllowedBatchProcessType(int processType)
        {
            // 僅允許：TransferFromOriginal(2)、ReturnToSite(3)、Destroy(5)、AddToReturnShipment(6)、InspectContents(7)、ConfirmOuterLabel(8)、TempData(9)
            var allowedTypes = new[] {
                (int)ShipmentInboundProcessType.TransferFromOriginal,
                (int)ShipmentInboundProcessType.ReturnToSite,
                (int)ShipmentInboundProcessType.Destroy,
                (int)ShipmentInboundProcessType.AddToReturnShipment,
                (int)ShipmentInboundProcessType.InspectContents,
                (int)ShipmentInboundProcessType.ConfirmOuterLabel,
                (int)ShipmentInboundProcessType.TempData
            };

            return allowedTypes.Contains(processType);
        }

        private List<ShipmentInboundProcessBatchUploadRowModel> ReadBatchUploadExcel(string filePath)
        {
            var result = new List<ShipmentInboundProcessBatchUploadRowModel>();

            IWorkbook workBook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            if (sheet == null) return result;

            bool read = false;
            int trackingNoIndex = -1;
            int processTypeIndex = -1;
            int remarkIndex = -1;

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                if (!read)
                {
                    for (int c = 0; c < row.LastCellNum; c++)
                    {
                        var header = row.GetCellData(c);
                        if (header == "單號") trackingNoIndex = c;
                        if (header == "處理方式") processTypeIndex = c;
                        if (header == "備註") remarkIndex = c;
                    }

                    if (trackingNoIndex >= 0 && processTypeIndex >= 0 && remarkIndex >= 0)
                    {
                        read = true;
                    }
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex);
                var processTypeText = row.GetCellData(processTypeIndex);
                var remark = row.GetCellData(remarkIndex);

                if (string.IsNullOrWhiteSpace(trackingNo) && string.IsNullOrWhiteSpace(processTypeText) && string.IsNullOrWhiteSpace(remark))
                {
                    continue;
                }

                result.Add(new ShipmentInboundProcessBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    ProcessTypeText = processTypeText,
                    Remark = remark
                });
            }

            return result;
        }

        /// <summary>
        /// 更新退件原因
        /// </summary>
        public void UpdateReturnReason(int id, string returnReason)
        {
            using (var db = CreateJetfDbContext())
            {
                var entity = db.ShipmentInbounds.FirstOrDefault(x => x.Id == id);
                if (entity == null)
                {
                    throw new Exception("查無此資料");
                }

                if (entity.OutboundDate.HasValue)
                {
                    throw new Exception($"出庫日期 {entity.OutboundDate.Value:yyyy/MM/dd}，無法更新資料");
                }

                entity.ReturnReason = returnReason;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// 批量上傳退件原因
        /// Excel 欄位：單號、退件原因
        /// 驗證規則：如果有單號找不到，整批上傳失敗，並回傳失敗原因
        /// </summary>
        public ResponseModel BatchUploadReturnReason(string filePath)
        {
            var res = new ResponseModel { status = Status.success, msg = "上傳成功" };

            var rows = ReadReturnReasonBatchUploadExcel(filePath);
            if (rows.Count == 0)
            {
                res.status = Status.error;
                res.msg = "Excel 無資料";
                return res;
            }

            var validationErrors = ValidateReturnReasonBatchUploadRows(rows);
            if (validationErrors.Any())
            {
                res.status = Status.error;
                res.msg = $"批量上傳失敗，共 {validationErrors.Count} 筆錯誤。";
                res.ReturnObject = validationErrors;
                return res;
            }

            using (var db = CreateJetfDbContext())
            {
                var trackingNos = rows.Select(x => x.TrackingNo).Distinct().ToList();
                var entities = db.ShipmentInbounds
                    .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                    .ToDictionary(x => x.TrackingNo, x => x);

                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var row in rows)
                        {
                            if (entities.ContainsKey(row.TrackingNo))
                            {
                                entities[row.TrackingNo].ReturnReason = row.ReturnReason;
                            }
                        }

                        db.SaveChanges();
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            res.msg = $"成功更新 {rows.Count} 筆";
            return res;
        }

        private List<ReturnReasonBatchUploadErrorModel> ValidateReturnReasonBatchUploadRows(List<ReturnReasonBatchUploadRowModel> rows)
        {
            var errors = new List<ReturnReasonBatchUploadErrorModel>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    errors.Add(new ReturnReasonBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        FieldName = "單號",
                        Reason = "單號不可為空"
                    });
                }
            }

            var trackingNos = rows
                .Select(x => (x.TrackingNo ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (trackingNos.Any())
            {
                List<string> existing;
                using (var db = CreateJetfDbContext())
                {
                    existing = db.ShipmentInbounds
                        .AsNoTracking()
                        .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                        .Select(x => x.TrackingNo)
                        .ToList();
                }
                var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.TrackingNo))
                    {
                        continue;
                    }

                    if (!existingSet.Contains(row.TrackingNo.Trim()))
                    {
                        errors.Add(new ReturnReasonBatchUploadErrorModel
                        {
                            RowNo = row.RowNo,
                            TrackingNo = row.TrackingNo,
                            FieldName = "單號",
                            Reason = "查無資料或已出庫"
                        });
                    }
                }
            }

            return errors;
        }

        private List<ReturnReasonBatchUploadRowModel> ReadReturnReasonBatchUploadExcel(string filePath)
        {
            var result = new List<ReturnReasonBatchUploadRowModel>();

            IWorkbook workBook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            if (sheet == null) return result;

            bool read = false;
            int trackingNoIndex = -1;
            int returnReasonIndex = -1;

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                if (!read)
                {
                    for (int c = 0; c < row.LastCellNum; c++)
                    {
                        var header = row.GetCellData(c);
                        if (header == "單號") trackingNoIndex = c;
                        if (header == "退件原因") returnReasonIndex = c;
                    }

                    if (trackingNoIndex >= 0 && returnReasonIndex >= 0)
                    {
                        read = true;
                    }
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex);
                var returnReason = row.GetCellData(returnReasonIndex);

                if (string.IsNullOrWhiteSpace(trackingNo) && string.IsNullOrWhiteSpace(returnReason))
                {
                    continue;
                }

                result.Add(new ReturnReasonBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    ReturnReason = returnReason
                });
            }

            return result;
        }
    }
}

