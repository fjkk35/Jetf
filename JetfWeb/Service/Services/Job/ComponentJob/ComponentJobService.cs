using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;
using Renci.SshNet.Messages;

namespace Service.Services.Job.ComponentJob
{
    public class ComponentJobService : _BaseService
    {
        private readonly TelegramBot _telegramBot;

        public ComponentJobService(TelegramBot telegramBot)
        {
            _telegramBot = telegramBot;
        }

        /// <summary>
        /// 酷彭發送訊息
        /// </summary>
        public async Task RunComponentJobAsync()
        {
            DateTime date = DateTime.Now;
            string sendDate = date.ToString("yyyyMMdd");
            string sendTime = date.ToString("HHmmss");

           await SendLineManifestAsync("Manifest", sendDate, sendTime);
           await SendLineCargoManifestAsync("CargoManifest", sendDate, sendTime);
        }

        async Task SendLineManifestAsync(string sendName, string sendDate, string sendTime)
        {
            DataTable dt = checkSend(sendName, sendDate, sendTime);
            if (dt.Rows.Count > 0)
            {
                bool success = false;
                DateTime sDateTime, eDateTime;
                string id, token, message, sTime, eTime;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    id = dt.Rows[i]["Id"].ToString();
                    //時間
                    sTime = dt.Rows[i]["STime"].ToString();
                    eTime = dt.Rows[i]["ETime"].ToString();
                    sDateTime = DateTime.ParseExact(sendDate + sTime.Trim() + "000", "yyyyMMddHHmmssfff", new System.Globalization.CultureInfo("zh-TW"));
                    eDateTime = DateTime.ParseExact(sendDate + eTime.Trim() + "999", "yyyyMMddHHmmssfff", new System.Globalization.CultureInfo("zh-TW"));
                    message = GetManifest(sDateTime, eDateTime);
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
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
                        UpdatTelegramSendMessageCoupang(id, sendDate);
                    }
                }
            }
        }

        async Task SendLineCargoManifestAsync(string sendName, string sendDate, string sendTime)
        {
            DataTable dt = checkSend(sendName, sendDate, sendTime);
            if (dt.Rows.Count > 0)
            {
                bool success = false;
                ArrayList messageList;
                DateTime sDateTime, eDateTime;
                string id, token, sTime, eTime;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    id = dt.Rows[i]["Id"].ToString();
                    //時間
                    sTime = dt.Rows[i]["STime"].ToString();
                    eTime = dt.Rows[i]["ETime"].ToString();
                    sDateTime = DateTime.ParseExact(sendDate + sTime.Trim() + "000", "yyyyMMddHHmmssfff", new System.Globalization.CultureInfo("zh-TW"));
                    eDateTime = DateTime.ParseExact(sendDate + eTime.Trim() + "999", "yyyyMMddHHmmssfff", new System.Globalization.CultureInfo("zh-TW"));
                    messageList = GetCargoManifest(sDateTime, eDateTime);
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                    for (int j = 0; j < groupId.Length; j++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[j]);

                        if (messageList.Count > 0)
                        {
                            for (int k = 0; k < messageList.Count; k++)
                            {
                                var result = await _telegramBot.SendTextMessageAsync(chatId, messageList[k].ToString());
                                if (result.Ok)
                                {
                                    success = true;
                                }
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
                        UpdatTelegramSendMessageCoupang(id, sendDate);
                    }
                }
            }
        }

        ArrayList GetCargoManifest(DateTime sDateTime, DateTime eDateTime)
        {
            //您好,JF收到Cargo Manifest
            //件數：XXXX    CargoManifest.MasterBagNo
            //票數：XXXX    Manifest.HawbNo
            //安排：297 - 84510086(MawbNo)  CI163(FlightNo)
            StringBuilder sb;
            ArrayList messageList = new ArrayList();
            DataTable dt = new DataTable();
            StringBuilder sql = new StringBuilder();
            //sql.Append("select a.MawbNo,a.FlightNo,COUNT(distinct a.MasterBagNo) as MasterBagNo,COUNT(distinct b.HawbNo) as HawbNo from [DATA_CENTER].[dbo].[ORDER_CARGO_MANIFEST] a ");
            //sql.Append("join [DATA_CENTER].[dbo].[ORDER_MANIFEST] b on a.MawbNo=b.MawbNo ");
            //sql.Append("where a.CrtDateTime between @sDateTime and @eDateTime ");
            //sql.Append("group by a.MawbNo,a.FlightNo ");

            sql.Append("with ");
            sql.Append("ORDER_CARGO_MANIFEST as ");
            sql.Append("( ");
            sql.Append("	select a.MawbNo,a.FlightNo,count(distinct MasterBagNo) as MasterBagNo from [DATA_CENTER].[dbo].[ORDER_CARGO_MANIFEST] a ");
            sql.Append("	where a.CrtDateTime between @sDateTime and @eDateTime ");
            sql.Append("	group by a.MawbNo,a.FlightNo ");
            sql.Append(") ");
            sql.Append("select a.MawbNo,a.FlightNo,a.MasterBagNo,count(distinct b.HawbNo) as HawbNo from ORDER_CARGO_MANIFEST a ");
            sql.Append("join [DATA_CENTER].[dbo].[ORDER_MANIFEST] b on a.MawbNo=b.MawbNo ");
            sql.Append("group by a.MawbNo,a.FlightNo,a.MasterBagNo ");

            using (SqlDataAdapter da = new SqlDataAdapter(sql.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@sDateTime", SqlDbType.NVarChar).Value = sDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                da.SelectCommand.Parameters.Add("@eDateTime", SqlDbType.NVarChar).Value = eDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                da.Fill(dt);
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                sb = new StringBuilder();
                sb.AppendLine("您好,JF收到Cargo Manifest");
                //顯示結束時間分鐘+1
                sb.AppendLine($"{sDateTime.ToString("MM/dd HH:mm")} -- {eDateTime.AddMinutes(+1).ToString("HH:mm")}");
                sb.AppendLine($"件數：{dt.Rows[i]["MasterBagNo"].ToString()}");
                sb.AppendLine($"票數：{dt.Rows[i]["HawbNo"].ToString()}");
                sb.AppendLine($"安排：{dt.Rows[i]["MawbNo"].ToString()}　{dt.Rows[i]["FlightNo"].ToString()}");
                messageList.Add(sb.ToString());
            }
            return messageList;
        }

        string GetManifest(DateTime sDateTime, DateTime eDateTime)
        {
            //您好, JF收到Manifest
            //MM / DD HH：MM-- HH：MM
            //合計：XXXX袋
            //G類：XXXX袋
            //X類：XXXX袋
            string message = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from [DATA_CENTER].[dbo].[ORDER_MANIFEST] where CrtDateTime between @sDateTime and @eDateTime", conn))
            {
                da.SelectCommand.Parameters.Add("@sDateTime", SqlDbType.NVarChar).Value = sDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                da.SelectCommand.Parameters.Add("@eDateTime", SqlDbType.NVarChar).Value = eDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                int total, total_HawbNo;
                DataRow[] drX, drG;
                DataView dv = new DataView(dt);
                //袋號
                DataTable dt_DeclType = dv.ToTable(true, "DeclType", "BagNo");
                //提單號碼
                DataTable dt_HawbNo = dv.ToTable(true, "HawbNo");
                drX = dt_DeclType.Select($"DeclType='X2' or DeclType='X3' ");
                drG = dt_DeclType.Select($"DeclType='G1' ");
                total = drX.Length + drG.Length;
                total_HawbNo = dt_HawbNo.Rows.Count;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("您好, JF收到Manifest");
                //顯示結束時間分鐘+1
                sb.AppendLine($"{sDateTime.ToString("MM/dd HH:mm")} -- {eDateTime.AddMinutes(+1).ToString("HH:mm")}");
                sb.AppendLine($"合計：{total}件／{total_HawbNo}票 ");
                sb.AppendLine($"G類：{drG.Length}件");
                sb.AppendLine($"X類：{drX.Length}件");
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
        public DataTable checkSend(string sendName, string sendDate, string eTime)
        {
            DateTime date = DateTime.Now;
            string week = DateTime.Now.DayOfWeek.ToString("d");
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM [jetf].[dbo].[TelegramSendMessageCoupang] ");
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

        public void UpdatTelegramSendMessageCoupang(string id, string sendDate)
        {
            using (SqlCommand cmd = new SqlCommand("update [jetf].[dbo].[TelegramSendMessageCoupang] set SendDate=@SendDate where Id=@Id", conn))
            {
                cmd.Parameters.Add("@SendDate", SqlDbType.NVarChar).Value = sendDate;
                cmd.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}
