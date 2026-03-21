using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Models.IncomeReport;

namespace Service.Services.Job.IncomeJob
{
    public static class IncomeRateReport
    {
        static IFont fontB;
        static XSSFDataFormat format;
        static XSSFFont font1;
        static XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Letf, cs_Right, cs_Center_Thick, cs_Center_Blue, cs_Center_Blue_Thick, cs_Int, cs_Int_Thick, cs_Int_Blue, cs_Int_Blue_Thick, cs_Double, cs_Percent, cs_Percent2, cs_Percent2_Blue;
        static SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);

        public static void GetIncomeRateReportExcel(string sDate, string eDate, string filePath)
        {
            //日統計表營收去年比報表
            var sDateTime = DateTime.ParseExact(sDate, "yyyyMMdd", null);
            var eDateTime = DateTime.ParseExact(eDate, "yyyyMMdd", null);
            //去年開始日期
            var lastSDate = new DateTime(sDateTime.Year, sDateTime.Month, 1).AddYears(-1);
            //去年結束日期
            var lastEDate = lastSDate.AddMonths(+1).AddDays(-1);
            var lastList = IncomeReportCustomerRate(lastSDate.ToString("yyyyMMdd"), lastEDate.ToString("yyyyMMdd"));
            var list = IncomeReportCustomerRate(sDate, eDate);

            IWorkbook workbook = new XSSFWorkbook();
            //空快日統計表營收去年比報表
            GetIncomeRateReportSheet(workbook, "空快", sDate, eDate, lastList.Item1, list.Item1);
            //海快日統計表營收去年比報表
            GetIncomeRateReportSheet(workbook, "海快", sDate, eDate, lastList.Item2, list.Item2);

            FileStream file = new FileStream(filePath, FileMode.Create);
            workbook.Write(file);
            file.Close();
            file.Dispose();
        }

        /// <summary>
        /// 營收報表-Excel-去年營收比
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <param name="titleName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public static void GetIncomeRateReportSheet(IWorkbook workbook, string tranType, string sDate, string eDate, List<IncomeReportCustomerRateModel> lastList, List<IncomeReportCustomerRateModel> list)
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
            sheet.DefaultRowHeight = 30 * 20;
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            var cra = new CellRangeAddress(0, 1, 0, 19);
            sheet.AddMergedRegion(cra);
            RegionUtil.SetBorderBottom(1, cra, sheet); // 下邊框
            RegionUtil.SetBorderLeft(1, cra, sheet); // 左邊框
            RegionUtil.SetBorderRight(1, cra, sheet); // 有邊框
            RegionUtil.SetBorderTop(1, cra, sheet); // 上邊框
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

            sheet.SetColumnWidth(0, 4500);
            sheet.SetColumnWidth(1, 9000);

            //合併儲存格框線
            sheet.GetRow(2).CreateCell(3).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(4).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(5).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(6).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(7).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(9).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(10).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(12).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(13).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(14).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(15).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(16).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(17).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(18).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(19).CellStyle = cs_Center;

            sheet.GetRow(3).CreateCell(0).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(1).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(3).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(4).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(6).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(7).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(9).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(10).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(12).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(13).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(15).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(16).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(18).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(19).CellStyle = cs_Center;

            sheet.GetRow(4).CreateCell(0).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(1).CellStyle = cs_Center;

            for (int i = 2; i < 20; i++)
            {
                row.GetCell(i).CellStyle = cs_Center;
                sheet.SetColumnWidth(i, 5500);
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
                    var totalCC = cc * totalDays;

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

                    row.CreateCell(3).CellStyle = cs_Right;
                    row.CreateCell(6).CellStyle = cs_Right;
                    row.CreateCell(9).CellStyle = cs_Right;
                    row.CreateCell(12).CellStyle = cs_Right;
                    row.CreateCell(15).CellStyle = cs_Right;
                    row.CreateCell(18).CellStyle = cs_Right;
                }

