using JETFWebAPI.Models.Global;
using JETFWebAPI.Models.LogisticsSpecialist;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JETFWebAPI.Services
{
    public class LogisticsSpecialistService
    {
        /// <summary>
        /// 查詢物流專家資料
        /// </summary>
        /// <param name="requestList"></param>
        /// <returns></returns>
        public async Task<string> QueryLogisticsSpecialistAsync(List<LogisticsSpecialistQueryRequest> requestList)
        {
            try
            {
                const string apiUrl = "https://gcp.dasgo.com.tw/api/Common/Query_Doc";
                const string bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIzNTI0IiwiYXBpVXNlcklkIjoiaXRyaSIsInN5c1VzZXJJZCI6IjM1MjQiLCJzeXNDb3JwSWQiOiI2OCIsInVzZXJJZCI6Iml0cmkiLCJ1c2VyTmFtZSI6Iuezu-e1seS4suaOpeeuoeeQhiIsInN5c0dyb3VwSWQiOiI0IiwiZ3JvdXBJZCI6IjA0MCIsImNvcnBTaG9ydE5hbWUiOiLmjbfnqanpgJrnianmtYEiLCJuYmYiOjE3NTU2NzQ1NDMsImV4cCI6MjA3MTIwNzM0MywiaWF0IjoxNzU1Njc0NTQzLCJpc3MiOiJKd3RBdXRoRGVtbyJ9.cjdnYHSR4wZ8-6hiMcShFEgjSpv3jotmf_AxlQxzaB0";

                using (HttpClient client = new HttpClient())
                {
                    // 設定 Authorization Header
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                    // 序列化請求資料
                    string jsonRequest = JsonConvert.SerializeObject(requestList);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    // 發送 POST 請求
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    // 讀取回應
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // 檢查請求是否成功
                    if (response.IsSuccessStatusCode)
                    {
                        return responseContent;
                    }
                    else
                    {
                        throw new Exception($"API 調用失敗: {response.StatusCode} - {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"查詢物流專家資料時發生錯誤: {ex.Message}");
            }
        }
    }
}