using Microsoft.EntityFrameworkCore;
using TaxPortalApi.Models.TaxDocuments.Entities;

namespace TaxPortalApi.Infrastructure.Persistence;

public class DataCenterDbContext(DbContextOptions<DataCenterDbContext> options) : DbContext(options)
{
    public DbSet<ClearanceTax> ClearanceTaxes => Set<ClearanceTax>();

    public DbSet<OriginalList> OriginalLists => Set<OriginalList>();

    public DbSet<SeaOrderOriginal> SeaOrderOriginals => Set<SeaOrderOriginal>();
}