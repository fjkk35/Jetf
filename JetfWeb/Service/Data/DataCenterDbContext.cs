using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;

namespace Service.Data
{
    public class DataCenterDbContext : DbContext
    {
        static DataCenterDbContext()
        {
            Database.SetInitializer<DataCenterDbContext>(null);
        }

        public DataCenterDbContext()
            : base(BuildConnectionString())
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<AirApprovalGEntity> AirApprovalGs { get; set; }

        public DbSet<AirDetainEntity> AirDetains { get; set; }

        public DbSet<CargoStatusEntity> CargoStatuses { get; set; }

        public DbSet<CargoStatusDetailEntity> CargoStatusDetails { get; set; }

        public DbSet<CesMainOrderEntity> CesMainOrders { get; set; }

        public DbSet<SeaOrderOriginalEntity> SeaOrderOriginals { get; set; }

        public DbSet<SeaOrderEditEntity> SeaOrderEdits { get; set; }

        public DbSet<ClearanceInfoEntity> ClearanceInfos { get; set; }

        public DbSet<ClearanceTaxEntity> ClearanceTaxes { get; set; }

        public DbSet<DespatchFromEntity> DespatchFroms { get; set; }

        public DbSet<EtlCniPreDeclareOrderEntity> EtlCniPreDeclareOrders { get; set; }

        public DbSet<EtlCnoPreDeclareCallbackEntity> EtlCnoPreDeclareCallbacks { get; set; }

        public DbSet<EtlFtzTaxEntity> EtlFtzTaxes { get; set; }

        public DbSet<EtlOrderInfoEntity> EtlOrderInfos { get; set; }

        public DbSet<EtlPlinkErrorEntity> EtlPlinkErrors { get; set; }

        public DbSet<EtlPlinkErrorCodeEntity> EtlPlinkErrorCodes { get; set; }

        public DbSet<EtlPreApprovalEntity> EtlPreApprovals { get; set; }

        public DbSet<EtlTactTaxEntity> EtlTactTaxes { get; set; }

        public DbSet<EtlTipcTaxEntity> EtlTipcTaxes { get; set; }

        public DbSet<MainOrderInfoEntity> MainOrderInfos { get; set; }

        public DbSet<MakeListEntity> MakeLists { get; set; }

        public DbSet<NameCertificationEntity> NameCertifications { get; set; }

        public DbSet<OrderCargoManifestEntity> OrderCargoManifests { get; set; }

        public DbSet<OrderManifestEntity> OrderManifests { get; set; }

        public DbSet<OriginalListEntity> OriginalLists { get; set; }

        public DbSet<SysAirBagEntity> SysAirBags { get; set; }

        public DbSet<SysCustEntity> SysCusts { get; set; }

        public DbSet<SysParamEntity> SysParams { get; set; }

        private static string BuildConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (settings == null)
            {
                throw new InvalidOperationException("找不到 DefaultConnection。");
            }

            var builder = new SqlConnectionStringBuilder(settings.ConnectionString)
            {
                InitialCatalog = "DATA_CENTER"
            };

            return builder.ConnectionString;
        }
    }
}
