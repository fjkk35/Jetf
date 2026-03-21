using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace Service.Helpers
{
    public static class CacheHelper
    {
        private static ObjectCache Cache => MemoryCache.Default;

        public static bool Exist(string key)
        {
            return Cache.Get(key) != null;
        }

        public static T Get<T>(string key)
        {
            return Get(key, () => default(T));
        }

        public static T Get<T>(string key, Func<T> refresh)
        {
            return Get<T, string>(key, arg => refresh(), null);
        }

        public static T Get<T, TArg>(string key, Func<TArg, T> refresh, TArg arg)
        {
            return Exist(key)
                ? (T)Cache[key]
                : refresh(arg);
        }

        public static void Remove(string key)
        {
            Cache.Remove(key);
        }

        public static T Set<T>(string key, T cachedObject, params string[] dependencyFiles)
        {
            Cache.Set(new CacheItem(key, cachedObject), CreateDependency(dependencyFiles));

            return cachedObject;
        }

        /// <summary>
        ///     Set cache object
        /// </summary>
        /// <param name="key"> The cache key</param>
        /// <param name="cachedObject">The cache object</param>
        /// <param name="policy">
        ///     An object that contains eviction details for the cache entry. This object provides more options for eviction than a simple absolute expiration.
        /// </param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Set<T>(string key, T cachedObject, CacheItemPolicy policy)
        {
            Cache.Set(new CacheItem(key, cachedObject), policy);

            return cachedObject;
        }

        private static CacheItemPolicy CreateDependency(params string[] fileList)
        {
            return AddChangeMonitor(new CacheItemPolicy
            {
                RemovedCallback = arguments => Remove(arguments.CacheItem.Key)
            }, fileList);
        }

        private static CacheItemPolicy AddChangeMonitor(CacheItemPolicy policy, params string[] fileList)
        {
            var files = (fileList ?? Enumerable.Empty<string>()).ToArray();
            if (files.Any())
            {
                policy.ChangeMonitors.Add(new HostFileChangeMonitor(files));
            }
            return policy;
        }

        /// <summary>
        ///     Get cache or add type of object to cache and return object
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="func">if the cache not exists, invoke func to get the value.</param>
        /// <param name="isRewrite">The default value is false. If true, overwrite the cache after executing func to get the value.</param>
        /// <typeparam name="T">The generic type</typeparam>
        /// <returns>Type of object</returns>
        /// <exception cref="ArgumentNullException">When any parameter of key, func, policy is null, ArgumentNullException will be thrown.</exception>
        public static T GetOrAdd<T>(string key, Func<T> func, bool isRewrite = false)
        {
            return GetOrAdd(key, func, new CacheItemPolicy() { Priority = CacheItemPriority.NotRemovable }, isRewrite);
        }

        /// <summary>
        ///     Get cache or add type of object to cache and return object
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="func">if the cache not exists, invoke func to get the value.</param>
        /// <param name="policy">The cache item policy</param>
        /// <param name="isRewrite">The default value is false. If true, overwrite the cache after executing func to get the value.</param>
        /// <typeparam name="T">The generic type</typeparam>
        /// <returns>Type of object</returns>
        /// <exception cref="ArgumentNullException">When any parameter of key, func, policy is null, ArgumentNullException will be thrown.</exception>
        public static T GetOrAdd<T>(string key, Func<T> func, CacheItemPolicy policy, bool isRewrite = false)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (func == null)
                throw new ArgumentNullException(nameof(func));

            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            lock (GetCacheObject(key))
            {
                if (!isRewrite && IsSameType<T>(key))
                    return Get<T>(key);
                var value = func();
                Cache.Set(key, value, policy);
            }
            return Get<T>(key);
        }

        private static bool IsSameType<T>(string key)
        {
            return Get<object>(key).GetType() == typeof(T);
        }

        private static object GetCacheObject(string key)
        {
            lock (Cache)
            {
                if (!Cache.Contains(key))
                    Cache.Add(key, NullObject.Instance, new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 10, 0) });
            }
            return Cache[key];
        }

        internal class NullObject
        {
            public static readonly NullObject Instance = new NullObject();
        }
    }
}
