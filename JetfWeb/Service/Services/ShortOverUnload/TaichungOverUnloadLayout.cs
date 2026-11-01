using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NPOI.OpenXmlFormats.Dml;
using NPOI.OpenXmlFormats.Dml.Spreadsheet;
using CT_ShapeProperties = NPOI.OpenXmlFormats.Dml.Spreadsheet.CT_ShapeProperties;
using CT_NonVisualDrawingProps = NPOI.OpenXmlFormats.Dml.Spreadsheet.CT_NonVisualDrawingProps;
using CT_NonVisualDrawingShapeProps = NPOI.OpenXmlFormats.Dml.Spreadsheet.CT_NonVisualDrawingShapeProps;
using System;
using System.IO;
using System.Linq;

namespace Service.Services.ShortOverUnload
{
    /// <summary>溢卸參考格式的 NPOI 版面定義；執行時不讀取範本檔案。</summary>
    internal static class TaichungOverUnloadLayout
    {
        private static readonly byte[] BillOfLadingSeal = LoadBillOfLadingSeal();
        private const int ReferenceStyleCount = 171;

        /// <summary>建立共用字型及樣式，保留參考格式並移除所有背景填色。</summary>
        internal static ICellStyle[] CreateStyles(XSSFWorkbook workbook)
        {
            // Excel 依工作簿預設字型計算欄寬；沿用參考檔的預設字型，確保繪圖與儲存格對齊。
            var defaultFont = (XSSFFont)workbook.GetFontAt(0);
            defaultFont.FontName = "新細明體";
            defaultFont.FontHeightInPoints = 12;
            defaultFont.Family = 2;
            defaultFont.GetCTFont().AddNewCharset().val = 136;

            var fonts = new XSSFFont[37];
            fonts[0] = Font(workbook, "新細明體", 12, false, false, 136, 2, "");
            fonts[1] = Font(workbook, "新細明體", 9, false, false, 136, 2, "");
            fonts[2] = Font(workbook, "新細明體", 12, false, false, 136, 1, "FF0000");
            fonts[3] = Font(workbook, "標楷體", 16, false, false, 136, 4, "");
            fonts[4] = Font(workbook, "標楷體", 26, true, false, 136, 4, "");
            fonts[4].Underline = FontUnderlineType.Single;
            fonts[5] = Font(workbook, "標楷體", 16, false, false, 136, 4, "");
            fonts[6] = Font(workbook, "新細明體", 12, false, false, 136, 1, "");
            fonts[7] = Font(workbook, "標楷體", 22, false, false, 136, 4, "");
            fonts[8] = Font(workbook, "標楷體", 18, false, false, 136, 4, "");
            fonts[9] = Font(workbook, "標楷體", 12, false, false, 136, 4, "");
            fonts[10] = Font(workbook, "標楷體", 11, false, false, 136, 4, "");
            fonts[11] = Font(workbook, "標楷體", 14, false, false, 136, 4, "");
            fonts[12] = Font(workbook, "標楷體", 10, false, false, 136, 4, "");
            fonts[13] = Font(workbook, "標楷體", 12, false, false, 136, 4, "");
            fonts[14] = Font(workbook, "標楷體", 11, true, false, 136, 4, "");
            fonts[15] = Font(workbook, "標楷體", 14, true, false, 136, 4, "");
            fonts[16] = Font(workbook, "標楷體", 16, true, false, 136, 4, "");
            fonts[17] = Font(workbook, "標楷體", 26, false, false, 136, 4, "");
            fonts[18] = Font(workbook, "標楷體", 18, false, false, 136, 4, "");
            fonts[19] = Font(workbook, "標楷體", 22, false, false, 136, 4, "");
            fonts[20] = Font(workbook, "Times New Roman", 10, false, false, 0, 1, "000000");
            fonts[21] = Font(workbook, "Times New Roman", 12, true, false, 0, 1, "000000");
            fonts[22] = Font(workbook, "Microsoft JhengHei", 12, true, false, 136, 2, "");
            fonts[23] = Font(workbook, "Microsoft JhengHei", 12, true, false, 0, 2, "");
            fonts[24] = Font(workbook, "細明體", 9, false, false, 136, 3, "");
            fonts[25] = Font(workbook, "Microsoft JhengHei", 10, true, false, 0, 2, "");
            fonts[26] = Font(workbook, "Microsoft JhengHei", 10, true, false, 136, 2, "");
            fonts[27] = Font(workbook, "Times New Roman", 11, true, false, 0, 1, "000000");
            fonts[28] = Font(workbook, "Microsoft JhengHei", 11, true, false, 0, 2, "");
            fonts[29] = Font(workbook, "Microsoft YaHei", 12, true, false, 0, 2, "0000FF");
            fonts[30] = Font(workbook, "Times New Roman", 12, true, false, 0, 2, "000000");
            fonts[31] = Font(workbook, "Microsoft YaHei", 12, true, false, 134, 2, "0000FF");
            fonts[32] = Font(workbook, "細明體", 12, true, false, 136, 1, "000000");
            fonts[33] = Font(workbook, "Times New Roman", 12, true, false, 134, 2, "000000");
            fonts[34] = Font(workbook, "Microsoft YaHei", 12, true, false, 134, 2, "000000");
            fonts[35] = Font(workbook, "Microsoft YaHei", 12, true, false, 134, 2, "");
            fonts[36] = Font(workbook, "Microsoft JhengHei", 12, true, false, 136, 1, "000000");
            var styles = new ICellStyle[ReferenceStyleCount + fonts.Length];
            styles[0] = Style(workbook, fonts[0], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[1] = Style(workbook, fonts[0], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[2] = Style(workbook, fonts[0], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[3] = Style(workbook, fonts[2], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[4] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 14, null, false);
            styles[5] = Style(workbook, fonts[3], 1, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[6] = Style(workbook, fonts[3], 1, 1, 1, 1, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[7] = Style(workbook, fonts[3], 1, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[8] = Style(workbook, fonts[3], 1, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 14, null, false);
            styles[9] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[10] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[11] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[12] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[13] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 14, null, false);
            styles[14] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[15] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 14, null, false);
            styles[16] = Style(workbook, fonts[4], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[17] = Style(workbook, fonts[5], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[18] = Style(workbook, fonts[6], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[19] = Style(workbook, fonts[7], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[20] = Style(workbook, fonts[0], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[21] = Style(workbook, fonts[5], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[22] = Style(workbook, fonts[8], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[23] = Style(workbook, fonts[9], 1, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[24] = Style(workbook, fonts[9], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[25] = Style(workbook, fonts[9], 1, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[26] = Style(workbook, fonts[9], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[27] = Style(workbook, fonts[9], 1, 1, 1, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[28] = Style(workbook, fonts[9], 1, 1, 1, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[29] = Style(workbook, fonts[9], 1, 1, 1, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[30] = Style(workbook, fonts[9], 1, 1, 1, 1, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[31] = Style(workbook, fonts[9], 1, 1, 1, 1, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[32] = Style(workbook, fonts[8], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 14, null, false);
            styles[33] = Style(workbook, fonts[0], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[34] = Style(workbook, fonts[9], 1, 1, 1, 1, HorizontalAlignment.General, VerticalAlignment.Bottom, true, 0, 0, null, false);
            styles[35] = Style(workbook, fonts[10], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[36] = Style(workbook, fonts[10], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[37] = Style(workbook, fonts[11], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[38] = Style(workbook, fonts[11], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[39] = Style(workbook, fonts[12], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[40] = Style(workbook, fonts[0], 0, 0, 2, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[41] = Style(workbook, fonts[3], 0, 0, 2, 0, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[42] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[43] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 176, "\"Ｃ\"\\:@", false);
            styles[44] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[45] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[46] = Style(workbook, fonts[0], 2, 1, 1, 1, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[47] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[48] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[49] = Style(workbook, fonts[0], 2, 1, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[50] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 49, null, false);
            styles[51] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 49, null, false);
            styles[52] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 49, null, false);
            styles[53] = Style(workbook, fonts[13], 2, 1, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            // 頁籤四原申報區補齊左右邊界與各列橫線，避免沿用範例中缺少的框線。
            styles[54] = Style(workbook, fonts[11], 1, 2, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[55] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[56] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 49, null, false);
            styles[57] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[58] = Style(workbook, fonts[11], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[59] = Style(workbook, fonts[11], 1, 2, 2, 1, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[60] = Style(workbook, fonts[11], 1, 1, 2, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[61] = Style(workbook, fonts[11], 1, 1, 2, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 49, null, false);
            styles[62] = Style(workbook, fonts[11], 1, 1, 2, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 14, null, false);
            styles[63] = Style(workbook, fonts[11], 1, 1, 2, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[64] = Style(workbook, fonts[13], 2, 1, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 49, null, false);
            styles[65] = Style(workbook, fonts[14], 1, 2, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[66] = Style(workbook, fonts[14], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[67] = Style(workbook, fonts[14], 1, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 49, null, false);
            styles[68] = Style(workbook, fonts[14], 1, 1, 1, 1, HorizontalAlignment.General, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[69] = Style(workbook, fonts[14], 2, 1, 1, 1, HorizontalAlignment.General, VerticalAlignment.Center, true, 0, 49, null, false);
            styles[70] = Style(workbook, fonts[0], 0, 1, 0, 1, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[71] = Style(workbook, fonts[0], 1, 0, 0, 1, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[72] = Style(workbook, fonts[0], 1, 1, 0, 1, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[73] = Style(workbook, fonts[0], 2, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[74] = Style(workbook, fonts[0], 0, 1, 1, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[75] = Style(workbook, fonts[3], 1, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[76] = Style(workbook, fonts[3], 1, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 49, null, false);
            styles[77] = Style(workbook, fonts[15], 2, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[78] = Style(workbook, fonts[0], 0, 1, 2, 1, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[79] = Style(workbook, fonts[16], 1, 1, 2, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[80] = Style(workbook, fonts[16], 1, 1, 2, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[81] = Style(workbook, fonts[16], 1, 1, 2, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[82] = Style(workbook, fonts[10], 0, 0, 2, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[83] = Style(workbook, fonts[3], 0, 0, 2, 0, HorizontalAlignment.General, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[84] = Style(workbook, fonts[17], 0, 0, 2, 0, HorizontalAlignment.Left, VerticalAlignment.Bottom, false, 0, 0, null, false);
            styles[85] = Style(workbook, fonts[17], 2, 0, 2, 0, HorizontalAlignment.Left, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[86] = Style(workbook, fonts[18], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 14, null, false);
            styles[87] = Style(workbook, fonts[18], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[88] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[89] = Style(workbook, fonts[19], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, false, 0, 0, null, false);
            styles[90] = Style(workbook, fonts[3], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Center, false, 0, 49, null, false);
            styles[91] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, false, 0, 0, null, false);
            styles[92] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[93] = Style(workbook, fonts[22], 1, 1, 0, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[94] = Style(workbook, fonts[21], 0, 1, 0, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[95] = Style(workbook, fonts[21], 0, 0, 0, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[96] = Style(workbook, fonts[23], 1, 0, 0, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[97] = Style(workbook, fonts[23], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[98] = Style(workbook, fonts[22], 1, 1, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[99] = Style(workbook, fonts[21], 0, 1, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[100] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[101] = Style(workbook, fonts[23], 1, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[102] = Style(workbook, fonts[21], 0, 1, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[103] = Style(workbook, fonts[23], 1, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[104] = Style(workbook, fonts[21], 0, 0, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[105] = Style(workbook, fonts[22], 1, 1, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[106] = Style(workbook, fonts[21], 0, 1, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[107] = Style(workbook, fonts[23], 1, 0, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[108] = Style(workbook, fonts[23], 0, 0, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[109] = Style(workbook, fonts[23], 0, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[110] = Style(workbook, fonts[22], 1, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[111] = Style(workbook, fonts[25], 0, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[112] = Style(workbook, fonts[25], 0, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[113] = Style(workbook, fonts[26], 1, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[114] = Style(workbook, fonts[27], 0, 1, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[115] = Style(workbook, fonts[27], 1, 0, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[116] = Style(workbook, fonts[21], 0, 1, 0, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[117] = Style(workbook, fonts[28], 1, 0, 1, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[118] = Style(workbook, fonts[22], 0, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[119] = Style(workbook, fonts[22], 0, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[120] = Style(workbook, fonts[22], 1, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[121] = Style(workbook, fonts[22], 0, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[122] = Style(workbook, fonts[22], 1, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[123] = Style(workbook, fonts[21], 1, 1, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[124] = Style(workbook, fonts[23], 0, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[125] = Style(workbook, fonts[23], 1, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[126] = Style(workbook, fonts[23], 1, 1, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[127] = Style(workbook, fonts[21], 0, 1, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[128] = Style(workbook, fonts[21], 0, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[129] = Style(workbook, fonts[21], 1, 0, 1, 1, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[130] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.General, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[131] = Style(workbook, fonts[23], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[132] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[133] = Style(workbook, fonts[23], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[134] = Style(workbook, fonts[23], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[135] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[136] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[137] = Style(workbook, fonts[30], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[138] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[139] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[140] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 4, null, false);
            styles[141] = Style(workbook, fonts[31], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[142] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[143] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Right, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[144] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[145] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[146] = Style(workbook, fonts[23], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[147] = Style(workbook, fonts[30], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[148] = Style(workbook, fonts[23], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[149] = Style(workbook, fonts[30], 0, 0, 0, 0, HorizontalAlignment.Right, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[150] = Style(workbook, fonts[29], 0, 0, 0, 0, HorizontalAlignment.Right, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[151] = Style(workbook, fonts[30], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[152] = Style(workbook, fonts[23], 0, 0, 0, 0, HorizontalAlignment.Right, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[153] = Style(workbook, fonts[30], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[154] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Right, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[155] = Style(workbook, fonts[22], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[156] = Style(workbook, fonts[22], 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[157] = Style(workbook, fonts[33], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[158] = Style(workbook, fonts[34], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[159] = Style(workbook, fonts[30], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[160] = Style(workbook, fonts[35], 0, 1, 0, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[161] = Style(workbook, fonts[35], 0, 0, 0, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[162] = Style(workbook, fonts[35], 1, 0, 0, 1, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[163] = Style(workbook, fonts[29], 0, 1, 0, 1, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[164] = Style(workbook, fonts[29], 1, 0, 0, 1, HorizontalAlignment.Center, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[165] = Style(workbook, fonts[35], 0, 1, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[166] = Style(workbook, fonts[35], 0, 0, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[167] = Style(workbook, fonts[35], 1, 0, 1, 0, HorizontalAlignment.Center, VerticalAlignment.Center, true, 0, 0, null, false);
            styles[168] = Style(workbook, fonts[21], 0, 1, 1, 0, HorizontalAlignment.General, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[169] = Style(workbook, fonts[23], 1, 0, 1, 0, HorizontalAlignment.General, VerticalAlignment.Top, true, 0, 0, null, false);
            styles[170] = Style(workbook, fonts[21], 0, 0, 0, 0, HorizontalAlignment.Left, VerticalAlignment.Top, true, 0, 0, null, false);
            // NPOI 可能重用相同字型，不能以固定偏移量推算 RichText 的字型索引。
            // 額外樣式保存每個參考字型的實際索引，不套用至儲存格。
            for (var index = 0; index < fonts.Length; index++)
            {
                styles[ReferenceStyleCount + index] = workbook.CreateCellStyle();
                styles[ReferenceStyleCount + index].SetFont(fonts[index]);
            }
            return styles;
        }

        /// <summary>依表別建立空白版面，包含欄寬、列高、固定文字、框線及列印設定。</summary>
        internal static ISheet CreateSheet(XSSFWorkbook workbook, string name, int layout, ICellStyle[] styles)
        {
            var sheet = (XSSFSheet)workbook.CreateSheet(name);
            switch (layout)
            {
                case 0:
                {
                    sheet.DefaultColumnWidth = 9;
                    sheet.DefaultRowHeightInPoints = 16.2f;
                    sheet.GetCTWorksheet().sheetFormatPr.defaultRowHeight = 16.2;
                    Columns(sheet, 0, 0, 6741, styles[1]);
                    Columns(sheet, 1, 1, 6627, styles[1]);
                    Columns(sheet, 2, 2, 3925, styles[1]);
                    Columns(sheet, 3, 3, 3868, styles[1]);
                    Columns(sheet, 4, 4, 6087, styles[1]);
                    Rows(sheet, 0, 0, 67.5f, styles, new[] { 16, 2, 2, 2, 2 });
                    Rows(sheet, 1, 1, 67.5f, styles, new[] { 9, 9, 9, 9, 9 });
                    Rows(sheet, 2, 2, 45f, styles, new[] { 9, 15, 9, 15, 1 });
                    Rows(sheet, 3, 3, 45f, styles, new[] { 9, 14, 9, 13, 1 });
                    Rows(sheet, 4, 4, 45f, styles, new[] { 9, 12, 1, 1, 1 });
                    Rows(sheet, 5, 5, 45f, styles, new[] { 11, 2, 2, 2, 2 });
                    Rows(sheet, 6, 6, 45f, styles, new[] { 9, 10, 9, 1, 1 });
                    Rows(sheet, 7, 7, 34.5f, styles, new[] { 7, 7, 8, 7, 4 });
                    Rows(sheet, 8, 21, 20.25f, styles, new[] { 6, 5, 5, 5, 4 });
                    Rows(sheet, 22, 22, 34.5f, styles, new[] { 0, 0, 0, 0, 0 });
                    Rows(sheet, 23, 23, 20.25f, styles, new[] { 3, 3, 3, 3, 3 });
                    Rows(sheet, 24, 24, 20.25f, styles, new[] { 17, 18, 18, 3, 3 });
                    // B26 為 22 點字型，增加列高以完整顯示關別名稱。
                    Rows(sheet, 25, 25, 30f, styles, new[] { 18, 19, 20, 3, 3 });
                    Rows(sheet, 26, 26, 20.25f, styles, new[] { 18, 18, 18, 3, 3 });
                    Rows(sheet, 27, 27, 20.25f, styles, new[] { 21, 20, 18, 3, 3 });
                    Rows(sheet, 28, 28, 45f, styles, new[] { 3, 3, 3, 3, 3 });
                    Rows(sheet, 29, 30, 26.25f, styles, new[] { 18, 18, 22, 3, 3 });
                    Rows(sheet, 31, 31, 30f, styles, new[] { 3, 3, 3, 3, 3 });
                    Rows(sheet, 32, 32, 16.5f, styles, new[] { 32, 33, 33, 33, 33 });
                    Rows(sheet, 33, 33, 45f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 34, 34, 16.5f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 35, 36, 25.5f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 37, 37, 16.5f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 38, 38, 21f, styles, new[] { 1, 1, 1, 1, 1 });
                    foreach (var range in new[] { "B26:C26", "A33:E33", "A28:B28", "A1:E1", "A6:E6" })
                        sheet.AddMergedRegion(CellRangeAddress.ValueOf(range));
                    Cell(sheet, "A1").SetCellValue("溢卸申請說明書");
                    Cell(sheet, "A2").SetCellValue("本公司捷豐國際物流股份有限公司承攬進口快遞貨物一批，進口資訊如下");
                    Cell(sheet, "A3").SetCellValue("進口日期");
                    Cell(sheet, "C3").SetCellValue("船名");
                    Cell(sheet, "A4").SetCellValue("航次");
                    Cell(sheet, "C4").SetCellValue("櫃　　號：");
                    Cell(sheet, "A5").SetCellValue("主提單號：");
                    Cell(sheet, "A7").SetCellValue("共計艙單數:");
                    Cell(sheet, "C7").SetCellValue("票");
                    Cell(sheet, "A8").SetCellValue("分提單號碼");
                    Cell(sheet, "B8").SetCellValue("裝貨單號");
                    Cell(sheet, "C8").SetCellValue("申報件數");
                    Cell(sheet, "D8").SetCellValue("實到件數");
                    Cell(sheet, "A25").SetCellValue("此致");
                    Cell(sheet, "B26").SetCellValue("財政部關務署台中關");
                    Cell(sheet, "C30").SetCellValue("立書人：捷豐國際物流股份有限公司");
                    Cell(sheet, "C31").SetCellValue("聯絡人：02-25148667");
                    sheet.SetMargin(MarginType.LeftMargin, 0.70866141732283472);
                    sheet.SetMargin(MarginType.RightMargin, 0.70866141732283472);
                    sheet.SetMargin(MarginType.TopMargin, 0.74803149606299213);
                    sheet.SetMargin(MarginType.BottomMargin, 0.74803149606299213);
                    sheet.PrintSetup.HeaderMargin = 0.15;
                    sheet.PrintSetup.FooterMargin = 0.1;
                    sheet.PrintSetup.PaperSize = 9;
                    sheet.PrintSetup.Scale = 79;
                    sheet.PrintSetup.Landscape = false;
                    sheet.SetZoom(85);
                    sheet.DisplayGridlines = true;
                    break;
                }
                case 1:
                {
                    sheet.DefaultColumnWidth = 9;
                    sheet.DefaultRowHeightInPoints = 16.2f;
                    sheet.GetCTWorksheet().sheetFormatPr.defaultRowHeight = 16.2;
                    Columns(sheet, 0, 0, 5831, styles[1]);
                    Columns(sheet, 1, 1, 4579, styles[1]);
                    Columns(sheet, 2, 2, 2417, styles[1]);
                    Columns(sheet, 3, 3, 3043, styles[1]);
                    Columns(sheet, 4, 4, 2986, styles[1]);
                    Columns(sheet, 5, 5, 9443, styles[1]);
                    Columns(sheet, 6, 6, 2161, styles[1]);
                    Columns(sheet, 7, 7, 3555, styles[1]);
                    Columns(sheet, 8, 8, 2417, styles[1]);
                    Columns(sheet, 9, 9, 10069, styles[1]);
                    Columns(sheet, 10, 10, 2588, styles[1]);
                    Columns(sheet, 11, 11, 4124, styles[1]);
                    Columns(sheet, 12, 12, 3072, styles[1]);
                    Rows(sheet, 0, 0, 16.5f, styles, new[] { 31, 30, 30, 30, 30, 30, 30, 30, 30, 1, 1, 1, 1 });
                    Rows(sheet, 1, 1, 16.5f, styles, new[] { 30, 34, 30, 30, 30, 30, 30, 30, 30, 1, 1, 1, 1 });
                    Rows(sheet, 2, 2, 16.5f, styles, new[] { 29, 29, 27, 27, 27, 28, 27, 27, 27, 1, 1, 1, 1 });
                    Rows(sheet, 3, 3, 16.5f, styles, new[] { 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 25 });
                    Rows(sheet, 4, 49, 16.5f, styles, new[] { 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 23 });
                    Cell(sheet, "A1").SetCellValue("海運快遞進口貨物清單");
                    Cell(sheet, "D1").SetCellValue("文件版次：");
                    Cell(sheet, "E1").SetCellValue("BIC_2.3");
                    Cell(sheet, "A2").SetCellValue("主提單號碼");
                    Cell(sheet, "B2").SetCellValue("海關通\n關號碼");
                    Cell(sheet, "C2").SetCellValue("船舶航次");
                    Cell(sheet, "D2").SetCellValue("船舶呼號");
                    Cell(sheet, "E2").SetCellValue("船公司代碼");
                    Cell(sheet, "F2").SetCellValue("卸存地\n點代碼");
                    Cell(sheet, "G2").SetCellValue("裝貨港");
                    Cell(sheet, "H2").SetCellValue("暫存地\n點代碼");
                    Cell(sheet, "I2").SetCellValue("船機代碼");
                    Cell(sheet, "A4").SetCellValue("分提單號碼");
                    Cell(sheet, "B4").SetCellValue("快遞業者\n統一編號");
                    Cell(sheet, "C4").SetCellValue("毛重");
                    Cell(sheet, "D4").SetCellValue("件數");
                    Cell(sheet, "E4").SetCellValue("件數單位");
                    Cell(sheet, "F4").SetCellValue("貨物名稱");
                    Cell(sheet, "G4").SetCellValue("數量");
                    Cell(sheet, "H4").SetCellValue("單價金額");
                    Cell(sheet, "I4").SetCellValue("進口人英文名稱");
                    Cell(sheet, "J4").SetCellValue("進口人英文地址");
                    Cell(sheet, "K4").SetCellValue("貨櫃種類");
                    Cell(sheet, "L4").SetCellValue("貨櫃號碼");
                    Cell(sheet, "M4").SetCellValue("封條號碼");
                    sheet.SetMargin(MarginType.LeftMargin, 0.7);
                    sheet.SetMargin(MarginType.RightMargin, 0.7);
                    sheet.SetMargin(MarginType.TopMargin, 0.75);
                    sheet.SetMargin(MarginType.BottomMargin, 0.75);
                    sheet.PrintSetup.HeaderMargin = 0.15;
                    sheet.PrintSetup.FooterMargin = 0.1;
                    sheet.PrintSetup.PaperSize = 9;
                    sheet.PrintSetup.Scale = 58;
                    sheet.PrintSetup.Landscape = true;
                    sheet.SetZoom(70);
                    sheet.DisplayGridlines = true;
                    break;
                }
                case 2:
                {
                    sheet.DefaultColumnWidth = 9;
                    sheet.DefaultRowHeightInPoints = 16.2f;
                    sheet.GetCTWorksheet().sheetFormatPr.defaultRowHeight = 16.2;
                    Columns(sheet, 0, 0, 6741, styles[1]);
                    Columns(sheet, 1, 1, 6627, styles[1]);
                    Columns(sheet, 2, 2, 5432, styles[1]);
                    Columns(sheet, 3, 3, 2673, styles[1]);
                    Columns(sheet, 4, 4, 2133, styles[1]);
                    Columns(sheet, 5, 5, 3925, styles[1]);
                    Columns(sheet, 6, 6, 3356, styles[1]);
                    Columns(sheet, 7, 7, 6656, styles[1]);
                    Columns(sheet, 8, 8, 4039, styles[1]);
                    Columns(sheet, 9, 9, 12288, styles[1]);
                    Columns(sheet, 10, 10, 3072, styles[1]);
                    Columns(sheet, 11, 11, 3356, styles[1]);
                    Rows(sheet, 0, 0, 105f, styles, new[] { 85, 84, 83, 82, 81, 78, 80, 80, 79, 78, 79, 78 });
                    Rows(sheet, 1, 1, 16.5f, styles, new[] { 77, 2, 2, 2, 75, 74, 76, 75, 75, 74, 75, 74 });
                    Rows(sheet, 2, 2, 16.5f, styles, new[] { 73, 2, 2, 2, 71, 70, 72, 72, 71, 70, 71, 70 });
                    Rows(sheet, 3, 3, 27f, styles, new[] { 69, 68, 66, 67, 67, 66, 66, 66, 66, 66, 66, 65 });
                    Rows(sheet, 4, 4, 19.5f, styles, new[] { 64, 63, 62, 60, 60, 62, 60, 60, 61, 60, 60, 59 });
                    Rows(sheet, 5, 7, 19.5f, styles, new[] { 49, 58, 44, 44, 44, 44, 44, 44, 51, 44, 44, 54 });
                    Rows(sheet, 8, 8, 20.25f, styles, new[] { 49, 57, 55, 55, 55, 55, 55, 55, 56, 55, 55, 54 });
                    Rows(sheet, 9, 9, 19.5f, styles, new[] { 53, 45, 44, 44, 44, 44, 44, 44, 51, 44, 44, 42 });
                    Rows(sheet, 10, 10, 19.5f, styles, new[] { 49, 52, 47, 51, 51, 51, 50, 50, 50, 50, 50, 42 });
                    Rows(sheet, 11, 11, 19.5f, styles, new[] { 49, 45, 44, 44, 44, 44, 42, 42, 43, 42, 42, 42 });
                    Rows(sheet, 12, 36, 19.5f, styles, new[] { 49, 48, 47, 47, 47, 47, 47, 47, 47, 47, 44, 42 });
                    Rows(sheet, 37, 37, 20.25f, styles, new[] { 46, 45, 44, 44, 44, 44, 42, 42, 43, 42, 42, 42 });
                    Rows(sheet, 38, 38, 21f, styles, new[] { 41, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40 });
                    Rows(sheet, 39, 39, 19.5f, styles, new[] { 39, 39, 38, 38, 38, 38, 37, 37, 37, 37, 37, 37 });
                    Rows(sheet, 40, 40, 16.5f, styles, new[] { 36, 2, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35 });
                    Rows(sheet, 41, 41, 16.5f, styles, new[] { 36, 2, 1, 35, 35, 35, 35, 35, 35, 35, 35, 35 });
                    foreach (var range in new[] { "A41:B41", "A2:D3", "G2:G3", "I1:J1", "A39:L39", "H2:H3", "A5:A9", "A42:B42", "E1:F1", "E2:F3", "K1:L1", "I2:J3", "K2:L3", "A10:A37" })
                        sheet.AddMergedRegion(CellRangeAddress.ValueOf(range));
                    // 合併完成後，補上「內容別」與「艙單號碼」之間的整段黑色直線。
                    var black = new XSSFColor();
                    // 使用完整 ARGB（FF000000），明確指定不透明黑色。
                    black.SetRgb(new byte[] { 255, 0, 0, 0 });
                    for (var rowIndex = 4; rowIndex <= 37; rowIndex++)
                    {
                        // 整頁縮小列印時，使用與外框相同的線寬，提高 A/B 欄分隔線在預覽中的可見度。
                        var contentStyle = (XSSFCellStyle)sheet.GetRow(rowIndex).GetCell(0).CellStyle;
                        contentStyle.BorderRight = BorderStyle.Medium;
                        contentStyle.SetRightBorderColor(black);
                        var manifestStyle = (XSSFCellStyle)sheet.GetRow(rowIndex).GetCell(1).CellStyle;
                        manifestStyle.BorderLeft = BorderStyle.Medium;
                        manifestStyle.SetLeftBorderColor(black);
                    }
                    Cell(sheet, "A1").SetCellValue("中華民國海關艙單");
                    Cell(sheet, "E1").SetCellValue("船名\n NAME OF VESSEL");
                    Cell(sheet, "G1").SetCellValue("航次\nVOY. NO.");
                    Cell(sheet, "H1").SetCellValue("船舶呼號 \nVESSEL CALL SIGN");
                    Cell(sheet, "I1").SetCellValue("船長姓名\nNAME OF MASTER");
                    Cell(sheet, "K1").SetCellValue("船隻掛號 \nVESSEL REG. NO.");
                    Cell(sheet, "A2").SetCellValue("■進口  □出口  □過境   更正單");
                    Cell(sheet, "A4").SetCellValue("內容別");
                    Cell(sheet, "B4").SetCellValue("艙單號碼");
                    Cell(sheet, "C4").SetCellValue("提單號碼");
                    Cell(sheet, "D4").SetCellValue("毛重(KG)");
                    Cell(sheet, "E4").SetCellValue("件數");
                    Cell(sheet, "F4").SetCellValue("件數單位");
                    Cell(sheet, "G4").SetCellValue("標記");
                    Cell(sheet, "H4").SetCellValue("貨物名稱");
                    Cell(sheet, "I4").SetCellValue("進口人英文名稱");
                    Cell(sheet, "J4").SetCellValue("進口人英文地址");
                    Cell(sheet, "K4").SetCellValue("卸存地");
                    Cell(sheet, "L4").SetCellValue("海關處理紀錄");
                    Cell(sheet, "A5").SetCellValue("原\n申\n報\n內\n容");
                    Cell(sheet, "A10").SetCellValue("更\n正\n內\n容");
                    Cell(sheet, "B10").SetCellValue("主艙號");
                    Cell(sheet, "C10").SetCellValue("主提單號碼");
                    Cell(sheet, "B12").SetCellValue("分艙號");
                    Cell(sheet, "C12").SetCellValue("分提單號碼");
                    Cell(sheet, "A39").SetCellValue("在此聲明所列更正艙單內容之項目，係依據『運輸工具進出口通關管理辦法』第二十五條規定，填具及簽發提單，如有內容不實，涉及詐欺偽造或其他違法情事，其一切法令責任一概與　貴關無涉。");
                    Cell(sheet, "A41").SetCellValue("立書人：捷豐國際物流股份有限公司");
                    Cell(sheet, "C41").SetCellValue("船 長 簽 章：");
                    Cell(sheet, "H41").SetCellValue("運送人或其代理人簽章：");
                    Cell(sheet, "A42").SetCellValue("聯絡人：02-25148667");
                    Cell(sheet, "H42").SetCellValue("CARRIER OR ITS AGENT SIGNATURE");
                    sheet.SetMargin(MarginType.LeftMargin, 0.7);
                    sheet.SetMargin(MarginType.RightMargin, 0.7);
                    sheet.SetMargin(MarginType.TopMargin, 0.75);
                    sheet.SetMargin(MarginType.BottomMargin, 0.75);
                    sheet.PrintSetup.HeaderMargin = 0.15;
                    sheet.PrintSetup.FooterMargin = 0.1;
                    sheet.PrintSetup.PaperSize = 9;
                    sheet.PrintSetup.Scale = 54;
                    sheet.PrintSetup.Landscape = true;
                    sheet.SetZoom(70);
                    sheet.DisplayGridlines = true;
                    break;
                }
                case 3:
                {
                    sheet.DefaultColumnWidth = 9;
                    sheet.DefaultRowHeightInPoints = 15.6f;
                    sheet.GetCTWorksheet().sheetFormatPr.defaultRowHeight = 15.6;
                    Columns(sheet, 0, 0, 4323, styles[91]);
                    Columns(sheet, 1, 1, 2816, styles[91]);
                    Columns(sheet, 2, 2, 6087, styles[91]);
                    Columns(sheet, 3, 3, 4664, styles[91]);
                    Columns(sheet, 4, 4, 6001, styles[91]);
                    Columns(sheet, 5, 5, 1024, styles[91]);
                    Columns(sheet, 6, 7, 2304, styles[91]);
                    Columns(sheet, 8, 8, 2076, styles[91]);
                    Columns(sheet, 9, 9, 2304, styles[91]);
                    Rows(sheet, 0, 0, 33f, styles, new[] { 91, 91, 91, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 1, 1, 21.75f, styles, new[] { 153, 153, 153, 170, 169, 168, 91, 167, 166, 165 });
                    Rows(sheet, 2, 2, 15.75f, styles, new[] { 153, 153, 153, 91, 164, 163, 91, 162, 161, 160 });
                    Rows(sheet, 3, 3, 70.5f, styles, new[] { 153, 153, 153, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 4, 4, 9.75f, styles, new[] { 159, 159, 91, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 5, 5, 104.25f, styles, new[] { 158, 158, 158, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 6, 6, 89.25f, styles, new[] { 157, 157, 157, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 7, 7, 20.25f, styles, new[] { 156, 156, 152, 154, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 8, 8, 21f, styles, new[] { 155, 155, 150, 154, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 9, 9, 18.75f, styles, new[] { 153, 153, 152, 149, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 10, 10, 23.25f, styles, new[] { 132, 151, 150, 149, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 11, 11, 43.5f, styles, new[] { 147, 147, 147, 147, 147, 91, 100, 100, 100, 100 });
                    Rows(sheet, 14, 14, 50.25f, styles, new[] { 148, 146, 147, 134, 134, 146, 92, 92, 146, 146 });
                    Rows(sheet, 15, 15, 105.75f, styles, new[] { 145, 144, 143, 142, 141, 140, 139, 139, 138, 138 });
                    Rows(sheet, 16, 16, 15.6f, styles, new[] { 136, 136, 137, 136, 136, 136, 91, 91, 135, 91 });
                    Rows(sheet, 18, 18, 57.75f, styles, new[] { 133, 133, 133, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 19, 19, 8.25f, styles, new[] { 134, 134, 134, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 20, 20, 21f, styles, new[] { 133, 133, 133, 133, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 21, 21, 42f, styles, new[] { 132, 131, 131, 91, 91, 91, 91, 91, 91, 91 });
                    Rows(sheet, 22, 22, 42.75f, styles, new[] { 130, 130, 130, 130, 129, 128, 128, 128, 128, 127 });
                    Rows(sheet, 23, 23, 34.65f, styles, new[] { 126, 122, 121, 125, 124, 120, 119, 119, 119, 118 });
                    Rows(sheet, 24, 24, 32.85f, styles, new[] { 123, 122, 121, 120, 118, 120, 119, 119, 119, 118 });
                    Rows(sheet, 25, 25, 45f, styles, new[] { 117, 116, 115, 114, 113, 112, 111, 110, 109, 91 });
                    Rows(sheet, 26, 26, 16.5f, styles, new[] { 108, 107, 104, 104, 106, 105, 104, 104, 104, 91 });
                    Rows(sheet, 27, 27, 21f, styles, new[] { 97, 103, 92, 92, 102, 98, 92, 92, 92, 91 });
                    Rows(sheet, 28, 28, 18.75f, styles, new[] { 97, 101, 100, 100, 99, 98, 92, 92, 92, 91 });
                    Rows(sheet, 29, 29, 23.25f, styles, new[] { 97, 96, 95, 95, 94, 93, 92, 92, 92, 91 });
                    foreach (var range in new[] { "A2:C4", "H2:J3", "E3:F3", "A6:C6", "A7:C7", "C9:D9", "A10:B10", "C10:D10", "A11:B11", "C11:D11", "A8:B8", "C8:D8", "D25:E25", "G12:J12", "B15:C15", "F15:H15", "I15:J15", "D16:E16", "F16:H16", "I16:J16", "A12:B12", "C12:E12", "B29:E29", "A19:C19", "A21:D21", "A22:C22", "E23:J23", "A24:A25", "B24:C24", "D24:E24", "F24:J24", "B25:C25", "B30:E30", "F25:J25", "A26:B26", "C26:D26", "E26:G26", "A27:A30", "B27:E27", "F27:F30", "G27:I30", "B28:E28" })
                        sheet.AddMergedRegion(CellRangeAddress.ValueOf(range));
                    Rich(sheet, "A2", "Shipper(发货人)\nXIAMEN TIANXIANGCHI SUPPLY CHAIN MANAGEMENT CO., LTD\nUNIT 603- 2. BUILDING 1, XIAMEN CROSS-*\n厦门天翔驰供应链管理有限公司\n电话:                 联系人:", new[] { 13, 107, 1, 24 }, new[] { 23, 29, 21, 36 }, styles);
                    Cell(sheet, "E2").SetCellValue("D/R  No.（编号  ）");
                    Rich(sheet, "H2", "厦门外代", new[] { 4 }, new[] { 29 }, styles);
                    Rich(sheet, "A6", "Consignee(收货人)\nJET-F WORLDWIDE EXPRESS CO., LTD.\n9F.-1,NO.268, SEC.2, FUXING S.\nRD.,DA-AN\nDISTRICT TAIPEI CITY 106, TAIWAN,\nCHINA", new[] { 14, 115 }, new[] { 35, 29 }, styles);
                    Rich(sheet, "A7", "Notify  Party(通知人)\nJET-F WORLDWIDE EXPRESS CO., LTD.\n9F.-1,NO.268, SEC.2, FUXING S. RD.,DA-AN\nDISTRICT TAIPEI CITY 106, TAIWAN,\nCHINA", new[] { 19, 114 }, new[] { 23, 29 }, styles);
                    Rich(sheet, "A8", "Pre-carriage  by(前程运输)", new[] { 22 }, new[] { 23 }, styles);
                    Cell(sheet, "C8").SetCellValue("Place  of  Receipt(收货地点)");
                    Cell(sheet, "C9").SetCellValue("XIAMEN,CHINA");
                    Rich(sheet, "A10", "Ocean Vessel(船名Voy.No.(航次)                                        ", new[] { 26, 40 }, new[] { 28, 29 }, styles);
                    Cell(sheet, "C10").SetCellValue("Port  of  Loading(装货港)");
                    Cell(sheet, "C11").SetCellValue("XIAMEN,CHINA");
                    Rich(sheet, "A12", "Port of Discharge(卸货港)\nKINMEN", new[] { 23, 6 }, new[] { 23, 29 }, styles);
                    Rich(sheet, "C12", "Place of Delivery(交货地点) \nKINMEN", new[] { 25, 6 }, new[] { 23, 29 }, styles);
                    Rich(sheet, "G12", " Final Destination(目的地)\n", new[] { 19, 3, 2 }, new[] { 21, 32, 21 }, styles);
                    Cell(sheet, "A15").SetCellValue("Container  No.\n集装箱号");
                    Cell(sheet, "B15").SetCellValue("Seal  No.(封志号) \nMark  &  Nos\n (标记和号码)");
                    Cell(sheet, "D15").SetCellValue("No.of  contai-\nners  of  p’kgs\n (箱数或件数)");
                    Cell(sheet, "E15").SetCellValue("Kind  of  Packages. Description  of  goods \n(包装种类与货名)");
                    Cell(sheet, "F15").SetCellValue("Gross  Weight\n毛重(公斤)");
                    Cell(sheet, "I15").SetCellValue("Measurement\n尺寸(立方米)");
                    Cell(sheet, "D16").SetCellValue("           SHIPPER'S LOAD,COUNT & SEAL\n            1×40HC(FCL)\n            CY/CY  S.T.C\n            快件\n            EXPRESS\n            KHH0638S");
                    Cell(sheet, "I16").SetCellValue("65CBM");
                    Cell(sheet, "A19").SetCellValue("TOTAL  NUMBER  OF  CONTAINERS \nOR  PACKAGES  (IN  WORDS)\n集装箱数或件数合计(大写)");
                    Cell(sheet, "A21").SetCellValue("Container  No.(箱号)  Seal  NO.(封志号)  Containers  Type(箱型）");
                    Rich(sheet, "E23", "Received(实收)  By  Terminal  clerk(场站员签字)\nFREIGHT PREPAID", new[] { 41, 15 }, new[] { 23, 29 }, styles);
                    Cell(sheet, "A24").SetCellValue("FREIGHT\n& \nCHARGES");
                    Rich(sheet, "B24", "Prepaid  at(预付地点)", new[] { 17 }, new[] { 23 }, styles);
                    Cell(sheet, "D24").SetCellValue("Payable  at(到付地点)");
                    Rich(sheet, "F24", "Place  of  Issue(签发地点)", new[] { 22 }, new[] { 23 }, styles);
                    Rich(sheet, "B25", "Total  Prepaid(预付总额)", new[] { 20 }, new[] { 23 }, styles);
                    Rich(sheet, "D25", "No.of  Original  B(s)/L(正本提单份数)", new[] { 31 }, new[] { 23 }, styles);
                    Rich(sheet, "F25", "Owner  of  Container(箱主)", new[] { 24 }, new[] { 23 }, styles);
                    Cell(sheet, "A26").SetCellValue("Service  Type  on  Receiving\n-CY         -CFS,       -DOOR");
                    Rich(sheet, "C26", "Service  Type  on  Delivery\n-CY       -CFS,         -DOOR", new[] { 57 }, new[] { 28 }, styles);
                    Rich(sheet, "E26", "Reefer  Temperature  Required(冷藏温度)", new[] { 35 }, new[] { 25 }, styles);
                    Rich(sheet, "H26", "°F", new[] { 2 }, new[] { 23 }, styles);
                    Cell(sheet, "I26").SetCellValue("°c");
                    Cell(sheet, "A27").SetCellValue("TYPE\nOF\nGOODS\n(种类)");
                    Cell(sheet, "B27").SetCellValue(" Ordinary               Reefer               Dangerous                Auto ");
                    Rich(sheet, "F27", "危险品", new[] { 3 }, new[] { 23 }, styles);
                    Cell(sheet, "G27").SetCellValue("Class:\nProperty\nIMDG Code Page:\nUN No.:");
                    Cell(sheet, "B28").SetCellValue("    (普通)                     (冷藏)                 (危险品)                   (裸装车)");
                    Cell(sheet, "B29").SetCellValue("                Liquid                  Live Animal        Bulk                   ");
                    Cell(sheet, "B30").SetCellValue("                (液体)                   (活动物)              (散货)                ");
                    sheet.SetMargin(MarginType.LeftMargin, 0.7);
                    sheet.SetMargin(MarginType.RightMargin, 0.7);
                    sheet.SetMargin(MarginType.TopMargin, 0.75);
                    sheet.SetMargin(MarginType.BottomMargin, 0.75);
                    sheet.PrintSetup.HeaderMargin = 0.3;
                    sheet.PrintSetup.FooterMargin = 0.3;
                    sheet.PrintSetup.PaperSize = 9;
                    sheet.PrintSetup.Scale = 61;
                    sheet.PrintSetup.Landscape = false;
                    sheet.SetZoom(85);
                    sheet.DisplayGridlines = true;
                    AddBillOfLadingDrawings(workbook, sheet);
                    break;
                }
                case 4:
                {
                    sheet.DefaultColumnWidth = 9;
                    sheet.DefaultRowHeightInPoints = 16.2f;
                    sheet.GetCTWorksheet().sheetFormatPr.defaultRowHeight = 16.2;
                    Columns(sheet, 0, 0, 6741, styles[1]);
                    Columns(sheet, 1, 1, 6627, styles[1]);
                    Columns(sheet, 2, 2, 3925, styles[1]);
                    Columns(sheet, 3, 3, 3868, styles[1]);
                    Columns(sheet, 4, 4, 6087, styles[1]);
                    Rows(sheet, 0, 0, 67.5f, styles, new[] { 16, 2, 2, 2, 2 });
                    Rows(sheet, 1, 1, 67.5f, styles, new[] { 9, 9, 9, 9, 9 });
                    Rows(sheet, 2, 2, 45f, styles, new[] { 9, 15, 9, 14, 1 });
                    Rows(sheet, 3, 3, 45f, styles, new[] { 9, 90, 9, 12, 1 });
                    Rows(sheet, 4, 4, 45f, styles, new[] { 9, 12, 1, 1, 1 });
                    Rows(sheet, 5, 5, 45f, styles, new[] { 11, 2, 2, 2, 2 });
                    Rows(sheet, 6, 6, 45f, styles, new[] { 9, 10, 9, 1, 1 });
                    Rows(sheet, 7, 7, 34.5f, styles, new[] { 7, 7, 8, 7, 4 });
                    Rows(sheet, 8, 21, 20.25f, styles, new[] { 6, 5, 5, 5, 4 });
                    Rows(sheet, 22, 23, 20.25f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 24, 24, 26.25f, styles, new[] { 9, 1, 1, 1, 1 });
                    Rows(sheet, 25, 25, 30f, styles, new[] { 1, 89, 2, 1, 1 });
                    Rows(sheet, 26, 26, 16.5f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 27, 27, 45f, styles, new[] { 88, 2, 1, 1, 1 });
                    Rows(sheet, 28, 28, 16.5f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 29, 30, 25.5f, styles, new[] { 1, 1, 87, 1, 1 });
                    Rows(sheet, 31, 31, 16.5f, styles, new[] { 1, 1, 1, 1, 1 });
                    Rows(sheet, 32, 32, 21f, styles, new[] { 86, 2, 2, 2, 2 });
                    foreach (var range in new[] { "A33:E33", "A28:B28", "B26:C26", "A1:E1", "A6:E6" })
                        sheet.AddMergedRegion(CellRangeAddress.ValueOf(range));
                    Cell(sheet, "A1").SetCellValue("准單申請書");
                    Cell(sheet, "A2").SetCellValue("本公司捷豐國際物流股份有限公司承攬進口快遞貨物一批，進口資訊如下");
                    Cell(sheet, "A3").SetCellValue("進口日期");
                    Cell(sheet, "C3").SetCellValue("船名");
                    Cell(sheet, "A4").SetCellValue("航次");
                    Cell(sheet, "C4").SetCellValue("櫃　　號：");
                    Cell(sheet, "A5").SetCellValue("主提單號：");
                    Cell(sheet, "A6").SetCellValue("茲因本批貨物有部分溢卸,今已由艙單台更正好艙單,今檢附更正好的艙單,供鈞關重送准單,以利海運快遞溢卸之貨物能順利通關放行。資訊如下:");
                    Cell(sheet, "A7").SetCellValue("共計艙單數:");
                    Cell(sheet, "C7").SetCellValue("票");
                    Cell(sheet, "A8").SetCellValue("分提單號碼");
                    Cell(sheet, "B8").SetCellValue("裝貨單號");
                    Cell(sheet, "C8").SetCellValue("申報件數");
                    Cell(sheet, "D8").SetCellValue("實到件數");
                    Cell(sheet, "A25").SetCellValue("此致");
                    Cell(sheet, "B26").SetCellValue("財政部關務署台中關");
                    Cell(sheet, "C30").SetCellValue("立書人：捷豐國際物流股份有限公司");
                    Cell(sheet, "C31").SetCellValue("聯絡人：02-25148667");
                    sheet.SetMargin(MarginType.LeftMargin, 0.70866141732283472);
                    sheet.SetMargin(MarginType.RightMargin, 0.70866141732283472);
                    sheet.SetMargin(MarginType.TopMargin, 0.74803149606299213);
                    sheet.SetMargin(MarginType.BottomMargin, 0.74803149606299213);
                    sheet.PrintSetup.HeaderMargin = 0.15;
                    sheet.PrintSetup.FooterMargin = 0.1;
                    sheet.PrintSetup.PaperSize = 9;
                    sheet.PrintSetup.Scale = 79;
                    sheet.PrintSetup.Landscape = false;
                    sheet.SetZoom(70);
                    sheet.DisplayGridlines = true;
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(layout));
            }
            var lastRow = layout == 1 ? 49 : layout == 2 ? 41 : layout == 3 ? 29 : 32;
            workbook.SetPrintArea(workbook.GetSheetIndex(sheet), 0, layout == 1 ? 12 : layout == 2 ? 11 : layout == 3 ? 9 : 4, 0, lastRow);
            sheet.FitToPage = true;
            sheet.PrintSetup.FitWidth = 1;
            sheet.PrintSetup.FitHeight = 1;
            return sheet;
        }

        /// <summary>補上 BL 的浮動底線、虛線分隔、方框及原始印章，座標使用參考檔的 EMU 單位。</summary>
        private static void AddBillOfLadingDrawings(XSSFWorkbook workbook, XSSFSheet sheet)
        {
            var drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();
            AddBlRule(drawing, 2, BlMarker(0, 22412, 1, 11204), BlTransform(22412, 216944, 5289176, 45719));
            AddBlRule(drawing, 3, BlMarker(0, 11205, 8, 200808), BlTransform(11205, 1846728, 5558119, 45719, false, true));
            AddBlRule(drawing, 4, BlMarker(0, 22412, 6, 11208), BlTransform(22412, 1245648, 5535706, 45719));
            AddBlRule(drawing, 5, BlMarker(0, 0, 6, 1109383), BlTransform(0, 1437043, 5591734, 45719));
            AddBlRule(drawing, 6, BlMarker(0, 0, 11, 1682), BlTransform(0, 2264822, 9749118, 45719));
            AddBlRule(drawing, 7, BlMarker(0, 0, 11, 537881), BlTransform(0, 2465741, 9704294, 45719));
            AddBlRule(drawing, 8, BlMarker(0, 22412, 17, 11204), BlTransform(22412, 3508784, 9693088, 56032, false, true));
            AddBlRule(drawing, 9, BlMarker(0, 1, 18, 672352), BlTransform(1, 3910852, 9749117, 100854, false, true));
            AddBlRule(drawing, 10, BlMarker(0, 0, 4, 56029), BlTransform(0, 878989, 5569324, 45719));
            AddBlDivider(drawing, 11, BlMarker(1, 358589, 12, 22411), BlMarker(1, 369795, 17, 56030), BlTransform(975809, 2491291, 11206, 1062319, true, false));
            AddBlDivider(drawing, 12, BlMarker(2, 1243852, 12, 11206), BlMarker(2, 1255058, 17, 44825), BlTransform(1853452, 2480086, 0, 1062319, true, false));
            AddBlDivider(drawing, 13, BlMarker(3, 1333500, 12, 22411), BlMarker(3, 1344706, 17, 56030), BlTransform(2468880, 2491291, 3586, 1062319, true, false));
            AddBlDivider(drawing, 14, BlMarker(5, 67235, 11, 537882), BlMarker(5, 78441, 17, 22412), BlTransform(3153335, 2465742, 11206, 1054250, true, false));
            AddBlDivider(drawing, 15, BlMarker(7, 515470, 11, 537883), BlMarker(7, 526676, 17, 22413), BlTransform(4836010, 2465743, 11206, 1054250, true, false));
            AddBlFrame(drawing, 16, BlMarker(0, 1199031, 25, 302558), BlMarker(1, 78442, 25, 470647), BlTransform(619911, 5346998, 75751, 449));
            AddBlFrame(drawing, 17, BlMarker(0, 593911, 25, 313765), BlMarker(0, 761998, 25, 481854), BlTransform(593911, 5350585, 23307, 449));
            AddBlFrame(drawing, 18, BlMarker(0, 78441, 25, 302559), BlMarker(0, 246528, 25, 470648), BlTransform(78441, 5346999, 168087, 449));
            AddBlFrame(drawing, 19, BlMarker(2, 616324, 25, 291353), BlMarker(2, 784411, 25, 459442), BlTransform(1850764, 5351033, 447, 449));
            AddBlFrame(drawing, 20, BlMarker(2, 1736912, 25, 291353), BlMarker(3, 89646, 25, 459442), BlTransform(1851212, 5351033, 90094, 449));
            AddBlFrame(drawing, 21, BlMarker(2, 1104900, 25, 286870), BlMarker(2, 1272987, 25, 454959), BlTransform(1851660, 5346550, 447, 449));
            AddBlFrame(drawing, 22, BlMarker(1, 470647, 26, 145676), BlMarker(1, 638734, 27, 100854), BlTransform(1087867, 5494916, 145227, 160918));
            AddBlFrame(drawing, 23, BlMarker(2, 986117, 26, 134471), BlMarker(2, 1154204, 27, 89649), BlTransform(1854797, 5483711, 447, 160918));
            AddBlFrame(drawing, 24, BlMarker(3, 358587, 26, 123263), BlMarker(3, 526674, 27, 78441), BlTransform(2210247, 5472503, 168087, 160918));
            AddBlFrame(drawing, 25, BlMarker(4, 324970, 26, 112058), BlMarker(4, 493057, 27, 67236), BlTransform(2793850, 5461298, 168087, 160918));
            AddBlFrame(drawing, 26, BlMarker(1, 459441, 28, 112060), BlMarker(1, 627528, 29, 44825), BlTransform(1076661, 5872780, 160467, 138505));
            AddBlFrame(drawing, 27, BlMarker(2, 974911, 28, 100855), BlMarker(2, 1142998, 29, 33620), BlTransform(1851211, 5861575, 447, 138505));
            AddBlFrame(drawing, 28, BlMarker(3, 347381, 28, 89647), BlMarker(3, 515468, 29, 22412), BlTransform(2199041, 5850367, 168087, 138505));
            AddBlFrame(drawing, 29, BlMarker(4, 324970, 28, 78441), BlMarker(4, 493057, 29, 11206), BlTransform(2793850, 5839161, 168087, 138505));
            AddBlRule(drawing, 30, BlMarker(4, 560294, 29, 33618), BlTransform(3029174, 6000078, 1064559, 67235));

            var pictureIndex = -1;
            var pictures = workbook.GetAllPictures();
            for (var index = 0; index < pictures.Count; index++)
            {
                if (((IPictureData)pictures[index]).Data.SequenceEqual(BillOfLadingSeal))
                {
                    pictureIndex = index;
                    break;
                }
            }
            if (pictureIndex < 0)
                pictureIndex = workbook.AddPicture(BillOfLadingSeal, PictureType.PNG);

            // CreatePicture 建立圖片關聯，再使用單儲存格錨點保留參考檔的原始位置與尺寸。
            var picture = (XSSFPicture)drawing.CreatePicture(new XSSFClientAnchor(), pictureIndex);
            var anchors = drawing.GetCTDrawing().CellAnchors;
            anchors.RemoveAt(anchors.Count - 1);
            var image = picture.GetCTPicture();
            image.nvPicPr.cNvPr.id = 31;
            image.nvPicPr.cNvPr.name = "BL 原始印章";
            image.spPr.xfrm = BlTransform(2961939, 4832871, 2078694, 1187825);
            anchors.Add(new CT_OneCellAnchor
            {
                from = BlMarker(4, 493059, 23, 100851),
                ext = new CT_PositiveSize2D { cx = 2078694, cy = 1187825 },
                picture = image,
                clientData = new CT_AnchorClientData()
            });
        }

        /// <summary>從組件內嵌資源載入參考檔的原始 PNG 印章，不依賴外部範本或圖片路徑。</summary>
        private static byte[] LoadBillOfLadingSeal()
        {
            using (var stream = typeof(TaichungOverUnloadLayout).Assembly.GetManifestResourceStream(
                "Service.Resources.ShortOverUnload.BillOfLadingSeal.png"))
            using (var output = new MemoryStream())
            {
                if (stream == null)
                    throw new InvalidOperationException("找不到 BL 原始印章內嵌資源。");
                stream.CopyTo(output);
                return output.ToArray();
            }
        }

        /// <summary>以水平預設線條重建參考檔的自訂路徑，避免 NPOI 2.5.6 遺失自訂幾何路徑。</summary>
        private static void AddBlRule(XSSFDrawing drawing, uint id, CT_Marker position, CT_Transform2D transform)
        {
            if (transform.flipV)
            {
                position.rowOff += transform.ext.cy;
                transform.off.y += transform.ext.cy;
            }
            transform.ext.cy = 0;
            transform.flipV = false;
            drawing.GetCTDrawing().CellAnchors.Add(new CT_OneCellAnchor
            {
                from = position,
                ext = new CT_PositiveSize2D { cx = transform.ext.cx, cy = 0 },
                sp = BlShape(id, "BL 底線 " + id, BlProperties(ST_ShapeType.line, transform, 9525, false)),
                clientData = new CT_AnchorClientData()
            });
        }

        /// <summary>重建貨物明細區的垂直虛線分隔。</summary>
        private static void AddBlDivider(XSSFDrawing drawing, uint id, CT_Marker from, CT_Marker to, CT_Transform2D transform)
        {
            drawing.GetCTDrawing().CellAnchors.Add(new CT_TwoCellAnchor
            {
                from = from,
                to = to,
                connector = new CT_Connector
                {
                    nvCxnSpPr = new CT_ConnectorNonVisual
                    {
                        cNvPr = new CT_NonVisualDrawingProps { id = id, name = "BL 虛線 " + id },
                        cNvCxnSpPr = new CT_NonVisualConnectorProperties()
                    },
                    spPr = BlProperties(ST_ShapeType.line, transform, 6350, true)
                },
                clientData = new CT_AnchorClientData()
            });
        }

        /// <summary>重建服務類別及貨物種類的原始方框。</summary>
        private static void AddBlFrame(XSSFDrawing drawing, uint id, CT_Marker from, CT_Marker to, CT_Transform2D transform)
        {
            var properties = BlProperties(ST_ShapeType.rect, transform, 3175, false);
            // 原始 accent1 (#4472C4) 使用 15% shade，轉為固定 RGB 以免受新工作簿佈景主題影響。
            properties.ln.solidFill.srgbClr.val = new byte[] { 10, 17, 29 };
            drawing.GetCTDrawing().CellAnchors.Add(new CT_TwoCellAnchor
            {
                from = from,
                to = to,
                sp = BlShape(id, "BL 方框 " + id, properties),
                clientData = new CT_AnchorClientData()
            });
        }

        /// <summary>建立 BL 繪圖物件的識別資訊及幾何屬性。</summary>
        private static CT_Shape BlShape(uint id, string name, CT_ShapeProperties properties)
        {
            return new CT_Shape
            {
                nvSpPr = new CT_ShapeNonVisual
                {
                    cNvPr = new CT_NonVisualDrawingProps { id = id, name = name },
                    cNvSpPr = new CT_NonVisualDrawingShapeProps()
                },
                spPr = properties
            };
        }

        /// <summary>建立無填色、固定線寬與顏色的預設幾何圖形。</summary>
        private static CT_ShapeProperties BlProperties(ST_ShapeType type, CT_Transform2D transform, int width, bool dashed)
        {
            var line = new CT_LineProperties
            {
                w = width,
                wSpecified = true,
                solidFill = new CT_SolidColorFillProperties { srgbClr = new CT_SRgbColor { val = new byte[] { 0, 0, 0 } } }
            };
            if (dashed)
                line.prstDash = new CT_PresetLineDashProperties { val = ST_PresetLineDashVal.dash };
            return new CT_ShapeProperties
            {
                xfrm = transform,
                prstGeom = new CT_PresetGeometry2D { prst = type, avLst = new CT_GeomGuideList() },
                noFill = new CT_NoFillProperties(),
                ln = line
            };
        }

        /// <summary>建立儲存格錨點，偏移量為 EMU。</summary>
        private static CT_Marker BlMarker(int column, long columnOffset, int row, long rowOffset)
        {
            return new CT_Marker { col = column, colOff = columnOffset, row = row, rowOff = rowOffset };
        }

        /// <summary>保留參考繪圖的轉換尺寸及翻轉設定。</summary>
        private static CT_Transform2D BlTransform(long x, long y, long width, long height, bool flipH = false, bool flipV = false)
        {
            return new CT_Transform2D
            {
                off = new CT_Point2D { x = x, y = y },
                ext = new CT_PositiveSize2D { cx = width, cy = height },
                flipH = flipH,
                flipV = flipV
            };
        }

        /// <summary>建立與參考格式相同的字型。</summary>
        private static XSSFFont Font(XSSFWorkbook workbook, string name, double size, bool bold, bool italic, int charset, int family, string rgb)
        {
            var font = (XSSFFont)workbook.CreateFont();
            font.FontName = name;
            font.FontHeightInPoints = size;
            font.IsBold = bold;
            font.IsItalic = italic;
            font.Family = family;
            if (charset != 0) font.GetCTFont().AddNewCharset().val = charset;
            if (rgb.Length == 6)
                font.GetCTFont().AddNewColor().rgb = new[] { (byte)255, Convert.ToByte(rgb.Substring(0, 2), 16), Convert.ToByte(rgb.Substring(2, 2), 16), Convert.ToByte(rgb.Substring(4, 2), 16) };
            return font;
        }

        /// <summary>設定字型、對齊、數值格式及四邊框線；填色固定為空白。</summary>
        private static ICellStyle Style(XSSFWorkbook workbook, IFont font, int left, int right, int top, int bottom,
            HorizontalAlignment horizontal, VerticalAlignment vertical, bool wrap, short rotation, short numberFormat, string customFormat, bool shrink)
        {
            var style = workbook.CreateCellStyle();
            style.SetFont(font);
            style.Alignment = horizontal;
            style.VerticalAlignment = vertical;
            style.WrapText = wrap;
            style.Rotation = rotation;
            style.ShrinkToFit = shrink;
            style.BorderLeft = (BorderStyle)left;
            style.BorderRight = (BorderStyle)right;
            style.BorderTop = (BorderStyle)top;
            style.BorderBottom = (BorderStyle)bottom;
            style.DataFormat = customFormat == null ? numberFormat : workbook.CreateDataFormat().GetFormat(customFormat);
            style.FillPattern = FillPattern.NoFill;
            return style;
        }

        /// <summary>設定連續欄位的精確欄寬及預設樣式。</summary>
        private static void Columns(ISheet sheet, int first, int last, int width, ICellStyle style)
        {
            for (var column = first; column <= last; column++)
            {
                sheet.SetColumnWidth(column, width);
                sheet.SetDefaultColumnStyle(column, style);
            }
        }

        /// <summary>建立連續資料列並依欄序套用參考樣式。</summary>
        private static void Rows(ISheet sheet, int first, int last, float height, ICellStyle[] styles, int[] indices)
        {
            for (var index = first; index <= last; index++)
            {
                var row = sheet.CreateRow(index);
                row.HeightInPoints = height;
                for (var column = 0; column < indices.Length; column++)
                    row.CreateCell(column).CellStyle = styles[indices[column]];
            }
        }

        /// <summary>取得已建立的參考格式儲存格。</summary>
        private static ICell Cell(ISheet sheet, string address)
        {
            var reference = new CellReference(address);
            return sheet.GetRow(reference.Row).GetCell(reference.Col);
        }

        /// <summary>保留固定文字中不同字型的各段落。</summary>
        private static void Rich(ISheet sheet, string address, string text, int[] lengths, int[] fontIndices, ICellStyle[] styles)
        {
            var rich = new XSSFRichTextString();
            var start = 0;
            for (var index = 0; index < lengths.Length; index++)
            {
                var font = (XSSFFont)sheet.Workbook.GetFontAt(styles[ReferenceStyleCount + fontIndices[index]].FontIndex);
                rich.Append(text.Substring(start, lengths[index]), font);
                start += lengths[index];
            }
            Cell(sheet, address).SetCellValue(rich);
        }
    }
}
