using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundWarehouseProcess;
using Service.Services.ShipmentInboundWarehouseProcess.Domain;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentInboundWarehouseProcessController : Controller
    {
        private readonly ShipmentInboundWarehouseProcessService _service;

        public ShipmentInboundWarehouseProcessController(ShipmentInboundWarehouseProcessService service)
        {
            _service = service;
        }

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢資料
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundWarehouseProcess)]
        public JsonResult SearchData(ShipmentInboundWarehouseProcessRequest request)
        {
            try
            {
                var result = _service.GetData(request);

                return Json(new
                {
                    Data = result
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

        /// <summary>
        /// 取得處理狀態清單
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundWarehouseProcess)]
        public JsonResult GetWarehouseProcessTypeList()
        {
            var list = Enum.GetValues(typeof(WarehouseProcessType))
                .Cast<WarehouseProcessType>()
                .Where(item => item == WarehouseProcessType.PendingDisposal
                    || item == WarehouseProcessType.PendingReturn
                    || item == WarehouseProcessType.OnHold)
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 更新處理狀態
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundWarehouseProcess)]
        public JsonResult UpdateProcessType(ShipmentInboundWarehouseProcessUpdateRequest request)
        {
            try
            {
                _service.UpdateProcessType(request);

                return Json(new ResponseModel());
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel
                {
                    status = "error",
                    msg = ex.Message
                });
            }
        }

        /// <summary>
        /// 批量上傳(Excel)：依單號更新倉庫處理狀態
        /// Excel 欄位：單號、處理狀態(中文)
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundWarehouseProcess)]
        public JsonResult BatchUpload(HttpPostedFileBase file)
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

                resopnseModel = _service.BatchUpload(filePath);
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel);
        }
    }
}