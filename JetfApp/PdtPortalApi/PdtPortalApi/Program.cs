using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdtPortalApi.Data;
using PdtPortalApi.Models.Responses;
using PdtPortalApi.Options;
using PdtPortalApi.Services;
using Serilog;
using Serilog.Events;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    var minimumLevel = ResolveLogLevel(context.Configuration["Logging:LogLevel:Default"], LogEventLevel.Information);
    var microsoftLevel = ResolveLogLevel(context.Configuration["Logging:LogLevel:Microsoft.AspNetCore"], LogEventLevel.Warning);
    var logDirectory = ResolveLogDirectory(context.Configuration["FileLogging:Path"]);
    Directory.CreateDirectory(logDirectory);

    configuration
        .MinimumLevel.Is(minimumLevel)
        .MinimumLevel.Override("Microsoft", microsoftLevel)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(logDirectory, "log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            shared: true,
            encoding: Encoding.UTF8);
});

builder.Services
    .AddControllers()
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

builder.Services.Configure<AppVersionOptions>(builder.Configuration.GetSection(AppVersionOptions.SectionName));
builder.Services.Configure<HmacOptions>(builder.Configuration.GetSection(HmacOptions.SectionName));
builder.Services.AddSingleton<IAppVersionService, AppVersionService>();
builder.Services.AddSingleton<IHmacSignatureService, HmacSignatureService>();
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
            Log.Error(exceptionFeature.Error, "Unhandled exception occurred while processing request {Path}", context.Request.Path);
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
    Log.Information("Starting Pdt Portal API");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Pdt Portal API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static LogEventLevel ResolveLogLevel(string? configuredLevel, LogEventLevel fallbackLevel)
{
    return Enum.TryParse<LogEventLevel>(configuredLevel, ignoreCase: true, out var level)
        ? level
        : fallbackLevel;
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
