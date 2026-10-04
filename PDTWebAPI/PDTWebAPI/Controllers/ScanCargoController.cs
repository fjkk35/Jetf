using Newtonsoft.Json;
using PDTWebAPI.Models.ScanCargo;
using PDTWebAPI.Models.Unpacking;
using PDTWebAPI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PDTWebAPI.Controllers
{
    public class ScanCargoController : ApiController
    {
        /// <summary>
        /// 掃貨上車資料上傳
        /// </summary>
        /// <returns></returns>
        public IHttpActionResult Upload([FromBody]UploadModel body)
        {
            PdtService pdtService = new PdtService();
            ScanCargoService scanCargoService = new ScanCargoService();
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
                    if (true || pdtService.CheckToken("Upload", strRequest, token))
                    {
                        response = scanCargoService.PostUpload(body);
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
                ControlNmae = "ScanCargo",
                ActionName = "Upload",
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
