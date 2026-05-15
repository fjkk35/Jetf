using Dapper;
using Service.Models.TelegramGroup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;
using TelegramLibrary.Model;

namespace Service.Services.TelegramGroup
{
    public class TelegramGroupService :_BaseService
    {
        private readonly TelegramBot _telegramBot;

        public TelegramGroupService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, TelegramBot telegramBot)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _telegramBot = telegramBot;
        }

        public List<TelegramGroupModel> GetTelegramGroup() 
        {
            var sql = @"SELECT * FROM [jetf].[dbo].[TelegramGroup]";

            return conn.Query<TelegramGroupModel>(sql).ToList();
        }

        public Task<TelegramResponse> SendTextMessageAsync(string chatId, string message)
        {
            return _telegramBot.SendTextMessageAsync(chatId, message);
        }
    }
}
