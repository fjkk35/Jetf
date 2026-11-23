using FluentFTP;
using JETFTAX.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CargoController : Controller
    {
        private readonly GlobalService _globalService;
        private readonly CargoService _cargoService;
        private readonly CustomerService _customerService;

        public CargoController(GlobalService globalService, CargoService cargoService, CustomerService customerService)
        {
            _globalService = globalService;
            _cargoService = cargoService;
            _customerService = customerService;
        }

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle, date2Style;

        /// <summary>
        /// 貨況查詢-下載處置說明檔案
        /// </summary>
        /// <param name="year"></param>
        /// <param name="prjId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DownloadFile(string filePath, string fileName)
        {
            //try
            //{
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/octet-stream", fileName); //MME 格式 可上網查 此為通用設定
                                                                       //}
                                                                       //catch (System.Exception)
                                                                       //{
                                                                       //return Content("<script>alert('查無此檔案');window.close()</script>");
                                                                       //}
        }

        /// <summary>
        /// 貨況查詢-取得稅金編號Pdf
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult GetTaxNumberPdf(string taxNumber)
        {
            try
            {
                //紀錄LOG
                _cargoService.InsertLog_Cargo_Status(new LogCargoStatusModel()
                {
                    Dlv_Inv = taxNumber,
                    Remark = "稅單查詢",
                    Search_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    User_Ip = _globalService.GetIPAddress(),
                    User_Id = UserContextService.GetUserId()
                });

                HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                FtpClient ftp = new FtpClient();
                ftp.Host = "192.168.1.5";
                ftp.Credentials = new NetworkCredential("tax_user", "a5d+46b2j59");
                ftp.Connect();
                //取得稅單Pdf路徑
                string filePath = _cargoService.GetClearance_Tax_Pdf(taxNumber);
                if (filePath == "")
                {
                    return Content("查無資料");
                }
                else
                {
                    byte[] content;
                    ftp.DownloadBytes(out content, filePath); //下載FTP檔案
                    MemoryStream stream = new MemoryStream(content);
                    stream.Position = 0;
                    ftp.Dispose();
                    Response.AppendHeader("Content-Disposition", "inline; filename=稅單.pdf;");
                    return File(content, "application/pdf");
                }
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        /// <summary>
        /// 貨況查詢-查看簽收單圖片
        /// </summary>
        /// <param name="cargoNumber"></param>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult CargoSignReceipt(string cargoNumber)
        {
            CargoSignReceiptViewModel vm = new CargoSignReceiptViewModel();
            vm.UrlList = new List<UrlItem>();
            string filePath;
            byte[] content;
            FtpClient ftp = new FtpClient();
            ftp.Host = "192.168.1.5";
            ftp.Credentials = new NetworkCredential("sign_user", "b9Q5-841ph66");
            ftp.Connect();
            //簽收單路徑
            DataTable dt = _cargoService.GetCargo_Sign_Receipt(cargoNumber);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                content = null;
                filePath = dt.Rows[i]["FilePath"].ToString();
                ftp.DownloadBytes(out content, filePath); //下載FTP檔案
                if (content != null)
                {
                    vm.UrlList.Add(new UrlItem()
                    {
                        Url = Convert.ToBase64String(content)
                    });
                }
            }
            ftp.Dispose();
            //紀錄LOG
            _cargoService.InsertLog_Cargo_Status(new LogCargoStatusModel()
            {
                Dlv_Inv = cargoNumber,
                Remark = "簽收單查詢",
                Search_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                User_Ip = _globalService.GetIPAddress(),
                User_Id = UserContextService.GetUserId()
            });

            return View(vm);
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargoShopee)]
        public ActionResult BatchSearchCargoShopee()
        {
            return View();
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表上傳
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargoShopee)]
        [HttpPost]
        public JsonResult BatchSearchCargoShopee(HttpPostedFileBase file)
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
                            resopnseModel = _cargoService.BatchSearchCargo2(filePath, fileName, UserContextService.GetUserId());
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
        /// 批量貨況查詢蝦皮明細表Excel
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargoShopee)]
        public ActionResult BatchSearchCargoShopeeExcel(string upload_time, string upload_ope)
        {
            string fileName = "";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetBatchSearchCargoShopeeWorkbook(upload_time, upload_ope);
                fileName = $"CNCB Return List-{DateTime.Now.ToString("yyyyMMdd")}.xlsx";
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
        /// 批量貨況查詢蝦皮明細表Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        IWorkbook GetBatchSearchCargoShopeeWorkbook(string upload_time, string upload_ope)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得批量貨況查詢明細表
            DataTable dt_Report = _cargoService.GetBatchSearchCargo2(upload_time, upload_ope).dt;
            //產生EXCEL
            GetBatchSearchCargoShopeeSheet(workbook, dt_Report, "Details");
            return workbook;
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetBatchSearchCargoShopeeSheet(IWorkbook workbook, DataTable dt_Report, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("Region");
            row.CreateCell(1).SetCellValue("shopee_batch");
            //尾程單號
            row.CreateCell(2).SetCellValue("lm_tracking_no");
            //分提單號
            row.CreateCell(3).SetCellValue("ordersn");
            //客戶訂單號
            row.CreateCell(4).SetCellValue("sls_trace_no");
            row.CreateCell(5).SetCellValue("Return_type");
            row.CreateCell(6).SetCellValue("Cargo_ready_date");
            row.CreateCell(7).SetCellValue("return_sls_trace_no");
            row.CreateCell(8).SetCellValue("Return_lm_tracking_no");
            row.CreateCell(9).SetCellValue("Carton_number");
            row.CreateCell(10).SetCellValue("Width_cm");
            row.CreateCell(11).SetCellValue("Length_cm");
            row.CreateCell(12).SetCellValue("Height_cm");
            row.CreateCell(13).SetCellValue("cbm");
            row.CreateCell(14).SetCellValue("weight");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 5000);
            sheet.SetColumnWidth(12, 5000);
            sheet.SetColumnWidth(13, 5000);
            sheet.SetColumnWidth(14, 5000);

            string expressNo;
            for (int i = 0; i < dt_Report.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                expressNo = dt_Report.Rows[i]["EXPRESS_NO"].ToString().Trim();

                row.CreateCell(0).SetCellValue("TW");
                row.CreateCell(2).SetCellValue(expressNo != "" ? expressNo : "-");
                row.CreateCell(3).SetCellValue(dt_Report.Rows[i]["TrackingNo"].ToString());
                row.CreateCell(4).SetCellValue(dt_Report.Rows[i]["ORDER_NO"].ToString());
                row.CreateCell(5).SetCellValue("FD");
                row.CreateCell(7).SetCellValue("-");
                row.CreateCell(8).SetCellValue("-");
                row.CreateCell(9).SetCellValue("-");
                row.CreateCell(10).SetCellValue("-");
                row.CreateCell(11).SetCellValue("-");
                row.CreateCell(12).SetCellValue("-");
                row.CreateCell(13).SetCellValue("-");
                row.CreateCell(14).SetCellValue("-");
            }
        }


        /// <summary>
        /// 處置說明下載
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.DownloadProcess)]
        public ActionResult DownloadProcess()
        {
            string custId, custName;
            var vm = new DownloadProcessViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            //客戶
            var dt_CustList = _customerService.GetCustomerList();

            var customerList = new List<SelectListItem>();
            customerList.Add(new SelectListItem() { Text = "全部", Value = "All" });
            for (int i = 0; i < dt_CustList.Rows.Count; i++)
            {
                custId = dt_CustList.Rows[i]["CUST_ID"].ToString().Trim();
                custName = $"{dt_CustList.Rows[i]["TRAN_TYPE"].ToString()}-{dt_CustList.Rows[i]["CUSTOMER"].ToString()}";
                customerList.Add(new SelectListItem() { Text = custName, Value = custId });
            }
            vm.ddlCustomerList = customerList;

            //分類
            var typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem() { Text = "全部", Value = "All" });
            typeList.Add(new SelectListItem() { Text = "貨況", Value = "1" });
            typeList.Add(new SelectListItem() { Text = "退運", Value = "2" });
            typeList.Add(new SelectListItem() { Text = "錯單公司名義收單", Value = "3" });
            typeList.Add(new SelectListItem() { Text = "現場轉出", Value = "4" });
            typeList.Add(new SelectListItem() { Text = "公司名義不回艙", Value = "5" });

            //分類
            var finistList = new List<SelectListItem>();
            finistList.Add(new SelectListItem() { Text = "全部", Value = "All" });
            finistList.Add(new SelectListItem() { Text = "結案", Value = "Y" });
            finistList.Add(new SelectListItem() { Text = "未結案", Value = "N" });

            vm.ddlProcessTypeList = typeList;
            vm.ddlFinistList = finistList;
            return View(vm);
        }

        /// <summary>
        ///  處置說明下載 Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.DownloadProcess)]
        public ActionResult ProcessExcel(DownloadProcessViewModel vm)
        {
            var sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            var eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            var report = _cargoService.ProcessReport(vm.custId, sDate, eDate, vm.ProcessType, vm.Finish);
            if (report.status == Status.error)
                return new JsonResult() { Data = new { msg = report.msg } };

            IWorkbook workbook = GetProcessWorkbook(report.Rows);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";

            fileName = $"{sDate}~{eDate}-處置說明.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = report.msg }
            };
        }

        /// <summary>
        /// 處置說明 Excel格式
        /// </summary>
        /// <param name="rows">處置說明報表資料列。</param>
        /// <returns>包含處置說明工作表的 Excel 活頁簿。</returns>
        public IWorkbook GetProcessWorkbook(IList<ProcessReportRowModel> rows)
        {
            IWorkbook workbook = new XSSFWorkbook();

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            XSSFDataFormat format = (XSSFDataFormat)workbook.CreateDataFormat();

            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("輸入日期");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("客戶名稱");
            row.CreateCell(3).SetCellValue("到港日");
            row.CreateCell(4).SetCellValue("主提單號");
            row.CreateCell(5).SetCellValue("清關袋號");
            row.CreateCell(6).SetCellValue("物流貨號");
            row.CreateCell(7).SetCellValue("入倉日期");
            row.CreateCell(8).SetCellValue("出倉日期");
            row.CreateCell(9).SetCellValue("收件人名稱");
            row.CreateCell(10).SetCellValue("收件人電話");
            row.CreateCell(11).SetCellValue("處置說明");
            row.CreateCell(12).SetCellValue("輸入時間");
            row.CreateCell(13).SetCellValue("輸入人員姓名");
            row.CreateCell(14).SetCellValue("處置說明結案碼");
            row.CreateCell(15).SetCellValue("輸入結案碼時間");
            row.CreateCell(16).SetCellValue("輸入結案碼人員姓名");


            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 3000);
            sheet.SetColumnWidth(2, 8000);
            sheet.SetColumnWidth(3, 4000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 8000);
            sheet.SetColumnWidth(10, 6000);
            sheet.SetColumnWidth(11, 30000);
            sheet.SetColumnWidth(12, 6000);
            sheet.SetColumnWidth(13, 6000);
            sheet.SetColumnWidth(14, 6000);
            sheet.SetColumnWidth(15, 6000);
            sheet.SetColumnWidth(16, 6000);


            for (int i = 0; i < rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);

                var item = rows[i];
                row.CreateCell(0).SetCellValue(item.DataDate ?? "");
                row.CreateCell(1).SetCellValue(item.IDataType ?? "");
                row.CreateCell(2).SetCellValue(item.Customer ?? "");
                if (item.Eta.HasValue)
                {
                    row.CreateCell(3).SetCellValue(item.Eta.Value);
                    row.GetCell(3).CellStyle = date2Style;
                }
                else
                {
                    row.CreateCell(3).SetCellValue("");
                }

                row.CreateCell(4).SetCellValue(item.MainNumber ?? "");
                row.CreateCell(5).SetCellValue(item.BlNo ?? "");
                row.CreateCell(6).SetCellValue(item.DlvInv ?? "");
                if (item.ISignInTime.HasValue)
                {
                    row.CreateCell(7).SetCellValue(item.ISignInTime.Value);
                    row.GetCell(7).CellStyle = dateStyle;
                }
                if (item.ISignOutTime.HasValue)
                {
                    row.CreateCell(8).SetCellValue(item.ISignOutTime.Value);
                    row.GetCell(8).CellStyle = dateStyle;
                }
                row.CreateCell(9).SetCellValue(item.Importer ?? "");
                row.CreateCell(10).SetCellValue(item.ImPhoneNo ?? "");
                row.CreateCell(11).SetCellValue(item.Remark ?? "");
                if (item.CreateDateTime.HasValue)
                {
                    row.CreateCell(12).SetCellValue(item.CreateDateTime.Value);
                    row.GetCell(12).CellStyle = dateStyle;
                }
                row.CreateCell(13).SetCellValue(item.UserName ?? "");
                row.CreateCell(14).SetCellValue(item.Finish ?? "");
                if (item.FinishDateTime.HasValue)
                {
                    row.CreateCell(15).SetCellValue(item.FinishDateTime.Value);
                    row.GetCell(15).CellStyle = dateStyle;
                }
                row.CreateCell(16).SetCellValue(item.FinishUserName ?? "");
            }

            return workbook;
        }

        /// <summary>
        /// 轉檔查詢
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.SearchWork)]
        public ActionResult SearchWork()
        {
            return View();
        }

        /// <summary>
        /// 轉檔查詢-明細
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.SearchWork)]
        public ActionResult GetLogWork()
        {
            DataTable dt = _cargoService.GetLog_Work();
            string data = JsonConvert.SerializeObject(dt);
            return Json(data, JsonRequestBehavior.AllowGet);
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
    }
}
