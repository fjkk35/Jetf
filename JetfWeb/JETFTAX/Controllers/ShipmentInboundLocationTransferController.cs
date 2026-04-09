using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Service.Models;
using Service.Services.ShipmentInboundLocationTransfer;
using Service.Services.ShipmentInboundLocationTransfer.Domain;
using Service.EnumTax;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentInboundLocationTransferController : Controller
    {
        private readonly ShipmentInboundLocationTransferService _service;

        public ShipmentInboundLocationTransferController(ShipmentInboundLocationTransferService service)
        {
            _service = service;
        }

        // GET: ShipmentInboundLocationTransfer
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢儲位資料
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundLocationTransfer)]
        public JsonResult SearchData(LocationTransferRequest request)
        {
            try
            {
                var result = _service.GetData(request);

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
        /// 更新儲位
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundLocationTransfer)]
        public JsonResult UpdateLocation(LocationTransferUpdateRequest request)
        {
            try
            {
                _service.UpdateLocation(request);

                return Json(new ResponseModel
                {
                    status = "success",
                    msg = "儲位更新成功"
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
    }
}