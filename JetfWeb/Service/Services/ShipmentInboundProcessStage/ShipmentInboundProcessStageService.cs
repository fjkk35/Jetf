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
                    query = query.Where(x => x.TrackingNo == trackingNo);
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
                        ProcessType = x.ProcessType
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
            entity.FreightPayerNo = request.FreightPayerNo;
            entity.FreightFee = request.FreightFee;
            entity.Fee = CalculateFee(request.FreightFee);
            entity.CarNo = request.CarNo;
            entity.PickupTime = DateTime.TryParse(request.PickupTime, out var pickupTime)
                ? pickupTime
                : (DateTime?)null;
            entity.Remark = request.Remark;
            entity.ProcessTime = DateTime.Now;
            entity.ProcessOpe = userId;

            if (isNew)
            {
                entity.Importer = request.ProcessImporter;
                entity.ImporterPhone = request.ProcessImporterPhone;
                entity.ImporterAddr = request.ProcessImporterAddr;
                return;
            }

            if (string.IsNullOrWhiteSpace(entity.Importer) && !string.IsNullOrWhiteSpace(request.ProcessImporter))
            {
                entity.Importer = request.ProcessImporter;
            }

            if (string.IsNullOrWhiteSpace(entity.ImporterPhone) && !string.IsNullOrWhiteSpace(request.ProcessImporterPhone))
            {
                entity.ImporterPhone = request.ProcessImporterPhone;
            }

            if (string.IsNullOrWhiteSpace(entity.ImporterAddr) && !string.IsNullOrWhiteSpace(request.ProcessImporterAddr))
            {
                entity.ImporterAddr = request.ProcessImporterAddr;
            }
        }

        private int CalculateFee(int? freightFee)
        {
            return (freightFee ?? 0) > 0 ? 30 : 0;
        }

        private ShipmentInboundProcessStageModel BuildStageModel(ShipmentInboundProcessStageEntity entity)
        {
            return new ShipmentInboundProcessStageModel
            {
                Id = entity.Id,
                TrackingNo = entity.TrackingNo,
                ReturnReason = entity.ReturnReason,
                ProcessType = entity.ProcessType
            };
        }

    }
}
