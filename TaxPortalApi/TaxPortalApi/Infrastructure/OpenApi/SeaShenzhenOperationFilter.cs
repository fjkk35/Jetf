using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaxPortalApi.Controllers;

namespace TaxPortalApi.Infrastructure.OpenApi;

/// <summary>
/// 新遞端點使用 Query 簽名驗證，在文件中取消繼承全域 JWT 要求。
/// </summary>
public sealed class SeaShenzhenOperationFilter : IOperationFilter
{
    /// <summary>
    /// 設定新遞 API 的文件驗證需求，取消繼承全域 Bearer Token 設定。
    /// </summary>
    /// <param name="operation">目前產生文件的 API 操作。</param>
    /// <param name="context">API 方法與控制器資訊。</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType == typeof(SeaShenzhenController))
        {
            operation.Security = [];
        }
    }
}
