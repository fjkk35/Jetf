using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.BatchEditOrder
{
    public class BatchEditOrderService : _BaseService
    {
        public BatchEditOrderService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent, cs_Percent2, dateStyle, date2Style;


        public IWorkbook Search(string source, string filePath)
        {
            var list = ReadExcel(filePath);
            //取得批量製單申報資料
            var dt = GetBatchEditOrderSearch(source, list);

            IWorkbook workbook = new XSSFWorkbook();
            //產生EXCEL
            GetBatchEditOrderSearchSheet(workbook, dt, "製單申報資料");

            return workbook;
        }


        /// <summary>
        /// 批量製單申報資料Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetBatchEditOrderSearchSheet(IWorkbook workbook, DataTable dt_Report, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("分提單號");
            row.CreateCell(1).SetCellValue("製單主號");
            row.CreateCell(2).SetCellValue("製單袋號");
            row.CreateCell(3).SetCellValue("物流單號");
            row.CreateCell(4).SetCellValue("製單申報姓名");
            row.CreateCell(5).SetCellValue("製單申報電話");
            row.CreateCell(6).SetCellValue("製單申報ID");
            row.CreateCell(7).SetCellValue("製單申報品名");
            row.CreateCell(8).SetCellValue("錯單代碼");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 5000);

            for (int i = 0; i < dt_Report.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt_Report.Rows[i]["TrackingNo"].ToString());
                row.CreateCell(1).SetCellValue(dt_Report.Rows[i]["MAINNUMBER"].ToString());
                row.CreateCell(2).SetCellValue(dt_Report.Rows[i]["BL_NO"].ToString());
                row.CreateCell(3).SetCellValue(dt_Report.Rows[i]["JETF_SERIAL"].ToString());
                row.CreateCell(4).SetCellValue(dt_Report.Rows[i]["IMPORTER"].ToString());
                row.CreateCell(5).SetCellValue(dt_Report.Rows[i]["IM_PHONENO"].ToString());
                row.CreateCell(6).SetCellValue(dt_Report.Rows[i]["IMPORTER_ID"].ToString());
                row.CreateCell(7).SetCellValue(dt_Report.Rows[i]["ITEM_ONAME"].ToString());
                row.CreateCell(8).SetCellValue(dt_Report.Rows[i]["MESSAGE"].ToString());
            }
        }

        /// <summary>
        /// 取得批量製單申報資料
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public DataTable GetBatchEditOrderSearch(string source,List<string> list)
        {
            var dt = new DataTable();
            string sql = string.Empty;

            if (source == "SEA")
            {
                sql = $@"
                                WITH CptSeaMainNumberDetail AS (
                                    SELECT 
                                        BagNumber,
                                        CorrectImporterName,
                                        CorrectImporterPhone,
                                        CorrectImporterId
                                    FROM [jetf].[dbo].CptSeaMainNumberDetail
                                    where exists (SELECT TrackingNo FROM @TrackingList where TrackingNo = BagNumber)
                                    and UploadOpe > ''
                                )
                                SELECT 
                                    a.TrackingNo,
                                    b.JETF_SERIAL,
                                    b.Mainnumber,
                                    b.BL_NO,
                                    ISNULL(d.CorrectImporterName, b.IMPORTER) AS IMPORTER,
                                    ISNULL(d.CorrectImporterPhone, b.IM_PHONENO) AS IM_PHONENO,
                                    ISNULL(d.CorrectImporterId, b.IMPORTER_ID) AS IMPORTER_ID, 
                                    b.ITEM_ONAME,
                                    c.MESSAGE
                                FROM @TrackingList a
                                LEFT JOIN DATA_CENTER.dbo.SEA_ORDER_EDIT b ON a.TrackingNo = b.BL_NO AND b.NW > 0 
                                LEFT JOIN [jetf].[dbo].[SEA_BAGNO_UPLOAD] c ON a.TrackingNo = c.BL_NO AND b.Mainnumber = c.Mainnumber
                                LEFT JOIN CptSeaMainNumberDetail d ON a.TrackingNo = d.BagNumber
                                ";
            }
            else
            {
                sql = $@"
                            select a.TrackingNo,d.DELIVERYNO as JETF_SERIAL,b.MAINNUMBER,b.BAGNO as BL_NO,b.RECIPIENT as IMPORTER,b.RECPHONE as IM_PHONENO,b.RECID as IMPORTER_ID,b.ITEMS as ITEM_ONAME,c.REASON as MESSAGE 
                            from @TrackingList a 
                            left join DATA_CENTER.dbo.MAKELIST b on a.TrackingNo=b.TRACKINGNO 
                            left join [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] c on a.TrackingNo=c.HAWB and b.MAINNUMBER=c.MAWB 
                            left join DATA_CENTER.dbo.[ORIGINALLIST] d on b.TRACKINGNO = d.TRACKINGNO 
                            ";
            }

            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                var dtData = new DataTable();
                dtData.Columns.Add("TrackingNo", typeof(string));
                foreach (var item in list.Distinct())
                {
                    dtData.Rows.Add(item);
                }
                da.SelectCommand.Parameters.Add("@TrackingList", SqlDbType.Structured).Value = dtData;
                da.SelectCommand.Parameters["@TrackingList"].TypeName = "dbo.TrackingNoList";
                da.SelectCommand.CommandTimeout = 300;
                da.Fill(dt);
            }

            if (source == "SEA")
            {
                //海運資料處理過濾
                dt = GetSeaQuery(dt);
            }

            return dt;
        }

        /// <summary>
        /// 海運資料處理過濾
        /// </summary>
        /// <returns></returns>
        DataTable GetSeaQuery(DataTable dt)
        {

            var query = from row in dt.AsEnumerable()
                        group row by string.IsNullOrEmpty(row.Field<string>("BL_NO")) 
                                    ? row.Field<string>("TrackingNo") 
                                    : row.Field<string>("BL_NO")
                        into grp
                        select grp.OrderByDescending(r => r.Field<string>("MESSAGE")).First();

            DataTable filter = query.CopyToDataTable();
            return filter;
        }

        List<string> ReadExcel(string filePath)
        {
            var list = new List<string>();

            bool read = false;
            string trackingno;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //分提單號
                    trackingno = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "分提單號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && trackingno != "")
                    {
                        list.Add(trackingno);
                    }
                }
            }
            return list;
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
