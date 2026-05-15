using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using Service.Services;
using Service.Services.Job.CainiaoCheckJob;
using Service.Services.Job.CainiaoNeedJob;
using Service.Services.Job.ComponentJob;
using Service.Services.Job.FtzWebClientJob;
using Service.Services.Job.IncomeJob;
using Service.Services.Job.ShipmentInboundProcessStageTransferJob;
using Service.Services.Job.SjlJob;
using Service.Services.Job.TactWebClientJob;
using System;
using System.Web.Mvc;

[assembly: OwinStartup(typeof(JETFTAX.App_Start.Startup))]
namespace JETFTAX.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();

            GlobalConfiguration.Configuration
                .UseSqlServerStorage("Data Source=192.168.1.4;Initial Catalog=jetf;Persist Security Info=True;User ID=user_c1;Password=a*741jef*;");

#if !DEBUG
            app.UseHangfireDashboard("/hangfire");
            app.UseHangfireServer();

            // 設定排程任務
            ConfigureHangfireJobs();
#endif
        }

        private void ConfigureHangfireJobs()
        {
            // 使用 Autofac 解析 TaxJobService
            var taxJobService = DependencyResolver.Current.GetService<TaxJobService>();
            var cainiaoCheckJobService = DependencyResolver.Current.GetService<CainiaoCheckJobService>();
            var componentJobService = DependencyResolver.Current.GetService<ComponentJobService>();
            var incomeJobService = DependencyResolver.Current.GetService<IncomeJobService>();
            var cainiaoNeedJobService = DependencyResolver.Current.GetService<CainiaoNeedJobService>();
            var tactWebClientJobService = DependencyResolver.Current.GetService<TactWebClientJobService>();
            var ftzWebClientJobService = DependencyResolver.Current.GetService<FtzWebClientJobService>();
            var sjlJobService = DependencyResolver.Current.GetService<SjlJobService>();
            var shipmentInboundProcessStageTransferJobService = DependencyResolver.Current.GetService<ShipmentInboundProcessStageTransferJobService>();
            
            var timeZoneOptions = new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"),
                MisfireHandling = MisfireHandlingMode.Strict
            };

            RecurringJob.AddOrUpdate("捷利海運稅金",
                  () => taxJobService.RunSeaTaxJobAsync(), Cron.Daily(22, 00), timeZoneOptions);

            RecurringJob.AddOrUpdate("捷利空運稅金",
                 () => taxJobService.RunEtlTaxJobAsync(), Cron.Daily(22, 05), timeZoneOptions);

            RecurringJob.AddOrUpdate("菜鳥資料檢查",
                    () => cainiaoCheckJobService.RunCainiaoCheckJobAsync(),
                    "*/10 8-18 * * *", // 每 10 分鐘執行一次 (08:00 ~ 18:50)
                    timeZoneOptions);

            RecurringJob.AddOrUpdate("菜鳥需預委任發送訊息",
                () => cainiaoNeedJobService.RunCainiaoNeedJob(),
                "50 8,11,14,16 * * *",
                timeZoneOptions);

            RecurringJob.AddOrUpdate("酷彭發送訊息",
                  () => componentJobService.RunComponentJobAsync(),
                 "*/10 * * * *",
                  timeZoneOptions);

            RecurringJob.AddOrUpdate("營收轉檔",
                  () => incomeJobService.InsertIncomeReport(), Cron.Daily(08, 30),
                    timeZoneOptions);

            RecurringJob.AddOrUpdate("營收報表",
                () => incomeJobService.RunIncomeJobAsync(),
                "*/10 09-22 * * *", // 每 10 分鐘執行一次 (09:00 ~ 22:00)
                timeZoneOptions);

            RecurringJob.AddOrUpdate("華儲查詢",
                () => tactWebClientJobService.RunTactWebClientJobAsync(),
                 "*/20 * * * *", // 每 20 分鐘執行一次
                timeZoneOptions);

            RecurringJob.AddOrUpdate("遠雄查詢",
                () => ftzWebClientJobService.RunFtzWebClientJobAsync(),
                 "*/20 * * * *", // 每 20 分鐘執行一次
                timeZoneOptions);

            RecurringJob.AddOrUpdate("金祥富稅金資料傳送",
                () => sjlJobService.RunJhfTaxJobAsync(),
                Cron.Daily(22, 10),
                timeZoneOptions);

            RecurringJob.AddOrUpdate("預先登記處理轉檔",
                () => shipmentInboundProcessStageTransferJobService.RunShipmentInboundProcessStageTransferJobAsync(),
                "*/10 8-19 * * *",
                timeZoneOptions);
        }


    }
}
