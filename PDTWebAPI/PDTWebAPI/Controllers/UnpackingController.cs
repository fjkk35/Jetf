using Newtonsoft.Json;
using NLog;
using PDTWebAPI.Models.Pdt;
using PDTWebAPI.Models.Unpacking;
using PDTWebAPI.Services;
using System;
using System.IO;
using System.Linq;
using System.Web.Http;
using TelegramLibrary;
using System.Net.Http;
using System.Threading.Tasks;

namespace PDTWebAPI.Controllers
{
    public class UnpackingController : ApiController
    {
        /// <summary>
        /// 袋號回傳
        /// </summary>
        /// <returns></returns>
        public async Task<IHttpActionResult> BagNo([FromBody]BagNoModel body)
        {
            PdtService pdtService = new PdtService();
            UnpackingService unpackingService = new UnpackingService();
            UnpackingResponseModel response = new UnpackingResponseModel();
            string strRequest = "";
            string token = GetHeaders("Token");
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();
                }

                if (ModelState.IsValid)
                {
                    if (pdtService.CheckToken("BagNo", strRequest, token))
                    {
                        response = await unpackingService.PostBagNoAsync(body);
                    }
                    else
                    {
                        response.Status = "Fail";
                        response.ResultMessage = "Token驗證錯誤";
                    }
                }
                else
                {
                    response.Status = "Fail";
                    response.ResultMessage = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }

            //寫入LOG紀錄
            GlobalService globalService = new GlobalService();
            globalService.InsertPdtWebAPILog(new Models.Global.PdtWebAPILogModel()
            {
                ControlNmae = "Unpacking",
                ActionName = "BagNo",
                RequestData = strRequest,
                ResponseData = JsonConvert.SerializeObject(response),
                Token = token
            });

            return Ok(response);
        }

        /// <summary>
        /// 分提單號回傳
        /// </summary>
        /// <returns></returns>
        public async Task<IHttpActionResult> TrackingNo([FromBody]TrackingNoModel body)
        {
            PdtService pdtService = new PdtService();
            UnpackingService unpackingService = new UnpackingService();
            UnpackingResponseModel response = new UnpackingResponseModel();
            string strRequest = "";
            string token = GetHeaders("Token");
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();
                }

                if (ModelState.IsValid )
                {
                    if (pdtService.CheckToken("TrackingNo", strRequest, token))
                    {
                        response = await unpackingService.PostTrackingNoAsync(body);
                    }
                    else
                    {
                        response.Status = "Fail";
                        response.ResultMessage = "Token驗證錯誤";
                    }
                }
                else
                {
                    response.Status = "Fail";
                    response.ResultMessage = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }

            //寫入LOG紀錄
            GlobalService globalService = new GlobalService();
            globalService.InsertPdtWebAPILog(new Models.Global.PdtWebAPILogModel()
            {
                ControlNmae = "Unpacking",
                ActionName = "TrackingNo",
                RequestData = strRequest,
                ResponseData = JsonConvert.SerializeObject(response),
                Token = token
            });

            return Ok(response);
        }

        public string GetHeaders(string key)
        {
            var headers = Request.Headers;
            string value = "";
            if (headers.Contains(key))
            {
                value = headers.GetValues(key).First();
            }
            return value;
        }
    }
}
