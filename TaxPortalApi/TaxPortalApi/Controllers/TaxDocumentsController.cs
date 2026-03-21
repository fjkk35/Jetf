using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxPortalApi.Extensions;
using TaxPortalApi.Models.Common;
using TaxPortalApi.Models.TaxDocuments;
using TaxPortalApi.Services.Interfaces;

namespace TaxPortalApi.Controllers;

/// <summary>
/// 稅金功能。
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TaxDocumentsController(ITaxDocumentService taxDocumentService) : ControllerBase
{
    /// <summary>
    /// 取得稅金單 PDF。
    /// </summary>
    /// <param name="request">稅金單查詢請求資料。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>成功時回傳包含 PDF 檔案內容的 API 回應。</returns>
    [HttpGet("get-tax-number-pdf")]
    [Produces("application/json")]
    public async Task<ActionResult<ApiResponse<TaxDocumentFileResponse>>> GetTaxNumberPdf([FromQuery] TaxDocumentQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await taxDocumentService.GetTaxDocumentAsync(userId, request, cancellationToken);

        var response = ApiResponse<TaxDocumentFileResponse>.Ok(new TaxDocumentFileResponse
        {
            FileName = result.FileName,
            ContentType = result.ContentType,
            ContentBase64 = Convert.ToBase64String(result.Content)
        });

        return Ok(response);
    }
}