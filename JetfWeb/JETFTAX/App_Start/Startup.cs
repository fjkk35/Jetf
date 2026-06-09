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
using Service.Services.Job.SeaShenzhenHctJob;
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
            var timeZoneOptions = new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"),
                MisfireHandling = MisfireHandlingMode.Strict
            };

            RecurringJob.AddOrUpdate<TaxJobService>("捷利海運稅金",
                  service => service.RunSeaTaxJobAsync(), Cron.Daily(22, 00), timeZoneOptions);

            RecurringJob.AddOrUpdate<TaxJobService>("捷利空運稅金",
                 service => service.RunEtlTaxJobAsync(), Cron.Daily(22, 05), timeZoneOptions);

            RecurringJob.AddOrUpdate<CainiaoCheckJobService>("菜鳥資料檢查",
                    service => service.RunCainiaoCheckJobAsync(),
                    "*/10 8-18 * * *", // 每 10 分鐘執行一次 (08:00 ~ 18:50)
                    timeZoneOptions);

            RecurringJob.AddOrUpdate<CainiaoNeedJobService>("菜鳥需預委任發送訊息",
                service => service.RunCainiaoNeedJob(),
                "50 8,11,14,16 * * *",
                timeZoneOptions);

            RecurringJob.AddOrUpdate<ComponentJobService>("酷彭發送訊息",
                  service => service.RunComponentJobAsync(),
                 "*/10 * * * *",
                  timeZoneOptions);

            RecurringJob.AddOrUpdate<IncomeJobService>("營收轉檔",
                  service => service.InsertIncomeReport(), Cron.Daily(08, 30),
                    timeZoneOptions);

            RecurringJob.AddOrUpdate<IncomeJobService>("營收報表",
                service => service.RunIncomeJobAsync(),
                "*/10 09-22 * * *", // 每 10 分鐘執行一次 (09:00 ~ 22:00)
                timeZoneOptions);

            RecurringJob.AddOrUpdate<TactWebClientJobService>("華儲查詢",
                service => service.RunTactWebClientJobAsync(),
                 "*/20 * * * *", // 每 20 分鐘執行一次
                timeZoneOptions);

            RecurringJob.AddOrUpdate<FtzWebClientJobService>("遠雄查詢",
                service => service.RunFtzWebClientJobAsync(),
                 "*/20 * * * *", // 每 20 分鐘執行一次
                timeZoneOptions);

            RecurringJob.AddOrUpdate<SjlJobService>("金祥富稅金資料傳送",
                service => service.RunJhfTaxJobAsync(),
                Cron.Daily(22, 10),
                timeZoneOptions);

            //RecurringJob.AddOrUpdate<SeaShenzhenHctJobService>("新遞深圳 HCT 託運傳送",
            //    service => service.RunSeaShenzhenHctJobAsync(),
            //    "*/10 * * * *",
            //    timeZoneOptions);

            RecurringJob.AddOrUpdate<ShipmentInboundProcessStageTransferJobService>("預先登記處理轉檔",
                service => service.RunShipmentInboundProcessStageTransferJobAsync(),
                "*/10 8-19 * * *",
                timeZoneOptions);
        }


    }
}
