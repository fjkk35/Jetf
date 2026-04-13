using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Models.Responses;
using PdtPortalApi.Options;
using PdtPortalApi.Services;

namespace PdtPortalApi.Controllers;

/// <summary>
/// App 版本相關 API。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AppController(
    IAppVersionService appVersionService,
    IOptions<AppVersionOptions> appVersionOptions,
    IWebHostEnvironment webHostEnvironment,
    ILogger<AppController> logger) : ControllerBase
{
    private readonly IAppVersionService _appVersionService = appVersionService;
    private readonly AppVersionOptions _appVersionOptions = appVersionOptions.Value;
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
    private readonly ILogger<AppController> _logger = logger;

    /// <summary>
    /// 檢查 App 版本是否需要強制更新。
    /// </summary>
    /// <param name="request">版本檢查請求。</param>
    /// <returns>版本檢查結果。</returns>
    [HttpGet("version-check")]
    [ProducesResponseType(typeof(ApiResponse<AppVersionCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppVersionCheckResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AppVersionCheckResponse>), StatusCodes.Status500InternalServerError)]
    public ActionResult<ApiResponse<AppVersionCheckResponse>> VersionCheck([FromQuery] AppVersionCheckRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.VersionCode))
            {
                return BadRequest(ApiResponse<AppVersionCheckResponse>.Fail("VALIDATION_ERROR", "versionCode 為必填"));
            }

            var result = _appVersionService.GetVersionCheckResult(request.VersionCode);
            result.ApkUrl = GetDownloadApkUrl();
            return Ok(ApiResponse<AppVersionCheckResponse>.Ok(result));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "App 版本檢查 API 執行失敗，VersionCode: {VersionCode}", request.VersionCode);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<AppVersionCheckResponse>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "App 版本檢查時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// 下載最新 APK。
    /// </summary>
    /// <returns>APK 檔案串流。</returns>
    [HttpGet("download-apk")]
    [Produces("application/vnd.android.package-archive")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public IActionResult DownloadApk()
    {
        try
        {
            var apkFilePath = ResolveApkFilePath();
            if (string.IsNullOrWhiteSpace(apkFilePath) || !System.IO.File.Exists(apkFilePath))
            {
                _logger.LogWarning("APK file not found. Configured path: {ConfiguredPath}, resolved path: {ResolvedPath}", _appVersionOptions.ApkFilePath, apkFilePath);
                return NotFound(ApiResponse.Fail("APK_NOT_FOUND", "找不到 APK 檔案", StatusCodes.Status404NotFound));
            }

            var downloadFileName = string.IsNullOrWhiteSpace(_appVersionOptions.ApkFileName)
                ? Path.GetFileName(apkFilePath)
                : _appVersionOptions.ApkFileName;

            return PhysicalFile(apkFilePath, "application/vnd.android.package-archive", downloadFileName, enableRangeProcessing: true);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "下載 APK API 執行失敗");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "下載 APK 時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }

    private string GetDownloadApkUrl()
    {
        if (!string.IsNullOrWhiteSpace(_appVersionOptions.ApkFilePath))
        {
            var apkFilePath = ResolveApkFilePath();
            if (System.IO.File.Exists(apkFilePath))
            {
                return Url.ActionLink(nameof(DownloadApk), values: null) ?? string.Empty;
            }

            _logger.LogWarning("APK URL was requested but file does not exist. Configured path: {ConfiguredPath}, resolved path: {ResolvedPath}", _appVersionOptions.ApkFilePath, apkFilePath);
            return string.Empty;
        }

        return _appVersionOptions.ApkUrl;
    }

    private string ResolveApkFilePath()
    {
        if (string.IsNullOrWhiteSpace(_appVersionOptions.ApkFilePath))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(_appVersionOptions.ApkFilePath))
        {
            return _appVersionOptions.ApkFilePath;
        }

        return Path.GetFullPath(Path.Combine(_webHostEnvironment.ContentRootPath, _appVersionOptions.ApkFilePath));
    }
}