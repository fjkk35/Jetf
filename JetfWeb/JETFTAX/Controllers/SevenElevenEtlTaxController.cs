using JETFTAX.Models.CainiaoFamilyTax;
using JETFTAX.Models.SevenElevenEtlTax;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Models.CptTradeVan;
using Service.Services.CainiaoFamilyTax;
using Service.Services.CainiaoHiLifeTax;
using Service.Services.SevenElevenEtlTaxTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SevenElevenEtlTaxController : Controller
    {
        private readonly SevenElevenEtlTaxService _sevenElevenEtlTaxTaxService;

        public SevenElevenEtlTaxController(SevenElevenEtlTaxService sevenElevenEtlTaxTaxService)
        {
            _sevenElevenEtlTaxTaxService = sevenElevenEtlTaxTaxService;
        }

        [UserAuthorize(Authority.SevenElevenEtlTax)]
        public ActionResult Index()
        {
            SevenElevenEtlTaxViewModel vm = new SevenElevenEtlTaxViewModel();
            List<SelectListItem> dateTimeList = new List<SelectListItem>();
            dateTimeList.Add(new SelectListItem() { Text = "前一天22:00-當日08:00", Value = "1" });
            dateTimeList.Add(new SelectListItem() { Text = "當日08:00-當日16:00", Value = "2" });
            dateTimeList.Add(new SelectListItem() { Text = "當日21:00-當日22:00", Value = "3" });
            vm.DateTimeList = dateTimeList;

            vm.StartDate = $"{DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")} 22:00:00";
            vm.EndDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 08:00:00";

            vm.CustomerList = EnumHelper.ToSelectList<EtlSevenElevenTax>();

            return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.SevenElevenEtlTax)]
        public ActionResult Download(SevenElevenEtlTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            string fileName;

            switch (vm.Customer)
            {
                case EtlSevenElevenTax.Sagawa:
                    fileName = $"{vm.Customer.ToDescription()}{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
                    break;
                case EtlSevenElevenTax.Cainiao:
                    fileName = $"{vm.Customer.ToDescription()}{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
                    break;
                default:
                    fileName = $"{vm.Customer.ToDescription()}{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
                    break;
            }

            var msg = "";
            try
            {
                var bytes = _sevenElevenEtlTaxTaxService.GetSevenElevenTax(vm.StartDate, vm.EndDate, vm.Customer);

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
        [UserAuthorize(Authority.SevenElevenEtlTax)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
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
                        resopnseModel = _sevenElevenEtlTaxTaxService.Upload(filePath, Session["user_id"].ToString());
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