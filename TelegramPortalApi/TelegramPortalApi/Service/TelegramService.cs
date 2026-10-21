using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TelegramLibrary;
using TelegramPortalApi.Models;

namespace TelegramPortalApi.Service
{
    public class TelegramService
    {
        private readonly TelegramBot _telegramBot;

        public TelegramService()
        {
            _telegramBot = new TelegramBot();
        }

        public async Task<TelegramLibrary.Model.TelegramResponse> SendMessageAsync(TelegramRequest request)
        {
            try
            {
                var chatId = _telegramBot.GetChatId(request.GroupId);

                if (string.IsNullOrEmpty(chatId))
                    return new TelegramLibrary.Model.TelegramResponse { Ok = false, Description = "查無GroupId，請確認" };

                switch (request.SendType)
                {
                    case SendType.Text:
                        return await _telegramBot.SendTextMessageAsync(chatId, request.Message);
                    case SendType.Photo:
                        Guid g = Guid.NewGuid();
                        string filePath = $"{AppDomain.CurrentDomain.BaseDirectory + "FileJpg\\"}{g.ToString()}.jpg";
                        Byte[] bytes = Convert.FromBase64String(request.Jpg);
                        File.WriteAllBytes(filePath, bytes);
                        var result = await _telegramBot.SendPhotoAsync(chatId, request.Message, filePath);
                        //刪除圖片
                        File.Delete(filePath);
                        return result;
                    default:
                        return new TelegramLibrary.Model.TelegramResponse { Ok = false, Description = "無效的SendType" };
                }
            }
            catch (Exception ex)
            {
                return new TelegramLibrary.Model.TelegramResponse
                {
                    Ok = false,
                    Description = ex.Message
                };
            }
           

        }

    }
}