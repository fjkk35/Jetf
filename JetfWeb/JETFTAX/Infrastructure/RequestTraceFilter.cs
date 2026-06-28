using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace JETFTAX.Infrastructure
{
    /// <summary>
    /// 記錄每個 MVC action 的 request 開始與結束時間。
    /// </summary>
    public sealed class RequestTraceFilter : ActionFilterAttribute
    {
        private static readonly Logger Logger = LogManager.GetLogger("RequestTrace");
        private const string UnknownUserId = "Unknown";
        private const string RequestTraceParamsKey = "RequestTraceFilter.Params";
        private const string RequestTraceStartTimeKey = "RequestTraceFilter.StartTimeUtc";

        /// <summary>
        /// Action 執行前紀錄 request begin。
        /// </summary>
        /// <param name="filterContext">目前 action context。</param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            CacheRequestParameters(filterContext);
            CacheRequestStartTime(filterContext);
            LogRequest(filterContext, "Request_Begin");
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Action 執行後紀錄 request end。
        /// </summary>
        /// <param name="filterContext">目前 action context。</param>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            LogRequest(filterContext, "Request_End");
            base.OnActionExecuted(filterContext);
        }

        private static void LogRequest(ControllerContext filterContext, string stage)
        {
            if (filterContext == null || filterContext.IsChildAction)
            {
                return;
            }

            RefreshUserIdLogContext(filterContext);

            var request = filterContext.HttpContext?.Request;
            if (request == null)
            {
                return;
            }

            var method = request.HttpMethod ?? "UNKNOWN";
            var path = request.Url?.AbsolutePath ?? request.RawUrl ?? string.Empty;
            if (stage == "Request_Begin")
            {
                var parameters = GetCachedParameters(filterContext);
                Logger.Debug($"{stage}[Debug] - [{method}] {path}{parameters}");
                return;
            }

            var costText = GetCostText(filterContext);
            Logger.Debug($"{stage}[Debug] - [{method}] {path}{costText}");
        }

        private static void RefreshUserIdLogContext(ControllerContext filterContext)
        {
            MappedDiagnosticsLogicalContext.Set("userId", ResolveCurrentUserId(filterContext));
        }

        private static string ResolveCurrentUserId(ControllerContext filterContext)
        {
            try
            {
                var userId = filterContext?.HttpContext?.Session?["user_id"]?.ToString();
                return string.IsNullOrWhiteSpace(userId) ? UnknownUserId : userId.Trim();
            }
            catch
            {
                return UnknownUserId;
            }
        }

        private static void CacheRequestParameters(ActionExecutingContext filterContext)
        {
            if (filterContext?.HttpContext == null)
            {
                return;
            }

            filterContext.HttpContext.Items[RequestTraceParamsKey] = BuildParameterText(filterContext.ActionParameters);
        }

        private static void CacheRequestStartTime(ActionExecutingContext filterContext)
        {
            if (filterContext?.HttpContext == null)
            {
                return;
            }

            filterContext.HttpContext.Items[RequestTraceStartTimeKey] = DateTime.UtcNow;
        }

        private static string GetCachedParameters(ControllerContext filterContext)
        {
            var parameters = filterContext?.HttpContext?.Items[RequestTraceParamsKey] as string;
            return parameters ?? string.Empty;
        }

        private static string GetCostText(ControllerContext filterContext)
        {
            if (filterContext?.HttpContext?.Items[RequestTraceStartTimeKey] is DateTime startTimeUtc)
            {
                var elapsed = DateTime.UtcNow - startTimeUtc;
                return $" | cost: {elapsed.TotalMilliseconds:0}ms";
            }

            return string.Empty;
        }

        private static string BuildParameterText(IDictionary<string, object> actionParameters)
        {
            if (actionParameters == null || actionParameters.Count == 0)
            {
                return string.Empty;
            }

            var payload = actionParameters
                .Where(parameter => parameter.Value != null)
                .ToDictionary(parameter => parameter.Key, parameter => SanitizeValue(parameter.Value, 0));

            if (payload.Count == 0)
            {
                return string.Empty;
            }

            try
            {
                return $" | Params={CreateSerializer().Serialize(payload)}";
            }
            catch (Exception ex)
            {
                return $" | Params=<serialize failed: {ex.GetType().Name}: {ex.Message}>";
            }
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            return new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 100
            };
        }

        private static object SanitizeValue(object value, int depth)
        {
            if (value == null)
            {
                return null;
            }

            if (depth >= 3)
            {
                return value.ToString();
            }

            var type = value.GetType();

            if (IsSimpleType(type))
            {
                return value;
            }

            if (value is System.Web.HttpPostedFileBase postedFile)
            {
                return new
                {
                    postedFile.FileName,
                    postedFile.ContentLength,
                    postedFile.ContentType
                };
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                return enumerable.Cast<object>()
                    .Select(item => SanitizeValue(item, depth + 1))
                    .ToList();
            }

            return type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .ToDictionary(
                    property => property.Name,
                    property =>
                    {
                        object propertyValue;
                        try
                        {
                            propertyValue = property.GetValue(value);
                        }
                        catch
                        {
                            propertyValue = null;
                        }

                        return SanitizeValue(propertyValue, depth + 1);
                    });
        }

        private static bool IsSimpleType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;

            return actualType.IsPrimitive
                || actualType.IsEnum
                || actualType == typeof(string)
                || actualType == typeof(decimal)
                || actualType == typeof(DateTime)
                || actualType == typeof(DateTimeOffset)
                || actualType == typeof(TimeSpan)
                || actualType == typeof(Guid)
                || actualType == typeof(Uri)
                || actualType == typeof(byte[])
                || actualType == typeof(char)
                || actualType == typeof(bool)
                || actualType == typeof(short)
                || actualType == typeof(int)
                || actualType == typeof(long)
                || actualType == typeof(float)
                || actualType == typeof(double)
                || actualType == typeof(uint)
                || actualType == typeof(ulong)
                || actualType == typeof(ushort)
                || actualType == typeof(sbyte)
                || actualType == typeof(byte)
                || actualType == typeof(IntPtr)
                || actualType == typeof(UIntPtr)
                || typeof(IFormattable).IsAssignableFrom(actualType)
                || typeof(IConvertible).IsAssignableFrom(actualType);
        }
    }
}
