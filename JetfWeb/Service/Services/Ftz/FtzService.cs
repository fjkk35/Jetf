using HtmlAgilityPack;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.Ftz.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Service.Services.Ftz
{
    public partial class FtzService : _BaseService
    {
        private const string BASE_URL = "https://ehu.ftz.com.tw/FTZEHU";
        private const string LOGIN_URL = BASE_URL + "/login.do";
        private const string QUERY_URL = BASE_URL + "/HWBQUERY_01.do";
        private const string MAIN_QUERY_URL = BASE_URL + "/MWBQUERY_01.do";
        private const string NOGCI_QUERY_URL = BASE_URL + "/NOGCIQUERY_01.do";
        //併袋號
        private const string EXPBAGNO_QUERY_URL = BASE_URL + "/EXPBAGNOQUERY_01.do";
        public const string SESSION_COOKIE_CONTAINER = "Ftz_CookieContainer";

        /// <summary>
        /// 取得帶有 Session 保持的 HttpClient
        /// </summary>
        private HttpClient GetHttpClient()
        {
            var session = HttpContext.Current?.Session;
            if (session == null)
            {
                return CreateHttpClient(new CookieContainer());
            }

            var cookieContainer = session[SESSION_COOKIE_CONTAINER] as CookieContainer;
            if (cookieContainer == null)
            {
                cookieContainer = new CookieContainer();
                session[SESSION_COOKIE_CONTAINER] = cookieContainer;
            }

            return CreateHttpClient(cookieContainer);
        }

        /// <summary>
        /// 建立 HttpClient
        /// </summary>
        private static HttpClient CreateHttpClient(CookieContainer cookieContainer)
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

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7");

            return httpClient;
        }

        /// <summary>
        /// 登入 Ftz 系統
        /// </summary>
        public async Task<ResopnseModel> LoginAsync(FtzLoginRequest request)
        {
            try
            {
                using (var httpClient = GetHttpClient())
                {
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("userId", request.UserId ?? "0335"),
                        new KeyValuePair<string, string>("userPd", request.UserPd ?? "24951752")
                    });

                    var response = await httpClient.PostAsync(LOGIN_URL, formContent);
                    var html = await response.Content.ReadAsStringAsync();

                    if (html.Contains("登入失敗") || html.Contains("密碼錯誤") || html.Contains("帳號錯誤"))
                    {
                        return new ResopnseModel("登入失敗：帳號或密碼錯誤");
                    }

                    return new ResopnseModel();
                }
            }
            catch (Exception ex)
            {
                return new ResopnseModel($"登入錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢 Ftz 資料
        /// </summary>
        public async Task<ResopnseModel> QueryAsync(FtzQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.HwbqList))
                {
                    return new ResopnseModel("請輸入查詢資料");
                }

                var hwbqList = request.HwbqList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (!hwbqList.Any())
                {
                    return new ResopnseModel("請輸入查詢資料");
                }

                var results = new List<FtzQueryResult>();

                using (var httpClient = GetHttpClient())
                {
                    foreach (var hwbq in hwbqList)
                    {
                        try
                        {
                            var result = await QuerySingleAsync(httpClient, hwbq);
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            results.Add(new FtzQueryResult
                            {
                                Hwbq = hwbq,
                                Remark = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                return new ResopnseModel
                {
                    ReturnObject = results
                };
            }
            catch (Exception ex)
            {
                return new ResopnseModel($"查詢錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆資料
        /// </summary>
        private async Task<FtzQueryResult> QuerySingleAsync(HttpClient httpClient, string hwbq)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ieType", "I"),
                new KeyValuePair<string, string>("hwbq", hwbq),
                new KeyValuePair<string, string>("orghwbq", ""),
                new KeyValuePair<string, string>("mwbq", "")
            });

            var response = await httpClient.PostAsync(QUERY_URL, formContent);
            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var model = new FtzQueryResult
            {
                Hwbq = hwbq
            };

            // 解析第一個 table (主號)
            var mwbNode = doc.DocumentNode.SelectSingleNode("//input[@id='mwb']");
            model.Mwb = mwbNode?.GetAttributeValue("value", "");
            model.Remark = doc.DocumentNode.InnerText.Contains("無資料") ? "查無分號" : "";

            // 解析第二個 table (詳細資料)
            var table = doc.DocumentNode.SelectSingleNode("(//table)[2]");


            if (table != null)
            {
                model.DeclNo = GetInputIdValue(table, "declNo");
                model.DeclType = GetInputNameValue(table, "declType");
                model.ClearanceType = GetInputIdValue(table, "clearanceType");
                model.BoxNo = GetInputIdValue(table, "boxNo");
                model.IE = GetInputIdValue(table, "ie");
                model.ReleaseTime = GetInputIdValue(table, "releaseTime");
                model.BoxNoExpressId = GetInputIdValue(table, "boxNoExpressId");
                model.Piece = GetInputIdValue(table, "piece");
                model.Weight = GetInputIdValue(table, "weight");
                model.GciDate1 = GetInputIdValue(table, "gciDate1");
                model.BoxNoExpressCName = GetInputIdValue(table, "boxNoExpressCName");
                model.GciPiece = GetInputIdValue(table, "gciPiece");
                model.GciWeight = GetInputIdValue(table, "gciWeight");
                model.GcoDate1 = GetInputIdValue(table, "gcoDate1");
                model.GcoPiece = GetInputIdValue(table, "gcoPiece");
            }


            return model;
        }

        /// <summary>
        /// 取得 input 元素的 value（根據 id）
        /// </summary>
        private string GetInputIdValue(HtmlNode table, string id)
        {
            string value = "";
            HtmlNode htmlNode = table.SelectSingleNode($".//*[@id='{id}']");
            if (htmlNode != null)
            {
                value = htmlNode.GetAttributeValue("value", string.Empty);
            }
            return value;
        }

        /// <summary>
        /// 取得 input 元素的 value（根據 name）
        /// </summary>
        private string GetInputNameValue(HtmlNode table, string name)
        {
            string value = "";
            HtmlNode htmlNode = table.SelectSingleNode($".//*[@name='{name}']");
            if (htmlNode != null)
            {
                value = htmlNode.GetAttributeValue("value", string.Empty);
            }
            return value;
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        public async Task<IWorkbook> ExportExcel(FtzQueryRequest request)
        {
            // 先查詢資料
            var queryResult = await QueryAsync(request);

            if (queryResult.status != Status.success || queryResult.ReturnObject == null)
            {
                throw new Exception(queryResult.msg ?? "查詢失敗");
            }

            var results = queryResult.ReturnObject as List<FtzQueryResult>;

            // 建立 Excel
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Ftz查詢結果");

            // 建立樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            // 建立表頭
            string[] headers = new string[]
            {
                 "主號","分提單號", "報單號碼", "類別", "通關方式", "箱號",
                "進出口別", "放行時間", "公司編號", "申報件數", "申報重量",
                "進倉時間", "公司名稱", "進倉件數", "進倉重量", "出倉時間", "出倉件數","錯誤訊息"
            };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            // 設定欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, 4000);
            }

            // 填入資料
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                IRow dataRow = sheet.CreateRow(i + 1);

                NpoiCell.CreateCell(dataRow, 0, item.Mwb ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.Hwbq ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.DeclNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.DeclType ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.ClearanceType ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 5, item.BoxNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 6, item.IE ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 7, item.ReleaseTime ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 8, item.BoxNoExpressId ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 9, item.Piece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 10, item.Weight ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 11, item.GciDate1 ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 12, item.BoxNoExpressCName ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 13, item.GciPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 14, item.GciWeight ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 15, item.GcoDate1 ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 16, item.GcoPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 17, item.Remark ?? "", dataStyle);
            }

            return workbook;
        }
    }
}
