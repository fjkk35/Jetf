using Service.EnumTax;
using Service.Models;
using Service.Services.BusinessRegistryNew;
using Service.Services.Importer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ImporterController : Controller
    {
        private readonly ImporterService _importerService;

        public ImporterController(ImporterService importerService)
        {
            _importerService = importerService;
        }

        [UserAuthorize(Authority.Importer)]
        public ActionResult Index()
        {
            return View();
        }

        [UserAuthorize(Authority.Importer)]
        public async Task<ActionResult> Search(ImporterSearchType type, string phoneOrId)
        {
            try
            {
                var list = phoneOrId
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim())
                    .ToList();

                if (list.Count > 100)
                    return Json(new ResopnseModel()
                    {
                        msg = "一次最多只能查詢100筆"
                    });

                var result = await _importerService.Search(type, list);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel()
                {
                    msg = ex.Message
                });
            }
        }

        [UserAuthorize(Authority.Importer)]
        public async Task<ActionResult> ExportExcel(ImporterSearchType type, string phoneOrId)
        {
            try
            {
                var list = phoneOrId
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim())
                    .ToList();

                if (list.Count > 100)
                    return Json(new
                    {
                        success = false,
                        message = "一次最多只能匯出100筆資料"
                    });

                // 呼叫 Service 層的匯出方法
                var result = await _importerService.ExportExcel(type, list);

                if (result.success)
                {
                    // 將檔案內容存入 TempData 供下載使用
                    string handle = Guid.NewGuid().ToString();
                    TempData[handle] = result.fileData;

                    return Json(new
                    {
                        success = true,
                        fileGuid = handle,
                        fileName = result.fileName,
                        recordCount = result.recordCount
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.message
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"匯出失敗：{ex.Message}"
                });
            }
        }
    }
}