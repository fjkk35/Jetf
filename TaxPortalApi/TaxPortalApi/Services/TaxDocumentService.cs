using FluentFTP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaxPortalApi.Infrastructure.Exceptions;
using TaxPortalApi.Infrastructure.Options;
using TaxPortalApi.Infrastructure.Persistence;
using TaxPortalApi.Models.TaxDocuments;
using TaxPortalApi.Services.Interfaces;

namespace TaxPortalApi.Services;

public sealed class TaxDocumentService(
    JetfDbContext jetfDbContext,
    DataCenterDbContext dataCenterDbContext,
    IOptions<TaxDocumentFtpOptions> ftpOptions) : ITaxDocumentService
{
    private static readonly HashSet<string> SpecialDataTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FTZ",
        "TACT"
    };

    private readonly TaxDocumentFtpOptions _ftpOptions = ftpOptions.Value;

    public async Task<TaxDocumentFileResult> GetTaxDocumentAsync(long userId, TaxDocumentQueryRequest request, CancellationToken cancellationToken = default)
    {
        var taxNumber = request.TaxNumber.Trim();
        var clearanceTax = await dataCenterDbContext.ClearanceTaxes
            .AsNoTracking()
            .Where(item => item.TaxNumber == taxNumber)
            .Select(item => new
            {
                item.DataType,
                item.MergeNumber
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (clearanceTax is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "找不到對應的稅單資料", "TAXDOC_001");
        }

        var mergeNumber = clearanceTax.MergeNumber?.Trim();
        if (string.IsNullOrWhiteSpace(mergeNumber))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "找不到對應的客戶資料", "TAXDOC_002");
        }

        var custCode = await ResolveCustCodeAsync(clearanceTax.DataType, mergeNumber, cancellationToken);
        if (string.IsNullOrWhiteSpace(custCode))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "找不到對應的客戶資料", "TAXDOC_002");
        }

        var normalizedUserId = ConvertToCustomerUserId(userId);
        var customerMatched = await jetfDbContext.TaxPortalCustomers
            .AsNoTracking()
            .AnyAsync(item => item.TaxPortalUserId == normalizedUserId && item.CustCode == custCode, cancellationToken);

        if (!customerMatched)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "查無可存取的稅金單資料", "TAXDOC_003");
        }

        var pdfInfo = await jetfDbContext.ClearanceTaxPdfs
            .AsNoTracking()
            .Where(item => item.TaxNumber == taxNumber)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.FilePath,
                item.FileName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (pdfInfo is null || string.IsNullOrWhiteSpace(pdfInfo.FilePath))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "找不到稅金單 PDF 路徑", "TAXDOC_004");
        }

        var remotePath = NormalizeRemotePath(pdfInfo.FilePath);
        await using var ftpClient = new AsyncFtpClient(_ftpOptions.Host, _ftpOptions.UserName, _ftpOptions.Password);
        await ftpClient.AutoConnect(cancellationToken);

        var fileExists = await ftpClient.FileExists(remotePath, cancellationToken);
        if (!fileExists)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "FTP 上找不到對應的稅金單 PDF", "TAXDOC_005");
        }

        var fileContent = await ftpClient.DownloadBytes(remotePath, token: cancellationToken);
        if (fileContent.Length == 0)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "FTP 上找不到對應的稅金單 PDF", "TAXDOC_005");
        }

        return new TaxDocumentFileResult
        {
            Content = fileContent,
            FileName = ResolveFileName(pdfInfo.FileName, remotePath)
        };
    }

    private async Task<string?> ResolveCustCodeAsync(string? dataType, string mergeNumber, CancellationToken cancellationToken)
    {
        if (SpecialDataTypes.Contains(dataType ?? string.Empty))
        {
            return await dataCenterDbContext.OriginalLists
                .AsNoTracking()
                .Where(item => item.DeliveryNo == mergeNumber)
                .Select(item => item.CustCode)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await dataCenterDbContext.SeaOrderOriginals
            .AsNoTracking()
            .Where(item => item.JetfSerial == mergeNumber)
            .Select(item => item.CustCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static int ConvertToCustomerUserId(long userId)
    {
        if (userId > int.MaxValue || userId < int.MinValue)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "目前使用者無法查詢稅金單資料", "TAXDOC_006");
        }

        return (int)userId;
    }

    private static string NormalizeRemotePath(string remotePath)
    {
        var normalizedPath = remotePath.Replace('\\', '/').Trim();
        return normalizedPath.StartsWith('/') ? normalizedPath : $"/{normalizedPath}";
    }

    private static string ResolveFileName(string? fileName, string remotePath)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return fileName;
        }

        var lastSlashIndex = remotePath.LastIndexOf('/');
        return lastSlashIndex >= 0 && lastSlashIndex < remotePath.Length - 1
            ? remotePath[(lastSlashIndex + 1)..]
            : "tax-document.pdf";
    }
}