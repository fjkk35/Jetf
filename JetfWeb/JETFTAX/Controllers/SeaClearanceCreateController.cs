using JETFTAX.Models.PostClearance;
using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearanceCreate;
using Service.Services;
using Service.Services.ErrorOrderSend;
using Service.Services.SeaClearance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaClearanceCreateController : Controller
    {
        private readonly SeaClearanceCreateService _seaClearanceCreateService;

        private readonly SeaClearanceService _seaClearanceService;

        

        public SeaClearanceCreateController(SeaClearanceCreateService seaClearanceCreateService, SeaClearanceService seaClearanceService)
        {
            _seaClearanceCreateService = seaClearanceCreateService;
            _seaClearanceService = seaClearanceService;
        }


        // GET: SeaClearanceCreate
        [UserAuthorize(Authority.SeaClearanceCreate)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaClearanceCreate)]
        public JsonResult SearchData(SeaClearanceCreateRequest request)
        {
            try
            {
                var result = _seaClearanceCreateService.GetSeaClearance(request);

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
        /// 上傳後段報關建檔
        /// </summary>
        /// <param name="file"></param>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearanceCreate)]
        public JsonResult UploadFile(HttpPostedFileBase file, string dataDate)
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
                            resopnseModel = _seaClearanceCreateService.UploadFile(filePath, dataDate, Session["user_id"].ToString());
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

        /// <summary>
        /// 取得上傳結果
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SeaClearanceCreate)]
        public ActionResult GetUploadResult(int id)
        { 
            var result = _seaClearanceCreateService.GetUploadResult(id);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 下載
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SeaClearanceCreate)]
        public ActionResult SeaClearanceDetailExcel(int id)
        {
            var workbook = _seaClearanceService.SeaClearanceForIdExcel(id);

            string handle = Guid.NewGuid().ToString();
            string fileName = $"{id}海快後段報關建檔_明細.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = "" }
            };
        }
    }
}
