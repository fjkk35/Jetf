using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.AccsNew.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Service.Services.AccsNew
{
    public class AccsNewService : _BaseService
    {
        private const string API_BASE_URL = "https://accsn.tradevan.com.tw/APACCS/api/v1";
        private const string VERIFY_CODE_URL = API_BASE_URL + "/login/verfiryCode";
        private const string LOGIN_URL = API_BASE_URL + "/login";
        private const string MANIFEST_QUERY_URL = API_BASE_URL + "/imManifest/manifestQry";
        private const string MANIFEST_DETAIL_URL = API_BASE_URL + "/imManifest/manifestDetail";
        private const string WEB_QUERY_PAGE_URL = "https://accsn.tradevan.com.tw/APACCS/web/E/E01002";
        private const string SESSION_TOKEN_KEY = "AccsNew_JwtToken";
        private const string SESSION_COOKIE_CONTAINER_KEY = "AccsNew_CookieContainer";

        private HttpClient CreateHttpClient(string token = null, string referer = null)
        {
            var cookieContainer = GetOrCreateCookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Pragma", "no-cache");

            if (!string.IsNullOrWhiteSpace(referer))
            {
                httpClient.DefaultRequestHeaders.Referrer = new Uri(referer);
            }

            var normalizedToken = NormalizeBearerToken(token ?? GetStoredToken());
            if (!string.IsNullOrWhiteSpace(normalizedToken))
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer_" + normalizedToken);
            }

            return httpClient;
        }

        public async Task<ResponseModel> GetVerifyCodeImageAsync()
        {
            try
            {
                using (var httpClient = CreateHttpClient())
                {
                    var response = await httpClient.GetAsync(VERIFY_CODE_URL);
                    var apiResponse = await ReadApiResponseAsync<VerifyCodeData>(response);

                    if (apiResponse == null || !IsApiSuccess(apiResponse) || apiResponse.Data == null)
                    {
                        return new ResponseModel(apiResponse?.Msg ?? "取得驗證碼失敗");
                    }

                    if (string.IsNullOrWhiteSpace(apiResponse.Data.VerifyPic) || string.IsNullOrWhiteSpace(apiResponse.Data.TransactionId))
                    {
                        return new ResponseModel("取得驗證碼失敗：回應資料不完整");
                    }

                    return new ResponseModel(new
                    {
                        ImageBase64 = $"data:image/png;base64,{apiResponse.Data.VerifyPic}",
                        TransactionId = apiResponse.Data.TransactionId
                    });
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"取得驗證碼失敗：{ex.Message}");
            }
        }

        public async Task<ResponseModel> LoginAsync(AccsNewLoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new ResponseModel("登入資料不可為空");
                }

                if (string.IsNullOrWhiteSpace(request.VerifyCode))
                {
                    return new ResponseModel("請輸入驗證碼");
                }

                if (string.IsNullOrWhiteSpace(request.CaptchaId))
                {
                    return new ResponseModel("驗證碼識別碼不存在，請重新取得驗證碼");
                }

                using (var httpClient = CreateHttpClient())
                {
                    var payload = new
                    {
                        u = string.IsNullOrWhiteSpace(request.UserId) ? "GUEST" : request.UserId.Trim(),
                        p = string.IsNullOrWhiteSpace(request.UserWd) ? "GUEST" : request.UserWd.Trim(),
                        captchaCode = request.VerifyCode.Trim(),
                        captchaId = request.CaptchaId.Trim(),
                        frontendV = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(LOGIN_URL, content);
                    var apiResponse = await ReadApiResponseAsync<LoginData>(response);
                    if (apiResponse == null)
                    {
                        return new ResponseModel("登入失敗：無法解析 API 回應");
                    }

                    if (!IsApiSuccess(apiResponse))
                    {
                        return new ResponseModel(apiResponse.Msg ?? "登入失敗");
                    }

                    if (apiResponse.Data == null || string.IsNullOrWhiteSpace(apiResponse.Data.Token))
                    {
                        return new ResponseModel(apiResponse.Msg ?? "登入成功但無法取得 Token");
                    }

                    var token = NormalizeBearerToken(apiResponse.Data.Token);

                    StoreToken(token);

                    return new ResponseModel(new AccsNewSessionInfo
                    {
                        Token = token,
                        IsLoggedIn = true,
                        LoginTime = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"登入失敗：{ex.Message}");
            }
        }

        public async Task<ResponseModel> QueryAsync(AccsNewQueryRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.MawbNumbers))
                {
                    return new ResponseModel("請輸入主號");
                }

                var mawbList = request.MawbNumbers
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                if (mawbList.Count == 0)
                {
                    return new ResponseModel("請輸入主號");
                }

                var token = GetStoredToken();
                if (string.IsNullOrWhiteSpace(token))
                {
                    return new ResponseModel("請先登入系統");
                }

                using (var httpClient = CreateHttpClient(token, WEB_QUERY_PAGE_URL))
                {
                    var results = new List<AccsNewQueryResult>();

                    foreach (var mawbNo in mawbList)
                    {
                        var queryResults = await QuerySingleMawbAsync(httpClient, mawbNo);
                        if (queryResults.Any(x => x.Status == "SessionExpired"))
                        {
                            ClearToken();
                            return new ResponseModel("您的登入已過期，請重新登入");
                        }

                        results.AddRange(queryResults);
                    }

                    for (int i = 0; i < results.Count; i++)
                    {
                        results[i].No = (i + 1).ToString();
                    }

                    return new ResponseModel(results);
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢失敗：{ex.Message}");
            }
        }

        private async Task<List<AccsNewQueryResult>> QuerySingleMawbAsync(HttpClient httpClient, string mawbNo)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["flightDate"] = string.Empty;
            queryString["estArrivalDate"] = string.Empty;
            queryString["voyageFlightNo"] = string.Empty;
            queryString["mawbNo"] = mawbNo;
            queryString["sort"] = "0";
            queryString["funcCode"] = "E01002";

            var response = await httpClient.GetAsync(MANIFEST_QUERY_URL + "?" + queryString);
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new List<AccsNewQueryResult>
                {
                    new AccsNewQueryResult
                    {
                        MawbNo = mawbNo,
                        Status = "SessionExpired",
                        Message = "登入已過期，請重新登入"
                    }
                };
            }

            var apiResponse = await ReadApiResponseAsync<ManifestQueryData>(response);
            if (apiResponse == null)
            {
                return new List<AccsNewQueryResult>
                {
                    CreateFallbackResult(mawbNo, "查詢失敗：無法解析 API 回應", "Error")
                };
            }

            if (!IsApiSuccess(apiResponse) || apiResponse.Data?.ImList == null || apiResponse.Data.ImList.Count == 0)
            {
                return new List<AccsNewQueryResult>
                {
                    CreateFallbackResult(mawbNo, apiResponse?.Msg ?? "查無資料", apiResponse != null && apiResponse.Status == "E" ? "Error" : "NoData")
                };
            }

            var results = new List<AccsNewQueryResult>();
            foreach (var item in apiResponse.Data.ImList)
            {
                var detailResponse = await QueryManifestDetailAsync(httpClient, item);

                if (detailResponse != null && string.Equals(detailResponse.Status, "Unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    return new List<AccsNewQueryResult>
                    {
                        new AccsNewQueryResult
                        {
                            MawbNo = mawbNo,
                            Status = "SessionExpired",
                            Message = "登入已過期，請重新登入"
                        }
                    };
                }

                results.Add(new AccsNewQueryResult
                {
                    MawbNo = string.IsNullOrWhiteSpace(item.MawbNo) ? mawbNo : item.MawbNo,
                    TotalPackageNumber = ToText(item.TotPackageNumber),
                    SplitPieceNumber = ToText(item.SplitPieceNumber),
                    Weight = ToText(detailResponse?.Data?.TotGrossWeight),
                    FlightNo = item.VoyageFlightNo,
                    ImportDate = FormatDisplayDate(item.EstArrivalDate),
                    VoyageFlightNo = item.VoyageFlightNo,
                    FlightDate = item.FlightDate,
                    EstArrivalDate = item.EstArrivalDate,
                    Status = detailResponse != null && IsApiSuccess(detailResponse) ? "Success" : "Error",
                    Message = detailResponse != null && !string.IsNullOrWhiteSpace(detailResponse.Msg)
                        ? detailResponse.Msg
                        : apiResponse.Msg
                });
            }

            return results;
        }

        public async Task<XSSFWorkbook> ExportExcel(AccsNewQueryRequest request)
        {
            var queryResult = await QueryAsync(request);
            if (queryResult.status == Status.error)
            {
                throw new Exception(queryResult.msg);
            }

            var data = queryResult.ReturnObject as List<AccsNewQueryResult>;
            if (data == null || data.Count == 0)
            {
                throw new Exception("查無資料可匯出");
            }

            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Accs關貿空運查詢(新)");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);
            var centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
            var rightStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Right);

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateCell(headerRow, 0, "項次", headerStyle);
            NpoiCell.CreateCell(headerRow, 1, "主號", headerStyle);
            NpoiCell.CreateCell(headerRow, 2, "主號總件數", headerStyle);
            NpoiCell.CreateCell(headerRow, 3, "本批件數", headerStyle);
            NpoiCell.CreateCell(headerRow, 4, "毛重", headerStyle);
            NpoiCell.CreateCell(headerRow, 5, "航機班次", headerStyle);
            NpoiCell.CreateCell(headerRow, 6, "進口日期", headerStyle);
            NpoiCell.CreateCell(headerRow, 7, "狀態", headerStyle);

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var row = sheet.CreateRow(i + 1);

                NpoiCell.CreateIntCell(row, 0, item.No ?? (i + 1).ToString(), rightStyle);
                NpoiCell.CreateCell(row, 1, item.MawbNo ?? string.Empty, dataStyle);
                NpoiCell.CreateIntCell(row, 2, item.TotalPackageNumber ?? string.Empty, rightStyle);
                NpoiCell.CreateIntCell(row, 3, item.SplitPieceNumber ?? string.Empty, rightStyle);
                NpoiCell.CreateDoubleCell(row, 4, item.Weight ?? string.Empty, rightStyle);
                NpoiCell.CreateCell(row, 5, item.FlightNo ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, 6, item.ImportDate ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, 7, item.Message ?? string.Empty, dataStyle);
            }

            for (int i = 0; i < 8; i++)
            {
                sheet.AutoSizeColumn(i);
                if (sheet.GetColumnWidth(i) < 3000)
                {
                    sheet.SetColumnWidth(i, 3000);
                }
            }

            return workbook;
        }

        private async Task<AccsApiResponse<ManifestDetailData>> QueryManifestDetailAsync(HttpClient httpClient, ManifestItem item)
        {
            var payload = new
            {
                mawbNo = item.MawbNo,
                voyageFlightNo = item.VoyageFlightNo,
                flightDate = item.FlightDate,
                estArrivalDate = item.EstArrivalDate
            };

            var postContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var postResponse = await httpClient.PostAsync(MANIFEST_DETAIL_URL, postContent);

            if (postResponse.StatusCode == HttpStatusCode.Unauthorized || postResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                return new AccsApiResponse<ManifestDetailData> { Status = "Unauthorized", Msg = "登入已過期" };
            }

            if (postResponse.StatusCode == HttpStatusCode.MethodNotAllowed || postResponse.StatusCode == HttpStatusCode.NotFound)
            {
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["mawbNo"] = item.MawbNo;
                queryString["voyageFlightNo"] = item.VoyageFlightNo;
                queryString["flightDate"] = item.FlightDate;
                queryString["estArrivalDate"] = item.EstArrivalDate;

                var getResponse = await httpClient.GetAsync(MANIFEST_DETAIL_URL + "?" + queryString);
                if (getResponse.StatusCode == HttpStatusCode.Unauthorized || getResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new AccsApiResponse<ManifestDetailData> { Status = "Unauthorized", Msg = "登入已過期" };
                }

                return await ReadApiResponseAsync<ManifestDetailData>(getResponse);
            }

            return await ReadApiResponseAsync<ManifestDetailData>(postResponse);
        }

        private async Task<AccsApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = DeserializeJson<AccsApiResponse<T>>(content);
            if (apiResponse != null)
            {
                if (string.IsNullOrWhiteSpace(apiResponse.Msg) && !response.IsSuccessStatusCode)
                {
                    apiResponse.Msg = response.ReasonPhrase;
                }

                return apiResponse;
            }

            if (!response.IsSuccessStatusCode)
            {
                return new AccsApiResponse<T>
                {
                    Status = "E",
                    Msg = string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase : content
                };
            }

            return null;
        }

        private T DeserializeJson<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                return null;
            }
        }

        private bool IsApiSuccess<T>(AccsApiResponse<T> response)
        {
            return response != null && string.Equals(response.Status, "S", StringComparison.OrdinalIgnoreCase);
        }

        private AccsNewQueryResult CreateFallbackResult(string mawbNo, string message, string status)
        {
            return new AccsNewQueryResult
            {
                No = "1",
                MawbNo = mawbNo,
                Status = status,
                Message = message
            };
        }

        private string NormalizeBearerToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            token = token.Trim();
            if (token.StartsWith("Bearer_", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7).Trim();
            }
            else if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7).Trim();
            }

            return token;
        }

        private string ToText(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private string FormatDisplayDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return date.ToString("yyyy/MM/dd");
            }

            return value;
        }

        private string GetStoredToken()
        {
            return HttpContext.Current?.Session?[SESSION_TOKEN_KEY] as string;
        }

        private CookieContainer GetOrCreateCookieContainer()
        {
            var session = HttpContext.Current?.Session;
            if (session == null)
            {
                return new CookieContainer();
            }

            var cookieContainer = session[SESSION_COOKIE_CONTAINER_KEY] as CookieContainer;
            if (cookieContainer == null)
            {
                cookieContainer = new CookieContainer();
                session[SESSION_COOKIE_CONTAINER_KEY] = cookieContainer;
            }

            return cookieContainer;
        }

        private void StoreToken(string token)
        {
            if (HttpContext.Current?.Session != null)
            {
                HttpContext.Current.Session[SESSION_TOKEN_KEY] = NormalizeBearerToken(token);
            }
        }

        private void ClearToken()
        {
            if (HttpContext.Current?.Session != null)
            {
                HttpContext.Current.Session[SESSION_TOKEN_KEY] = null;
                HttpContext.Current.Session[SESSION_COOKIE_CONTAINER_KEY] = null;
            }
        }

        private class AccsApiResponse<T>
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("msg")]
            public string Msg { get; set; }

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; }

            [JsonProperty("data")]
            public T Data { get; set; }
        }

        private class VerifyCodeData
        {
            [JsonProperty("transactionId")]
            public string TransactionId { get; set; }

            [JsonProperty("verifyPic")]
            public string VerifyPic { get; set; }
        }

        private class LoginData
        {
            [JsonProperty("menuList")]
            public List<LoginMenuItem> MenuList { get; set; }

            [JsonProperty("homePage")]
            public string HomePage { get; set; }

            [JsonProperty("token")]
            public string Token { get; set; }
        }

        private class LoginMenuItem
        {
            [JsonProperty("codeId")]
            public string CodeId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("parentId")]
            public string ParentId { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("target")]
            public string Target { get; set; }

            [JsonProperty("order")]
            public int? Order { get; set; }

            [JsonProperty("grp")]
            public int? Grp { get; set; }
        }

        private class ManifestQueryData
        {
            [JsonProperty("imList")]
            public List<ManifestItem> ImList { get; set; }
        }

        private class ManifestItem
        {
            [JsonProperty("mawbNo")]
            public string MawbNo { get; set; }

            [JsonProperty("voyageFlightNo")]
            public string VoyageFlightNo { get; set; }

            [JsonProperty("flightDate")]
            public string FlightDate { get; set; }

            [JsonProperty("estArrivalDate")]
            public string EstArrivalDate { get; set; }

            [JsonProperty("totPackageNumber")]
            public int? TotPackageNumber { get; set; }

            [JsonProperty("splitPieceNumber")]
            public int? SplitPieceNumber { get; set; }
        }

        private class ManifestDetailData
        {
            [JsonProperty("totGrossWeight")]
            public decimal? TotGrossWeight { get; set; }
        }
    }
}
