using JETFWebAPI.Models.Coupang;
using JETFWebAPI.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace JETFWebAPI.Controllers
{
    public class CoupangController : ApiController
    {
        Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IHttpActionResult Manifest([FromBody]ManifestRequestModel body)
        {
            //http://localhost:56641/api/Coupang/Manifest
            //http://192.168.1.9/JETFWebAPI/api/Coupang/Manifest
            //https://service.jet-f.com/JETFWebAPI/api/Coupang/Manifest
            CoupangService coupangService = new CoupangService();
            ManifestResponseModel response = new ManifestResponseModel();
            string strRequest="";
            string token = GetHeaders("Token");
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();

                    //紀錄Log
                    //logger.Info($"Manifest\r\n{strRequest}");
                }

                if (ModelState.IsValid)
                {
                    //if (coupangService.CheckToken("Manifest", strRequest,token))
                    //{
                        response = coupangService.PostManifest(body);
                    //}
                    //else {
                    //    response.resultCode = "FAIL";
                    //    response.data = "false";
                    //    response.resultMessage = "Token驗證錯誤";
                    //}
                }
                else 
                {
                    response.resultCode = "FAIL";
                    response.data = "false";
                    response.resultMessage = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.resultCode = "FAIL";
                response.data = "false";
                response.resultMessage = ex.Message ;
            }

            //錯誤寫入LOG紀錄
            if (response.resultCode == "FAIL")
            {
                GlobalService globalService = new GlobalService();
                globalService.InsertWebAPILog(new Models.Global.WebAPILogModel()
                {
                    ControlNmae = "Coupang",
                    ActionName = "Manifest",
                    RequestData = strRequest,
                    ResponseData = JsonConvert.SerializeObject(response),
                    Remark=token
                });
            }
            return Ok(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IHttpActionResult CargoManifest([FromBody]CargoManifestRequestModel body)
        {
            //http://localhost:56641/api/Coupang/CargoManifest
            //http://192.168.1.9/JETFWebAPI/api/Coupang/CargoManifest
            string strRequest="";
            string token = GetHeaders("Token");
            CoupangService coupangService = new CoupangService();
            CargoManifestResponseModel response = new CargoManifestResponseModel();
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();

                    //紀錄Log
                    //logger.Info($"CargoManifest\r\n{strRequest}");
                }
               
                if (ModelState.IsValid)
                {
                    if (coupangService.CheckToken("CargoManifest", strRequest, token))
                    {
                        response = coupangService.PostCargoManifest(body);
                    }
                    else
                    {
                        response.resultCode = "FAIL";
                        response.data = "false";
                        response.resultMessage = "Token驗證錯誤";
                    }
                }
                else {
                    response.resultCode = "FAIL";
                    response.data = "false";
                    response.resultMessage = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.resultCode = "FAIL";
                response.data = "false";
                response.resultMessage = ex.Message;
            }

            //錯誤寫入LOG紀錄
            if (response.resultCode == "FAIL")
            {
                GlobalService globalService = new GlobalService();
                globalService.InsertWebAPILog(new Models.Global.WebAPILogModel()
                {
                    ControlNmae = "Coupang",
                    ActionName = "CargoManifest",
                    RequestData = strRequest,
                    ResponseData = JsonConvert.SerializeObject(response),
                    Remark = token
                });
            }

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
