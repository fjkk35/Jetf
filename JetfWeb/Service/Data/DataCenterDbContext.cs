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

        public DbSet<SeaOrderOriginalEntity> SeaOrderOriginals { get; set; }

        public DbSet<OriginalListEntity> OriginalLists { get; set; }

        public DbSet<SysCustEntity> SysCusts { get; set; }

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
