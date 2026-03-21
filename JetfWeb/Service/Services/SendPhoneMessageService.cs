using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using Service.Models;
using Service.Models.CptTradeVan;
using Service.Models.ErrorOrderSend;
using Service.Models.SendPhoneMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class SendPhoneMessageService :_BaseService
    {

        public SendMessageResponse SendPhoneMessage(int id,string phone,string message) 
        {
            try
            {
                var data = new Dictionary<string, string>
                            {
                                { "UID", "jetfsystem" },
                                { "Pwd", GetPassword() },
                                { "DA", phone },
                                { "SM", message },
                            };

                var response = Send(data);

                return new SendMessageResponse()
                {
                    Id = id,
                    SendResult = response.ErrorCode == "0" ? Status.success : Status.error,
                    SmsRowId = response.RowId,
                    SmsCnt = response.Cnt,
                    SmsErrorCode = response.ErrorCode
                };
            }
            catch (Exception ex)
            {
                return new SendMessageResponse()
                {
                    Id = id,
                    SendResult = Status.error,
                    SendResultMessage = ex.Message
                };
            }
        }


        public OTPReceive Send(Dictionary<string, string> data)
        {
                using (var client = new HttpClient()) 
                {
                   
                    using (var content = new FormUrlEncodedContent(data)) 
                    {
                        var response =  client.PostAsync("https://smsc.ite2.com.tw:8090/ApiSMSC/Sms/SendSms", content).Result;
                        response.EnsureSuccessStatusCode();
                        string responseString = response.Content.ReadAsStringAsync().Result;
                        OTPReceive receive = JsonConvert.DeserializeObject<OTPReceive>(responseString);
                        return receive;
                    }
                }  
        }


        private string GetPassword() 
        {
            string pwd = "system24951752";
            byte[] strByte = Encoding.UTF8.GetBytes(pwd);
            return Convert.ToBase64String(strByte);
        }
    }
}
