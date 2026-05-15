using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.SeaTaxUpload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaTaxUploadController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly SeaTaxUploadService _seaTaxUploadService = new SeaTaxUploadService();

        public SeaTaxUploadController(DropDownListService dropDownListService)
        {
            _dropDownListService = dropDownListService;
        }

        /// <summary>
        /// 海運稅金類型驗證規則。
        /// </summary>
        private sealed class SeaTaxValidationRule
        {
            public SeaTaxValidationRule(string displayName, params string[] fileNameKeywords)
            {
                DisplayName = displayName;
                FileNameKeywords = fileNameKeywords ?? Array.Empty<string>();
            }

            public string DisplayName { get; }

            public string[] FileNameKeywords { get; }
        }

        private readonly Dictionary<SeaTaxType, SeaTaxValidationRule> _seaTaxValidationRules =
            new Dictionary<SeaTaxType, SeaTaxValidationRule>
            {
                { SeaTaxType.TPCT, new SeaTaxValidationRule("台北貨櫃", "tpct", "TPCT") },
                { SeaTaxType.TIPC, new SeaTaxValidationRule("台灣港務", "港務") },
                { SeaTaxType.IPOST, new SeaTaxValidationRule("高雄郵聯", "高雄") },
                { SeaTaxType.CHWN, new SeaTaxValidationRule("高雄郵聯(全旺)", "全旺") },
                { SeaTaxType.JFKH, new SeaTaxValidationRule("高雄郵聯(捷豐)", "捷豐") },
                { SeaTaxType.WAHA, new SeaTaxValidationRule("萬海", "萬海") },
                { SeaTaxType.UNIJ, new SeaTaxValidationRule("連捷", "連捷") },
                { SeaTaxType.JFKL, new SeaTaxValidationRule("基隆港務(捷豐)", "基隆港") }
            };

        /// <summary>
        /// 海運稅金資料上傳(新)。
        /// </summary>
        [UserAuthorize(Authority.UploadSeaTax)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [UserAuthorize(Authority.UploadSeaTax)]
        public JsonResult GetSeaTaxTypeList()
        {
            var list = _dropDownListService.GetSeaTaxTypeList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 海運稅金資料上傳(新)-檔案。
        /// </summary>
        [UserAuthorize(Authority.UploadSeaTax)]
        public JsonResult UploadFile(HttpPostedFileBase file, string date, SeaTaxType? taxType)
        {
            var responseModel = new ResponseModel();

            try
            {
                if (string.IsNullOrWhiteSpace(date))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "請選擇日期";
                    return Json(responseModel, JsonRequestBehavior.AllowGet);
                }

                if (!taxType.HasValue)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "請選擇稅金類型";
                    return Json(responseModel, JsonRequestBehavior.AllowGet);
                }

                var uploadDate = Convert.ToDateTime(date).ToString("yyyyMMdd");
                if (file == null)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "未選擇檔案";
                    return Json(responseModel, JsonRequestBehavior.AllowGet);
                }

                if (file.ContentLength <= 0)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "未選擇檔案";
                    return Json(responseModel, JsonRequestBehavior.AllowGet);
                }

                var fileType = Path.GetExtension(file.FileName);
                responseModel = ValidateSeaTaxFile(file.FileName, fileType, taxType.Value, uploadDate);
                if (responseModel.status == Status.error)
                {
                    return Json(responseModel, JsonRequestBehavior.AllowGet);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                responseModel = _seaTaxUploadService.UploadFile(uploadDate, filePath, taxType.Value, Session["user_id"].ToString());
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 驗證海運稅金上傳檔案格式。
        /// </summary>
        private ResponseModel ValidateSeaTaxFile(string fileName, string fileType, SeaTaxType taxType, string date)
        {
            var response = new ResponseModel();

            if (fileType != ".xlsx")
            {
                response.status = Status.error;
                response.msg = $"海運-[{_seaTaxValidationRules[taxType].DisplayName}]副檔名需為xlsx";
                return response;
            }

            var rule = _seaTaxValidationRules[taxType];
            var containsKeyword = rule.FileNameKeywords.Any(keyword => fileName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!containsKeyword)
            {
                response.status = Status.error;
                response.msg = $"海運-[{rule.DisplayName}]檔名不包含{string.Join("或", rule.FileNameKeywords)}，請確認";
                return response;
            }

            if (fileName.IndexOf(date.Substring(4, 4), StringComparison.OrdinalIgnoreCase) < 0)
            {
                response.status = Status.error;
                response.msg = "海運-檔名日期需和上傳日期相同";
                return response;
            }

            return response;
        }
    }
}
