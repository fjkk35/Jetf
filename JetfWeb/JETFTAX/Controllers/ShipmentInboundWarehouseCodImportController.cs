using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services.ShipmentInboundWarehouseCodImport;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 倉庫代收上傳控制器。
    /// </summary>
    public sealed class ShipmentInboundWarehouseCodImportController : Controller
    {
        private readonly ShipmentInboundWarehouseCodImportService _service;

        /// <summary>
        /// 建立倉庫代收上傳控制器。
        /// </summary>
        /// <param name="service">倉庫代收上傳服務。</param>
        public ShipmentInboundWarehouseCodImportController(
            ShipmentInboundWarehouseCodImportService service)
        {
            _service = service;
        }

        /// <summary>
        /// 倉庫代收上傳頁面。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.ShipmentInboundWarehouseCodImport)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 下載倉庫代收上傳 Excel 範例。
        /// </summary>
        /// <returns>Excel 範例檔案。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundWarehouseCodImport)]
        public ActionResult DownloadExample()
        {
            var workbook = new XSSFWorkbook();
            try
            {
                var sheet = workbook.CreateSheet("倉庫代收");
                var headers = new[]
                {
                    "託運單號", "訂單編號", "件數", "客戶", "收件人", "地址", "電話",
                    "類別", "客代", "狀態", "代收款", "模式", "廠商對應單號", "訂單狀態"
                };

                var headerRow = sheet.CreateRow(0);
                var headerStyle = workbook.CreateCellStyle();
                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);
                for (var index = 0; index < headers.Length; index++)
                {
                    var cell = headerRow.CreateCell(index);
                    cell.SetCellValue(headers[index]);
                    cell.CellStyle = headerStyle;
                }

                var dataRow = sheet.CreateRow(1);
                dataRow.CreateCell(0).SetCellValue("05705950927");
                dataRow.CreateCell(1).SetCellValue("20260701061111781");
                dataRow.CreateCell(2).SetCellValue(1);
                dataRow.CreateCell(3).SetCellValue("久爺-maogougo");
                dataRow.CreateCell(4).SetCellValue("王小明");
                dataRow.CreateCell(5).SetCellValue(string.Empty);
                dataRow.CreateCell(6).SetCellValue("0911123456");
                dataRow.CreateCell(7).SetCellValue("日翊物流");
                dataRow.CreateCell(8).SetCellValue("0001");
                dataRow.CreateCell(9).SetCellValue("配送完成");
                dataRow.CreateCell(10).SetCellValue(1000);
                dataRow.CreateCell(11).SetCellValue("正物流");
                dataRow.CreateCell(12).SetCellValue("B400000000001");
                dataRow.CreateCell(13).SetCellValue("已完成");

                for (var index = 0; index < headers.Length; index++)
                {
                    sheet.AutoSizeColumn(index);
                }

                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "倉庫代收上傳_範例.xlsx");
                }
            }
            finally
            {
                workbook.Close();
            }
        }

        /// <summary>
        /// 保存並處理倉庫代收 Excel 檔案。
        /// </summary>
        /// <param name="file">xlsx 檔案。</param>
        /// <returns>上傳結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundWarehouseCodImport)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            var response = new ResponseModel();
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new ResponseModel("未選擇檔案"));
                }

                var extension = Path.GetExtension(file.FileName);
                if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new ResponseModel("副檔名需為 xlsx"));
                }

                var uploadDirectory = Server.MapPath("~/UploadFIle");
                Directory.CreateDirectory(uploadDirectory);
                var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                var fileName = $"{originalName}_{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";
                var filePath = Path.Combine(uploadDirectory, fileName);
                file.SaveAs(filePath);

                response = _service.Upload(filePath);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.status = Status.error;
                response.msg = ex.Message;
            }

            return Json(response);
        }
    }
}
