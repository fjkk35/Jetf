using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundProcess;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentInboundProcessController : Controller
    {
        private readonly ShipmentInboundProcessService _shipmentInboundProcessService;

        public ShipmentInboundProcessController(ShipmentInboundProcessService shipmentInboundProcessService)
        {
            _shipmentInboundProcessService = shipmentInboundProcessService;
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult SearchData(ShipmentInboundProcessRequest searchRequest)
        {
            try
            {
                var result = _shipmentInboundProcessService.GetData(searchRequest);

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
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public ActionResult ExportExcel(ShipmentInboundProcessRequest searchRequest)
        {
            try
            {
                var fileBytes = _shipmentInboundProcessService.ExportExcel(searchRequest);

                string startDate = DateTime.TryParse(searchRequest.InboundDateStart, out var sd) ? sd.ToString("yyyyMMdd") : "";
                string endDate = DateTime.TryParse(searchRequest.InboundDateEnd, out var ed) ? ed.ToString("yyyyMMdd") : "";
                string fileName = $"貨件退件處理_{startDate}_{endDate}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetProcessTypeList()
        {
            var list = Enum.GetValues(typeof(ShipmentInboundProcessType))
                .Cast<ShipmentInboundProcessType>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetProcessTransNoList()
        {
            var list = Enum.GetValues(typeof(ShipmentInboundProcessTransNo))
                .Cast<ShipmentInboundProcessTransNo>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetFreightPayerNoList()
        {
            var list = Enum.GetValues(typeof(ShipmentInboundFreightPayerNo))
                .Cast<ShipmentInboundFreightPayerNo>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult UpdateProcessType(ShipmentInboundProcessUpdateRequest request)
        {
            try
            {
                var result = _shipmentInboundProcessService.UpdateProcessType(request);

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

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetDetailById(int id)
        {
            try
            {
                var detail = _shipmentInboundProcessService.GetDetailById(id);
                return Json(detail, JsonRequestBehavior.AllowGet);
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
        /// 取得貨物來源清單
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetSourceTypeList()
        {
            try
            {
                var list = _shipmentInboundProcessService.GetSourceTypeList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 批量上傳(Excel)：依單號更新貨件回倉處理
        /// Excel 欄位：單號、處理方式、備註
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
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

                resopnseModel = _shipmentInboundProcessService.BatchUpload(filePath);
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel);
        }

        /// <summary>
        /// 更新退件原因
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult UpdateReturnReason(int id, string returnReason)
        {
            try
            {
                _shipmentInboundProcessService.UpdateReturnReason(id, returnReason);
                return Json(new ResponseModel());
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel
                {
                    status = Status.error,
                    msg = ex.Message
                });
            }
        }

        /// <summary>
        /// 批量上傳退件原因
        /// Excel 欄位：單號、退件原因
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult BatchUploadReturnReason(HttpPostedFileBase file)
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

                resopnseModel = _shipmentInboundProcessService.BatchUploadReturnReason(filePath);
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