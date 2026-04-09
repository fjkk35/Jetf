using Dapper;
using Service.Models.ErrorOrderSend;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Models.ErrorOrderSmsMessage;

namespace Service.Services.ErrorOrderSmsMessage
{
    public class ErrorOrderSmsMessageService : _BaseService
    {
        /// <summary>
        /// 取得罐頭簡訊
        /// </summary>
        /// <returns></returns>
        public List<ErrorOrderSmsMessageModel> GetErrorOrderSmsMessage()
        {
            var sqlQuery = "SELECT * FROM jetf.dbo.ErrorOrderSmsMessage";

            return conn.Query<ErrorOrderSmsMessageModel>(sqlQuery).ToList();
        }

        public ErrorOrderSmsMessageModel GetDetail(int id) 
        {
            var sqlQuery = "SELECT * FROM jetf.dbo.ErrorOrderSmsMessage where Id=@Id";

            return conn.Query<ErrorOrderSmsMessageModel>(sqlQuery,
                new 
                {
                    Id = id
                }).FirstOrDefault();
        }

        /// <summary>
        /// 刪除罐頭簡訊
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ResponseModel Delete(int id)
        {
            try
            {
                var sqlQuery = "DELETE FROM jetf.dbo.ErrorOrderSmsMessage WHERE Id = @Id";

                conn.Execute(sqlQuery, new { Id = id });

                return new ResponseModel() { };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ResponseModel Create(ErrorOrderSmsMessageModel model,string userId)
        {
            try
            {
                var sqlQuery = "INSERT INTO jetf.dbo.ErrorOrderSmsMessage (Name, Content,EditOpe,EditDateTime) VALUES (@Name, @Content,@EditOpe,@EditDateTime)";

                conn.Execute(sqlQuery, 
                    new 
                    {
                        Name = model.Name,
                        Content = model.Content,
                        EditOpe = userId,
                        EditDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });

                return new ResponseModel() { };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 更新資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ResponseModel Update(ErrorOrderSmsMessageModel model, string userId)
        {
            try
            {
                var sqlQuery = @"
                            update jetf.dbo.ErrorOrderSmsMessage set Name = @Name,Content = @Content,EditOpe = @EditOpe,EditDateTime = @EditDateTime
                            where Id =@Id
                            ";

                conn.Execute(sqlQuery,
                    new
                    {
                        Id =model.Id,
                        Name = model.Name,
                        Content = model.Content,
                        EditOpe = userId,
                        EditDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });

                return new ResponseModel() { };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }
    }
}
