using Service.Extensions;
using Service.EnumTax;
using Service.Models;
using Service.Services.ShipmentInboundRecord;
using Service.Services.ShipmentInboundRecord.Domain;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentInboundRecordController : Controller
    {
        private readonly ShipmentInboundRecordService _shipmentInboundRecordService;

        public ShipmentInboundRecordController(ShipmentInboundRecordService shipmentInboundRecordService)
        {
            _shipmentInboundRecordService = shipmentInboundRecordService;
        }

        // GET: ShipmentInboundRecord
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 詳細頁面
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public ActionResult Detail()
        {
            return View();
        }

        /// <summary>
        /// 取得詳細資料 API
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetDetailById(int id)
        {
            try
            {
                var data = _shipmentInboundRecordService.GetDetailById(id);
                if (data == null)
                {
                    return Json(new { error = "查無資料" }, JsonRequestBehavior.AllowGet);
                }

                data.ExceptionFilePaths = data.ExceptionFilePaths
                    .Select(ToImageDataUrl)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                return new JsonResult
                {
                    Data = new { Data = data },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    MaxJsonLength = int.MaxValue
                };
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private string ToImageDataUrl(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            try
            {
                var physicalPath = filePath;
                if (!Path.IsPathRooted(physicalPath))
                {
                    var relativePath = filePath.StartsWith("~")
                        ? filePath
                        : "~/" + filePath.TrimStart('~', '/', '\\').Replace('\\', '/');

                    physicalPath = Server.MapPath(relativePath);
                }

                if (!System.IO.File.Exists(physicalPath))
                {
                    return null;
                }

                var mimeType = System.Web.MimeMapping.GetMimeMapping(physicalPath);
                var fileBytes = System.IO.File.ReadAllBytes(physicalPath);
                return $"data:{mimeType};base64,{Convert.ToBase64String(fileBytes)}";
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult SearchData(ShipmentInboundRecordRequest searchRequest)
        {
            try
            {
                var result = _shipmentInboundRecordService.GetData(searchRequest);

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

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
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
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetWarehouseProcessTypeList()
        {
            var list = Enum.GetValues(typeof(WarehouseProcessType))
                .Cast<WarehouseProcessType>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 取得客戶清單
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetCustList()
        {
            try
            {
                var list = _shipmentInboundRecordService.GetCustList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得貨物來源清單
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetSourceTypeList()
        {
            try
            {
                var list = _shipmentInboundRecordService.GetSourceTypeList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 匯出Excel
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult ExportExcel(ShipmentInboundRecordRequest searchRequest)
        {
            try
            {
                var result = _shipmentInboundRecordService.GetExportExcel(searchRequest);

                var handle = Guid.NewGuid().ToString();
                TempData[handle] = result.FileBytes;

                return Json(new { fileGuid = handle, fileName = result.FileName, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { fileGuid = "", fileName = "", msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 更新金額
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult UpdateAmount(UpdateAmountRequest request)
        {
            try
            {
                _shipmentInboundRecordService.UpdateAmount(request);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得編輯歷史記錄
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetEditHistory(int shipmentInboundId)
        {
            try
            {
                var data = _shipmentInboundRecordService.GetEditHistory(shipmentInboundId);
                return Json(new { Data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
