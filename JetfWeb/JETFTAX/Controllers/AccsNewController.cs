using Service.Models;
using Service.Services.AccsNew;
using Service.Services.AccsNew.Domain;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class AccsNewController : Controller
    {
        private readonly AccsNewService _accsNewService;

        public AccsNewController(AccsNewService accsNewService)
        {
            _accsNewService = accsNewService;
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetVerifyCode()
        {
            try
            {
                var result = await _accsNewService.GetVerifyCodeImageAsync();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Login(AccsNewLoginRequest request)
        {
            try
            {
                var result = await _accsNewService.LoginAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        public async Task<JsonResult> Query(AccsNewQueryRequest request)
        {
            try
            {
                var result = await _accsNewService.QueryAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        public async Task<JsonResult> ExportExcel(AccsNewQueryRequest request)
        {
            try
            {
                var workbook = await _accsNewService.ExportExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Accs關貿空運查詢_新_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }

                return new JsonResult()
                {
                    Data = new { fileGuid = handle, fileName = fileName, msg = "" }
                };
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel($"匯出失敗：{ex.Message}"));
            }
        }
    }
}