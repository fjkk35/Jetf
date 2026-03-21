using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Service.Models.ErrorOrderSend;
using Service.EnumTax;
using Service.Extensions;
using Dapper;
using NPOI.SS.Util;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using NPOI.SS.Formula.Functions;
using Renci.SshNet.Messages;
using Service.Services.ErrorOrderSendCustomer;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Messaging;
using Service.Models.ErrorOrderSmsMessage;
using Spire.Xls.Core.Spreadsheet;

namespace Service.Services.ErrorOrderSend
{
    public class ErrorOrderSendService : _BaseService
    {
        private readonly string lineMessageToken = "f93v5z2VU4LOdd0MCcFYuU0SBxucVSVpiv3fWVATRjEYpSFs0wGo94FdzfxMQt4Oxvfn6xNaTtqdEDtUzDQoVnlhwBGY5XeCLSdhbPJOacz013ieEdse3NerN3tOeq91XZmRAQdW//Pttc+BUYHuYgdB04t89/1O/w1cDnyilFU=";

        private readonly SendPhoneMessageService _sendPhoneMessageService;

        private readonly ErrorOrderSendCustomerService _errorOrderSendCustomerService;

        public ErrorOrderSendService(SendPhoneMessageService sendPhoneMessageService, ErrorOrderSendCustomerService errorOrderSendCustomerService)
        {
            _sendPhoneMessageService = sendPhoneMessageService;
            _errorOrderSendCustomerService = errorOrderSendCustomerService;
        }


        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="type"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel Upload(string filePath, int smsMessageId, string userId)
        {
            try
            {
                var list = ReadExcel(filePath);

                if (list.Any() == false)
                {
                    return new ResopnseModel("上傳檔案筆數:0");
                }

                //確認要發送手機、Line
                list = CheckSendType(list);

                //客戶平台資料對應
                CustomerPlatformMapping(list);

                //發送訊息對應
                ErrorOrderSendMessageMapping(list, smsMessageId);

                return InsertData(list, filePath, userId);
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }

        public ResopnseModel Send(int id,string userId)
        {
            try
            {
                var list = GetErrorOrderSendDetail(id);

                if(list.Any() == false)
                    return new ResopnseModel("沒有資料需要發送，請確認");

                //發送手機
                SendPhone(list);

                //發送LINE
                SendLine(list);

                UpdateErrorOrderSend(id, userId);
                return new ResopnseModel()
                {
                    msg = "發送成功"
                };
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }


        public ResopnseModel Delete(int id,string userId)
        {
            try
            {
                DeleteErrorOrderSend(id, userId);
                return new ResopnseModel()
                {
                    msg = "刪除成功"
                };
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }

        /// <summary>
        /// 是否發送過
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsSend(int id)
        { 
            var sqlQuery = @"select * from  jetf.dbo.[ErrorOrderSend] where IsSend='1' and Id=@Id";

            var result = conn.QueryFirstOrDefault(sqlQuery, new { Id = id });

            return result != null;
        }

        /// <summary>
        /// 下載明細
        /// </summary>
        public IWorkbook ErrorOrderSendDetailExcel(int id) 
        {
            var list = GetErrorOrderSendDetail(id);

            IWorkbook workbook = new XSSFWorkbook();

            ISheet sheet = workbook.CreateSheet("明細");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("簡訊名稱");
            row.CreateCell(1).SetCellValue("手機");
            row.CreateCell(2).SetCellValue("客戶");
            row.CreateCell(3).SetCellValue("平台");
            row.CreateCell(4).SetCellValue("分提單號");
            row.CreateCell(5).SetCellValue("發送類別");
            row.CreateCell(6).SetCellValue("發送狀態");
            row.CreateCell(7).SetCellValue("發送結果");
            row.CreateCell(8).SetCellValue("發送結果訊息");
            row.CreateCell(9).SetCellValue("簡訊流水號");
            row.CreateCell(10).SetCellValue("則數");
            row.CreateCell(11).SetCellValue("發送結果代碼");
            row.CreateCell(12).SetCellValue("發送時間");
            row.CreateCell(13).SetCellValue("訊息");

            sheet.SetColumnWidth(0, 15000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 5000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 5000);
            sheet.SetColumnWidth(12, 5000);
            sheet.SetColumnWidth(13, 5000);

            var irow = 1;
            list.ForEach((item) =>
            {
                IRow dataRow = sheet.CreateRow(irow++);
                dataRow.CreateCell(0).SetCellValue(item.SmsName);
                dataRow.CreateCell(1).SetCellValue(item.Phone);
                dataRow.CreateCell(2).SetCellValue(item.Customer);
                dataRow.CreateCell(3).SetCellValue(item.Platform);
                dataRow.CreateCell(4).SetCellValue(item.TrackingNo);
                dataRow.CreateCell(5).SetCellValue(item.SendType.ToString());
                dataRow.CreateCell(6).SetCellValue(item.IsSend == "0" ? "未發送" : "已發送");
                dataRow.CreateCell(7).SetCellValue(item.SendResult);
                dataRow.CreateCell(8).SetCellValue(item.SendResultMessage);
                dataRow.CreateCell(9).SetCellValue(item.SmsRowId);
                dataRow.CreateCell(10).SetCellValue(item.SmsCnt);
                dataRow.CreateCell(11).SetCellValue(item.SmsErrorCode);
                dataRow.CreateCell(12).SetCellValue(item.SendDateTime?.ToString("yyyy/MM/dd HH:mm:ss"));
                dataRow.CreateCell(13).SetCellValue(item.Message);
            });

            return workbook;
        }

        private void SendLine(List<ErrorOrderSendDetailModel> list) 
        {
            var lines = list.Where(x => x.SendType == SendType.Line).ToList();

            foreach (var item in lines)
            {
                var resopnse = SendLineMessage(item.Id,item.LineUserId, item.Message);

                //LINE發送失敗，直接發簡訊
                if (resopnse.SendResult == Status.error)
                {
                    //發簡訊
                    resopnse = _sendPhoneMessageService.SendPhoneMessage(item.Id, item.Phone, item.Message);
                }

                //更新發送結果
                UpdateErrorOrderSendDetail(resopnse);
            }
        }

        private void SendPhone(List<ErrorOrderSendDetailModel> list)
        {
            var phones = list.Where(x => x.SendType == SendType.Sms).ToList();

            foreach (var item in phones)
            {
                var resopnse = _sendPhoneMessageService.SendPhoneMessage(item.Id, item.Phone, item.Message);

                //更新發送結果
                UpdateErrorOrderSendDetail(resopnse);
            }
        }

        public void UpdateErrorOrderSendDetail(SendMessageResponse response) 
        {
            var sqlQuery = @"update jetf.dbo.[ErrorOrderSendDetail] set IsSend='1',SendResult=@SendResult,SendResultMessage=@SendResultMessage,SmsRowId=@SmsRowId,SmsCnt=@SmsCnt,SmsErrorCode=@SmsErrorCode,SendDateTime=GETDATE()
                             where Id=@Id ";

            conn.Execute(sqlQuery, new 
            { 
                Id = response.Id,
                SendResult = response.SendResult,
                SendResultMessage = response.SendResultMessage,
                SmsRowId = response.SmsRowId,
                SmsCnt = response.SmsCnt,
                SmsErrorCode = response.SmsErrorCode
            });
        }

        /// <summary>
        /// 更新為發送
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        private void UpdateErrorOrderSend(int id,string userId) 
        {
            var sqlQuery = @"update jetf.dbo.[ErrorOrderSend] set IsSend='1',SendOpe=@SendOpe,SendDateTime=GETDATE()
                             where Id=@Id ";

            conn.Execute(sqlQuery, new { Id = id, SendOpe = userId });
        }

        /// <summary>
        /// 刪除未發送的明細
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        private void DeleteErrorOrderSend(int id, string userId)
        {
            var sqlQuery = @"update jetf.dbo.[ErrorOrderSend] set IsDelete='1',DeleteDateTime=@DeleteDateTime,DeleteOpe=@DeleteOpe
                             where Id=@Id and IsSend='0'";
            conn.Execute(sqlQuery, new 
            { 
                Id = id,
                DeleteDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DeleteOpe = userId 
            });
        }

        public List<ErrorOrderSendModel> GetErrorOrderSend() 
        { 
            var sqlQuery = @"
               with SendResult as 
               (
	                select 
	                ErrorOrderSendId,
	                sum(case when SendResult='success' then 1 else 0 end) as SuccessCount,
	                sum(case when SendResult='error' then 1 else 0 end) as ErrorCount
	                from jetf.dbo.ErrorOrderSendDetail
	                group by ErrorOrderSendId
               )
               SELECT top 100 a.*,b.SuccessCount,b.ErrorCount FROM jetf.dbo.ErrorOrderSend a
               join SendResult b on a.Id = b.ErrorOrderSendId
               where IsDelete='0'
               order by Id desc";

            return conn.Query<ErrorOrderSendModel>(sqlQuery).ToList();
        }

        public ErrorOrderSmsMessageModel GetErrorOrderSmsMessage(int id)
        {
            var sqlQuery = @"select * from jetf.dbo.[ErrorOrderSmsMessage]
                             where Id=@Id
                           ";

            return conn.Query<ErrorOrderSmsMessageModel>(sqlQuery,
                new 
                { 
                    Id = id
                }).FirstOrDefault();
        }

        public List<ErrorOrderSendDetailModel> GetErrorOrderSendDetail(int id) 
        { 
            var sqlQuery = "SELECT * FROM jetf.dbo.ErrorOrderSendDetail where ErrorOrderSendId=@ErrorOrderSendId";

            return conn.Query<ErrorOrderSendDetailModel>(sqlQuery, new { ErrorOrderSendId = id }).ToList();
        }

        /// <summary>
        /// 讀取Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        List<ErrorOrderSendDetailModel> ReadExcel(string filePath)
        {
            var list = new List<ErrorOrderSendDetailModel>();

            bool read = false;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);

            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCellData(0) == "手機號碼" &&
                        sheet.GetRow(i).GetCellData(1) == "客戶" &&
                        sheet.GetRow(i).GetCellData(2) == "分提單號")
                    {
                        read = true;
                        continue;
                    }

                    if (read &&
                       !string.IsNullOrEmpty(sheet.GetRow(i).GetCellData(0)) &&
                       !string.IsNullOrEmpty(sheet.GetRow(i).GetCellData(1)) &&
                       !string.IsNullOrEmpty(sheet.GetRow(i).GetCellData(2)))
                    {
                        list.Add(new ErrorOrderSendDetailModel()
                        {
                            Phone = Regex.Replace(sheet.GetRow(i).GetCellData(0), @"[^\d]", ""),
                            Customer = sheet.GetRow(i).GetCellData(1),
                            TrackingNo = sheet.GetRow(i).GetCellData(2)
                        });
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResopnseModel InsertData(List<ErrorOrderSendDetailModel> list,string filePath,string userId)
        {
            var resopnseModel = new ResopnseModel();
            resopnseModel.msg = $"上傳檔案筆數：{list.Count}";

            var insertErrorOrderSendSql = @"insert [jetf].[dbo].[ErrorOrderSend]([FileName], [FilePath],[TotalCount], [PhoneCount], [LineCount], [UploadOpe])
                                    OUTPUT INSERTED.Id
                                    values(@FileName,@FilePath,@TotalCount,@PhoneCount,@LineCount,@UploadOpe)";

            var insertErrorOrderSendDetailSql = @"insert [jetf].[dbo].[ErrorOrderSendDetail]([ErrorOrderSendId], [Phone], [Customer],[Platform], [TrackingNo], [SendType],[LineUserId],[Message],[SmsName])
                                    values(@ErrorOrderSendId, @Phone, @Customer,@Platform, @TrackingNo, @SendType,@LineUserId,@Message,@SmsName)";


            if (conn.State != ConnectionState.Open)
                conn.Open();

            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // 插入 ErrorOrderSend
                    var errorOrderSendId = conn.QuerySingle<int>(insertErrorOrderSendSql, new
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        TotalCount = list.Count,
                        PhoneCount = list.Count(x => x.SendType == SendType.Sms),
                        LineCount = list.Count(x => x.SendType == SendType.Line),
                        UploadOpe = userId
                    }, transaction: tran);

                    // 插入 ErrorOrderSendDetail
                    foreach (var item in list)
                    {
                        conn.Execute(insertErrorOrderSendDetailSql, new
                        {
                            ErrorOrderSendId = errorOrderSendId,
                            Phone = item.Phone,
                            Customer = item.Customer,
                            Platform = item.Platform,
                            SmsName = item.SmsName,
                            Message = item.Message,
                            TrackingNo = item.TrackingNo,
                            SendType = item.SendType,
                            LineUserId = item.LineUserId,
                        }, transaction: tran);
                    }

                    // 確認寫入
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    resopnseModel = new ResopnseModel(ex.Message);

                    // 取消寫入
                    tran.Rollback();
                }
            }

            conn.Close();

            return resopnseModel;
        }

