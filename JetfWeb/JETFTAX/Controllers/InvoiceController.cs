using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
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
    public class InvoiceController : Controller
    {
        GlobalService globalService = new GlobalService();
        InvoiceService invoiceService = new InvoiceService();

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle, date2Style;

        /// <summary>
        /// 6-1.開立電子發票作業
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.InvoiceProcessing)]
        public ActionResult InvoiceWork()
        {
            return View();
        }

        /// <summary>
        /// 6-1.開立電子發票作業
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.InvoiceProcessing)]
        public JsonResult InvoiceWork(HttpPostedFileBase file)
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
                            //寫入資料
                            resopnseModel = invoiceService.InvoiceWork(filePath, fileName, Session["user_id"].ToString());
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
        /// 6-1.開立電子發票作業Excel
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.InvoiceProcessing)]
        public ActionResult InvoiceWorkExcel(string upload_time, string upload_ope)
        {
            string fileName = "";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetInvoiceWorkWorkbook(upload_time, upload_ope);
                fileName = $"{DateTime.Now.ToString("yyyyMMdd")}開立電子發票作業_{DateTime.Now.ToString("HHmmss")}.xlsx";
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
        /// 批量貨況查詢明細表Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        IWorkbook GetInvoiceWorkWorkbook(string upload_time, string upload_ope)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得空快錯單袋號資料
            DataTable dt = invoiceService.GetInvoiceWork(upload_time, upload_ope).dt;
            //產生EXCEL
            GetInvoiceWorkSheet(workbook, dt, "開立電子發票作業");
            return workbook;
        }

        /// <summary>
        /// 開立電子發票作業Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetInvoiceWorkSheet(IWorkbook workbook, DataTable dt, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("序號");
            row.CreateCell(1).SetCellValue("資料代號");
            row.CreateCell(2).SetCellValue("發票號碼");
            row.CreateCell(3).SetCellValue("發票日期");
            row.CreateCell(4).SetCellValue("發票時間");
            row.CreateCell(5).SetCellValue("應稅發票總銷售額(不含稅金額)");
            row.CreateCell(6).SetCellValue("免稅發票總銷售額");
            row.CreateCell(7).SetCellValue("零稅率發票總銷售額");
            row.CreateCell(8).SetCellValue("發票總稅額");
            row.CreateCell(9).SetCellValue("發票總金額(含稅)");
            row.CreateCell(10).SetCellValue("統一編號");
            row.CreateCell(11).SetCellValue("統編抬頭");
            row.CreateCell(12).SetCellValue("發票開立通知方式");
            row.CreateCell(13).SetCellValue("收件人Email");
            row.CreateCell(14).SetCellValue("收件人手機");
            row.CreateCell(15).SetCellValue("銷售單交易識別碼");
            row.CreateCell(16).SetCellValue("銷售單交易編號");
            row.CreateCell(17).SetCellValue("銷售單交易日期");
            row.CreateCell(18).SetCellValue("銷售單交易時間");
            row.CreateCell(19).SetCellValue("個人/公司識別碼");
            row.CreateCell(20).SetCellValue("會員登入帳號");
            row.CreateCell(21).SetCellValue("發票第一聯說明文字");
            row.CreateCell(22).SetCellValue("發票備註");
            row.CreateCell(23).SetCellValue("通關方式註記");
            row.CreateCell(24).SetCellValue("買受人註記欄");
            row.CreateCell(25).SetCellValue("買受人簽署適用零稅率註記");
            row.CreateCell(26).SetCellValue("發票收件地址-郵遞區號");
            row.CreateCell(27).SetCellValue("發票收件地址-街道路名");
            row.CreateCell(28).SetCellValue("銷售項目序號");
            row.CreateCell(29).SetCellValue("銷售品名");
            row.CreateCell(30).SetCellValue("銷售數量");
            row.CreateCell(31).SetCellValue("未稅單價");
            row.CreateCell(32).SetCellValue("品項未稅銷售額");
            row.CreateCell(33).SetCellValue("銷稅稅別");
            row.CreateCell(34).SetCellValue("產品描述");
            row.CreateCell(35).SetCellValue("單位");
            row.CreateCell(36).SetCellValue("單一欄位備註");
            row.CreateCell(37).SetCellValue("相關號碼");
            row.CreateCell(38).SetCellValue("沖帳別");
            row.CreateCell(39).SetCellValue("相關號碼");
            row.CreateCell(40).SetCellValue("彙開註記");
            row.CreateCell(41).SetCellValue("扣抵金額");
            row.CreateCell(42).SetCellValue("原幣金額");
            row.CreateCell(43).SetCellValue("匯率");
            row.CreateCell(44).SetCellValue("幣別");
            row.CreateCell(45).SetCellValue("發票列印方式");
            row.CreateCell(46).SetCellValue("發票收件人");

            for (int i = 0; i < 47; i++)
            {
                row.GetCell(i).CellStyle = cs_Center;
                sheet.AutoSizeColumn(i);
            }

            sheet.SetColumnWidth(2,6000);

            int irow = 1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["Seq"].ToString());
                row.CreateCell(1).SetCellValue("M");
                row.CreateCell(2).SetCellValue(dt.Rows[i]["InvoiceNo"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["InvoiceDate"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["Amount"].ToString());
                row.CreateCell(6).SetCellValue("0");
                row.CreateCell(7).SetCellValue("0");
                row.CreateCell(8).SetCellValue(dt.Rows[i]["Tax"].ToString());
                row.CreateCell(9).SetCellValue(dt.Rows[i]["TotalAmount"].ToString());
                row.CreateCell(10).SetCellValue(dt.Rows[i]["VATNo"].ToString());
                row.CreateCell(11).SetCellValue(dt.Rows[i]["VATTitle"].ToString());
                row.CreateCell(12).SetCellValue("0");
                row.CreateCell(13).SetCellValue(dt.Rows[i]["Email"].ToString());
                row.CreateCell(45).SetCellValue("4");
                irow++;

                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["Seq"].ToString());
                row.CreateCell(1).SetCellValue("D");
                row.CreateCell(28).SetCellValue("0001");
                row.CreateCell(29).SetCellValue(dt.Rows[i]["ProductName"].ToString());
                row.CreateCell(30).SetCellValue("1");
                row.CreateCell(31).SetCellValue(dt.Rows[i]["Amount"].ToString());
                row.CreateCell(32).SetCellValue(dt.Rows[i]["Amount"].ToString());
                row.CreateCell(33).SetCellValue("T");
                row.CreateCell(36).SetCellValue(dt.Rows[i]["TrackingNo"].ToString());
                irow++;
            }
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