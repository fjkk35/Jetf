using System.Data.Entity;

namespace Service.Data
{
    public class JetfDbContext : DbContext
    {
        static JetfDbContext()
        {
            Database.SetInitializer<JetfDbContext>(null);
        }

        public JetfDbContext()
            : base("name=DefaultConnection")
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<ShipmentInboundEntity> ShipmentInbounds { get; set; }

        public DbSet<SeaClearanceDetailEntity> SeaClearanceDetails { get; set; }

        public DbSet<SeaClearanceDetailOriginalMappingEntity> SeaClearanceDetailOriginalMappings { get; set; }

        public DbSet<SeaClearanceFeeEntity> SeaClearanceFees { get; set; }

        public DbSet<CustomsBrokerEntity> CustomsBrokers { get; set; }

        public DbSet<CustomsBrokerageEntity> CustomsBrokerages { get; set; }

        public DbSet<AbnormalStateEntity> AbnormalStates { get; set; }

        public DbSet<ShipmentInboundExceptionEntity> ShipmentInboundExceptions { get; set; }

        public DbSet<ShipmentInboundEditHistoryEntity> ShipmentInboundEditHistories { get; set; }

        public DbSet<ShipmentInboundLocationHistoryEntity> ShipmentInboundLocationHistories { get; set; }

        public DbSet<FeeMasterEntity> FeeMasters { get; set; }

        public DbSet<CustomerMasterEntity> CustomerMasters { get; set; }
    }
}
