using Service.Extensions;
using Service.EnumTax;
using Service.Models;
using Service.Services.ShipmentInboundCommon;
using Service.Services.ShipmentInboundRecord;
using Service.Services.ShipmentInboundRecord.Domain;
using System;
using System.Linq;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentInboundRecordController : Controller
    {
        private readonly ShipmentInboundRecordService _shipmentInboundRecordService;
        private readonly ShipmentInboundExceptionImageStorageService _imageStorageService;

        public ShipmentInboundRecordController(
            ShipmentInboundRecordService shipmentInboundRecordService,
            ShipmentInboundExceptionImageStorageService imageStorageService)
        {
            _shipmentInboundRecordService = shipmentInboundRecordService;
            _imageStorageService = imageStorageService;
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

                data.ExceptionImages = data.ExceptionImages
                    .Select((x, index) =>
                    {
                        x.ImageUrl = Url.Action("GetExceptionImage", "ShipmentInboundRecord", new { id = data.Id, imageIndex = index });
                        return x;
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
                    .ToList();
                data.ExceptionFilePaths = data.ExceptionImages.Select(x => x.ImageUrl).ToList();

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

        /// <summary>
        /// 取得異常圖片。
        /// </summary>
        /// <param name="id">貨件入庫資料主鍵。</param>
        /// <param name="imageIndex">圖片索引。</param>
        /// <returns>圖片檔案。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public ActionResult GetExceptionImage(int id, int imageIndex)
        {
            try
            {
                var filePath = _shipmentInboundRecordService.GetExceptionImagePath(id, imageIndex);
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return HttpNotFound();
                }

                var fileBytes = _imageStorageService.ReadAllBytes(filePath);
                if (fileBytes == null || fileBytes.Length == 0)
                {
                    return HttpNotFound();
                }

                var mimeType = System.Web.MimeMapping.GetMimeMapping(filePath);
                return File(fileBytes, mimeType);
            }
            catch
            {
                return HttpNotFound();
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
        /// 依進口方式取得不明貨件可選客戶清單。
        /// </summary>
        /// <param name="dataType">進口方式。</param>
        /// <returns>客戶下拉選單資料。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetUnknownShipmentCustList(string dataType)
        {
            try
            {
                var list = _shipmentInboundRecordService.GetUnknownShipmentCustList(dataType);
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 依進口方式取得不明貨件可選派件公司清單。
        /// </summary>
        /// <param name="dataType">進口方式。</param>
        /// <returns>派件公司下拉選單資料。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetUnknownShipmentTransList(string dataType)
        {
            try
            {
                var list = _shipmentInboundRecordService.GetUnknownShipmentTransList(dataType);
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
        /// 匯出客戶版 Excel。
        /// </summary>
        /// <param name="searchRequest">查詢條件。</param>
        /// <returns>客戶版 Excel 檔案。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult ExportCustomerExcel(ShipmentInboundRecordRequest searchRequest)
        {
            try
            {
                var result = _shipmentInboundRecordService.GetCustomerExportExcel(searchRequest);

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
        /// 更新單號
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult UpdateTrackingNo(UpdateTrackingNoRequest request)
        {
            try
            {
                _shipmentInboundRecordService.UpdateTrackingNo(request);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 更新貨件來源。
        /// </summary>
        /// <param name="request">更新請求。</param>
        /// <returns>更新結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult UpdateSourceType(UpdateSourceTypeRequest request)
        {
            try
            {
                _shipmentInboundRecordService.UpdateSourceType(request);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 更新不明貨件的基本資料。
        /// </summary>
        /// <param name="request">更新請求。</param>
        /// <returns>更新結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult UpdateUnknownShipmentBasicInfo(UpdateUnknownShipmentBasicInfoRequest request)
        {
            try
            {
                _shipmentInboundRecordService.UpdateUnknownShipmentBasicInfo(request);
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
