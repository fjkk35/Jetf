using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;
using TelegramLibrary.Model;
using Service.Services.TelegramGroup;

namespace Service.Services.Job.CainiaoCheckJob
{
    public class CainiaoCheckJobService :_BaseService
    {
        private readonly TelegramBot _telegramBot;

        public CainiaoCheckJobService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, TelegramBot telegramBot)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _telegramBot = telegramBot;
        }

        /// <summary>
        /// 菜鳥檢查資料筆數
        /// </summary>
        public async Task RunCainiaoCheckJobAsync()
        {
            try
            {
                DateTime date = DateTime.Now;
                string sendDate = date.ToString("yyyyMMdd");
                string sendTime = date.AddMinutes(-8).ToString("HHmmss");

                await SendTelegramCainiaoCheck("CainiaoCheck", sendDate, sendTime);
            }
            catch (Exception ex)
            {
                WriteJobErrorLog("菜鳥資料檢查", ex);
            }
        }


        async Task SendTelegramCainiaoCheck(string sendName, string sendDate, string sendTime)
        {
            DataTable dt = CheckSend(sendName, sendDate, sendTime);
            if (dt.Rows.Count > 0)
            {
                bool success = false;
                DateTime sDateTime, eDateTime;
                string id, message, sTime, eTime;
                int checkCount = 0;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    id = dt.Rows[i]["Id"].ToString();
                    //時間
                    sTime = dt.Rows[i]["STime"].ToString();
                    eTime = dt.Rows[i]["ETime"].ToString();
                    sDateTime = DateTime.ParseExact(sendDate + sTime.Trim() + "000", "yyyyMMddHHmmssfff", new System.Globalization.CultureInfo("zh-TW"));
                    eDateTime = DateTime.ParseExact(sendDate + eTime.Trim() + "999", "yyyyMMddHHmmssfff", new System.Globalization.CultureInfo("zh-TW"));
                    //檢查筆數
                    Int32.TryParse(dt.Rows[i]["CheckCount"].ToString(), out checkCount);
                    //取得Line發送訊息
                    message = GetCainiaoMessage(checkCount, sDateTime, eDateTime);
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
                        else
                        {
                            success = true;
                        }
                    }
                    //更新Telegram發送日期
                    if (success)
                    {
                        UpdateTelegramSendMessageCainiao(id, sendDate);
                    }
                }
            }
        }

        string GetCainiaoMessage(int checkCount, DateTime sDateTime, DateTime eDateTime)
        {
            //8 / 4
            //09：00--09：10
            //菜鳥訊息通知：
            //1.接收
            //空幾筆，海幾筆
            //2.已回
            //空幾筆，海幾筆
            //3.未回
            //空幾筆，海幾筆
            //4.差異
            //空幾筆，海幾筆
            string message = "";

            DataTable dt_Etl = GetDate("5", sDateTime, eDateTime);
            DataTable dt_Sea = GetDate("2", sDateTime, eDateTime);
            //空運
            int etlTotalCount = Convert.ToInt32(dt_Etl.Rows[0]["TotalCount"]);
            int etlTotalCountY = Convert.ToInt32(dt_Etl.Rows[0]["TotalCountY"]);
            int etlTotalCountN = Convert.ToInt32(dt_Etl.Rows[0]["TotalCountN"]);
            int etlDiff = etlTotalCount - etlTotalCountY - etlTotalCountN;
            //海運
            int seaTotalCount = Convert.ToInt32(dt_Sea.Rows[0]["TotalCount"]);
            int seaTotalCountY = Convert.ToInt32(dt_Sea.Rows[0]["TotalCountY"]);
            int seaTotalCountN = Convert.ToInt32(dt_Sea.Rows[0]["TotalCountN"]);
            int seaDiff = seaTotalCount - seaTotalCountY - seaTotalCountN;

            if (etlTotalCount <= checkCount || etlTotalCountY <= checkCount || etlTotalCountN > checkCount || etlDiff > checkCount ||
                seaTotalCount <= checkCount || seaTotalCountY <= checkCount || seaTotalCountN > checkCount || seaDiff > checkCount)
            {
                string etlTotalCountString = etlTotalCount.ToString();
                string etlTotalCountYString = etlTotalCountY.ToString();
                string etlTotalCountNString = etlTotalCountN.ToString();
                string etlDiffString = etlDiff.ToString();
                string seaTotalCountString = seaTotalCount.ToString();
                string seaTotalCountYString = seaTotalCountY.ToString();
                string seaTotalCountNString = seaTotalCountN.ToString();
                string seaDiffString = seaDiff.ToString();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(sDateTime.ToString("MM/dd"));
                sb.AppendLine($"{sDateTime.ToString("HH:mm")} -- {eDateTime.AddMinutes(+1).ToString("HH:mm")}");
                sb.AppendLine("菜鳥訊息通知：");
                sb.AppendLine($"1.接收");
                sb.AppendLine($"空　{etlTotalCountString}，海　{seaTotalCountString}");
                sb.AppendLine($"2.已回");
                sb.AppendLine($"空　{etlTotalCountYString}，海　{seaTotalCountYString}");
                sb.AppendLine($"3.未回");
                sb.AppendLine($"空　{etlTotalCountNString}，海　{seaTotalCountNString}");
                sb.AppendLine($"4.差異");
                sb.AppendLine($"空　{etlDiffString}，海　{seaDiffString}");
                message = sb.ToString();
            }

            return message;
        }

        string GetToken(string groupId)
        {
            string token = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from jetf.[dbo].[LineGroup] where GroupId=@GroupId", conn))
            {
                da.SelectCommand.Parameters.Add("@GroupId", SqlDbType.NVarChar).Value = groupId;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                token = dt.Rows[0]["Token"].ToString();
            }
            return token;
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
        /// 取得菜鳥接收回覆筆數
        /// </summary>
        /// <param name="transportType">2海快，5空快</param>
        /// <param name="sDateTime"></param>
        /// <param name="eDateTime"></param>
        /// <returns></returns>
        public DataTable GetDate(string transportType, DateTime sDateTime, DateTime eDateTime)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("select ");
            sb.Append("count(a.LP_CODE) as TotalCount, ");
            sb.Append("isnull(sum(case when b.[STATUS]='Y' then 1 else 0 end),0) as TotalCountY, ");
            sb.Append("isnull(sum(case when b.[STATUS]='N' then 1 else 0 end),0) as TotalCountN ");
            sb.Append("from DATA_CENTER.dbo.ETL_CNI_PRE_DECLARE_ORDER a ");
            sb.Append("left join DATA_CENTER.dbo.ETL_CNO_PRE_DECLARE_CALLBACK b on a.LP_CODE=b.LP_CODE ");
            sb.Append("where a.TRANSPORT_TYPE=@TRANSPORT_TYPE and a.CREATE_TIME between @SDateTime and @EDateTime ");

            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@TRANSPORT_TYPE", SqlDbType.NVarChar).Value = transportType;
                da.SelectCommand.Parameters.Add("@SDateTime", SqlDbType.NVarChar).Value = sDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                da.SelectCommand.Parameters.Add("@EDateTime", SqlDbType.NVarChar).Value = eDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                da.Fill(dt);
            }
            return dt;
        }
    }
}
