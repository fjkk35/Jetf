using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class ShenzhenController : Controller
    {
        ShenzhenService shenzhenService = new ShenzhenService();

        // GET: Shenzhen
        public ActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Upload(HttpPostedFileBase file)
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
                            resopnseModel = shenzhenService.Upload(filePath, Session["user_id"].ToString());
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