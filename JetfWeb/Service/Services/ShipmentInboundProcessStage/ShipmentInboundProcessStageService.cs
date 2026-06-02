using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using Service.Services.ShipmentInboundProcessStage.Domain;

namespace Service.Services.ShipmentInboundProcessStage
{
    /// <summary>
    /// 貨件回倉預先登記處理服務。
    /// </summary>
    public class ShipmentInboundProcessStageService : _BaseService
    {
        public ShipmentInboundProcessStageService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        private static readonly ShipmentInboundProcessType[] RemarkOnlyProcessTypes =
        {
            ShipmentInboundProcessType.TransferFromOriginal,
            ShipmentInboundProcessType.ReturnToSite,
            ShipmentInboundProcessType.Destroy,
            ShipmentInboundProcessType.AddToReturnShipment,
            ShipmentInboundProcessType.InspectContents,
            ShipmentInboundProcessType.ConfirmOuterLabel,
            ShipmentInboundProcessType.TransferBySystem
        };

        /// <summary>
        /// 依條件取得預先登記處理資料。
        /// </summary>
        public ShipmentInboundProcessStageResponse GetData(ShipmentInboundProcessStageRequest request)
        {
            {
                IQueryable<ShipmentInboundProcessStageEntity> query = JetfDb.ShipmentInboundProcessStages.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(request.TrackingNo))
                {
                    var trackingNo = request.TrackingNo.Trim();
                    query = query.Where(x => x.TrackingNo.Contains(trackingNo));
                }

                if (!string.IsNullOrWhiteSpace(request.CreatedOpe))
                {
                    var createdOpe = request.CreatedOpe.Trim();
                    query = query.Where(x => x.CreatedOpe.Contains(createdOpe));
                }

                var createdTimeStart = ParseSearchDate(request.CreatedTimeStart, "輸入日期(起)");
                if (createdTimeStart.HasValue)
                {
                    var start = createdTimeStart.Value.Date;
                    query = query.Where(x => x.CreatedTime >= start);
                }

                var createdTimeEnd = ParseSearchDate(request.CreatedTimeEnd, "輸入日期(迄)");
                if (createdTimeEnd.HasValue)
                {
                    var endExclusive = createdTimeEnd.Value.Date.AddDays(1);
                    query = query.Where(x => x.CreatedTime < endExclusive);
                }

                var matchTimieStart = ParseSearchDate(request.MatchTimieStart, "匹配日期(起)");
                if (matchTimieStart.HasValue)
                {
                    var start = matchTimieStart.Value.Date;
                    query = query.Where(x => x.MatchTimie.HasValue && x.MatchTimie.Value >= start);
                }

                var matchTimieEnd = ParseSearchDate(request.MatchTimieEnd, "匹配日期(迄)");
                if (matchTimieEnd.HasValue)
                {
                    var endExclusive = matchTimieEnd.Value.Date.AddDays(1);
                    query = query.Where(x => x.MatchTimie.HasValue && x.MatchTimie.Value < endExclusive);
                }

                if (request.IsMatched.HasValue)
                {
                    query = request.IsMatched.Value
                        ? query.Where(x => x.MatchTimie.HasValue)
                        : query.Where(x => !x.MatchTimie.HasValue);
                }

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
                var totalCount = query.Count();

                var data = query
                    .OrderByDescending(x => x.CreatedTime)
                    .ThenByDescending(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new ShipmentInboundProcessStageModel
                    {
                        Id = x.Id,
                        TrackingNo = x.TrackingNo,
                        ReturnReason = x.ReturnReason,
                        Remark = x.Remark,
                        Cod = x.Cod,
                        FreightFee = x.FreightFee,
                        Tax = x.Tax,
                        CcFee = x.CcFee,
                        Fee = x.Fee,
                        CreatedTime = x.CreatedTime,
                        CreatedOpe = x.CreatedOpe,
                        MatchTimie = x.MatchTimie,
                        ProcessType = x.ProcessType,
                        ProcessTransNo = x.ProcessTransNo.HasValue
                            ? (ShipmentInboundProcessTransNo?)x.ProcessTransNo.Value
                            : null
                    })
                    .ToList();

                return new ShipmentInboundProcessStageResponse
                {
                    Data = data,
                    TotalCount = totalCount
                };
            }
        }

