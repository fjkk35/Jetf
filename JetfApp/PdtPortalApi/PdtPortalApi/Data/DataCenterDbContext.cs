using Microsoft.EntityFrameworkCore;
using PdtPortalApi.Models.Entities;

namespace PdtPortalApi.Data;

public sealed class DataCenterDbContext(DbContextOptions<DataCenterDbContext> options) : DbContext(options)
{
	public DbSet<SeaOrderOriginalEntity> SeaOrderOriginals => Set<SeaOrderOriginalEntity>();

	public DbSet<OriginalListEntity> OriginalLists => Set<OriginalListEntity>();
}