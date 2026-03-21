using JETFTAX.Models;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Models.IncomeReport;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class IncomeController : Controller
    {
        GlobalService globalService = new GlobalService();
        IncomeService incomeService = new IncomeService();

        IFont fontB, font2;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Right, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent, cs_Percent2;

        /// <summary>
        /// 營收報表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.IncomeReport)]
        public ActionResult IncomeReport()
        {
            IncomeReportViewModel vm = new IncomeReportViewModel()
            {
                sDate = DateTime.Now.ToString("yyyy-MM-dd"),
                eDate = DateTime.Now.ToString("yyyy-MM-dd")
            };
            return View(vm);
        }

        /// <summary>
        /// 營收報表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.IncomeReport)]
        public ActionResult IncomeReportExcel(IncomeReportViewModel vm)
        {
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");

            string msg = "";
            string handle = Guid.NewGuid().ToString();
            string fileName = "";

            fileName = $"{sDate}~{eDate}-營收報表.xlsx";

            try
            {
                if (vm.rdoSearchType == "Yes")
                {
                    //重新轉檔
                    incomeService.Insert_Income_Report(vm.sDate, vm.eDate);
                }

                //日統計表營收去年比報表
                var sDateTime = DateTime.ParseExact(sDate, "yyyyMMdd", null);
                var eDateTime = DateTime.ParseExact(eDate, "yyyyMMdd", null);
                //去年開始日期
                var lastSDate = new DateTime(sDateTime.Year, sDateTime.Month, 1).AddYears(-1);
                //去年結束日期
                var lastEDate = lastSDate.AddMonths(+1).AddDays(-1);
                var lastList = incomeService.IncomeReportCustomerRate(lastSDate.ToString("yyyyMMdd"), lastEDate.ToString("yyyyMMdd"));
                var list = incomeService.IncomeReportCustomerRate(sDate, eDate);

                IWorkbook workbook = new XSSFWorkbook();
                //日倉儲營收
                GetIncomeReportDaySheet(workbook, sDate, eDate);
                //營收日統計表
                GetIncomeReportDay2Sheet(workbook, sDate, eDate);
                //空快日統計表營收去年比報表
                GetIncomeReportDay2RateSheet(workbook, "空快" , sDate, eDate, lastList.Item1, list.Item1);
                //海快日統計表營收去年比報表
                GetIncomeReportDay2RateSheet(workbook, "海快", sDate, eDate, lastList.Item2, list.Item2);
                //明細
                GetIncomeReportDetailsSheet(workbook, sDate, eDate);

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
        /// 營收報表-Excel-頁籤-營收明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeReportDetailsSheet(IWorkbook workbook, string sDate, string eDate)
        {
            DataTableModel dataTableModel = incomeService.IncomeReport_Details(sDate, eDate);
            DataTable dt = dataTableModel.dt;

            int total_fee, total_tax, total_ccfee, total_bag_number, total_count;
            double cc, tariff, total_cc, total_fee2, total_gw;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet("明細");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("作業日");
            row.CreateCell(1).SetCellValue("分類");
            row.CreateCell(2).SetCellValue("倉儲");
            row.CreateCell(3).SetCellValue("客戶代號");
            row.CreateCell(4).SetCellValue("客戶中文");
            row.CreateCell(5).SetCellValue("派件公司代號");
            row.CreateCell(6).SetCellValue("派件公司");
            row.CreateCell(7).SetCellValue("包稅不包稅");
            row.CreateCell(8).SetCellValue("應收關稅單價");
            row.CreateCell(9).SetCellValue("清關單價(未稅)");
            row.CreateCell(10).SetCellValue("清關收入(未稅)");
            row.CreateCell(11).SetCellValue("應收手續費(含稅)");
            row.CreateCell(12).SetCellValue("應收手續費(未稅)");
            row.CreateCell(13).SetCellValue("應付稅金");
            row.CreateCell(14).SetCellValue("應付報關費");
            row.CreateCell(15).SetCellValue("重量");
            row.CreateCell(16).SetCellValue("袋數");
            row.CreateCell(17).SetCellValue("筆數");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 6000);
            sheet.SetColumnWidth(10, 6000);
            sheet.SetColumnWidth(11, 6000);
            sheet.SetColumnWidth(12, 6000);
            sheet.SetColumnWidth(13, 6000);
            sheet.SetColumnWidth(14, 6000);
            sheet.SetColumnWidth(15, 6000);
            sheet.SetColumnWidth(16, 6000);
            sheet.SetColumnWidth(17, 6000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                total_fee = 0;
                total_tax = 0;
                total_ccfee = 0;
                total_bag_number = 0;
                total_count = 0;

                tariff = 0;
                cc = 0;
                total_cc = 0;
                total_fee2 = 0;
                total_gw = 0;

                int.TryParse(dt.Rows[i]["TOTAL_FEE"].ToString(), out total_fee);
                int.TryParse(dt.Rows[i]["TOTAL_TAX"].ToString(), out total_tax);
                int.TryParse(dt.Rows[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dt.Rows[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dt.Rows[i]["TOTAL_COUNT"].ToString(), out total_count);

                double.TryParse(dt.Rows[i]["TARIFF"].ToString(), out tariff);
                double.TryParse(dt.Rows[i]["CC"].ToString(), out cc);
                double.TryParse(dt.Rows[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dt.Rows[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                double.TryParse(dt.Rows[i]["TOTAL_GW"].ToString(), out total_gw);

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dt.Rows[i]["TRAN_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["DATA_TYPE"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["DESPATCH_NO"].ToString());
                row.CreateCell(4).SetCellValue(dt.Rows[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["TRANS_NO"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["TRANS_NAME"].ToString());
                row.CreateCell(7).SetCellValue(dt.Rows[i]["INCLUDE_TAX"].ToString());
                row.CreateCell(8).SetCellValue(tariff);
                row.CreateCell(9).SetCellValue(cc);
                row.CreateCell(10).SetCellValue(total_cc);
                row.CreateCell(11).SetCellValue(total_fee);
                row.CreateCell(12).SetCellValue(total_fee2);
                row.CreateCell(13).SetCellValue(total_tax);
                row.CreateCell(14).SetCellValue(total_ccfee);
                row.CreateCell(15).SetCellValue(total_gw);
                row.CreateCell(16).SetCellValue(total_bag_number);
                row.CreateCell(17).SetCellValue(total_count);
            }
        }

        /// <summary>
        /// 營收報表-Excel-頁籤-日倉儲營收
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeReportDaySheet(IWorkbook workbook, string sDate, string eDate)
        {
            DataTableModel dataTableModel = incomeService.IncomeReport_Day(sDate, eDate);
            DataTable dt = dataTableModel.dt;

            int rowCount = 0, subCount = 0, total_fee2, total_bag_number, total_count, total_fee2Add, total_bag_numberAdd, total_countAdd, total_tax_N, total_tax_Y, total_tax_Nadd, total_tax_Yadd, total_ccfee, total_ccfeeadd, total_diff, total_diffadd, total_income, total_incomeadd;
            double total_cc, total_gw, total_ccAdd, total_gwAdd, total_tariff, total_tariffadd;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet("日倉儲營收");
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 19));
            row.CreateCell(0).SetCellValue(eDate + "海空快日倉儲營收報表(未稅)");
            row.GetCell(0).CellStyle = cs_Title;

            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("分類");
            row.GetCell(0).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 0, 0));
            row.CreateCell(1).SetCellValue("倉儲");
            row.GetCell(1).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 1, 1));
            row.CreateCell(2).SetCellValue($"當日({eDate})");
            row.GetCell(2).CellStyle = cs_Center_Blue;
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 10));
            row.CreateCell(11).SetCellValue($"累計({sDate}－{eDate})");
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 11, 19));
            row.GetCell(11).CellStyle = cs_Center_Blue;

            row = sheet.CreateRow(3);
            row.CreateCell(2).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 2, 2));
            row.CreateCell(3).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 3, 3));
            row.CreateCell(4).SetCellValue("包稅稅金差額收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 4, 6));

            row.CreateCell(7).SetCellValue("營收小計");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 7, 7));
            row.CreateCell(8).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 8, 8));
            row.CreateCell(9).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 9, 9));
            row.CreateCell(10).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 10, 10));
            row.CreateCell(11).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 11, 11));
            row.CreateCell(12).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 12, 12));

            row.CreateCell(13).SetCellValue("包稅稅金差額收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 13, 15));

            row.CreateCell(16).SetCellValue("營收小計");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 16, 16));
            row.CreateCell(17).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 17, 17));
            row.CreateCell(18).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 18, 18));
            row.CreateCell(19).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 19, 19));

            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center_Blue;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center_Blue;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;

            row = sheet.CreateRow(4);
            row.CreateCell(4).SetCellValue("應收關稅");
            row.CreateCell(5).SetCellValue("應付稅金");
            row.CreateCell(6).SetCellValue("差額");

            row.CreateCell(13).SetCellValue("應收關稅");
            row.CreateCell(14).SetCellValue("應付稅金");
            row.CreateCell(15).SetCellValue("差額");


            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;


            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);

            var dt_Group = from t in dt.AsEnumerable()
                           group t by new { TRAN_TYPE = t.Field<string>("TRAN_TYPE") } into g
                           orderby g.Key.TRAN_TYPE
                           select new
                           {
                               TRAN_TYPE = g.Key.TRAN_TYPE,
                               DATA_TYPE = "小計",
                               TOTAL_CC = g.Sum(r => r.Field<decimal?>("TOTAL_CC")),
                               TOTAL_FEE2 = g.Sum(r => r.Field<int?>("TOTAL_FEE2")),
                               TOTAL_TARIFF = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFF")),
                               TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                               TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                               TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                               TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                               TOTAL_CCAdd = g.Sum(r => r.Field<decimal?>("TOTAL_CCAdd")),
                               TOTAL_FEE2Add = g.Sum(r => r.Field<int?>("TOTAL_FEE2Add")),
                               TOTAL_TARIFFAdd = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFFAdd")),
                               TOTAL_TAX_YAdd = g.Sum(r => r.Field<int?>("TOTAL_TAX_YAdd")),
                               TOTAL_GWAdd = g.Sum(r => r.Field<decimal?>("TOTAL_GWAdd")),
                               TOTAL_BAG_NUMBERAdd = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBERAdd")),
                               TOTAL_COUNTAdd = g.Sum(r => r.Field<int?>("TOTAL_COUNTAdd")),
                           };

            rowCount = 5;
            //小計
            foreach (var item in dt_Group)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_fee2Add = 0;
                total_bag_numberAdd = 0;
                total_countAdd = 0;
                total_tax_N = 0;
                total_tax_Nadd = 0;
                total_tax_Y = 0;
                total_tax_Yadd = 0;
                total_ccfee = 0;
                total_ccfeeadd = 0;

                total_cc = 0;
                total_gw = 0;
                total_ccAdd = 0;
                total_gwAdd = 0;
                total_tariff = 0;
                total_tariffadd = 0;

                int.TryParse(item.TOTAL_FEE2.ToString(), out total_fee2);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_FEE2Add.ToString(), out total_fee2Add);
                int.TryParse(item.TOTAL_BAG_NUMBERAdd.ToString(), out total_bag_numberAdd);
                int.TryParse(item.TOTAL_COUNTAdd.ToString(), out total_countAdd);

                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_YAdd.ToString(), out total_tax_Yadd);
                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                double.TryParse(item.TOTAL_CCAdd.ToString(), out total_ccAdd);
                double.TryParse(item.TOTAL_GWAdd.ToString(), out total_gwAdd);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);
                double.TryParse(item.TOTAL_TARIFFAdd.ToString(), out total_tariffadd);

                //差額
                total_diff = Convert.ToInt32(Math.Ceiling(total_tariff)) - total_tax_Y;
                total_diffadd = Convert.ToInt32(Math.Ceiling(total_tariffadd)) - total_tax_Yadd;
                //營收小計
                total_income = Convert.ToInt32(Math.Ceiling(total_cc)) + total_fee2 + total_diff;
                total_incomeadd = Convert.ToInt32(Math.Ceiling(total_ccAdd)) + total_fee2Add + total_diffadd;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.TRAN_TYPE.ToString()); //分類
                row.CreateCell(1).SetCellValue(item.DATA_TYPE.ToString()); //倉儲
                row.CreateCell(2).SetCellValue(Math.Ceiling(total_cc));  //清關收入
                row.CreateCell(3).SetCellValue(total_fee2);//手續費
                row.CreateCell(4).SetCellValue(Math.Ceiling(total_tariff));//應收
                row.CreateCell(5).SetCellValue(total_tax_Y); //包稅應付稅金
                row.CreateCell(6).SetCellValue(total_diff); //差額
                row.CreateCell(7).SetCellValue(total_income); //營收小計
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw)); //重量
                row.CreateCell(9).SetCellValue(total_bag_number); //袋數
                row.CreateCell(10).SetCellValue(total_count); //筆數
                row.CreateCell(11).SetCellValue(Math.Ceiling(total_ccAdd));//清關收入
                row.CreateCell(12).SetCellValue(total_fee2Add);//手續費
                row.CreateCell(13).SetCellValue(Math.Ceiling(total_tariffadd));//應收
                row.CreateCell(14).SetCellValue(total_tax_Yadd);//包稅應付稅金
                row.CreateCell(15).SetCellValue(total_diffadd);//差額
                row.CreateCell(16).SetCellValue(total_incomeadd);//營收小計
                row.CreateCell(17).SetCellValue(Math.Ceiling(total_gwAdd));//重量
                row.CreateCell(18).SetCellValue(total_bag_numberAdd);//袋數
                row.CreateCell(19).SetCellValue(total_countAdd);//筆數

                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int_Blue;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int_Blue;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;

                rowCount++;

                subCount++;
            }
            //合計
            row = sheet.CreateRow(rowCount);
            row.CreateCell(1).SetCellValue("合計"); //合計
            row.CreateCell(2).CellFormula = $"SUM(C6:C{6 + subCount - 1})";
            row.CreateCell(3).CellFormula = $"SUM(D6:D{6 + subCount - 1})";
            row.CreateCell(4).CellFormula = $"SUM(E6:E{6 + subCount - 1})";
            row.CreateCell(5).CellFormula = $"SUM(F6:F{6 + subCount - 1})";
            row.CreateCell(6).CellFormula = $"SUM(G6:G{6 + subCount - 1})";
            row.CreateCell(7).CellFormula = $"SUM(H6:H{6 + subCount - 1})";
            row.CreateCell(8).CellFormula = $"SUM(I6:I{6 + subCount - 1})";
            row.CreateCell(9).CellFormula = $"SUM(J6:J{6 + subCount - 1})";
            row.CreateCell(10).CellFormula = $"SUM(K6:K{6 + subCount - 1})";
            row.CreateCell(11).CellFormula = $"SUM(L6:L{6 + subCount - 1})";
            row.CreateCell(12).CellFormula = $"SUM(M6:M{6 + subCount - 1})";
            row.CreateCell(13).CellFormula = $"SUM(N6:N{6 + subCount - 1})";
            row.CreateCell(14).CellFormula = $"SUM(O6:O{6 + subCount - 1})";
            row.CreateCell(15).CellFormula = $"SUM(P6:P{6 + subCount - 1})";
            row.CreateCell(16).CellFormula = $"SUM(Q6:Q{6 + subCount - 1})";
            row.CreateCell(17).CellFormula = $"SUM(R6:R{6 + subCount - 1})";
            row.CreateCell(18).CellFormula = $"SUM(S6:S{6 + subCount - 1})";
            row.CreateCell(19).CellFormula = $"SUM(T6:T{6 + subCount - 1})";

            row.GetCell(1).CellStyle = cs_Center_Blue;
            row.GetCell(2).CellStyle = cs_Int_Blue;
            row.GetCell(3).CellStyle = cs_Int_Blue;
            row.GetCell(4).CellStyle = cs_Int_Blue;
            row.GetCell(5).CellStyle = cs_Int_Blue;
            row.GetCell(6).CellStyle = cs_Int_Blue;
            row.GetCell(7).CellStyle = cs_Int_Blue;
            row.GetCell(8).CellStyle = cs_Int_Blue;
            row.GetCell(9).CellStyle = cs_Int_Blue;
            row.GetCell(10).CellStyle = cs_Int_Blue;
            row.GetCell(11).CellStyle = cs_Int_Blue;
            row.GetCell(12).CellStyle = cs_Int_Blue;
            row.GetCell(13).CellStyle = cs_Int_Blue;
            row.GetCell(14).CellStyle = cs_Int_Blue;
            row.GetCell(15).CellStyle = cs_Int_Blue;
            row.GetCell(16).CellStyle = cs_Int_Blue;
            row.GetCell(17).CellStyle = cs_Int_Blue;
            row.GetCell(18).CellStyle = cs_Int_Blue;
            row.GetCell(19).CellStyle = cs_Int_Blue;
            rowCount++;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_fee2Add = 0;
                total_bag_numberAdd = 0;
                total_countAdd = 0;
                total_tax_N = 0;
                total_tax_Nadd = 0;
                total_tax_Y = 0;
                total_tax_Yadd = 0;
                total_ccfee = 0;
                total_ccfeeadd = 0;


                total_cc = 0;
                total_gw = 0;
                total_ccAdd = 0;
                total_gwAdd = 0;
                total_tariff = 0;
                total_tariffadd = 0;

                int.TryParse(dt.Rows[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dt.Rows[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dt.Rows[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dt.Rows[i]["TOTAL_FEE2Add"].ToString(), out total_fee2Add);
                int.TryParse(dt.Rows[i]["TOTAL_BAG_NUMBERAdd"].ToString(), out total_bag_numberAdd);
                int.TryParse(dt.Rows[i]["TOTAL_COUNTAdd"].ToString(), out total_countAdd);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_NADD"].ToString(), out total_tax_Nadd);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_YADD"].ToString(), out total_tax_Yadd);
                int.TryParse(dt.Rows[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dt.Rows[i]["TOTAL_CCFEEADD"].ToString(), out total_ccfeeadd);


                double.TryParse(dt.Rows[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dt.Rows[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dt.Rows[i]["TOTAL_CCAdd"].ToString(), out total_ccAdd);
                double.TryParse(dt.Rows[i]["TOTAL_GWAdd"].ToString(), out total_gwAdd);
                double.TryParse(dt.Rows[i]["TOTAL_TARIFF"].ToString(), out total_tariff);
                double.TryParse(dt.Rows[i]["TOTAL_TARIFFADD"].ToString(), out total_tariffadd);

                //差額
                total_diff = Convert.ToInt32(Math.Ceiling(total_tariff)) - total_tax_Y;
                total_diffadd = Convert.ToInt32(Math.Ceiling(total_tariffadd)) - total_tax_Yadd;
                //營收小計
                total_income = Convert.ToInt32(Math.Ceiling(total_cc)) + total_fee2 + total_diff;
                total_incomeadd = Convert.ToInt32(Math.Ceiling(total_ccAdd)) + total_fee2Add + total_diffadd;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["TRAN_TYPE"].ToString()); //分類
                row.CreateCell(1).SetCellValue(dt.Rows[i]["DATA_TYPE"].ToString()); //倉儲
                row.CreateCell(2).SetCellValue(Math.Ceiling(total_cc));  //清關收入
                row.CreateCell(3).SetCellValue(total_fee2);//手續費
                row.CreateCell(4).SetCellValue(Math.Ceiling(total_tariff));//應收
                row.CreateCell(5).SetCellValue(total_tax_Y); //包稅應付稅金
                row.CreateCell(6).SetCellValue(total_diff); //差額
                row.CreateCell(7).SetCellValue(total_income); //營收小計
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw)); //重量
                row.CreateCell(9).SetCellValue(total_bag_number); //袋數
                row.CreateCell(10).SetCellValue(total_count); //筆數
                row.CreateCell(11).SetCellValue(Math.Ceiling(total_ccAdd));//清關收入
                row.CreateCell(12).SetCellValue(total_fee2Add);//手續費
                row.CreateCell(13).SetCellValue(Math.Ceiling(total_tariffadd));//應收
                row.CreateCell(14).SetCellValue(total_tax_Yadd);//包稅應付稅金
                row.CreateCell(15).SetCellValue(total_diffadd);//差額
                row.CreateCell(16).SetCellValue(total_incomeadd);//營收小計
                row.CreateCell(17).SetCellValue(Math.Ceiling(total_gwAdd));//重量
                row.CreateCell(18).SetCellValue(total_bag_numberAdd);//袋數
                row.CreateCell(19).SetCellValue(total_countAdd);//筆數

                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int_Blue;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int_Blue;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;

                rowCount++;
            }
        }

        /// <summary>
        /// 營收報表-Excel-頁籤-日統計報表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeReportDay2Sheet(IWorkbook workbook, string sDate, string eDate)
        {
            DataTableModel dataTableModel = incomeService.IncomeReport_Day2(sDate, eDate);
            DataTable dt = dataTableModel.dt;

            int rowCount = 0, total_fee2, total_bag_number, total_count, total_fee2Add, total_bag_numberAdd, total_countAdd, total_tax_N, total_tax_Y, total_tax_Nadd, total_tax_Yadd, total_ccfee, total_ccfeeadd, total_diff, total_diffadd, total_income, total_incomeadd;
            double total_cc, total_gw, total_ccAdd, total_gwAdd, total_tariff, total_tariffadd;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet("日統計報表");
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 20));
            row.CreateCell(0).SetCellValue(eDate + "海空快營收日統計表(未稅)");
            row.GetCell(0).CellStyle = cs_Title;

            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("分類");
            row.GetCell(0).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 0, 0));
            row.CreateCell(1).SetCellValue("倉儲");
            row.GetCell(1).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 1, 1));
            row.CreateCell(2).SetCellValue("客戶");
            row.GetCell(2).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 2, 2));
            row.CreateCell(3).SetCellValue($"當日({eDate})");
            row.GetCell(3).CellStyle = cs_Center_Blue;
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 3, 10));
            row.CreateCell(12).SetCellValue($"累計({sDate}－{eDate})");
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 11, 19));
            row.GetCell(12).CellStyle = cs_Center_Blue;

            row = sheet.CreateRow(3);
            row.CreateCell(3).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 3, 3));
            row.CreateCell(4).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 4, 4));
            row.CreateCell(5).SetCellValue("包稅稅金差額收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 5, 7));

            row.CreateCell(8).SetCellValue("營收小計");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 8, 8));
            row.CreateCell(9).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 9, 9));
            row.CreateCell(10).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 10, 10));
            row.CreateCell(11).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 11, 11));
            row.CreateCell(12).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 12, 12));
            row.CreateCell(13).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 13, 13));

            row.CreateCell(14).SetCellValue("包稅稅金差額收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 14, 16));

            row.CreateCell(17).SetCellValue("營收小計");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 17, 17));
            row.CreateCell(18).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 18, 18));
            row.CreateCell(19).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 19, 19));
            row.CreateCell(20).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 20, 20));

            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center_Blue;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center_Blue;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;

            row = sheet.CreateRow(4);
            row.CreateCell(5).SetCellValue("應收關稅");
            row.CreateCell(6).SetCellValue("應付稅金");
            row.CreateCell(7).SetCellValue("差額");

            row.CreateCell(14).SetCellValue("應收關稅");
            row.CreateCell(15).SetCellValue("應付稅金");
            row.CreateCell(16).SetCellValue("差額");

            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;


            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 7000);
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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_fee2Add = 0;
                total_bag_numberAdd = 0;
                total_countAdd = 0;
                total_tax_N = 0;
                total_tax_Nadd = 0;
                total_tax_Y = 0;
                total_tax_Yadd = 0;
                total_ccfee = 0;
                total_ccfeeadd = 0;

                total_cc = 0;
                total_gw = 0;
                total_ccAdd = 0;
                total_gwAdd = 0;
                total_tariff = 0;
                total_tariffadd = 0;

                int.TryParse(dt.Rows[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dt.Rows[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dt.Rows[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dt.Rows[i]["TOTAL_FEE2Add"].ToString(), out total_fee2Add);
                int.TryParse(dt.Rows[i]["TOTAL_BAG_NUMBERAdd"].ToString(), out total_bag_numberAdd);
                int.TryParse(dt.Rows[i]["TOTAL_COUNTAdd"].ToString(), out total_countAdd);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_NADD"].ToString(), out total_tax_Nadd);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_YADD"].ToString(), out total_tax_Yadd);
                int.TryParse(dt.Rows[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dt.Rows[i]["TOTAL_CCFEEADD"].ToString(), out total_ccfeeadd);


                double.TryParse(dt.Rows[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dt.Rows[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dt.Rows[i]["TOTAL_CCAdd"].ToString(), out total_ccAdd);
                double.TryParse(dt.Rows[i]["TOTAL_GWAdd"].ToString(), out total_gwAdd);
                double.TryParse(dt.Rows[i]["TOTAL_TARIFF"].ToString(), out total_tariff);
                double.TryParse(dt.Rows[i]["TOTAL_TARIFFADD"].ToString(), out total_tariffadd);

                //差額
                total_diff = Convert.ToInt32(Math.Ceiling(total_tariff)) - total_tax_Y;
                total_diffadd = Convert.ToInt32(Math.Ceiling(total_tariffadd)) - total_tax_Yadd;
                //營收小計
                total_income = Convert.ToInt32(Math.Ceiling(total_cc)) + total_fee2 + total_diff;
                total_incomeadd = Convert.ToInt32(Math.Ceiling(total_ccAdd)) + total_fee2Add + total_diffadd;

                rowCount = i + 5;
                row = sheet.CreateRow(i + 5);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["TRAN_TYPE"].ToString()); //分類
                row.CreateCell(1).SetCellValue(dt.Rows[i]["DATA_TYPE"].ToString()); //倉儲
                row.CreateCell(2).SetCellValue(dt.Rows[i]["DESPATCH_NAME"].ToString());//客戶
                row.CreateCell(3).SetCellValue(Math.Ceiling(total_cc));  //清關收入
                row.CreateCell(4).SetCellValue(total_fee2);//手續費
                row.CreateCell(5).SetCellValue(Math.Ceiling(total_tariff));//應收
                row.CreateCell(6).SetCellValue(total_tax_Y); //包稅應付稅金
                row.CreateCell(7).SetCellValue(total_diff); //差額
                row.CreateCell(8).SetCellValue(total_income); //營收小計
                row.CreateCell(9).SetCellValue(Math.Ceiling(total_gw)); //重量
                row.CreateCell(10).SetCellValue(total_bag_number); //袋數
                row.CreateCell(11).SetCellValue(total_count); //筆數

                row.CreateCell(12).SetCellValue(Math.Ceiling(total_ccAdd));//清關收入
                row.CreateCell(13).SetCellValue(total_fee2Add);//手續費
                row.CreateCell(14).SetCellValue(Math.Ceiling(total_tariffadd));//應收
                row.CreateCell(15).SetCellValue(total_tax_Yadd);//包稅應付稅金
                row.CreateCell(16).SetCellValue(total_diffadd);//差額
                row.CreateCell(17).SetCellValue(total_incomeadd);//營收小計
                row.CreateCell(18).SetCellValue(Math.Ceiling(total_gwAdd));//重量
                row.CreateCell(19).SetCellValue(total_bag_numberAdd);//袋數
                row.CreateCell(20).SetCellValue(total_countAdd);//筆數

                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int_Blue;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int_Blue;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;
            }

            row = sheet.CreateRow(rowCount + 1);
            row.CreateCell(2).SetCellValue("合計");//合計
            row.CreateCell(3).CellFormula = $"SUM(D6:D{rowCount + 1})";
            row.CreateCell(4).CellFormula = $"SUM(E6:E{rowCount + 1})";
            row.CreateCell(5).CellFormula = $"SUM(F6:F{rowCount + 1})";
            row.CreateCell(6).CellFormula = $"SUM(G6:G{rowCount + 1})";
            row.CreateCell(7).CellFormula = $"SUM(H6:H{rowCount + 1})";
            row.CreateCell(8).CellFormula = $"SUM(I6:I{rowCount + 1})";
            row.CreateCell(9).CellFormula = $"SUM(J6:J{rowCount + 1})";
            row.CreateCell(10).CellFormula = $"SUM(K6:K{rowCount + 1})";
            row.CreateCell(11).CellFormula = $"SUM(L6:L{rowCount + 1})";
            row.CreateCell(12).CellFormula = $"SUM(M6:M{rowCount + 1})";
            row.CreateCell(13).CellFormula = $"SUM(N6:N{rowCount + 1})";
            row.CreateCell(14).CellFormula = $"SUM(O6:O{rowCount + 1})";
            row.CreateCell(15).CellFormula = $"SUM(P6:P{rowCount + 1})";
            row.CreateCell(16).CellFormula = $"SUM(Q6:Q{rowCount + 1})";
            row.CreateCell(17).CellFormula = $"SUM(R6:R{rowCount + 1})";
            row.CreateCell(18).CellFormula = $"SUM(S6:S{rowCount + 1})";
            row.CreateCell(19).CellFormula = $"SUM(T6:T{rowCount + 1})";
            row.CreateCell(20).CellFormula = $"SUM(U6:U{rowCount + 1})";

            row.GetCell(2).CellStyle = cs_Int;
            row.GetCell(3).CellStyle = cs_Int;
            row.GetCell(4).CellStyle = cs_Int;
            row.GetCell(5).CellStyle = cs_Int;
            row.GetCell(6).CellStyle = cs_Int;
            row.GetCell(7).CellStyle = cs_Int;
            row.GetCell(8).CellStyle = cs_Int_Blue;
            row.GetCell(9).CellStyle = cs_Int;
            row.GetCell(10).CellStyle = cs_Int;
            row.GetCell(11).CellStyle = cs_Int;
            row.GetCell(12).CellStyle = cs_Int;
            row.GetCell(13).CellStyle = cs_Int;
            row.GetCell(14).CellStyle = cs_Int;
            row.GetCell(15).CellStyle = cs_Int;
            row.GetCell(16).CellStyle = cs_Int;
            row.GetCell(17).CellStyle = cs_Int_Blue;
            row.GetCell(18).CellStyle = cs_Int;
            row.GetCell(19).CellStyle = cs_Int;
            row.GetCell(20).CellStyle = cs_Int;
        }

        /// <summary>
        /// 營收報表-Excel-去年營收比
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <param name="titleName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeReportDay2RateSheet(IWorkbook workbook,string tranType, string sDate, string eDate, List<IncomeReportCustomerRateModel> lastList, List<IncomeReportCustomerRateModel> list)
        {
            var sDateTime = DateTime.ParseExact(sDate, "yyyyMMdd", null);
            var eDateTime = DateTime.ParseExact(eDate, "yyyyMMdd", null);
            //去年開始日期
            var lastSDate = new DateTime(sDateTime.Year, sDateTime.Month, 1).AddYears(-1);
            //去年結束日期
            var lastEDate = lastSDate.AddMonths(+1).AddDays(-1);
            //天數
            var totalDays = Convert.ToInt32((eDateTime - sDateTime).TotalDays) + 1;

            string sheetName = $"{tranType}去年比";
            string titleName = $"{tranType}日統計營收去年比報表(未稅)";

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 19));
            row.CreateCell(0).SetCellValue(eDate + titleName);
            row.GetCell(0).CellStyle = cs_Title;

            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("分類");
            row.GetCell(0).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 0, 0));

            row.CreateCell(1).SetCellValue("客戶");
            row.GetCell(1).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 1, 1));

            row.CreateCell(2).SetCellValue($"當日({eDate})");
            row.GetCell(2).CellStyle = cs_Center_Blue;
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 10));

            row.CreateCell(11).SetCellValue($"累計({sDate}－{eDate})");
            row.GetCell(11).CellStyle = cs_Center_Blue;
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 11, 19));

            row = sheet.CreateRow(3);
            row.CreateCell(2).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 2, 4));

            row.CreateCell(5).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 5, 7));

            row.CreateCell(8).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 8, 10));

            row.CreateCell(11).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 11, 13));

            row.CreateCell(14).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 14, 16));

            row.CreateCell(17).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 17, 19));

            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;

            row = sheet.CreateRow(4);
            //清關收入
            row.CreateCell(2).SetCellValue("今年");
            row.CreateCell(3).SetCellValue("去年");
            row.CreateCell(4).SetCellValue("去年比");

            //手續費
            row.CreateCell(5).SetCellValue("今年");
            row.CreateCell(6).SetCellValue("去年");
            row.CreateCell(7).SetCellValue("去年比");

            //重量
            row.CreateCell(8).SetCellValue("今年");
            row.CreateCell(9).SetCellValue("去年");
            row.CreateCell(10).SetCellValue("去年比");

            //清關收入
            row.CreateCell(11).SetCellValue("今年");
            row.CreateCell(12).SetCellValue("去年");
            row.CreateCell(13).SetCellValue("去年比");

            //手續費
            row.CreateCell(14).SetCellValue("今年");
            row.CreateCell(15).SetCellValue("去年");
            row.CreateCell(16).SetCellValue("去年比");

            //重量
            row.CreateCell(17).SetCellValue("今年");
            row.CreateCell(18).SetCellValue("去年");
            row.CreateCell(19).SetCellValue("去年比");

            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 7000);
            for (int i = 2; i < 20; i++)
            {
                row.GetCell(i).CellStyle = cs_Center;
                sheet.SetColumnWidth(i, 3500);
            }

            int rowCount = 6;

            var result = list.GroupJoin(lastList,
             A => A.DespatchName,
             B => B.DespatchName,
             (A, B) => new { A, B })
            .SelectMany(
            r => r.B.DefaultIfEmpty(),
            (r, b) => new { Item = r.A, lastItem = b }
            ).ToList();

            result.ForEach(r =>
            {
                row = sheet.CreateRow(rowCount);
                //去年資料
                if (r.lastItem != null)
                {
                    //清關收入
                    var cc = Math.Ceiling(Convert.ToDouble((r.lastItem.TotalCC / lastEDate.Day)));
                    var totalCC = cc * totalDays ;

                    //手續費
                    var fee = Math.Ceiling(Convert.ToDouble((r.lastItem.TotalFEE2 / lastEDate.Day)));
                    var totalFee = fee * totalDays;

                    //重量
                    var gw = Math.Ceiling(Convert.ToDouble((r.lastItem.TotalGw / lastEDate.Day)));
                    var totalGw = gw * totalDays;

                    row.CreateCell(3).SetCellValue(cc);//當日-去年清關收入
                    row.CreateCell(6).SetCellValue(fee);//當日-去年手續費
                    row.CreateCell(9).SetCellValue(gw);//當日-去年重量

                    row.CreateCell(12).SetCellValue(totalCC);//去年清關收入
                    row.CreateCell(15).SetCellValue(totalFee);//去年手續費
                    row.CreateCell(18).SetCellValue(totalGw);//去年重量


                    row.CreateCell(4).CellFormula = cc > 0 ? $"C{rowCount + 1}/D{rowCount + 1}" : null;
                    row.CreateCell(7).CellFormula = fee > 0 ? $"F{rowCount + 1}/G{rowCount + 1}" : null;
                    row.CreateCell(10).CellFormula = gw > 0 ? $"I{rowCount + 1}/J{rowCount + 1}" : null;
                    row.CreateCell(13).CellFormula = totalCC > 0 ? $"L{rowCount + 1}/M{rowCount + 1}" : null;
                    row.CreateCell(16).CellFormula = totalFee > 0 ? $"O{rowCount + 1}/P{rowCount + 1}" : null;
                    row.CreateCell(19).CellFormula = totalGw > 0 ? $"R{rowCount + 1}/S{rowCount + 1}" : null;

                    row.GetCell(4).CellStyle = cs_Percent2;
                    row.GetCell(7).CellStyle = cs_Percent2;
                    row.GetCell(10).CellStyle = cs_Percent2;
                    row.GetCell(13).CellStyle = cs_Percent2;
                    row.GetCell(16).CellStyle = cs_Percent2;
                    row.GetCell(19).CellStyle = cs_Percent2;

                    row.GetCell(3).CellStyle = cs_Int;
                    row.GetCell(6).CellStyle = cs_Int;
                    row.GetCell(9).CellStyle = cs_Int;
                    row.GetCell(12).CellStyle = cs_Int;
                    row.GetCell(15).CellStyle = cs_Int;
                    row.GetCell(18).CellStyle = cs_Int;
                }
                else 
                {
                    row.CreateCell(4).SetCellValue("-");
                    row.CreateCell(7).SetCellValue("-");
                    row.CreateCell(10).SetCellValue("-");
                    row.CreateCell(13).SetCellValue("-");
                    row.CreateCell(16).SetCellValue("-");
                    row.CreateCell(19).SetCellValue("-");
                    row.GetCell(4).CellStyle = cs_Right;
                    row.GetCell(7).CellStyle = cs_Right;
                    row.GetCell(10).CellStyle = cs_Right;
                    row.GetCell(13).CellStyle = cs_Right;
                    row.GetCell(16).CellStyle = cs_Right;
                    row.GetCell(19).CellStyle = cs_Right;
                }

                row.CreateCell(0).SetCellValue(r.Item.TranType); //分類
                row.CreateCell(1).SetCellValue(r.Item.DespatchName);//客戶
                row.CreateCell(2).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.CC)));//清關收入
                row.CreateCell(5).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.FEE2)));//手續費
                row.CreateCell(8).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.Gw)));//重量
                row.CreateCell(11).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.TotalCC)));//清關收入
                row.CreateCell(14).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.TotalFEE2)));//手續費
                row.CreateCell(17).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.TotalGw)));//重量

                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;

                rowCount++;
            });

            //去年客戶今年沒有
            var lastResult = lastList
                .Where(r => !list.Select(it => it.DespatchName).Contains(r.DespatchName))
                .ToList();

            if (lastResult != null)
            {
                lastResult.ForEach(r =>
                {
                    row = sheet.CreateRow(rowCount);
                    //清關收入
                    var cc = Math.Ceiling(Convert.ToDouble((r.TotalCC / lastEDate.Day)));
                    var totalCC = cc * totalDays;

                    //手續費
                    var fee = Math.Ceiling(Convert.ToDouble((r.TotalFEE2 / lastEDate.Day)));
                    var totalFee = fee * totalDays;

                    //重量
                    var gw = Math.Ceiling(Convert.ToDouble((r.TotalGw / lastEDate.Day)));
                    var totalGw = gw * totalDays;

                    row.CreateCell(0).SetCellValue(r.TranType); //分類
                    row.CreateCell(1).SetCellValue(r.DespatchName);//客戶
                    row.CreateCell(3).SetCellValue(cc);//當日清關收入
                    row.CreateCell(6).SetCellValue(fee);//當日手續費
                    row.CreateCell(9).SetCellValue(gw);//當日清關收入
                    row.CreateCell(12).SetCellValue(totalCC);//去年清關收入
                    row.CreateCell(15).SetCellValue(totalFee);//去年手續費
                    row.CreateCell(18).SetCellValue(totalGw);//去年清關收入

                    row.CreateCell(4).SetCellValue("-100%");
                    row.CreateCell(7).SetCellValue("-100%");
                    row.CreateCell(10).SetCellValue("-100%");
                    row.CreateCell(13).SetCellValue("-100%");
                    row.CreateCell(16).SetCellValue("-100%");
                    row.CreateCell(19).SetCellValue("-100%");
                    row.GetCell(4).CellStyle = cs_Right;
                    row.GetCell(7).CellStyle = cs_Right;
                    row.GetCell(10).CellStyle = cs_Right;
                    row.GetCell(13).CellStyle = cs_Right;
                    row.GetCell(16).CellStyle = cs_Right;
                    row.GetCell(19).CellStyle = cs_Right;

                    row.GetCell(3).CellStyle = cs_Int;
                    row.GetCell(6).CellStyle = cs_Int;
                    row.GetCell(9).CellStyle = cs_Int;
                    row.GetCell(12).CellStyle = cs_Int;
                    row.GetCell(15).CellStyle = cs_Int;
                    row.GetCell(18).CellStyle = cs_Int;

                    rowCount++;
                });
            }

            #region 小計
            row = sheet.CreateRow(5);

            row.CreateCell(1).SetCellValue("小計");
            row.CreateCell(2).CellFormula = $"SUM(C7:C{rowCount})";
            row.CreateCell(3).CellFormula = $"SUM(D7:D{rowCount})";
            row.CreateCell(4).CellFormula = $"C{6}/D{6}";
            row.CreateCell(5).CellFormula = $"SUM(F7:F{rowCount})";
            row.CreateCell(6).CellFormula = $"SUM(G7:G{rowCount})";
            row.CreateCell(7).CellFormula = $"F{6}/G{6}";
            row.CreateCell(8).CellFormula = $"SUM(I7:I{rowCount})";
            row.CreateCell(9).CellFormula = $"SUM(J7:J{rowCount})";
            row.CreateCell(10).CellFormula = $"I{6}/J{6}";
            row.CreateCell(11).CellFormula = $"SUM(L7:L{rowCount})";
            row.CreateCell(12).CellFormula = $"SUM(M7:M{rowCount})";
            row.CreateCell(13).CellFormula = $"L{6}/M{6}";

            row.CreateCell(14).CellFormula = $"SUM(O7:O{rowCount})";
            row.CreateCell(15).CellFormula = $"SUM(P7:P{rowCount})";
            row.CreateCell(16).CellFormula = $"O{6}/P{6}";

            row.CreateCell(17).CellFormula = $"SUM(R7:R{rowCount})";
            row.CreateCell(18).CellFormula = $"SUM(S7:S{rowCount})";
            row.CreateCell(19).CellFormula = $"R{6}/S{6}";

            row.GetCell(1).CellStyle = cs_Center_Blue;
            row.GetCell(2).CellStyle = cs_Int_Blue;
            row.GetCell(3).CellStyle = cs_Int_Blue;
            row.GetCell(4).CellStyle = cs_Percent;
            row.GetCell(5).CellStyle = cs_Int_Blue;
            row.GetCell(6).CellStyle = cs_Int_Blue;
            row.GetCell(7).CellStyle = cs_Percent;
            row.GetCell(8).CellStyle = cs_Int_Blue;
            row.GetCell(9).CellStyle = cs_Int_Blue;
            row.GetCell(10).CellStyle = cs_Percent;
            row.GetCell(11).CellStyle = cs_Int_Blue;
            row.GetCell(12).CellStyle = cs_Int_Blue;
            row.GetCell(13).CellStyle = cs_Percent;
            row.GetCell(14).CellStyle = cs_Int_Blue;
            row.GetCell(15).CellStyle = cs_Int_Blue;
            row.GetCell(16).CellStyle = cs_Percent;
            row.GetCell(17).CellStyle = cs_Int_Blue;
            row.GetCell(18).CellStyle = cs_Int_Blue;
            row.GetCell(19).CellStyle = cs_Percent;

            #endregion
        }

        /// <summary>
        /// 營收報表-到港日
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.IncomeEtaReport)]
        public ActionResult IncomeETAReport()
        {
            IncomeReportViewModel vm = new IncomeReportViewModel()
            {
                sDate = DateTime.Now.ToString("yyyy-MM-dd"),
                eDate = DateTime.Now.ToString("yyyy-MM-dd")
            };
            return View(vm);
        }

        /// <summary>
        /// 營收報表-到港日-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.IncomeEtaReport)]
        public ActionResult IncomeETAReportExcel(IncomeReportViewModel vm)
        {
            if (vm.rdoSearchType == "Yes")
            {
                //重新轉檔
                incomeService.Insert_Income_ETA_Report(vm.sDate, vm.eDate);
            }

            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");

            IWorkbook workbook = GetIncomeETAReportWorkbook(sDate, eDate);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";

            fileName = $"{sDate}~{eDate}-營收報表-到港日.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = "" }
            };
        }

        /// <summary>
        /// 營收報表-到港日-Excel
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        IWorkbook GetIncomeETAReportWorkbook(string sDate, string eDate)
        {
            DataRow[] dr;
            string customer, type;
            IWorkbook workbook = new XSSFWorkbook();

            //倉儲總表
            DataTable dt_Report_Type = incomeService.IncomeETAReport_Type(sDate, eDate).dt;
            //用倉儲區分 sheet
            var dt_Type = from t in dt_Report_Type.AsEnumerable()
                          group t by new { DATA_TYPE = t.Field<string>("DATA_TYPE") } into g
                          select new
                          {
                              DATA_TYPE = g.Key.DATA_TYPE
                          };

            //合計倉儲總表
            GetIncomeETAReportTypeSheet(workbook, dt_Report_Type.Select("1=1", "DATADATE,DATA_TYPE,MAINNUMBER"), "倉儲總表", sDate, eDate);

            //倉儲總表
            foreach (var item in dt_Type)
            {
                type = item.DATA_TYPE;
                if (type == null)
                {
                    type = "無倉儲";
                    dr = dt_Report_Type.Select($"DATA_TYPE is null or DATA_TYPE=''", "DATADATE");
                }
                else
                {
                    dr = dt_Report_Type.Select($"DATA_TYPE='{type}'", "DATADATE");
                }
                //取得頁籤
                GetIncomeETAReportTypeSheet(workbook, dr, type, sDate, eDate);
            }

            //合計總表
            GetIncomeETAReportSheet(workbook, "總表", sDate, eDate);

            //總表
            DataTable dt_Report_Day = incomeService.IncomeETAReport_Day(sDate, eDate).dt;
            //用客戶區分 sheet
            var dt_Customer = from t in dt_Report_Day.AsEnumerable()
                              group t by new { customer = t.Field<string>("DESPATCH_NAME") } into g
                              select new
                              {
                                  customer = g.Key.customer
                              };
            //客戶總表
            foreach (var item in dt_Customer)
            {
                customer = item.customer;
                if (customer == null)
                {
                    customer = "無客戶";
                    dr = dt_Report_Day.Select($"DESPATCH_NAME is null or DESPATCH_NAME=''", "DATADATE");
                }
                else
                {
                    dr = dt_Report_Day.Select($"DESPATCH_NAME='{customer}'", "DATADATE");
                }
                //取得頁籤
                GetIncomeETAReportDaySheet(workbook, dr, $"{customer}總表", sDate, eDate);
            }

            //明細
            DataTable dt_Report_Day2 = incomeService.IncomeETAReport_Day2(sDate, eDate).dt;
            //客戶明細
            foreach (var item in dt_Customer)
            {
                customer = item.customer;
                if (customer == null)
                {
                    customer = "無客戶";
                    dr = dt_Report_Day2.Select($"DESPATCH_NAME is null or DESPATCH_NAME=''", "DATADATE,MAINNUMBER");
                }
                else
                {
                    dr = dt_Report_Day2.Select($"DESPATCH_NAME='{customer}'", "DATADATE,MAINNUMBER");
                }
                //取得頁籤
                GetIncomeETAReportDay2Sheet(workbook, dr, $"{customer}明細", sDate, eDate);
            }

            return workbook;
        }

        /// <summary>
        /// 營收報表-到港日-Excel-頁籤-倉儲總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeETAReportSheet(IWorkbook workbook, string sheetName, string sDate, string eDate)
        {
            //總表
            DataTable dt_Report = incomeService.IncomeETAReport(sDate, eDate).dt;

            int total_fee2, total_bag_number, total_count, total_piece, total_in_time_piece, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_in_time_gw, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 14));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("客戶代號");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("原單件數");
            row.CreateCell(3).SetCellValue("原單毛重");
            row.CreateCell(4).SetCellValue("入倉件數");
            row.CreateCell(5).SetCellValue("入倉毛重");
            row.CreateCell(6).SetCellValue("清關收入");
            row.CreateCell(7).SetCellValue("手續費");
            row.CreateCell(8).SetCellValue("應收關稅");
            row.CreateCell(9).SetCellValue("包稅應付稅金");
            row.CreateCell(10).SetCellValue("出貨人應付稅金");
            row.CreateCell(11).SetCellValue("收件人應付稅金");
            row.CreateCell(12).SetCellValue("應付報關費");
            row.CreateCell(13).SetCellValue("袋數");
            row.CreateCell(14).SetCellValue("筆數");

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


            sheet.SetColumnWidth(0, 4500);
            sheet.SetColumnWidth(1, 4500);
            sheet.SetColumnWidth(2, 4500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            for (int i = 0; i < dt_Report.Rows.Count; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_in_time_piece = 0;

                total_cc = 0;
                total_gw = 0;
                total_tariff = 0;


                int.TryParse(dt_Report.Rows[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dt_Report.Rows[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dt_Report.Rows[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dt_Report.Rows[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dt_Report.Rows[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dt_Report.Rows[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dt_Report.Rows[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dt_Report.Rows[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dt_Report.Rows[i]["TOTAL_IN_TIME_PIECE"].ToString(), out total_in_time_piece);

                double.TryParse(dt_Report.Rows[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dt_Report.Rows[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dt_Report.Rows[i]["TOTAL_IN_TIME_GW"].ToString(), out total_in_time_gw);
                double.TryParse(dt_Report.Rows[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 3);
                row.CreateCell(0).SetCellValue(dt_Report.Rows[i]["DESPATCH_NO"].ToString());
                row.CreateCell(1).SetCellValue(dt_Report.Rows[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(2).SetCellValue(total_piece);
                row.CreateCell(3).SetCellValue(total_gw);
                row.CreateCell(4).SetCellValue(total_in_time_piece);
                row.CreateCell(5).SetCellValue(total_in_time_gw);
                row.CreateCell(6).SetCellValue(total_cc);
                row.CreateCell(7).SetCellValue(total_fee2);
                row.CreateCell(8).SetCellValue(total_tariff);
                row.CreateCell(9).SetCellValue(total_tax_Y);
                row.CreateCell(10).SetCellValue(total_tax_C);
                row.CreateCell(11).SetCellValue(total_tax_N);
                row.CreateCell(12).SetCellValue(total_ccfee);
                row.CreateCell(13).SetCellValue(total_bag_number);
                row.CreateCell(14).SetCellValue(total_count);

                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Double;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Double;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
            }
        }

        /// <summary>
        /// 營收報表-到港日-Excel-頁籤-倉儲
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeETAReportTypeSheet(IWorkbook workbook, DataRow[] dr, string sheetName, string sDate, string eDate)
        {
            int total_fee2, total_bag_number, total_count, total_piece, total_in_time_piece, total_piece_hct, total_piece_jetf, total_piece_whs, total_piece_car, total_piece_oth, total_tax_N, total_tax_C, total_tax_Y, total_ccfee;
            double total_cc, total_gw, total_in_time_gw, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 24));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;

            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("到港日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("櫃號");
            row.CreateCell(4).SetCellValue("客戶代號");
            row.CreateCell(5).SetCellValue("客戶");
            row.CreateCell(6).SetCellValue("原單件數");

            row.CreateCell(7).SetCellValue("派件公司件數");

            row.CreateCell(12).SetCellValue("原單毛重");
            row.CreateCell(13).SetCellValue("輕重櫃");
            row.CreateCell(14).SetCellValue("入倉件數");
            row.CreateCell(15).SetCellValue("入倉毛重");
            row.CreateCell(16).SetCellValue("清關收入");
            row.CreateCell(17).SetCellValue("手續費");
            row.CreateCell(18).SetCellValue("應收關稅");
            row.CreateCell(19).SetCellValue("包稅應付稅金");
            row.CreateCell(20).SetCellValue("出貨人應付稅金");
            row.CreateCell(21).SetCellValue("收件人應付稅金");
            row.CreateCell(22).SetCellValue("應付報關費");
            row.CreateCell(23).SetCellValue("袋數");
            row.CreateCell(24).SetCellValue("筆數");


            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 0, 0));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 1, 1));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 2, 2));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 3, 3));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 4, 4));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 5, 5));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 6, 6));
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 7, 11));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 12, 12));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 13, 13));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 14, 14));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 15, 15));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 16, 16));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 17, 17));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 18, 18));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 19, 19));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 20, 20));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 21, 21));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 22, 22));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 23, 23));
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 24, 24));

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;
            row.GetCell(21).CellStyle = cs_Center;
            row.GetCell(22).CellStyle = cs_Center;
            row.GetCell(23).CellStyle = cs_Center;
            row.GetCell(24).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);
            sheet.SetColumnWidth(21, 4500);
            sheet.SetColumnWidth(22, 4500);
            sheet.SetColumnWidth(23, 4500);
            sheet.SetColumnWidth(24, 4500);

            row = sheet.CreateRow(3);
            row.CreateCell(7).SetCellValue("新竹");
            row.CreateCell(8).SetCellValue("捷豐自派");
            row.CreateCell(9).SetCellValue("回倉庫");
            row.CreateCell(10).SetCellValue("專車派送");
            row.CreateCell(11).SetCellValue("其它");
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;

            for (int i = 0; i < dr.Length; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_C = 0;
                total_tax_Y = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_piece = 0;
                total_piece_hct = 0;
                total_piece_jetf = 0;
                total_piece_whs = 0;
                total_piece_car = 0;
                total_piece_oth = 0;

                total_cc = 0;
                total_gw = 0;
                total_tariff = 0;


                int.TryParse(dr[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dr[i]["TOTAL_IN_TIME_PIECE"].ToString(), out total_in_time_piece);
                int.TryParse(dr[i]["TOTAL_PIECE_HCT"].ToString(), out total_piece_hct);
                int.TryParse(dr[i]["TOTAL_PIECE_JETF"].ToString(), out total_piece_jetf);
                int.TryParse(dr[i]["TOTAL_PIECE_WHS"].ToString(), out total_piece_whs);
                int.TryParse(dr[i]["TOTAL_PIECE_CAR"].ToString(), out total_piece_car);
                int.TryParse(dr[i]["TOTAL_PIECE_OTH"].ToString(), out total_piece_oth);

                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_IN_TIME_GW"].ToString(), out total_in_time_gw);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 4);

                row.CreateCell(0).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dr[i]["DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["MAINNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["CONT_NO"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["DESPATCH_NO"].ToString());
                row.CreateCell(5).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(6).SetCellValue(total_piece);
                row.CreateCell(7).SetCellValue(total_piece_hct);
                row.CreateCell(8).SetCellValue(total_piece_jetf);
                row.CreateCell(9).SetCellValue(total_piece_whs);
                row.CreateCell(10).SetCellValue(total_piece_car);
                row.CreateCell(11).SetCellValue(total_piece_oth);

                row.CreateCell(12).SetCellValue(total_gw);
                row.CreateCell(13).SetCellValue(total_gw >= 7000 ? "重" : "輕");
                row.CreateCell(14).SetCellValue(total_in_time_piece);
                row.CreateCell(15).SetCellValue(total_in_time_gw);
                row.CreateCell(16).SetCellValue(total_cc);
                row.CreateCell(17).SetCellValue(total_fee2);
                row.CreateCell(18).SetCellValue(total_tariff);
                row.CreateCell(19).SetCellValue(total_tax_Y);
                row.CreateCell(20).SetCellValue(total_tax_C);
                row.CreateCell(21).SetCellValue(total_tax_N);
                row.CreateCell(22).SetCellValue(total_ccfee);
                row.CreateCell(23).SetCellValue(total_bag_number);
                row.CreateCell(24).SetCellValue(total_count);

                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Double;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Double;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Double;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;
                row.GetCell(21).CellStyle = cs_Int;
                row.GetCell(22).CellStyle = cs_Int;
                row.GetCell(23).CellStyle = cs_Int;
                row.GetCell(24).CellStyle = cs_Int;
            }
        }

        /// <summary>
        /// 營收報表-到港日-Excel-頁籤-客戶總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeETAReportDaySheet(IWorkbook workbook, DataRow[] dr, string sheetName, string sDate, string eDate)
        {
            int total_fee2, total_bag_number, total_count, total_piece, total_in_time_piece, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_in_time_gw, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 17));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("到港日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("客戶代號");
            row.CreateCell(4).SetCellValue("客戶");
            row.CreateCell(5).SetCellValue("原單件數");
            row.CreateCell(6).SetCellValue("原單毛重");
            row.CreateCell(7).SetCellValue("入倉件數");
            row.CreateCell(8).SetCellValue("入倉毛重");
            row.CreateCell(9).SetCellValue("清關收入");
            row.CreateCell(10).SetCellValue("手續費");
            row.CreateCell(11).SetCellValue("應收關稅");
            row.CreateCell(12).SetCellValue("包稅應付稅金");
            row.CreateCell(13).SetCellValue("出貨人應付稅金");
            row.CreateCell(14).SetCellValue("收件人應付稅金");
            row.CreateCell(15).SetCellValue("應付報關費");
            row.CreateCell(16).SetCellValue("袋數");
            row.CreateCell(17).SetCellValue("筆數");

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
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 6000);
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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);


            for (int i = 0; i < dr.Length; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_in_time_piece = 0;

                total_cc = 0;
                total_gw = 0;
                total_tariff = 0;


                int.TryParse(dr[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dr[i]["TOTAL_IN_TIME_PIECE"].ToString(), out total_in_time_piece);

                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_IN_TIME_GW"].ToString(), out total_in_time_gw);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 3);

                row.CreateCell(0).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dr[i]["DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["MAINNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["DESPATCH_NO"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(5).SetCellValue(total_piece);
                row.CreateCell(6).SetCellValue(total_gw);
                row.CreateCell(7).SetCellValue(total_in_time_piece);
                row.CreateCell(8).SetCellValue(total_in_time_gw);
                row.CreateCell(9).SetCellValue(total_cc);
                row.CreateCell(10).SetCellValue(total_fee2);
                row.CreateCell(11).SetCellValue(total_tariff);
                row.CreateCell(12).SetCellValue(total_tax_Y);
                row.CreateCell(13).SetCellValue(total_tax_C);
                row.CreateCell(14).SetCellValue(total_tax_N);
                row.CreateCell(15).SetCellValue(total_ccfee);
                row.CreateCell(16).SetCellValue(total_bag_number);
                row.CreateCell(17).SetCellValue(total_count);

                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Double;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Double;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
            }
        }

        /// <summary>
        /// 營收報表-到港日-Excel-頁籤-客戶明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeETAReportDay2Sheet(IWorkbook workbook, DataRow[] dr, string sheetName, string sDate, string eDate)
        {
            int total_fee2, total_bag_number, total_count, total_piece, total_in_time_piece, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_in_time_gw, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 20));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("到港日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("客戶代號");
            row.CreateCell(4).SetCellValue("客戶");
            row.CreateCell(5).SetCellValue("派件公司代號");
            row.CreateCell(6).SetCellValue("派件公司");
            row.CreateCell(7).SetCellValue("包稅不包稅");

            row.CreateCell(8).SetCellValue("原單件數");
            row.CreateCell(9).SetCellValue("原單毛重");
            row.CreateCell(10).SetCellValue("入倉件數");
            row.CreateCell(11).SetCellValue("入倉毛重");
            row.CreateCell(12).SetCellValue("清關收入");
            row.CreateCell(13).SetCellValue("手續費");
            row.CreateCell(14).SetCellValue("應收關稅");
            row.CreateCell(15).SetCellValue("包稅應付稅金");
            row.CreateCell(16).SetCellValue("出貨人應付稅金");
            row.CreateCell(17).SetCellValue("收件人應付稅金");
            row.CreateCell(18).SetCellValue("應付報關費");
            row.CreateCell(19).SetCellValue("袋數");
            row.CreateCell(20).SetCellValue("筆數");

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
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 6000);
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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);


            for (int i = 0; i < dr.Length; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_in_time_piece = 0;

                total_cc = 0;
                total_gw = 0;
                total_tariff = 0;


                int.TryParse(dr[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dr[i]["TOTAL_IN_TIME_PIECE"].ToString(), out total_in_time_piece);

                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_IN_TIME_GW"].ToString(), out total_in_time_gw);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 3);
                row.CreateCell(0).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dr[i]["DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["MAINNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["DESPATCH_NO"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(5).SetCellValue(dr[i]["TRANS_NO"].ToString());
                row.CreateCell(6).SetCellValue(dr[i]["TRANS_NAME"].ToString());
                row.CreateCell(7).SetCellValue(dr[i]["INCLUDE_TAX"].ToString());
                row.CreateCell(8).SetCellValue(total_piece);
                row.CreateCell(9).SetCellValue(total_gw);
                row.CreateCell(10).SetCellValue(total_in_time_piece);
                row.CreateCell(11).SetCellValue(total_in_time_gw);
                row.CreateCell(12).SetCellValue(total_cc);
                row.CreateCell(13).SetCellValue(total_fee2);
                row.CreateCell(14).SetCellValue(total_tariff);
                row.CreateCell(15).SetCellValue(total_tax_Y);
                row.CreateCell(16).SetCellValue(total_tax_C);
                row.CreateCell(17).SetCellValue(total_tax_N);
                row.CreateCell(18).SetCellValue(total_ccfee);
                row.CreateCell(19).SetCellValue(total_bag_number);
                row.CreateCell(20).SetCellValue(total_count);

                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Double;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Double;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;

            }
        }

        /// <summary>
        /// 營收總表及明細表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.IncomeDetails)]
        public ActionResult IncomeDetailsReport()
        {
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "海運", Value = "SEA" });
            sourceList.Add(new SelectListItem() { Text = "空運", Value = "ETL" });

            IncomeReportViewModel vm = new IncomeReportViewModel()
            {
                sDate = DateTime.Now.ToString("yyyy-MM-dd"),
                eDate = DateTime.Now.ToString("yyyy-MM-dd"),
                ddlSourceList = sourceList
            };


            return View(vm);
        }

        /// <summary>
        /// 營收總表及明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.IncomeDetails)]
        public ActionResult IncomeDetailsReportExcel(IncomeReportViewModel vm)
        {
            string fileName = "";
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            IWorkbook workbook;
            if (vm.source == "SEA")
            {
                workbook = GetIncomeDetailsSeaReportWorkbook(vm.source, sDate, eDate);
                fileName = $"{sDate}~{eDate}-海運-營收總表及明細表.xlsx";
            }
            else {
                workbook = GetIncomeDetailsEtlReportWorkbook(vm.source, sDate, eDate);
                fileName = $"{sDate}~{eDate}-空運-營收總表及明細表.xlsx";
            }
           

            string handle = Guid.NewGuid().ToString();
      

         

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = "" }
            };
        }

        /// <summary>
        /// 營收總表及明細表-Excel-海運
        /// </summary>
        /// <param name="original"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        IWorkbook GetIncomeDetailsSeaReportWorkbook(string original, string sDate, string eDate)
        {
            DataRow[] dr;
            string customer;
            IWorkbook workbook = new XSSFWorkbook();

            //總表
            DataTable dt_Report = incomeService.IncomeDetailsReport(original, sDate, eDate).dt;
            //用客戶區分 sheet
            var dt_Customer = from t in dt_Report.AsEnumerable()
                              group t by new { customer = t.Field<string>("DESPATCH_NAME") } into g
                              select new
                              {
                                  customer = g.Key.customer
                              };

            //海快通關狀態彙總表
            GetIncomeDetailsSeaReportSheet(workbook, dt_Report, "海快通關狀態彙總表", sDate, eDate);
            //總表
            GetIncomeDetailsSeaReportSheet2(workbook, dt_Report.Select("1=1", "DATADATE,MAINNUMBER"), "總表", sDate, eDate);

            //客戶總表
            foreach (var item in dt_Customer)
            {
                customer = item.customer;
                if (customer == null)
                {
                    customer = "無客戶";
                    dr = dt_Report.Select($"DESPATCH_NAME is null or DESPATCH_NAME=''", "DATADATE,MAINNUMBER");
                }
                else
                {
                    dr = dt_Report.Select($"DESPATCH_NAME='{customer}'", "DATADATE,MAINNUMBER");
                }
                //取得頁籤
                GetIncomeDetailsSeaReportSheet2(workbook, dr, $"{customer}總表", sDate, eDate);
            }

            //明細
            DataTable dt_Details = incomeService.IncomeDetails(original, sDate, eDate).dt;

            ////客戶明細
            foreach (var item in dt_Customer)
            {
                customer = item.customer;
                if (customer == null)
                {
                    customer = "無客戶";
                    dr = dt_Details.Select($"DESPATCH_NAME is null or DESPATCH_NAME=''", "DATADATE,MAINNUMBER");
                }
                else
                {
                    dr = dt_Details.Select($"DESPATCH_NAME='{customer}'", "DATADATE,MAINNUMBER");
                }
                //取得頁籤
                GetIncomeDetailsSeaSheet(workbook, dr, $"{customer}明細", sDate, eDate);
            }

            return workbook;
        }

        /// <summary>
        /// 營收總表及明細表-Excel-海運-頁籤-海快通關狀態彙總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeDetailsSeaReportSheet(IWorkbook workbook, DataTable dt_Report, string sheetName, string sDate, string eDate)
        {
            int rowCount, total_fee, total_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 16));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title_Left;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("通關主號數");
            row.CreateCell(3).SetCellValue("入倉件數");
            row.CreateCell(4).SetCellValue("出倉件數");
            row.CreateCell(5).SetCellValue("C3件數");
            row.CreateCell(6).SetCellValue("入-出件數");
            row.CreateCell(7).SetCellValue("入倉毛重");
            row.CreateCell(8).SetCellValue("袋數");
            row.CreateCell(9).SetCellValue("筆數");
            row.CreateCell(10).SetCellValue("清關收入");
            row.CreateCell(11).SetCellValue("手續費");
            row.CreateCell(12).SetCellValue("應收關稅");
            row.CreateCell(13).SetCellValue("包稅應付稅金");
            row.CreateCell(14).SetCellValue("出貨人應付稅金");
            row.CreateCell(15).SetCellValue("收件人應付稅金");
            row.CreateCell(16).SetCellValue("應付報關費");

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
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;

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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);

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
                row.CreateCell(7).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(8).SetCellValue(total_bag_number);
                row.CreateCell(9).SetCellValue(total_count);
                row.CreateCell(10).SetCellValue(Math.Ceiling(total_cc));
                row.CreateCell(11).SetCellValue(total_fee);
                row.CreateCell(12).SetCellValue(Math.Ceiling(total_tariff));
                row.CreateCell(13).SetCellValue(total_tax_Y);
                row.CreateCell(14).SetCellValue(total_tax_C);
                row.CreateCell(15).SetCellValue(total_tax_N);
                row.CreateCell(16).SetCellValue(total_ccfee);


                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
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
                row.CreateCell(7).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(8).SetCellValue(total_bag_number);
                row.CreateCell(9).SetCellValue(total_count);
                row.CreateCell(10).SetCellValue(Math.Ceiling(total_cc));
                row.CreateCell(11).SetCellValue(total_fee);
                row.CreateCell(12).SetCellValue(Math.Ceiling(total_tariff));
                row.CreateCell(13).SetCellValue(total_tax_Y);
                row.CreateCell(14).SetCellValue(total_tax_C);
                row.CreateCell(15).SetCellValue(total_tax_N);
                row.CreateCell(16).SetCellValue(total_ccfee);


                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                rowCount++;
            }
        }

        /// <summary>
        /// 營收總表及明細表-Excel-海運-頁籤-客戶總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeDetailsSeaReportSheet2(IWorkbook workbook, DataRow[] dr, string sheetName, string sDate, string eDate)
        {
            int total_fee, total_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 20));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("客戶代號");
            row.CreateCell(4).SetCellValue("客戶");
            row.CreateCell(5).SetCellValue("原單件數");
            row.CreateCell(6).SetCellValue("入倉件數");
            row.CreateCell(7).SetCellValue("出倉件數");
            row.CreateCell(8).SetCellValue("C3件數");
            row.CreateCell(9).SetCellValue("入-出件數");
            row.CreateCell(10).SetCellValue("原單毛重");
            row.CreateCell(11).SetCellValue("入倉毛重");
            row.CreateCell(12).SetCellValue("袋數");
            row.CreateCell(13).SetCellValue("筆數");
            row.CreateCell(14).SetCellValue("清關收入");
            row.CreateCell(15).SetCellValue("手續費");
            row.CreateCell(16).SetCellValue("應收關稅");
            row.CreateCell(17).SetCellValue("包稅應付稅金");
            row.CreateCell(18).SetCellValue("出貨人應付稅金");
            row.CreateCell(19).SetCellValue("收件人應付稅金");
            row.CreateCell(20).SetCellValue("應付報關費");

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
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;

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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);

            for (int i = 0; i < dr.Length; i++)
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

                int.TryParse(dr[i]["TOTAL_FEE"].ToString(), out total_fee);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dr[i]["TOTAL_OUT_PIECE"].ToString(), out total_out_piece);
                int.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[1], out total_piece_all);
                int.TryParse(dr[i]["TOTAL_PIECE_C3"].ToString(), out total_piece_c3);

                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[0], out total_gw_all);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 3);
                row.CreateCell(0).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dr[i]["I_DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["MAINNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["DESPATCH_NO"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(5).SetCellValue(total_piece_all);
                row.CreateCell(6).SetCellValue(total_piece);
                row.CreateCell(7).SetCellValue(total_out_piece);
                row.CreateCell(8).SetCellValue(total_piece_c3);
                row.CreateCell(9).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(10).SetCellValue(total_gw_all);
                row.CreateCell(11).SetCellValue(total_gw);
                row.CreateCell(12).SetCellValue(total_bag_number);
                row.CreateCell(13).SetCellValue(total_count);
                row.CreateCell(14).SetCellValue(total_cc);
                row.CreateCell(15).SetCellValue(total_fee);
                row.CreateCell(16).SetCellValue(total_tariff);
                row.CreateCell(17).SetCellValue(total_tax_Y);
                row.CreateCell(18).SetCellValue(total_tax_C);
                row.CreateCell(19).SetCellValue(total_tax_N);
                row.CreateCell(20).SetCellValue(total_ccfee);


                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Double;
                row.GetCell(11).CellStyle = cs_Double;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;

            }
        }

        /// <summary>
        /// 營收總表及明細表-Excel-海運-頁籤-客戶明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeDetailsSeaSheet(IWorkbook workbook, DataRow[] dr, string sheetName, string sDate, string eDate)
        {
            int total_fee, total_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 23));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("客戶代號");
            row.CreateCell(4).SetCellValue("客戶");
            row.CreateCell(5).SetCellValue("派件公司代號");
            row.CreateCell(6).SetCellValue("派件公司");
            row.CreateCell(7).SetCellValue("包稅不包稅");
            row.CreateCell(8).SetCellValue("原單件數");
            row.CreateCell(9).SetCellValue("入倉件數");
            row.CreateCell(10).SetCellValue("出倉件數");
            row.CreateCell(11).SetCellValue("C3件數");
            row.CreateCell(12).SetCellValue("入-出件數");
            row.CreateCell(13).SetCellValue("原單毛重");
            row.CreateCell(14).SetCellValue("入倉毛重");
            row.CreateCell(15).SetCellValue("袋數");
            row.CreateCell(16).SetCellValue("筆數");
            row.CreateCell(17).SetCellValue("清關收入");
            row.CreateCell(18).SetCellValue("手續費");
            row.CreateCell(19).SetCellValue("應收關稅");
            row.CreateCell(20).SetCellValue("包稅應付稅金");
            row.CreateCell(21).SetCellValue("出貨人應付稅金");
            row.CreateCell(22).SetCellValue("收件人應付稅金");
            row.CreateCell(23).SetCellValue("應付報關費");

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
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;
            row.GetCell(21).CellStyle = cs_Center;
            row.GetCell(22).CellStyle = cs_Center;
            row.GetCell(23).CellStyle = cs_Center;

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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);
            sheet.SetColumnWidth(21, 4500);
            sheet.SetColumnWidth(22, 4500);
            sheet.SetColumnWidth(23, 4500);


            for (int i = 0; i < dr.Length; i++)
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


                int.TryParse(dr[i]["TOTAL_FEE"].ToString(), out total_fee);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dr[i]["TOTAL_OUT_PIECE"].ToString(), out total_out_piece);
                int.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[1], out total_piece_all);
                int.TryParse(dr[i]["TOTAL_PIECE_C3"].ToString(), out total_piece_c3);

                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[0], out total_gw_all);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 3);
                row.CreateCell(0).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dr[i]["I_DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["MAINNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["DESPATCH_NO"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(5).SetCellValue(dr[i]["TRANS_NO"].ToString());
                row.CreateCell(6).SetCellValue(dr[i]["TRANS_NAME"].ToString());
                row.CreateCell(7).SetCellValue(dr[i]["INCLUDE_TAX"].ToString());
                row.CreateCell(8).SetCellValue(total_piece_all);
                row.CreateCell(9).SetCellValue(total_piece);
                row.CreateCell(10).SetCellValue(total_out_piece);
                row.CreateCell(11).SetCellValue(total_piece_c3);
                row.CreateCell(12).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(13).SetCellValue(total_gw_all);
                row.CreateCell(14).SetCellValue(total_gw);
                row.CreateCell(15).SetCellValue(total_bag_number);
                row.CreateCell(16).SetCellValue(total_count);
                row.CreateCell(17).SetCellValue(total_cc);
                row.CreateCell(18).SetCellValue(total_fee);
                row.CreateCell(19).SetCellValue(total_tariff);
                row.CreateCell(20).SetCellValue(total_tax_Y);
                row.CreateCell(21).SetCellValue(total_tax_C);
                row.CreateCell(22).SetCellValue(total_tax_N);
                row.CreateCell(23).SetCellValue(total_ccfee);


                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Double;
                row.GetCell(14).CellStyle = cs_Double;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;
                row.GetCell(21).CellStyle = cs_Int;
                row.GetCell(22).CellStyle = cs_Int;
                row.GetCell(23).CellStyle = cs_Int;
            }
        }

        /// <summary>
        /// 營收總表及明細表-Excel-空運
        /// </summary>
        /// <param name="original"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        IWorkbook GetIncomeDetailsEtlReportWorkbook(string original, string sDate, string eDate)
        {
            DataRow[] dr;
            string customer;
            IWorkbook workbook = new XSSFWorkbook();

            //總表
            DataTable dt_Report = incomeService.IncomeDetailsReport(original, sDate, eDate).dt;
            //用客戶區分 sheet
            var dt_Customer = from t in dt_Report.AsEnumerable()
                              group t by new { customer = t.Field<string>("DESPATCH_NAME") } into g
                              select new
                              {
                                  customer = g.Key.customer
                              };

            //空快通關狀態彙總表
            GetIncomeDetailsEtlReportSheet(workbook, dt_Report, "空快通關狀態彙總表", sDate, eDate);
            //總表
            GetIncomeDetailsEtlReportSheet2(workbook, dt_Report.Select("1=1", "DATADATE,MAINNUMBER"), "總表", sDate, eDate);

            //客戶總表
            foreach (var item in dt_Customer)
            {
                customer = item.customer;
                if (customer == null)
                {
                    customer = "無客戶";
                    dr = dt_Report.Select($"DESPATCH_NAME is null or DESPATCH_NAME=''", "DATADATE,MAINNUMBER");
                }
                else
                {
                    dr = dt_Report.Select($"DESPATCH_NAME='{customer}'", "DATADATE,MAINNUMBER");
                }
                //取得頁籤
                GetIncomeDetailsEtlReportSheet2(workbook, dr, $"{customer}總表", sDate, eDate);
            }

            //明細
            DataTable dt_Details = incomeService.IncomeDetails(original, sDate, eDate).dt;

            ////客戶明細
            foreach (var item in dt_Customer)
            {
                customer = item.customer;
                if (customer == null)
                {
                    customer = "無客戶";
                    dr = dt_Details.Select($"DESPATCH_NAME is null or DESPATCH_NAME=''", "DATADATE,MAINNUMBER");
                }
                else
                {
                    dr = dt_Details.Select($"DESPATCH_NAME='{customer}'", "DATADATE,MAINNUMBER");
                }
                //取得頁籤
                GetIncomeDetailsEtlSheet(workbook, dr, $"{customer}明細", sDate, eDate);
            }

            return workbook;
        }

        /// <summary>
        /// 營收總表及明細表-Excel-空運-頁籤-空快通關狀態彙總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeDetailsEtlReportSheet(IWorkbook workbook, DataTable dt_Report, string sheetName, string sDate, string eDate)
        {
            int rowCount, total_fee, total_bag_number, total_out_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 17));
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
            row.CreateCell(11).SetCellValue("清關收入");
            row.CreateCell(12).SetCellValue("手續費");
            row.CreateCell(13).SetCellValue("應收關稅");
            row.CreateCell(14).SetCellValue("包稅應付稅金");
            row.CreateCell(15).SetCellValue("出貨人應付稅金");
            row.CreateCell(16).SetCellValue("收件人應付稅金");
            row.CreateCell(17).SetCellValue("應付報關費");
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
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;

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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);

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
                row.CreateCell(11).SetCellValue(Math.Ceiling(total_cc));
                row.CreateCell(12).SetCellValue(total_fee);
                row.CreateCell(13).SetCellValue(Math.Ceiling(total_tariff));
                row.CreateCell(14).SetCellValue(total_tax_Y);
                row.CreateCell(15).SetCellValue(total_tax_C);
                row.CreateCell(16).SetCellValue(total_tax_N);
                row.CreateCell(17).SetCellValue(total_ccfee);


                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Percent2;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
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
                row.CreateCell(11).SetCellValue(Math.Ceiling(total_cc));
                row.CreateCell(12).SetCellValue(total_fee);
                row.CreateCell(13).SetCellValue(Math.Ceiling(total_tariff));
                row.CreateCell(14).SetCellValue(total_tax_Y);
                row.CreateCell(15).SetCellValue(total_tax_C);
                row.CreateCell(16).SetCellValue(total_tax_N);
                row.CreateCell(17).SetCellValue(total_ccfee);


                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Percent2;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
                rowCount++;
            }
        }

        /// <summary>
        /// 營收總表及明細表-Excel-空運-頁籤-客戶總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeDetailsEtlReportSheet2(IWorkbook workbook, DataRow[] dr, string sheetName, string sDate, string eDate)
        {
            int total_fee, total_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 20));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("客戶代號");
            row.CreateCell(4).SetCellValue("客戶");
            row.CreateCell(5).SetCellValue("原單件數");
            row.CreateCell(6).SetCellValue("入倉件數");
            row.CreateCell(7).SetCellValue("出倉件數");
            row.CreateCell(8).SetCellValue("C3件數");
            row.CreateCell(9).SetCellValue("入-出件數");
            row.CreateCell(10).SetCellValue("原單毛重");
            row.CreateCell(11).SetCellValue("入倉毛重");
            row.CreateCell(12).SetCellValue("袋數");
            row.CreateCell(13).SetCellValue("筆數");
            row.CreateCell(14).SetCellValue("清關收入");
            row.CreateCell(15).SetCellValue("手續費");
            row.CreateCell(16).SetCellValue("應收關稅");
            row.CreateCell(17).SetCellValue("包稅應付稅金");
            row.CreateCell(18).SetCellValue("出貨人應付稅金");
            row.CreateCell(19).SetCellValue("收件人應付稅金");
            row.CreateCell(20).SetCellValue("應付報關費");

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
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;

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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);

            for (int i = 0; i < dr.Length; i++)
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

                int.TryParse(dr[i]["TOTAL_FEE"].ToString(), out total_fee);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dr[i]["TOTAL_OUT_PIECE"].ToString(), out total_out_piece);
                int.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[1], out total_piece_all);
                int.TryParse(dr[i]["TOTAL_PIECE_C3"].ToString(), out total_piece_c3);

                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[0], out total_gw_all);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 3);
                row.CreateCell(0).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dr[i]["I_DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["MAINNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["DESPATCH_NO"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(5).SetCellValue(total_piece_all);
                row.CreateCell(6).SetCellValue(total_piece);
                row.CreateCell(7).SetCellValue(total_out_piece);
                row.CreateCell(8).SetCellValue(total_piece_c3);
                row.CreateCell(9).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(10).SetCellValue(total_gw_all);
                row.CreateCell(11).SetCellValue(total_gw);
                row.CreateCell(12).SetCellValue(total_bag_number);
                row.CreateCell(13).SetCellValue(total_count);
                row.CreateCell(14).SetCellValue(total_cc);
                row.CreateCell(15).SetCellValue(total_fee);
                row.CreateCell(16).SetCellValue(total_tariff);
                row.CreateCell(17).SetCellValue(total_tax_Y);
                row.CreateCell(18).SetCellValue(total_tax_C);
                row.CreateCell(19).SetCellValue(total_tax_N);
                row.CreateCell(20).SetCellValue(total_ccfee);


                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Double;
                row.GetCell(11).CellStyle = cs_Double;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;

            }
        }

        /// <summary>
        /// 營收總表及明細表-Excel-空運-頁籤-客戶明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetIncomeDetailsEtlSheet(IWorkbook workbook, DataRow[] dr, string sheetName, string sDate, string eDate)
        {
            int total_fee, total_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 23));
            row.CreateCell(0).SetCellValue(sDate + "-" + eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("客戶代號");
            row.CreateCell(4).SetCellValue("客戶");
            row.CreateCell(5).SetCellValue("派件公司代號");
            row.CreateCell(6).SetCellValue("派件公司");
            row.CreateCell(7).SetCellValue("包稅不包稅");
            row.CreateCell(8).SetCellValue("原單件數");
            row.CreateCell(9).SetCellValue("入倉件數");
            row.CreateCell(10).SetCellValue("出倉件數");
            row.CreateCell(11).SetCellValue("C3件數");
            row.CreateCell(12).SetCellValue("入-出件數");
            row.CreateCell(13).SetCellValue("原單毛重");
            row.CreateCell(14).SetCellValue("入倉毛重");
            row.CreateCell(15).SetCellValue("袋數");
            row.CreateCell(16).SetCellValue("筆數");
            row.CreateCell(17).SetCellValue("清關收入");
            row.CreateCell(18).SetCellValue("手續費");
            row.CreateCell(19).SetCellValue("應收關稅");
            row.CreateCell(20).SetCellValue("包稅應付稅金");
            row.CreateCell(21).SetCellValue("出貨人應付稅金");
            row.CreateCell(22).SetCellValue("收件人應付稅金");
            row.CreateCell(23).SetCellValue("應付報關費");

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
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;
            row.GetCell(21).CellStyle = cs_Center;
            row.GetCell(22).CellStyle = cs_Center;
            row.GetCell(23).CellStyle = cs_Center;

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
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);
            sheet.SetColumnWidth(21, 4500);
            sheet.SetColumnWidth(22, 4500);
            sheet.SetColumnWidth(23, 4500);


            for (int i = 0; i < dr.Length; i++)
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


                int.TryParse(dr[i]["TOTAL_FEE"].ToString(), out total_fee);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_TAX_C"].ToString(), out total_tax_C);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_PIECE"].ToString(), out total_piece);
                int.TryParse(dr[i]["TOTAL_OUT_PIECE"].ToString(), out total_out_piece);
                int.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[1], out total_piece_all);
                int.TryParse(dr[i]["TOTAL_PIECE_C3"].ToString(), out total_piece_c3);

                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_GW_PIECE_All"].ToString().Split(',')[0], out total_gw_all);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);

                row = sheet.CreateRow(i + 3);
                row.CreateCell(0).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dr[i]["I_DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["MAINNUMBER"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["DESPATCH_NO"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());
                row.CreateCell(5).SetCellValue(dr[i]["TRANS_NO"].ToString());
                row.CreateCell(6).SetCellValue(dr[i]["TRANS_NAME"].ToString());
                row.CreateCell(7).SetCellValue(dr[i]["INCLUDE_TAX"].ToString());
                row.CreateCell(8).SetCellValue(total_piece_all);
                row.CreateCell(9).SetCellValue(total_piece);
                row.CreateCell(10).SetCellValue(total_out_piece);
                row.CreateCell(11).SetCellValue(total_piece_c3);
                row.CreateCell(12).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(13).SetCellValue(total_gw_all);
                row.CreateCell(14).SetCellValue(total_gw);
                row.CreateCell(15).SetCellValue(total_bag_number);
                row.CreateCell(16).SetCellValue(total_count);
                row.CreateCell(17).SetCellValue(total_cc);
                row.CreateCell(18).SetCellValue(total_fee);
                row.CreateCell(19).SetCellValue(total_tariff);
                row.CreateCell(20).SetCellValue(total_tax_Y);
                row.CreateCell(21).SetCellValue(total_tax_C);
                row.CreateCell(22).SetCellValue(total_tax_N);
                row.CreateCell(23).SetCellValue(total_ccfee);


                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Double;
                row.GetCell(14).CellStyle = cs_Double;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;
                row.GetCell(21).CellStyle = cs_Int;
                row.GetCell(22).CellStyle = cs_Int;
                row.GetCell(23).CellStyle = cs_Int;
            }
        }

        /// <summary>
        /// Excel Style
        /// </summary>
        /// <param name="workbook"></param>
        void GetWorkbookStyle(IWorkbook workbook) {
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

            cs_Right = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Right.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
            cs_Right.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

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
            cs_Percent.DataFormat = format.GetFormat("0%");
            cs_Percent.SetFont(fontB);

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0%");
            cs_Percent2.SetFont(font1);

        }
    }
}