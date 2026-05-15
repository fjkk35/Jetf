using Dapper;
using Service.Extensions;
using Service.Models;
using Service.Models.ErrorOrderSend;
using Service.Models.ErrorOrderSendDetail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.ErrorOrderSendDetail
{
    public class ErrorOrderSendDetailService :_BaseService
    {
        public ErrorOrderSendDetailService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public ResponseModel GetErrorOrderSendDetail(string startDate,string endDate,string trackingNo) 
        {
            var sqlQuery = @"
select b.* from [jetf].[dbo].[ErrorOrderSend] a
join [jetf].[dbo].[ErrorOrderSendDetail] b on a.Id = b.ErrorOrderSendId
where a.CrtDateTime between @StartDate and @EndDate
";
            if (!string.IsNullOrEmpty(trackingNo))
            {
                sqlQuery += " and b.TrackingNo = @TrackingNo";
            }

            var list = conn.Query<ErrorOrderSendDetailModel>(sqlQuery, 
                new 
                { 
                    StartDate = startDate,
                    EndDate = endDate,
                    TrackingNo = trackingNo
                }).ToList();

            // 將查詢結果轉換為新的 ViewModel
            var resopnse = list.Select(item => new ErrorOrderSendDetailResponse
            {
                Phone = item.Phone,
                Customer = item.Customer,
                Platform = item.Platform,
                TrackingNo = item.TrackingNo,
                SendType = item.SendType.ToString(),
                IsSend = item.IsSend == "1" ? "已發送" : "未發送",
                SendResult = item.SendResult,
                SendResultMessage = item.SendResultMessage,
                SendDateTime = item.SendDateTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                Message = item.Message
            }).ToList();

            return new ResponseModel()
            {
                ReturnObject = resopnse
            };
        }
    }
}
