using FluentFTP.Helpers;
using HtmlAgilityPack;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.AccsShopee.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Service.Services.AccsShopee
{
    public class AccsShopeeService : _BaseService
    {
        private const string BASE_URL = "https://accs.tradevan.com.tw";
        private const string LOGIN_URL = BASE_URL + "/accsw-bin/APACCS/userLoginAction.do";
        private const string QUERY_PAGE_URL = BASE_URL + "/accsw-bin/APACCS/customer/FrmImMergeQuery.jsp";
        private const string QUERY_ACTION_URL = BASE_URL + "/accsw-bin/APACCS/cImMergeQueryAction.do";
        private const string VERIFY_CODE_URL = BASE_URL + "/accsw-bin/APACCS/verifyPic.jsp";

        private const string SESSION_COOKIE_CONTAINER = "AccsShopee_CookieContainer";

        /// <summary>
        /// 取得或建立 HttpClient（使用 Session 保持狀態）
        /// </summary>
        private HttpClient GetHttpClient()
        {
            var session = HttpContext.Current?.Session;
            if (session == null)
            {
                // 如果沒有 Session（例如在背景工作），直接建立新的
                return CreateHttpClient(new CookieContainer());
            }

            // 從 Session 取得 CookieContainer
            var cookieContainer = session[SESSION_COOKIE_CONTAINER] as CookieContainer;
            if (cookieContainer == null)
            {
                cookieContainer = new CookieContainer();
                session[SESSION_COOKIE_CONTAINER] = cookieContainer;
            }

            // 每次都建立新的 HttpClient，但共用同一個 CookieContainer
            return CreateHttpClient(cookieContainer);
        }

        /// <summary>
        /// 建立 HttpClient
        /// </summary>
        private HttpClient CreateHttpClient(CookieContainer cookieContainer)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // 設定預設 Headers
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7");

            return httpClient;
        }

        /// <summary>
        /// 取得驗證碼圖片
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel> GetVerifyCodeImageAsync()
        {
            try
            {
                using (var httpClient = GetHttpClient())
                {
                    // 先訪問登入頁面以建立 Session
                    var loginPageResponse = await httpClient.GetAsync(LOGIN_URL);
                    loginPageResponse.EnsureSuccessStatusCode();

                    // 取得驗證碼圖片
                    var verifyCodeResponse = await httpClient.GetAsync(VERIFY_CODE_URL);
                    verifyCodeResponse.EnsureSuccessStatusCode();

                    var imageBytes = await verifyCodeResponse.Content.ReadAsByteArrayAsync();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    return new ResponseModel
                    {
                        ReturnObject = new
                        {
                            ImageBase64 = $"data:image/jpeg;base64,{base64Image}",
                            SessionId = GetSessionId()
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"取得驗證碼失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 登入 Accs 系統
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ResponseModel> LoginAsync(AccsLoginRequest request)
        {
            try
            {
                using (var httpClient = GetHttpClient())
                {
                    // 準備登入表單資料
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("userid", request.UserId ?? "GUEST"),
                        new KeyValuePair<string, string>("userwd", request.UserWd ?? "GUEST"),
                        new KeyValuePair<string, string>("verifyCode", request.VerifyCode),
                        new KeyValuePair<string, string>("loginType", "1")
                    });

                    // 發送登入請求
                    var response = await httpClient.PostAsync(LOGIN_URL, formContent);
                    var html = await response.Content.ReadAsStringAsync();

                    // 檢查登入是否成功
                    if (html.Contains("驗證碼錯誤") || html.Contains("登入失敗"))
                    {
                        return new ResponseModel("登入失敗：驗證碼錯誤或帳號密碼不正確");
                    }

                    // 取得 Token
                    var token = ExtractToken(html);
                    if (string.IsNullOrEmpty(token))
                    {
                        // 可能需要導向查詢頁面
                        var queryPageResponse = await httpClient.GetAsync(QUERY_PAGE_URL);
                        var queryPageHtml = await queryPageResponse.Content.ReadAsStringAsync();
                        token = ExtractToken(queryPageHtml);
                    }

                    if (string.IsNullOrEmpty(token))
                    {
                        return new ResponseModel("登入失敗：無法取得 Token");
                    }

                    return new ResponseModel
                    {
                        ReturnObject = new AccsSessionInfo
                        {
                            Token = token,
                            SessionCookie = GetSessionId(),
                            IsLoggedIn = true,
                            LoginTime = DateTime.Now
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"登入失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢資料
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ResponseModel> QueryAsync(AccsQueryRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.MawbNumbers))
                {
                    return new ResponseModel("請輸入主提單號");
                }

                var results = new List<AccsQueryResult>();
                var mawbList = request.MawbNumbers
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .ToList();


                using (var httpClient = GetHttpClient())
                {
                    foreach (var mawb in mawbList)
                    {
                        try
                        {
                            var result = await QuerySingleAsync(httpClient, mawb, request.Token);
                            
                            // 檢查是否有 SessionExpired 狀態
                            if (result.Any(r => r.Status == "SessionExpired"))
                            {
                                // 清除 Session Cookie
                                ClearSession();
                                return new ResponseModel("您的登入已過期，請重新登入");
                            }
                            
                            results.AddRange(result);
                        }
                        catch (Exception ex)
                        {
                            results.Add(new AccsQueryResult
                            {
                                MawbNo = mawb,
                                Status = "Error",
                                Message = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                return new ResponseModel
                {
                    ReturnObject = results
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆資料
        /// </summary>
        private async Task<List<AccsQueryResult>> QuerySingleAsync(HttpClient httpClient, string mawbNo, string token)
        {
            try
            {
                // 步驟1: 先查詢以取得連結參數
                var searchFormContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("org.apache.struts.taglib.html.TOKEN", token),
                    new KeyValuePair<string, string>("mawb_no", mawbNo),
                    new KeyValuePair<string, string>("voyage_flight_no", ""),
                    new KeyValuePair<string, string>("flight_date", ""),
                    new KeyValuePair<string, string>("est_arrival_date", "")
                });

                var searchResponse = await httpClient.PostAsync(QUERY_ACTION_URL, searchFormContent);
                var searchHtml = await searchResponse.Content.ReadAsStringAsync();

                // 檢查是否 Session 過期（userID 為空）
                if (IsSessionExpired(searchHtml))
                {
                    return new List<AccsQueryResult>() {
                        new AccsQueryResult {
                            MawbNo = mawbNo,
                            Status = "SessionExpired",
                            Message = "登入已過期，請重新登入"
                        }
                    };
                }

                // 步驟2: 解析搜尋結果，找到所有主提單號的連結
                var linkParamsList = ExtractAllLinkParameters(searchHtml, mawbNo);
                if (linkParamsList == null || linkParamsList.Count == 0)
                {
                    return new List<AccsQueryResult>() {
                        new AccsQueryResult {
                            MawbNo = mawbNo,
                            Status = "NoData",
                            Message = "查無資料"
                        }
                    };
                }

                // 步驟3: 查詢所有連結並彙整資料
                var results = new List<AccsQueryResult>();

                foreach (var linkParams in linkParamsList)
                {
                    try
                    {
                        var detailFormContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("org.apache.struts.taglib.html.TOKEN", token),
                            new KeyValuePair<string, string>("mawb_no", linkParams.MawbNo),
                            new KeyValuePair<string, string>("voyage_flight_no", linkParams.VoyageFlightNo),
                            new KeyValuePair<string, string>("est_arrival_date", linkParams.EstArrivalDate),
                            new KeyValuePair<string, string>("flight_date", linkParams.FlightDate),
                            new KeyValuePair<string, string>("qry_mawb_no", linkParams.QryMawbNo),
                            new KeyValuePair<string, string>("qry_voyage_flight_no", linkParams.QryVoyageFlightNo ?? ""),
                            new KeyValuePair<string, string>("qry_flight_date", linkParams.QryFlightDate ?? ""),
                            new KeyValuePair<string, string>("qry_est_arrival_date", linkParams.QryEstArrivalDate ?? ""),
                            new KeyValuePair<string, string>("qry_abnormal_mark", linkParams.QryAbnormalMark ?? ""),
                            new KeyValuePair<string, string>("qry_carrier", linkParams.QryCarrier ?? ""),
                            new KeyValuePair<string, string>("qry_sort", linkParams.QrySort ?? "0")
                        });

                        var detailResponse = await httpClient.PostAsync(BASE_URL + "/accsw-bin/APACCS/cImMergeListAction.do", detailFormContent);
                        var detailHtml = await detailResponse.Content.ReadAsStringAsync();

                        // 檢查明細頁面是否 Session 過期
                        if (IsSessionExpired(detailHtml))
                        {
                            return new List<AccsQueryResult>() {
                                new AccsQueryResult {
                                    MawbNo = mawbNo,
                                    Status = "SessionExpired",
                                    Message = "登入已過期，請重新登入"
                                }
                            };
                        }

                        var result = ParseDetailResult(detailHtml, mawbNo);
                        result.RawHtml = detailHtml;
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new AccsQueryResult
                        {
                            MawbNo = mawbNo,
                            Status = "Error",
                            Message = $"查詢明細失敗：{ex.Message}"
                        });
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                return new List<AccsQueryResult>() {
                    new AccsQueryResult {
                        MawbNo = mawbNo,
                        Status = "Error",
                        Message = $"查詢失敗：{ex.Message}"
                    }
                };
            }
        }

        /// <summary>
        /// 從搜尋結果 HTML 中提取所有連結參數
        /// </summary>
        private List<AccsLinkParameters> ExtractAllLinkParameters(string html, string mawbNo)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var parametersList = new List<AccsLinkParameters>();

                // 尋找所有包含主提單號的連結
                var linkNodes = doc.DocumentNode.SelectNodes($"//a[contains(text(), '{mawbNo}')]");
                if (linkNodes == null || linkNodes.Count == 0)
                {
                    return null;
                }

                foreach (var linkNode in linkNodes)
                {
                    var onClickAttr = linkNode.GetAttributeValue("href", "");
                    if (string.IsNullOrEmpty(onClickAttr))
                    {
                        continue;
                    }

                    // 使用正則表達式提取參數
                    var parameters = new AccsLinkParameters();

                    var mawbNoMatch = Regex.Match(onClickAttr, @"mawb_no\.value='([^']+)'");
                    if (mawbNoMatch.Success) parameters.MawbNo = mawbNoMatch.Groups[1].Value;

                    var estArrivalMatch = Regex.Match(onClickAttr, @"est_arrival_date\.value='([^']+)'");
                    if (estArrivalMatch.Success) parameters.EstArrivalDate = estArrivalMatch.Groups[1].Value;

                    var voyageFlightMatch = Regex.Match(onClickAttr, @"voyage_flight_no\.value='([^']+)'");
                    if (voyageFlightMatch.Success) parameters.VoyageFlightNo = voyageFlightMatch.Groups[1].Value;

                    var flightDateMatch = Regex.Match(onClickAttr, @"flight_date\.value='([^']+)'");
                    if (flightDateMatch.Success) parameters.FlightDate = flightDateMatch.Groups[1].Value;

                    var qryMawbNoMatch = Regex.Match(onClickAttr, @"qry_mawb_no\.value='([^']+)'");
                    if (qryMawbNoMatch.Success) parameters.QryMawbNo = qryMawbNoMatch.Groups[1].Value;

                    var qryVoyageMatch = Regex.Match(onClickAttr, @"qry_voyage_flight_no\.value='([^']*)'");
                    if (qryVoyageMatch.Success) parameters.QryVoyageFlightNo = qryVoyageMatch.Groups[1].Value;

                    var qryFlightDateMatch = Regex.Match(onClickAttr, @"qry_flight_date\.value='([^']*)'");
                    if (qryFlightDateMatch.Success) parameters.QryFlightDate = qryFlightDateMatch.Groups[1].Value;

                    var qryEstArrivalMatch = Regex.Match(onClickAttr, @"qry_est_arrival_date\.value='([^']*)'");
                    if (qryEstArrivalMatch.Success) parameters.QryEstArrivalDate = qryEstArrivalMatch.Groups[1].Value;

                    var qryAbnormalMatch = Regex.Match(onClickAttr, @"qry_abnormal_mark\.value='([^']*)'");
                    if (qryAbnormalMatch.Success) parameters.QryAbnormalMark = qryAbnormalMatch.Groups[1].Value;

                    var qryCarrierMatch = Regex.Match(onClickAttr, @"qry_carrier\.value='([^']*)'");
                    if (qryCarrierMatch.Success) parameters.QryCarrier = qryCarrierMatch.Groups[1].Value;

                    var qrySortMatch = Regex.Match(onClickAttr, @"qry_sort\.value='([^']*)'");
                    if (qrySortMatch.Success) parameters.QrySort = qrySortMatch.Groups[1].Value;

                    // 只加入有效的參數
                    if (!string.IsNullOrEmpty(parameters.MawbNo))
                    {
                        parametersList.Add(parameters);
                    }
                }

                return parametersList.Count > 0 ? parametersList : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析詳細資料頁面
        /// </summary>
        private AccsQueryResult ParseDetailResult(string html, string mawbNo)
        {
            var result = new AccsQueryResult
            {
                MawbNo = mawbNo,
                Status = "Success"
            };

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 檢查是否有錯誤訊息
                var errorNode = doc.DocumentNode.SelectSingleNode("//span[@class='error']");
                if (errorNode != null)
                {
                    result.Status = "Error";
                    result.Message = errorNode.InnerText.Trim();
                    return result;
                }

                // 根據實際 HTML 結構，直接選擇第三個表格
                var dataTable = doc.DocumentNode.SelectNodes("//table[@border='1' and @bordercolor='#79A8AE']")?[0];

                if (dataTable == null)
                {
                    // 嘗試另一種方式：找到包含 "主號總件數" 的表格
                    dataTable = doc.DocumentNode.SelectSingleNode("//table[.//td[contains(text(), '主號總件數')]]");
                }

                if (dataTable == null)
                {
                    result.Status = "NoData";
                    result.Message = "無法找到資料表格";
                    return result;
                }

                // 提取各個欄位資料
                var tdTotalHwb = dataTable.SelectSingleNode(".//td[.//text()[contains(., '主號總件數')]]/following-sibling::td[1]");
                var tdTotal = dataTable.SelectSingleNode(".//td[.//text()[contains(., '本批件數')]]/following-sibling::td[1]");
                var tdWeight = dataTable.SelectSingleNode(".//td[.//text()[contains(., '毛重')] and not(.//text()[contains(., '重量單位')])]/following-sibling::td[1]");
                var tdFlightNo = dataTable.SelectSingleNode(".//td[.//text()[contains(., '航機班次')]]/following-sibling::td[1]");
                var tdImportDate = dataTable.SelectSingleNode(".//td[.//text()[contains(., '進口日期')]]/following-sibling::td[1]");

                // 清理並設定資料
                result.TotalHwb = CleanText(tdTotalHwb?.InnerText);
                result.Total = CleanText(tdTotal?.InnerText);
                result.Weight = CleanText(tdWeight?.InnerText);
                result.FlightNo = CleanText(tdFlightNo?.InnerText);
                result.ImportDate = CleanText(tdImportDate?.InnerText);

                // 檢查是否有取得資料
                if (string.IsNullOrEmpty(result.TotalHwb) &&
                    string.IsNullOrEmpty(result.Total) &&
                    string.IsNullOrEmpty(result.Weight))
                {
                    result.Status = "NoData";
                    result.Message = "無法解析資料欄位";
                }
                else
                {
                    result.Message = "查詢成功";
                }

            }
            catch (Exception ex)
            {
                result.Status = "ParseError";
                result.Message = $"解析資料失敗：{ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 清理文字（移除 &nbsp; 和多餘空白）
        /// </summary>
        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text
                .Replace("&nbsp;", "")
                .Replace("\r\n", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        public async Task<XSSFWorkbook> ExportExcel(AccsQueryRequest request)
        {
            try
            {
                // 執行查詢
                var queryResult = await QueryAsync(request);

                if (queryResult.status == "error")
                {
                    throw new Exception(queryResult.msg);
                }

                var data = queryResult.ReturnObject as List<AccsQueryResult>;
                if (data == null || data.Count == 0)
                {
                    throw new Exception("查無資料可匯出");
                }

                // 檢查是否有 Session 過期的資料
                if (data.Any(x => x.Status == "SessionExpired"))
                {
                    throw new Exception("登入已過期，請重新登入");
                }

                // 建立 Excel
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Accs關貿空運查詢結果");

                // 建立樣式
                var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
                var dataStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);
                var centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
                var rightStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Right);

                // 建立標題列
                var headerRow = sheet.CreateRow(0);
                NpoiCell.CreateCell(headerRow, 0, "項次", headerStyle);
                NpoiCell.CreateCell(headerRow, 1, "主號", headerStyle);
                NpoiCell.CreateCell(headerRow, 2, "主號總件數", headerStyle);
                NpoiCell.CreateCell(headerRow, 3, "本批件數", headerStyle);
                NpoiCell.CreateCell(headerRow, 4, "毛重", headerStyle);
                NpoiCell.CreateCell(headerRow, 5, "航機班次", headerStyle);
                NpoiCell.CreateCell(headerRow, 6, "進口日期", headerStyle);
                NpoiCell.CreateCell(headerRow, 7, "狀態", headerStyle);

                // 填入資料
                int rowIndex = 1;
                foreach (var item in data)
                {
                    var dataRow = sheet.CreateRow(rowIndex);

                    NpoiCell.CreateIntCell(dataRow, 0, rowIndex, rightStyle);
                    NpoiCell.CreateCell(dataRow, 1, item.MawbNo ?? "", dataStyle);
                    NpoiCell.CreateIntCell(dataRow, 2, item.TotalHwb ?? "", rightStyle);
                    NpoiCell.CreateIntCell(dataRow, 3, item.Total ?? "", rightStyle);
                    NpoiCell.CreateDoubleCell(dataRow, 4, item.Weight ?? "", rightStyle);
                    NpoiCell.CreateCell(dataRow, 5, item.FlightNo ?? "", centerStyle);
                    NpoiCell.CreateCell(dataRow, 6, item.ImportDate ?? "", centerStyle);
                    NpoiCell.CreateCell(dataRow, 7, item.Message ?? "", dataStyle);

                    rowIndex++;
                }

                // 自動調整欄寬
                for (int i = 0; i < 8; i++)
                {
                    sheet.AutoSizeColumn(i);
                    // 設定最小寬度
                    if (sheet.GetColumnWidth(i) < 3000)
                    {
                        sheet.SetColumnWidth(i, 3000);
                    }
                }

                return workbook;
            }
            catch (Exception ex)
            {
                throw new Exception($"匯出 Excel 失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 從 HTML 中提取 Token
        /// </summary>
        private string ExtractToken(string html)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var tokenInput = doc.DocumentNode.SelectSingleNode("//input[@name='org.apache.struts.taglib.html.TOKEN']");
                if (tokenInput != null)
                {
                    return tokenInput.GetAttributeValue("value", "");
                }

                // 使用正則表達式作為備用方案
                var match = Regex.Match(html, @"name=""org\.apache\.struts\.taglib\.html\.TOKEN""\s+value=""([^""]+)""");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 取得 Session ID
        /// </summary>
        private string GetSessionId()
        {
            var session = HttpContext.Current?.Session;
            if (session == null) return null;

            var cookieContainer = session[SESSION_COOKIE_CONTAINER] as CookieContainer;
            if (cookieContainer == null) return null;

            var cookies = cookieContainer.GetCookies(new Uri(BASE_URL));
            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name.Contains("JSESSIONID") || cookie.Name.Contains("SESSION"))
                {
                    return cookie.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// 檢查 Session 是否過期（檢查 HTML 中的 userID 是否為空）
        /// </summary>
        private bool IsSessionExpired(string html)
        {
            try
            {
                // 如果頁面包含「登出」關鍵字，代表使用者已登入 → Session 未過期
                if (html.Contains("登出") || html.Contains("登出系統"))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// 清除 Session Cookie
        /// </summary>
        private void ClearSession()
        {
            var session = HttpContext.Current?.Session;
            if (session != null)
            {
                session[SESSION_COOKIE_CONTAINER] = null;
            }
        }

        /// <summary>
        /// 連結參數類別
        /// </summary>
        private class AccsLinkParameters
        {
            public string MawbNo { get; set; }
            public string VoyageFlightNo { get; set; }
            public string EstArrivalDate { get; set; }
            public string FlightDate { get; set; }
            public string QryMawbNo { get; set; }
            public string QryVoyageFlightNo { get; set; }
            public string QryFlightDate { get; set; }
            public string QryEstArrivalDate { get; set; }
            public string QryAbnormalMark { get; set; }
            public string QryCarrier { get; set; }
            public string QrySort { get; set; }
        }
    }
}
