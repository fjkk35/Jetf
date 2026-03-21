using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;

namespace Service.Services.Job.CainiaoNeedJob
{
    /// <summary>
    /// 菜鳥需委任Job
    /// </summary>
    public class CainiaoNeedJobService : _BaseService
    {
        private readonly TelegramBot _telegramBot;

        public CainiaoNeedJobService(TelegramBot telegramBot)
        {
            _telegramBot = telegramBot;
        }

        public async Task RunCainiaoNeedJob() 
        {
            DateTime date = DateTime.Now;
            string sendDate = date.ToString("yyyyMMdd");
            string sendTime = date.ToString("HHmmss");

            //菜鳥
            await SendLineCainiaoNeedAsync("CainiaoNeed", sendDate, sendTime);

            //蝦皮
            await SendLineShopeeNeedAsync("ShopeeNeed", sendDate, sendTime);

        }


        /// <summary>
        /// 菜鳥
        /// </summary>
        /// <param name="sendName"></param>
        /// <param name="sendDate"></param>
        /// <param name="sendTime"></param>
        async Task SendLineCainiaoNeedAsync(string sendName, string sendDate, string sendTime)
        {
            DataTable dt = CheckSend(sendName, sendDate, sendTime);
            if (dt.Rows.Count > 0)
            {
                bool success = false;
                string id, token, message;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    id = dt.Rows[i]["Id"].ToString();
                    var type = dt.Rows[i]["Type"].ToString();
                    //取得Line發送訊息
                    message = GetCainiaoMessage(type);

                    //取得發送群組token
                    string[] groupId = dt.Rows[i]["GroupId"].ToString().Split(',');

                    for (int j = 0; j < groupId.Length; j++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[j]);
                        if (message != "")
                        {
                            var result = await _telegramBot.SendTextMessageAsync(chatId, message);
                            if (result.Ok)
                            {
                                success = true;
                            }
                        }
                    }
                    //更新LINE發送日期
                    if (success)
                    {
                        UpdateTelegramSendMessageCainiao(id, sendDate);
                    }
                }
            }
        }

        /// <summary>
        /// 蝦皮
        /// </summary>
        /// <param name="sendName"></param>
        /// <param name="sendDate"></param>
        /// <param name="sendTime"></param>
        async Task SendLineShopeeNeedAsync(string sendName, string sendDate, string sendTime)
        {
            DataTable dt = CheckSend(sendName, sendDate, sendTime);
            if (dt.Rows.Count > 0)
            {
                bool success = false;
                string id, token, message;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    id = dt.Rows[i]["Id"].ToString();
                    //取得Line發送訊息
                    message = GetShopeeMessage();

                    //取得發送群組token
                    string[] groupId = dt.Rows[i]["GroupId"].ToString().Split(',');

                    for (int j = 0; j < groupId.Length; j++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[j]);
                        if (message != "")
                        {
                            var result = await _telegramBot.SendTextMessageAsync(chatId, message);
                            if (result.Ok)
                            {
                                success = true;
                            }
                        }
                    }
                    //更新LINE發送日期
                    if (success)
                    {
                        UpdateTelegramSendMessageCainiao(id, sendDate);
                    }
                }
            }
        }


        /// <summary>
        /// 菜鳥訊息
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string GetCainiaoMessage(string type)
        {
            //8 / 17 菜鳥空運
            //總筆數：
            //預先委任：
            //委任確認：
            //未回委任：
            //實際未回：

            var title = type == "2" ? "菜鳥海運" : "菜鳥空運";

            var today = DateTime.Now;

            var beforeday = today.AddDays(-1);

            //前一天
            var beforeData = GetData(type, beforeday);
            //今天
            var todayData = GetData(type, today);

            var sb = new StringBuilder();
            sb.AppendLine($"{title}");
            sb.AppendLine($"{beforeday.ToString("MM/dd")}");
            sb.AppendLine($"　總筆數： {beforeData.Rows[0]["Total"].ToString()}");
            sb.AppendLine($"預先委任： {beforeData.Rows[0]["NeedTotal"].ToString()}");
            sb.AppendLine($"委任確認： {beforeData.Rows[0]["ReplyTotal"].ToString()}");
            sb.AppendLine($"未回委任： {beforeData.Rows[0]["NoReplyTotal"].ToString()}");
            sb.AppendLine($"實際未回： {beforeData.Rows[0]["RealNoReplyTotal"].ToString()}");
            sb.AppendLine("");
            sb.AppendLine($"{today.ToString("MM/dd ")}");
            sb.AppendLine($"　總筆數： {todayData.Rows[0]["Total"].ToString()}");
            sb.AppendLine($"預先委任： {todayData.Rows[0]["NeedTotal"].ToString()}");
            sb.AppendLine($"委任確認： {todayData.Rows[0]["ReplyTotal"].ToString()}");
            sb.AppendLine($"未回委任： {todayData.Rows[0]["NoReplyTotal"].ToString()}");
            sb.AppendLine($"實際未回： {todayData.Rows[0]["RealNoReplyTotal"].ToString()}");

            return sb.ToString();
        }

        /// <summary>
        /// 蝦皮訊息
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string GetShopeeMessage()
        {
            //8 / 17 蝦皮空運
            //預先委任：
            //委任確認：
            //實際未回：

            var today = DateTime.Now;

            var beforeday = today.AddDays(-1);

            //前一天
            var beforeData = GetData(beforeday);
            //今天
            var todayData = GetData(today);

            var sb = new StringBuilder();
            sb.AppendLine($"蝦皮空運");
            sb.AppendLine($"{beforeday.ToString("MM/dd")}");
            sb.AppendLine($"預先委任： {beforeData.Rows[0]["NeedTotal"].ToString()}");
            sb.AppendLine($"委任確認： {beforeData.Rows[0]["ReplyTotal"].ToString()}");
            sb.AppendLine($"實際未回： {beforeData.Rows[0]["RealNoReplyTotal"].ToString()}");
            sb.AppendLine("");
            sb.AppendLine($"{today.ToString("MM/dd")}");
            sb.AppendLine($"預先委任： {todayData.Rows[0]["NeedTotal"].ToString()}");
            sb.AppendLine($"委任確認： {todayData.Rows[0]["ReplyTotal"].ToString()}");
            sb.AppendLine($"實際未回： {todayData.Rows[0]["RealNoReplyTotal"].ToString()}");

            return sb.ToString();
        }

        /// <summary>
        /// 取得是否要發送LINE
        /// </summary>
        /// <param name="sendName"></param>
        /// <returns></returns>
        public DataTable CheckSend(string sendName, string sendDate, string eTime)
        {
            string week = DateTime.Now.DayOfWeek.ToString("d");
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM [jetf].[dbo].[TelegramSendMessageCainiao] ");
            sb.Append("where SendName=@SendName and ETime<=@ETime and SendDate<@SendDate ");
            switch (week)
            {
                case "1":
                    sb.Append("and W1='1' ");
                    break;
                case "2":
                    sb.Append("and W2='1' ");
                    break;
                case "3":
                    sb.Append("and W3='1' ");
                    break;
                case "4":
                    sb.Append("and W4='1' ");
                    break;
                case "5":
                    sb.Append("and W5='1' ");
                    break;
                case "6":
                    sb.Append("and W6='1' ");
                    break;
                case "0":
                    sb.Append("and W7='1' ");
                    break;
            }

            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@SendName", SqlDbType.NVarChar).Value = sendName;
                da.SelectCommand.Parameters.Add("@ETime", SqlDbType.NVarChar).Value = eTime;
                da.SelectCommand.Parameters.Add("@SendDate", SqlDbType.NVarChar).Value = sendDate;
                da.Fill(dt);
            }
            return dt;
        }

        public void UpdateTelegramSendMessageCainiao(string id, string sendDate)
        {
            using (SqlCommand cmd = new SqlCommand("update [jetf].[dbo].[TelegramSendMessageCainiao] set SendDate=@SendDate where Id=@Id", conn))
            {
                cmd.Parameters.Add("@SendDate", SqlDbType.NVarChar).Value = sendDate;
                cmd.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// 取得菜鳥預委筆數
        /// </summary>
        /// <param name="transportType">2海快，5空快</param>
        /// <param name="sDateTime"></param>
        /// <param name="eDateTime"></param>
        /// <returns></returns>
        public DataTable GetData(string type, DateTime dateTime)
        {
            DataTable dt = new DataTable();

            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_Select_CainiaoNeed]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@Type", SqlDbType.NVarChar).Value = type;
                da.SelectCommand.Parameters.Add("@SDataDate", SqlDbType.NVarChar).Value = $"{dateTime.ToString("yyyy-MM-dd")} 00:00:00";
                da.SelectCommand.Parameters.Add("@EDataDate", SqlDbType.NVarChar).Value = $"{dateTime.ToString("yyyy-MM-dd")} 23:59:59.999";
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得蝦皮預委筆數
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public DataTable GetData(DateTime dateTime)
        {
            DataTable dt = new DataTable();

            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_Select_ShopeeNeed]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@SDataDate", SqlDbType.NVarChar).Value = $"{dateTime.ToString("yyyy-MM-dd")} 00:00:00";
                da.SelectCommand.Parameters.Add("@EDataDate", SqlDbType.NVarChar).Value = $"{dateTime.ToString("yyyy-MM-dd")} 23:59:59.999";
                da.Fill(dt);
            }
            return dt;
        }
    }
}
