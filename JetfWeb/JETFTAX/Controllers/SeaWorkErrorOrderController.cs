using JETFTAX.Models.SeaWorkErrorOrder;
using JETFTAX.Models.WorkLoad;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.SeaWorkErrorOrder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaWorkErrorOrderController : Controller
    {
        private readonly SeaWorkErrorOrderService _seaWorkErrorOrderService;

        public SeaWorkErrorOrderController(SeaWorkErrorOrderService seaWorkErrorOrderService)
        {
            _seaWorkErrorOrderService = seaWorkErrorOrderService;
        }

        // GET: SeaWorkErrorOrder
        [UserAuthorize(Authority.SeaWorkErrorOrder)]
        public ActionResult Index()
        {
            SeaWorkErrorOrderViewModel vm = new SeaWorkErrorOrderViewModel();
            vm.DataDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        [UserAuthorize(Authority.SeaWorkErrorOrder)]
        public ActionResult Upload(HttpPostedFileBase file, SeaWorkErrorOrderViewModel vm) 
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                string fileType, fileName, filePath;
                if (file != null)
                {
                    if (file.ContentLength > 0)
                    {
                        fileType = Path.GetExtension(file.FileName);
                        if (fileType != ".xlsx")
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "副檔名需為xlsx";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);
                            resopnseModel = _seaWorkErrorOrderService.Upload(filePath, vm.DataDate, Session["user_id"].ToString());
                        }
                    }
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "未選擇檔案";
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }
    }
}