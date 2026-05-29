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
using JETFTAX.Hubs;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 貨件回倉處理頁面的查詢、編輯與批次作業控制器。
    /// </summary>
    public class ShipmentInboundProcessController : Controller
    {
        private readonly ShipmentInboundProcessService _shipmentInboundProcessService;

        /// <summary>
        /// 初始化貨件回倉處理控制器。
        /// </summary>
        /// <param name="shipmentInboundProcessService">貨件回倉處理服務。</param>
        public ShipmentInboundProcessController(ShipmentInboundProcessService shipmentInboundProcessService)
        {
            _shipmentInboundProcessService = shipmentInboundProcessService;
        }

        /// <summary>
        /// 顯示貨件回倉處理首頁。
        /// </summary>
        /// <returns>首頁畫面。</returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 依條件查詢貨件回倉處理資料。
        /// </summary>
        /// <param name="searchRequest">查詢條件與分頁資訊。</param>
        /// <returns>查詢結果與總筆數。</returns>
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

        /// <summary>
        /// 匯出貨件回倉處理查詢結果為 Excel。
        /// </summary>
        /// <param name="searchRequest">查詢條件。</param>
        /// <returns>Excel 檔案或錯誤訊息。</returns>
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

        /// <summary>
        /// 取得處理方式清單。
        /// </summary>
        /// <returns>處理方式選單資料。</returns>
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

        /// <summary>
        /// 取得重出派件公司清單。
        /// </summary>
        /// <returns>派件公司選單資料。</returns>
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

        /// <summary>
        /// 取得運費支付方清單。
        /// </summary>
        /// <returns>運費支付方選單資料。</returns>
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

        /// <summary>
        /// 更新指定貨件的處理方式並廣播最新資料。
        /// </summary>
        /// <param name="request">更新內容。</param>
        /// <returns>更新結果與最新單筆列表資料。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult UpdateProcessType(ShipmentInboundProcessUpdateRequest request)
        {
            try
            {
                _shipmentInboundProcessService.UpdateProcessType(request);
                var row = _shipmentInboundProcessService.GetRowById(request.Id);
                MainHubNotifier.BroadcastRowUpdated(row);

                return Json(new ResponseModel
                {
                    ReturnObject = row
                });
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
        /// 開始編輯指定貨件並建立處理鎖定。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <returns>建立鎖定後的最新單筆列表資料。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult BeginProcessEdit(int id)
        {
            try
            {
                var result = _shipmentInboundProcessService.BeginProcessEdit(id);
                MainHubNotifier.BroadcastRowUpdated(result);
                return Json(new ResponseModel
                {
                    ReturnObject = result
                });
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
        /// 釋放指定貨件的處理鎖定。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <returns>釋放後的最新單筆列表資料。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult ReleaseProcessEdit(int id)
        {
            try
            {
                var result = _shipmentInboundProcessService.ReleaseProcessEdit(id);
                MainHubNotifier.BroadcastRowUpdated(result);
                return Json(new ResponseModel
                {
                    ReturnObject = result
                });
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
        /// 取得指定貨件的處理明細。
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <returns>單筆處理明細。</returns>
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
        /// <returns>貨物來源選單資料。</returns>
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
        /// <param name="file">上傳的 Excel 檔案。</param>
        /// <returns>批次處理結果。</returns>
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
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <param name="returnReason">新的退件原因。</param>
        /// <returns>更新結果。</returns>
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
        /// 更新備註
        /// </summary>
        /// <param name="id">貨件回倉資料 Id。</param>
        /// <param name="remark">新的備註。</param>
        /// <returns>更新結果與最新單筆列表資料。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult UpdateRemark(int id, string remark)
        {
            try
            {
                _shipmentInboundProcessService.UpdateRemark(id, remark);
                var row = _shipmentInboundProcessService.GetRowById(id);
                MainHubNotifier.BroadcastRowUpdated(row);

                return Json(new ResponseModel
                {
                    ReturnObject = row
                });
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
        /// <param name="file">上傳的 Excel 檔案。</param>
        /// <returns>批次處理結果。</returns>
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
