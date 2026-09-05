using JETFWebAPI.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace JETFWebAPI.Filters
{
    /// <summary>
    /// Token 驗證回應類型列舉
    /// </summary>
    public enum TokenValidationResponseType
    {
        /// <summary>
        /// 預設格式 (ResultCode, Error)
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Logistics 格式 (ResultCode, Error, Rows)
        /// </summary>
        Logistics = 1,
        
        /// <summary>
        /// Status 格式 (Status, Message)
        /// </summary>
        Status = 2,
        
        /// <summary>
        /// Success 格式 (Success, Message, Data)
        /// </summary>
        Success = 3
    }

    /// <summary>
    /// Token 驗證 Attribute
    /// </summary>
    public class TokenValidationAttribute : ActionFilterAttribute
    {
        private readonly string _apiName;
        private readonly TokenValidationResponseType _responseType;

        /// <summary>
        /// 建構函式 (使用預設回應格式)
        /// </summary>
        /// <param name="apiName">API 名稱</param>
        public TokenValidationAttribute(string apiName) : this(apiName, TokenValidationResponseType.Default)
        {
        }

        /// <summary>
        /// 建構函式 (指定回應格式)
        /// </summary>
        /// <param name="apiName">API 名稱</param>
        /// <param name="responseType">回應格式類型</param>
        public TokenValidationAttribute(string apiName, TokenValidationResponseType responseType)
        {
            _apiName = apiName;
            _responseType = responseType;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
#if DEBUG
                base.OnActionExecuting(actionContext);
                return;
#endif

                // 取得 Token
                var token = GetHeaders(actionContext.Request, "Token");

                // 讀取請求內容
                string strRequest = "";
                if (HttpContext.Current?.Request?.InputStream != null)
                {
                    HttpContext.Current.Request.InputStream.Position = 0;
                    using (StreamReader stmReader = new StreamReader(HttpContext.Current.Request.InputStream))
                    {
                        strRequest = HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                        stmReader.Close();
                    }
                }

                // 檢查 Token
                var checkToken = CheckToken(_apiName, strRequest, token);

                if (!checkToken)
                {
                    // 根據 enum 建立不同格式的錯誤回應
                    var errorResponse = CreateErrorResponse("Token驗證失敗");

                    actionContext.Response = actionContext.Request.CreateResponse(
                        HttpStatusCode.OK, // 使用 200 OK 但包含錯誤資訊
                        errorResponse
                    );
                    return;
                }

                base.OnActionExecuting(actionContext);
            }
            catch (Exception ex)
            {
                // 例外處理時也根據 enum 回傳相同格式
                var errorResponse = CreateErrorResponse(string.Format("Token 驗證時發生錯誤: {0}", ex.Message));

                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.OK, // 使用 200 OK 但包含錯誤資訊
                    errorResponse
                );
            }
        }

        /// <summary>
        /// 根據回應類型建立錯誤回應
        /// </summary>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <returns>錯誤回應物件</returns>
        private object CreateErrorResponse(string errorMessage)
        {
            switch (_responseType)
            {
                case TokenValidationResponseType.Logistics:
                    return new
                    {
                        ResultCode = "01",
                        Error = errorMessage,
                    };

                case TokenValidationResponseType.Status:
                    return new
                    {
                        Status = "Fail",
                        Message = errorMessage
                    };

                case TokenValidationResponseType.Success:
                    return new
                    {
                        Success = false,
                        Message = errorMessage,
                    };

                case TokenValidationResponseType.Default:
                default:
                    return new
                    {
                        ResultCode = "01",
                        Error = errorMessage
                    };
            }
        }

        /// <summary>
        /// 取得 Header 值
        /// </summary>
        /// <param name="request">HTTP 請求</param>
        /// <param name="key">Header 鍵值</param>
        /// <returns>Header 值</returns>
        private string GetHeaders(HttpRequestMessage request, string key)
        {
            if (request.Headers.Contains(key))
            {
                return request.Headers.GetValues(key).FirstOrDefault() ?? "";
            }
            return "";
        }

        private bool CheckToken(string api, string body, string token)
        {
            //測試不驗證token
            //return true;

            bool result = false;
            string check = GetToken(api, body);
            if (check == token.ToUpper())
            {
                result = true;
            }
            return result;
        }

        private string GetToken(string api, string body)
        {
            string key = "bVAW8U9Pci9kNCu68qC1IEQGCtz3DRfO";
            string token = ToMD5($"{key}{api}{body}");
            return token;
        }

        private string ToMD5(string str)
        {
            using (var cryptoMD5 = System.Security.Cryptography.MD5.Create())
            {
                //將字串編碼成 UTF8 位元組陣列
                var bytes = Encoding.UTF8.GetBytes(str);

                //取得雜湊值位元組陣列
                var hash = cryptoMD5.ComputeHash(bytes);

                //取得 MD5
                var md5 = BitConverter.ToString(hash)
                  .Replace("-", String.Empty)
                  .ToUpper();
                return md5;
            }
        }
    }
}