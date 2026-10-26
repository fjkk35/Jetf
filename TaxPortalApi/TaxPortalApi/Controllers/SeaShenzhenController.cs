using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxPortalApi.Models.Common;
using TaxPortalApi.Models.SeaShenzhen;
using TaxPortalApi.Services.Interfaces;

namespace TaxPortalApi.Controllers;

/// <summary>
/// 新遞
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SeaShenzhenController(
    ISeaShenzhenService seaShenzhenService) : ControllerBase
{
    /// <summary>
    /// 查詢金額。
    /// </summary>
    /// <remarks>
    /// 使用 HMAC-SHA256 驗證，不需 JWT。簽章原文為 trackinngNo={trackinngNo}&amp;timestamp={timestamp}。
    /// Timestamp 使用 UTC Unix 秒數，只接受最近 180 秒。
    /// 金額依新遞託運資料查詢頁面規則，TaxPayment 回傳原始值。
    /// </remarks>
    /// <param name="request">物流貨號與簽名參數。</param>
    /// <returns>物流貨號、到付款、稅金、稅金手續費及支付方式。</returns>
    [HttpGet("get-amount")]
    [ProducesResponseType(typeof(ApiResponse<SeaShenzhenAmountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<SeaShenzhenAmountResponse>>> GetAmount(
        [FromQuery] SeaShenzhenQueryRequest request)
    {
        seaShenzhenService.ValidateSignature(request);
        var result = await seaShenzhenService.GetAmountAsync(request.TrackinngNo);
        return Ok(ApiResponse<SeaShenzhenAmountResponse>.Ok(result));
    }

    /// <summary>
    /// 查詢簽收單。
    /// </summary>
    /// <remarks>
    /// 使用 HMAC-SHA256 驗證，不需 JWT。簽章規則與金額查詢相同。
    /// 先確認 SeaShenzhenOriginal 存在該物流貨號，才查詢 Cargo_Sign_Receipt 並讀取圖片。
    /// 同一物流貨號的多張簽收單以 images 清單回傳，內容為不含 data URI 前綴的 Base64。
    /// </remarks>
    /// <param name="request">物流貨號與簽名參數。</param>
    /// <returns>簽收圖片清單。</returns>
    [HttpGet("get-sign-receipts")]
    [ProducesResponseType(typeof(ApiResponse<SeaShenzhenSignReceiptResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<SeaShenzhenSignReceiptResponse>>> GetSignReceipts(
        [FromQuery] SeaShenzhenQueryRequest request)
    {
        seaShenzhenService.ValidateSignature(request);
        var result = await seaShenzhenService.GetSignReceiptsAsync(request.TrackinngNo);
        return Ok(ApiResponse<SeaShenzhenSignReceiptResponse>.Ok(result));
    }
}
