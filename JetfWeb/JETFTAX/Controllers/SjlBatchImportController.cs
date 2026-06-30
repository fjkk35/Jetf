using Service.EnumTax;
using Service.Models;
using Service.Services.SjlBatchImport;
using System;
using System.Collections.Generic;
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
        public JsonResult Upload(HttpPostedFileBase[] files)
        {
            ResponseModel resopnseModel = new ResponseModel();
            try
            {
                var uploadFiles = GetUploadFiles(files);
                var selectedFileCount = GetSelectedFileCount();
                if (selectedFileCount.HasValue && selectedFileCount.Value != uploadFiles.Count)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = $"本次選擇 {selectedFileCount.Value} 個檔案，但後端僅收到 {uploadFiles.Count} 個檔案，請重新選擇檔案後再上傳";
                    return Json(resopnseModel);
                }

                if (uploadFiles.Count == 0)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "未選擇檔案";
                    return Json(resopnseModel);
                }

                foreach (var file in uploadFiles)
                {
                    var fileType = Path.GetExtension(file.FileName);
                    if (!string.Equals(fileType, ".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = "副檔名需為 xlsx";
                        return Json(resopnseModel);
                    }
                }

                var uploadFolder = Server.MapPath("~/UploadFIle");
                Directory.CreateDirectory(uploadFolder);

                var savedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var uploadTime = DateTime.Now;
                for (var i = 0; i < uploadFiles.Count; i++)
                {
                    var file = uploadFiles[i];
                    var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{uploadTime:yyyyMMddHHmmssfff}_{i + 1}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadFolder, fileName);
                    file.SaveAs(filePath);
                    savedFiles.Add(filePath, Path.GetFileName(file.FileName));
                }

                resopnseModel = _sjlBatchImportService.Upload(savedFiles);
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel);
        }

        private List<HttpPostedFileBase> GetUploadFiles(HttpPostedFileBase[] boundFiles)
        {
            var boundFileList = new List<HttpPostedFileBase>();
            if (boundFiles != null)
            {
                foreach (var file in boundFiles)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        boundFileList.Add(file);
                    }
                }
            }

            var requestFileList = new List<HttpPostedFileBase>();
            for (var i = 0; i < Request.Files.Count; i++)
            {
                var file = Request.Files[i];
                if (file != null && file.ContentLength > 0)
                {
                    requestFileList.Add(file);
                }
            }

            return requestFileList.Count >= boundFileList.Count
                ? requestFileList
                : boundFileList;
        }

        private int? GetSelectedFileCount()
        {
            int fileCount;
            if (int.TryParse(Request.Form["fileCount"], out fileCount) && fileCount > 0)
            {
                return fileCount;
            }

            return null;
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
