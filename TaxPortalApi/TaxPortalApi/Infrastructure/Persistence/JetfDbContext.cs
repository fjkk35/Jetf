using Microsoft.EntityFrameworkCore;
using TaxPortalApi.Models.Auth;
using TaxPortalApi.Models.SeaShenzhen.Entities;
using TaxPortalApi.Models.TaxDocuments.Entities;

namespace TaxPortalApi.Infrastructure.Persistence;

public class JetfDbContext(DbContextOptions<JetfDbContext> options) : DbContext(options)
{
    public DbSet<TaxPortalUser> TaxPortalUsers => Set<TaxPortalUser>();

    public DbSet<TaxPortalCustomer> TaxPortalCustomers => Set<TaxPortalCustomer>();

    public DbSet<ClearanceTaxPdf> ClearanceTaxPdfs => Set<ClearanceTaxPdf>();

    public DbSet<SeaShenzhenOriginal> SeaShenzhenOriginals => Set<SeaShenzhenOriginal>();

    public DbSet<ShenzhenFeeMaster> ShenzhenFeeMasters => Set<ShenzhenFeeMaster>();

    public DbSet<CargoSignReceipt> CargoSignReceipts => Set<CargoSignReceipt>();
}
