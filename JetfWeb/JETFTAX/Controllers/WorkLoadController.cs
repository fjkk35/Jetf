using JETFTAX.Models;
using JETFTAX.Models.WorkLoad;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.WorkDay;
using Service.Services.WorkLoad;
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
    public class WorkLoadController : Controller
    {
        private readonly CargoService _cargoService;
        private readonly IncomeService _incomeService;
        private readonly WorkDayService _workDayService;
        private readonly WorkLoadService _workLoadService;

        public WorkLoadController(WorkDayService workDayService, WorkLoadService workLoadService, CargoService cargoService, IncomeService incomeService)
        {
            _workLoadService = workLoadService;
            _workDayService = workDayService;
            _cargoService = cargoService;
            _incomeService = incomeService;
        }

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent, cs_Percent2, dateStyle, date2Style;

        /// <summary>
        /// 海空快通關狀態彙總表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.ClearanceStatusReport)]
        public ActionResult CCStatusReport()
        {
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "海運", Value = "SEA" });
            sourceList.Add(new SelectListItem() { Text = "空運", Value = "ETL" });

            CCStatusReportViewModel vm = new CCStatusReportViewModel()
            {
                sDate = DateTime.Now.ToString("yyyy-MM-dd"),
                eDate = DateTime.Now.ToString("yyyy-MM-dd"),
                ddlSourceList = sourceList
            };

            return View(vm);
        }

        /// <summary>
        /// 海快通關狀態彙總表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.ClearanceStatusReport)]
        public ActionResult CCStatusReportExcel(CCStatusReportViewModel vm)
        {

            string fileName = "";
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                if (vm.source == "SEA")
                {
                    workbook = GetCCStatusSetReportWorkbook(vm.source, sDate, eDate);
                    fileName = $"{sDate}~{eDate}-海快通關狀態彙總表.xlsx";
                }
                else
                {
                    workbook = GetCCStatusEtlReportWorkbook(vm.source, sDate, eDate);
                    fileName = $"{sDate}~{eDate}-空快通關狀態彙總表.xlsx";
                }

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
        /// 海快通關狀態彙總表-Excel-海快
        /// </summary>
        /// <param name="original"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        IWorkbook GetCCStatusSetReportWorkbook(string original, string sDate, string eDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //總表
            DataTable dt_Report = _incomeService.IncomeDetailsReport(original, sDate, eDate).dt;
            //海快通關狀態彙總表
            GetCCStatusSetReportSheet(workbook, dt_Report, "海快通關狀態彙總表", sDate, eDate);

            return workbook;
        }

        /// <summary>
        /// 海快通關狀態彙總表-Excel-海快-頁籤-海快通關狀態彙總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetCCStatusSetReportSheet(IWorkbook workbook, DataTable dt_Report, string sheetName, string sDate, string eDate)
        {
            int rowCount, total_fee, total_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 10));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title_Left;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("通關主號數");
            row.CreateCell(3).SetCellValue("入倉件數\nA");
            row.CreateCell(4).SetCellValue("出倉件數\nB");
            row.CreateCell(5).SetCellValue("C3件數\nC");
            row.CreateCell(6).SetCellValue("留庫件數\nD=A-B");
            row.CreateCell(7).SetCellValue("查驗比率\n(C+D)/A");
            row.CreateCell(8).SetCellValue("入倉毛重");
            row.CreateCell(9).SetCellValue("袋數");
            row.CreateCell(10).SetCellValue("筆數");

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

            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 3500);
            sheet.SetColumnWidth(4, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);


            var dt_Group_Type = from t in dt_Report.AsEnumerable()
                                group t by new { DATADATE = t.Field<string>("DATADATE"), I_DATA_TYPE = t.Field<string>("I_DATA_TYPE") } into g
                                orderby g.Key.DATADATE
                                select new
                                {
                                    DATADATE = g.Key.DATADATE,
                                    I_DATA_TYPE = g.Key.I_DATA_TYPE,
                                    TOTAL_MAINNUMBER = g.Count(),
                                    TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                    TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                    TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                    TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                    TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                    TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                    TOTAL_CC = g.Sum(r => r.Field<decimal?>("TOTAL_CC")),
                                    TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                    TOTAL_TARIFF = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFF")),
                                    TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                    TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                    TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                    TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                                };

            var dt_Group_Day = from t in dt_Report.AsEnumerable()
                               group t by new { DATADATE = t.Field<string>("DATADATE") } into g
                               orderby g.Key.DATADATE
                               select new
                               {
                                   DATADATE = g.Key.DATADATE,
                                   I_DATA_TYPE = "日小計",
                                   TOTAL_MAINNUMBER = g.Count(),
                                   TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                   TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                   TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                   TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                   TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                   TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                   TOTAL_CC = g.Sum(r => r.Field<decimal?>("TOTAL_CC")),
                                   TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                   TOTAL_TARIFF = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFF")),
                                   TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                   TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                   TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                   TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                               };

            //for (int i = 0; i < dr.Length; i++)
            rowCount = 3;
            foreach (var item in dt_Group_Day)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_piece_c3);
                row.CreateCell(6).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(7).CellFormula = $"(F{rowCount + 1}+G{rowCount + 1})/D{rowCount + 1}";
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(9).SetCellValue(total_bag_number);
                row.CreateCell(10).SetCellValue(total_count);

                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Percent2;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;

                rowCount++;
            }

            foreach (var item in dt_Group_Type)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_piece_c3);
                row.CreateCell(6).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(7).CellFormula = $"(F{rowCount + 1}+G{rowCount + 1})/D{rowCount + 1}";
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(9).SetCellValue(total_bag_number);
                row.CreateCell(10).SetCellValue(total_count);

                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Percent2;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                rowCount++;
            }
        }

        /// <summary>
        /// 海快通關狀態彙總表-Excel-Workbook
        /// </summary>
        /// <param name="original"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        IWorkbook GetCCStatusEtlReportWorkbook(string original, string sDate, string eDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //總表
            DataTable dt_Report = _incomeService.IncomeDetailsReport(original, sDate, eDate).dt;
            //空快通關狀態彙總表
            GetCCStatusEtlReportSheet(workbook, dt_Report, "空快通關狀態彙總表", sDate, eDate);
            return workbook;
        }

        /// <summary>
        /// 海快通關狀態彙總表-Excel-Workbook-頁籤-空快通關狀態彙總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetCCStatusEtlReportSheet(IWorkbook workbook, DataTable dt_Report, string sheetName, string sDate, string eDate)
        {
            int rowCount, total_fee, total_bag_number, total_out_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 10));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title_Left;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("通關主號數");
            row.CreateCell(3).SetCellValue("入倉件數\nA");
            row.CreateCell(4).SetCellValue("出倉件數\nB");
            row.CreateCell(5).SetCellValue("入倉袋數\nC");
            row.CreateCell(6).SetCellValue("出倉袋數\nD");
            row.CreateCell(7).SetCellValue("查驗袋數\nE=C-D");
            row.CreateCell(8).SetCellValue("查驗比率\nE/A");
            row.CreateCell(9).SetCellValue("入倉毛重");
            row.CreateCell(10).SetCellValue("筆數");
            row.Height = 30 * 40;

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

            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 3500);
            sheet.SetColumnWidth(4, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);

            var dt_Group_Type = from t in dt_Report.AsEnumerable()
                                group t by new { DATADATE = t.Field<string>("DATADATE"), I_DATA_TYPE = t.Field<string>("I_DATA_TYPE") } into g
                                orderby g.Key.DATADATE
                                select new
                                {
                                    DATADATE = g.Key.DATADATE,
                                    I_DATA_TYPE = g.Key.I_DATA_TYPE,
                                    TOTAL_MAINNUMBER = g.Count(),
                                    TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                    TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                    TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                    TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                    TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                    TOTAL_OUT_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_OUT_BAG_NUMBER")),
                                    TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                    TOTAL_CC = g.Sum(r => r.Field<int?>("TOTAL_CC")),
                                    TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                    TOTAL_TARIFF = g.Sum(r => r.Field<int?>("TOTAL_TARIFF")),
                                    TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                    TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                    TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                    TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                                };

            var dt_Group_Day = from t in dt_Report.AsEnumerable()
                               group t by new { DATADATE = t.Field<string>("DATADATE") } into g
                               orderby g.Key.DATADATE
                               select new
                               {
                                   DATADATE = g.Key.DATADATE,
                                   I_DATA_TYPE = "日小計",
                                   TOTAL_MAINNUMBER = g.Count(),
                                   TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                   TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                   TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                   TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                   TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                   TOTAL_OUT_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_OUT_BAG_NUMBER")),
                                   TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                   TOTAL_CC = g.Sum(r => r.Field<int?>("TOTAL_CC")),
                                   TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                   TOTAL_TARIFF = g.Sum(r => r.Field<int?>("TOTAL_TARIFF")),
                                   TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                   TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                   TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                   TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                               };

            //for (int i = 0; i < dr.Length; i++)
            rowCount = 3;
            foreach (var item in dt_Group_Day)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_out_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_OUT_BAG_NUMBER.ToString(), out total_out_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_bag_number);
                row.CreateCell(6).SetCellValue(total_out_bag_number);
                row.CreateCell(7).CellFormula = $"F{rowCount + 1}-G{rowCount + 1}";
                row.CreateCell(8).CellFormula = $"H{rowCount + 1}/D{rowCount + 1}";
                row.CreateCell(9).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(10).SetCellValue(total_count);



                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Percent2;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                rowCount++;
            }

            foreach (var item in dt_Group_Type)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_out_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_OUT_BAG_NUMBER.ToString(), out total_out_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_bag_number);
                row.CreateCell(6).SetCellValue(total_out_bag_number);
                row.CreateCell(7).CellFormula = $"F{rowCount + 1}-G{rowCount + 1}";
                row.CreateCell(8).CellFormula = $"H{rowCount + 1}/D{rowCount + 1}";
                row.CreateCell(9).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(10).SetCellValue(total_count);

                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Percent2;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                rowCount++;
            }
        }

        /// <summary>
        /// 上傳空快錯單袋號
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.UploadEtlErrorBagNo)]
        public ActionResult UploadFileEtlBagNo()
        {
            return View();
        }

        /// <summary>
        /// 上傳空快錯單袋號
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.UploadEtlErrorBagNo)]
        public JsonResult UploadFileEtlBagNo(HttpPostedFileBase file)
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
                            resopnseModel = _workLoadService.UploadFileEtlBagNo(filePath, UserContextService.GetUserId());
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
        /// 上傳空快錯單-上傳檔案-下載Excel
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.UploadEtlErrorBagNo)]
        public ActionResult EtlBagNoExcel(string upload_time, string upload_ope)
        {
            string fileName = "";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetEtlBagNoWorkbook(upload_time, upload_ope);
                fileName = $"空快錯單袋號.xlsx";
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
        /// 上傳空快錯單-上傳檔案-下載Excel-Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        IWorkbook GetEtlBagNoWorkbook(string upload_time, string upload_ope)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得空快錯單袋號資料
            DataTable dt_Report = _workLoadService.GetEtlBagNo(upload_time, upload_ope).dt;
            //產生EXCEL
            GetEtlBagNoSheet(workbook, dt_Report, "空快錯單袋號");
            return workbook;
        }

        /// <summary>
        /// 上傳空快錯單-上傳檔案-下載Excel-Workbook-頁籤-空快錯單袋號
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetEtlBagNoSheet(IWorkbook workbook, DataTable dt_Report, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("袋號");
            row.CreateCell(1).SetCellValue("主號");
            row.CreateCell(2).SetCellValue("分提單號");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);

            for (int i = 0; i < dt_Report.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt_Report.Rows[i]["BAGNO"].ToString());
                row.CreateCell(1).SetCellValue(dt_Report.Rows[i]["MAINNUMBER"].ToString());
                row.CreateCell(2).SetCellValue(dt_Report.Rows[i]["TRACKINGNO"].ToString());
            }
        }

        /// <summary>
        /// 上傳海快錯單
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.UploadSeaErrorBagNo)]
        public ActionResult UploadFileSeaBagNo()
        {
            UploadFileSeaBagNoViewModel vm = new UploadFileSeaBagNoViewModel();
            vm.DataDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 上傳海快錯單-上傳檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [HttpPost]
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.UploadSeaErrorBagNo)]
        public JsonResult UploadFileSeaBagNo(HttpPostedFileBase file, UploadFileSeaBagNoViewModel vm)
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
                            resopnseModel = _workLoadService.UploadFileSeaBagNo(filePath, vm.DataDate, UserContextService.GetUserId());
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
        /// 上傳海快艙單號碼
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.UploadSeaManifest)]
        public ActionResult UploadFileSeaManifest()
        {
            UploadFileSeaManifestViewModel vm = new UploadFileSeaManifestViewModel();
            vm.DataDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 上傳海快艙單號碼-上傳檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [HttpPost]
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.UploadSeaManifest)]
        public JsonResult UploadFileSeaManifest(HttpPostedFileBase file, UploadFileSeaManifestViewModel vm)
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
                            resopnseModel = _workLoadService.UploadFileSeaManifest(filePath, vm.DataDate, UserContextService.GetUserId());
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
        /// 海快錯單作業
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.SeaErrorWorkLoad)]
        public ActionResult SeaBagNoWork()
        {
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "日期", Value = "日期" });
            sourceList.Add(new SelectListItem() { Text = "上傳時間", Value = "上傳時間" });

            SeaBagNoWorkViewModel vm = new SeaBagNoWorkViewModel()
            {
                sDate = DateTime.Now.ToString("yyyy-MM-dd") + " 00:00",
                eDate = DateTime.Now.ToString("yyyy-MM-dd") + " 23:59",
                ddlSourceList = sourceList
            };
            return View(vm);
        }

        /// <summary>
        /// 海快錯單作業-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.SeaErrorWorkLoad)]
        public ActionResult SeaBagNoWorkExcel(SeaBagNoWorkViewModel vm)
        {
            string fileName = "";
            string sDate;
            string eDate;
            string source = vm.source;
            if (source == "日期")
            {
                sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
                eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            }
            else
            {
                sDate = vm.sDate + " :00";
                eDate = vm.eDate + " :59";
            }
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = _workLoadService.GetSeaBagNoWorkWorkbook(source, sDate, eDate);
                if (workbook.NumberOfSheets == 0)
                {
                    workbook.CreateSheet("工作表1");
                }
                fileName = $"{sDate}~{eDate}-海快錯單作業.xlsx";
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
        /// 海快錯單作業-Excel-預委任
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.SeaErrorWorkLoad)]
        public ActionResult SeaBagNoWorkAppointExcel(SeaBagNoWorkViewModel vm)
        {
            string fileName = "";
            string sDate;
            string eDate;
            string source = vm.source;
            if (source == "日期")
            {
                sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
                eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            }
            else
            {
                sDate = vm.sDate + " :00";
                eDate = vm.eDate + " :59";
            }
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = _workLoadService.GetSeaBagNoWorkAppointWorkbook(source, sDate, eDate);
                if (workbook.NumberOfSheets == 0)
                {
                    workbook.CreateSheet("工作表1");
                }
                fileName = $"{sDate}~{eDate}-海快預委任.xlsx";
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
        /// 海快錯單作業-Excel-具結
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.SeaErrorWorkLoad)]
        public ActionResult SeaBagNoWorkBindOverExcel(SeaBagNoWorkViewModel vm)
        {
            string fileName = "";
            string sDate;
            string eDate;
            string source = vm.source;
            if (source == "日期")
            {
                sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
                eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            }
            else
            {
                sDate = vm.sDate + " :00";
                eDate = vm.eDate + " :59";
            }

            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = _workLoadService.GetSeaBagNoWorkBindOverWorkbook(source, sDate, eDate);
                if (workbook.NumberOfSheets == 0)
                {
                    workbook.CreateSheet("工作表1");
                }
                fileName = $"{sDate}~{eDate}-海快具結.xlsx";
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

            cs_Percent = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.DataFormat = format.GetFormat("0.00%");
            cs_Percent.SetFont(font1);

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
