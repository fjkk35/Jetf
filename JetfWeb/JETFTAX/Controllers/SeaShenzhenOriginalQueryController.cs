using Service.EnumTax;
using Service.Services.SeaShenzhenOriginal;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 新遞託運資料查詢控制器。
    /// </summary>
    public class SeaShenzhenOriginalQueryController : Controller
    {
        private readonly SeaShenzhenOriginalService _seaShenzhenOriginalService;

        public SeaShenzhenOriginalQueryController(SeaShenzhenOriginalService seaShenzhenOriginalService)
        {
            _seaShenzhenOriginalService = seaShenzhenOriginalService;
        }

        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult SearchData(SeaShenzhenOriginalQueryRequest request)
        {
            try
            {
                var result = _seaShenzhenOriginalService.GetData(request);

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
    }
}