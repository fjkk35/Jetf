using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.Mvc;
using Hangfire;
using JETFTAX.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Service.Data;
using Service.Services;
using Service.Services.Job.CainiaoCheckJob;
using Service.Services.Job.CainiaoNeedJob;
using Service.Services.Job.ComponentJob;
using Service.Services.Job.FeeMasterCodJob;
using Service.Services.Job.FtzWebClientJob;
using Service.Services.Job.IncomeJob;
using Service.Services.Job.SeaShenzhenHctJob;
using Service.Services.Job.ShipmentInboundProcessStageTransferJob;
using Service.Services.Job.SjlJob;
using Service.Services.Job.TactWebClientJob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;
using TelegramLibrary;

namespace JETFTAX
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected void Application_Start()
        {
            CleanupExpiredLogFolders();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //調整Json太長序列化錯誤
            var jsonValueProviderFactory = ValueProviderFactories.Factories
                .OfType<JsonValueProviderFactory>()
                .FirstOrDefault();

            if (jsonValueProviderFactory != null)
            {
                ValueProviderFactories.Factories.Remove(jsonValueProviderFactory);
            }

            ValueProviderFactories.Factories.Add(new CustomJsonValueProviderFactory());

            var services = new ServiceCollection();
            ConfigureServices(services);
            var resolver = new DotnetCoreDIDependencyResolver(services.BuildServiceProvider());
            DependencyResolver.SetResolver(resolver);

            // 创建Autofac容器构建器
            var builder = new ContainerBuilder();
            builder.Populate(services);
            
            // 自動註冊所有服務
            RegisterAllServices(builder);

            // 構建容器
            var container = builder.Build();
            // 設定MVC的依賴解析器
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

            // 設定 Hangfire 使用 Autofac
            GlobalConfiguration.Configuration.UseAutofacActivator(container);
        }

        private void RegisterAllServices(ContainerBuilder builder)
        {
            var requestScopeTag = MatchingScopeLifetimeTags.RequestLifetimeScopeTag;
            var backgroundJobScopeTag = AutofacJobActivator.LifetimeScopeTag;

            builder.RegisterType<JetfDbContext>()
                   .AsSelf()
                   .InstancePerMatchingLifetimeScope(requestScopeTag, backgroundJobScopeTag);

            builder.RegisterType<DataCenterDbContext>()
                   .AsSelf()
                   .InstancePerMatchingLifetimeScope(requestScopeTag, backgroundJobScopeTag);

            builder.Register(context => new SqlConnection(
                       ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                   .AsSelf()
                   .InstancePerMatchingLifetimeScope(requestScopeTag, backgroundJobScopeTag);

            // 取得當前應用程式域的所有組件
            var serviceAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Service");

            if (serviceAssembly != null)
            {
                // 自動註冊所有以 "Service" 結尾的類別（排除 Job 相關服務）
                builder.RegisterAssemblyTypes(serviceAssembly)
                       .Where(t => t.Name.EndsWith("Service") && 
                                  t.IsClass && 
                                  !t.IsAbstract &&
                                  t.Namespace != null &&
                                  t.Namespace.StartsWith("Service.Services") &&
                                  !t.Namespace.Contains(".Job") && // 排除 Job 服務
                                  t.Name != "TaxJobService") // 排除 TaxJobService
                       .AsSelf()
                       .InstancePerRequest();

                // 自動註冊 API 類別
                builder.RegisterAssemblyTypes(serviceAssembly)
                       .Where(t => t.Name.EndsWith("Api") && 
                                  t.IsClass && 
                                  !t.IsAbstract)
                       .AsSelf()
                       .InstancePerRequest();
            }

            // 排程作業 - 保持原有註冊方式
            builder.RegisterType(typeof(TaxJobService)).As<TaxJobService>().InstancePerLifetimeScope();
            builder.RegisterType(typeof(TelegramBot)).As<TelegramBot>().InstancePerLifetimeScope();
            builder.RegisterType<CainiaoCheckJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<ComponentJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<FeeMasterCodJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<IncomeJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<CainiaoNeedJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<TactWebClientJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<FtzWebClientJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<SjlJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<SeaShenzhenHctJobService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<ShipmentInboundProcessStageTransferJobService>().AsSelf().InstancePerLifetimeScope();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            var controlles = typeof(MvcApplication).Assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract)
                .Where(t => typeof(IController).IsAssignableFrom(t))
                .Where(t => t.Name.EndsWith("Controller"));

            foreach (var ctrl in controlles)
            {
                services.AddTransient(ctrl);
            }
            services.AddHttpClient();
        }

        protected void Application_AcquireRequestState()
        {
            SetUserIdLogContext();
        }

        protected void Application_EndRequest()
        {
            MappedDiagnosticsLogicalContext.Remove("userId");
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            if (exception == null)
            {
                return;
            }

            SetUserIdLogContext();

            var request = Context?.Request;
            var method = request?.HttpMethod ?? "UNKNOWN";
            var path = request?.Url?.AbsolutePath ?? request?.RawUrl ?? string.Empty;

            Logger.Error(exception, $"Unhandled_Exception[Error] - [{method}] {path}");
        }

        private void SetUserIdLogContext()
        {
            MappedDiagnosticsLogicalContext.Set("userId", ResolveCurrentUserId());
        }

        private string ResolveCurrentUserId()
        {
            try
            {
                var hasSessionState = Context?.Handler is IRequiresSessionState || Context?.Handler is IReadOnlySessionState;
                var userId = hasSessionState ? Session?["user_id"]?.ToString() : null;
                return string.IsNullOrWhiteSpace(userId) ? "Unknown" : userId.Trim();
            }
            catch
            {
                return "Unknown";
            }
        }

        private static void CleanupExpiredLogFolders()
        {
            var logRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            if (!Directory.Exists(logRootPath))
            {
                return;
            }

            var expiredDate = DateTime.Today.AddDays(-7);
            foreach (var directory in Directory.GetDirectories(logRootPath))
            {
                var directoryName = Path.GetFileName(directory);
                if (!DateTime.TryParseExact(directoryName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var logDate))
                {
                    continue;
                }

                if (logDate >= expiredDate)
                {
                    continue;
                }

                Directory.Delete(directory, true);
            }
        }
    }

    internal class DotnetCoreDIDependencyResolver : IDependencyResolver
    {
        private IServiceProvider serviceProvider;

        public DotnetCoreDIDependencyResolver(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return this.serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.serviceProvider.GetServices(serviceType);
        }
    }
}
