using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace JETFWebAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            DeleteExpiredLogDirectories();
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        private static void DeleteExpiredLogDirectories()
        {
            const int retentionDays = 30;
            string logDirectory = Path.Combine(HttpRuntime.AppDomainAppPath, "Logs");
            DateTime cutoffDate = DateTime.Today.AddDays(-retentionDays);

            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    return;
                }

                foreach (string directoryPath in Directory.GetDirectories(logDirectory))
                {
                    string directoryName = Path.GetFileName(directoryPath);
                    DateTime directoryDate;

                    if (DateTime.TryParseExact(
                        directoryName,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out directoryDate) && directoryDate < cutoffDate)
                    {
                        try
                        {
                            Directory.Delete(directoryPath, true);
                        }
                        catch (IOException)
                        {
                            // Keep folders that are still in use and retry next startup.
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Keep folders that the application cannot delete.
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Log cleanup must not prevent the application from starting.
            }
            catch (UnauthorizedAccessException)
            {
                // Log cleanup must not prevent the application from starting.
            }
        }
    }
}
