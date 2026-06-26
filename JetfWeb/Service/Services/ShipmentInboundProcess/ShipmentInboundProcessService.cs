using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundCommon;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundProcess
{
    /// <summary>
    /// 貨件回倉處理服務，負責查詢、鎖定、更新與批次處理作業。
    /// </summary>
    public class ShipmentInboundProcessService : _BaseService
    {
        public ShipmentInboundProcessService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 處理鎖定逾時分鐘數。
        /// 超過此時間的鎖定視為失效，前端不顯示且允許下一位人員接手。
        /// </summary>
        private const int ProcessEditLockTimeoutMinutes = 10;

        /// <summary>
        /// 依查詢條件取得貨件回倉處理列表資料。
        /// </summary>
        /// <param name="request">查詢條件與分頁資訊。</param>
        /// <returns>查詢結果與總筆數。</returns>
        public ShipmentInboundProcessResponse GetData(ShipmentInboundProcessRequest request)
        {
            {
                var query = BuildWhereConditions(JetfDb.ShipmentInbounds.AsNoTracking(), request);
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
                        ProcessStartTime = x.ProcessStartTime,
                        ProcessStartOpe = x.ProcessStartOpe,
                        Tax = x.Tax,
                        Ccfee = x.Ccfee,
                        Cod = x.Cod,
                        Remark = x.Remark
                    })
                    .ToList();

                FillFeePolicyFlags(data);
                FillTrackingNoCounts(data, query);
                NormalizeExpiredProcessEditDisplay(data);
                FillCustomerAndTransNames(data);
                FillLatestExceptionReasons(data);

                return new ShipmentInboundProcessResponse
                {
                    Data = data,
                    TotalCount = totalCount
                };
            }
        }

        /// <summary>
        /// 更新貨件回倉處理方式與相關欄位。
        /// </summary>
        /// <param name="request">更新內容。</param>
        /// <returns>更新是否成功。</returns>
        public bool UpdateProcessType(ShipmentInboundProcessUpdateRequest request)
        {
            var userId = GetUserId();

            {
                using (var tx = JetfDb.Database.BeginTransaction())
                {
                    try
                    {
                        var existing = JetfDb.ShipmentInbounds.FirstOrDefault(x => x.Id == request.Id);

                        if (existing == null)
                        {
                            throw new Exception("查無此資料");
                        }

                        if (existing.OutboundDate.HasValue)
                        {
                            throw new Exception($"重出日期 {existing.OutboundDate.Value:yyyy/MM/dd}，無法更新資料");
                        }

                        NormalizeExpiredProcessEditLock(existing);

                        if (existing.ProcessStartTime.HasValue &&
                            !string.Equals(existing.ProcessStartOpe, userId, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new Exception($"此筆資料已由 {existing.ProcessStartOpe} 於 {existing.ProcessStartTime.Value:yyyy/MM/dd HH:mm:ss} 開始處理");
                        }

                        var oldProcessType = existing.ProcessType;
                        var oldTax = existing.Tax;
                        var oldCcfee = existing.Ccfee;
                        var oldCod = existing.Cod;
                        var oldFee = existing.Fee;
                        var oldRemark = existing.Remark;
                        var newProcessType = (ShipmentInboundProcessType)request.ProcessType;

                        NormalizeUpdateRequest(request);
                        ValidateUpdateRequest(request, newProcessType);

                        var newFee = ShipmentInboundFeePolicy.CalculateProcessFee(
                            existing.CustCode,
                            newProcessType,
                            request.ProcessTransNo,
                            request.FreightPayerNo,
                            request.FreightFee,
                            request.Tax,
                            request.Ccfee);

                        existing.ProcessType = newProcessType;
                        existing.ProcessTransNo = request.ProcessTransNo;
                        existing.ProcessImporter = request.ProcessImporter;
                        existing.ProcessImporterPhone = request.ProcessImporterPhone;
                        existing.ProcessImporterAddr = request.ProcessImporterAddr;
                        existing.StoreCode = request.StoreCode;
                        existing.StoreName = request.StoreName;
                        existing.FreightPayerNo = request.FreightPayerNo;
                        existing.FreightFee = request.FreightFee;
                        existing.Fee = newFee;
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
                        existing.ProcessStartTime = null;
                        existing.ProcessStartOpe = null;

                        if (oldProcessType != newProcessType)
                        {
                            var oldValueText = oldProcessType.HasValue
                                ? oldProcessType.Value.ToDescription()
                                : string.Empty;
                            var newValueText = newProcessType.ToDescription();

                            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
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
                            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
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
                            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
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
                            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "到付款",
                                OldValue = oldCod?.ToString(),
                                NewValue = request.Cod.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        if (oldFee != newFee)
                        {
                            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                            {
                                ShipmentInboundId = request.Id,
                                FieldName = "手續費",
                                OldValue = oldFee?.ToString(),
                                NewValue = newFee.ToString(),
                                EditTime = DateTime.Now,
                                EditUser = userId
                            });
                        }

                        AddEditHistoryIfOldValueExists(request.Id, "備註", oldRemark, request.Remark, userId);

                        JetfDb.SaveChanges();
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

        /// <summary>
        /// 正規化更新請求中的字串欄位，移除頭尾空白。
        /// </summary>
        /// <param name="request">待更新的請求資料。</param>
        private void NormalizeUpdateRequest(ShipmentInboundProcessUpdateRequest request)
        {
            request.ProcessImporter = request.ProcessImporter?.Trim();
            request.ProcessImporterPhone = request.ProcessImporterPhone?.Trim();
            request.ProcessImporterAddr = request.ProcessImporterAddr?.Trim();
            request.StoreCode = request.StoreCode?.Trim();
            request.StoreName = request.StoreName?.Trim();
            request.CarNo = request.CarNo?.Trim();
            request.Remark = request.Remark?.Trim();
        }

        /// <summary>
        /// 驗證更新請求是否符合指定處理方式的必填規則。
        /// </summary>
        /// <param name="request">待驗證的請求資料。</param>
        /// <param name="newProcessType">本次設定的處理方式。</param>
        private void ValidateUpdateRequest(
            ShipmentInboundProcessUpdateRequest request,
            ShipmentInboundProcessType newProcessType)
        {
            if (newProcessType != ShipmentInboundProcessType.NewTrackingNo)
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

        /// <summary>
        /// 依 Id 取得單筆貨件回倉處理明細。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <returns>單筆明細資料。</returns>
        public ShipmentInboundProcessDetailModel GetDetailById(int id)
        {
            {
                var detail = JetfDb.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ShipmentInboundProcessDetailModel
                    {
                        Id = x.Id,
                        CustCode = x.CustCode,
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

                if (detail != null)
                {
                    detail.IsSpecialFeeCustomer = ShipmentInboundFeePolicy.IsSpecialFeeCustomer(detail.CustCode);
                }

                return detail;
            }
        }

        /// <summary>
        /// 開始編輯指定貨件回倉資料，建立處理鎖定。
        /// 若原鎖定已逾時，會由目前使用者接手；若原鎖定者就是目前使用者，則直接續用編輯。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <returns>最新的單筆列表資料。</returns>
        public ShipmentInboundProcessModel BeginProcessEdit(int id)
        {
            var userId = GetUserId();

            {
                var entity = JetfDb.ShipmentInbounds.FirstOrDefault(x => x.Id == id);
                if (entity == null)
                {
                    throw new Exception("查無此資料");
                }

                NormalizeExpiredProcessEditLock(entity);

                if (entity.ProcessStartTime.HasValue)
                {
                    if (string.Equals(entity.ProcessStartOpe, userId, StringComparison.OrdinalIgnoreCase))
                    {
                        entity.ProcessStartTime = DateTime.Now;
                        JetfDb.SaveChanges();
                        return BuildShipmentInboundProcessModel(entity);
                    }

                    throw new Exception($"此筆資料已由 {entity.ProcessStartOpe} 於 {entity.ProcessStartTime.Value:yyyy/MM/dd HH:mm:ss} 開始處理");
                }

                entity.ProcessStartTime = DateTime.Now;
                entity.ProcessStartOpe = userId;
                JetfDb.SaveChanges();

                return BuildShipmentInboundProcessModel(entity);
            }
        }

        /// <summary>
        /// 釋放指定貨件回倉資料的處理鎖定。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <returns>最新的單筆列表資料。</returns>
        public ShipmentInboundProcessModel ReleaseProcessEdit(int id)
        {
            var userId = GetUserId();

            {
                var entity = JetfDb.ShipmentInbounds.FirstOrDefault(x => x.Id == id);
                if (entity == null)
                {
                    throw new Exception("查無此資料");
                }

                if (NormalizeExpiredProcessEditLock(entity))
                {
                    JetfDb.SaveChanges();
                    return BuildShipmentInboundProcessModel(entity);
                }

                if (!entity.ProcessStartTime.HasValue)
                {
                    return BuildShipmentInboundProcessModel(entity);
                }

                if (!string.Equals(entity.ProcessStartOpe, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return BuildShipmentInboundProcessModel(entity);
                }

                entity.ProcessStartTime = null;
                entity.ProcessStartOpe = null;
                JetfDb.SaveChanges();

                return BuildShipmentInboundProcessModel(entity);
            }
        }

        /// <summary>
        /// 依 Id 取得單筆列表資料。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <returns>單筆列表資料。</returns>
        public ShipmentInboundProcessModel GetRowById(int id)
        {
            {
                var entity = JetfDb.ShipmentInbounds.AsNoTracking().FirstOrDefault(x => x.Id == id);
                if (entity == null)
                {
                    throw new Exception("查無此資料");
                }

                return BuildShipmentInboundProcessModel(entity);
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
        /// 依查詢條件組合篩選條件。
        /// </summary>
        /// <param name="query">原始查詢。</param>
        /// <param name="request">查詢條件。</param>
        /// <returns>套用條件後的查詢物件。</returns>
        private IQueryable<Data.ShipmentInboundEntity> BuildWhereConditions(
            IQueryable<Data.ShipmentInboundEntity> query,
            ShipmentInboundProcessRequest request)
        {
            query = query.WhereIf(!string.IsNullOrWhiteSpace(request.DataType), x => x.DataType == request.DataType);
            query = query.WhereIf(
                DateTime.TryParse(request.InboundDateStart, out var startDate),
                x => x.InboundDate >= startDate);

                if (DateTime.TryParse(request.InboundDateEnd, out var endDate))
                {
                    var inboundDateEnd = endDate.AddDays(1);
                    query = query.WhereIf(true, x => x.InboundDate < inboundDateEnd);
                }

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
            query = query.WhereIf(request.IsOrderOriginal.HasValue, x => x.IsOrderOriginal == request.IsOrderOriginal.Value);

            // 結案條件處理，不包含開箱確認內容物狀況、暫存資料
            query = query.WhereIf(
                request.IsClosed == true,
                x => x.ProcessType.HasValue
                    && x.ProcessType != ShipmentInboundProcessType.InspectContents
                    && x.ProcessType != ShipmentInboundProcessType.ConfirmOuterLabel
                    && x.ProcessType != ShipmentInboundProcessType.TempData);
            query = query.WhereIf(
                request.IsClosed == false,
                x => !x.ProcessTime.HasValue
                    || x.ProcessType == ShipmentInboundProcessType.InspectContents
                    || x.ProcessType == ShipmentInboundProcessType.ConfirmOuterLabel
                    || x.ProcessType == ShipmentInboundProcessType.TempData);

            return query;
        }

        /// <summary>
        /// 取得空運客戶名稱對照表。
        /// </summary>
        /// <param name="custCodes">客戶代碼清單。</param>
        /// <returns>客戶代碼與名稱對照。</returns>
        private Dictionary<string, string> GetAirCustNames(List<string> custCodes)
        {
            return GetAirCustomerNames(custCodes);
        }

        /// <summary>
        /// 將資料實體轉為列表顯示模型。
        /// 若處理鎖定已逾時，會清空顯示欄位。
        /// </summary>
        /// <param name="entity">貨件回倉資料實體。</param>
        /// <returns>列表顯示模型。</returns>
        private ShipmentInboundProcessModel BuildShipmentInboundProcessModel(Data.ShipmentInboundEntity entity)
        {
            var model = new ShipmentInboundProcessModel
            {
                Id = entity.Id,
                DataType = entity.DataType,
                InboundDate = entity.InboundDate,
                TrackingNo = entity.TrackingNo,
                SourceType = entity.SourceType.HasValue
                    ? (ShipmentInboundSourceType)entity.SourceType.Value
                    : default(ShipmentInboundSourceType),
                ReturnTrackingNo = entity.ReturnTrackingNo,
                CustCode = entity.CustCode,
                IsSpecialFeeCustomer = ShipmentInboundFeePolicy.IsSpecialFeeCustomer(entity.CustCode),
                TransNo = entity.TransNo,
                TransName = entity.TransName,
                ReturnReason = entity.ReturnReason,
                ProcessType = entity.ProcessType.HasValue
                    ? (ShipmentInboundProcessType?)entity.ProcessType.Value
                    : null,
                ProcessStartTime = entity.ProcessStartTime,
                ProcessStartOpe = entity.ProcessStartOpe,
                Tax = entity.Tax,
                Ccfee = entity.Ccfee,
                Cod = entity.Cod,
                Remark = entity.Remark
            };

            NormalizeExpiredProcessEditDisplay(model);
            FillCustomerAndTransNames(new List<ShipmentInboundProcessModel> { model });
            FillLatestExceptionReasons(new List<ShipmentInboundProcessModel> { model });

            return model;
        }

        /// <summary>
        /// 一次查出目前頁面單號在查詢結果中的重複筆數，避免逐筆查詢。
        /// </summary>
        /// <param name="data">目前頁面資料。</param>
        /// <param name="query">已套用查詢條件的資料來源。</param>
        private void FillTrackingNoCounts(List<ShipmentInboundProcessModel> data, IQueryable<Data.ShipmentInboundEntity> query)
        {
            var trackingNos = data
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .Select(x => x.TrackingNo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!trackingNos.Any())
            {
                return;
            }

            var counts = query
                .Where(x => trackingNos.Contains(x.TrackingNo))
                .GroupBy(x => x.TrackingNo)
                .Select(x => new
                {
                    TrackingNo = x.Key,
                    Count = x.Count()
                })
                .ToList()
                .ToDictionary(x => x.TrackingNo, x => x.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var item in data)
            {
                if (!string.IsNullOrWhiteSpace(item.TrackingNo) &&
                    counts.TryGetValue(item.TrackingNo, out var count))
                {
                    item.TrackingNoCount = count;
                }
            }
        }

        /// <summary>
        /// 取得海運客戶名稱對照表。
        /// </summary>
        /// <param name="custCodes">客戶代碼清單。</param>
        /// <returns>客戶代碼與名稱對照。</returns>
        private Dictionary<string, string> GetSeaCustNames(List<string> custCodes)
        {
            return GetSeaCustomerNames(custCodes);
        }

        /// <summary>
        /// 取得空運派件公司名稱對照表。
        /// </summary>
        /// <param name="transNos">派件公司代碼清單。</param>
        /// <returns>派件公司代碼與名稱對照。</returns>
        private Dictionary<string, string> GetAirTransNames(List<string> transNos)
        {
            return base.GetAirTransNames(transNos);
        }

        /// <summary>
        /// Marks rows whose freight fee and handling fee are forced to zero by customer policy.
        /// </summary>
        /// <param name="data">Rows to mark.</param>
        private void FillFeePolicyFlags(List<ShipmentInboundProcessModel> data)
        {
            foreach (var item in data)
            {
                item.IsSpecialFeeCustomer = ShipmentInboundFeePolicy.IsSpecialFeeCustomer(item.CustCode);
            }
        }

        /// <summary>
        /// 補齊客戶名稱與派件公司名稱。
        /// </summary>
        /// <param name="data">待補齊名稱的資料列。</param>
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
        /// 一次回填查詢結果每筆資料最後一筆異常原因，避免逐筆查詢。
        /// </summary>
        private void FillLatestExceptionReasons(List<ShipmentInboundProcessModel> data)
        {
            var latestExceptionReasonDict = GetLatestExceptionReasonMap(data.Select(x => x.Id).Distinct().ToList());

            foreach (var item in data)
            {
                if (latestExceptionReasonDict.TryGetValue(item.Id, out var exceptionReason))
                {
                    item.ExceptionReason = exceptionReason;
                }
            }
        }

        /// <summary>
        /// 取得指定貨件清單最後一筆異常原因對照表。
        /// </summary>
        /// <param name="shipmentInboundIds">貨件入庫 Id 清單。</param>
        /// <returns>貨件入庫 Id 與異常原因對照。</returns>
        private Dictionary<int, string> GetLatestExceptionReasonMap(List<int> shipmentInboundIds)
        {
            if (!shipmentInboundIds.Any())
            {
                return new Dictionary<int, string>();
            }

            var latestExceptions = JetfDb.ShipmentInboundExceptions
                .AsNoTracking()
                .WhereBulkContains(
                    JetfDb,
                    shipmentInboundIds,
                    row => row.ShipmentInboundId,
                    key => key);

            var exceptionReasonIds = latestExceptions
                .Where(x => x.ExceptionReasonId.HasValue)
                .Select(x => x.ExceptionReasonId.Value)
                .Distinct()
                .ToList();

            var exceptionReasonDict = JetfDb.ShipmentInboundExceptionReasons
                .AsNoTracking()
                .WhereBulkContains(
                    JetfDb,
                    exceptionReasonIds,
                    row => row.Id,
                    key => key)
                .ToDictionary(x => x.Id, x => x.Reason);

            return latestExceptions
                .GroupBy(x => x.ShipmentInboundId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(x => x.CreatedTime ?? DateTime.MinValue)
                        .ThenByDescending(x => x.Id)
                        .Select(x => x.ExceptionReasonId.HasValue && exceptionReasonDict.ContainsKey(x.ExceptionReasonId.Value)
                            ? exceptionReasonDict[x.ExceptionReasonId.Value]
                            : null)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty);
        }

        /// <summary>
        /// 將逾時的處理鎖定從顯示模型中清空，避免前端顯示過期鎖定資訊。
        /// </summary>
        /// <param name="data">待處理的資料列清單。</param>
        private void NormalizeExpiredProcessEditDisplay(List<ShipmentInboundProcessModel> data)
        {
            foreach (var item in data)
            {
                NormalizeExpiredProcessEditDisplay(item);
            }
        }

        /// <summary>
        /// 將逾時的處理鎖定從顯示模型中清空，避免前端顯示過期鎖定資訊。
        /// </summary>
        /// <param name="model">待處理的資料列。</param>
        private void NormalizeExpiredProcessEditDisplay(ShipmentInboundProcessModel model)
        {
            if (model == null || !IsProcessEditExpired(model.ProcessStartTime))
            {
                return;
            }

            model.ProcessStartTime = null;
            model.ProcessStartOpe = null;
        }

        /// <summary>
        /// 清除資料實體中已逾時的處理鎖定。
        /// </summary>
        /// <param name="entity">待處理的資料實體。</param>
        /// <returns>是否有清除逾時鎖定。</returns>
        private bool NormalizeExpiredProcessEditLock(Data.ShipmentInboundEntity entity)
        {
            if (entity == null || !IsProcessEditExpired(entity.ProcessStartTime))
            {
                return false;
            }

            entity.ProcessStartTime = null;
            entity.ProcessStartOpe = null;
            return true;
        }

        /// <summary>
        /// 判斷處理鎖定是否已逾時。
        /// </summary>
        /// <param name="processStartTime">開始處理時間。</param>
        /// <returns>逾時回傳 true，否則回傳 false。</returns>
        private bool IsProcessEditExpired(DateTime? processStartTime)
        {
            return processStartTime.HasValue
                && processStartTime.Value.AddMinutes(ProcessEditLockTimeoutMinutes) <= DateTime.Now;
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>Excel 檔案位元組陣列。</returns>
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

        /// <summary>
        /// 建立貨件回倉處理匯出 Excel 活頁簿。
        /// </summary>
        /// <param name="data">匯出資料。</param>
        /// <returns>Excel 活頁簿物件。</returns>
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
                "異常原因",
                "退件原因",
                "處理方式",
                "備註"
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
                NpoiCell.CreateCell(row, 7, item.ExceptionReason ?? "", dataStyle);
                NpoiCell.CreateCell(row, 8, item.ReturnReason ?? "", dataStyle);
                NpoiCell.CreateCell(row, 9, item.ProcessTypeName ?? "", dataStyle);
                NpoiCell.CreateCell(row, 10, item.Remark ?? "", dataStyle);
            }

            sheet.AutoSizeColumns(headers.Length, scale: 1.2, minWidth: 15);

            return workbook;
        }

        /// <summary>
        /// 批量上傳(貨件回倉處理)
        /// Excel 標題：單號、處理方式(中文)、退件原因、備註；退件原因/備註內容可空白
        /// 整批驗證：任一筆驗證失敗則整批失敗，不更新任何資料。
        /// </summary>
        /// <param name="filePath">上傳檔案路徑。</param>
        /// <returns>批次處理結果。</returns>
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
            {
                var existingData = JetfDb.ShipmentInbounds
                    .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                    .ToDictionary(x => x.TrackingNo, x => x);

                using (var tx = JetfDb.Database.BeginTransaction())
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
                                var oldReturnReason = existing.ReturnReason;
                                var oldRemark = existing.Remark;
                                var shipmentInboundId = existing.Id;
                                var targetProcessType = (ShipmentInboundProcessType)newProcessType.Value;

                                existing.ProcessType = targetProcessType;
                                if (row.ReturnReason != null)
                                {
                                    existing.ReturnReason = row.ReturnReason;
                                }

                                if (row.Remark != null)
                                {
                                    existing.Remark = row.Remark;
                                }

                                existing.ProcessTime = DateTime.Now;
                                existing.ProcessOpe = userId;

                                if (oldProcessType != targetProcessType)
                                {
                                    var oldValueText = oldProcessType.HasValue
                                        ? oldProcessType.Value.ToDescription()
                                        : string.Empty;
                                    var newValueText = targetProcessType.ToDescription();

                                    JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
                                    {
                                        ShipmentInboundId = shipmentInboundId,
                                        FieldName = "處理方式",
                                        OldValue = oldValueText,
                                        NewValue = newValueText,
                                        EditTime = DateTime.Now,
                                        EditUser = userId
                                    });
                                }

                                if (row.ReturnReason != null)
                                {
                                    AddEditHistoryIfOldValueExists(shipmentInboundId, "退件原因", oldReturnReason, row.ReturnReason, userId);
                                }

                                if (row.Remark != null)
                                {
                                    AddEditHistoryIfOldValueExists(shipmentInboundId, "備註", oldRemark, row.Remark, userId);
                                }
                            }
                        }

                        JetfDb.SaveChanges();
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

        /// <summary>
        /// 驗證批量上傳的每一列資料。
        /// </summary>
        /// <param name="rows">批量上傳資料列。</param>
        /// <returns>驗證錯誤清單。</returns>
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
                {
                    existing = JetfDb.ShipmentInbounds
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

        /// <summary>
        /// 判斷批次上傳是否允許指定的處理方式。
        /// </summary>
        /// <param name="processType">處理方式代碼。</param>
        /// <returns>允許回傳 true，否則回傳 false。</returns>
        private bool IsAllowedBatchProcessType(int processType)
        {
            // 僅允許：TransferFromOriginal(2)、ReturnToSite(3)、Destroy(5)、AddToReturnShipment(6)、InspectContents(7)、ConfirmOuterLabel(8)、TempData(9)、TransferBySystem(10)
            var allowedTypes = new[] {
                (int)ShipmentInboundProcessType.TransferFromOriginal,
                (int)ShipmentInboundProcessType.ReturnToSite,
                (int)ShipmentInboundProcessType.Destroy,
                (int)ShipmentInboundProcessType.AddToReturnShipment,
                (int)ShipmentInboundProcessType.InspectContents,
                (int)ShipmentInboundProcessType.ConfirmOuterLabel,
                (int)ShipmentInboundProcessType.TempData,
                (int)ShipmentInboundProcessType.TransferBySystem
            };

            return allowedTypes.Contains(processType);
        }

        /// <summary>
        /// 讀取批量上傳處理方式的 Excel 內容。
        /// </summary>
        /// <param name="filePath">上傳檔案路徑。</param>
        /// <returns>批量上傳資料列。</returns>
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
            int returnReasonIndex = -1;
            int remarkIndex = -1;
            var requiredHeaders = new[] { "單號", "處理方式", "退件原因", "備註" };

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                if (!read)
                {
                    var headerIndexes = GetExcelHeaderIndexes(row);
                    if (!requiredHeaders.Any(headerIndexes.ContainsKey))
                    {
                        continue;
                    }

                    var missingHeaders = requiredHeaders
                        .Where(x => !headerIndexes.ContainsKey(x))
                        .ToList();
                    if (missingHeaders.Any())
                    {
                        throw new Exception($"Excel 缺少必要欄位：{string.Join("、", missingHeaders)}");
                    }

                    trackingNoIndex = headerIndexes["單號"];
                    processTypeIndex = headerIndexes["處理方式"];
                    if (headerIndexes.ContainsKey("退件原因"))
                    {
                        returnReasonIndex = headerIndexes["退件原因"];
                    }

                    if (headerIndexes.ContainsKey("備註"))
                    {
                        remarkIndex = headerIndexes["備註"];
                    }

                    read = true;
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex);
                var processTypeText = row.GetCellData(processTypeIndex);
                var returnReason = returnReasonIndex >= 0 ? row.GetCellData(returnReasonIndex) : null;
                var remark = remarkIndex >= 0 ? row.GetCellData(remarkIndex) : null;

                if (string.IsNullOrWhiteSpace(trackingNo)
                    && string.IsNullOrWhiteSpace(processTypeText)
                    && string.IsNullOrWhiteSpace(returnReason)
                    && string.IsNullOrWhiteSpace(remark))
                {
                    continue;
                }

                result.Add(new ShipmentInboundProcessBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    ProcessTypeText = processTypeText,
                    ReturnReason = returnReason,
                    Remark = remark
                });
            }

            return result;
        }

        private Dictionary<string, int> GetExcelHeaderIndexes(IRow row)
        {
            var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < row.LastCellNum; c++)
            {
                var header = (row.GetCellData(c) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(header) || indexes.ContainsKey(header))
                {
                    continue;
                }

                indexes.Add(header, c);
            }

            return indexes;
        }

        /// <summary>
        /// 更新退件原因
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <param name="returnReason">新的退件原因。</param>
        public void UpdateReturnReason(int id, string returnReason)
        {
            var userId = GetUserId();
            var entity = JetfDb.ShipmentInbounds.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                throw new Exception("查無此資料");
            }

            if (entity.OutboundDate.HasValue)
            {
                throw new Exception($"出庫日期 {entity.OutboundDate.Value:yyyy/MM/dd}，無法更新資料");
            }

            var oldReturnReason = entity.ReturnReason;
            if (string.Equals(oldReturnReason ?? string.Empty, returnReason ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            entity.ReturnReason = returnReason;
            AddEditHistoryIfOldValueExists(id, "退件原因", oldReturnReason, returnReason, userId);
            JetfDb.SaveChanges();
        }

        /// <summary>
        /// 更新備註並寫入編輯歷史。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <param name="remark">新的備註。</param>
        public void UpdateRemark(int id, string remark)
        {
            var userId = GetUserId();
            var entity = JetfDb.ShipmentInbounds.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                throw new Exception("查無資料");
            }

            if (entity.OutboundDate.HasValue)
            {
                throw new Exception($"已出庫日期 {entity.OutboundDate.Value:yyyy/MM/dd}，無法更新備註");
            }

            var oldRemark = entity.Remark;
            if (string.Equals(oldRemark ?? string.Empty, remark ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            entity.Remark = remark;
            AddEditHistoryIfOldValueExists(id, "備註", oldRemark, remark, userId);
            JetfDb.SaveChanges();
        }

        private void AddEditHistoryIfOldValueExists(int shipmentInboundId, string fieldName, string oldValue, string newValue, string userId)
        {
            if (string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            JetfDb.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
            {
                ShipmentInboundId = shipmentInboundId,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                EditTime = DateTime.Now,
                EditUser = userId
            });
        }

        /// <summary>
        /// 批量上傳退件原因。
        /// Excel 欄位：單號、退件原因、備註。
        /// 驗證規則：失敗列回傳原因，成功列仍會更新退件原因與備註。
        /// </summary>
        /// <param name="filePath">上傳檔案路徑。</param>
        /// <returns>批次處理結果。</returns>
        public ResponseModel BatchUploadReturnReason(string filePath)
        {
            var res = new ResponseModel { status = Status.success, msg = "上傳成功" };
            var userId = GetUserId();

            var rows = ReadReturnReasonBatchUploadExcel(filePath);
            if (rows.Count == 0)
            {
                res.status = Status.error;
                res.msg = "Excel 無資料";
                return res;
            }

            var errors = ValidateReturnReasonBatchUploadRows(rows);
            var invalidRowNos = new HashSet<int>(errors.Select(x => x.RowNo));
            var validRows = rows
                .Where(x => !invalidRowNos.Contains(x.RowNo))
                .ToList();
            var successCount = 0;
            {
                var trackingNos = validRows
                    .Select(x => (x.TrackingNo ?? string.Empty).Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var entities = JetfDb.ShipmentInbounds
                    .Where(x => trackingNos.Contains(x.TrackingNo) && !x.OutboundDate.HasValue)
                    .ToList()
                    .GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

                if (validRows.Any())
                {
                    using (var tx = JetfDb.Database.BeginTransaction())
                    {
                        try
                        {
                            foreach (var row in validRows)
                            {
                                var trackingNo = (row.TrackingNo ?? string.Empty).Trim();
                                if (!entities.ContainsKey(trackingNo))
                                {
                                    errors.Add(new ReturnReasonBatchUploadErrorModel
                                    {
                                        RowNo = row.RowNo,
                                        TrackingNo = row.TrackingNo,
                                        FieldName = "單號",
                                        Reason = "查無資料或已出庫"
                                    });
                                    continue;
                                }

                                var entity = entities[trackingNo];
                                var oldReturnReason = entity.ReturnReason;
                                var oldRemark = entity.Remark;

                                entity.ReturnReason = row.ReturnReason;
                                entity.Remark = row.Remark;
                                AddEditHistoryIfOldValueExists(entity.Id, "退件原因", oldReturnReason, row.ReturnReason, userId);
                                AddEditHistoryIfOldValueExists(entity.Id, "備註", oldRemark, row.Remark, userId);
                                successCount++;
                            }

                            JetfDb.SaveChanges();
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }

            var failureCount = errors.Select(x => x.RowNo).Distinct().Count();
            res.status = successCount > 0 ? Status.success : Status.error;
            res.msg = $"成功 {successCount} 筆，失敗 {failureCount} 筆。";
            res.ReturnObject = new
            {
                SuccessCount = successCount,
                FailureCount = failureCount,
                Errors = errors
            };
            return res;
        }

        private List<ReturnReasonBatchUploadErrorModel> ValidateReturnReasonBatchUploadRows(List<ReturnReasonBatchUploadRowModel> rows)
        {
            var errors = new List<ReturnReasonBatchUploadErrorModel>();

            foreach (var row in rows)
            {
                row.TrackingNo = row.TrackingNo?.Trim();
                row.ReturnReason = row.ReturnReason?.Trim();
                row.Remark = row.Remark?.Trim();

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
                {
                    existing = JetfDb.ShipmentInbounds
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
                        if (header == "退件原因") returnReasonIndex = c;
                        if (header == "備註") remarkIndex = c;
                    }

                    if (trackingNoIndex >= 0 && returnReasonIndex >= 0 && remarkIndex >= 0)
                    {
                        read = true;
                    }
                    continue;
                }

                var trackingNo = row.GetCellData(trackingNoIndex);
                var returnReason = row.GetCellData(returnReasonIndex);
                var remark = row.GetCellData(remarkIndex);

                if (string.IsNullOrWhiteSpace(trackingNo)
                    && string.IsNullOrWhiteSpace(returnReason)
                    && string.IsNullOrWhiteSpace(remark))
                {
                    continue;
                }

                result.Add(new ReturnReasonBatchUploadRowModel
                {
                    RowNo = i + 1,
                    TrackingNo = trackingNo,
                    ReturnReason = returnReason,
                    Remark = remark
                });
            }

            if (!read)
            {
                throw new Exception("Excel 欄位需包含：單號、退件原因、備註");
            }

            return result;
        }
    }
}

