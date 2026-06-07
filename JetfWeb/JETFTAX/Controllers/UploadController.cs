using JETFTAX.Models;
using Service.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Service.Services;
using static JETFTAX.Controllers.AccountController;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using Service.EnumTax;

namespace JETFTAX.Controllers
{
    public class UploadController : Controller
    {
        private readonly UploadService _uploadService;
        private readonly DropDownListService _dropDownListService;

        public UploadController(DropDownListService dropDownListService, UploadService uploadService) 
        {
            _dropDownListService = dropDownListService;
            _uploadService = uploadService;
        }

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle, date2Style;

        #region 海運稅金驗證配置

        /// <summary>
        /// 海運稅金類型驗證規則
        /// </summary>
        private class SeaTaxValidationRule
        {
            public string DisplayName { get; set; }
            public string[] FileNameKeywords { get; set; }

            public SeaTaxValidationRule(string displayName, params string[] keywords)
            {
                DisplayName = displayName;
                FileNameKeywords = keywords;
            }
        }

        /// <summary>
        /// 取得稅金類型驗證規則
        /// </summary>
        private Dictionary<SeaTaxType, SeaTaxValidationRule> SeaTaxValidationRules = new Dictionary<SeaTaxType, SeaTaxValidationRule>
        {
            { SeaTaxType.TPCT, new SeaTaxValidationRule("台北貨櫃", "tpct", "TPCT") },
            { SeaTaxType.TIPC, new SeaTaxValidationRule("台灣港務", "港務") },
            { SeaTaxType.IPOST, new SeaTaxValidationRule("高雄郵聯", "高雄") },
            { SeaTaxType.CHWN, new SeaTaxValidationRule("高雄郵聯(全旺)", "全旺") },
            { SeaTaxType.JFKH, new SeaTaxValidationRule("高雄郵聯(捷豐)", "捷豐") },
            { SeaTaxType.WAHA, new SeaTaxValidationRule("萬海","萬海") },
            { SeaTaxType.UNIJ, new SeaTaxValidationRule("連捷","連捷") },
            { SeaTaxType.JFKL, new SeaTaxValidationRule("基隆港務(捷豐)", "基隆港") }
        };

        /// <summary>
        /// 驗證檔案格式和檔名
        /// </summary>
        private ResponseModel ValidateSeaTaxFile(string fileName, string fileType, SeaTaxType taxType, string date)
        {
            var response = new ResponseModel();

            // 驗證副檔名
            if (fileType != ".xlsx")
            {
                response.status = Status.error;
                response.msg = $"海運-[{SeaTaxValidationRules[taxType].DisplayName}]副檔名需為xlsx";
                return response;
            }

            // 驗證檔名關鍵字
            var rule = SeaTaxValidationRules[taxType];
            bool containsKeyword = rule.FileNameKeywords.Any(keyword => fileName.IndexOf(keyword) >= 0);
            
            if (!containsKeyword)
            {
                string keywords = string.Join("或", rule.FileNameKeywords);
                response.status = Status.error;
                response.msg = $"海運-[{rule.DisplayName}]檔名不包含{keywords}，請確認";
                return response;
            }

            // 驗證日期
            if (fileName.IndexOf(date.Substring(4, 4)) < 0)
            {
                response.status = Status.error;
                response.msg = "海運-檔名日期需和上傳日期相同";
                return response;
            }

            return response;
        }

        #endregion


        /// <summary>
        /// 1-1.海運稅金資料上傳
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", Authority.UploadSeaTax)]
        //[UserAuthorize(Authority.UploadSeaTax)]
        //public ActionResult Seatax()
        //{
        //    SeataxViewModel vm = new SeataxViewModel();
        //    vm.ddlTaxTypeList = _dropDownListService.GetSeaTaxTypeList();
        //    vm.date = DateTime.Now.ToString("yyyy-MM-dd");
        //    return View(vm);
        //}

