using iTextSharp.text;
using iTextSharp.text.pdf;
using JETFTAX.Models.CCLWork;
using JETFTAX.Models.EtlClearanceDetails;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services;
using Service.Services.EtlClearanceDetails;
using Service.Services.ScanCargoCustomer;
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
    public class CCLWorkController : Controller
    {
        private readonly ScanCargoCustomerService _scanCargoCustomerService;
        private readonly EtlClearanceDetailsService _etlClearanceDetailsService;
        private readonly GlobalService _globalService;
        private readonly CCLWorkService _cclWorkService;

        public CCLWorkController(ScanCargoCustomerService scanCargoCustomerService, EtlClearanceDetailsService etlClearanceDetailsService, GlobalService globalService, CCLWorkService cclWorkService) 
        {
            _scanCargoCustomerService = scanCargoCustomerService;
            _etlClearanceDetailsService = etlClearanceDetailsService;
            _globalService = globalService;
            _cclWorkService = cclWorkService;
        }

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle;
        iTextSharp.text.Font font8, font9, font10, font11, font12, font14, font16, font18, font20, fontB18;

        /// <summary>
        /// 空快清關主號明細表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "4", "6")]
        [UserAuthorize(Authority.EtlClearanceMainDetails)]
        public ActionResult ETLCCLMainDetails()
        {
            EtlClearanceDetailsViewModel vm = new EtlClearanceDetailsViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd") + " 00:00";
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd") + " 23:59";
            vm.dataTime = "EditDateTime";
            return View(vm);
        }

        /// <summary>
        /// 空快清關主號明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "4")]
        [UserAuthorize(Authority.EtlClearanceMainDetails)]
        public ActionResult ETLCCLMainDetailsExcel(EtlClearanceDetailsViewModel vm)
        {
            string sDate = vm.sDate;
            string eDate = vm.eDate;
            string fileName = $"{sDate}～{eDate}-空快清關主號明細表.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetETLCCLMainDetailsWorkbook(sDate, eDate);
                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }

                //紀錄LOG
                _etlClearanceDetailsService.InsertLog_ClearanceWork(new LogClearanceWork()
                {
                    WorkName = "空快清關主號明細表",
                    DownloadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Ip = _globalService.GetIPAddress(),
                    UserId = UserContextService.GetUserId()
                });
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
        /// 空快清關主號明細表-Excel
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetETLCCLMainDetailsWorkbook(string sDate, string eDate)
        {
            string mawbNo;
            IWorkbook workbook = new XSSFWorkbook();
            //空快清關明細表
            DataTable dt_Order_Cargo_Manifest = _cclWorkService.GetOrder_Cargo_Manifest(sDate, eDate);
            //主號X類
            var dt_Group = from t in dt_Order_Cargo_Manifest.AsEnumerable()
                           group t by new { MawbNo = t.Field<string>("MawbNo") } into g
                           orderby g.Key.MawbNo
                           select new
                           {
                               MawbNo = g.Key.MawbNo,
                           };
            foreach (var item in dt_Group)
            {
                //主號
                mawbNo = item.MawbNo.ToString();
                if (mawbNo != "")
                {
                    //空快清關明細表
                    GetETLCCLMainDetailsSheet(workbook, dt_Order_Cargo_Manifest, mawbNo, $"捷豐清關主單資料({item.MawbNo})", sDate, eDate);
                }
                else
                {
                    //空快清關明細表
                    GetETLCCLMainDetailsSheet(workbook, dt_Order_Cargo_Manifest, mawbNo, $"捷豐清關主單資料(無主號)", sDate, eDate);
                }
            }
            return workbook;
        }

        /// <summary>
        /// 空快清關主號明細表-Excel-頁籤
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetETLCCLMainDetailsSheet(IWorkbook workbook, DataTable dt_Order_Cargo_Manifest, string mawbNo, string sheetName, string sDate, string eDate)
        {

            List<CargoManifestModel> dt_Details = (from t in dt_Order_Cargo_Manifest.AsEnumerable()
                                                   where t["MawbNo"].ToString() == mawbNo
                                                   orderby t["MawbNo"]
                                                   select new CargoManifestModel()
                                                   {
                                                       To = t["To"].ToString(),
                                                       Broker = t["Broker"].ToString(),
                                                       Date = t["Date"].ToString(),
                                                       BillingCode = t["BillingCode"].ToString(),
                                                       Tel = t["Tel"].ToString(),
                                                       Fax = t["Fax"].ToString(),
                                                       FlightNo = t["FlightNo"].ToString(),
                                                       MawbNo = t["MawbNo"].ToString(),
                                                       TotalCnt = t["TotalCnt"].ToString(),
                                                       TotalGrossWeight = t["TotalGrossWeight"].ToString(),
                                                       ItemNo = t["ItemNo"].ToString(),
                                                       MasterBagNo = t["MasterBagNo"].ToString(),
                                                       Ctn = t["Ctn"].ToString(),
                                                       GrossWeight = t["GrossWeight"].ToString(),
                                                       Description = t["Description"].ToString(),
                                                       DeclaredTo = t["DeclaredTo"].ToString(),
                                                       Remark = t["Remark"].ToString(),
                                                       Manifest_CrtDateTime = t["Manifest_CrtDateTime"].ToString()
                                                   }).ToList();

            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("Cargo Manifest");

            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("TO:");
            row.CreateCell(1).SetCellValue(dt_Details[0].To);
            row.CreateCell(3).SetCellValue("BROKER:");
            row.CreateCell(4).SetCellValue(dt_Details[0].Broker);
            row.CreateCell(5).SetCellValue("DATE:");
            row.CreateCell(6).SetCellValue(dt_Details[0].Date);

            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("BILLING CODE:");
            row.CreateCell(1).SetCellValue(dt_Details[0].BillingCode);
            row.CreateCell(3).SetCellValue("TEL:");
            row.CreateCell(4).SetCellValue(dt_Details[0].Tel);
            row.CreateCell(5).SetCellValue("FAX:");
            row.CreateCell(6).SetCellValue(dt_Details[0].Fax);

            row = sheet.CreateRow(3);
            row.CreateCell(0).SetCellValue("FLIGHT NO.");
            row.CreateCell(1).SetCellValue(dt_Details[0].FlightNo);
            row.CreateCell(3).SetCellValue("MAWB.NO");
            row.CreateCell(4).SetCellValue(dt_Details[0].MawbNo);

            //表頭 
            row = sheet.CreateRow(4);
            row.CreateCell(0).SetCellValue("ITEM");
            row.CreateCell(1).SetCellValue("MASTER BAG NO.");
            row.CreateCell(2).SetCellValue("CTN");
            row.CreateCell(3).SetCellValue("G.W.");
            row.CreateCell(4).SetCellValue("DESCRIPTION");
            row.CreateCell(5).SetCellValue("DECLARED TO");
            row.CreateCell(6).SetCellValue("Remark");
            row.CreateCell(7).SetCellValue("原袋號進來日期");

            sheet.SetColumnWidth(0, 7000);
            sheet.SetColumnWidth(1, 7000);
            sheet.SetColumnWidth(2, 3000);
            sheet.SetColumnWidth(3, 3000);
            sheet.SetColumnWidth(4, 7000);
            sheet.SetColumnWidth(5, 7000);
            sheet.SetColumnWidth(6, 7000);
            sheet.SetColumnWidth(7, 7000);

            int irow = 5;
            int num;
            double dbl;
            foreach (var item in dt_Details)
            {
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(irow - 4);
                row.CreateCell(1).SetCellValue(item.MasterBagNo);
                //件數
                if (int.TryParse(item.Ctn, out num))
                {
                    row.CreateCell(2).SetCellValue(num);
                }
                else
                {
                    row.CreateCell(2).SetCellValue(item.Ctn.ToString());
                }
                //GrossWeight
                if (double.TryParse(item.GrossWeight.ToString(), out dbl))
                {
                    row.CreateCell(3).SetCellValue(dbl);
                }
                else
                {
                    row.CreateCell(3).SetCellValue(item.GrossWeight.ToString());
                }
                row.CreateCell(4).SetCellValue(item.Description);
                row.CreateCell(5).SetCellValue(item.DeclaredTo);
                row.CreateCell(6).SetCellValue(item.Remark);
                if (item.Manifest_CrtDateTime != "")
                {
                    row.CreateCell(7).SetCellValue(Convert.ToDateTime(item.Manifest_CrtDateTime));
                    row.GetCell(7).CellStyle = dateStyle;
                }
                irow++;
            }
        }

        /// <summary>
        /// 上傳拆袋資料
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "4", "6")]
        [UserAuthorize(Authority.UploadUnpackingBagNo)]
        public ActionResult UploadFileB6F()
        {
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "海運", Value = "SEA" });
            sourceList.Add(new SelectListItem() { Text = "空運", Value = "ETL" });
            UploadFileB6FViewModel vm = new UploadFileB6FViewModel()
            {
                ddlSourceList = sourceList
            };
            return View(vm);
        }

        /// <summary>
        /// 上傳拆袋資料-檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [HttpPost]
        //[UserAuthorize("1", "2", "4", "6")]
        [UserAuthorize(Authority.UploadUnpackingBagNo)]
        public JsonResult UploadFileB6F(HttpPostedFileBase file, UploadFileB6FViewModel vm)
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
                            resopnseModel = _cclWorkService.UploadFileB6F(filePath, vm.source, UserContextService.GetUserId());
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
        /// 已拆袋明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.UnpackingBagNoDetails)]
        public ActionResult B6FUnpackingDetails()
        {
            B6FUnpackingDetailsViewModel vm = new B6FUnpackingDetailsViewModel();
            DateTime date = DateTime.Now;
            vm.sDate = $"{date.ToString("yyyy-MM-dd")} 00:00";
            vm.eDate = $"{date.ToString("yyyy-MM-dd")} 23:59";
            DataTable dt_DataType = _cclWorkService.GetPdtDataType();
            List<SelectListItem> dataTypeList = new List<SelectListItem>();
            foreach (DataRow item in dt_DataType.Rows)
            {
                dataTypeList.Add(new SelectListItem() { Text = item["DataType"].ToString(), Value = item["DataType"].ToString() });
            }
            vm.ddlDataTypeList = dataTypeList;
            return View(vm);
        }

        /// <summary>
        /// 已拆袋明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.UnpackingBagNoDetails)]
        public ActionResult B6FUnpackingDetailsExcel(B6FUnpackingDetailsViewModel vm)
        {
            string sDate = vm.sDate;
            string eDate = vm.eDate;
            string fileName = $"{sDate}~{eDate}-B6F已拆袋明細表.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetB6FUnpackingDetailsWorkbook(sDate, eDate, vm.dataType);
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
        /// 已拆袋明細表-Excel
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetB6FUnpackingDetailsWorkbook(string sDate, string eDate, string dataType)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //B6F已拆袋明細資料
            DataTable dt = new DataTable();
            if (dataType == "TACT" || dataType == "FTZ" || dataType == "華儲通關" || dataType == "遠雄通關")
            {
                dt = _cclWorkService.GetB6F_Unpacking_Upload(sDate, eDate, dataType);
                //B6F已拆袋明細
                GetB6FUnpackingDetailsSheet(workbook, dt, "B6F已拆袋明細表", sDate, eDate);
            }
            else
            {
                dt = _cclWorkService.GetB6F_Sea_Unpacking_Upload(sDate, eDate, dataType);
                //B6F已拆袋明細
                GetB6FSeaUnpackingDetailsSheet(workbook, dt, "B6F已拆袋明細表", sDate, eDate);
            }
            return workbook;
        }

        /// <summary>
        /// 已拆袋明細-Excel-頁籤(空運)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetB6FUnpackingDetailsSheet(IWorkbook workbook, DataTable dt, string sheetName, string sDate, string eDate)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("航班日期");
            row.CreateCell(1).SetCellValue("申請單位");
            row.CreateCell(2).SetCellValue("航班號");
            row.CreateCell(3).SetCellValue("主號");
            row.CreateCell(4).SetCellValue("袋號");
            row.CreateCell(5).SetCellValue("申請拆袋分提單號");
            row.CreateCell(6).SetCellValue("備註");
            row.CreateCell(7).SetCellValue("袋號掃描時間");
            row.CreateCell(8).SetCellValue("分提單號掃描時間");
            row.CreateCell(9).SetCellValue("袋號掃描人員");
            row.CreateCell(10).SetCellValue("分提單號掃描人員");
            row.CreateCell(11).SetCellValue("ZZZA上傳時間");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 7000);
            sheet.SetColumnWidth(2, 3000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 7000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 5000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 5000);

            DateTime scan_upload_time;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["FLIGHTDATE"].ToString());
                row.CreateCell(1).SetCellValue(dt.Rows[i]["CUSTOMER"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["FLIGHTNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["MAINNUMBER"].ToString());
                row.CreateCell(4).SetCellValue(dt.Rows[i]["BAGNO"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["REMARK"].ToString());
                //袋號掃描時間
                if (DateTime.TryParse(dt.Rows[i]["SCAN_UPLOAD_TIME"].ToString(), out scan_upload_time))
                {
                    row.CreateCell(7).SetCellValue(scan_upload_time);
                    row.GetCell(7).CellStyle = dateStyle;
                }
                //分提單號掃描時間
                if (DateTime.TryParse(dt.Rows[i]["SCAN_UPLOAD_TIME2"].ToString(), out scan_upload_time))
                {
                    row.CreateCell(8).SetCellValue(scan_upload_time);
                    row.GetCell(8).CellStyle = dateStyle;
                }
                row.CreateCell(9).SetCellValue(dt.Rows[i]["SCAN_UPLOAD_OPE"].ToString());
                row.CreateCell(10).SetCellValue(dt.Rows[i]["SCAN_UPLOAD_OPE2"].ToString());
               
                //ZZZA上傳時間
                if (DateTime.TryParse(dt.Rows[i]["ZZZA_UPLOAD_TIME"].ToString(), out var zzza_upload_time))
                {
                    row.CreateCell(11).SetCellValue(zzza_upload_time);
                    row.GetCell(11).CellStyle = dateStyle;
                }
            }
        }

        /// <summary>
        /// 已拆袋明細-Excel-頁籤(海運)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetB6FSeaUnpackingDetailsSheet(IWorkbook workbook, DataTable dt, string sheetName, string sDate, string eDate)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("作業地區");
            row.CreateCell(1).SetCellValue("主號");
            row.CreateCell(2).SetCellValue("分提單號");
            row.CreateCell(3).SetCellValue("PDT訊息");
            row.CreateCell(4).SetCellValue("掃描人員");
            row.CreateCell(5).SetCellValue("掃描時間");

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 7000);
            sheet.SetColumnWidth(2, 7000);
            sheet.SetColumnWidth(3, 7000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);

            DateTime scan_upload_time;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["DATATYPE"].ToString());
                row.CreateCell(1).SetCellValue(dt.Rows[i]["MAINNUMBER"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["PDTMESSAGE"].ToString());
                row.CreateCell(4).SetCellValue(dt.Rows[i]["SCAN_UPLOAD_OPE"].ToString());
                //掃描時間
                if (DateTime.TryParse(dt.Rows[i]["SCAN_UPLOAD_TIME"].ToString(), out scan_upload_time))
                {
                    row.CreateCell(5).SetCellValue(scan_upload_time);
                    row.GetCell(5).CellStyle = dateStyle;
                }
            }
        }

        /// <summary>
        /// 拆袋作業明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.UnpackingBagNoWorkDetails)]
        public ActionResult UnpackingDetails()
        {
            UnpackingDetailsViewModel vm = new UnpackingDetailsViewModel();
            DateTime date = DateTime.Now;
            vm.sDate = $"{date.ToString("yyyy-MM-dd")} 00:00";
            vm.eDate = $"{date.ToString("yyyy-MM-dd")} 23:59";
            DataTable dt_DataType = _cclWorkService.GetPdtDataType();
            List<SelectListItem> dataTypeList = new List<SelectListItem>();
            foreach (DataRow item in dt_DataType.Rows)
            {
                dataTypeList.Add(new SelectListItem() { Text = item["DataType"].ToString(), Value = item["DataType"].ToString() });
            }
            vm.ddlDataTypeList = dataTypeList;
            return View(vm);
        }

        /// <summary>
        /// 拆袋作業明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.UnpackingBagNoWorkDetails)]
        public ActionResult UnpackingDetailsExcel(UnpackingDetailsViewModel vm)
        {
            string dataType = vm.dataType;
            string sDate = vm.sDate;
            string eDate = vm.eDate;
            string fileName = $"{sDate}~{eDate}-拆袋明細表.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetUnpackingDetailsWorkbook(dataType, sDate, eDate);
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
        /// 拆袋作業明細表-Excel
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetUnpackingDetailsWorkbook(string dataType, string sDate, string eDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //拆袋明細資料
            DataTable dt = _cclWorkService.GetPdtUnpacking(dataType, sDate, eDate);
            //拆袋明細
            GetUnpackingDetailsSheet(workbook, dt, $"拆袋明細表", sDate, eDate);

            return workbook;
        }

        /// <summary>
        /// 拆袋作業明細表-Excel-頁籤
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetUnpackingDetailsSheet(IWorkbook workbook, DataTable dt, string sheetName, string sDate, string eDate)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("作業地區");
            row.CreateCell(1).SetCellValue("袋號");
            row.CreateCell(2).SetCellValue("分提單號");
            row.CreateCell(3).SetCellValue("作業人員");
            row.CreateCell(4).SetCellValue("作業時間");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);

            DateTime uploadTime;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["DataType"].ToString());
                row.CreateCell(1).SetCellValue(dt.Rows[i]["BagNo"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["TrackingNo"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["UploadOpe"].ToString());
                //作業時間
                if (DateTime.TryParse(dt.Rows[i]["UploadTime"].ToString(), out uploadTime))
                {
                    row.CreateCell(4).SetCellValue(uploadTime);
                    row.GetCell(4).CellStyle = dateStyle;
                }

            }
        }

        /// <summary>
        /// 掃貨上車交接派件公司明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.ScanCargoDetails)]
        public ActionResult ScanCargoDetails()
        {
            ScanCargoDetailsViewModel vm = new ScanCargoDetailsViewModel();
            DateTime date = DateTime.Now;
            vm.sDate = $"{date.ToString("yyyy-MM-dd")} 00:00";
            vm.eDate = $"{date.ToString("yyyy-MM-dd")} 23:59";
            DataTable dt_DataType = _cclWorkService.GetPdtDataType();
            List<SelectListItem> dataTypeList = new List<SelectListItem>();
            foreach (DataRow item in dt_DataType.Rows)
            {
                dataTypeList.Add(new SelectListItem() { Text = item["DataType"].ToString(), Value = item["DataType"].ToString() });
            }
            vm.ddlDataTypeList = dataTypeList;

            DataTable dt_Trans = _cclWorkService.GetPdtTrans();
            List<SelectListItem> transList = new List<SelectListItem>();
            foreach (DataRow item in dt_Trans.Rows)
            {
                transList.Add(new SelectListItem() { Text = item["TransName"].ToString(), Value = item["TransNo"].ToString() });
            }
            vm.ddlTransList = transList;
            return View(vm);
        }

        /// <summary>
        /// 掃貨上車交接派件公司明細表-PDF
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.ScanCargoDetails)]
        public ActionResult ScanCargoDetailsPdf(ScanCargoDetailsViewModel vm)
        {
            ResponseModel resopnseModel = new ResponseModel();
            DataTable dt = _cclWorkService.GetScanCargoDetailsPdf(vm.trans, vm.dataType, vm.sDate, vm.eDate);
            if (dt.Rows.Count > 0)
            {
                string dataDate = Convert.ToDateTime(vm.eDate).ToString("yyyy/MM/dd");
                byte[] content = GetPdf(dt, dataDate);
                Response.AppendHeader("Content-Disposition", "inline; filename=掃貨上車.pdf;");
                return File(content, "application/pdf");
            }
            else
            {
                return Content("查無資料");
            }
        }

        /// <summary>
        /// 掃貨上車交接派件公司明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.ScanCargoDetails)]
        public ActionResult ScanCargoDetailsExcel(ScanCargoDetailsViewModel vm)
        {
            string handle = Guid.NewGuid().ToString();
            string dataDate = Convert.ToDateTime(vm.eDate).ToString("yyyy/MM/dd");
            string fileName = $"{dataDate}交接單統計表.xlsx";

            string msg = "";
            try
            {

                //取得資料
                var result = _scanCargoCustomerService.GetScanCargoCustomerDetailsPdf(vm.trans, vm.dataType, vm.sDate, vm.eDate);

                DataTable dt = result.Item1;
                DataTable dt_Exclude = result.Item2;

                IWorkbook workbook = GetScanCargoDetailsWorkbook(dt, dt_Exclude);
                //拆袋作業差異表
                DataTable dt_Diff = _cclWorkService.GetClearanceInfoScanCargoDetails(vm.dataType, vm.sDate, vm.eDate);
                //客戶名稱
                DataTable dt_Cust = _cclWorkService.GetClearanceInfoScanCargoCustomer(vm.dataType, vm.sDate, vm.eDate);
                GetClearanceInfoScanCargoDetailsSheet(workbook, dt_Diff, dt_Cust);

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
        /// 掃貨上車交接派件公司明細表-PDF
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        byte[] GetPdf(DataTable dt, string dataDate)
        {
            string carNo;
            DataRow[] dr;
            FontFactory.Register(@"C:\windows\Fonts\msjh.ttc");
            font8 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 8f);
            font9 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 9f);
            font10 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 10f);
            font11 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 11f);
            font12 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 12f);
            font14 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 14f);
            font16 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 16f);
            font18 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 18f);
            font20 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 20f);
            fontB18 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 18f, Font.BOLD);

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 0, 0, 20, 10);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();
                PdfPTable table;
                string transName;
                int page;
                int count = 100; //一頁筆數
                //派件公司分頁
                var dt_Group = from t in dt.AsEnumerable()
                               group t by new { TransName = t.Field<string>("TransName") } into g
                               orderby g.Key.TransName
                               select new
                               {
                                   TransName = g.Key.TransName,
                               };
                foreach (var item in dt_Group)
                {
                    transName = item.TransName ?? "";
                    if (transName != "")
                    {
                        dr = dt.Select($"TransName='{transName}'");
                    }
                    else
                    {
                        dr = dt.Select("TransName is null");
                    }

                    page = (int)Math.Ceiling(dr.Length / (count + 0.0));
                    for (int i = 0; i < page; i++)
                    {
                        if (i > 0)
                        {
                            pdfDoc.NewPage();
                        }
                        carNo = dr[0]["CarNo"].ToString();
                        table = new PdfPTable(new float[] { 1 });
                        table.TotalWidth = 550f;
                        table.LockedWidth = true;
                        table.AddCell(new PdfPCell(TabTitle(transName, dataDate)) { PaddingTop = 0, Border = 0 });
                        table.AddCell(new PdfPCell(TabBody(dr, i, count)) { PaddingTop = 0, Border = 0 });
                        table.AddCell(new PdfPCell(TabFooter(transName, carNo)) { PaddingTop = 0, Border = 0 });
                        pdfDoc.Add(table);
                    }
                    pdfDoc.NewPage();
                }

                pdfDoc.Close();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 掃貨上車交接派件公司明細表-PDF-標題
        /// </summary>
        /// <param name="transName"></param>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        PdfPTable TabTitle(string transName, string dataDate)
        {
            PdfPTable table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550f;
            table.LockedWidth = true;
            table.AddCell(new PdfPCell(new Phrase("捷豐 貨物轉交 簽收單", font16)) { Border = 0, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase($"派件公司：{transName}", font12)) { Border = 0, MinimumHeight = 15, HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(new PdfPCell(new Phrase($"日期：{dataDate}", font12)) { Border = 0, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_LEFT });
            return table;
        }

        /// <summary>
        /// 掃貨上車交接派件公司明細表-PDF-內容
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        PdfPTable TabBody(DataRow[] dr, int page, int count)
        {
            int start = (page * count);
            int end = start + 50;
            string data, data2,field, field2;
            PdfPTable table = new PdfPTable(new float[] { 20, 90, 100, 20, 90, 100 });
            table.TotalWidth = 550;
            //table.LockedWidth = true;
            table.AddCell(new PdfPCell(new Phrase("編號", font12)) { MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("交接單號", font12)) { MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("外箱條碼", font12)) { MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("編號", font12)) { MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("交接單號", font12)) { MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("外箱條碼", font12)) { MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_CENTER });
            for (int i = start; i < end; i++)
            {
                data = "";
                data2 = "";
                field = "";
                field2 = "";
                if (dr.Length > i)
                {
                    data = dr[i]["Data"].ToString().Trim();
                    field = dr[i]["FIELD_X"].ToString().Trim();
                }
                if (dr.Length > i + 50)
                {
                    data2 = dr[i + 50]["Data"].ToString().Trim();
                    field2 = dr[i + 50]["FIELD_X"].ToString().Trim();
                }

                table.AddCell(new PdfPCell(new Phrase((i + 1).ToString(), font9)) { MinimumHeight = 10, HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase(data, font9)) { MinimumHeight = 10, HorizontalAlignment = Element.ALIGN_LEFT });
                table.AddCell(new PdfPCell(new Phrase(field, font9)) { MinimumHeight = 10, HorizontalAlignment = Element.ALIGN_LEFT });
                table.AddCell(new PdfPCell(new Phrase((i + 51).ToString(), font9)) { MinimumHeight = 10,HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase(data2, font9)) { MinimumHeight = 10, HorizontalAlignment = Element.ALIGN_LEFT });
                table.AddCell(new PdfPCell(new Phrase(field2, font9)) { MinimumHeight = 10, HorizontalAlignment = Element.ALIGN_LEFT });
            }
            table.AddCell(new PdfPCell(new Phrase("備註：交貨時請務必給司機簽收,並寫上交貨時間!! 謝謝", font10)) { MinimumHeight = 25, HorizontalAlignment = Element.ALIGN_LEFT, Colspan = 6 });
            return table;
        }

        /// <summary>
        /// 掃貨上車交接派件公司明細表-PDF-頁尾
        /// </summary>
        /// <param name="transName"></param>
        /// <returns></returns>
        PdfPTable TabFooter(string transName,string carNo)
        {
            PdfPTable table = new PdfPTable(new float[] { 1, 1, 1, 1});
            table.TotalWidth = 550f;
            table.LockedWidth = true;
            //table.AddCell(new PdfPCell(new Phrase("*交貨時請務必給司機簽收,並寫上交貨時間!! 謝謝", font14)) { Border = 0, PaddingTop = 10, MinimumHeight = 40, HorizontalAlignment = Element.ALIGN_LEFT, Colspan = 6 });
            //table.AddCell(new PdfPCell(new Phrase("捷豐人員\n交接簽名：", font12)) { Border = 0, MinimumHeight = 15, PaddingTop = 5, HorizontalAlignment = Element.ALIGN_LEFT, Colspan = 3 });
            //table.AddCell(new PdfPCell(new Phrase("接駁人員\n簽名：", font12)) { Border = 0, MinimumHeight = 15, PaddingTop = 5, HorizontalAlignment = Element.ALIGN_LEFT, Colspan = 3 });
            //table.AddCell(new PdfPCell(new Phrase($"{transName}\n派件人員簽收：", font12)) { Border = 0, MinimumHeight = 15, PaddingTop = 15, HorizontalAlignment = Element.ALIGN_LEFT, Colspan = 3 });
            //table.AddCell(new PdfPCell(new Phrase($"車號\n櫃號：{carNo}", font12)) { Border = 0, MinimumHeight = 15, PaddingTop = 15, HorizontalAlignment = Element.ALIGN_LEFT, Colspan = 3 });
            table.AddCell(new PdfPCell(new Phrase("捷豐人員\n交接簽名：", font10)) { Border = 0, MinimumHeight = 15, PaddingTop = 10, HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(new PdfPCell(new Phrase("接駁人員\n簽名：", font10)) { Border = 0, MinimumHeight = 15, PaddingTop = 10, HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(new PdfPCell(new Phrase($"{transName} 派件人員\n簽收：", font10)) { Border = 0, MinimumHeight = 15, PaddingTop = 10, HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(new PdfPCell(new Phrase($"車號\n櫃號：{carNo}", font10)) { Border = 0, MinimumHeight = 15, PaddingTop = 10, HorizontalAlignment = Element.ALIGN_LEFT });
            return table;
        }

        public IWorkbook GetScanCargoDetailsWorkbook(DataTable dt,DataTable dt_Exclude)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得Excel樣式
            GetWorkbookStyle(workbook);
            //取得交接單統計表Sheet
            GetHandoverReportSheet(workbook, dt);
            //取得交接單明細表Sheet
            GetHandoverDetailSheet(workbook, "交接單明細表", dt);
            //取得排除空快回倉明細Sheet
            GetHandoverDetailSheet(workbook, "排除空快回倉明細", dt_Exclude);
            return workbook;
        }

        /// <summary>
        /// 取得交接單統計表Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public ISheet GetHandoverReportSheet(IWorkbook workbook, DataTable dt)
        {
            int irow, subTotal;
            ISheet sheet = workbook.CreateSheet("交接單統計表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("交接單統計表");
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 2));
            row.GetCell(0).CellStyle = cs_Center;

            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("派件公司");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("件數");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 4000);

            irow = 2;
            var transNameGroup = dt.AsEnumerable().GroupBy(t => new { TransName = t.Field<string>("TransName") });
            foreach (var item in transNameGroup)
            {
                var dt_Group = from t in dt.AsEnumerable()
                               where t.Field<string>("TransName") == item.Key.TransName
                               group t by new { TransName = t.Field<string>("TransName"), DESPATCHNAME = t.Field<string>("DESPATCHNAME") } into g
                               orderby g.Key.TransName, g.Key.DESPATCHNAME
                               select new
                               {
                                   TransName = g.Key.TransName,
                                   DESPATCHNAME = g.Key.DESPATCHNAME,
                                   TotalCount = g.Count()
                               };
                subTotal = 0;
                foreach (var item2 in dt_Group)
                {
                    row = sheet.CreateRow(irow);
                    row.CreateCell(0).SetCellValue(item2.TransName);
                    row.CreateCell(1).SetCellValue(item2.DESPATCHNAME);
                    row.CreateCell(2).SetCellValue(item2.TotalCount);
                    row.GetCell(0).CellStyle = cs_Title_Left;
                    row.GetCell(1).CellStyle = cs_Title_Left;
                    row.GetCell(2).CellStyle = cs_Int;
                    subTotal += item2.TotalCount;
                    irow++;
                }
                //小計
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue("小計");
                row.CreateCell(2).SetCellValue(subTotal);
                row.GetCell(0).CellStyle = cs_Center_Blue;
                row.GetCell(2).CellStyle = cs_Int_Blue;
                irow++;
            }
            return sheet;
        }

        /// <summary>
        /// 取得交接單明細表Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public ISheet GetHandoverDetailSheet(IWorkbook workbook,string sheetName, DataTable dt)
        {
            int irow, subTotal;
            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("客戶");
            row.CreateCell(1).SetCellValue("派件公司");
            row.CreateCell(2).SetCellValue("交接單號");
            row.CreateCell(3).SetCellValue("外箱條碼");
            row.CreateCell(4).SetCellValue("訂單號碼");
            row.CreateCell(5).SetCellValue("交艙時間");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            if (dt.Rows.Count > 0)
            {
                //排序
                dt.DefaultView.Sort = "TransName,DESPATCHNAME";
                dt = dt.DefaultView.ToTable();

                irow = 1;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    row = sheet.CreateRow(irow);
                    row.CreateCell(0).SetCellValue(dt.Rows[i]["DESPATCHNAME"].ToString());
                    row.CreateCell(1).SetCellValue(dt.Rows[i]["TransName"].ToString());
                    row.CreateCell(2).SetCellValue(dt.Rows[i]["Data"].ToString());
                    row.CreateCell(3).SetCellValue(dt.Rows[i]["FIELD_X"].ToString());
                    row.CreateCell(4).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                    row.CreateCell(5).SetCellValue(dt.Rows[i]["ArrivalTime"].ToString());
                    irow++;
                }
            }

            return sheet;
        }

        /// <summary>
        /// 拆袋作業差異表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public ISheet GetClearanceInfoScanCargoDetailsSheet(IWorkbook workbook, DataTable dt,DataTable dt_Cust)
        {
            int irow;

            ISheet sheet = workbook.CreateSheet("刷槍作業差異表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主號");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("入倉袋數");
            row.CreateCell(3).SetCellValue("現場實際刷貨");
            row.CreateCell(4).SetCellValue("查驗袋數");
            row.CreateCell(5).SetCellValue("查驗的袋號");
            row.CreateCell(6).SetCellValue("差異");
            row.CreateCell(7).SetCellValue("差異的袋號");
            row.CreateCell(8).SetCellValue("差異的分號");
            row.CreateCell(9).SetCellValue("備註");

            sheet.SetColumnWidth(0,5000);
            sheet.SetColumnWidth(1, 8000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4,5000);
            sheet.SetColumnWidth(5, 10000);
            sheet.AutoSizeColumn(6);
            sheet.SetColumnWidth(7,10000);
            sheet.SetColumnWidth(8, 10000);
            sheet.SetColumnWidth(9, 5000);

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

            var dt_Group = (from t in dt.AsEnumerable()
                           group t by new { MAIN_NUMBER = t.Field<string>("MAIN_NUMBER") } into g
                           orderby g.Key.MAIN_NUMBER
                           select new
                           {
                               MainNumber = g.Key.MAIN_NUMBER,
                               TotalCount = g.Count(),
                               ScanCount = g.Where(m => m.Field<string>("Data") != null).Count(),
                               CheckCount = g.Where(m => m.Field<string>("Data") == null && m.Field<DateTime?>("SIGN_OUT_TIME") == null).Count(),
                               CheckBagNumber = string.Join(",", g.Where(m => m.Field<string>("Data") == null && m.Field<DateTime?>("SIGN_OUT_TIME") == null)
                                                     .Select(m => m.Field<string>("BAG_NUMBER"))),
                               DiffCount = g.Where(m => m.Field<string>("Data") == null && m.Field<DateTime?>("SIGN_OUT_TIME") != null).Count(),
                               DiffList = g.Where(m => m.Field<string>("Data") == null && m.Field<DateTime?>("SIGN_OUT_TIME") != null)
                                                     .Select(m => new {
                                                         BagNumber = m.Field<string>("BAG_NUMBER"),
                                                         //相同袋號大於1筆 不顯示分提單號
                                                         MergeNumber = g.Count(x => x.Field<string>("BAG_NUMBER") == m.Field<string>("BAG_NUMBER")) > 1
                                                                       ? ""
                                                                       : m.Field<string>("MERGE_NUMBER")
                                                     }).Distinct().ToList(),
                           }).ToList();

            //取得處置說明
            var processTypeRemark = new Dictionary<string, string>
            {
                { "3", "公司名義" },
                { "4", "現場轉出" }
            };
            var list = dt_Group.SelectMany(t => 
             t.DiffList.Where(r => string.IsNullOrEmpty(r.MergeNumber) == false)
            .Select(r => r.MergeNumber))
            .ToList();

            var process = _cclWorkService.GetProcess(list);


            irow = 1;
            var transNameGroup = dt.AsEnumerable().GroupBy(t => new { TransName = t.Field<string>("TransName") });

            var wrapStyle = NpoiStyle.WrapStyle(workbook);

            foreach (var item in dt_Group)
            {
                //客戶
                var customer = (from r in dt_Cust.AsEnumerable()
                               where r.Field<string>("MAIN_NUMBER") == item.MainNumber
                               select r.Field<string>("DESPATCHNAME")).ToList();

                int startRow = irow; // 記錄合併起始行
                int diffCount = item.DiffList.Count; // DiffList 數量決定要往下合併的列數
                int endRow = startRow + (diffCount > 0 ? diffCount - 1 : 0); // 結束行

                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(item.MainNumber);
                row.CreateCell(1).SetCellValue(string.Join("，", customer));
                row.CreateCell(2).SetCellValue(item.TotalCount);
                row.CreateCell(3).SetCellValue(item.ScanCount);
                row.CreateCell(4).SetCellValue(item.CheckCount);
                row.CreateCell(5).SetCellValue(item.CheckBagNumber);
                row.CreateCell(6).SetCellValue(item.DiffCount);

                foreach (var diff in item.DiffList)
                {
                    row = sheet.GetRow(irow) ?? sheet.CreateRow(irow);
                    row.CreateCell(7).SetCellValue(diff.BagNumber);
                    row.CreateCell(8).SetCellValue(diff.MergeNumber);
                    if (process.ContainsKey(diff.MergeNumber))
                    {
                        var processType = process[diff.MergeNumber];
                        row.CreateCell(9).SetCellValue(processTypeRemark[processType]);
                    }

                    irow++;
                }

                // 合併 A~G 欄 (0~6 欄)
                if (diffCount > 1)
                {
                    for (int col = 0; col <= 6; col++) // 0~6欄合併
                    {
                        var mergeRegion = new NPOI.SS.Util.CellRangeAddress(startRow, endRow, col, col);
                        sheet.AddMergedRegion(mergeRegion);
                    }
                }

                // 若 DiffList 為空，手動遞增 irow
                if (item.DiffList.Count == 0)
                {
                    irow++;
                }
            }
            return sheet;
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
            fontB.FontName = "微軟正黑體";
            fontB.FontHeightInPoints = 12;
            font1 = (XSSFFont)workbook.CreateFont();
            font1.FontName = "微軟正黑體";
            font1.FontHeightInPoints = 12;
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
            cs_Title_Left.SetFont(font1);

            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center.SetFont(font1);
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
            cs_Int.SetFont(font1);

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

        }
    }
}