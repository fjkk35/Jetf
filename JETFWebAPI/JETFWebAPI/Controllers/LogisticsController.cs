using JETFWebAPI.Models.Jetf;
using JETFWebAPI.Services;
using JETFWebAPI.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using JETFWebAPI.Models.Logistics;

namespace JETFWebAPI.Controllers
{
    public class LogisticsController : ApiController
    {
        private readonly LogisticsService _logisticsService;

        public LogisticsController()
        {
            _logisticsService = new LogisticsService();
        }

        /// <summary>
        /// 查詢託運單配送狀態
        /// </summary>
        /// <param name="requests">查詢請求列表</param>
        /// <returns>託運單配送狀態資料</returns>
        [HttpPost]
        [TokenValidation("Query", TokenValidationResponseType.Logistics)]
        public async Task<IHttpActionResult> Query([FromBody] List<QueryRequest> request)
        {
            try
            {
                // 呼叫外部 API
                var result = await _logisticsService.QueryAsync(request);

                return ResponseMessage(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(result, Encoding.UTF8, "application/json")
                });
            }
            catch (Exception ex)
            {
                // 記錄錯誤 (這裡可以加入日誌記錄)
                return InternalServerError(new Exception(string.Format("查詢託運單配送狀態時發生錯誤: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// 下載圖片
        /// </summary>
        /// <param name="request">下載圖片請求</param>
        /// <returns>圖片資料</returns>
        [HttpPost]
        [TokenValidation("DownLoadImage", TokenValidationResponseType.Logistics)]
        public async Task<IHttpActionResult> DownLoadImage([FromBody] DownLoadImageRequest request)
        {
            try
            {
                // 呼叫外部 API
                var result = await _logisticsService.DownLoadImageAsync(request);

                // 回傳成功結果
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                return InternalServerError(new Exception(string.Format("下載圖片時發生錯誤: {0}", ex.Message), ex));
            }
        }
    }
}
