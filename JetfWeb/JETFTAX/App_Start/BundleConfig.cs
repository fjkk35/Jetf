using System.Web;
using System.Web.Optimization;

namespace JETFTAX
{
    public class BundleConfig
    {
        // 如需統合的詳細資訊，請瀏覽 https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // 使用開發版本的 Modernizr 進行開發並學習。然後，當您
            // 準備好可進行生產時，請使用 https://modernizr.com 的建置工具，只挑選您需要的測試。
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            // Angular.js bundle
            bundles.Add(new ScriptBundle("~/bundles/angular").Include(
                        "~/Scripts/lib/angular/angular.min.js",
                        "~/Scripts/lib/angular/ui-bootstrap-tpls.min.js",
                        "~/Scripts/lib/angular/angular-locale_zh-cn.js",
                        "~/Scripts/lib/moment/moment.min.js",
                        "~/Scripts/lib/moment/locale/zh-tw.js",
                        "~/Scripts/common/angular-filters.js",
                        "~/Scripts/_Layout.js"
                        )
                        // controllers
                        .IncludeDirectory(
                            "~/Scripts/ng-controllers",
                            "*.js",
                            true
                        )
                        // directives
                        .IncludeDirectory(
                            "~/Scripts/directives",
                            "*.js",
                            true
                        )
                );

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));
        }
    }
}
