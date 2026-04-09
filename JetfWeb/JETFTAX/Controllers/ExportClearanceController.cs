using Service.EnumTax;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ExportClearanceController : Controller
    {

        ExportClearanceService exportClearanceService = new ExportClearanceService();

        /// <summary>
        /// 上傳空運出口航班及出入倉時間
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1","3")]
        [UserAuthorize(Authority.UploadEtlExportFlight)]
        public ActionResult UploadExportFlight()
        {
            return View();
        }

        /// <summary>
        ///  上傳空運出口航班及出入倉時間
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.UploadEtlExportFlight)]
        public JsonResult UploadExportFlight(HttpPostedFileBase file)
        {
            ResponseModel resopnseModel = new ResponseModel();
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
                            resopnseModel = exportClearanceService.UploadExportFlight(filePath, Session["user_id"].ToString());
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