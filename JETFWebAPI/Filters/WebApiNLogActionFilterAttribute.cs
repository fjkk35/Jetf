using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using NLog;
using Newtonsoft.Json;

namespace JETFWebAPI.Filters
{
    /// <summary>
    /// WebAPI NLog Action 日誌記錄過濾器 - 為每個 UserId 建立獨立日誌檔案
    /// 優化版：只對有 [WebApiLog] 標記的方法進行記錄
    /// </summary>
    public class WebApiNLogActionFilterAttribute : ActionFilterAttribute
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private DateTime _startTime;
        private string _requestBody;
        private WebApiLogAttribute _logAttribute;

        /// <summary>
        /// Action 執行前記錄
        /// </summary>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            _startTime = DateTime.Now;

            try
            {
                // 檢查是否有 WebApiLog Attribute
                _logAttribute = GetLogAttribute(actionContext);
                if (_logAttribute == null)
                {
                    // 沒有標記，不記錄日誌
                    base.OnActionExecuting(actionContext);
                    return;
                }

                // 取得使用者相關資訊
                var userId = GetUserId(actionContext);
                var userIp = GetClientIpAddress(actionContext);

                MappedDiagnosticsLogicalContext.Set("userId", userId);
                MappedDiagnosticsLogicalContext.Set("userIp", userIp);

                // 記錄請求內容 (根據 Attribute 設定)
                if (_logAttribute.LogRequestBody)
                {
                    _requestBody = GetSmartRequestBody(actionContext.Request, _logAttribute.MaxContentLength);
                }

                var controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
                var actionName = actionContext.ActionDescriptor.ActionName;

                var logData = new
                {
                    Type = "Request",
                    Controller = controllerName,
                    Action = actionName,
                    Description = _logAttribute.Description,
                    UserId = userId,
                    UserIP = userIp,
                    Method = actionContext.Request.Method.Method,
                    Url = GetSimplifiedUrl(actionContext.Request.RequestUri),
                    Headers = GetImportantHeaders(actionContext.Request),
                    Parameters = GetFilteredActionParameters(actionContext),
                    RequestBody = _requestBody,
                    StartTime = _startTime
                };

                Logger.Info($"REQUEST | {controllerName}.{actionName} | {JsonConvert.SerializeObject(logData, Formatting.None)}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "記錄 WebAPI 請求日誌時發生錯誤");
            }

            base.OnActionExecuting(actionContext);
        }

