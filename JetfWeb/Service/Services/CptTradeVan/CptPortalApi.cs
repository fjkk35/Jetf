using Newtonsoft.Json;
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
    public class CptPortalApi
    {
        public Gb326Model GetGb326(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 20;
            Gb326Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB326!query1";

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb326Model>(response.Content.ReadAsStringAsync().Result);
                        }
                        else
                        {
                            result = new Gb326Model() { Msg = "(Gb326)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Gb326Model() { Msg = ex.Message };
                }

                if (result != null && (result.Msg.Contains("執行成功") || result.Msg.Contains("查無資料")))
                {
                    break;
                }

                retryCount++;
                Thread.Sleep(1000);
            } while (retryCount < maxRetries);

            return result;
        }

        public Gb301Model GetGb301(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 20;
            Gb301Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB301!queryAir";

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            return JsonConvert.DeserializeObject<Gb301Model>(response.Content.ReadAsStringAsync().Result);
                        }
                        else
                        {
                            return new Gb301Model() { Msg = "(GB301)進口報單通關流程查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Gb301Model() { Msg = ex.Message };
                }

                if (result != null && (result.Msg.Contains("執行成功") || result.Msg.Contains("查無資料")))
                {
                    break;
                }

                retryCount++;
                Thread.Sleep(1000);
            } while (retryCount < maxRetries);

            return result;
        }

        public Gb302Model GetGb302(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 20;
            Gb302Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB302!query";

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            return JsonConvert.DeserializeObject<Gb302Model>(response.Content.ReadAsStringAsync().Result);
                        }
                        else
                        {
                            return new Gb302Model() { Msg = "(GB302)進口報單不受理報關原因查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Gb302Model() { Msg = ex.Message };
                }

                if (result != null && (result.Msg.Contains("執行成功") || result.Msg.Contains("查無資料")))
                {
                    break;
                }

                retryCount++;
                Thread.Sleep(1000);
            } while (retryCount < maxRetries);

            return result;
        }

        public Gb321Model GetGb321(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 20;
            Gb321Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB321!query";


                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            return JsonConvert.DeserializeObject<Gb321Model>(response.Content.ReadAsStringAsync().Result);
                        }
                        else
                        {
                            return new Gb321Model() { Msg = "(GB301)進口報單通關流程查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Gb321Model() { Msg = ex.Message };
                }

                if (result != null && (result.Msg.Contains("執行成功") || result.Msg.Contains("查無資料")))
                {
                    break;
                }

                retryCount++;
                Thread.Sleep(1000);
            } while (retryCount < maxRetries);

            return result;
        }
    }
}
