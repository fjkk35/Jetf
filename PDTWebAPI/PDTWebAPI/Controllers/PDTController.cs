using Newtonsoft.Json;
using PDTWebAPI.Models.Pdt;
using PDTWebAPI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web;
using System.Data;

namespace PDTWebAPI.Controllers
{
    public class PdtController : ApiController
    {
        PdtService pdtService = new PdtService();
        /// <summary>
        /// 登入
        /// </summary>
        /// <returns></returns>
        public IHttpActionResult Login([FromBody]LoginModel body)
        {
            LoginResponseModel response = new LoginResponseModel();
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
                    if (pdtService.CheckToken("Login", strRequest, token))
                    {
                        response = pdtService.PostLogin(body);
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
                ControlNmae = "Pdt",
                ActionName = "Login",
                RequestData = strRequest,
                ResponseData = JsonConvert.SerializeObject(response),
                Token = token
            });

            return Ok(response);
        }

        [HttpGet]
        public IHttpActionResult DownloadAPK(string uploadOpe, string token)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            if (pdtService.CheckToken("Login", uploadOpe, token))
            {
                DataTable dt = pdtService.GetPdtVersion();
                string filePath = dt.Rows[0]["FilePath"].ToString();
                FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                response.Content.Headers.ContentDisposition.FileName = HttpUtility.UrlPathEncode(Path.GetFileName(filePath));
                response.Content.Headers.ContentLength = stream.Length;
            }
            return ResponseMessage(response);
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
