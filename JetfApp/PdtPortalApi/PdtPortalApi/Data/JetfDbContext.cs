using Microsoft.EntityFrameworkCore;
using PdtPortalApi.Models.Entities;

namespace PdtPortalApi.Data;

public sealed class JetfDbContext(DbContextOptions<JetfDbContext> options) : DbContext(options)
{
	public DbSet<UserMasterEntity> UserMasters => Set<UserMasterEntity>();

	public DbSet<ShipmentInboundSourceTypeEntity> ShipmentInboundSourceTypes => Set<ShipmentInboundSourceTypeEntity>();

	public DbSet<ShipmentInboundEntity> ShipmentInbounds => Set<ShipmentInboundEntity>();

    public DbSet<ShipmentInboundExceptionEntity> ShipmentInboundExceptions => Set<ShipmentInboundExceptionEntity>();

    public DbSet<ShipmentInboundExceptionReasonEntity> ShipmentInboundExceptionReasons => Set<ShipmentInboundExceptionReasonEntity>();

    public DbSet<ShipmentInboundEditHistoryEntity> ShipmentInboundEditHistories => Set<ShipmentInboundEditHistoryEntity>();

	public DbSet<FeeMasterEntity> FeeMasters => Set<FeeMasterEntity>();
}
