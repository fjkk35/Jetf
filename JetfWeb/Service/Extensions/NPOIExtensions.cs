using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Service.Extensions
{
    public static class NPOIExtensions
    {
        /// <summary>
        /// 讀取 Excel 儲存格內容。
        /// </summary>
        /// <param name="row">Excel 資料列。</param>
        /// <param name="cellIndex">儲存格索引。</param>
        /// <returns>儲存格內容。</returns>
        public static string GetCellData(this IRow row, int cellIndex)
        {
            return GetCellData(row, cellIndex, true);
        }

        /// <summary>
        /// 依指定日期格式化方式讀取 Excel 儲存格內容。
        /// </summary>
        /// <param name="row">Excel 資料列。</param>
        /// <param name="cellIndex">儲存格索引。</param>
        /// <param name="formatDate">是否依儲存格樣式格式化日期。</param>
        /// <returns>儲存格內容。</returns>
        public static string GetCellData(
            this IRow row,
            int cellIndex,
            bool formatDate)
        {
            var cell = row.GetCell(cellIndex);

            // 確保單元格不為 null
            if (cell != null)
            {
                if (!formatDate)
                {
                    return GetCellDataWithoutStyle(cell);
                }

                // 檢查是否為日期類型
                if (cell.CellType == CellType.Numeric &&
                    DateUtil.IsCellDateFormatted(cell))
                {
                    // 如果是日期類型，返回格式化的日期字串
                    return cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss");
                }

                // 否則，返回該單元格的文字內容（若為空則返回空字串）
                return cell.ToString().Trim();
            }

            // 如果單元格為 null，返回空字串
            return string.Empty;
        }

        /// <summary>
        /// 不讀取 Excel 樣式，直接依儲存格型別取得內容。
        /// </summary>
        /// <param name="cell">Excel 儲存格。</param>
        /// <returns>儲存格內容。</returns>
        private static string GetCellDataWithoutStyle(ICell cell)
        {
            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString(
                        CultureInfo.InvariantCulture);
                case CellType.String:
                    return (cell.StringCellValue ?? string.Empty).Trim();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Error:
                    return cell.ErrorCellValue.ToString(
                        CultureInfo.InvariantCulture);
                case CellType.Formula:
                    switch (cell.CachedFormulaResultType)
                    {
                        case CellType.Numeric:
                            return cell.NumericCellValue.ToString(
                                CultureInfo.InvariantCulture);
                        case CellType.String:
                            return (cell.StringCellValue ?? string.Empty).Trim();
                        case CellType.Boolean:
                            return cell.BooleanCellValue.ToString();
                        case CellType.Error:
                            return cell.ErrorCellValue.ToString(
                                CultureInfo.InvariantCulture);
                        default:
                            return string.Empty;
                    }
                default:
                    return string.Empty;
            }
        }

        public static void SetNullableCellValue(this ICell cell, double? value)
        {
            if (value.HasValue)
                cell.SetCellValue(value.Value);
        }

        public static void SetNullableCellValue(this ICell cell, int? value)
        {
            if (value.HasValue)
                cell.SetCellValue(value.Value);
        }
    }

    public static class NpoiStyle
    {
        public static XSSFCellStyle TitleStyle(IWorkbook workbook)
        {
            XSSFCellStyle csTitle = (XSSFCellStyle)workbook.CreateCellStyle();
            IFont font = workbook.CreateFont();
            font.FontHeightInPoints = 14;
            font.Boldweight = (short)FontBoldWeight.Bold;
            csTitle.SetFont(font);
            csTitle.Alignment = HorizontalAlignment.Center;
            csTitle.VerticalAlignment = VerticalAlignment.Center;
            return csTitle;
        }

        public static XSSFCellStyle WrapStyle(IWorkbook workbook)
        {
            var style = (XSSFCellStyle)workbook.CreateCellStyle();
            style.WrapText = true;//設置換行這個要先設置
            return style;
        }

        /// <summary>
        /// 建立標題樣式
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="fontSize">字體大小</param>
        /// <param name="isBold">是否粗體</param>
        /// <returns>樣式</returns>
        public static ICellStyle CreateHeaderStyle(IWorkbook workbook, short fontSize = 12, bool isBold = true)
        {
            ICellStyle headerStyle = workbook.CreateCellStyle();
            IFont headerFont = workbook.CreateFont();
            headerFont.FontHeightInPoints = fontSize;
            headerFont.IsBold = isBold;
            headerStyle.SetFont(headerFont);
            headerStyle.Alignment = HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = VerticalAlignment.Center;
            return headerStyle;
        }

        /// <summary>
        /// 建立標題自動換行樣式
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="fontSize">字體大小</param>
        /// <param name="isBold">是否粗體</param>
        /// <returns></returns>
        public static ICellStyle CreateHeaderWrapTextStyle(IWorkbook workbook, short fontSize = 12, bool isBold = true)
        {
            ICellStyle wrapStyle = workbook.CreateCellStyle();
            wrapStyle.WrapText = true;
            wrapStyle.Alignment = HorizontalAlignment.Center;
            wrapStyle.VerticalAlignment = VerticalAlignment.Center;
            IFont headerFont = workbook.CreateFont();
            headerFont.FontHeightInPoints = fontSize;
            headerFont.IsBold = isBold;
            wrapStyle.SetFont(headerFont);
            wrapStyle.Alignment = HorizontalAlignment.Center;
            wrapStyle.VerticalAlignment = VerticalAlignment.Center;
            return wrapStyle;
        }

        /// <summary>
        /// 建立資料樣式
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="alignment">水平對齊</param>
        /// <returns>樣式</returns>
        public static ICellStyle CreateDataStyle(IWorkbook workbook, HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            ICellStyle dataStyle = workbook.CreateCellStyle();
            dataStyle.Alignment = alignment;
            dataStyle.VerticalAlignment = VerticalAlignment.Center;
            return dataStyle;
        }

        /// <summary>
        /// 建立日期時間樣式
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="dateFormat">日期格式，預設為 yyyy-mm-dd hh:mm:ss</param>
        /// <returns>樣式</returns>
        public static ICellStyle CreateDateTimeStyle(IWorkbook workbook, string dateFormat = "yyyy-mm-dd hh:mm:ss")
        {
            ICellStyle dateStyle = workbook.CreateCellStyle();
            dateStyle.DataFormat = workbook.CreateDataFormat().GetFormat(dateFormat);
            dateStyle.Alignment = HorizontalAlignment.Center;
            dateStyle.VerticalAlignment = VerticalAlignment.Center;
            return dateStyle;
        }

        /// <summary>
        /// 建立數字樣式
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="numberFormat">數字格式，預設為 #,##0</param>
        /// <returns>樣式</returns>
        public static ICellStyle CreateNumberStyle(IWorkbook workbook, string numberFormat = "#,##0")
        {
            ICellStyle numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat(numberFormat);
            numberStyle.Alignment = HorizontalAlignment.Right;
            numberStyle.VerticalAlignment = VerticalAlignment.Center;
            return numberStyle;
        }

        /// <summary>
        /// 建立小數樣式
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="decimalFormat">小數格式，預設為 #,##0.00</param>
        /// <returns>樣式</returns>
        public static ICellStyle CreateDecimalStyle(IWorkbook workbook, string decimalFormat = "#,##0.00")
        {
            ICellStyle decimalStyle = workbook.CreateCellStyle();
            decimalStyle.DataFormat = workbook.CreateDataFormat().GetFormat(decimalFormat);
            decimalStyle.Alignment = HorizontalAlignment.Right;
            decimalStyle.VerticalAlignment = VerticalAlignment.Center;
            return decimalStyle;
        }

        /// <summary>
        /// 建立特殊顏色樣式（如紅色粗體）
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="color">字體顏色</param>
        /// <param name="isBold">是否粗體</param>
        /// <param name="baseStyle">基礎樣式（可選）</param>
        /// <returns>樣式</returns>
        public static ICellStyle CreateColorStyle(IWorkbook workbook, short color, bool isBold = false, ICellStyle baseStyle = null)
        {
            ICellStyle colorStyle = workbook.CreateCellStyle();

            // 如果有基礎樣式，先複製其屬性
            if (baseStyle != null)
            {
                colorStyle.CloneStyleFrom(baseStyle);
            }

            IFont colorFont = workbook.CreateFont();
            colorFont.Color = color;
            colorFont.IsBold = isBold;
            colorStyle.SetFont(colorFont);

            return colorStyle;
        }

        /// <summary>
        /// 建立自動換行樣式
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="alignment">水平對齊</param>
        /// <param name="verticalAlignment">垂直對齊</param>
        /// <returns>樣式</returns>
        public static ICellStyle CreateWrapTextStyle(IWorkbook workbook, HorizontalAlignment alignment = HorizontalAlignment.Center, VerticalAlignment verticalAlignment = VerticalAlignment.Center)
        {
            ICellStyle wrapStyle = workbook.CreateCellStyle();
            wrapStyle.WrapText = true;
            wrapStyle.Alignment = alignment;
            wrapStyle.VerticalAlignment = verticalAlignment;
            return wrapStyle;
        }
    }

    public static class NpoiCell 
    {
        /// <summary>
        /// 建立字串儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnIndex">欄位索引</param>
        /// <param name="value">值</param>
        /// <param name="style">樣式</param>
        public static void CreateCell(IRow row, int columnIndex, string value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            cell.SetCellValue(value ?? "");
            cell.CellStyle = style;
        }

        /// <summary>
        /// 建立日期時間儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnIndex">欄位索引</param>
        /// <param name="value">日期時間值</param>
        /// <param name="style">樣式</param>
        public static void CreateDateTimeCell(IRow row, int columnIndex, DateTime? value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            if (value.HasValue)
            {
                cell.SetCellValue(value.Value);
            }
            else
            {
                cell.SetCellValue("");
            }
            cell.CellStyle = style;
        }

        /// <summary>
        /// 建立日期時間儲存格
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnIndex"></param>
        /// <param name="value"></param>
        /// <param name="style"></param>
        public static void CreateDateTimeCell(IRow row, int columnIndex, string value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            bool isDateTime = DateTime.TryParseExact(
            value,
            new[] { "yyyy/MM/dd", "yyyy.MM.dd"},
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime dateValue
            );

            if (isDateTime)
            {
                cell.SetCellValue(dateValue);
                cell.CellStyle = style;
            }
            else
            {
                cell.SetCellValue(value);
            }
        }


        /// <summary>
        /// 建立整數儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnIndex">欄位索引</param>
        /// <param name="value">整數值</param>
        /// <param name="style">樣式</param>
        public static void CreateIntCell(IRow row, int columnIndex, int? value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            if (value.HasValue)
            {
                cell.SetCellValue(value.Value);
            }
            else
            {
                cell.SetCellValue("");
            }
            cell.CellStyle = style;
        }

        /// <summary>
        /// 建立整數儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnIndex">欄位索引</param>
        /// <param name="value">整數值</param>
        /// <param name="style">樣式</param>
        public static void CreateIntCell(IRow row, int columnIndex, string value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            if (int.TryParse(value, out var result))
            {
                cell.SetCellValue(result);
            }
            else
            {
                cell.SetCellValue(value);
            }
            cell.CellStyle = style;
        }

        /// <summary>
        /// 建立雙精度浮點數儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnIndex">欄位索引</param>
        /// <param name="value">雙精度浮點數值</param>
        /// <param name="style">樣式</param>
        public static void CreateDoubleCell(IRow row, int columnIndex, double? value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            if (value.HasValue)
            {
                cell.SetCellValue(value.Value);
            }
            else
            {
                cell.SetCellValue("");
            }
            cell.CellStyle = style;
        }

        /// <summary>
        /// 建立雙精度浮點數儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnIndex">欄位索引</param>
        /// <param name="value">雙精度浮點數值</param>
        /// <param name="style">樣式</param>
        public static void CreateDoubleCell(IRow row, int columnIndex, string value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            if (double.TryParse(value, out var result))
            {
                cell.SetCellValue(result);
            }
            else
            {
                cell.SetCellValue(value);
            }
            cell.CellStyle = style;
        }

        /// <summary>
        /// 建立布林值儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnIndex">欄位索引</param>
        /// <param name="value">布林值</param>
        /// <param name="style">樣式</param>
        public static void CreateBooleanCell(IRow row, int columnIndex, bool? value, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            if (value.HasValue)
            {
                cell.SetCellValue(value.Value);
            }
            else
            {
                cell.SetCellValue("");
            }
            cell.CellStyle = style;
        }

        /// <summary>
        /// 批量建立表頭儲存格
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="headers">表頭文字陣列</param>
        /// <param name="style">樣式</param>
        public static void CreateHeaderCells(IRow row, string[] headers, ICellStyle style)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                CreateCell(row, i, headers[i], style);
            }
        }

        public static void CreateHeaderCells(IRow row, List<string> headers, ICellStyle style)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                CreateCell(row, i, headers[i], style);
            }
        }

        public static ICell GetCell(IRow row, int columnIndex)
        {
            return row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);
        }

        /// <summary>
        /// AutoSize 後再放大（可調倍率）
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="columnCount"></param>
        /// <param name="fixedWidths"></param>
        /// <param name="scale"></param>
        /// <param name="minWidth"></param>
        public static void AutoSizeColumns(this ISheet sheet, int columnCount, Dictionary<int, int> fixedWidths = null, double scale = 1.0, int minWidth = 0)
        {
            minWidth = minWidth > 0 ? minWidth * 256 : 0;

            for (int i = 0; i < columnCount; i++)
            {
                // ① 有指定固定寬度 → 直接用
                if (fixedWidths != null && fixedWidths.TryGetValue(i, out int fixedWidth))
                {
                    sheet.SetColumnWidth(i, fixedWidth);
                    continue;
                }

                // ② 其餘欄位 → AutoSize
                sheet.AutoSizeColumn(i);

                int width = sheet.GetColumnWidth(i);

                if (scale != 1.0)
                    width = (int)(width * scale);

                if (minWidth > 0 && width < minWidth)
                    width = minWidth;

                // Excel 最大欄寬限制
                int maxWidth = 255 * 256; 
                if (width > maxWidth)
                    width = maxWidth;

                sheet.SetColumnWidth(i, width);
            }
        }
    }
}
