using Service.EnumTax;
using Service.Services.CoupangReportForm;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CoupangReportFormController : Controller
    {
        private readonly CoupangReportFormService _coupangReportFormService;

        public CoupangReportFormController(CoupangReportFormService coupangReportFormService)
        {
            _coupangReportFormService = coupangReportFormService;
        }

        [UserAuthorize(Authority.EtlClearanceMainDetails)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.EtlClearanceMainDetails)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = string.Empty;
            var msg = string.Empty;

            try
            {
                // 前端只接受 xlsx，後端再做一次基本檔案驗證，避免非 Excel 檔進入 NPOI 處理。
                if (file == null || file.ContentLength <= 0)
                {
                    msg = "未選擇檔案";
                }
                else if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    msg = "副檔名需為xlsx";
                }
                else
                {
                    fileName = Path.GetFileName(file.FileName);
                    var uploadFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), uploadFileName);
                    file.SaveAs(filePath);

                    // Service 直接以原 workbook 補值，Controller 只負責暫存結果供既有 DownloadFile 下載。
                    var workbook = _coupangReportFormService.BuildWorkbook(filePath);
                    using (var fileStream = new MemoryStream())
                    {
                        workbook.Write(fileStream);
                        TempData[handle] = fileStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                fileName = string.Empty;
            }

            return Json(new { fileGuid = handle, fileName = fileName, msg = msg }, JsonRequestBehavior.AllowGet);
        }
    }
}
