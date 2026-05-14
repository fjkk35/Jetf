using Service.Data;
using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Service.Services.ShipmentInboundProcessStage.Domain;

namespace Service.Services.ShipmentInboundProcessStage
{
    /// <summary>
    /// 貨件回倉預先登記處理服務。
    /// </summary>
    public class ShipmentInboundProcessStageService : _BaseService
    {
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
            using (var db = CreateJetfDbContext())
            {
                IQueryable<ShipmentInboundProcessStageEntity> query = db.ShipmentInboundProcessStages.AsNoTracking();

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
            using (var db = CreateJetfDbContext())
            {
                return db.ShipmentInboundProcessStages
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

            using (var db = CreateJetfDbContext())
            {
                var duplicateExists = db.ShipmentInboundProcessStages.Any(x =>
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

                    db.ShipmentInboundProcessStages.Add(entity);
                }
                else
                {
                    entity = db.ShipmentInboundProcessStages.FirstOrDefault(x => x.Id == request.Id.Value);
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

                db.SaveChanges();
                return BuildStageModel(entity);
            }
        }

        /// <summary>
        /// 依 Id 取得列表單筆資料。
        /// </summary>
        public ShipmentInboundProcessStageModel GetRowById(int id)
        {
            using (var db = CreateJetfDbContext())
            {
                var entity = db.ShipmentInboundProcessStages.AsNoTracking().FirstOrDefault(x => x.Id == id);
                if (entity == null)
                {
                    throw new Exception("查無此資料");
                }

                return BuildStageModel(entity);
            }
        }

        private ShipmentInboundProcessType ValidateAndNormalizeRequest(ShipmentInboundProcessStageSaveRequest request)
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

            if (!Enum.IsDefined(typeof(ShipmentInboundProcessType), request.ProcessType))
            {
                throw new Exception("請選擇處理方式");
            }

            var processType = (ShipmentInboundProcessType)request.ProcessType;
            NormalizeFieldsByProcessType(request, processType);
            ValidateRequiredFields(request, processType);

            return processType;
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
            ShipmentInboundProcessType processType,
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
