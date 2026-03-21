using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Models.EtlUnpackingZ;
using Service.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class EtlUnpackingZController : Controller
    {
        EtlUnpackingZService etlUnpackingZService = new EtlUnpackingZService();

        public ActionResult Upload()
        {
            return View();
        }

        /// <summary>
        /// 上傳
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.UploadEtlMergeBagNo)]
        public JsonResult Upload(HttpPostedFileBase[] files)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                string fileName, filePath;
                if (files != null)
                {
                    if (files.Length > 0)
                    {
                        //查询副檔名是否都是 Csv
                        bool isCsv = files.All(file => IsCsv(file.FileName));

                        if (!isCsv)
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "副檔名需為csv";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            List<string> filePaths = new List<string>();
                            foreach (var file in files)
                            {
                                fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                                filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                                file.SaveAs(filePath);

                                filePaths.Add(filePath);
                            }
                            resopnseModel = etlUnpackingZService.Upload(filePaths, Session["user_id"].ToString());
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


        [HttpPost]
        [UserAuthorize(Authority.UploadEtlMergeBagNo)]
        public JsonResult Download(string data)
        {
            string handle = Guid.NewGuid().ToString();
            string fileName = $"{DateTime.Now.Date.ToString("yyyyMMdd")}空運進口貨物新艙單資料查詢.xlsx";
            string msg = "";
            try
            {
                List<string> list = data.Split('\n')
                                       .Select(line => line.Trim())
                                       .Where(line => !string.IsNullOrWhiteSpace(line))
                                       .ToList();

                IWorkbook workbook = etlUnpackingZService.Download(list);

                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };
        }


        //判断副檔名是否Csv
        static bool IsCsv(string fileName)
        {
            return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        }
    }
}