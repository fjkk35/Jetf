using Service.EnumTax;
using Service.Models;
using Service.Services.SjlBatchImport;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SjlBatchImportController : Controller
    {
        private readonly SjlBatchImportService _sjlBatchImportService;

        public SjlBatchImportController(SjlBatchImportService sjlBatchImportService)
        {
            _sjlBatchImportService = sjlBatchImportService;
        }

        // GET: SjlBatchImport
        [UserAuthorize(Authority.SjlBatchImport)]
        public ActionResult Index()
        {
            return View();
        }

        [UserAuthorize(Authority.SjlBatchImport)]
        public ActionResult Search()
        {
            return View();
        }

        /// <summary>
        /// 上傳捷利托運資料。
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SjlBatchImport)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            ResponseModel resopnseModel = new ResponseModel();
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "未選擇檔案";
                    return Json(resopnseModel);
                }

                var fileType = Path.GetExtension(file.FileName);
                if (fileType != ".xlsx")
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "副檔名需為 xlsx";
                    return Json(resopnseModel);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                resopnseModel = _sjlBatchImportService.Upload(filePath);
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel);
        }

        [HttpPost]
        [UserAuthorize(Authority.SjlBatchImport)]
        public JsonResult SearchData(Service.Services.SjlBatchImport.Domain.SjlBatchImportSearchRequest request)
        {
            try
            {
                var result = _sjlBatchImportService.GetSearchData(request);
                return Json(new
                {
                    Data = result.Data,
                    TotalCount = result.TotalCount
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.SjlBatchImport)]
        public JsonResult UpdateTransName(Service.Services.SjlBatchImport.Domain.SjlShippingDataUpdateTransNameRequest request)
        {
            try
            {
                var result = _sjlBatchImportService.UpdateTransName(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}