        /// <summary>
        /// 1-1.海運稅金資料上傳-檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadSeaTax)]
        public JsonResult UploadFile(HttpPostedFileBase file, SeataxViewModel vm)
        {
            ResponseModel resopnseModel = new ResponseModel();
            try
            {
                string fileType, fileName, filePath, date;
                date = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
                if (file != null)
                {
                    if (file.ContentLength > 0)
                    {
                        fileType = Path.GetExtension(file.FileName);
                        
                        //驗證
                        resopnseModel = ValidateSeaTaxFile(file.FileName, fileType, vm.taxType, date);

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);

                            resopnseModel = _uploadService.UploadFile(date, filePath, vm.taxType, UserContextService.GetUserId());
                            //記錄LOG
                            //InsertLog_Rec($"海運稅金{vm.taxType}", fileName);
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
        /// 2-1.G類資料上傳
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadSeaTax)]
        public ActionResult SeataxG()
        {
            SeataxViewModel vm = new SeataxViewModel();
            //List<SelectListItem> taxTypeList = new List<SelectListItem>();
            //taxTypeList.Add(new SelectListItem() { Text = "G類TPCT-台北貨櫃", Value = "TPCT" });
            //taxTypeList.Add(new SelectListItem() { Text = "G類TIPC-台灣港務", Value = "TIPC" });
            //taxTypeList.Add(new SelectListItem() { Text = "G類IPOST-高雄郵聯", Value = "IPOST" });
            //vm.ddlTaxTypeList = taxTypeList;
            vm.date = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 2-1.G類資料上傳-檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadSeaTaxG)]
        public JsonResult UploadFileG(HttpPostedFileBase file, SeataxViewModel vm)
        {
            ResponseModel resopnseModel = new ResponseModel();
            try
            {
                string fileType, fileName, filePath, date;
                date = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
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

                            resopnseModel = _uploadService.UploadFileG(date, filePath, UserContextService.GetUserId());
                            //記錄LOG
                            //InsertLog_Rec($"海運G類{vm.taxType}", fileName);
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
        /// 4-1.物流代收金額上傳
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadCollectibleAmount)]
        public ActionResult Receive()
        {
            return View();
        }

