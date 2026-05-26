using Service.EnumTax;
using Service.Models;
using Service.Services.BatchSearchCargo2;
using Service.Services.BatchSearchCargo2.Domain;
using System;
using System.IO;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class BatchSearchCargo2Controller : Controller
    {
        private readonly BatchSearchCargo2Service _batchSearchCargo2Service;

        public BatchSearchCargo2Controller(BatchSearchCargo2Service batchSearchCargo2Service)
        {
            _batchSearchCargo2Service = batchSearchCargo2Service;
        }

        [UserAuthorize(Authority.BatchSearchCargo)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.BatchSearchCargo)]
        public JsonResult ExportExcel(BatchSearchCargo2Request request)
        {
            try
            {
                var userId = Convert.ToString(Session["user_id"]);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new ResponseModel("登入狀態已失效，請重新登入"));
                }

                var workbook = _batchSearchCargo2Service.ExportExcel(request, userId);
                string handle = Guid.NewGuid().ToString();
                string fileName = $"批量貨況查詢明細表_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                using (var fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }

                return Json(new
                {
                    fileGuid = handle,
                    fileName,
                    msg = string.Empty
                });
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel($"匯出失敗：{ex.Message}"));
            }
        }
    }
}