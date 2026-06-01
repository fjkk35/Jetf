using Newtonsoft.Json;
using NLog;
using Service.EnumTax;
using Service.Models;
using Service.Services.Ezway;
using Service.Services.Ezway.Domain;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// Ezway 海運電子商務通關平台頁面與 AJAX API 控制器。
    /// </summary>
    public class EzwaySeaController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly EzwaySeaService _ezwayService;

        /// <summary>
        /// 建立 EzwaySeaController。
        /// </summary>
        public EzwaySeaController(EzwaySeaService ezwayService)
        {
            _ezwayService = ezwayService;
        }

        /// <summary>
        /// 顯示 Ezway 海運主畫面。
        /// </summary>
        [UserAuthorize(Authority.Ezway)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        /// <summary>
        /// 初始化 Ezway 畫面狀態。
        /// </summary>
        public async Task<JsonResult> Initialize()
        {
            try
            {
                LogActionRequest("初始化", new { });
                var result = await _ezwayService.GetPageStateAsync();
                var pageState = result.ReturnObject as EzwayPageState;
                LogActionResponse("初始化", new
                {
                    result.IsSuccess,
                    result.msg,
                    IsLoggedIn = pageState?.IsLoggedIn,
                    LoggedInAccounts = pageState?.LoggedInAccounts?.Count ?? 0,
                    LoginCaptchaRequired = pageState?.LoginCaptchaState?.CaptchaRequired,
                    QueryCaptchaRequired = pageState?.QueryCaptchaState?.CaptchaRequired
                });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器初始化失敗");
                LogActionResponse("初始化", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        /// <summary>
        /// 刷新登入驗證碼。
        /// </summary>
        public async Task<JsonResult> RefreshLoginCaptcha()
        {
            try
            {
                LogActionRequest("刷新登入驗證碼", new { });
                var result = await _ezwayService.RefreshLoginCaptchaAsync();
                var captchaState = result.ReturnObject as EzwayCaptchaState;
                LogActionResponse("刷新登入驗證碼", new
                {
                    result.IsSuccess,
                    result.msg,
                    CaptchaRequired = captchaState?.CaptchaRequired
                });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器刷新登入驗證碼失敗");
                LogActionResponse("刷新登入驗證碼", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        /// <summary>
        /// 執行 Ezway 登入。
        /// </summary>
        public async Task<JsonResult> Login(EzwayLoginRequest request)
        {
            try
            {
                LogActionRequest("登入", new
                {
                    request?.LoginProfileKey,
                    request?.LoginProfileLabel,
                    request?.CompanyId,
                    request?.Account,
                    request?.CaptchaRequired,
                    request?.TermsAccepted
                });

                var result = await _ezwayService.LoginAsync(request);
                var loginResult = result.ReturnObject as EzwayLoginResult;
                LogActionResponse("登入", new
                {
                    result.IsSuccess,
                    result.msg,
                    IsLoggedIn = loginResult?.IsLoggedIn,
                    RequiresTermsAgreement = loginResult?.RequiresTermsAgreement,
                    LoginProfile = loginResult?.CurrentAccount?.LoginProfileLabel,
                    Account = loginResult?.CurrentAccount?.Account
                });
                return Json(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器登入失敗");
                LogActionResponse("登入", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        /// <summary>
        /// 啟用指定的已登入 Ezway 帳號。
        /// </summary>
        public async Task<JsonResult> ActivateAccount(EzwayActivateAccountRequest request)
        {
            try
            {
                LogActionRequest("啟用已登入帳號", new { request?.AccountSessionKey });
                var result = await _ezwayService.ActivateLoggedInAccountAsync(request);
                var pageState = result.ReturnObject as EzwayPageState;
                LogActionResponse("啟用已登入帳號", new
                {
                    result.IsSuccess,
                    result.msg,
                    IsLoggedIn = pageState?.IsLoggedIn,
                    LoginProfile = pageState?.CurrentAccount?.LoginProfileLabel,
                    Account = pageState?.CurrentAccount?.Account,
                    QueryCaptchaRequired = pageState?.QueryCaptchaState?.CaptchaRequired
                });
                return Json(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器啟用已登入帳號失敗");
                LogActionResponse("啟用已登入帳號", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        /// <summary>
        /// 清除 Ezway 登入資訊。
        /// </summary>
        public async Task<JsonResult> Logout()
        {
            try
            {
                LogActionRequest("登出", new { });
                var result = await _ezwayService.LogoutAsync();
                var pageState = result.ReturnObject as EzwayPageState;
                LogActionResponse("登出", new
                {
                    result.IsSuccess,
                    result.msg,
                    IsLoggedIn = pageState?.IsLoggedIn,
                    LoggedInAccounts = pageState?.LoggedInAccounts?.Count ?? 0,
                    LoginCaptchaRequired = pageState?.LoginCaptchaState?.CaptchaRequired
                });
                return Json(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器登出失敗");
                LogActionResponse("登出", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpGet]
        /// <summary>
        /// 取得查詢驗證碼設定。
        /// </summary>
        public async Task<JsonResult> QuerySetting()
        {
            try
            {
                LogActionRequest("取得查詢驗證碼設定", new { });
                var result = await _ezwayService.GetQueryCaptchaStateAsync();
                var captchaState = result.ReturnObject as EzwayCaptchaState;
                LogActionResponse("取得查詢驗證碼設定", new
                {
                    result.IsSuccess,
                    result.msg,
                    CaptchaRequired = captchaState?.CaptchaRequired
                });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器取得查詢驗證碼設定失敗");
                LogActionResponse("取得查詢驗證碼設定", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        /// <summary>
        /// 取得 Ezway 海運簡易查詢所需的下拉選單資料。
        /// </summary>
        public async Task<JsonResult> QueryOptions()
        {
            try
            {
                LogActionRequest("取得海運查詢下拉", new { });
                var result = await _ezwayService.GetSeaQueryOptionsAsync();
                var queryOptions = result.ReturnObject as EzwaySeaQueryOptions;
                LogActionResponse("取得海運查詢下拉", new
                {
                    result.IsSuccess,
                    result.msg,
                    BrokerQueryField = queryOptions?.BrokerQueryField,
                    BrokerOptions = queryOptions?.BrokerOptions?.Count ?? 0,
                    ConsolidatorOptions = queryOptions?.ConsolidatorOptions?.Count ?? 0
                });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 海運控制器取得查詢下拉失敗");
                LogActionResponse("取得海運查詢下拉", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        /// <summary>
        /// 執行單筆分提單查詢。
        /// </summary>
        public async Task<JsonResult> Query(EzwayQueryRequest request)
        {
            try
            {
                LogActionRequest("單筆查詢", new
                {
                    request?.QueryApiType,
                    request?.Manual,
                    request?.GroupUserId,
                    request?.BrokerUserId,
                    request?.Consolidator,
                    request?.ConsolidatorUserId,
                    HawbCount = CountHawb(request?.HawbNo),
                    request?.QueryCaptchaRequired
                });

                var result = await _ezwayService.QuerySingleAsync(request);
                var queryResponse = result.ReturnObject as EzwayQueryResponse;
                LogActionResponse("單筆查詢", new
                {
                    result.IsSuccess,
                    result.msg,
                    ResultCount = queryResponse?.Results?.Count ?? 0,
                    CaptchaRequired = queryResponse?.QueryCaptchaState?.CaptchaRequired
                });
                return Json(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器單筆查詢失敗");
                LogActionResponse("單筆查詢", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        /// <summary>
        /// 執行整批查詢。
        /// </summary>
        public async Task<JsonResult> BatchQuery(EzwayQueryRequest request)
        {
            try
            {
                LogActionRequest("整批查詢", new
                {
                    request?.QueryApiType,
                    request?.Manual,
                    request?.GroupUserId,
                    request?.BrokerUserId,
                    request?.Consolidator,
                    request?.ConsolidatorUserId,
                    HawbCount = CountHawb(request?.HawbNo),
                    request?.QueryCaptchaRequired
                });

                var result = await _ezwayService.QueryBatchAsync(request);
                var queryResponse = result.ReturnObject as EzwayQueryResponse;
                LogActionResponse("整批查詢", new
                {
                    result.IsSuccess,
                    result.msg,
                    ResultCount = queryResponse?.Results?.Count ?? 0,
                    CaptchaRequired = queryResponse?.QueryCaptchaState?.CaptchaRequired
                });
                return Json(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器整批查詢失敗");
                LogActionResponse("整批查詢", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        /// <summary>
        /// 將 Ezway 查詢結果匯出為 Excel。
        /// </summary>
        public async Task<JsonResult> ExportExcel(EzwayExportRequest request)
        {
            try
            {
                LogActionRequest("匯出Excel", new
                {
                    ResultsCount = request?.Results?.Count ?? 0,
                    HawbCount = CountHawb(request?.QueryRequest?.HawbNo)
                });

                var workbook = await _ezwayService.ExportExcelAsync(request);

                string handle = Guid.NewGuid().ToString();
                string queryTitle = ResolveExportQueryTitle(request?.QueryRequest?.QueryApiType);
                string fileName = $"{queryTitle}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                using (var fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }

                LogActionResponse("匯出Excel", new { fileGuid = handle, fileName = fileName });
                return Json(new { fileGuid = handle, fileName = fileName, msg = string.Empty });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ezway 控制器匯出Excel失敗");
                LogActionResponse("匯出Excel", new { Success = false, Message = ex.Message });
                return Json(new ResponseModel($"匯出失敗：{ex.Message}"));
            }
        }

        /// <summary>
        /// 記錄 controller request 摘要。
        /// </summary>
        private static void LogActionRequest(string actionName, object payload)
        {
            Logger.Info($"Ezway 控制器請求記錄：動作={actionName}, 內容={JsonConvert.SerializeObject(payload)}");
        }

        /// <summary>
        /// 記錄 controller response 摘要。
        /// </summary>
        private static void LogActionResponse(string actionName, object payload)
        {
            Logger.Info($"Ezway 控制器回應記錄：動作={actionName}, 內容={JsonConvert.SerializeObject(payload)}");
        }

        /// <summary>
        /// 計算分提單號輸入筆數。
        /// </summary>
        private static int CountHawb(string hawbNumbersText)
        {
            if (string.IsNullOrWhiteSpace(hawbNumbersText))
            {
                return 0;
            }

            return hawbNumbersText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value?.Trim())
                .Count(value => !string.IsNullOrWhiteSpace(value));
        }

        /// <summary>
        /// 依查詢頁類型回傳匯出檔名使用的中文標題。
        /// </summary>
        private static string ResolveExportQueryTitle(string queryApiType)
        {
            return string.Equals(queryApiType, "X4", StringComparison.OrdinalIgnoreCase)
                ? "預先委任確認查詢(X4)"
                : "預先委任確認查詢(簡易)";
        }
    }
}
