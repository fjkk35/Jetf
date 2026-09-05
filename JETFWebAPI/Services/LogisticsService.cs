using JETFWebAPI.Controllers;
using JETFWebAPI.Models.Logistics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JETFWebAPI.Services
{
    /// <summary>
    /// 物流查詢服務
    /// </summary>
    public class LogisticsService
    {
        private readonly string _apiBaseUrl = "https://gcp.dasgo.com.tw/api/Common/";
        private readonly string _bearerToken = @"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIzNTI0IiwiYXBpVXNlcklkIjoiaXRyaSIsInN5c1VzZXJJZCI6IjM1MjQiLCJzeXNDb3JwSWQiOiI2OCIsInVzZXJJZCI6Iml0cmkiLCJ1c2VyTmFtZSI6Iuezu-e1seS4suaOpeeuoeeQhiIsInN5c0dyb3VwSWQiOiI0IiwiZ3JvdXBJZCI6IjA0MCIsImNvcnBTaG9ydE5hbWUiOiLmjbfnqanpgJrnianmtYEiLCJuYmYiOjE3NzMxOTg0OTksImV4cCI6MjA4ODgxNzY5OSwiaWF0IjoxNzczMTk4NDk5LCJpc3MiOiJKd3RBdXRoRGVtbyJ9.FE4fnJB_jEOyShb4rOUsPVlWFJdBN5UYL5FR9Fo-xHY";

        /// <summary>
        /// 查詢託運單配送狀態
        /// </summary>
        /// <param name="requests">查詢請求列表</param>
        /// <returns>查詢結果</returns>
        public async Task<string> QueryAsync(List<QueryRequest> request)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // 設定 HttpClient 超時時間
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // 序列化請求資料為 camelCase
                    var jsonContent = JsonConvert.SerializeObject(request);

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // 發送 POST 請求到外部 API
                    var response = await httpClient.PostAsync(_apiBaseUrl + "Query_Doc", content);

                    // 讀取回應內容
                    var responseContent = await response.Content.ReadAsStringAsync();

                    return responseContent;
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new QueryResponse()
                {
                    ResultCode = "01",
                    Error = ex.Message,
                });
            }
        }

        /// <summary>
        /// 下載圖片
        /// </summary>
        /// <param name="request">下載圖片請求</param>
        /// <returns>下載結果</returns>
        public async Task<DownLoadImageResponse> DownLoadImageAsync(DownLoadImageRequest request)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // 設定 HttpClient 超時時間
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // 序列化請求資料
                    var jsonContent = JsonConvert.SerializeObject(request);

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // 發送 POST 請求到外部 API
                    var response = await httpClient.PostAsync(_apiBaseUrl + "DownLoad_Image", content);

                    // 讀取回應內容
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // 直接反序列化回應資料，不做任何檢查
                    var result = JsonConvert.DeserializeObject<DownLoadImageResponse>(responseContent);

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new DownLoadImageResponse() 
                { 
                    ResultCode = "01",
                    Error = ex.Message
                };
            }
        }
    }
}