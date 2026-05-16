using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.SearchCargo;
using Service.Services.SearchCargo.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SearchCargoController : Controller
    {
        private readonly SearchCargoService _searchCargoService;

        public SearchCargoController(SearchCargoService searchCargoService)
        {
            _searchCargoService = searchCargoService;
        }

        // GET: SearchCargo
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢貨況列表
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult SearchData(SearchCargoRequest request)
        {
            try
            {
                var result = _searchCargoService.SearchCargo(request);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 取得貨況明細
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult Detail(string id, string source)
        {
            return View();
        }

        /// <summary>
        /// 取得貨況明細資料
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult GetDetail(string id, string source)
        {
            try
            {
            var detail = _searchCargoService.GetCargoDetail(source, id);

                if (detail == null)
                {
                    return Json(new { success = false, error = "查無資料" });
                }

                return Json(new { success = true, data = detail }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 取得處置說明列表
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult GetProcessList(string dlv_inv)
        {
            try
            {
                var list = _searchCargoService.GetProcess(dlv_inv);

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 新增處置說明
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult AddProcess(HttpPostedFileBase file, string dlv_inv, string process_type, string remark)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(remark))
                {
                    return Json(new { status = Status.error, msg = "請輸入處置說明" });
                }

                var model = new ProcessModel()
                {
                    Dlv_Inv = dlv_inv,
                    Process_Type = process_type,
                    DataDate = DateTime.Now.ToString("yyyyMMdd"),
                    Remark = remark,
                    User_Id = Session["user_id"]?.ToString()
                };

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(@"D:\UploadProcess", fileName);
                    #if DEBUG
                         filePath = Path.Combine(@"F:\UploadProcess", fileName);
                    #endif
                    file.SaveAs(filePath);

                    model.FileName = file.FileName;
                    model.FilePath = filePath;
                }

                var result = _searchCargoService.InsertProcess(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { status = Status.error, msg = ex.Message });
            }
        }

        /// <summary>
        /// 處置說明結案
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult FinishProcess(string dlv_inv)
        {
            try
            {
                var result = _searchCargoService.FinishProcess(dlv_inv, Session["user_id"]?.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { status = Status.error, msg = ex.Message });
            }
        }

        /// <summary>
        /// 刪除處置說明
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult DeleteProcess(string id)
        {
            try
            {
                var result = _searchCargoService.DeleteProcess(id, Session["user_id"]?.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { status = Status.error, msg = ex.Message });
            }
        }

        /// <summary>
        /// 取得貨況查詢紀錄
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult GetLogCargoStatus(string dlv_inv)
        {
            try
            {
                var list = _searchCargoService.GetLogCargoStatus(dlv_inv);
                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 取得通關袋號明細
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult GetCargoTargetBagNumber(string bagNumber)
        {
            try
            {
                var list = _searchCargoService.GetCargoTargetBagNumber(bagNumber);
                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 取得通關分提單號明細
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult GetCargoTargetTrackingNo(string bagNumber)
        {
            try
            {
                var list = _searchCargoService.GetCargoTargetTrackingNo(bagNumber);
                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 取得速派新遞貨號資料
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult GetShenzhenCargoByTrackingNo(string trackingNo)
        {
            try
            {
                var list = _searchCargoService.GetShenzhenCargoByTrackingNo(trackingNo);
                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}