                row.CreateCell(0).SetCellValue(r.Item.TranType); //分類
                row.CreateCell(1).SetCellValue(r.Item.DespatchName);//客戶
                row.CreateCell(2).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.CC)));//清關收入
                row.CreateCell(5).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.FEE2)));//手續費
                row.CreateCell(8).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.Gw)));//重量
                row.CreateCell(11).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.TotalCC)));//清關收入
                row.CreateCell(14).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.TotalFEE2)));//手續費
                row.CreateCell(17).SetCellValue(Math.Ceiling(Convert.ToDouble(r.Item.TotalGw)));//重量

                row.GetCell(0).CellStyle = cs_Letf;
                row.GetCell(1).CellStyle = cs_Letf;
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
                    row.CreateCell(2).SetCellValue("");
                    row.CreateCell(3).SetCellValue(cc);//當日清關收入
                    row.CreateCell(5).SetCellValue("");
                    row.CreateCell(6).SetCellValue(fee);//當日手續費
                    row.CreateCell(8).SetCellValue("");
                    row.CreateCell(9).SetCellValue(gw);//當日清關收入
                    row.CreateCell(11).SetCellValue("");
                    row.CreateCell(12).SetCellValue(totalCC);//去年清關收入
                    row.CreateCell(14).SetCellValue("");
                    row.CreateCell(15).SetCellValue(totalFee);//去年手續費
                    row.CreateCell(17).SetCellValue("");
                    row.CreateCell(18).SetCellValue(totalGw);//去年清關收入

                    row.CreateCell(4).SetCellValue("-100%");
                    row.CreateCell(7).SetCellValue("-100%");
                    row.CreateCell(10).SetCellValue("-100%");
                    row.CreateCell(13).SetCellValue("-100%");
                    row.CreateCell(16).SetCellValue("-100%");
                    row.CreateCell(19).SetCellValue("-100%");

                    row.GetCell(0).CellStyle = cs_Letf;
                    row.GetCell(1).CellStyle = cs_Letf;
                    row.GetCell(2).CellStyle = cs_Letf;
                    row.GetCell(5).CellStyle = cs_Letf;
                    row.GetCell(8).CellStyle = cs_Letf;
                    row.GetCell(11).CellStyle = cs_Letf;
                    row.GetCell(14).CellStyle = cs_Letf;
                    row.GetCell(17).CellStyle = cs_Letf;

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
            row.CreateCell(0).CellStyle = cs_Center;
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
            row.GetCell(4).CellStyle = cs_Percent2_Blue;
            row.GetCell(5).CellStyle = cs_Int_Blue;
            row.GetCell(6).CellStyle = cs_Int_Blue;
            row.GetCell(7).CellStyle = cs_Percent2_Blue;
            row.GetCell(8).CellStyle = cs_Int_Blue;
            row.GetCell(9).CellStyle = cs_Int_Blue;
            row.GetCell(10).CellStyle = cs_Percent2_Blue;
            row.GetCell(11).CellStyle = cs_Int_Blue;
            row.GetCell(12).CellStyle = cs_Int_Blue;
            row.GetCell(13).CellStyle = cs_Percent2_Blue;
            row.GetCell(14).CellStyle = cs_Int_Blue;
            row.GetCell(15).CellStyle = cs_Int_Blue;
            row.GetCell(16).CellStyle = cs_Percent2_Blue;
            row.GetCell(17).CellStyle = cs_Int_Blue;
            row.GetCell(18).CellStyle = cs_Int_Blue;
            row.GetCell(19).CellStyle = cs_Percent2_Blue;

            #endregion
        }

        /// <summary>
        /// 空快、海快營收客戶去年比
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public static Tuple<List<IncomeReportCustomerRateModel>, List<IncomeReportCustomerRateModel>> IncomeReportCustomerRate(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Report_Day2_Rate]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }

            var list = dt.AsEnumerable().Select(r => new IncomeReportCustomerRateModel()
            {
                TranType = r.Field<string>("TranType").Trim(),
                DespatchName = r.Field<string>("DespatchName").Trim(),
                CC = r.Field<decimal?>("CC") ?? 0,
                FEE2 = r.Field<int?>("FEE2") ?? 0,
                Gw = r.Field<decimal?>("Gw") ?? 0,
                BagNumberCount = r.Field<int?>("BagNumberCount") ?? 0,
                Count = r.Field<int?>("TotalCount") ?? 0,
                TotalCC = r.Field<decimal?>("TotalCC") ?? 0,
                TotalFEE2 = r.Field<int?>("TotalFEE2") ?? 0,
                TotalGw = r.Field<decimal?>("TotalGw") ?? 0,
                TotalBagNumberCount = r.Field<int?>("TotalBagNumberCount") ?? 0,
                TotalCount = r.Field<int?>("TotalCount") ?? 0,
            }).ToList();

            var etlList = list.Where(r => r.TranType == "進口空快").ToList();
            var seaList = list.Where(r => r.TranType == "進口海快").ToList();
            return new Tuple<List<IncomeReportCustomerRateModel>, List<IncomeReportCustomerRateModel>>(etlList, seaList);
        }

        static void GetWorkbookStyle(IWorkbook workbook)
        {
            //藍色的Style
            fontB = workbook.CreateFont();
            fontB.FontName = "微軟正黑體";
            fontB.Color = NPOI.SS.UserModel.IndexedColors.Blue.Index;
            fontB.FontHeightInPoints = 18;

            font1 = (XSSFFont)workbook.CreateFont();
            font1.FontName = "微軟正黑體";
            font1.FontHeightInPoints = 18;

            //標題
            cs_Title = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Title.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.SetFont(font1);
            //標題
            cs_Title_Left = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title_Left.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Title_Left.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Title_Left.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.SetFont(font1);


            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.SetFont(font1);

            cs_Letf = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Letf.WrapText = true;//設置換行這個要先設置
            cs_Letf.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Letf.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Letf.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.SetFont(font1);

            cs_Right = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Right.WrapText = true;//設置換行這個要先設置
            cs_Right.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
            cs_Right.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Right.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Right.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Right.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Right.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Right.SetFont(font1);


            cs_Center_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Thick.WrapText = true;//設置換行這個要先設置
            cs_Center_Thick.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Thick.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.SetFont(font1);

            cs_Center_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center_Blue.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.SetFont(fontB);

            cs_Center_Blue_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue_Thick.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue_Thick.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center_Blue_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.SetFont(fontB);

            format = (XSSFDataFormat)workbook.CreateDataFormat();

            cs_Int = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.DataFormat = format.GetFormat("#,##0");
            cs_Int.SetFont(font1);

            cs_Int_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.DataFormat = format.GetFormat("#,##0");
            cs_Int_Thick.SetFont(font1);

            cs_Int_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int_Blue.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue.SetFont(fontB);

            cs_Int_Blue_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int_Blue_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue_Thick.SetFont(fontB);

            cs_Double = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Double.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.DataFormat = format.GetFormat("#,##0.000");
            cs_Double.SetFont(font1);

            cs_Percent = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.DataFormat = format.GetFormat("0.00%");
            cs_Percent.SetFont(font1);

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Percent2.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0%");
            cs_Percent2.SetFont(font1);

            cs_Percent2_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Percent2_Blue.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2_Blue.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2_Blue.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2_Blue.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2_Blue.DataFormat = format.GetFormat("0%");
            cs_Percent2_Blue.SetFont(fontB);


        }
    }
}
