using Microsoft.EntityFrameworkCore;
using PdtPortalApi.Data;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Entities;
using PdtPortalApi.Models.Requests;

namespace PdtPortalApi.Services;

public sealed class PortalService(
    JetfDbContext jetfDbContext,
    DataCenterDbContext dataCenterDbContext,
    ILogger<PortalService> logger) : IPortalService
{
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
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>存在時回傳 true，否則回傳 false。</returns>
    public async Task<bool> CheckInboundDataAsync(string trackingNo, CancellationToken cancellationToken)
    {
        try
        {
            var seaOrderExists = await _dataCenterDbContext.SeaOrderOriginals
                .AsNoTracking()
                .AnyAsync(entity => entity.JetfSerial == trackingNo, cancellationToken);

            if (seaOrderExists)
            {
                return true;
            }

            return await _dataCenterDbContext.OriginalLists
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.TrackingNo == trackingNo || entity.DeliveryNo == trackingNo,
                    cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "檢查原始入庫資料失敗，TrackingNo: {TrackingNo}", trackingNo);
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

			var entity = new ShipmentInboundEntity
			{
				DataType = dataType,
                InboundDate = request.InboundDate.LocalDateTime,
				TrackingNo = request.TrackingNo,
				SeqNo = request.SeqNo,
				LocationCode = request.LocationCode,
				SourceType = request.SourceType,
				ReturnTrackingNo = request.ReturnTrackingNo ?? string.Empty,
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
}
