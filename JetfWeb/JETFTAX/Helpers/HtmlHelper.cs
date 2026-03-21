using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace JETFTAX
{
    public static class AssetVersioning
    {
        public static MvcHtmlString Script(this HtmlHelper html, string src)
        {
            return MvcHtmlString.Create($@"<script src=""{GetPathWithHash(src)}""></script>");
        }

        public static MvcHtmlString Css(this HtmlHelper html, string href)
        {
            return MvcHtmlString.Create($@"<link href=""{GetPathWithHash(href)}"" rel=""stylesheet"" />");
        }

        public static string GetPathWithHash(string path)
        {
            return $"{VirtualPathUtility.ToAbsolute(path)}?v={GetFileHash(path)}";
        }

        private static MemoryCache _cache = MemoryCache.Default;
        public static string GetFileHash(string path)
        {
            try
            {
                var physicalPath = HostingEnvironment.MapPath(path);
                if (!File.Exists(physicalPath))
                {
                    return string.Empty;
                }

                var cacheKey = $"__asset_hash__{path}";
                if (_cache.Contains(cacheKey))
                {
                    return _cache[cacheKey] as string;
                }

                using (var sha256 = SHA256.Create())
                {
                    var hash = HttpServerUtility.UrlTokenEncode(
                        sha256.ComputeHash(File.ReadAllBytes(physicalPath)));

                    // CacheItemPolicy: 設定快取的回收機制
                    // HostFileChangeMonitor: 偵測到檔案或資料夾異動時移除快取
                    // 設定快取監聽檔案或資料夾異動時移除快取
                    var policy = new CacheItemPolicy();
                    policy.ChangeMonitors.Add(new HostFileChangeMonitor(new string[] { physicalPath }));
                    _cache.Add(cacheKey, hash, policy);
                    return hash;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}