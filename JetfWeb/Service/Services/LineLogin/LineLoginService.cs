using Dapper;
using Newtonsoft.Json;
using Service.Models;
using Service.Models.LineLogin;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.LineLogin
{
    public class LineLoginService : _BaseService
    {
        public LineLoginService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        // LINE Login Channel ID
        private readonly string clientId = "2006503112";     
        // LINE Login Channel Secret
        private readonly string clientSecret = "afbc926e74b37ab5b97bad87e87c317f"; 
        // Callback URL
        private readonly string redirectUri = "https://ws.jet-f.com/JETF/LineLogin/PhoneBind";

        // 測試LINE Login Channel ID
        //private readonly string clientId = "2006592376";
        // 測試 LINE Login Channel Secret
        //private readonly string clientSecret = "546c903591b15159147a2ea5b19f6a9a";
        //測試 Callback URL
        //private readonly string redirectUri = "https://localhost:44347/LineLogin/PhoneBind";

        public LineTokenResponse GetAccessToken(string authorizationCode)
        {
            using (var client = new HttpClient())
            {
                var requestUrl = "https://api.line.me/oauth2/v2.1/token";

                //POST 請求的內容
                var content = new StringContent(
                    $"grant_type=authorization_code" +
                    $"&code={authorizationCode}" +
                    $"&redirect_uri={redirectUri}" +
                    $"&client_id={clientId}" +
                    $"&client_secret={clientSecret}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                // 發送請求
                var response = client.PostAsync(requestUrl, content).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;

                // 檢查回應是否成功
                if (!response.IsSuccessStatusCode)
                {
                    //Console.WriteLine("Error retrieving access token: " + responseContent);
                    return new LineTokenResponse();
                }

                var result = JsonConvert.DeserializeObject<LineTokenResponse>(responseContent);

                return result;
            }
        }

        public LineUserProfile GetUserProfile(string accessToken, string idToken)
        {
            using (var client = new HttpClient())
            {
                // 設定 Authorization 標頭，將 Access Token 放入其中
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // 向 LINE Profile API 發送請求
                var response = client.GetAsync("https://api.line.me/v2/profile").Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                {
                    //Console.WriteLine("Error retrieving user profile: " + responseContent);
                    return new LineUserProfile();
                }

                // 解析 JSON 回應，將其轉換為 LineUserProfile 物件
                var userProfile = JsonConvert.DeserializeObject<LineUserProfile>(responseContent);

                //userProfile.Email = GetEmailFromIdToken(idToken);

                return userProfile;
            }
        }

        public string GetPhone(string userId)
        {
            var sqlQuery = "SELECT Phone FROM jetf.[dbo].[LineUserProfile] WHERE UserId = @UserId";

            var phone = conn.QueryFirstOrDefault<string>(sqlQuery, new { UserId = userId });
            return phone;
        }

        /// <summary>
        /// 更新Line用戶資料
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public int UpsertLineUserProfile(LineUserProfile profile)
        {
            //沒有Id 就不新增
            if (string.IsNullOrEmpty(profile.UserId))
                return 0;

            string upsertSql = @"
                IF EXISTS (SELECT 1 FROM jetf.dbo.LineUserProfile WHERE UserId = @UserId)
                BEGIN
                    UPDATE jetf.dbo.LineUserProfile 
                    SET DisplayName = @DisplayName, PictureUrl = @PictureUrl, StatusMessage = @StatusMessage,IsUnblocked='1',UpdateDateTime=getdate()
                    WHERE UserId = @UserId
                END
                ELSE
                BEGIN
                    INSERT INTO jetf.dbo.LineUserProfile 
                    (UserId, DisplayName, PictureUrl, StatusMessage, IsUnblocked) 
                    VALUES 
                    (@UserId, @DisplayName, @PictureUrl, @StatusMessage, '1')
                END";

            var parameters = new
            {
                UserId = profile.UserId,
                DisplayName = profile.DisplayName,
                PictureUrl = profile.PictureUrl,
                StatusMessage = profile.StatusMessage
            };

            return conn.Execute(upsertSql, parameters);
        }

        public ResponseModel UpdatePhone(string userId, string phone)
        {
            string sql = "UPDATE jetf.[dbo].[LineUserProfile] SET Phone = @Phone,UpdateDateTime=getdate() WHERE UserId = @UserId";

            var parameters = new
            {
                UserId = userId,
                Phone = phone,
            };

            conn.Execute(sql, parameters);

            var response = new ResponseModel()
            {
                msg = "綁定手機成功"
            };
            return response;
        }

        /// <summary>
        /// 取得Email
        /// </summary>
        /// <param name="idToken"></param>
        /// <returns></returns>
        public string GetEmailFromIdToken(string idToken)
        {
            var handler = new JwtSecurityTokenHandler();

            // 驗證 ID Token 的格式
            if (!handler.CanReadToken(idToken))
            {
                return string.Empty;
            }

            // 讀取並解析 ID Token
            var jsonToken = handler.ReadToken(idToken) as JwtSecurityToken;

            // 驗證並取得 "email" 權限的內容
            var email = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value;

            return email;
        }

    }
}