        /// <summary>
        /// 依 Id 取得單筆詳細資料。
        /// </summary>
        public ShipmentInboundProcessStageDetailModel GetDetailById(int id)
        {
            {
                return JetfDb.ShipmentInboundProcessStages
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ShipmentInboundProcessStageDetailModel
                    {
                        Id = x.Id,
                        TrackingNo = x.TrackingNo,
                        ReturnReason = x.ReturnReason,
                        ProcessType = x.ProcessType,
                        ProcessTransNo = x.ProcessTransNo.HasValue
                            ? (ShipmentInboundProcessTransNo?)x.ProcessTransNo.Value
                            : null,
                        ProcessImporter = x.ProcessImporter,
                        ProcessImporterPhone = x.ProcessImporterPhone,
                        ProcessImporterAddr = x.ProcessImporterAddr,
                        StoreCode = x.StoreCode,
                        StoreName = x.StoreName,
                        Tax = x.Tax,
                        CcFee = x.CcFee,
                        Cod = x.Cod,
                        FreightPayerNo = x.FreightPayerNo.HasValue
                            ? (ShipmentInboundFreightPayerNo?)x.FreightPayerNo.Value
                            : null,
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
        /// 新增或更新預先登記處理資料。
        /// </summary>
        public ShipmentInboundProcessStageModel SaveProcess(ShipmentInboundProcessStageSaveRequest request)
        {
            var userId = GetUserId();
            var processType = ValidateAndNormalizeRequest(request);

            {
                var duplicateExists = JetfDb.ShipmentInboundProcessStages.Any(x =>
                    x.TrackingNo == request.TrackingNo &&
                    !x.MatchTimie.HasValue &&
                    (!request.Id.HasValue || x.Id != request.Id.Value));

                if (duplicateExists)
                {
                    throw new Exception("此單號已存在預先登記資料");
                }

                ShipmentInboundProcessStageEntity entity;
                var isNew = !request.Id.HasValue;

                if (isNew)
                {
                    entity = new ShipmentInboundProcessStageEntity
                    {
                        CreatedOpe = userId,
                        CreatedTime = DateTime.Now
                    };

                    JetfDb.ShipmentInboundProcessStages.Add(entity);
                }
                else
                {
                    entity = JetfDb.ShipmentInboundProcessStages.FirstOrDefault(x => x.Id == request.Id.Value);
                    if (entity == null)
                    {
                        throw new Exception("查無此資料");
                    }

                    if (entity.MatchTimie.HasValue)
                    {
                        throw new Exception("此資料已有匹配日期，不能再進行修改");
                    }
                }

                ApplyRequest(entity, request, processType, userId, isNew);

                JetfDb.SaveChanges();
                return BuildStageModel(entity);
            }
        }

        /// <summary>
        /// 批量上傳預先登記退件原因。
        /// </summary>
        /// <param name="filePath">上傳檔案路徑。</param>
        /// <returns>批次處理結果。</returns>
        public ResponseModel BatchUploadReturnReason(string filePath)
        {
            var response = new ResponseModel();
            var rows = ReadBatchUploadReturnReasonExcel(filePath);

            if (rows.Count == 0)
            {
                response.status = Status.error;
                response.msg = "Excel 無資料";
                return response;
            }

            var errors = ValidateBatchUploadReturnReasonRows(rows)
                .OrderBy(x => x.RowNo)
                .ThenBy(x => x.FieldName)
                .ToList();
            var failureCount = errors
                .Select(x => x.RowNo)
                .Distinct()
                .Count();

            if (failureCount > 0)
            {
                response.status = Status.error;
                response.msg = $"驗證失敗，未寫入任何資料。成功 0 筆，失敗 {failureCount} 筆。";
                response.ReturnObject = new
                {
                    SuccessCount = 0,
                    FailureCount = failureCount,
                    Errors = errors
                };

                return response;
            }

            var userId = GetUserId();
            var now = DateTime.Now;
            var entities = rows.Select(row => new ShipmentInboundProcessStageEntity
            {
                TrackingNo = (row.TrackingNo ?? string.Empty).Trim(),
                ReturnReason = row.ReturnReason,
                Remark = row.Remark,
                Tax = 0,
                CcFee = 0,
                Cod = 0,
                FreightFee = 0,
                Fee = 0,
                ProcessTime = now,
                ProcessOpe = userId,
                CreatedOpe = userId,
                CreatedTime = now,
                IsMatch = false,
                MatchTimie = null
            }).ToList();

            JetfDb.ShipmentInboundProcessStages.AddRange(entities);
            JetfDb.SaveChanges();

            response.msg = $"成功 {entities.Count} 筆，失敗 0 筆。";
            response.ReturnObject = new
            {
                SuccessCount = entities.Count,
                FailureCount = 0,
                Errors = new List<ShipmentInboundProcessStageBatchUploadErrorModel>()
            };

            return response;
        }

        /// <summary>
        /// 依 Id 取得列表單筆資料。
        /// </summary>
        public ShipmentInboundProcessStageModel GetRowById(int id)
        {
            {
                var entity = JetfDb.ShipmentInboundProcessStages.AsNoTracking().FirstOrDefault(x => x.Id == id);
                if (entity == null)
                {
                    throw new Exception("查無此資料");
                }

                return BuildStageModel(entity);
            }
        }

        private List<ShipmentInboundProcessStageBatchUploadErrorModel> ValidateBatchUploadReturnReasonRows(
            List<ShipmentInboundProcessStageBatchUploadRowModel> rows)
        {
            var errors = new List<ShipmentInboundProcessStageBatchUploadErrorModel>();

            foreach (var row in rows)
            {
                row.TrackingNo = row.TrackingNo?.Trim();
                row.ReturnReason = row.ReturnReason?.Trim();
                row.Remark = row.Remark?.Trim();

                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    errors.Add(new ShipmentInboundProcessStageBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        FieldName = "單號",
                        Reason = "單號不可為空"
                    });
                }
            }

            var duplicateTrackingNos = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var row in duplicateTrackingNos)
            {
                errors.Add(new ShipmentInboundProcessStageBatchUploadErrorModel
                {
                    RowNo = row.RowNo,
                    TrackingNo = row.TrackingNo,
                    FieldName = "單號",
                    Reason = "Excel 內單號重複"
                });
            }

            var trackingNos = rows
                .Select(x => x.TrackingNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (trackingNos.Any())
            {
                var existingTrackingNos = JetfDb.ShipmentInboundProcessStages
                    .AsNoTracking()
                    .Where(x => trackingNos.Contains(x.TrackingNo) && !x.MatchTimie.HasValue)
                    .Select(x => x.TrackingNo)
                    .Distinct()
                    .ToList();
                var existingSet = new HashSet<string>(existingTrackingNos, StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.TrackingNo))
                    {
                        continue;
                    }

                    if (!existingSet.Contains(row.TrackingNo))
                    {
                        continue;
                    }

                    errors.Add(new ShipmentInboundProcessStageBatchUploadErrorModel
                    {
                        RowNo = row.RowNo,
                        TrackingNo = row.TrackingNo,
                        FieldName = "單號",
                        Reason = "此單號已存在且尚未匹配，不能重複新增"
                    });
                }
            }

