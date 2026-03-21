using JETFTAX.Models.BatchEditOrder;
using JETFTAX.Models.WorkLoad;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.BatchEditOrder;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class BatchEditOrderController : Controller
    {
        private readonly BatchEditOrderService _batchEditOrderService;

        CargoService cargoService = new CargoService();

        public BatchEditOrderController(BatchEditOrderService batchEditOrderService) 
        {
            _batchEditOrderService = batchEditOrderService;
        }

        // GET: BatchEditOrder
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 批量製單申報資料查詢
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.BatchSearchEditOrder)]
        public ActionResult Search()
        {
            BatchEditOrderSearchViewModel vm = new BatchEditOrderSearchViewModel();
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "海運", Value = "SEA" });
            sourceList.Add(new SelectListItem() { Text = "空運", Value = "ETL" });
            vm.ddlSourceList = sourceList;

            return View(vm);
        }

        /// <summary>
        /// 批量製單申報資料查詢-上傳檔案
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.BatchSearchEditOrder)]
        [HttpPost]
        public JsonResult Search(string source, HttpPostedFileBase file)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                if (file != null)
                {
                    if (file.ContentLength > 0)
                    {
                       var fileType = Path.GetExtension(file.FileName);
                        if (fileType != ".xlsx")
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "副檔名需為xlsx";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            string msg = "";
                            string handle = Guid.NewGuid().ToString();
                            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);
                            //Excel
                            var workbook = _batchEditOrderService.Search(source, filePath);
                            fileName = $"{DateTime.Now.ToString("yyyyMMdd")}批量製單申報資料查詢.xlsx";
                            using (MemoryStream fileStream = new MemoryStream())
                            {
                                workbook.Write(fileStream);
                                TempData[handle] = fileStream.ToArray();
                            }
                            return new JsonResult()
                            {
                                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
                            };
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