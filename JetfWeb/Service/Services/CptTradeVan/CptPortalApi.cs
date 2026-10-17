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
    public class CptPortalApi : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetLogger("CptPortalApi");
        private static readonly SemaphoreSlim RequestIntervalLock = new SemaphoreSlim(1, 1);
        private static readonly Stopwatch RequestIntervalTimer = new Stopwatch();
        private readonly HttpClient _httpClient;

        public CptPortalApi()
        {
            _httpClient = new HttpClient();
        }

        public CptPortalApi(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
        public Gb326Model GetGb326(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB326!query1";
            var apiName = "GB326";

            return ExecuteWithRetry(() =>
            {
                Gb326Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb326Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb326Model() { Msg = "(Gb326)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result = new Gb326Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// 1.(GB301)進口報單通關流程查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb301Model GetGb301(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB301!queryAir";
            var apiName = "GB301";

            return ExecuteWithRetry(() =>
            {
                Gb301Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            var responseResult = JsonConvert.DeserializeObject<Gb301Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, responseResult?.Status, responseResult?.Msg);
                            return responseResult;
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            return new Gb301Model() { Msg = "(GB301)進口報單通關流程查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result = new Gb301Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        public Gb302Model GetGb302(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB302!query";
            var apiName = "GB302";

            return ExecuteWithRetry(() =>
            {
                Gb302Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            var responseResult = JsonConvert.DeserializeObject<Gb302Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, responseResult?.Status, responseResult?.Msg);
                            return responseResult;
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            return new Gb302Model() { Msg = "(GB302)進口報單不受理報關原因查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result = new Gb302Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// 11.(GB321)進口簡易申報收單作業結果查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb321Model GetGb321(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB321!query";
            var apiName = "GB321";

            return ExecuteWithRetry(() =>
            {
                Gb321Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb321Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb321Model() { Msg = "(GB321)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result = new Gb321Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// (GB350)空運進口貨物新艙單資料查詢-主號
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb350Model GetGb350Mawb(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryMawb";
            var apiName = "GB350";

            return ExecuteWithRetry(() =>
            {
                Gb350Model result = new Gb350Model();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                        var formData = new FormUrlEncodedContent(parameters).ReadAsStringAsync().Result;
                        var content = new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded");

                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb350Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.Msg = $"(GB350主號)查詢失敗：HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result.Msg = $"(GB350主號)查詢失敗：{ex.GetBaseException().Message}";
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// (GB350)空運進口貨物新艙單資料查詢-明細
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb350DetailModel GetGb350Detail(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryDetail";
            var apiName = "GB350";

            return ExecuteWithRetry(() =>
            {
                Gb350DetailModel result = new Gb350DetailModel();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb350DetailModel>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.Msg = $"(GB350明細)查詢失敗：HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result.Msg = $"(GB350明細)查詢失敗：{ex.GetBaseException().Message}";
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// (GB325)海運進口艙單表頭及彙整資料查詢
        /// </summary>
        /// <param name="parameters">查詢參數，包含海關通關號碼 vslRegNo。</param>
        /// <returns>海運進口艙單表頭及彙整資料查詢結果。</returns>
        public Gb325Model GetGb325(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB325!query";
            var apiName = "GB325";

            return ExecuteWithRetry(() =>
            {
                Gb325Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        using (var content = new FormUrlEncodedContent(parameters))
                        using (HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult())
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = JsonConvert.DeserializeObject<Gb325Model>(response.Content.ReadAsStringAsync().Result)
                                    ?? new Gb325Model() { Msg = "(GB325)海運進口艙單表頭及彙整資料查詢回應為空，請重新查詢" };
                                LogResponseError(apiName, result?.Status, result?.Msg);
                            }
                            else
                            {
                                Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                                result = new Gb325Model() { Msg = "(GB325)海運進口艙單表頭及彙整資料查詢失敗，請重新查詢" };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result = new Gb325Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// 1.(GB330)海關通關號碼查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb330Model GetGb330(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB330!query";
            var apiName = "GB330";

            return ExecuteWithRetry(() =>
            {
                Gb330Model result = new Gb330Model();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb330Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.Status, result?.Msg);

                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.Msg = "(GB330)海關通關號碼查詢，請重新查詢";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result.Msg = ex.Message;
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// (GB378)海運貨櫃銷艙比例查詢作業
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb378Model GetGb378(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB378!query";
            var apiName = "GB378";

            return ExecuteWithRetry(() =>
            {
                Gb378Model result = new Gb378Model();
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb378Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.status, result?.msg);

                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result.msg = "(GB378)海運貨櫃銷艙比例查詢作業，請重新查詢";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result.msg = ex.Message;
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.status, result?.msg));
        }

        /// <summary>
        /// 17.(GB353)進口簡易報單錯單查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb353Model GetGb353(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB353!query";
            var apiName = "GB353";

            return ExecuteWithRetry(() =>
            {
                Gb353Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb353Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb353Model() { Msg = "(GB353)進口簡易報單錯單查詢，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result = new Gb353Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

        /// <summary>
        /// (GB350)空運進口貨物新艙單資料查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb350Model GetGb350(Dictionary<string, string> parameters)
        {
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryMawb";
            var apiName = "GB350";

            return ExecuteWithRetry(() =>
            {
                Gb350Model result = null;
                var stopwatch = LogRequestBegin(apiName, parameters);
                try
                {
                    //關貿Api

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = SendRequestAsync(client, url, content).GetAwaiter().GetResult();

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb350Model>(response.Content.ReadAsStringAsync().Result);
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb350Model() { Msg = "(GB350)空運進口貨物新艙單資料查詢，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
                    result = new Gb350Model() { Msg = ex.Message };
                }
                finally
                {
                    LogRequestEnd(apiName, stopwatch);
                }

                return result;
            }, result => IsSuccessfulResponse(result?.Status, result?.Msg));
        }

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
                    //關貿Api
                    //將參數轉換成 FormUrlEncodedContent
                    var content = new FormUrlEncodedContent(parameters);
                    //發送 POST 請求
                    //_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (HttpResponseMessage response = await SendRequestAsync(_httpClient, url, content).ConfigureAwait(false))
                    {
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb321Model>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            LogResponseError(apiName, result?.Status, result?.Msg);
                        }
                        else
                        {
                            Logger.Error($"Request_Error[Error] - [POST] {apiName} | HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
                            result = new Gb321Model() { Msg = "(GB321)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Request_Error[Error] - [POST] {apiName}");
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
        /// 執行同步 API 查詢，失敗時等待 5 秒，最多重試 3 次。
        /// </summary>
        /// <typeparam name="T">API 查詢結果型別。</typeparam>
        /// <param name="request">包含錯誤處理與紀錄的單次 API 查詢。</param>
        /// <param name="isSuccessful">判斷結果是否成功或查無資料。</param>
        /// <returns>成功結果，或重試用盡後的最後一次結果。</returns>
        private static T ExecuteWithRetry<T>(Func<T> request, Func<T, bool> isSuccessful)
        {
            T result = default(T);
            // 第 0 次為首次查詢，第 1～3 次為重試；最後一次失敗後直接回傳。
            for (var retry = 0; retry <= 3; retry++)
            {
                if (retry > 0)
                    Thread.Sleep(5000);

                result = request();
                if (isSuccessful(result))
                    return result;
            }
            return result;
        }

        /// <summary>
        /// 執行非同步 API 查詢，失敗時等待 5 秒，最多重試 3 次。
        /// </summary>
        /// <typeparam name="T">API 查詢結果型別。</typeparam>
        /// <param name="request">包含錯誤處理與紀錄的單次 API 查詢。</param>
        /// <param name="isSuccessful">判斷結果是否成功或查無資料。</param>
        /// <returns>成功結果，或重試用盡後的最後一次結果。</returns>
        private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> request, Func<T, bool> isSuccessful)
        {
            T result = default(T);
            // 非同步重試使用延遲等待，不阻塞處理執行緒。
            for (var retry = 0; retry <= 3; retry++)
            {
                if (retry > 0)
                    await Task.Delay(5000).ConfigureAwait(false);

                result = await request().ConfigureAwait(false);
                if (isSuccessful(result))
                    return result;
            }
            return result;
        }

        /// <summary>
        /// 發送 API 請求，所有實例及同步、非同步呼叫共用至少 0.5 秒的發送間隔。
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
        /// 記錄每次 API 嘗試的代碼與參數，並開始計算耗時。
        /// </summary>
        /// <param name="apiName">關港貿 API 代碼。</param>
        /// <param name="parameters">本次查詢參數。</param>
        /// <returns>供結束紀錄使用的計時器。</returns>
        private static Stopwatch LogRequestBegin(string apiName, Dictionary<string, string> parameters)
        {
            // 每次重試均保留獨立紀錄，參數以 JSON 呈現，沿用現有 request log 格式。
            Logger.Debug($"Request_Begin[Debug] - [POST] {apiName} | Params={JsonConvert.SerializeObject(parameters)}");
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

            Logger.Error($"Request_Error[Error] - [POST] {apiName} | Status={status} | Msg={message ?? "API 回應為空"}");
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
            Logger.Debug($"Request_End[Debug] - [POST] {apiName} | cost: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
