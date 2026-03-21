using Dapper;
using HtmlAgilityPack;
using Service.Services.Job.TactWebClientJob.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Service.Services.Job.TactWebClientJob
{
    public class TactWebClientJobService : _BaseService
    {
        public async Task<bool> RunTactWebClientJobAsync()
        {
            // 建立 HttpClientHandler 以保持 Cookie
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AllowAutoRedirect = true
            };

            using (var client = new HttpClient(handler))
            {
                try
                {
                    // 設定基本 headers
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");

                    // 步驟 1: 登入
                    bool loginSuccess = await Login(client, "3027", "24951752");

                    if (!loginSuccess)
                    {
                        return false;
                    }

                    // 步驟 2: 查詢資料
                    var startDate = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd");
                    var endDate = DateTime.Now.ToString("yyyy/MM/dd");
                    string htmlResult = await QueryData(client, "I", startDate, endDate);

                    // 步驟 3: 解析 HTML 資料
                    var list = ParseHtmlData(htmlResult);

                    //步驟 4: 申報件數大於2的要找出併分提單號
                    var merges = list
                        .Where(r => !string.IsNullOrWhiteSpace(r.TrackingNo) && r.DeclaredQty > 1)
                        .ToList();

                    // 連線
                    await conn.OpenAsync();

                    foreach (var r in merges)
                    {
                        //取得併分提單號
                        var result = await GetMergeTrackingNo(r.TrackingNo);
                        if (result.Any())
                        {
                            foreach (var trackingNo in result)
                            {
                                list.Add(new TactRelnonoutModel()
                                {
                                    TrackingNo = trackingNo,
                                    DeclaredQty = 0,
                                });
                            }
                        }
                    }

                    conn.Close();

                    //步驟 5: 寫入資料庫
                    await SaveToDatabase(list);
                }
                catch (Exception ex)
                {
                    return false;
                }
                finally 
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }

                return true;
            }
        }


        /// <summary>
        /// 登入網站
        /// </summary>
        private async Task<bool> Login(HttpClient client, string username, string password)
        {
            try
            {
                var loginUrl = "https://www.tactl.com/login.php";

                // 準備登入表單資料
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("acct_id", username),
                    new KeyValuePair<string, string>("acct_pw", password)
                });

                // 發送 POST 請求
                var response = await client.PostAsync(loginUrl, formData);
                var responseContent = await response.Content.ReadAsStringAsync();

                // 檢查是否登入成功（可以根據回應內容判斷）
                // 這裡假設登入成功後會設定 Cookie 或轉向到其他頁面
                if (response.IsSuccessStatusCode)
                {
                    // 可以檢查回應內容是否包含登入成功的標記
                    // 例如：if (responseContent.Contains("登入成功") || !responseContent.Contains("登入失敗"))
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 查詢資料
        /// </summary>
        /// <param name="client">HttpClient 實例</param>
        /// <param name="ieType">進出口類型：I=進口, E=出口</param>
        /// <param name="startDate">開始日期 (格式: yyyy/MM/dd)</param>
        /// <param name="endDate">結束日期 (格式: yyyy/MM/dd)</param>
        private async Task<string> QueryData(HttpClient client, string ieType, string startDate, string endDate)
        {
            try
            {
                var queryUrl = "https://www.tactl.com/ehuweb/ehu_relnonout_query.php";

                // 準備查詢表單資料
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("ie_rad", ieType),
                    new KeyValuePair<string, string>("sdate", startDate),
                    new KeyValuePair<string, string>("edate", endDate)
                });

                // 發送 POST 請求
                var response = await client.PostAsync(queryUrl, formData);
                response.EnsureSuccessStatusCode();

                // 讀取回應內容
                var responseContent = await response.Content.ReadAsStringAsync();

                return responseContent;
            }
            catch (Exception ex)
            {
                return $"查詢時發生錯誤: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析貨物資料 HTML
        /// </summary>
        /// <param name="html">HTML 內容</param>
        /// <returns>貨物資料列表</returns>
        private List<TactRelnonoutModel> ParseHtmlData(string html)
        {
            var list = new List<TactRelnonoutModel>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // 尋找 id_contain div 中的 table
            var tableNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='id_contain']//table");

            if (tableNode == null)
            {
                return list;
            }

            // 取得所有的 tr (跳過第一個標題列)
            var rows = tableNode.SelectNodes(".//tr");

            if (rows == null || rows.Count <= 1)
            {
                return list;
            }

            // 從第二列開始解析 (第一列是標題)
            for (int i = 1; i < rows.Count; i++)
            {
                var cells = rows[i].SelectNodes(".//td");

                if (cells == null || cells.Count < 9)
                {
                    continue;
                }

                var model = new TactRelnonoutModel
                {
                    BagNumber = GetCellText(cells[0]),
                    MainNumber = GetCellText(cells[1]),
                    DeclarationNumber = GetCellText(cells[2]),
                    TrackingNo = GetCellText(cells[3]),
                    DeclaredQty = ParseInt(GetCellText(cells[4])),
                    InboundQty = ParseInt(GetCellText(cells[5])),
                    OutboundQty = ParseInt(GetCellText(cells[6])),
                    InboundTime = GetCellText(cells[7]),
                    CargoStatus = GetCellText(cells[8])
                };

                list.Add(model);
            }

            return list;
        }

        /// <summary>
        /// 取得儲存格文字內容
        /// </summary>
        private string GetCellText(HtmlNode cell)
        {
            return cell?.InnerText?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 將字串轉換為整數
        /// </summary>
        private int ParseInt(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return 0;
        }

        /// <summary>
        /// 儲存資料到資料庫
        /// </summary>
        /// <param name="list">TACT 放行未出倉資料列表</param>
        private async Task SaveToDatabase(List<TactRelnonoutModel> list)
        {
            if (list == null || !list.Any())
            {
                return;
            }

            try
            {
                await conn.OpenAsync();

                // 取得所有非空的 TrackingNo
                var trackingNos = list
                    .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                    .Select(x => x.TrackingNo)
                    .Distinct()
                    .ToList();

                if (!trackingNos.Any())
                    return;

                // 一次查出已存在的 TrackingNo
                var existingSql = @"
                    SELECT TrackingNo 
                    FROM [jetf].[dbo].[TactRelnonout]
                    WHERE TrackingNo IN @TrackingNos
                    ";

                var existing = (await conn.QueryAsync<string>(existingSql, new { TrackingNos = trackingNos }))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // 篩出要新增的資料
                var toInsert = list
                    .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo) && !existing.Contains(x.TrackingNo))
                    .ToList();

                if (toInsert.Count == 0)
                    return;

                // 批次新增資料
                var insertSql = @"
                        INSERT INTO [jetf].[dbo].[TactRelnonout] 
                        (TrackingNo, DeclaredQty)
                        VALUES (@TrackingNo, @DeclaredQty)
                    ";

                await conn.ExecuteAsync(insertSql, toInsert);
            }
            catch (Exception ex)
            {
                // 記錄錯誤或拋出例外
                throw new Exception($"儲存資料到資料庫時發生錯誤: {ex.Message}", ex);
            }
            finally
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 取得併分提單號
        /// </summary>
        /// <param name="trackingNo">分提單號</param>
        /// <returns>並分提單號列表</returns>
        private async Task<List<string>> GetMergeTrackingNo(string trackingNo)
        {
            if (string.IsNullOrWhiteSpace(trackingNo))
            {
                return new List<string>();
            }

            // 呼叫 Stored Procedure
            var result = await conn.QueryAsync<string>(
                "jetf.[dbo].[USP_GetMergeTrackingNo]",
                new { TrackingNo = trackingNo },
                commandType: System.Data.CommandType.StoredProcedure
            );

            return result?.ToList() ?? new List<string>();
        }
    }
}
