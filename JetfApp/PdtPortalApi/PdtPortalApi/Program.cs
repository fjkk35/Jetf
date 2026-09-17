using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Config;
using NLog.Web;
using PdtPortalApi.Data;
using PdtPortalApi.Filters;
using PdtPortalApi.Models.Responses;
using PdtPortalApi.Options;
using PdtPortalApi.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var logDirectory = ResolveLogDirectory(builder.Configuration["FileLogging:Path"]);
Directory.CreateDirectory(logDirectory);

var nlogConfigPath = Path.Combine(builder.Environment.ContentRootPath, "NLog.config");
var nlogConfiguration = new XmlLoggingConfiguration(nlogConfigPath);
nlogConfiguration.Variables["logDirectory"] = logDirectory;
LogManager.Configuration = nlogConfiguration;

builder.Logging.ClearProviders();
builder.Host.UseNLog();
var startupLogger = LogManager.GetCurrentClassLogger();

builder.Services
    .AddControllers(options => options.Filters.AddService<ApiRequestTraceFilter>(int.MinValue))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message));

            var message = string.Join("；", errors);
            return new BadRequestObjectResult(
                ApiResponse.Fail(
                    "VALIDATION_ERROR",
                    string.IsNullOrWhiteSpace(message) ? "請求資料格式不正確" : message,
                    StatusCodes.Status400BadRequest));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddDbContext<JetfDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<DataCenterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DataCenterConnection")));
builder.Services.AddMemoryCache();

builder.Services.Configure<AppVersionOptions>(builder.Configuration.GetSection(AppVersionOptions.SectionName));
builder.Services.Configure<HmacOptions>(builder.Configuration.GetSection(HmacOptions.SectionName));
builder.Services.Configure<ShipmentInboundPhotoSftpOptions>(builder.Configuration.GetSection(ShipmentInboundPhotoSftpOptions.SectionName));
builder.Services.AddSingleton<IAppVersionService, AppVersionService>();
builder.Services.AddSingleton<IHmacSignatureService, HmacSignatureService>();
builder.Services.AddSingleton<IApiAuditLogger, ApiAuditLogger>();
builder.Services.AddScoped<ApiRequestTraceFilter>();
builder.Services.AddScoped<IPortalService, PortalService>();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error is not null)
        {
            startupLogger.Error(exceptionFeature.Error, "Unhandled exception occurred while processing request {Path}", context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            ApiResponse.Fail(
                "INTERNAL_SERVER_ERROR",
                "系統發生未預期錯誤",
                StatusCodes.Status500InternalServerError));
    });
});

app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
});

app.MapScalarApiReference("/scalar", options =>
{
    options.WithTitle("Pdt Portal API");
    options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    startupLogger.Info("Starting Pdt Portal API");
    app.Run();
}
catch (Exception exception)
{
    startupLogger.Fatal(exception, "Pdt Portal API terminated unexpectedly");
}
finally
{
    LogManager.Shutdown();
}

static string ResolveLogDirectory(string? configuredPath)
{
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.Combine(AppContext.BaseDirectory, "logs");
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
}
