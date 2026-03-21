using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TaxPortalApi.Infrastructure.OpenApi;
using TaxPortalApi.Infrastructure.Options;
using TaxPortalApi.Infrastructure.Persistence;
using TaxPortalApi.Middleware;
using TaxPortalApi.Models.Common;
using TaxPortalApi.Services;
using TaxPortalApi.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection 尚未設定，請檢查 appsettings 設定檔或環境變數。");
}

var dataCenterConnectionString = builder.Configuration.GetConnectionString("DataCenterConnection") ?? string.Empty;
if (string.IsNullOrWhiteSpace(dataCenterConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DataCenterConnection 尚未設定，請檢查 appsettings 設定檔或環境變數。");
}

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
var controllerSummaryMap = ControllerXmlCommentsProvider.LoadControllerSummaries(xmlPath);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join("；", context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .Where(errorMessage => !string.IsNullOrWhiteSpace(errorMessage)));

        var response = ApiResponse<object?>.Fail(
            string.IsNullOrWhiteSpace(message) ? "請求資料驗證失敗" : message,
            StatusCodes.Status400BadRequest);

        return new BadRequestObjectResult(response);
    };
});
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .ValidateDataAnnotations()
    .Validate(options => options.Key.Length >= 32, "Jwt:Key 長度至少需要 32 個字元")
    .ValidateOnStart();
builder.Services
    .AddOptions<TaxDocumentFtpOptions>()
    .Bind(builder.Configuration.GetSection(TaxDocumentFtpOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddDbContext<JetfDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<DataCenterDbContext>(options => options.UseSqlServer(dataCenterConnectionString));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ITaxDocumentService, TaxDocumentService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(ApiResponse<object?>.Fail(
                    "未授權，請提供有效的 Bearer Token",
                    StatusCodes.Status401Unauthorized));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(ApiResponse<object?>.Fail(
                    "您沒有存取此資源的權限",
                    StatusCodes.Status403Forbidden));
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var bearerSecurityScheme = new OpenApiSecurityScheme
    {
        Description = "請輸入 JWT Token。格式：Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaxPortal API",
        Version = "v1",
        Description = "TaxPortal API OpenAPI 文件。"
    });

    options.CustomOperationIds(apiDescription =>
    {
        if (apiDescription.ActionDescriptor is ControllerActionDescriptor controllerAction)
        {
            return $"{controllerAction.ControllerName}-{controllerAction.ActionName}";
        }

        return apiDescription.RelativePath;
    });

    options.TagActionsBy(apiDescription =>
    {
        if (apiDescription.ActionDescriptor is ControllerActionDescriptor controllerAction)
        {
            var controllerTypeName = controllerAction.ControllerTypeInfo.FullName;
            if (!string.IsNullOrWhiteSpace(controllerTypeName)
                && controllerSummaryMap.TryGetValue(controllerTypeName, out var controllerDisplayName))
            {
                return [controllerDisplayName];
            }

            return [controllerAction.ControllerName];
        }

        return [apiDescription.GroupName ?? "Default"];
    });

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: false);
    }

    options.AddSecurityDefinition("Bearer", bearerSecurityScheme);

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", null, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.UseForwardedHeaders();

app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
    options.PreSerializeFilters.Add((swaggerDocument, httpRequest) =>
    {
        var pathBase = httpRequest.PathBase.HasValue ? httpRequest.PathBase.Value : string.Empty;
        var serverUrl = $"{httpRequest.Scheme}://{httpRequest.Host}{pathBase}";

        swaggerDocument.Servers = new List<OpenApiServer>
        {
            new()
            {
                Url = serverUrl,
                Description = "目前 API 主機"
            }
        };
    });
});

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("稅務入口 API 文件")
        .ForceDarkMode()
        .WithOpenApiRoutePattern("/openapi/{documentName}.json");

    options.HideDarkModeToggle = true;
});

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/scalar")).AllowAnonymous();
app.MapControllers();

app.Run();
