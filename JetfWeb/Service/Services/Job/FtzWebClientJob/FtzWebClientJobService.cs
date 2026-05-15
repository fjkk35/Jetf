using Dapper;
using Newtonsoft.Json;
using Service.Services.Job.FtzWebClientJob.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Job.FtzWebClientJob
{
    public class FtzWebClientJobService : _BaseService
    {
        public FtzWebClientJobService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public async Task<bool> RunFtzWebClientJobAsync()
        {
            // 建立 HttpClientHandler 以保留 Cookie
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
                    bool loginSuccess = await Login(client, "0335", "24951752");

                    if (!loginSuccess)
                    {
                        return false;
                    }

                    // 步驟 2: 查詢資料
                    var startDate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
                    var endDate = DateTime.Now.ToString("yyyyMMdd");
                    var jsonResult = await QueryData(client, "I", "0335", startDate, endDate);

                    if (string.IsNullOrEmpty(jsonResult))
                    {
                        return false;
                    }

                    // 步驟 3: 解析 JSON 資料
                    var data = ParseJsonData(jsonResult);

                    if (data == null || data.rows == null || !data.rows.Any())
                    {
                        return true; // 沒有資料也視為成功
                    }

                    // 步驟 4: 申報件數大於 2 的資料要找出併分提單號
                    var merges = data?.rows
                        .Where(r => !string.IsNullOrWhiteSpace(r.hwb) && ParseInt(r.piece) > 1)
                        .ToList();

                    // 連線資料庫
                    await conn.OpenAsync();

                    foreach (var r in merges)
                    {
                        // 取得併分提單號
                        var result = await GetMergeTrackingNo(r.hwb);
                        if (result.Any())
                        {
                            foreach (var trackingNo in result)
                            {
                                data.rows.Add(new Row()
                                {
                                    hwb = trackingNo,
                                    piece = "0",
                                });
                            }
                        }
                    }

                    conn.Close();

                    // 步驟 5: 寫入資料庫
                    await SaveToDatabase(data.rows);
                }
                catch (Exception ex)
                {
                    WriteJobErrorLog("遠雄查詢", ex);
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
        private async Task<bool> Login(HttpClient client, string userId, string userPd)
        {
            try
            {
                var loginUrl = "https://ehu.ftz.com.tw/FTZEHU/login.do";

                // 準備登入表單資料
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("userId", userId),
                    new KeyValuePair<string, string>("userPd", userPd)
                });

                // 發送 POST 請求
                var response = await client.PostAsync(loginUrl, formData);
                var responseContent = await response.Content.ReadAsStringAsync();

                // 檢查是否登入成功
                if (response.IsSuccessStatusCode)
                {
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
    /// <param name="ieType">進出口別，I=進口，E=出口</param>
    /// <param name="eid">公司 ID</param>
    /// <param name="startDate">開始日期，格式為 yyyyMMdd</param>
    /// <param name="endDate">結束日期，格式為 yyyyMMdd</param>
        private async Task<string> QueryData(HttpClient client, string ieType, string eid, string startDate, string endDate)
        {
            try
            {
                var queryUrl = "https://ehu.ftz.com.tw/FTZEHU/NORLEGCOQUERY_01.do";

        // 準備查詢參數
                var nd = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                var queryString = $"?ieType={ieType}&eid={eid}&d1={startDate}&d2={endDate}&_search=false&nd={nd}&rows=10000&page=1&sidx=&sord=asc";

        // 發送 GET 請求
                var response = await client.GetAsync(queryUrl + queryString);
                response.EnsureSuccessStatusCode();

        // 讀取回應內容
                var responseContent = await response.Content.ReadAsStringAsync();

                return responseContent;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        /// <summary>
    /// 解析 JSON 資料
        /// </summary>
    /// <param name="json">JSON 內容</param>
        /// <returns>FtzRelnonoutModel</returns>
        private FtzRelnonoutModel ParseJsonData(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<FtzRelnonoutModel>(json);
                return data;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 儲存資料到資料庫
        /// </summary>
        /// <param name="rows">FTZ 回傳的資料列</param>
        private async Task SaveToDatabase(List<Row> rows)
        {
            if (rows == null || !rows.Any())
            {
                return;
            }

            try
            {
                await conn.OpenAsync();

                // 取得所有非空的 TrackingNo (hwb)
                var trackingNos = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.hwb))
                    .Select(x => x.hwb)
                    .Distinct()
                    .ToList();

                if (!trackingNos.Any())
                    return;

                // 一次查出已存在的 TrackingNo
                var existingSql = @"
                    SELECT TrackingNo 
                    FROM [jetf].[dbo].[FtzRelnonout]
                    WHERE TrackingNo IN @TrackingNos
                    ";

                var existing = (await conn.QueryAsync<string>(existingSql, new { TrackingNos = trackingNos }))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // 篩出要新增的資料，並轉成資料庫欄位格式
                var toInsert = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.hwb) && !existing.Contains(x.hwb))
                    .Select(x => new
                    {
                        TrackingNo = x.hwb,
                        DeclaredQty = ParseInt(x.piece)
                    })
                    .ToList();

                if (toInsert.Count == 0)
                    return;

                // 批次新增資料
                var insertSql = @"
                        INSERT INTO [jetf].[dbo].[FtzRelnonout] 
                        (TrackingNo, DeclaredQty)
                        VALUES (@TrackingNo, @DeclaredQty)
                    ";

                await conn.ExecuteAsync(insertSql, toInsert);
            }
            catch (Exception ex)
            {
                // 保留原始例外並往外拋出
                throw new Exception($"FTZ 儲存資料到資料庫時發生錯誤: {ex.Message}", ex);
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
        /// <param name="trackingNo">主提單號</param>
        /// <returns>併分提單號清單</returns>
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

        /// <summary>
    /// 將字串轉成整數
        /// </summary>
        private int ParseInt(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return 0;
        }
    }
}