        /// <summary>
        /// 4-1.物流代收金額上傳-檔案
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadCollectibleAmount)]
        public JsonResult UploadFileReceive(HttpPostedFileBase file)
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
                            resopnseModel.msg = "[代收檔]副檔名需為xlsx";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);

                            resopnseModel = _uploadService.UploadFileReceive(filePath, UserContextService.GetUserId());
                            //記錄LOG
                            //InsertLog_Rec($"海運稅金{vm.taxType}", fileName);
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
        /// 5-1.物流代收匯款上傳
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadCollectibleRemittance)]
        public ActionResult Transfer()
        {
            return View();
        }

        /// <summary>
        /// 5-1.物流代收匯款上傳-檔案
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadCollectibleRemittance)]
        public JsonResult UploadFileTransfer(HttpPostedFileBase file)
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
                            resopnseModel.msg = "[匯款檔]副檔名需為xlsx";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);

                            resopnseModel = _uploadService.UploadFileTransfer(filePath, UserContextService.GetUserId());
                            //記錄LOG
                            //InsertLog_Rec($"海運稅金{vm.taxType}", fileName);
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
        /// 3-7.菜鳥包稅稅金方式修改上傳
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadCainiaoModifyTax)]
        public ActionResult CainiaoTaxEdit()
        {
            CainiaoTaxEditViewModel vm = new CainiaoTaxEditViewModel();
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "海運", Value = "Sea" });
            sourceList.Add(new SelectListItem() { Text = "空運", Value = "Etl" });

            List<SelectListItem> columnList = new List<SelectListItem>();
            columnList.Add(new SelectListItem() { Text = "分提單號", Value = "TrackingNo" });
            columnList.Add(new SelectListItem() { Text = "物流貨號", Value = "JetfSerial" });

            vm.ddlSourceList = sourceList;
            vm.ddlColumnList = columnList;
            return View(vm);
        }

        /// <summary>
        /// 3-7.菜鳥包稅稅金方式修改上傳
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.UploadCainiaoModifyTax)]
        [HttpPost]
        public ActionResult CainiaoTaxEdit(HttpPostedFileBase file, CainiaoTaxEditViewModel vm)
        {
            ResponseModel resopnseModel = new ResponseModel();
            try
            {
                string fileType, fileName, filePath,source, column;
                source = vm.Source;
                column = vm.Column;

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

                        if (source == "Etl" && column == "JetfSerial")
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "空運無上傳物流貨號欄位";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);
                            //寫入資料
                            resopnseModel = _uploadService.CainiaoTaxEdit(filePath, fileName, source, column, UserContextService.GetUserId());
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
        /// 3-7.菜鳥包稅稅金方式修改上傳Excel
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.UploadCainiaoModifyTax)]
        public ActionResult CainiaoTaxEditExcel(string source,string column,string upload_time, string upload_ope)
        {
            string fileName = "";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetCainiaoTaxEditWorkbook(source, column, upload_time, upload_ope);
                fileName = $"菜鳥包稅稅金方式修改_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
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

        /// <summary>
        /// 3-7.菜鳥包稅稅金方式修改上傳Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        IWorkbook GetCainiaoTaxEditWorkbook(string source, string column, string upload_time, string upload_ope)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得菜鳥包稅稅金方式修改資料
            DataTable dt_Report = _uploadService.GetCainiaoTaxEdit(source, column, upload_time, upload_ope).dt;
            //產生EXCEL
            GetCainiaoTaxEditSheet(workbook, dt_Report, "菜鳥包稅稅金方式修改");
            return workbook;
        }

        /// <summary>
        /// 3-7.菜鳥包稅稅金方式修改上傳Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetCainiaoTaxEditSheet(IWorkbook workbook, DataTable dt_Report, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("分提單號");
            row.CreateCell(1).SetCellValue("稅金支付方式");
            row.CreateCell(2).SetCellValue("派件公司");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
          
            for (int i = 0; i < dt_Report.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt_Report.Rows[i]["TrackingNo"].ToString());
                row.CreateCell(1).SetCellValue(dt_Report.Rows[i]["TAX_PAYMENT"].ToString());
                row.CreateCell(2).SetCellValue(dt_Report.Rows[i]["TRANS_TAXPAYMENT"].ToString());
            }
        }

        /// <summary>
        /// Excel Style
        /// </summary>
        /// <param name="workbook"></param>
        void GetWorkbookStyle(IWorkbook workbook)
        {
            //藍色的Style
            fontB = workbook.CreateFont();
            fontB.Color = NPOI.SS.UserModel.IndexedColors.Blue.Index;

            font1 = (XSSFFont)workbook.CreateFont();
            //標題
            cs_Title = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //標題
            cs_Title_Left = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title_Left.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Title_Left.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Title.BorderTop = BorderStyle.Thin;
            //cs_Title.BorderBottom = BorderStyle.Thin;
            //cs_Title.BorderLeft = BorderStyle.Thin;
            //cs_Title.BorderRight = BorderStyle.Thin;
            //cs_Title.SetFont(font1);

            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center.BorderTop = BorderStyle.Thin;
            //cs_Center.BorderBottom = BorderStyle.Thin;
            //cs_Center.BorderLeft = BorderStyle.Thin;
            //cs_Center.BorderRight = BorderStyle.Thin;

            cs_Center_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center_Blue.BorderTop = BorderStyle.Thin;
            //cs_Center_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Center_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Center_Blue.BorderRight = BorderStyle.Thin;
            cs_Center_Blue.SetFont(fontB);

            format = (XSSFDataFormat)workbook.CreateDataFormat();
            cs_Int = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int.BorderTop = BorderStyle.Thin;
            //cs_Int.BorderBottom = BorderStyle.Thin;
            //cs_Int.BorderLeft = BorderStyle.Thin;
            //cs_Int.BorderRight = BorderStyle.Thin;
            cs_Int.DataFormat = format.GetFormat("#,##0");

            cs_Int_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int_Blue.BorderTop = BorderStyle.Thin;
            //cs_Int_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Int_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Int_Blue.BorderRight = BorderStyle.Thin;
            cs_Int_Blue.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue.SetFont(fontB);

            cs_Double = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Double.BorderTop = BorderStyle.Thin;
            //cs_Double.BorderBottom = BorderStyle.Thin;
            //cs_Double.BorderLeft = BorderStyle.Thin;
            //cs_Double.BorderRight = BorderStyle.Thin;
            cs_Double.DataFormat = format.GetFormat("#,##0.000");

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0.000%");
            cs_Percent2.SetFont(font1);


            dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            dateStyle.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm:ss");

            date2Style = (XSSFCellStyle)workbook.CreateCellStyle();
            date2Style.DataFormat = format.GetFormat("yyyy/mm/dd");

        }

        /// <summary>
        ///5-4.回倉重出貨明細表上傳
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]

        //[LoginFilter]
        //public ActionResult AgainCargo()
        //{
        //    return View();
        //}

        ///// <summary>
        ///// 5-4.回倉重出貨明細表上傳
        ///// </summary>
        ///// <param name="file"></param>
        ///// <param name="vm"></param>
        ///// <returns></returns>
        ////[UserAuthorize("1", "2")]
        //[LoginFilter]
        //public JsonResult AgainCargo(HttpPostedFileBase file)
        //{
        //    ResopnseModel resopnseModel = new ResopnseModel();
        //    try
        //    {
        //        string fileType, fileName, filePath;
        //        if (file != null)
        //        {
        //            if (file.ContentLength > 0)
        //            {
        //                fileType = Path.GetExtension(file.FileName);
        //                if (fileType != ".xlsx")
        //                {
        //                    resopnseModel.status = Status.error;
        //                    resopnseModel.msg = "[匯款檔]副檔名需為xlsx";
        //                }

        //                if (resopnseModel.status != Status.error)
        //                {
        //                    fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
        //                    filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
        //                    file.SaveAs(filePath);

        //                    resopnseModel = _uploadService.UploadFileAgainCargo(filePath, Session["user_id"].ToString());
        //                }
        //            }
        //        }
        //        else
        //        {
        //            resopnseModel.status = Status.error;
        //            resopnseModel.msg = "未選擇檔案";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        resopnseModel.status = Status.error;
        //        resopnseModel.msg = ex.Message;
        //    }

        //    return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        //}





    }
}
