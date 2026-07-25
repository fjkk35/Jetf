using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services;
using Service.Services.ReconciliationLogistics;
using Service.Services.ReconciliationLogistics.Domain;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 物流銷帳控制器。
    /// </summary>
    public sealed class ReconciliationLogisticsController : Controller
    {
        private readonly ReconciliationLogisticsService _service;
        private static readonly SemaphoreSlim UploadSemaphore =
            new SemaphoreSlim(1, 1);
        private static string UploadExecutingUserId = string.Empty;

        /// <summary>
        /// 建立物流銷帳控制器。
        /// </summary>
        /// <param name="service">物流銷帳服務。</param>
        public ReconciliationLogisticsController(ReconciliationLogisticsService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示物流銷帳頁面。
        /// </summary>
        /// <returns>物流銷帳頁面。</returns>
        [UserAuthorize(Authority.ReconciliationLogistics)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得支援的物流公司選項。
        /// </summary>
        /// <returns>物流公司下拉選項。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationLogistics)]
        public JsonResult GetCompanies()
        {
            try
            {
                var options = Enum.GetValues(typeof(ReconciliationLogisticsCompany))
                    .Cast<ReconciliationLogisticsCompany>()
                    .Select(x => new
                    {
                        Value = ((int)x).ToString(),
                        Text = x.ToDescription(),
                        FileExtension =
                            x == ReconciliationLogisticsCompany.Ktj
                                ? ".csv"
                                : ".xlsx"
                    })
                    .ToList();

                return Json(new ResponseModel(options), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得物流銷帳狀態下拉選項。
        /// </summary>
        /// <returns>物流銷帳狀態下拉選項。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationLogistics)]
        public JsonResult GetStatuses()
        {
            try
            {
                var options = Enum
                    .GetValues(typeof(ReconciliationLogisticsResultStatus))
                    .Cast<ReconciliationLogisticsResultStatus>()
                    .Select(x => new
                    {
                        Value = ((int)x).ToString(),
                        Text = x.ToDescription()
                    })
                    .ToList();

                return Json(new ResponseModel(options), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 分頁查詢物流銷帳紀錄。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>物流銷帳分頁資料。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationLogistics)]
        public JsonResult Search(ReconciliationLogisticsQueryRequest request)
        {
            try
            {
                return Json(new ResponseModel(_service.Search(request)));
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 上傳物流銷帳檔案。
        /// </summary>
        /// <param name="file">上傳檔案。</param>
        /// <param name="company">物流公司。</param>
        /// <param name="repaymentDate">回款日期。</param>
        /// <returns>上傳與銷帳結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationLogistics)]
        public JsonResult Upload(
            HttpPostedFileBase file,
            ReconciliationLogisticsCompany? company,
            DateTime? repaymentDate)
        {
            var responseModel = new ResponseModel();
            if (!UploadSemaphore.Wait(0))
            {
                responseModel.status = Status.error;
                responseModel.msg =
                    $"[{UploadExecutingUserId}]正在執行物流銷帳，請等待執行完成後再試";
                return Json(responseModel);
            }

            try
            {
                UploadExecutingUserId = UserContextService.GetUserId();

                if (!repaymentDate.HasValue)
                {
                    return Json(new ResponseModel("請選擇回款日期"));
                }

                if (!company.HasValue ||
                    !Enum.IsDefined(typeof(ReconciliationLogisticsCompany), company.Value))
                {
                    return Json(new ResponseModel("請選擇物流公司"));
                }

                if (file == null || file.ContentLength == 0)
                {
                    return Json(new ResponseModel("請選擇檔案"));
                }

                var expectedExtension =
                    company.Value == ReconciliationLogisticsCompany.Ktj
                        ? ".csv"
                        : ".xlsx";
                if (!string.Equals(
                    Path.GetExtension(file.FileName),
                    expectedExtension,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new ResponseModel(
                        $"{company.Value.ToDescription()}上傳檔案副檔名需為 " +
                        expectedExtension.TrimStart('.')));
                }

                var response = _service.Upload(
                    file.InputStream,
                    file.FileName,
                    company.Value,
                    repaymentDate.Value);
                var result = response.ReturnObject as ReconciliationLogisticsUploadResult;
                if (result != null && (result.Results.Any() || result.Data.Any()))
                {
                    try
                    {
                        // 直接使用本次完整結果產生 Excel，包含未寫入資料庫的驗證及比對失敗明細。
                        var fileBytes = _service.ExportExcel(
                            result,
                            company.Value,
                            repaymentDate.Value);
                        result.FileGuid = Guid.NewGuid().ToString();
                        result.FileName = $"物流銷帳結果_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                        // 下載端只需帶回識別碼及檔名，實際檔案暫存在伺服器端 TempData。
                        TempData[result.FileGuid] = fileBytes;
                    }
                    catch (Exception ex)
                    {
                        // Excel 產生失敗不改變原本的銷帳或驗證結果，避免成功資料被誤判為整批失敗。
                        result.FileGuid = null;
                        result.FileName = null;
                        result.ExcelErrorMessage = response.IsSuccess
                            ? $"銷帳處理已完成，但 Excel 產生失敗：{ex.GetBaseException().Message}"
                            : $"Excel 產生失敗：{ex.GetBaseException().Message}";
                        response.msg = $"{result.Message}；{result.ExcelErrorMessage}";
                    }
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
            finally
            {
                UploadExecutingUserId = string.Empty;
                UploadSemaphore.Release();
            }
        }

    }
}
