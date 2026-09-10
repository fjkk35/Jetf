using Microsoft.EntityFrameworkCore;
using PdtPortalApi.Models.Entities;

namespace PdtPortalApi.Data;

public sealed class DataCenterDbContext(DbContextOptions<DataCenterDbContext> options) : DbContext(options)
{
	public DbSet<SeaOrderOriginalEntity> SeaOrderOriginals => Set<SeaOrderOriginalEntity>();

	public DbSet<SeaOrderEditEntity> SeaOrderEdits => Set<SeaOrderEditEntity>();

	public DbSet<OriginalListEntity> OriginalLists => Set<OriginalListEntity>();

	public DbSet<MakeListEntity> MakeLists => Set<MakeListEntity>();
}
