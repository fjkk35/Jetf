using HtmlAgilityPack;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.Tact.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Dapper;

namespace Service.Services.Tact
{
    public partial class TactService : _BaseService
    {
        public TactService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        private const string BASE_URL = "https://www.tactl.com";
        private const string LOGIN_URL = BASE_URL + "/login.php";
        private const string QUERY_URL = BASE_URL + "/ehuweb/ehu_hwb_query.php";
        private const string MAIN_QUERY_URL = BASE_URL + "/ehuweb/ehu_mwb_query.php";
        private const string BAG_QUERY_URL = BASE_URL + "/ehuweb/ehu_bagno_query.php";
        public const string SESSION_COOKIE_CONTAINER = "Tact_CookieContainer";

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
                Timeout = TimeSpan.FromSeconds(60)
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7");

            return httpClient;
        }

        /// <summary>
        /// 登入 Tact 系統
        /// </summary>
        public async Task<ResponseModel> LoginAsync(TactLoginRequest request)
        {
            try
            {
                using (var httpClient = GetHttpClient())
                {
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("acct_id", request.AcctId ?? "3027"),
                        new KeyValuePair<string, string>("acct_pw", request.AcctPw ?? "24951752"),
                        new KeyValuePair<string, string>("lbutton", "")
                    });

                    var response = await httpClient.PostAsync(LOGIN_URL, formContent);
                    var html = await response.Content.ReadAsStringAsync();

                    if (html.Contains("登入失敗") || html.Contains("密碼錯誤") || html.Contains("帳號錯誤") || html.Contains("login.php"))
                    {
                        return new ResponseModel("登入失敗：帳號或密碼錯誤");
                    }

                    return new ResponseModel();
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"登入錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢 Tact 資料
        /// </summary>
        public async Task<ResponseModel> QueryAsync(TactQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.HwbNoList))
                {
                    return new ResponseModel("請輸入查詢資料");
                }

                var hwbNoList = request.HwbNoList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (!hwbNoList.Any())
                {
                    return new ResponseModel("請輸入查詢資料");
                }

                var results = new List<TactHwbModel>();

                using (var httpClient = GetHttpClient())
                {
                    foreach (var hwbNo in hwbNoList)
                    {
                        try
                        {
                            var resultList = await QuerySingleAsync(httpClient, hwbNo);
                            results.AddRange(resultList);
                        }
                        catch (Exception ex)
                        {
                            results.Add(new TactHwbModel
                            {
                                TrackingNo = hwbNo,
                                Remark = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                return new ResponseModel(results);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆資料
        /// </summary>
        private async Task<List<TactHwbModel>> QuerySingleAsync(HttpClient httpClient, string hwbNo)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ie_rad", "I"),
                new KeyValuePair<string, string>("hwb_no", hwbNo)
            });

            var response = await httpClient.PostAsync(QUERY_URL, formContent);
            var html = await response.Content.ReadAsStringAsync();

            return ParseHtml(hwbNo, html);
        }

        /// <summary>
        /// 解析 HTML 取得資料
        /// </summary>
        private List<TactHwbModel> ParseHtml(string hwbNo, string htmlContent)
        {
            List<TactHwbModel> list = new List<TactHwbModel>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 取得第2個 table
            HtmlNode table = doc.DocumentNode.SelectSingleNode("(//table)[2]");
            if (table != null)
            {
                list.Add(GetTableData(hwbNo, table));
            }

            // 取得第3個 table
            HtmlNode table2 = doc.DocumentNode.SelectSingleNode("(//table)[3]");
            if (table2 != null)
            {
                list.Add(GetTableData(hwbNo, table2));
            }

            // 如果沒有資料，回傳一筆查無資料的記錄
            if (!list.Any())
            {
                list.Add(new TactHwbModel
                {
                    TrackingNo = hwbNo,
                    Remark = "查無資料"
                });
            }

            return list;
        }

        /// <summary>
        /// 取得 table 資料
        /// </summary>
        private TactHwbModel GetTableData(string trackingNo, HtmlNode table)
        {
            TactHwbModel model = new TactHwbModel();
            model.TrackingNo = trackingNo;

            if (table != null)
            {
                foreach (HtmlNode row in table.SelectNodes(".//tr"))
                {
                    // 提取單元格內容
                    HtmlNodeCollection cells = row.SelectNodes("td");
                    if (cells != null && cells.Count >= 2)
                    {
                        // 提取第二個單元格的內容
                        string value = cells[1].InnerText.Trim();
                        switch (cells[0].InnerText.Trim())
                        {
                            case "主提單號":
                                model.MainNumber = value;
                                break;
                            case "分提單號":
                                model.TrackingNo = value;
                                break;
                            case "報關類別":
                                model.DeclType = value;
                                break;
                            case "併袋號":
                                model.BagNumber = value;
                                break;
                            case "報單號碼":
                                model.DeclNo = value;
                                break;
                            case "通關方式":
                                model.ClearanceType = value;
                                break;
                            case "申報件數":
                                model.Piece = value;
                                break;
                            case "進倉件數":
                                model.GciPiece = value;
                                break;
                            case "出倉件數":
                                model.GcoPiece = value;
                                break;
                            case "申報重量":
                                model.Weight = value;
                                break;
                            case "進倉重量":
                                model.GciWeight = value;
                                break;
                            case "進倉時間":
                                model.GciDate1 = value;
                                break;
                            case "出倉時間":
                                model.GcoDate1 = value;
                                break;
                            case "航機班次":
                                model.FlightNo = value;
                                break;
                            case "更改後報單":
                                model.UpdateDecl = value;
                                break;
                            case "稅費金額":
                                model.Amount = value;
                                break;
                            case "備註":
                                model.Remark = value;
                                break;
                        }
                    }
                    else
                    {
                        model.Remark2 = row.InnerText;
                    }
                }
            }

            return model;
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        public async Task<IWorkbook> ExportExcel(TactQueryRequest request)
        {
            // 先查詢資料
            var queryResult = await QueryAsync(request);

            if (queryResult.status != Status.success || queryResult.ReturnObject == null)
            {
                throw new Exception(queryResult.msg ?? "查詢失敗");
            }

            var results = queryResult.ReturnObject as List<TactHwbModel>;

            // 建立 Excel
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Tact查詢結果");

            // 建立樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            // 建立表頭
            string[] headers = new string[]
            {
                "主提單號", "分提單號", "報關類別", "併袋號", "報單號碼", "通關方式", 
                "申報件數", "進倉件數", "出倉件數", "申報重量", "進倉重量", 
                "進倉時間", "出倉時間", "航機班次", "更改後報單", "稅費金額", "備註", "備註2"
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

                NpoiCell.CreateCell(dataRow, 0, item.MainNumber ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.TrackingNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.DeclType ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.BagNumber ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.DeclNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 5, item.ClearanceType ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 6, item.Piece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 7, item.GciPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 8, item.GcoPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 9, item.Weight ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 10, item.GciWeight ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 11, item.GciDate1 ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 12, item.GcoDate1 ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 13, item.FlightNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 14, item.UpdateDecl ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 15, item.Amount ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 16, item.Remark ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 17, item.Remark2 ?? "", dataStyle);
            }

            return workbook;
        }

    }
}
