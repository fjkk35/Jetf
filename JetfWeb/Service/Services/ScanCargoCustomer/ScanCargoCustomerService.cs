using iTextSharp.text;
using iTextSharp.text.pdf;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.ScanCargoCustomer
{
    /// <summary>
    /// 建構式
    /// </summary>
    public class ScanCargoCustomerService
    {
        private SqlConnection conn;


        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle;
        iTextSharp.text.Font font8, font9, font10, font11, font12, font14, font16, font18, font20, fontB18;


        public ScanCargoCustomerService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 掃貨上車交接派件公司明細表-PDF
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        public byte[] GetCustomerPdf(DataTable dt, string dataDate, string pdfTrans, string dataType)
        {
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
                Document pdfDoc = new Document(PageSize.A4, 0, 0, 20f, 10f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                //取得客戶明細PDF
                GetCustomerDetailsPdf(pdfDoc, dt, dataDate);

                DateTime nowDateTime = DateTime.Now;
                //取得客戶總表PDF
                GetCustomerReportPdf(pdfDoc, dt, nowDateTime, pdfTrans, dataType);

                pdfDoc.Close();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 取得掃貨上車PDT資料(客戶)
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="dataType"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        public (DataTable, DataTable) GetScanCargoCustomerDetailsPdf(string trans, string dataType, string sDate, string eDate)
        {
            DataTable dt = GetScanCargoCustomerDetails(trans, dataType, sDate, eDate);
            DataTable dt_Exclude = new DataTable();
            //排除空快回艙資料
            if (trans != "74")
            {
                //空快回艙資料
                dt_Exclude = GetScanCargoCustomerDetails("74", dataType, sDate, eDate);

                //空快回艙資料，掃貨上車有掃到的資料
                var filterExclude = from r in dt.AsEnumerable()
                                    where dt_Exclude.AsEnumerable()
                                              .Any(x => r.Field<string>("Data") == x.Field<string>("Data"))
                                    select r;

                if (filterExclude.Any())
                {
                    dt_Exclude = filterExclude.CopyToDataTable();
                }
                else
                {
                    dt_Exclude = new DataTable();
                }

                //排除空快回艙資料
                dt = GetScanCargoCustomerDetailsExclude(dt, dt_Exclude);
            }

            return (dt, dt_Exclude);
        }

        /// <summary>
        /// 取得掃貨上車PDT資料(客戶)
        /// </summary>
        /// <returns></returns>
        public DataTable GetScanCargoCustomerDetails(string trans, string dataType, string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();

            //60-Coupang CVS長慶、61-Coupang Home黑貓、80-酷澎全家、81-酷澎711、82-酷澎黑貓 
            //明細表掃到0H4時需轉換爲9015的號碼
            var transList = new List<string>
            {
                "60","61", "80", "81", "82"
            };

            if ((dataType == "TACT" || dataType == "FTZ") && transList.Contains(trans))
            {
                sb.Append("with PdtScanCargoUpload as ( ");
                sb.Append("select a.Id,a.ArrivalTime,a.UploadTime,Data,DESPATCHNO,[jetf].[dbo].[GetTRANS_NAME](CLEARANCEWAREHOUSING) as TransName,FIELD_X,CarNo,TRACKINGNO from ( ");
                sb.Append("	 select a.Id,a.ArrivalTime,a.UploadTime,ROW_NUMBER() OVER (PARTITION BY isnull(b.TRACKINGNO,a.Data) ORDER BY isnull(b.TRACKINGNO,a.Data)) as ROW_ID,isnull(b.TRACKINGNO,a.Data) as Data,isnull(b.DESPATCHNO,c.DESPATCHNO) as DESPATCHNO,isnull(b.CLEARANCEWAREHOUSING,c.CLEARANCEWAREHOUSING) as CLEARANCEWAREHOUSING,isnull(b.FIELD_X,c.FIELD_X) as FIELD_X,CarNo,isnull(b.TRACKINGNO,c.TRACKINGNO) as TRACKINGNO  from [jetf].[dbo].[PdtScanCargoUpload] a ");
                sb.Append("     left join DATA_CENTER.[dbo].[ORIGINALLIST] b on a.Data=b.BAGNO and b.CREATEDATE >'2026-01-01 00:00:00' ");
                sb.Append("     left join DATA_CENTER.[dbo].[ORIGINALLIST] c on a.Data=c.TRACKINGNO and c.CREATEDATE >'2026-01-01 00:00:00' ");
                sb.Append("	    where a.TransNo=@TransNo and a.DataType=@DataType and a.UploadTime between @SDate and @EDate ");
                sb.Append(") a where ROW_ID='1' ");
                sb.Append(") ");
                sb.Append("select b.DESPATCHNAME,a.* from PdtScanCargoUpload a ");
                sb.Append("left join [DATA_CENTER].[dbo].[DESPATCHFROM] b on  a.DESPATCHNO=b.DESPATCHNO ");
            }
            else
            {
                sb.Append("with PdtScanCargoUpload as ( ");
                sb.Append("select a.Id,a.ArrivalTime,a.UploadTime,Data,DESPATCHNO,[jetf].[dbo].[GetTRANS_NAME](CLEARANCEWAREHOUSING) as TransName,FIELD_X,CarNo,TRACKINGNO from ( ");
                sb.Append("	 select a.Id,a.ArrivalTime,a.UploadTime,ROW_NUMBER() OVER (PARTITION BY a.Data ORDER BY a.Data) as ROW_ID,a.Data,isnull(b.DESPATCHNO,c.DESPATCHNO) as DESPATCHNO,isnull(b.CLEARANCEWAREHOUSING,c.CLEARANCEWAREHOUSING) as CLEARANCEWAREHOUSING,isnull(b.FIELD_X,c.FIELD_X) as FIELD_X,CarNo,isnull(b.TRACKINGNO,c.TRACKINGNO) as TRACKINGNO from [jetf].[dbo].[PdtScanCargoUpload] a ");
                sb.Append("     left join DATA_CENTER.[dbo].[ORIGINALLIST] b on a.Data=b.BAGNO and b.CREATEDATE >'2026-01-01 00:00:00' ");
                sb.Append("     left join DATA_CENTER.[dbo].[ORIGINALLIST] c on a.Data=c.TRACKINGNO and c.CREATEDATE >'2026-01-01 00:00:00' ");
                sb.Append("	    where a.TransNo=@TransNo and a.DataType=@DataType and a.UploadTime between @SDate and @EDate ");
                sb.Append(") a where ROW_ID='1' ");
                sb.Append(") ");
                sb.Append("select b.DESPATCHNAME,a.* from PdtScanCargoUpload a ");
                sb.Append("left join [DATA_CENTER].[dbo].[DESPATCHFROM] b on  a.DESPATCHNO=b.DESPATCHNO ");
            }

            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@TransNo", SqlDbType.NVarChar).Value = trans;
                da.SelectCommand.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = dataType;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} :00";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} :59";
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得掃貨上車PDT資料(客戶)-排除空快回艙資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetScanCargoCustomerDetailsExclude(DataTable dt, DataTable dt_Exclude)
        {
            //掃貨上車資料，空快回艙有掃到需要移除
            if (dt.Rows.Count > 0 && dt_Exclude.Rows.Count > 0)
            {
                var filter = from r in dt.AsEnumerable()
                             where !dt_Exclude.AsEnumerable()
                                              .Any(x => r.Field<string>("Data") == x.Field<string>("Data"))
                             select r;

                dt = filter.CopyToDataTable();
            }
            return dt;
        }





        /// <summary>
        /// 取得客戶明細PDF
        /// </summary>
        /// <param name="pdfDoc"></param>
        /// <param name="dt"></param>
        /// <param name="dataDate"></param>
        void GetCustomerDetailsPdf(Document pdfDoc, DataTable dt, string dataDate)
        {
            string carNo;
            DataRow[] dr;
            PdfPTable table;
            string transName, customer;
            int page;
            int count = 100; //一頁筆數
                             //派件公司分頁
            var dt_Group = from t in dt.AsEnumerable()
                           group t by new { TransName = t.Field<string>("TransName"), DESPATCHNAME = t.Field<string>("DESPATCHNAME") } into g
                           orderby g.Key.TransName, g.Key.DESPATCHNAME
                           select new
                           {
                               TransName = g.Key.TransName,
                               DESPATCHNAME = g.Key.DESPATCHNAME
                           };
            foreach (var item in dt_Group)
            {
                customer = item.DESPATCHNAME ?? "";
                transName = item.TransName ?? "";
                if (transName != "")
                {
                    dr = dt.Select($"TransName='{transName}' and DESPATCHNAME='{customer}'");
                }
                else
                {
                    dr = dt.Select("TransName is null and DESPATCHNAME is null");
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
                    table.AddCell(new PdfPCell(TabTitle(customer, transName, dataDate)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabBody(dr, i, count)) { PaddingTop = -15, Border = 0 });
                    table.AddCell(new PdfPCell(TabFooter(transName, carNo)) { PaddingTop = 0, Border = 0 });
                    pdfDoc.Add(table);
                }
                pdfDoc.NewPage();
            }

        }

        PdfPTable TabTitle(string customer, string transName, string dataDate)
        {
            PdfPTable table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550f;
            table.LockedWidth = true;
            table.AddCell(new PdfPCell(new Phrase("捷豐 貨物轉交 簽收單", font16)) { Border = 0, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase($"客戶：{customer}", font12)) { PaddingTop = -15, Border = 0, MinimumHeight = 15, HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(new PdfPCell(new Phrase($"派件公司：{transName}", font12)) { PaddingTop = -15, Border = 0, MinimumHeight = 15, HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(new PdfPCell(new Phrase($"日期：{dataDate}", font12)) { PaddingTop = -15, Border = 0, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_LEFT });
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
            string data, data2, field, field2;
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
                table.AddCell(new PdfPCell(new Phrase((i + 51).ToString(), font9)) { MinimumHeight = 10, HorizontalAlignment = Element.ALIGN_RIGHT });
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
        PdfPTable TabFooter(string transName, string carNo)
        {
            PdfPTable table = new PdfPTable(new float[] { 1, 1, 1, 1 });
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

        PdfPTable TabReceiptTitle(string transName, DateTime dataDate, string pdfTransName, string dataType)
        {
            PdfPTable table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550f;
            table.LockedWidth = true;
            table.AddCell(new PdfPCell(new Phrase($"{dataType}　{transName}　　{pdfTransName}", font16)) { MinimumHeight = 30, HorizontalAlignment = Element.ALIGN_CENTER, BorderWidth = 1f });
            table.AddCell(new PdfPCell(new Phrase($"交付日期　{dataDate.Year - 1911}年　{dataDate.Month}月　{dataDate.Day}日　{dataDate.Hour}：{dataDate.Minute}", font14)) { MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_CENTER, BorderWidth = 1f });
            return table;
        }

        PdfPTable TabReceiptBody(IEnumerable<ReportData> dt_Group, string unit)
        {
            PdfPTable tableCustomer = new PdfPTable(new float[] { 1, 1, 1 });
            tableCustomer.TotalWidth = 550;
            //table.LockedWidth = true;

            PdfPTable table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550;
            //table.LockedWidth = true;
            float borderWidth = 1f;

            foreach (var item in dt_Group)
            {
                tableCustomer.AddCell(new PdfPCell(new Phrase($"{item.DESPATCHNAME ?? "________________"}：{item.TotalCount} {unit}", font12)) { Border = 0, PaddingTop = 10, MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_LEFT });
            }

            //補上缺少的資料欄位，不補上資料會無法顯示
            var count = dt_Group.Count() % 3;
            if (count != 0)
            {
                for (int i = 0; i < 3 - count; i++)
                {
                    tableCustomer.AddCell(new PdfPCell(new Phrase("　", font12)) { Border = 0, PaddingTop = 10, MinimumHeight = 18, HorizontalAlignment = Element.ALIGN_LEFT });
                }
            }
            table.AddCell(new PdfPCell(tableCustomer) { Border = 0, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth, MinimumHeight = 40 });
            table.AddCell(new PdfPCell(new Phrase($"交付{unit}數：共　{dt_Group.Sum(r => r.TotalCount)} {unit}", font14)) { Border = 0, PaddingTop = 5, Colspan = 4, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            table.AddCell(new PdfPCell(new Phrase("捷豐人員：_____________________(簽名)　　　派件人員：_____________________(簽名)", font14)) { Border = 0, PaddingTop = 20, Colspan = 4, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            //table.AddCell(new PdfPCell(new Phrase("時　　間：_____________________", font14)) { Border = 0, PaddingTop = 15,PaddingBottom = 10, Colspan = 4, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            return table;
        }

        PdfPTable TabReceiptBody2(DateTime dataDate)
        {
            PdfPTable table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550;
            float borderWidth = 1f;
            table.AddCell(new PdfPCell(new Phrase("　", font14)) { Border = 0, PaddingTop = 15, PaddingBottom = 10, Colspan = 4, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            //table.AddCell(new PdfPCell(new Phrase($"時　　間：{dataDate.ToString("MM/dd HH:mm")}", font14)) { Border = 0, PaddingTop = 15, PaddingBottom = 10, Colspan = 4, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            //table.AddCell(new PdfPCell(new Phrase("時　　間：_____________________", font14)) { Border = 0, PaddingTop = 15, PaddingBottom = 10, Colspan = 4, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            return table;
        }

        PdfPTable TabReceiptBody3(string unit)
        {
            PdfPTable table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550;
            //table.LockedWidth = true;
            float borderWidth = 1f;

            table.AddCell(new PdfPCell(new Phrase("- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -", font14)) { Border = 0, Colspan = 4, HorizontalAlignment = Element.ALIGN_CENTER, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            table.AddCell(new PdfPCell(new Phrase($"實收{unit}數：_____________________({unit})　　　　退貨件數：_____________________", font14)) { Border = 0, PaddingTop = 10, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            table.AddCell(new PdfPCell(new Phrase("收倉人員：_____________________(簽名)", font14)) { Border = 0, PaddingTop = 20, PaddingBottom = 10, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_LEFT, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth });
            return table;
        }

        PdfPTable TabReceiptFooter(string remark)
        {
            PdfPTable table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550f;
            table.LockedWidth = true;
            float borderWidth = 1f;
            table.AddCell(new PdfPCell(new Phrase(remark, font12)) { PaddingTop = 5, PaddingBottom = 5, HorizontalAlignment = Element.ALIGN_CENTER, BorderWidth = borderWidth });
            return table;
        }


        public class ReportData
        {
            public string TransName { get; set; }

            public string DESPATCHNAME { get; set; }

            public int TotalCount { get; set; }
        }


        /// <summary>
        /// 取得客戶總表PDF
        /// </summary>
        /// <param name="pdfDoc"></param>
        /// <param name="dt"></param>
        /// <param name="dataDate"></param>
        void GetCustomerReportPdf(Document pdfDoc, DataTable dt, DateTime dataDate, string pdfTransNo, string dataType)
        {
            PdfPTable table;
            table = new PdfPTable(new float[] { 1 });
            table.TotalWidth = 550f;
            table.LockedWidth = true;

            //Pdt的派件公司名稱
            string pdfTransName = GetPdtTransName(pdfTransNo);

            var transNameGroup = dt.AsEnumerable().GroupBy(t => new { TransName = t.Field<string>("TransName") });
            int count = transNameGroup.Count();
            //單位
            string unit = pdfTransNo == "61" ? "件" : "袋";

            foreach (var item in transNameGroup)
            {
                var dt_Group = from t in dt.AsEnumerable()
                               where t.Field<string>("TransName") == item.Key.TransName
                               group t by new { TransName = t.Field<string>("TransName"), DESPATCHNAME = t.Field<string>("DESPATCHNAME") } into g
                               orderby g.Key.TransName, g.Key.DESPATCHNAME
                               select new ReportData
                               {
                                   TransName = g.Key.TransName,
                                   DESPATCHNAME = g.Key.DESPATCHNAME,
                                   TotalCount = g.Count()
                               };

                table = new PdfPTable(new float[] { 1 });
                table.TotalWidth = 550f;
                table.LockedWidth = true;

                int customerCount = dt_Group.Count();

                if (customerCount > 6)
                {
                    // 第一個區塊
                    table.AddCell(new PdfPCell(TabReceiptTitle(item.Key.TransName, dataDate, pdfTransName, dataType)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody(dt_Group, unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody2(dataDate)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptFooter("敬請查收無誤後在本單簽章，謝謝!")) { PaddingTop = 0, Border = 0 });

                    // 第二個區塊
                    table.AddCell(new PdfPCell(TabReceiptTitle(item.Key.TransName, dataDate, pdfTransName, dataType)) { PaddingTop = 25, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody(dt_Group, unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody3(unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptFooter("敬請查收無誤後在本單簽章，謝謝!(接駁)")) { PaddingTop = 0, Border = 0 });

                    pdfDoc.Add(table);
                    pdfDoc.NewPage();

                    // 第三個區塊（新頁面）
                    table = new PdfPTable(new float[] { 1 });
                    table.TotalWidth = 550f;
                    table.LockedWidth = true;

                    table.AddCell(new PdfPCell(TabReceiptTitle(item.Key.TransName, dataDate, pdfTransName, dataType)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody(dt_Group, unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody3(unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptFooter("敬請查收無誤後在本單簽章，謝謝!(尾程)")) { PaddingTop = 0, Border = 0 });
                }
                else
                {
                    // 原有邏輯：客戶數量 <= 6
                    table.AddCell(new PdfPCell(TabReceiptTitle(item.Key.TransName, dataDate, pdfTransName, dataType)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody(dt_Group, unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody2(dataDate)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptFooter("敬請查收無誤後在本單簽章，謝謝!")) { PaddingTop = 0, Border = 0 });

                    table.AddCell(new PdfPCell(TabReceiptTitle(item.Key.TransName, dataDate, pdfTransName, dataType)) { PaddingTop = 25, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody(dt_Group, unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody3(unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptFooter("敬請查收無誤後在本單簽章，謝謝!(接駁)")) { PaddingTop = 0, Border = 0 });

                    table.AddCell(new PdfPCell(TabReceiptTitle(item.Key.TransName, dataDate, pdfTransName, dataType)) { PaddingTop = 25, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody(dt_Group, unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptBody3(unit)) { PaddingTop = 0, Border = 0 });
                    table.AddCell(new PdfPCell(TabReceiptFooter("敬請查收無誤後在本單簽章，謝謝!(尾程)")) { PaddingTop = 0, Border = 0 });
                }

                pdfDoc.Add(table);
                pdfDoc.NewPage();
            }
        }

        /// <summary>
        /// 取得派件公司名稱
        /// </summary>
        /// <returns></returns>
        public string GetPdtTransName(string transNo)
        {
            string transName = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[PdtTrans] where TransNo=@TransNo ", conn))
            {
                da.SelectCommand.Parameters.Add("@TransNo", SqlDbType.NVarChar).Value = transNo;
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt);
            }

            if (dt.Rows.Count > 0)
            {
                transName = dt.Rows[0]["TransName"].ToString();
            }
            return transName;
        }
    }
}
