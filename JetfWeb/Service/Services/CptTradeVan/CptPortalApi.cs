using Newtonsoft.Json;
using NLog;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Service.Models.CptTradeVan;

namespace Service.Services.CptTradeVan
{
    /// <summary>
    /// 關港貿 API 非同步查詢，統一管理呼叫間隔、重試及紀錄。
    /// </summary>
    public class CptPortalApi
    {
        private static readonly Logger Logger = LogManager.GetLogger("CptPortalApi");
        private static readonly SemaphoreSlim RequestIntervalLock = new SemaphoreSlim(1, 1);
        private static readonly Stopwatch RequestIntervalTimer = new Stopwatch();
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 初始化使用 HTTP 用戶端工廠的 API 服務。
        /// </summary>
        /// <param name="httpClientFactory">HTTP 用戶端工廠。</param>
        public CptPortalApi(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// 非同步查詢 GB326 進口簡易申報收單作業結果。
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>進口簡易申報收單作業查詢結果。</returns>
        public async Task<Gb326Model> GetGb326Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB326!query1";
            var apiName = "GB326";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb326Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb326Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb326Model() { Msg = "(Gb326)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result = new Gb326Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// 1.(GB301)進口報單通關流程查詢
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>API 查詢結果。</returns>
        public async Task<Gb301Model> GetGb301Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB301!queryAir";
            var apiName = "GB301";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb301Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            var responseResult = JsonConvert.DeserializeObject<Gb301Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, responseResult?.Status, responseResult?.Msg);
                            return responseResult;
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            return new Gb301Model() { Msg = "(GB301)進口報單通關流程查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result = new Gb301Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// 非同步查詢 GB302 進口報單不受理報關原因。
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>進口報單不受理報關原因查詢結果。</returns>
        public async Task<Gb302Model> GetGb302Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB302!query";
            var apiName = "GB302";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb302Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            var responseResult = JsonConvert.DeserializeObject<Gb302Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, responseResult?.Status, responseResult?.Msg);
                            return responseResult;
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            return new Gb302Model() { Msg = "(GB302)進口報單不受理報關原因查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result = new Gb302Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// (GB350)空運進口貨物新艙單資料查詢-主號
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>API 查詢結果。</returns>
        public async Task<Gb350Model> GetGb350MawbAsync(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryMawb";
            var apiName = "GB350";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb350Model result = new Gb350Model();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        var formData = await new FormUrlEncodedContent(parameters).ReadAsStringAsync().ConfigureAwait(false);
                        var content = new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded");

                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb350Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.Msg = $"(GB350主號)查詢失敗：HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result.Msg = $"(GB350主號)查詢失敗：{ex.GetBaseException().Message}";
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// (GB350)空運進口貨物新艙單資料查詢-明細
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>API 查詢結果。</returns>
        public async Task<Gb350DetailModel> GetGb350DetailAsync(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryDetail";
            var apiName = "GB350";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb350DetailModel result = new Gb350DetailModel();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb350DetailModel>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.Msg = $"(GB350明細)查詢失敗：HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result.Msg = $"(GB350明細)查詢失敗：{ex.GetBaseException().Message}";
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// (GB325)海運進口艙單表頭及彙整資料查詢
        /// </summary>
        /// <param name="parameters">查詢參數，包含海關通關號碼 vslRegNo。</param>
        /// <returns>海運進口艙單表頭及彙整資料查詢結果。</returns>
        public async Task<Gb325Model> GetGb325Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB325!query";
            var apiName = "GB325";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb325Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        using (var content = new FormUrlEncodedContent(parameters))
                        using (HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = JsonConvert.DeserializeObject<Gb325Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                                    ?? new Gb325Model() { Msg = "(GB325)海運進口艙單表頭及彙整資料查詢回應為空，請重新查詢" };
                                LogResponseError(apiName, result?.Status, result?.Msg);
                            }
                            else
                            {
                                Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                                result = new Gb325Model() { Msg = "(GB325)海運進口艙單表頭及彙整資料查詢失敗，請重新查詢" };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result = new Gb325Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// 1.(GB330)海關通關號碼查詢
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>API 查詢結果。</returns>
        public async Task<Gb330Model> GetGb330Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB330!query";
            var apiName = "GB330";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb330Model result = new Gb330Model();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb330Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.Status, result?.Msg);

                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.Msg = "(GB330)海關通關號碼查詢，請重新查詢";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result.Msg = ex.Message;
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// (GB378)海運貨櫃銷艙比例查詢作業
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>API 查詢結果。</returns>
        public async Task<Gb378Model> GetGb378Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB378!query";
            var apiName = "GB378";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb378Model result = new Gb378Model();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb378Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.status, result?.msg);

                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.msg = "(GB378)海運貨櫃銷艙比例查詢作業，請重新查詢";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result.msg = ex.Message;
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.status, result?.msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// 17.(GB353)進口簡易報單錯單查詢
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>API 查詢結果。</returns>
        public async Task<Gb353Model> GetGb353Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB353!query";
            var apiName = "GB353";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb353Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb353Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb353Model() { Msg = "(GB353)進口簡易報單錯單查詢，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result = new Gb353Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// (GB350)空運進口貨物新艙單資料查詢
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>API 查詢結果。</returns>
        public async Task<Gb350Model> GetGb350Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryMawb";
            var apiName = "GB350";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb350Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false);

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb350Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb350Model() { Msg = "(GB350)空運進口貨物新艙單資料查詢，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result = new Gb350Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// 非同步查詢 GB321 進口簡易申報收單作業結果。
        /// </summary>
        /// <param name="parameters">API 查詢參數。</param>
        /// <returns>進口簡易申報收單作業查詢結果。</returns>
        public async Task<Gb321Model> GetGb321Async(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB321!query";
            var apiName = "GB321";

            return await ExecuteWithRetryAsync(async () =>
            {
                Gb321Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    // 每次查詢與重試皆由工廠建立用戶端，結束後釋放。
                    using (HttpClient client = CreateHttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        using (var content = new FormUrlEncodedContent(parameters))
                        using (HttpResponseMessage response = await SendRequestAsync(client, url, content).ConfigureAwait(false))
                        {
                            // 檢查請求是否成功
                            if (response.IsSuccessStatusCode)
                            {
                                result = JsonConvert.DeserializeObject<Gb321Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                                LogResponseError(apiName, result?.Status, result?.Msg);
                            }
                            else
                            {
                                Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                                result = new Gb321Model() { Msg = "(GB321)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName}");
                    result = new Gb321Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg)).ConfigureAwait(false);
        }

        /// <summary>
        /// 建立本次查詢使用的 HTTP 用戶端。
        /// </summary>
        /// <returns>由工廠建立的 HTTP 用戶端。</returns>
        private HttpClient CreateHttpClient()
        {
            // 各查詢共用相同的用戶端來源，每次嘗試結束後由查詢方法釋放。
            return _httpClientFactory.CreateClient(nameof(CptPortalApi));
        }

        /// <summary>
        /// 每次重新取得外部查詢服務觀察到的伺服器出口 IPv4。
        /// 目前暫停呼叫，待有需要時再啟用。
        /// </summary>
        /// <returns>量測到的對外 IPv4，查詢失敗時回傳 Unknown。</returns>
        private async Task<string> GetPublicIpAsync()
        {
            try
            {
                // 每次 GBXXX 呼叫與重試皆重新量測，不共用先前取得的 IP。
                using (var client = CreateHttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    client.DefaultRequestHeaders.ConnectionClose = true;
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                    {
                        NoCache = true,
                        NoStore = true
                    };
                    var value = await client.GetStringAsync("https://api.ipify.org").ConfigureAwait(false);
                    if (!IPAddress.TryParse(value.Trim(), out var address)
                        || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Logger.Error($"PublicIp_Error[Error] - [GET] [{GetLogUserId()}] PublicIp | 對外 IP 查詢未回傳有效的 IPv4 位址。");
                        return "Unknown";
                    }

                    // 這是查詢服務看到的出口；多出口 NAT 不保證每個目的地使用相同 IP。
                    return address.ToString();
                }
            }
            catch (Exception ex)
            {
                // 查詢失敗也不快取，下次呼叫重新量測，並繼續執行這次 GBXXX。
                Logger.Error(ex, $"PublicIp_Error[Error] - [GET] [{GetLogUserId()}] PublicIp");
                return "Unknown";
            }
        }

        /// <summary>
        /// 執行非同步 API 查詢，失敗時等待 5 秒，最多重試 3 次。
        /// </summary>
        /// <typeparam name="T">API 查詢結果型別。</typeparam>
        /// <param name="request">包含錯誤處理與紀錄的單次 API 查詢。</param>
        /// <param name="isSuccessful">判斷結果是否成功或查無資料。</param>
        /// <returns>成功結果，或重試用盡後的最後一次結果。</returns>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> request, Func<T, bool> isSuccessful)
        {
            T result = default(T);
            // 第 0 次為首次查詢，第 1～3 次為重試，使用非同步延遲等待。
            for (var retry = 0; retry <= 3; retry++)
            {
                if (retry > 0)
                    await Task.Delay(5000).ConfigureAwait(false);

                // 暫停查詢對外 IP，直接執行 API；需要時可在此重新啟用。
                result = await request().ConfigureAwait(false);
                if (isSuccessful(result))
                    return result;
            }
            return result;
        }

        /// <summary>
        /// 發送 API 請求，所有實例的呼叫共用至少 0.5 秒的發送間隔。
        /// </summary>
        /// <param name="client">本次查詢使用的 HTTP 用戶端。</param>
        /// <param name="url">實際呼叫的 API 網址。</param>
        /// <param name="content">本次查詢的表單內容。</param>
        /// <returns>API 的 HTTP 回應。</returns>
        private static async Task<HttpResponseMessage> SendRequestAsync(HttpClient client, string url, HttpContent content)
        {
            Task<HttpResponseMessage> request;
            await RequestIntervalLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // 使用單調遞增的計時器，避免系統時間調整影響間隔；首筆可立即發送。
                while (RequestIntervalTimer.IsRunning)
                {
                    var remaining = 500 - RequestIntervalTimer.Elapsed.TotalMilliseconds;
                    if (remaining <= 0)
                        break;
                    await Task.Delay((int)Math.Ceiling(remaining)).ConfigureAwait(false);
                }

                request = client.PostAsync(url, content);
                RequestIntervalTimer.Restart();
            }
            finally
            {
                // 只序列化發送時機，等候回應時釋放鎖定，讓其他呼叫可依間隔發送。
                RequestIntervalLock.Release();
            }
            return await request.ConfigureAwait(false);
        }

        /// <summary>
        /// 判斷 API 回應是否成功，查無資料視為正常查詢結果。
        /// </summary>
        /// <param name="status">API 回傳狀態。</param>
        /// <param name="message">API 回傳訊息。</param>
        /// <returns>成功或查無資料時回傳 true，其餘回傳 false。</returns>
        private static bool IsSuccessfulResponse(string status, string message)
        {
            // 成功判斷同時提供重試與錯誤紀錄使用，避免兩處採用不同規則。
            if (message?.Contains("查無資料") == true)
                return true;

            var successfulStatus = status == "ok" || status == "success";
            var failedStatus = !string.IsNullOrWhiteSpace(status) && !successfulStatus;
            var failedMessage = message?.Contains("失敗") == true || message?.Contains("錯誤") == true;
            return !failedStatus && !failedMessage
                && (successfulStatus || message?.Contains("成功") == true);
        }

        /// <summary>
        /// 取得目前紀錄使用的登入帳號。
        /// </summary>
        /// <returns>登入帳號，未提供時回傳 Unknown。</returns>
        private static string GetLogUserId()
        {
            // 沿用 MVC 設定的邏輯內容，讓帳號隨非同步查詢與重試傳遞。
            var userId = MappedDiagnosticsLogicalContext.Get("userId");
            return string.IsNullOrWhiteSpace(userId) ? "Unknown" : userId.Trim();
        }

        /// <summary>
        /// 記錄每次 API 嘗試的代碼與參數，並開始計算耗時。
        /// </summary>
        /// <param name="apiName">關港貿 API 代碼。</param>
        /// <param name="parameters">本次查詢參數。</param>
        /// <returns>供結束紀錄使用的計時器。</returns>
        private static Stopwatch LogRequestBegin(string apiName, Dictionary<string, string> parameters)
        {
            // 每次重試均保留獨立紀錄，參數以 JSON 呈現，沿用現有 request log 格式。
            Logger.Debug($"Request_Begin[Debug] - [POST] [{GetLogUserId()}] {apiName} | Params={JsonConvert.SerializeObject(parameters)}");
            return Stopwatch.StartNew();
        }

        /// <summary>
        /// 記錄 HTTP 成功回應中的 API 業務錯誤或空白回應。
        /// </summary>
        /// <param name="apiName">關港貿 API 代碼。</param>
        /// <param name="status">API 回傳狀態。</param>
        /// <param name="message">API 回傳訊息。</param>
        private static void LogResponseError(string apiName, string status, string message)
        {
            // 與重試流程共用成功判斷，失敗時仍寫入同一份 CptPortalApi log。
            if (IsSuccessfulResponse(status, message))
                return;

            Logger.Error($"Request_Error[Error] - [POST] [{GetLogUserId()}] {apiName} | Status={status} | Msg={message ?? "API 回應為空"}");
        }

        /// <summary>
        /// 記錄每次 API 嘗試結束及耗時，成功與失敗均會執行。
        /// </summary>
        /// <param name="apiName">關港貿 API 代碼。</param>
        /// <param name="stopwatch">本次 API 嘗試的計時器。</param>
        private static void LogRequestEnd(string apiName, Stopwatch stopwatch)
        {
            // 由 finally 呼叫，確保提前回傳、重試或發生例外時仍有結束紀錄。
            stopwatch.Stop();
            Logger.Debug($"Request_End[Debug] - [POST] [{GetLogUserId()}] {apiName} | cost: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
