using JETFTAX.Models.BatchUploadProcess;
using Service.EnumTax;
using Service.Models;
using Service.Services.BatchUploadProcess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class BatchUploadProcessController : Controller
    {
        private readonly BatchUploadProcessService _batchUploadProcessService;

        public BatchUploadProcessController(BatchUploadProcessService batchUploadProcessService)
        {
            _batchUploadProcessService = batchUploadProcessService;
        }

        [UserAuthorize(Authority.BatchUploadProcess)]
        public ActionResult Index()
        {
            var vm = new BatchUploadProcessViewModel();

            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Text = "處置說明", Value = "1" });
            list.Add(new SelectListItem() { Text = "已結案", Value = "2" });
            vm.ProcessTypeList = list;

            return View(vm);
        }

        /// <summary>
        /// 處置說明批次上傳-檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchUploadProcess)]
        public JsonResult Upload(BatchUploadProcessViewModel vm, HttpPostedFileBase file)
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
                            //寫入資料
                            resopnseModel = _batchUploadProcessService.BatchUploadProcess(vm.Status,filePath, fileName, Session["user_id"].ToString());
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