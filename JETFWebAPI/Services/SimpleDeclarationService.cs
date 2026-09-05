using Dapper;
using iTextSharp.text.pdf;
using iTextSharp.text;
using JETFWebAPI.Models;
using JETFWebAPI.Models.SimpleDeclaration;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Web;
using JETFWebAPI.Models.Jetf;
using System.Drawing;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace JETFWebAPI.Services
{
    public class SimpleDeclarationService : _BaseService
    {
        iTextSharp.text.Font fontTitle;
        iTextSharp.text.Font font12;
        iTextSharp.text.Font fontBody;

        /// <summary>
        /// 簡易報單Pdf查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public SimpleDeclarationPdfResponseModel PostSimpleDeclarationPdf(SimpleDeclarationPdfModel body)
        {
            string trackingNo = body.TrackingNo;

            try
            {
                var clearacceTax = GetClearacceTax(trackingNo);

                var isEtl = IsEtl(clearacceTax.FirstOrDefault()?.Data_Type);

                if (clearacceTax.Any() == false || GetOrderEdit(trackingNo, isEtl).Any() == false)
                {
                    return new SimpleDeclarationPdfResponseModel()
                    {
                        Status = "Success",
                        ResultCode = "N",
                        ResultMessage = "查無稅金資料",
                        TrackingNo = trackingNo
                    };
                }

                //測試
                //string url = "http://localhost:56641/api/Jetf/GetSimpleDeclarationPdf";
                string url = "https://service.jet-f.com/JETFWebAPI/api/Jetf/GetSimpleDeclarationPdf";

#if DEBUG
                url = "http://localhost:56641/api/Jetf/GetSimpleDeclarationPdf";
#endif

                return new SimpleDeclarationPdfResponseModel()
                {
                    Status = "Success",
                    ResultCode = "Y",
                    Url = $"{url}?trackingNo={trackingNo}&token={GetToken("GetSimpleDeclarationPdf", trackingNo)}",
                    TrackingNo = trackingNo
                };
            }
            catch (Exception ex)
            {
                return new SimpleDeclarationPdfResponseModel()
                {
                    Status = "Fail",
                    ResultMessage = ex.Message,
                    TrackingNo = trackingNo
                };
            }
        }


        /// <summary>
        /// 取得簡易報單PDF
        /// </summary>
        public byte[] GetSimpleDeclarationPdf(string trackingNo)
        {
            FontFactory.Register(@"C:\windows\Fonts\msjh.ttc");
            fontTitle = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 16f);
            font12 = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 11f);
            fontBody = FontFactory.GetFont("微軟正黑體", BaseFont.IDENTITY_H, 10f);

            //取得資料
            var data = GetData(trackingNo);

            //查無資料
            if (data.ClearacceTaxList.Any() == false || data.SeaOrderEditList.Any() == false)
            {
                return null;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                var pdfDoc = new Document(PageSize.A4.Rotate(), 0, 0, 20f, 10f);
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var size = 5;

                data.ClearacceTaxList.ForEach(clearacceTax =>
                {
                    for (var i = 0; i < data.SeaOrderEditList.Count; i += size)
                    {
                        //是否最後一次執行
                        bool isLast = (i + size >= data.SeaOrderEditList.Count);

                        var list = data.SeaOrderEditList.Skip(i).Take(size).ToList();

                        var table = new PdfPTable(new float[] { 1 });
                        table.TotalWidth = 800f;
                        table.LockedWidth = true;
                        table.AddCell(new PdfPCell(TableTitle(data)) { PaddingTop = 0, Border = 0 });
                        table.AddCell(new PdfPCell(TableBody(list)) { PaddingTop = 0, Border = 0 });
                        if (isLast)
                        {
                            table.AddCell(new PdfPCell(TableFooter(clearacceTax)) { PaddingTop = 0, Border = 0 });
                        }

                        pdfDoc.Add(table);
                        pdfDoc.NewPage();
                    }

                });



                pdfDoc.Close();
                return stream.ToArray();
            }

        }

        PdfPTable TableTitle(SimpleDeclarationModel data)
        {
            var date = DateTime.Now.ToString("yyyy/MM/dd");
            PdfPTable table = new PdfPTable(new float[] { 1, 1, 1 });
            table.TotalWidth = 800f;
            table.LockedWidth = true;

            table.AddCell(new PdfPCell(new Phrase("進口快遞貨物簡易申報單", fontTitle)) { Colspan = 3, Border = 0, MinimumHeight = 20, HorizontalAlignment = Element.ALIGN_CENTER });

            table.AddCell(new PdfPCell(new Phrase($"主 提 單 號 碼：{data.ClearacceInfo?.Main_Number}", font12)) { Colspan = 2, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"列印日期：{date}", font12)) { Border = 0, MinimumHeight = 20 });

            table.AddCell(new PdfPCell(new Phrase($"報 關 人：{data.CustomsDeclarer}", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"報單號碼：{data.ClearacceInfo?.Clearance_Number}", font12)) { Colspan = 2, Border = 0, MinimumHeight = 20 });

            table.AddCell(new PdfPCell(new Phrase($"進 口 日 期 ：{data.CesMainOrder?.Field_Date?.ToString("yyyy/MM/dd")}", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"類別：{data.ClearacceInfo?.Clearance_Type}", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"航機班次：{data.CesMainOrder?.Field_B}", font12)) { Border = 0, MinimumHeight = 20 });

            table.AddCell(new PdfPCell(new Phrase($"進口人(納稅義務人)統一編號：{data.SeaOrderEditList.FirstOrDefault()?.Importer_Id}", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"報關日期：{data.CesMainOrder?.Field_Date?.ToString("yyyy/MM/dd")}", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"存放處所：{data.CesMainOrder?.Field_E}", font12)) { Border = 0, MinimumHeight = 20 });

            table.AddCell(new PdfPCell(new Phrase($"名稱：{data.SeaOrderEditList.FirstOrDefault()?.Importer}", font12)) { Colspan = 2, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"稅費帳號：{data.TaxAccount}", font12)) { Border = 0, MinimumHeight = 20 });

            table.AddCell(new PdfPCell(new Phrase($"地址：{data.SeaOrderEditList.FirstOrDefault()?.Im_Add}", font12)) { Colspan = 2, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"幣別：TWD 當期匯率：1", font12)) { Border = 0, MinimumHeight = 20 });

            table.AddCell(new PdfPCell(new Phrase($"快遞業者統一編號：24951752", font12)) { Colspan = 3, Border = 0, MinimumHeight = 20 });

            table.AddCell(new PdfPCell(new Phrase($"名稱：捷豐國際物流股份有限公司JET-F WORLDWIDE EXPRESS CO., LTD.", font12)) { Colspan = 3, Border = 0, MinimumHeight = 20 });

            return table;
        }

        PdfPTable TableBody(List<SeaOrderEdit> list)
        {
            var minimumHeight = 50;
            PdfPTable table = new PdfPTable(new float[]
            {
                1.4f,
                1.9f,
                0.6f,
                0.8f,
                0.8f,
                0.8f,
                0.6f,
                1.9f,
                0.6f,
                0.8f,
                0.8f,
                1.4f,
                0.6f,0.6f,0.6f,0.6f });
            table.TotalWidth = 800f;
            table.LockedWidth = true;

            table.AddCell(new PdfPCell(new Phrase($"分提單號碼", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"收貨人名稱/地址\r\n統一編號\r\n寄件人名稱", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"起運\r\n國別", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"總件數\r\n單位", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"總毛重\r\n(Kgs)", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"總淨重\r\n(Kgs)", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"項次", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"貨物名稱\r\n商標(牌名),規格等", font12)) { Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"生產\r\n國別", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"數量\r\n單位", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"完稅\r\n價格", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"進口稅率", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"貨物\r\n稅率", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"納稅\r\n辦法", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"驗貨\r\n檯號", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"特殊\r\n記載", font12)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = 20 });

            // 添加一條水平線
            iTextSharp.text.pdf.draw.LineSeparator lineSeparator = new iTextSharp.text.pdf.draw.LineSeparator();
            PdfPCell lineCell = new PdfPCell();
            lineCell.AddElement(lineSeparator);
            lineCell.Colspan = 16; // 設置跨越的列數
            lineCell.Border = 0;
            lineCell.MinimumHeight = 20;
            table.AddCell(lineCell);

            //資料
            list.ForEach(r =>
            {
                if (r.Gw > 0)
                {
                    table.AddCell(new PdfPCell(new Phrase($"{r.Bl_No}", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                    table.AddCell(new PdfPCell(new Phrase($"{r.Importer}\r\n{r.Im_Add}\r\n{r.Importer_Id}\r\n{r.Exporter}", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                    table.AddCell(new PdfPCell(new Phrase($"{r.Ex_CounrtyCode}", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                    table.AddCell(new PdfPCell(new Phrase($"{r.Piece}\r\n{r.Piece_Unit}", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                    table.AddCell(new PdfPCell(new Phrase($"{r.Gw}\r\n(Kgs)", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                    table.AddCell(new PdfPCell(new Phrase($"{r.Nw}\r\n(Kgs)", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                }
                else
                {
                    table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { Colspan = 6, Border = 0, MinimumHeight = minimumHeight });
                }


                table.AddCell(new PdfPCell(new Phrase($"{r.Item_No}", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"{r.Item_Name}\r\n{r.Trademark}", fontBody)) { Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"{r.MadeIn}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"{r.Quantity}\r\n{r.Quantity_Unit}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"{r.Invoice_Amount}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"{r.Tax1}\r\n{r.Ccc_Code}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"31", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
                table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = 0, MinimumHeight = minimumHeight });
            });

            return table;
        }

        PdfPTable TableFooter(ClearacceTaxModel clearacceTax)
        {
            PdfPTable table = new PdfPTable(new float[]
              {
                1.4f,
                1.9f,
                0.6f,
                0.8f,
                0.8f,
                0.8f,
                0.6f,
                1.9f,
                0.6f,
                0.8f,
                0.8f,
                1.8f,
                0.5f,0.5f,0.5f,0.5f });
            table.TotalWidth = 800f;
            table.LockedWidth = true;



            //進口稅
            table.AddCell(new PdfPCell(new Phrase($"進口稅：", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 13, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"{clearacceTax.ImportTax.ToString("N0")}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 2, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { Border = 0 });
            // 營業稅
            table.AddCell(new PdfPCell(new Phrase($"營業稅：", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 13, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"{clearacceTax.BusinessTax.ToString("N0")}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 2, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { Border = 0 });
            //稅費合計
            table.AddCell(new PdfPCell(new Phrase($"稅費合計：", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 13, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"{clearacceTax.Tax_Amount.ToString("N0")}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 2, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { Border = 0 });
            //營業稅稅基
            table.AddCell(new PdfPCell(new Phrase($"營業稅稅基：", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 13, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"{clearacceTax.Tax_Base.ToString("N0")}", fontBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = 2, Border = 0, MinimumHeight = 20 });
            table.AddCell(new PdfPCell(new Phrase($"", fontBody)) { Border = 0 });
            return table;
        }

        public SimpleDeclarationModel GetData(string blNo)
        {
            var result = new SimpleDeclarationModel();

            result.ClearacceTaxList = GetClearacceTax(blNo);

            var isEtl = IsEtl(result.ClearacceTaxList.FirstOrDefault()?.Data_Type);

            //報關人
            result.CustomsDeclarer = isEtl ? "TS1" : "619";

            //稅費帳號
            result.TaxAccount = isEtl ? "735324951752" : "681224951752";

            result.SeaOrderEditList = GetOrderEdit(blNo, isEtl);

            result.ClearacceInfo = GetClearacceInfo(blNo);

            if (result.ClearacceInfo != null)
            {
                result.CesMainOrder = GetCesMainOrder(result.ClearacceInfo.Main_Number, isEtl);
            }

            //空運起運國別
            if (isEtl)
            {
                var counrtyCode = GetFlightOrigin(result.CesMainOrder?.Field_B);
                result.SeaOrderEditList.ForEach(r =>
                {
                    r.Ex_CounrtyCode = counrtyCode;
                });
            }

            return result;
        }

        /// <summary>
        /// 製單
        /// </summary>
        /// <param name="blNo"></param>
        /// <param name="isEtl"></param>
        /// <returns></returns>
        public List<SeaOrderEdit> GetOrderEdit(string blNo, bool isEtl)
        {
            var sqlQuery = @"
select MAINNUMBER,BL_NO,IMPORTER_ID,IMPORTER,IM_ADD,EXPORTER,EX_COUNRTYCODE,PIECE,PIECE_UNIT,GW,NW,ITEM_NO,ITEM_NAME,MADEIN,CCC_CODE,TRADEMARK,QUANTITY,QUANTITY_UNIT,INVOICE_AMOUNT,b.Tax1 from DATA_CENTER.dbo.SEA_ORDER_EDIT a
left join jetf.dbo.TaxData b on a.CCC_CODE =b.ProductNo
where BL_NO=@BL_NO order by ITEM_NO";

            //空運
            if (isEtl)
            {
                sqlQuery = @"
select 
TRACKINGNO as 'Bl_No',
MainNumber,
RECID as 'Importer_Id',
RECIPIENT as 'Importer',
RECADDRESS as 'Im_Add',
SENDCOMPANY as 'Exporter',
ORIGIN as 'MadeIn',
PIECES as 'Piece',
'CTN' as 'Piece_Unit',
WEIGHT as 'Gw',
ITEMS as  'Item_Name',
QUANTITY,
Unit as 'Quantity_Unit',
UNITPRICE as 'Invoice_Amount',
LOCID as 'Ccc_Code',
b.Tax1
from DATA_CENTER.dbo.MAKELIST a
left join jetf.dbo.TaxData b on a.LOCID = b.ProductNo
where TRACKINGNO=@BL_NO
";
            }

            var list = conn.Query<SeaOrderEdit>(sqlQuery, new { BL_NO = blNo })
                        .OrderBy(r => r.Item_No).ToList();


            var i = 1;

            foreach (var item in list)
            {
                item.Invoice_Amount = Math.Round(item.Invoice_Amount);

                item.Gw = Math.Round(item.Gw, 2);

                item.Nw = Math.Round((item.Gw * 0.97m), 2);

                if (isEtl)
                {
                    item.Item_No = i;
                    i++;
                }
            }

            return list;
        }

        public CesMainOrderModel GetCesMainOrder(string mainNumber, bool isEtl)
        {
            var sqlQuery = @"
select FIELD_DATE,FIELD_B,FIELD_E from CES_MAIN_ORDER
where MAIN_NUMBER=@MAIN_NUMBER
";

            if (isEtl)
            {
                sqlQuery = @"
select 
DELIVERYDATE as 'Field_Date',
FLIGHTNUMBER as 'Field_B',
'C2038' as 'Field_E'
from MAINORDERINFO
where MAINNUMBER=@MAIN_NUMBER
";

            }
            var result = conn.QueryFirstOrDefault<CesMainOrderModel>(sqlQuery, new { MAIN_NUMBER = mainNumber });

            return result;
        }

        public ClearacceInfoModel GetClearacceInfo(string blNo)
        {
            var sqlQuery = @"select MAIN_NUMBER,CLEARANCE_NUMBER,CLEARANCE_TYPE from DATA_CENTER.dbo.CLEARANCE_INFO
                            where MERGE_NUMBER=@MERGE_NUMBER";

            var result = conn.QueryFirstOrDefault<ClearacceInfoModel>(sqlQuery, new { MERGE_NUMBER = blNo });

            //把CLEARANCE_NUMBER的第3、4如果是，X2,X3換成空白
            if (result != null)
            {
                result.Clearance_Number = Regex.Replace(result.Clearance_Number, @"(?<=^..)X[23]", "  ");
            }

            return result;
        }

        public List<ClearacceTaxModel> GetClearacceTax(string blNo)
        {
            var slqQuery = @"
                            select DATA_TYPE,TAX_BASE,TAX_AMOUNT from  DATA_CENTER.dbo.CLEARANCE_TAX
                            where MERGE_NUMBER=@MERGE_NUMBER and TAX_AMOUNT > 0";

            var result = conn.Query<ClearacceTaxModel>(slqQuery, new { MERGE_NUMBER = blNo }).ToList();

            if (result.Any())
            {
                result.ForEach(r =>
                {
                    r.BusinessTax = Math.Round(r.Tax_Base * 0.05);

                    //營業稅 > 稅費合計， 營業稅 = 稅費合計
                    if (r.BusinessTax > r.Tax_Amount)
                    {
                        r.BusinessTax = r.Tax_Amount;
                    }

                    r.ImportTax = r.Tax_Amount - r.BusinessTax;

                });
            }

            return result;
        }

        /// <summary>
        /// 取得空運起運國別
        /// </summary>
        /// <param name="flightNumber"></param>
        /// <returns></returns>
        public string GetFlightOrigin(string flightNumber) 
        {
            if(string.IsNullOrEmpty(flightNumber) == false)
                flightNumber = flightNumber.Replace(" ", "").Trim();

            var sqlQuery = @"select CounrtyCode from jetf.dbo.FlightOrigin
where FlightNumber=@FlightNumber";

            var result = conn.QueryFirstOrDefault<string>(sqlQuery, new { FlightNumber = flightNumber });

            return result;
        }

        public bool IsEtl(string type) 
        {
            return type == "FTZ" || type == "TACT" ? true : false;
        }
    }
}