using JETFTAX.Models.CainiaoHiLifeTax;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.CainiaoHiLifeTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CainiaoHiLifeTaxController : Controller
    {
        private readonly CainiaoHiLifeTaxService _cainiaoHiLifeTaxService;

        public CainiaoHiLifeTaxController(CainiaoHiLifeTaxService cainiaoHiLifeTaxService)
        {
            _cainiaoHiLifeTaxService = cainiaoHiLifeTaxService;
        }

        [UserAuthorize(Authority.CainiaoHiLifeTax)]
        public ActionResult Index()
        {
            CainiaoHiLifeTaxViewModel vm = new CainiaoHiLifeTaxViewModel();
            List<SelectListItem> dateTimeList = new List<SelectListItem>();
            dateTimeList.Add(new SelectListItem() { Text = "前一天22:00-當日08:00", Value = "1" });
            dateTimeList.Add(new SelectListItem() { Text = "當日08:00-當日16:00", Value = "2" });
            dateTimeList.Add(new SelectListItem() { Text = "當日21:00-當日22:00", Value = "3" });
            vm.DateTimeList = dateTimeList;

            vm.StartDate = $"{DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")} 22:00:00";
            vm.EndDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 08:00:00";

            return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.CainiaoHiLifeTax)]
        public ActionResult Download(CainiaoHiLifeTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"菜鳥空快萊爾富稅金{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
            var msg = "";
            try
            {
                byte[] bytes = _cainiaoHiLifeTaxService.GetCainiaoHiLifeTax(vm.StartDate, vm.EndDate);

                using (MemoryStream fileStream = new MemoryStream())
                {
                    TempData[handle] = bytes.ToArray();
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

        [HttpPost]
        [UserAuthorize(Authority.CainiaoHiLifeTax)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            ResponseModel resopnseModel = new ResponseModel();
            try
            {
                string fileType, fileName, filePath;
                if (file != null)
                {
                    fileType = Path.GetExtension(file.FileName);
                    if (fileType != ".txt")
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = "副檔名需為txt";
                    }

                    if (resopnseModel.status != Status.error)
                    {
                        fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                        filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                        file.SaveAs(filePath);
                        resopnseModel = _cainiaoHiLifeTaxService.Upload(filePath, UserContextService.GetUserId());
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