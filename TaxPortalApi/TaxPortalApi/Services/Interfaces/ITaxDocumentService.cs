using TaxPortalApi.Models.TaxDocuments;

namespace TaxPortalApi.Services.Interfaces;

public interface ITaxDocumentService
{
    Task<TaxDocumentFileResult> GetTaxDocumentAsync(long userId, TaxDocumentQueryRequest request, CancellationToken cancellationToken = default);
}