using Service.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{

    public class DropDownListController : Controller
    {
        private readonly DropDownListService _dropDownListService;

        public DropDownListController(DropDownListService dropDownListService)
        {
            _dropDownListService = dropDownListService;
        }

        /// <summary>
        /// 海運客戶
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSeaCustomerList()
        {
           var result = _dropDownListService.GetSeaCustomerList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        ///  取得海運倉別
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSeaWarehouseTypeList()
        {
            var result = _dropDownListService.GetSeaWarehouseTypeList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        ///  取得報關方式
        /// </summary>
        /// <returns></returns>
        public ActionResult GetPostEntryTypeList()
        {
            var result = _dropDownListService.GetPostEntryTypeList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        
    }
}