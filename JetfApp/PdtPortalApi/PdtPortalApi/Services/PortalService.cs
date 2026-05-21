using System.Globalization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PdtPortalApi.Data;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Entities;
using PdtPortalApi.Models.Enums;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Options;
using Renci.SshNet;

namespace PdtPortalApi.Services;

public sealed class PortalService(
    JetfDbContext jetfDbContext,
    DataCenterDbContext dataCenterDbContext,
    IOptions<ShipmentInboundPhotoSftpOptions> shipmentInboundPhotoSftpOptions,
    ILogger<PortalService> logger) : IPortalService
{
    private const string LocationFieldName = "儲位";
    private readonly JetfDbContext _jetfDbContext = jetfDbContext;
    private readonly DataCenterDbContext _dataCenterDbContext = dataCenterDbContext;
    private readonly ShipmentInboundPhotoSftpOptions _shipmentInboundPhotoSftpOptions = shipmentInboundPhotoSftpOptions.Value;
    private readonly ILogger<PortalService> _logger = logger;

    /// <summary>
    /// 依帳號檢查使用者是否存在。
    /// </summary>
    /// <param name="account">登入帳號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>存在時回傳 true，否則回傳 false。</returns>
    public async Task<bool> LoginAsync(string account, CancellationToken cancellationToken)
    {
        try
        {
            return await _jetfDbContext.UserMasters
                .AsNoTracking()
                .AnyAsync(entity => entity.UserId == account, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢登入帳號失敗，Account: {Account}", account);
            throw;
        }
    }

    /// <summary>
    /// 取得貨件來源清單。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>貨件來源資料。</returns>
    public async Task<IReadOnlyList<ShipmentInboundSourceTypeDto>> GetShipmentInboundSourcesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _jetfDbContext.ShipmentInboundSourceTypes
                .AsNoTracking()
                .OrderBy(entity => entity.Id)
                .Select(entity => new ShipmentInboundSourceTypeDto
                {
                    Id = entity.Id,
                    SourceType = entity.SourceType
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢貨件來源清單失敗");
            throw;
        }
    }

    /// <summary>
    /// 檢查是否存在原始入庫資料。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="sourceType">貨件來源。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>單號檢查結果。</returns>
    public async Task<TrackingCheckResult> CheckInboundDataAsync(
        string trackingNo,
        ShipmentInboundSourceType sourceType,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedTrackingNo = trackingNo.Trim();
            var expectedLength = GetExpectedTrackingNoLength(sourceType);
            var isTrackingNoLengthValid = !expectedLength.HasValue || normalizedTrackingNo.Length == expectedLength.Value;

            var seaOrderExists = await _dataCenterDbContext.SeaOrderOriginals
                .AsNoTracking()
                .AnyAsync(entity => entity.JetfSerial == normalizedTrackingNo, cancellationToken);

            var airOrderExists = await _dataCenterDbContext.OriginalLists
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.TrackingNo == normalizedTrackingNo || entity.DeliveryNo == normalizedTrackingNo,
                    cancellationToken);

            var hasOriginalData = seaOrderExists || airOrderExists;
            var messages = new List<string>();
            if (!hasOriginalData)
            {
                messages.Add("不明貨");
            }
            if (!isTrackingNoLengthValid)
            {
                messages.Add($"貨件來源長度不符合:{expectedLength!.Value}碼");
            }

            return new TrackingCheckResult
            {
                IsValid = messages.Count == 0,
                Message = messages.Count == 0 ? "操作成功" : string.Join("、", messages)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "檢查原始入庫資料失敗，TrackingNo: {TrackingNo}, SourceType: {SourceType}",
                trackingNo,
                sourceType);
            throw;
        }
    }

    /// <summary>
    /// 建立入庫資料。
    /// </summary>
    /// <param name="request">入庫請求資料。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>處理結果。</returns>
    public async Task<ServiceResult> CreateShipmentInboundAsync(CreateShipmentInboundRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var seqNo = request.SeqNo ?? string.Empty;
            if (string.IsNullOrEmpty(seqNo))
            {
                return ServiceResult.Fail(
                    "SEQ_NO_REQUIRED",
                    "流水編號必填",
                    StatusCodes.Status400BadRequest);
            }

            var isDuplicateSeqNo = await IsDuplicateShipmentInboundSeqNoAsync(seqNo, cancellationToken);
            if (isDuplicateSeqNo)
            {
                _logger.LogDebug("入庫流水編號重複 SeqNo: {SeqNo}", seqNo);
                return ServiceResult.Fail(
                    "DUPLICATE_SEQ_NO",
                    "流水編號已存在，請確認後再寫入",
                    StatusCodes.Status409Conflict);
            }

            var isDuplicate = await IsDuplicateShipmentInboundAsync(request.TrackingNo, cancellationToken);
            if (isDuplicate)
            {
                _logger.LogDebug("入庫資料重複 TrackingNo: {TrackingNo}", request.TrackingNo);
                return ServiceResult.Fail(
                    "DUPLICATE_TRACKING_NO",
                    "入庫資料重複，該單號已有符合條件的資料",
                    StatusCodes.Status409Conflict);
            }

            var seaData = await GetSeaDataAsync(request.TrackingNo, cancellationToken);
            var airData = seaData is null
                ? await GetAirDataAsync(request.TrackingNo, cancellationToken)
                : null;

            var hasOriginalData = seaData is not null || airData is not null;
            var dataType = seaData is not null ? "海運" : airData is not null ? "空運" : string.Empty;
            var size = string.IsNullOrWhiteSpace(request.Size) ? "小" : request.Size.Trim();

            var originalJetfSerial = string.Empty;
            var mainNumber = string.Empty;
            var originalTrackingNo = string.Empty;
            var custCode = string.Empty;
            var transNo = string.Empty;
            var transName = string.Empty;
            var importer = string.Empty;
            var importerPhone = string.Empty;
            var importerAddr = string.Empty;
            var sourceCod = 0;

            switch (dataType)
            {
                case "海運":
                    originalJetfSerial = seaData?.OriginalJetfSerial ?? string.Empty;
                    mainNumber = seaData?.MainNumber ?? string.Empty;
                    originalTrackingNo = seaData?.OriginalTrackingNo ?? string.Empty;
                    custCode = seaData?.CustCode ?? string.Empty;
                    transName = seaData?.TransName ?? string.Empty;
                    importer = seaData?.Importer ?? string.Empty;
                    importerPhone = seaData?.ImporterPhone ?? string.Empty;
                    importerAddr = seaData?.ImporterAddr ?? string.Empty;
                    sourceCod = seaData?.Cc ?? 0;
                    break;

                case "空運":
                    originalJetfSerial = airData?.OriginalJetfSerial ?? string.Empty;
                    mainNumber = airData?.MainNumber ?? string.Empty;
                    originalTrackingNo = airData?.OriginalTrackingNo ?? string.Empty;
                    custCode = airData?.CustCode ?? string.Empty;
                    transNo = airData?.TransNo ?? string.Empty;
                    importer = airData?.Importer ?? string.Empty;
                    importerPhone = airData?.ImporterPhone ?? string.Empty;
                    importerAddr = airData?.ImporterAddr ?? string.Empty;
                    sourceCod = airData?.Cc ?? 0;
                    break;
            }

            var feeData = string.IsNullOrWhiteSpace(originalJetfSerial)
                ? null
                : await GetFeeDataAsync(originalJetfSerial, cancellationToken);
            var cod = feeData?.Cod ?? sourceCod;
            var fee = (feeData?.Tax ?? 0) > 0 || (feeData?.Ccfee ?? 0) > 0 ? 30 : 0;

			var entity = new ShipmentInboundEntity
			{
				DataType = dataType,
                InboundDate = request.InboundDate.LocalDateTime,
				TrackingNo = request.TrackingNo,
				SeqNo = seqNo,
 				LocationCode = request.LocationCode,
 				SourceType = request.SourceType,
 				ReturnTrackingNo = request.ReturnTrackingNo ?? string.Empty,
                OriginalJetfSerial = originalJetfSerial,
                MainNumber = mainNumber,
                OriginalTrackingNo = originalTrackingNo,
                Size = size,
 				CustCode = custCode,
 				TransNo = transNo,
 				TransName = transName,
				Importer = importer,
				ImporterPhone = importerPhone,
				ImporterAddr = importerAddr,
				IsOrderOriginal = hasOriginalData,
                UploadOpe = request.UploadOpe ?? string.Empty,
				CreatedTime = DateTime.Now,
				Tax = feeData?.Tax ?? 0,
				Ccfee = feeData?.Ccfee ?? 0,
				Cod = cod,
				Fee = fee,
            };

			await _jetfDbContext.ShipmentInbounds.AddAsync(entity, cancellationToken);
			var affectedRows = await _jetfDbContext.SaveChangesAsync(cancellationToken);

			return affectedRows > 0
				? ServiceResult.Success("入庫資料寫入成功")
				: ServiceResult.Fail("INSERT_FAILED", "入庫資料寫入失敗");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "寫入入庫資料失敗，TrackingNo: {TrackingNo}", request.TrackingNo);
            return ServiceResult.Fail(
                "INTERNAL_SERVER_ERROR",
                "入庫資料寫入時發生未預期錯誤",
                StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 取得異常原因清單。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>異常原因資料。</returns>
    public async Task<IReadOnlyList<ShipmentInboundExceptionReasonDto>> GetShipmentInboundExceptionReasonsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _jetfDbContext.ShipmentInboundExceptionReasons
                .AsNoTracking()
                .OrderBy(entity => entity.Id)
                .Select(entity => new ShipmentInboundExceptionReasonDto
                {
                    Id = entity.Id,
                    Reason = entity.Reason
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢異常原因清單失敗");
            throw;
        }
    }

    /// <summary>
    /// 建立異常件資料。
    /// </summary>
    /// <param name="request">異常件請求資料。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>處理結果。</returns>
    public async Task<ServiceResult> CreateShipmentInboundExceptionAsync(
        CreateShipmentInboundExceptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var seqNo = request.SeqNo.Trim();
            var matchingInbounds = await _jetfDbContext.ShipmentInbounds
                .AsNoTracking()
                .Where(entity => entity.SeqNo == seqNo && entity.OutboundTime == null)
                .Select(entity => new
                {
                    entity.Id
                })
                .ToListAsync(cancellationToken);

            if (matchingInbounds.Count == 0)
            {
                return ServiceResult.Fail(
                    "SHIPMENT_INBOUND_NOT_FOUND",
                    "查無符合條件的入庫資料");
            }

            if (matchingInbounds.Count > 1)
            {
                return ServiceResult.Fail(
                    "MULTIPLE_SHIPMENT_INBOUND_FOUND",
                    "查到多筆符合條件的入庫資料，請確認流水號");
            }

            var photoPathResult = await SaveExceptionPhotoAsync(request.Photo, cancellationToken);
            if (!photoPathResult.IsSuccess || string.IsNullOrWhiteSpace(photoPathResult.PhotoPath))
            {
                return ServiceResult.Fail(photoPathResult.ErrorCode, photoPathResult.Message, photoPathResult.Code);
            }

            var exceptionReasonExists = await _jetfDbContext.ShipmentInboundExceptionReasons
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == request.ExceptionReasonId, cancellationToken);
            if (!exceptionReasonExists)
            {
                return ServiceResult.Fail(
                    "SHIPMENT_INBOUND_EXCEPTION_REASON_NOT_FOUND",
                    "查無符合條件的異常原因");
            }

            var entity = new ShipmentInboundExceptionEntity
            {
                ShipmentInboundId = matchingInbounds[0].Id,
                SeqNo = request.SeqNo,
                ExceptionReasonId = request.ExceptionReasonId,
                FilePath = photoPathResult.PhotoPath,
                UploadOpe = request.UploadOpe.Trim()
            };

            await _jetfDbContext.ShipmentInboundExceptions.AddAsync(entity, cancellationToken);
            var affectedRows = await _jetfDbContext.SaveChangesAsync(cancellationToken);

            return affectedRows > 0
                ? ServiceResult.Success("異常件上傳成功")
                : ServiceResult.Fail("INSERT_FAILED", "異常件上傳失敗");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "建立異常件資料失敗，SeqNo: {SeqNo}", request.SeqNo);
            return ServiceResult.Fail(
                "INTERNAL_SERVER_ERROR",
                "異常件上傳時發生未預期錯誤",
                StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 更新單件儲位。
    /// </summary>
    /// <param name="request">儲位調撥請求。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>處理結果。</returns>
    public async Task<ServiceResult> UpdateLocationCodeAsync(
        UpdateLocationCodeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await _jetfDbContext.Database.BeginTransactionAsync(cancellationToken);
            var seqNo = request.SeqNo.Trim();
            var newLocationCode = NormalizeLocationCode(request.LocationCode);
            var editUser = request.EditUser.Trim();

            var matchingInbounds = await _jetfDbContext.ShipmentInbounds
                .AsNoTracking()
                .Where(entity => entity.SeqNo == seqNo && entity.OutboundTime == null)
                .Select(entity => new
                {
                    entity.Id,
                    entity.LocationCode
                })
                .ToListAsync(cancellationToken);

            if (matchingInbounds.Count == 0)
            {
                return ServiceResult.Fail(
                    "SHIPMENT_INBOUND_NOT_FOUND",
                    "查無符合條件的入庫資料");
            }

            if (matchingInbounds.Count > 1)
            {
                return ServiceResult.Fail(
                    "MULTIPLE_SHIPMENT_INBOUND_FOUND",
                    "查到多筆符合條件的入庫資料，請確認流水號");
            }

            var inbound = matchingInbounds[0];
            var oldLocationCode = inbound.LocationCode?.Trim() ?? string.Empty;
            if (string.Equals(oldLocationCode, newLocationCode, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(
                    "LOCATION_CODE_UNCHANGED",
                    "新儲位與原儲位相同，無需更新");
            }

            var updatedRows = await _jetfDbContext.ShipmentInbounds
                .Where(entity => entity.Id == inbound.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(entity => entity.LocationCode, newLocationCode),
                    cancellationToken);

            if (updatedRows <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ServiceResult.Fail("UPDATE_FAILED", "儲位調撥失敗");
            }

            await AddLocationEditHistoryAsync(inbound.Id, oldLocationCode, newLocationCode, editUser, cancellationToken);

            var affectedRows = await _jetfDbContext.SaveChangesAsync(cancellationToken);
            if (affectedRows <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ServiceResult.Fail("UPDATE_FAILED", "儲位調撥失敗");
            }

            await transaction.CommitAsync(cancellationToken);
            return ServiceResult.Success($"儲位調撥成功\n流水號：{seqNo}\n新儲位：{newLocationCode}");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "單件儲位調撥失敗，SeqNo: {SeqNo}", request.SeqNo);
            return ServiceResult.Fail(
                "INTERNAL_SERVER_ERROR",
                "單件儲位調撥時發生未預期錯誤",
                StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 取得整板儲位調撥件數。
    /// </summary>
    /// <param name="request">件數查詢請求。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>處理結果。</returns>
    public async Task<ServiceResult> GetBatchLocationUpdateCountAsync(
        GetBatchLocationUpdateCountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var oldLocationCode = NormalizeLocationCode(request.OldLocationCode);
            var newLocationCode = NormalizeLocationCode(request.NewLocationCode);

            if (string.Equals(oldLocationCode, newLocationCode, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(
                    "SAME_LOCATION_CODE",
                    "原儲位不可與新儲位相同");
            }

            var updateCount = await _jetfDbContext.ShipmentInbounds
                .AsNoTracking()
                .CountAsync(
                    entity => entity.LocationCode == oldLocationCode && entity.OutboundTime == null,
                    cancellationToken);

            if (updateCount == 0)
            {
                return ServiceResult.Fail(
                    "SHIPMENT_INBOUND_NOT_FOUND",
                    "查無符合條件的入庫資料");
            }

            return ServiceResult.Success(
                $"確認要將原儲位 [{oldLocationCode}] 的資料調撥至新儲位 [{newLocationCode}] 嗎？\n更新筆數：[{updateCount}]");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "查詢整板儲位調撥件數失敗，OldLocationCode: {OldLocationCode}, NewLocationCode: {NewLocationCode}",
                request.OldLocationCode,
                request.NewLocationCode);
            return ServiceResult.Fail(
                "INTERNAL_SERVER_ERROR",
                "查詢整板儲位調撥件數時發生未預期錯誤",
                StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 執行整板儲位調撥。
    /// </summary>
    /// <param name="request">整板調撥請求。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>處理結果。</returns>
    public async Task<ServiceResult> BatchUpdateLocationCodeAsync(
        BatchUpdateLocationCodeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await _jetfDbContext.Database.BeginTransactionAsync(cancellationToken);
            var oldLocationCode = NormalizeLocationCode(request.OldLocationCode);
            var newLocationCode = NormalizeLocationCode(request.NewLocationCode);
            var editUser = request.EditUser.Trim();

            if (string.Equals(oldLocationCode, newLocationCode, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(
                    "SAME_LOCATION_CODE",
                    "原儲位不可與新儲位相同");
            }

            var matchingInbounds = await _jetfDbContext.ShipmentInbounds
                .AsNoTracking()
                .Where(entity => entity.LocationCode == oldLocationCode && entity.OutboundTime == null)
                .Select(entity => new
                {
                    entity.Id,
                    entity.LocationCode
                })
                .ToListAsync(cancellationToken);

            if (matchingInbounds.Count == 0)
            {
                return ServiceResult.Fail(
                    "SHIPMENT_INBOUND_NOT_FOUND",
                    "查無符合條件的入庫資料");
            }

            var inboundIds = matchingInbounds.Select(inbound => inbound.Id).ToList();
            var updatedRows = await _jetfDbContext.ShipmentInbounds
                .Where(entity => inboundIds.Contains(entity.Id))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(entity => entity.LocationCode, newLocationCode),
                    cancellationToken);

            if (updatedRows != matchingInbounds.Count)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ServiceResult.Fail("UPDATE_FAILED", "整板儲位調撥失敗");
            }

            foreach (var inbound in matchingInbounds)
            {
                var originalLocationCode = inbound.LocationCode?.Trim() ?? string.Empty;
                await AddLocationEditHistoryAsync(inbound.Id, originalLocationCode, newLocationCode, editUser, cancellationToken);
            }

            var affectedRows = await _jetfDbContext.SaveChangesAsync(cancellationToken);
            if (affectedRows <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ServiceResult.Fail("UPDATE_FAILED", "整板儲位調撥失敗");
            }

            await transaction.CommitAsync(cancellationToken);
            return ServiceResult.Success($"整板儲位調撥成功，已更新 {matchingInbounds.Count} 筆資料");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "整板儲位調撥失敗，OldLocationCode: {OldLocationCode}, NewLocationCode: {NewLocationCode}",
                request.OldLocationCode,
                request.NewLocationCode);
            return ServiceResult.Fail(
                "INTERNAL_SERVER_ERROR",
                "整板儲位調撥時發生未預期錯誤",
                StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 查詢海運原始資料。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>海運原始資料；若不存在則回傳 null。</returns>
    private async Task<SeaOrderOriginalDto?> GetSeaDataAsync(string trackingNo, CancellationToken cancellationToken)
    {
        try
        {
            var seaData = await _dataCenterDbContext.SeaOrderOriginals
                .AsNoTracking()
                .Where(entity => entity.JetfSerial == trackingNo)
                .OrderByDescending(entity => entity.Gw)
                .Select(entity => new
                {
                    TrackingNo = entity.JetfSerial,
                    OriginalJetfSerial = entity.JetfSerial,
                    MainNumber = entity.MainNumber,
                    OriginalTrackingNo = entity.BlNo,
                    ImporterAddr = entity.ImporterAddr,
                    ImporterPhone = entity.ImporterPhone,
                    Importer = entity.Importer,
                    CustCode = entity.CustCode,
                    TransName = entity.TransName,
                    Cc = entity.CC
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (seaData is null)
            {
                return null;
            }

            return new SeaOrderOriginalDto
            {
                TrackingNo = seaData.TrackingNo,
                OriginalJetfSerial = seaData.OriginalJetfSerial,
                MainNumber = seaData.MainNumber,
                OriginalTrackingNo = seaData.OriginalTrackingNo,
                ImporterAddr = seaData.ImporterAddr,
                ImporterPhone = seaData.ImporterPhone,
                Importer = seaData.Importer,
                CustCode = seaData.CustCode,
                TransName = seaData.TransName,
                Cc = ConvertCcToCod(seaData.Cc)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢海運原始資料失敗，TrackingNo: {TrackingNo}", trackingNo);
            throw;
        }
    }

    /// <summary>
    /// 查詢空運原始資料。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>空運原始資料；若不存在則回傳 null。</returns>
    private async Task<AirOrderOriginalDto?> GetAirDataAsync(string trackingNo, CancellationToken cancellationToken)
    {
        try
        {
            var airData = await _dataCenterDbContext.OriginalLists
                .AsNoTracking()
                .Where(entity => entity.TrackingNo == trackingNo || entity.DeliveryNo == trackingNo)
                .Select(entity => new
                {
                    TrackingNo = entity.TrackingNo,
                    OriginalJetfSerial = entity.DeliveryNo,
                    MainNumber = entity.MainNumber,
                    OriginalTrackingNo = entity.TrackingNo,
                    Importer = entity.Importer,
                    ImporterPhone = entity.ImporterPhone,
                    ImporterAddr = entity.ImporterAddr,
                    CustCode = entity.CustCode,
                    TransNo = entity.TransNo,
                    Cc = entity.CC
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (airData is null)
            {
                return null;
            }

            return new AirOrderOriginalDto
            {
                TrackingNo = airData.TrackingNo,
                OriginalJetfSerial = airData.OriginalJetfSerial,
                MainNumber = airData.MainNumber,
                OriginalTrackingNo = airData.OriginalTrackingNo,
                Importer = airData.Importer,
                ImporterPhone = airData.ImporterPhone,
                ImporterAddr = airData.ImporterAddr,
                CustCode = airData.CustCode,
                TransNo = airData.TransNo?.ToString() ?? string.Empty,
                Cc = ConvertCcToCod(airData.Cc)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢空運原始資料失敗，TrackingNo: {TrackingNo}", trackingNo);
            throw;
        }
    }

    /// <summary>
    /// 查詢費用資料。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>費用資料；若不存在則回傳 null。</returns>
    private async Task<FeeMasterDto?> GetFeeDataAsync(string dlvInv, CancellationToken cancellationToken)
    {
        try
        {
            return await _jetfDbContext.FeeMasters
                .AsNoTracking()
                .Where(entity => entity.Download == "1" && entity.IncludeTax == "N" && entity.DlvInv == dlvInv)
                .Select(entity => new FeeMasterDto
                {
                    Tax = (entity.Tax1 ?? 0) + (entity.Tax2 ?? 0),
                    Ccfee = entity.Ccfee ?? 0,
                    Cod = entity.Cod ?? 0,
                    Fee =30,
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢稅金資料失敗，dlvInv: {dlvInv}", dlvInv);
            throw;
        }
    }

    private static int ConvertCcToCod(decimal? value)
    {
        return value.HasValue ? (int)value.Value : 0;
    }

    private static int ConvertCcToCod(double? value)
    {
        return value.HasValue ? (int)value.Value : 0;
    }

    private static int ConvertCcToCod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue)
            || decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out invariantValue)
            ? (int)invariantValue
            : 0;
    }

    /// <summary>
    /// 檢查入庫資料是否重複。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>符合重複條件時回傳 true，否則回傳 false。</returns>
    private async Task<bool> IsDuplicateShipmentInboundAsync(string trackingNo, CancellationToken cancellationToken)
    {
        try
        {
            var duplicateThreshold = DateTime.Now.AddDays(-3);

            return await _jetfDbContext.ShipmentInbounds
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.TrackingNo == trackingNo && (!entity.OutboundDate.HasValue ||entity.OutboundDate < duplicateThreshold ),
                    cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "檢查入庫重複資料失敗，TrackingNo: {TrackingNo}", trackingNo);
            throw;
        }
    }

    /// <summary>
    /// 檢查入庫流水編號是否重複。
    /// </summary>
    /// <param name="seqNo">流水編號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>存在相同流水編號時回傳 true，否則回傳 false。</returns>
    private async Task<bool> IsDuplicateShipmentInboundSeqNoAsync(string seqNo, CancellationToken cancellationToken)
    {
        try
        {
            return await _jetfDbContext.ShipmentInbounds
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.SeqNo == seqNo,
                    cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "檢查入庫流水編號重複失敗，SeqNo: {SeqNo}", seqNo);
            throw;
        }
    }

    private static int? GetExpectedTrackingNoLength(ShipmentInboundSourceType sourceType)
    {
        return sourceType switch
        {
            ShipmentInboundSourceType.Hct => 10,
            ShipmentInboundSourceType.TCat => 12,
            ShipmentInboundSourceType.SevenEleven => 8,
            ShipmentInboundSourceType.Hilife => 18,
            ShipmentInboundSourceType.OK => 11,
            ShipmentInboundSourceType.Family => 11,
            ShipmentInboundSourceType.Yto => 12,
            ShipmentInboundSourceType.Ktj => 11,
            ShipmentInboundSourceType.ShopeeSite => 14,
            _ => null
        };
    }

    private async Task AddLocationEditHistoryAsync(
        int shipmentInboundId,
        string oldLocationCode,
        string newLocationCode,
        string editUser,
        CancellationToken cancellationToken)
    {
        await _jetfDbContext.ShipmentInboundEditHistories.AddAsync(
            new ShipmentInboundEditHistoryEntity
            {
                ShipmentInboundId = shipmentInboundId,
                FieldName = LocationFieldName,
                OldValue = oldLocationCode,
                NewValue = newLocationCode,
                EditTime = DateTime.Now,
                EditUser = editUser
            },
            cancellationToken);
    }

    private static string NormalizeLocationCode(string locationCode)
    {
        return locationCode.Trim().ToUpperInvariant();
    }

    private async Task<PhotoSaveResult> SaveExceptionPhotoAsync(string photoBase64, CancellationToken cancellationToken)
    {
        try
        {
            if (!HasValidPhotoSftpConfiguration())
            {
                _logger.LogError("異常件照片 SFTP 設定不完整");
                return PhotoSaveResult.Fail(
                    "PHOTO_SFTP_NOT_CONFIGURED",
                    "異常件照片上傳設定不完整",
                    StatusCodes.Status500InternalServerError);
            }

            var normalizedPhotoBase64 = NormalizePhotoBase64(photoBase64);
            if (string.IsNullOrWhiteSpace(normalizedPhotoBase64))
            {
                return PhotoSaveResult.Fail("INVALID_PHOTO", "照片為必填");
            }

            byte[] photoBytes;
            try
            {
                photoBytes = Convert.FromBase64String(normalizedPhotoBase64);
            }
            catch (FormatException exception)
            {
                _logger.LogWarning(exception, "異常件照片格式不正確");
                return PhotoSaveResult.Fail("INVALID_PHOTO", "照片格式不正確");
            }

            var now = DateTime.Now;
            var remoteDirectory = BuildExceptionPhotoRemoteDirectory(now);
            var remoteFilePath = $"{remoteDirectory}/{BuildExceptionPhotoFileName(now)}";

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var sftpClient = new SftpClient(
                    _shipmentInboundPhotoSftpOptions.Host,
                    _shipmentInboundPhotoSftpOptions.Port,
                    _shipmentInboundPhotoSftpOptions.Username,
                    _shipmentInboundPhotoSftpOptions.Password);

                sftpClient.Connect();
                EnsureRemoteDirectoryExists(sftpClient, remoteDirectory);

                using var photoStream = new MemoryStream(photoBytes, writable: false);
                sftpClient.UploadFile(photoStream, remoteFilePath);

                if (sftpClient.IsConnected)
                {
                    sftpClient.Disconnect();
                }
            }, cancellationToken);

            return PhotoSaveResult.Success(BuildExceptionPhotoUri(remoteFilePath));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "儲存異常件照片失敗");
            return PhotoSaveResult.Fail(
                "PHOTO_SAVE_FAILED",
                "異常件照片儲存失敗",
                StatusCodes.Status500InternalServerError);
        }
    }

    private static string NormalizePhotoBase64(string photoBase64)
    {
        var trimmedPhoto = photoBase64.Trim();
        var commaIndex = trimmedPhoto.IndexOf(',');
        return commaIndex >= 0 ? trimmedPhoto[(commaIndex + 1)..] : trimmedPhoto;
    }

    private bool HasValidPhotoSftpConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_shipmentInboundPhotoSftpOptions.Host)
            && _shipmentInboundPhotoSftpOptions.Port > 0
            && !string.IsNullOrWhiteSpace(_shipmentInboundPhotoSftpOptions.Username)
            && !string.IsNullOrWhiteSpace(_shipmentInboundPhotoSftpOptions.Password)
            && !string.IsNullOrWhiteSpace(_shipmentInboundPhotoSftpOptions.RootDirectory);
    }

    private string BuildExceptionPhotoRemoteDirectory(DateTime timestamp)
    {
        var normalizedRootDirectory = NormalizeRemotePath(_shipmentInboundPhotoSftpOptions.RootDirectory);
        return $"{normalizedRootDirectory}/{timestamp:yyyyMMdd}";
    }

    private string BuildExceptionPhotoUri(string remoteFilePath)
    {
        return $"sftp://{_shipmentInboundPhotoSftpOptions.Host}:{_shipmentInboundPhotoSftpOptions.Port}{NormalizeRemotePath(remoteFilePath)}";
    }

    private static string BuildExceptionPhotoFileName(DateTime timestamp)
    {
        return $"{timestamp.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}_{Guid.NewGuid():N}.jpg";
    }

    private static string NormalizeRemotePath(string path)
    {
        var normalizedPath = path.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(normalizedPath)
            ? "/"
            : $"/{normalizedPath}";
    }

    private static void EnsureRemoteDirectoryExists(SftpClient sftpClient, string remoteDirectory)
    {
        var normalizedDirectory = NormalizeRemotePath(remoteDirectory);
        var pathSegments = normalizedDirectory
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        var currentPath = string.Empty;
        foreach (var pathSegment in pathSegments)
        {
            currentPath = string.IsNullOrEmpty(currentPath)
                ? $"/{pathSegment}"
                : $"{currentPath}/{pathSegment}";

            if (!sftpClient.Exists(currentPath))
            {
                sftpClient.CreateDirectory(currentPath);
            }
        }
    }

    private sealed class PhotoSaveResult
    {
        public bool IsSuccess { get; init; }

        public string PhotoPath { get; init; } = string.Empty;

        public string ErrorCode { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public int Code { get; init; } = StatusCodes.Status200OK;

        public static PhotoSaveResult Success(string photoPath)
        {
            return new PhotoSaveResult
            {
                IsSuccess = true,
                PhotoPath = photoPath
            };
        }

        public static PhotoSaveResult Fail(
            string errorCode,
            string message,
            int code = StatusCodes.Status400BadRequest)
        {
            return new PhotoSaveResult
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                Message = message,
                Code = code
            };
        }
    }
}
