using JETFWebAPI.Models.LineMessage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using NLog;
using JETFWebAPI.Enum;
using Dapper;

namespace JETFWebAPI.Services
{
    public class LineMessageService
    {
        Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private SqlConnection conn;

        private readonly string lineMessageToken = "f93v5z2VU4LOdd0MCcFYuU0SBxucVSVpiv3fWVATRjEYpSFs0wGo94FdzfxMQt4Oxvfn6xNaTtqdEDtUzDQoVnlhwBGY5XeCLSdhbPJOacz013ieEdse3NerN3tOeq91XZmRAQdW//Pttc+BUYHuYgdB04t89/1O/w1cDnyilFU=";

        /// <summary>
        /// 建構式
        /// </summary>
        public LineMessageService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);

        }

        ~LineMessageService()
        {
            conn.Dispose();
        }

        public void LineMessageWebhookAsync(LineWebhookEvent request)
        {
            try
            {
                //新增Line Webhook資料
                InsertLineMessageWebhook(request);

                LineUserProfile(request);
            }
            catch (Exception ex)
            {
                logger.Debug(ex.Message);
            }
        }

        public void LineUserProfile(LineWebhookEvent request)
        {
            var type = request.Events[0].Type;

            var userId = request.Events[0].Source?.UserId;

            if (type == WebhookEventType.follow)
            {
                //追蹤
                var profile = GetLineUserProfile(userId);
                UpsertLineUserProfile(profile);
            }

            if (type == WebhookEventType.unfollow)
            {
                //不追蹤
                UpdateIsUnblocked(userId, false);
            }
        }

        /// <summary>
        /// 取得LineMessageProfile
        /// </summary>
        public LineUserProfile GetLineUserProfile(string userId)
        {
            using (var client = new HttpClient())
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lineMessageToken);
                var response = client.GetAsync($"https://api.line.me/v2/bot/profile/{userId}").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<LineUserProfile>(content);
            }
        }

        private int InsertLineMessageWebhook(LineWebhookEvent request)
        {
            string strSql = @"
                    INSERT INTO jetf.dbo.LineMessageWebhook 
                    (Destination, EventType, IsUnblocked, WebhookEventId, IsRedelivery, Timestamp, SourceType, UserId, ReplyToken, Mode) 
                    VALUES 
                    (@Destination, @EventType, @IsUnblocked, @WebhookEventId, @IsRedelivery, @Timestamp, @SourceType, @UserId, @ReplyToken, @Mode)";

            var parameters = new
            {
                Destination = request.Destination,
                EventType = request.Events[0].Type.ToString(),
                IsUnblocked = request.Events[0].Follow?.IsUnblocked,
                WebhookEventId = request.Events[0].WebhookEventId,
                IsRedelivery = request.Events[0].DeliveryContext?.IsRedelivery,
                Timestamp = request.Events[0].Timestamp,
                SourceType = request.Events[0].Source?.Type,
                UserId = request.Events[0].Source?.UserId,
                ReplyToken = request.Events[0].ReplyToken,
                Mode = request.Events[0].Mode
            };

            return conn.Execute(strSql, parameters);
        }

        private int UpsertLineUserProfile(LineUserProfile profile)
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

        /// <summary>
        /// 更新IsUnblocked
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="isUnblocked"></param>
        /// <returns></returns>
        private int UpdateIsUnblocked(string userId, bool isUnblocked) { 
            string strSql = @"
                    UPDATE jetf.dbo.LineUserProfile
                    SET IsUnblocked = @IsUnblocked,UpdateDateTime=getdate()
                    WHERE UserId = @UserId";

            var parameters = new
            {
                UserId = userId,
                IsUnblocked = isUnblocked,
            };

            return conn.Execute(strSql, parameters);
        }

        private List<string> GetLineMessageWebhook() 
        {
            var sqlQuery = "select UserId from [jetf].[dbo].[LineMessageWebhook] where EventType='follow'" +
                    "group by UserId";

            return conn.Query<string>(sqlQuery).ToList();
        }


        public void UpdateLineUserProfile() 
        {
            var userIds = GetLineMessageWebhook();

            foreach (var userId in userIds)
            {
                var profile = GetLineUserProfile(userId);

                UpsertLineUserProfile(profile);
            }
        }
    }
}