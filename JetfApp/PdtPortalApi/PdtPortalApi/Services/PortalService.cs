using System.Globalization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using PdtPortalApi.Data;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Entities;
using PdtPortalApi.Models.Enums;
using PdtPortalApi.Models.Requests;

namespace PdtPortalApi.Services;

public sealed class PortalService(
    JetfDbContext jetfDbContext,
    DataCenterDbContext dataCenterDbContext,
    ILogger<PortalService> logger) : IPortalService
{
    private const string ExceptionPhotoDirectory = @"F:\UploadPdt";
    private readonly JetfDbContext _jetfDbContext = jetfDbContext;
    private readonly DataCenterDbContext _dataCenterDbContext = dataCenterDbContext;
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

            var feeData = await GetFeeDataAsync(request.TrackingNo, cancellationToken);
            var hasOriginalData = seaData is not null || airData is not null;
            var dataType = seaData is not null ? "海運" : airData is not null ? "空運" : string.Empty;
            var size = string.IsNullOrWhiteSpace(request.Size) ? "小" : request.Size.Trim();
 
 			var entity = new ShipmentInboundEntity
 			{
 				DataType = dataType,
                InboundDate = request.InboundDate.LocalDateTime,
				TrackingNo = request.TrackingNo,
				SeqNo = request.SeqNo,
 				LocationCode = request.LocationCode,
 				SourceType = request.SourceType,
 				ReturnTrackingNo = request.ReturnTrackingNo ?? string.Empty,
                Size = size,
 				CustCode = seaData?.CustCode ?? airData?.CustCode ?? string.Empty,
 				TransNo = airData?.TransNo ?? string.Empty,
 				TransName = seaData?.TransName ?? string.Empty,
				Importer = seaData?.Importer ?? airData?.Importer ?? string.Empty,
				ImporterPhone = seaData?.ImporterPhone ?? airData?.ImporterPhone ?? string.Empty,
				ImporterAddr = seaData?.ImporterAddr ?? airData?.ImporterAddr ?? string.Empty,
				IsOrderOriginal = hasOriginalData,
                UploadOpe = request.UploadOpe ?? string.Empty,
				CreatedTime = DateTime.Now,
				Tax = feeData?.Tax ?? 0,
				Ccfee = feeData?.Ccfee ?? 0,
				Cod = feeData?.Cod ?? 0,
				Fee = feeData?.Fee ?? 0,
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

            var entity = new ShipmentInboundExceptionEntity
            {
                ShipmentInboundId = matchingInbounds[0].Id,
                SeqNo = request.SeqNo,
                Reason = request.Reason.Trim(),
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
    /// 查詢海運原始資料。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>海運原始資料；若不存在則回傳 null。</returns>
    private async Task<SeaOrderOriginalDto?> GetSeaDataAsync(string trackingNo, CancellationToken cancellationToken)
    {
        try
        {
            return await _dataCenterDbContext.SeaOrderOriginals
                .AsNoTracking()
                .Where(entity => entity.JetfSerial == trackingNo)
                .OrderByDescending(entity => entity.Gw)
                .Select(entity => new SeaOrderOriginalDto
                {
                    TrackingNo = entity.JetfSerial,
                    ImporterAddr = entity.ImporterAddr,
                    ImporterPhone = entity.ImporterPhone,
                    Importer = entity.Importer,
                    CustCode = entity.CustCode,
                    TransName = entity.TransName
                })
                .FirstOrDefaultAsync(cancellationToken);
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
            return await _dataCenterDbContext.OriginalLists
                .AsNoTracking()
                .Where(entity => entity.TrackingNo == trackingNo)
                .Select(entity => new AirOrderOriginalDto
                {
                    TrackingNo = entity.TrackingNo,
                    Importer = entity.Importer,
                    ImporterPhone = entity.ImporterPhone,
                    ImporterAddr = entity.ImporterAddr,
                    CustCode = entity.CustCode,
                    TransNo = entity.TransNo
                })
                .FirstOrDefaultAsync(cancellationToken);
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
    private async Task<FeeMasterDto?> GetFeeDataAsync(string trackingNo, CancellationToken cancellationToken)
    {
        try
        {
            return await _jetfDbContext.FeeMasters
                .AsNoTracking()
                .Where(entity => entity.Download == "1" && entity.IncludeTax == "N" && entity.TrackingNo == trackingNo)
                .Select(entity => new FeeMasterDto
                {
                    TrackingNo = entity.TrackingNo,
                    Tax = (entity.Tax1 ?? 0) + (entity.Tax2 ?? 0),
                    Ccfee = entity.Ccfee ?? 0,
                    Cod = entity.Cod ?? 0,
                    Fee =30,
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢費用資料失敗，TrackingNo: {TrackingNo}", trackingNo);
            throw;
        }
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

    private async Task<PhotoSaveResult> SaveExceptionPhotoAsync(string photoBase64, CancellationToken cancellationToken)
    {
        try
        {
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

            Directory.CreateDirectory(ExceptionPhotoDirectory);

            string filePath;
            do
            {
                var fileName = DateTime.Now.ToString("yyyyMMddhhmmssfff", CultureInfo.InvariantCulture);
                filePath = Path.Combine(ExceptionPhotoDirectory, $"{fileName}.jpg");
                if (!File.Exists(filePath))
                {
                    break;
                }

                await Task.Delay(1, cancellationToken);
            } while (true);

            await File.WriteAllBytesAsync(filePath, photoBytes, cancellationToken);
            return PhotoSaveResult.Success(filePath);
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
