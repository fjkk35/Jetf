using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.UnpackingStatistics
{
    // 改為 public 供外部方法回傳使用
    public class UnpackingStatisticsModel
    {
        public DateTime Date { get; set; }
        public string DataType { get; set; }
        public string Customer { get; set; }
        public int TotalCount { get; set; }
    }

    public class UnpackingStatisticsService : _BaseService
    {
        class SheetData
        {
            public string DataType { get; set; }
            public List<PivotRow> Rows { get; set; }
            public List<string> Customers { get; set; }
            public int GrandTotal { get; set; }
        }

        public class PivotRow
        {
            public string Date { get; set; } // MM/dd
            public int DayTotal { get; set; }
            public Dictionary<string, int> CustomerValues { get; set; } = new Dictionary<string, int>();
        }

        public List<UnpackingStatisticsModel> GetRaw(string startDate, string endDate)
        {
            var sql = @"with B6F_UNPACKING_UPLOAD as (
                        SELECT DATATYPE,CAST(SCAN_UPLOAD_TIME2 AS DATE) Date,CUSTOMER FROM [jetf].[dbo].[B6F_UNPACKING_UPLOAD] 
                        WHERE 
                        SCAN_UPLOAD_TIME2 between @StartDate and @EndDate
                        )
                        select Date,DataType,Customer,Count(*) as TotalCount from B6F_UNPACKING_UPLOAD
                        group by DATATYPE,Date,CUSTOMER";
            return conn.Query<UnpackingStatisticsModel>(sql, new { StartDate = startDate + " 00:00:00", EndDate = endDate + " 23:59:59" }, commandTimeout: 600).ToList();
        }

        public object GetPivotData(string startDate, string endDate)
        {
            var list = GetRaw(startDate, endDate);
            var result = new List<SheetData>();

            // 解析日期範圍
            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate);
            var allDates = new List<DateTime>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                allDates.Add(date);
            }

            foreach (var g in list.GroupBy(r => r.DataType))
            {
                var customers = g.Select(r => r.Customer).Distinct().OrderBy(r => r).ToList();
                var rows = new List<PivotRow>();
                
                // 對每個日期建立資料列，沒有資料的日期顯示 0
                foreach (var date in allDates.OrderBy(d => d))
                {
                    var dateData = g.Where(x => x.Date.Date == date.Date).ToList();
                    
                    var row = new PivotRow
                    {
                        Date = date.ToString("MM/dd"),
                        DayTotal = dateData.Sum(x => x.TotalCount)
                    };
                    
                    foreach (var c in customers)
                    {
                        row.CustomerValues[c] = dateData.Where(x => x.Customer == c).Sum(x => x.TotalCount);
                    }
                    rows.Add(row);
                }
                
                result.Add(new SheetData
                {
                    DataType = g.Key,
                    Customers = customers,
                    Rows = rows,
                    GrandTotal = rows.Sum(r => r.DayTotal)
                });
            }

            // 如果沒有任何資料，但仍需要顯示日期區間
            if (!result.Any() && allDates.Any())
            {
                result.Add(new SheetData
                {
                    DataType = "無資料",
                    Customers = new List<string>(),
                    Rows = allDates.Select(date => new PivotRow
                    {
                        Date = date.ToString("MM/dd"),
                        DayTotal = 0,
                        CustomerValues = new Dictionary<string, int>()
                    }).ToList(),
                    GrandTotal = 0
                });
            }

            return result;
        }

        public IWorkbook GetWorkbook(string startDate, string endDate)
        {
            var pivot = (List<SheetData>)GetPivotData(startDate, endDate);
            IWorkbook wb = new XSSFWorkbook();
            var style = BuildStyles(wb);
            foreach (var sheetData in pivot)
            {
                var sheet = wb.CreateSheet(sheetData.DataType);
                int colIndex = 0;
                var header = sheet.CreateRow(0);
                header.CreateCell(colIndex).SetCellValue("日期"); colIndex++;
                header.CreateCell(colIndex).SetCellValue("當日合計"); colIndex++;
                foreach (var c in sheetData.Customers)
                {
                    header.CreateCell(colIndex).SetCellValue(c); colIndex++;
                }
                //套 style
                for (int i = 0; i < colIndex; i++) header.GetCell(i).CellStyle = style.Header;

                int rowIdx = 1;
                foreach (var r in sheetData.Rows)
                {
                    var row = sheet.CreateRow(rowIdx++);
                    int ci = 0;
                    row.CreateCell(ci).SetCellValue(r.Date); row.GetCell(ci).CellStyle = style.Text; ci++;
                    row.CreateCell(ci).SetCellValue(r.DayTotal); row.GetCell(ci).CellStyle = style.Int; ci++;
                    foreach (var c in sheetData.Customers)
                    {
                        row.CreateCell(ci).SetCellValue(r.CustomerValues[c]);
                        row.GetCell(ci).CellStyle = style.Int;
                        ci++;
                    }
                }
                // 小計列
                var totalRow = sheet.CreateRow(sheetData.Rows.Count + 1);
                int tc = 0;
                totalRow.CreateCell(tc).SetCellValue("小計"); totalRow.GetCell(tc).CellStyle = style.SubTotal; tc++;
                totalRow.CreateCell(tc).SetCellValue(sheetData.GrandTotal); totalRow.GetCell(tc).CellStyle = style.SubTotalInt; tc++;
                foreach (var c in sheetData.Customers)
                {
                    var sum = sheetData.Rows.Sum(r => r.CustomerValues[c]);
                    totalRow.CreateCell(tc).SetCellValue(sum);
                    totalRow.GetCell(tc).CellStyle = style.SubTotalInt;
                    tc++;
                }
                
                // 調整欄位寬度 - 根據內容動態調整
                for (int i = 0; i < tc; i++)
                {
                    if (i == 0) // 日期欄
                    {
                        sheet.SetColumnWidth(i, 3000); // MM/dd 格式不需要太寬
                    }
                    else if (i == 1) // 當日合計欄
                    {
                        sheet.SetColumnWidth(i, 4000);
                    }
                    else // 客戶名稱欄位
                    {
                        // 根據客戶名稱長度動態調整寬度
                        var customerName = sheetData.Customers[i - 2];
                        var width = Math.Max(4000, customerName.Length * 500 + 2000); // 基礎寬度 + 字元長度調整
                        width = Math.Min(width, 8000); // 設定最大寬度避免過寬
                        sheet.SetColumnWidth(i, width);
                    }
                }
            }
            return wb;
        }

        #region Styles
        class Styles
        {
            public ICellStyle Header { get; set; }
            public ICellStyle Int { get; set; }
            public ICellStyle Text { get; set; }
            public ICellStyle SubTotal { get; set; }
            public ICellStyle SubTotalInt { get; set; }
        }
        Styles BuildStyles(IWorkbook wb)
        {
            var font = wb.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 11;
            var bold = wb.CreateFont();
            bold.FontName = "微軟正黑體"; bold.IsBold = true; bold.FontHeightInPoints = 11;

            ICellStyle header = wb.CreateCellStyle(); header.SetFont(bold); header.Alignment = HorizontalAlignment.Center; header.VerticalAlignment = VerticalAlignment.Center;
            ICellStyle txt = wb.CreateCellStyle(); txt.SetFont(font);
            ICellStyle intStyle = wb.CreateCellStyle(); intStyle.SetFont(font); intStyle.DataFormat = wb.CreateDataFormat().GetFormat("#,##0");
            ICellStyle sub = wb.CreateCellStyle(); sub.SetFont(bold); sub.Alignment = HorizontalAlignment.Center;
            ICellStyle subInt = wb.CreateCellStyle(); subInt.SetFont(bold); subInt.DataFormat = wb.CreateDataFormat().GetFormat("#,##0");

            return new Styles { Header = header, Int = intStyle, Text = txt, SubTotal = sub, SubTotalInt = subInt };
        }
        #endregion
    }
}