        /// <summary>
        /// Action 執行後記錄
        /// </summary>
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            try
            {
                // 如果沒有 log attribute，直接返回
                if (_logAttribute == null)
                {
                    base.OnActionExecuted(actionExecutedContext);
                    return;
                }

                var endTime = DateTime.Now;
                var duration = (endTime - _startTime).TotalMilliseconds;

                var controllerName = actionExecutedContext.ActionContext.ControllerContext.ControllerDescriptor.ControllerName;
                var actionName = actionExecutedContext.ActionContext.ActionDescriptor.ActionName;
                var userId = MappedDiagnosticsLogicalContext.Get("userId");

                // 智能取得回應內容 (根據 Attribute 設定)
                object responseContent = null;
                if (_logAttribute.LogResponseBody)
                {
                    responseContent = GetSmartResponseContent(actionExecutedContext.Response, _logAttribute.MaxContentLength);
                }

                var statusCode = actionExecutedContext.Response?.StatusCode.ToString() ?? "Unknown";

                var logData = new
                {
                    Type = "Response",
                    Controller = controllerName,
                    Action = actionName,
                    Description = _logAttribute.Description,
                    UserId = userId,
                    StatusCode = statusCode,
                    Duration = $"{duration:F2}ms",
                    Response = responseContent,
                    EndTime = endTime,
                    HasException = actionExecutedContext.Exception != null,
                    ExceptionMessage = actionExecutedContext.Exception?.Message
                };

                if (actionExecutedContext.Exception != null)
                {
                    Logger.Error(actionExecutedContext.Exception, $"RESPONSE | {controllerName}.{actionName} | {JsonConvert.SerializeObject(logData, Formatting.None)}");
                }
                else
                {
                    Logger.Info($"RESPONSE | {controllerName}.{actionName} | {JsonConvert.SerializeObject(logData, Formatting.None)}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "記錄 WebAPI 回應日誌時發生錯誤");
            }
            finally
            {
                // 清理 MDC (只有在有設定時才清理)
                if (_logAttribute != null)
                {
                    MappedDiagnosticsLogicalContext.Remove("userId");
                    MappedDiagnosticsLogicalContext.Remove("userIp");
                }
            }

            base.OnActionExecuted(actionExecutedContext);
        }

        /// <summary>
        /// 取得 WebApiLog Attribute
        /// </summary>
        private WebApiLogAttribute GetLogAttribute(HttpActionContext actionContext)
        {
            // 先檢查 Action 層級的 Attribute
            var actionAttribute = actionContext.ActionDescriptor.GetCustomAttributes<WebApiLogAttribute>(true).FirstOrDefault();
            if (actionAttribute != null)
                return actionAttribute;

            // 再檢查 Controller 層級的 Attribute
            var controllerAttribute = actionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<WebApiLogAttribute>(true).FirstOrDefault();
            return controllerAttribute;
        }

        /// <summary>
        /// 取得使用者ID
        /// </summary>
        private string GetUserId(HttpActionContext actionContext)
        {
            try
            {
                // 嘗試從 HTTP 上下文取得 Session
                if (HttpContext.Current?.Session != null)
                {
                    var sessionUserId = HttpContext.Current.Session["user_id"]?.ToString();
                    if (!string.IsNullOrEmpty(sessionUserId))
                        return sessionUserId;
                }

                // 嘗試從 Header 取得使用者資訊
                var request = actionContext.Request;
                if (request.Headers.Contains("UserId"))
                {
                    return request.Headers.GetValues("UserId").FirstOrDefault() ?? "Unknown";
                }

                // 嘗試從 Token 解析使用者ID
                if (request.Headers.Contains("Token"))
                {
                    var token = request.Headers.GetValues("Token").FirstOrDefault();
                    return $"TokenUser_{token?.Substring(0, Math.Min(8, token?.Length ?? 0))}";
                }

                return "API_User";
            }
            catch
            {
                return "API_User";
            }
        }

        /// <summary>
        /// 取得客戶端IP位址
        /// </summary>
        private string GetClientIpAddress(HttpActionContext actionContext)
        {
            try
            {
                if (HttpContext.Current?.Request != null)
                {
                    var request = HttpContext.Current.Request;
                    var userHostAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    
                    if (string.IsNullOrEmpty(userHostAddress))
                        userHostAddress = request.ServerVariables["REMOTE_ADDR"];

                    if (string.IsNullOrEmpty(userHostAddress))
                        userHostAddress = request.UserHostAddress;

                    return userHostAddress ?? "Unknown";
                }

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 取得簡化的 URL
        /// </summary>
        private string GetSimplifiedUrl(Uri requestUri)
        {
            if (requestUri == null) return "Unknown";

            try
            {
                var baseUrl = $"{requestUri.Scheme}://{requestUri.Host}:{requestUri.Port}{requestUri.AbsolutePath}";
                
                if (!string.IsNullOrEmpty(requestUri.Query))
                {
                    var queryCount = requestUri.Query.Split('&').Length;
                    baseUrl += $"?{queryCount} parameters";
                }
                
                return baseUrl;
            }
            catch
            {
                return requestUri.ToString();
            }
        }

        /// <summary>
        /// 取得重要的請求標頭
        /// </summary>
        private Dictionary<string, string> GetImportantHeaders(HttpRequestMessage request)
        {
            try
            {
                var headers = new Dictionary<string, string>();
                var importantHeaders = new[] { "Content-Type", "Accept", "Authorization", "User-Agent" };
                
                foreach (var headerName in importantHeaders)
                {
                    if (request.Headers.Contains(headerName))
                    {
                        var value = string.Join(", ", request.Headers.GetValues(headerName));
                        if (headerName == "Authorization")
                            value = value.Length > 20 ? value.Substring(0, 20) + "***" : "***";
                        headers[headerName] = value;
                    }
                    else if (request.Content?.Headers != null && request.Content.Headers.Contains(headerName))
                    {
                        headers[headerName] = string.Join(", ", request.Content.Headers.GetValues(headerName));
                    }
                }

                return headers;
            }
            catch
            {
                return new Dictionary<string, string> { { "Error", "無法取得標頭資訊" } };
            }
        }

        /// <summary>
        /// 取得過濾後的 Action 參數
        /// </summary>
        private Dictionary<string, object> GetFilteredActionParameters(HttpActionContext actionContext)
        {
            try
            {
                var parameters = new Dictionary<string, object>();
                
                foreach (var param in actionContext.ActionArguments)
                {
                    if (param.Value != null)
                    {
                        var paramName = param.Key.ToLower();
                        if (paramName.Contains("password") || paramName.Contains("token") || paramName.Contains("secret"))
                        {
                            parameters[param.Key] = "***隱藏***";
                        }
                        else
                        {
                            parameters[param.Key] = GetParameterSummary(param.Value, _logAttribute.MaxContentLength);
                        }
                    }
                    else
                    {
                        parameters[param.Key] = null;
                    }
                }

                return parameters;
            }
            catch
            {
                return new Dictionary<string, object> { { "Error", "無法取得參數資訊" } };
            }
        }

        /// <summary>
        /// 取得參數摘要
        /// </summary>
        private object GetParameterSummary(object parameter, int maxLength)
        {
            if (parameter == null) return null;

            try
            {
                var paramType = parameter.GetType();
                
                if (paramType.IsPrimitive || parameter is string || parameter is DateTime)
                    return parameter;

                var json = JsonConvert.SerializeObject(parameter, Formatting.None);
                
                if (json.Length > maxLength)
                {
                    return new { 
                        Type = paramType.Name, 
                        Size = $"{json.Length} chars",
                        Summary = json.Substring(0, 200) + "...(截斷)"
                    };
                }
                
                return JsonConvert.DeserializeObject(json);
            }
            catch
            {
                return $"無法序列化的 {parameter.GetType().Name} 物件";
            }
        }

        /// <summary>
        /// 智能取得請求內容
        /// </summary>
        private string GetSmartRequestBody(HttpRequestMessage request, int maxLength)
        {
            try
            {
                if (request.Content != null)
                {
                    var contentTask = request.Content.ReadAsStringAsync();
                    contentTask.Wait(TimeSpan.FromSeconds(5));
                    var content = contentTask.Result;
                    
                    if (content.Length > maxLength)
                    {
                        return content.Substring(0, maxLength) + $"...(截斷，原始長度: {content.Length})";
                    }
                    
                    // 過濾敏感資訊
                    if (content.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        content = System.Text.RegularExpressions.Regex.Replace(
                            content, 
                            @"""password"":\s*""[^""]*""", 
                            "\"password\":\"***\"", 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }
                    
                    return content;
                }
            }
            catch (Exception ex)
            {
                return $"讀取請求內容失敗: {ex.Message}";
            }

            return string.Empty;
        }

        /// <summary>
        /// 智能取得回應內容
        /// </summary>
        private object GetSmartResponseContent(HttpResponseMessage response, int maxLength)
        {
            try
            {
                if (response?.Content != null)
                {
                    var contentTask = response.Content.ReadAsStringAsync();
                    contentTask.Wait(TimeSpan.FromSeconds(5));
                    var content = contentTask.Result;

                    if (content.Length > maxLength)
                    {
                        try
                        {
                            var jsonObj = JsonConvert.DeserializeObject(content);
                            return new { 
                                Type = "JSON", 
                                Size = $"{content.Length} chars",
                                Summary = content.Substring(0, 200) + "...(截斷)"
                            };
                        }
                        catch
                        {
                            return new { 
                                Type = "Text", 
                                Size = $"{content.Length} chars",
                                Summary = content.Substring(0, 200) + "...(截斷)"
                            };
                        }
                    }

                    try
                    {
                        return JsonConvert.DeserializeObject(content);
                    }
                    catch
                    {
                        return content;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return $"讀取回應內容失敗: {ex.Message}";
            }
        }
    }
}