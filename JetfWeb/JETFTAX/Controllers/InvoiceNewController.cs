using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Service.Services.InvoiceNew;
using Service.Models;
using System.IO;
using NPOI.SS.UserModel;
using Service.EnumTax;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class InvoiceNewController : Controller
    {
        private readonly InvoiceNewService _invoiceNewService;

        public InvoiceNewController(InvoiceNewService invoiceNewService)
        {
            _invoiceNewService = invoiceNewService;
        }

        /// <summary>
        /// 開立電子發票作業New
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.InvoiceProcessing)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 開立電子發票作業New上傳並匯出
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.InvoiceProcessing)]
        public JsonResult InvoiceWorkNew(HttpPostedFileBase file)
        {
            ResponseModel resopnseModel = new ResponseModel();
            string handle = "";
            string fileName = "";
            
            try
            {
                string fileType, uploadFileName, filePath;
                if (file != null)
                {
                    if (file.ContentLength > 0)
                    {
                        fileType = Path.GetExtension(file.FileName);
                        if (fileType != ".xlsx")
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "副檔名需為xlsx";
                            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
                        }

                        uploadFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                        filePath = Path.Combine(Server.MapPath("~/UploadFIle"), uploadFileName);
                        file.SaveAs(filePath);
                        
                        // 讀取檔案並直接產生 Excel
                        resopnseModel = _invoiceNewService.InvoiceWorkNew(filePath, uploadFileName, Session["user_id"].ToString());

                        if (resopnseModel.status == Status.success)
                        {
                            // 從 Service 取得已產生的 Workbook
                            IWorkbook workbook = resopnseModel.ReturnObject as IWorkbook;
                            
                            if (workbook != null)
                            {
                                fileName = $"{DateTime.Now.ToString("yyyyMMdd")}開立電子發票作業New_{DateTime.Now.ToString("HHmmss")}.xlsx";
                                handle = Guid.NewGuid().ToString();

                                using (MemoryStream fileStream = new MemoryStream())
                                {
                                    workbook.Write(fileStream);
                                    TempData[handle] = fileStream.ToArray();
                                }

                                resopnseModel.msg = "處理成功";
                                resopnseModel.ReturnObject = new { fileGuid = handle, fileName = fileName };
                            }
                            else
                            {
                                resopnseModel.status = Status.error;
                                resopnseModel.msg = "產生檔案失敗";
                            }
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