            return errors;
        }

        private List<ShipmentInboundProcessStageBatchUploadRowModel> ReadBatchUploadReturnReasonExcel(string filePath)
        {
            var result = new List<ShipmentInboundProcessStageBatchUploadRowModel>();

            IWorkbook workBook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            if (sheet == null)
            {
                return result;
            }

            var headerFound = false;
            var trackingNoIndex = -1;
            var returnReasonIndex = -1;
            var remarkIndex = -1;

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                {
                    continue;
                }

                if (!headerFound)
                {
                    for (int columnIndex = 0; columnIndex < row.LastCellNum; columnIndex++)
                    {
                        var header = row.GetCellData(columnIndex);
                        if (header == "單號")
                        {
                            trackingNoIndex = columnIndex;
                        }

                        if (header == "退件原因")
                        {
                            returnReasonIndex = columnIndex;
                        }

                        if (header == "備註")
                        {
                            remarkIndex = columnIndex;
                        }
                    }

                    headerFound = trackingNoIndex >= 0 && returnReasonIndex >= 0 && remarkIndex >= 0;
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex)?.Trim();
                var returnReason = row.GetCellData(returnReasonIndex)?.Trim();
                var remark = row.GetCellData(remarkIndex)?.Trim();

                if (string.IsNullOrWhiteSpace(trackingNo)
                    && string.IsNullOrWhiteSpace(returnReason)
                    && string.IsNullOrWhiteSpace(remark))
                {
                    continue;
                }

                result.Add(new ShipmentInboundProcessStageBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    ReturnReason = returnReason,
                    Remark = remark
                });
            }

            if (!headerFound)
            {
                throw new Exception("Excel 欄位需包含：單號、退件原因、備註");
            }

            return result;
        }

        private ShipmentInboundProcessType? ValidateAndNormalizeRequest(ShipmentInboundProcessStageSaveRequest request)
        {
            request.TrackingNo = request.TrackingNo?.Trim();
            request.ReturnReason = request.ReturnReason?.Trim();
            request.ProcessImporter = request.ProcessImporter?.Trim();
            request.ProcessImporterPhone = request.ProcessImporterPhone?.Trim();
            request.ProcessImporterAddr = request.ProcessImporterAddr?.Trim();
            request.StoreCode = request.StoreCode?.Trim();
            request.StoreName = request.StoreName?.Trim();
            request.CarNo = request.CarNo?.Trim();
            request.Remark = request.Remark?.Trim();

            if (string.IsNullOrWhiteSpace(request.TrackingNo))
            {
                throw new Exception("單號為必填欄位");
            }

            if (!request.ProcessType.HasValue)
            {
                NormalizeFieldsWithoutProcessType(request);
                return null;
            }

            if (!Enum.IsDefined(typeof(ShipmentInboundProcessType), request.ProcessType.Value))
            {
                throw new Exception("處理方式不正確");
            }

            var processType = (ShipmentInboundProcessType)request.ProcessType.Value;
            NormalizeFieldsByProcessType(request, processType);
            ValidateRequiredFields(request, processType);

            return processType;
        }

        private void NormalizeFieldsWithoutProcessType(ShipmentInboundProcessStageSaveRequest request)
        {
            request.ProcessTransNo = null;
            request.ProcessImporter = null;
            request.ProcessImporterPhone = null;
            request.ProcessImporterAddr = null;
            request.StoreCode = null;
            request.StoreName = null;
            request.Tax = 0;
            request.CcFee = 0;
            request.Cod = 0;
            request.FreightPayerNo = null;
            request.FreightFee = 0;
            request.Fee = 0;
            request.CarNo = null;
            request.PickupTime = null;
        }

        private void NormalizeFieldsByProcessType(
            ShipmentInboundProcessStageSaveRequest request,
            ShipmentInboundProcessType processType)
        {
            if (processType != ShipmentInboundProcessType.NewTrackingNo &&
                processType != ShipmentInboundProcessType.TempData)
            {
                request.ProcessTransNo = null;
                request.FreightPayerNo = null;
                request.FreightFee = 0;
                request.StoreCode = null;
                request.StoreName = null;
            }

            if (processType != ShipmentInboundProcessType.NewTrackingNo &&
                processType != ShipmentInboundProcessType.TempData &&
                processType != ShipmentInboundProcessType.SelfPickup)
            {
                request.Tax = 0;
                request.CcFee = 0;
                request.Cod = 0;
            }

            if (processType == ShipmentInboundProcessType.SelfPickup)
            {
                request.CcFee = 0;
                request.Cod = 0;
            }

            if (processType != ShipmentInboundProcessType.SelfPickup)
            {
                request.CarNo = null;
                request.PickupTime = null;
            }

            if (RemarkOnlyProcessTypes.Contains(processType))
            {
                request.ProcessImporter = null;
                request.ProcessImporterPhone = null;
                request.ProcessImporterAddr = null;
            }

            if (processType == ShipmentInboundProcessType.NewTrackingNo ||
                processType == ShipmentInboundProcessType.TempData)
            {
                if (request.ProcessTransNo == (byte)ShipmentInboundProcessTransNo.SevenEleven)
                {
                    request.ProcessImporterAddr = null;
                }
                else
                {
                    request.StoreCode = null;
                    request.StoreName = null;
                }

                request.FreightFee = request.FreightPayerNo == (byte)ShipmentInboundFreightPayerNo.Consignee
                    ? 120
                    : 0;
                return;
            }

            request.FreightFee = 0;
        }

        private void ValidateRequiredFields(
            ShipmentInboundProcessStageSaveRequest request,
            ShipmentInboundProcessType processType)
        {
            if (processType != ShipmentInboundProcessType.NewTrackingNo)
            {
                return;
            }

            if (!request.ProcessTransNo.HasValue)
            {
                throw new Exception("請選擇重出派件公司");
            }

            if (string.IsNullOrWhiteSpace(request.ProcessImporter))
            {
                throw new Exception("收件人為必填欄位");
            }

            if (string.IsNullOrWhiteSpace(request.ProcessImporterPhone))
            {
                throw new Exception("電話為必填欄位");
            }

            if (!request.FreightPayerNo.HasValue)
            {
                throw new Exception("重出運費支付方為必填欄位");
            }

            var processTransNo = (ShipmentInboundProcessTransNo)request.ProcessTransNo.Value;
            if (processTransNo == ShipmentInboundProcessTransNo.SevenEleven)
            {
                if (string.IsNullOrWhiteSpace(request.StoreCode))
                {
                    throw new Exception("門市店號為必填欄位");
                }

                if (string.IsNullOrWhiteSpace(request.StoreName))
                {
                    throw new Exception("門市名稱為必填欄位");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(request.ProcessImporterAddr))
            {
                throw new Exception("宅配地址為必填欄位");
            }
        }

        private void ApplyRequest(
            ShipmentInboundProcessStageEntity entity,
            ShipmentInboundProcessStageSaveRequest request,
            ShipmentInboundProcessType? processType,
            string userId,
            bool isNew)
        {
            entity.TrackingNo = request.TrackingNo;
            entity.ReturnReason = request.ReturnReason;
            entity.ProcessType = processType;
            entity.ProcessTransNo = request.ProcessTransNo;
            entity.ProcessImporter = request.ProcessImporter;
            entity.ProcessImporterPhone = request.ProcessImporterPhone;
            entity.ProcessImporterAddr = request.ProcessImporterAddr;
            entity.StoreCode = request.StoreCode;
            entity.StoreName = request.StoreName;
            entity.Tax = request.Tax;
            entity.CcFee = request.CcFee;
            entity.Cod = request.Cod;
            entity.FreightPayerNo = request.FreightPayerNo;
            entity.FreightFee = request.FreightFee;
            entity.Fee = CalculateFee(request.FreightFee, request.Tax, request.CcFee);
            entity.CarNo = request.CarNo;
            entity.PickupTime = DateTime.TryParse(request.PickupTime, out var pickupTime)
                ? pickupTime
                : (DateTime?)null;
            entity.Remark = request.Remark;
            entity.ProcessTime = DateTime.Now;
            entity.ProcessOpe = userId;
            entity.UpdatedOpe = isNew ? null : userId;
            entity.UpdatedTime = isNew ? (DateTime?)null : DateTime.Now;

            if (isNew)
            {
                entity.IsMatch = false;
                entity.MatchTimie = null;
            }
        }

        private DateTime? ParseSearchDate(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!DateTime.TryParse(value, out var date))
            {
                throw new Exception($"{fieldName}格式錯誤，請使用 yyyy-MM-dd");
            }

            return date.Date;
        }

        private int CalculateFee(int? freightFee, int? tax, int? ccFee)
        {
            return (freightFee ?? 0) > 0
                || (tax ?? 0) > 0
                || (ccFee ?? 0) > 0
                ? 30
                : 0;
        }

        private ShipmentInboundProcessStageModel BuildStageModel(ShipmentInboundProcessStageEntity entity)
        {
            return new ShipmentInboundProcessStageModel
            {
                Id = entity.Id,
                TrackingNo = entity.TrackingNo,
                ReturnReason = entity.ReturnReason,
                Remark = entity.Remark,
                Cod = entity.Cod,
                FreightFee = entity.FreightFee,
                Tax = entity.Tax,
                CcFee = entity.CcFee,
                Fee = entity.Fee,
                CreatedTime = entity.CreatedTime,
                CreatedOpe = entity.CreatedOpe,
                MatchTimie = entity.MatchTimie,
                ProcessType = entity.ProcessType
            };
        }

    }
}