        /// <summary>
        /// 確認寄送方式
        /// </summary>
        /// <returns></returns>
        private List<ErrorOrderSendDetailModel> CheckSendType(List<ErrorOrderSendDetailModel> list)
        {
            var users = GetLineUserProfile();

            list.ForEach(r =>
            {
                var user = users.FirstOrDefault(x => x.Phone == r.Phone);

                if (user != null)
                {
                    r.LineUserId = user.UserId;

                    r.SendType = SendType.Line;
                }
                else {
                    r.SendType = SendType.Sms;
                }
            });

            return list;
        }

        /// <summary>
        /// 客戶平台資料對應
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private void CustomerPlatformMapping(List<ErrorOrderSendDetailModel> list) 
        { 
            var customers = _errorOrderSendCustomerService.GetCustomerPlatformMapping();

            list.ForEach(r =>
            {
                var customer = customers.FirstOrDefault(x => x.Customer == r.Customer);

                r.Platform = customer?.Platform ?? "海外";
            });
        }

        private void ErrorOrderSendMessageMapping(List<ErrorOrderSendDetailModel> list, int smsMessageId)
        {
            var message = GetErrorOrderSmsMessage(smsMessageId);

            list.ForEach(r =>
            {
                r.SmsName = message.Name;
                r.Message = message.Content
                .Replace("＜平台＞",$"{r.Platform}")
                .Replace("＜分提單號＞", $"{r.TrackingNo}");
            });
        }

        /// <summary>
        /// 取得Line用戶綁定手機
        /// </summary>
        /// <returns></returns>
        private List<LineUserProfile> GetLineUserProfile()
        {
           string sqlQuery = "SELECT UserId, Phone FROM jetf.dbo.LineUserProfile WHERE IsUnblocked = '1'";
           return conn.Query<LineUserProfile>(sqlQuery).ToList();
        }

        /// <summary>
        /// Line發送訊息給使用者
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public SendMessageResponse SendLineMessage(int id,string userId, string message)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lineMessageToken);

                    var payload = new
                    {
                        to = userId,
                        messages = new[]
                        {
                        new
                        {
                            type = "text",
                            text = message
                        }
                    }
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                    var response = client.PostAsync("https://api.line.me/v2/bot/message/push", content).Result;

                    var result = new SendMessageResponse()
                    {
                        Id = id,
                        SendResult = response.StatusCode == HttpStatusCode.OK ? Status.success : Status.error,
                    };

                    // 如果發送失敗，記錄錯誤訊息
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        result.SendResultMessage = $"HTTP Status: {response.StatusCode}, Response: {responseContent}";
                    }

                    return result;
                }
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
    }
}
