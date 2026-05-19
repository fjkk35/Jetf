using JETFTAX.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.DownloadEtlNew;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class DownloadEtlNewController : Controller
    {
        private readonly GlobalService _globalService;
        private readonly DownloadEtlNewService _downloadEtlNewService;

        public DownloadEtlNewController(GlobalService globalService, DownloadEtlNewService downloadEtlNewService)
        {
            _globalService = globalService;
            _downloadEtlNewService = downloadEtlNewService;
        }

        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult Index()
        {
            return View();
        }

        [UserAuthorize(Authority.DownloadEtlTax)]
        public JsonResult UploadEtl(DownloadEtlViewModel vm)
        {
            var responseModel = new ResponseModel();

            try
            {
                responseModel = _downloadEtlNewService.UploadEtl(vm.date, vm.timeBetween, vm.sTime, vm.eTime, Session["user_id"].ToString());
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult EtlExcel(DownloadEtlViewModel vm)
        {
            var dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            var handle = Guid.NewGuid().ToString();
            var fileName = string.Empty;
            var msg = string.Empty;

            switch (vm.timeBetween)
            {
                case "1":
                    if (vm.company == "新竹物流")
                    {
                        fileName = string.Format("{0}-菜鳥新竹-票.xlsx", dataDate);
                    }
                    else if (vm.company == "新瑞宅配")
                    {
                        fileName = string.Format("{0}-菜鳥全速配-票.xlsx", dataDate);
                    }
                    else if (vm.company == "圓通自取")
                    {
                        fileName = string.Format("{0}-菜鳥圓通-票.xlsx", dataDate);
                    }
                    break;
                case "2":
                    if (vm.company == "新竹物流")
                    {
                        fileName = string.Format("{0}-下午菜鳥新竹-票.xlsx", dataDate);
                    }
                    else if (vm.company == "新瑞宅配")
                    {
                        fileName = string.Format("{0}-下午菜鳥全速配-票.xlsx", dataDate);
                    }
                    else if (vm.company == "圓通自取")
                    {
                        fileName = string.Format("{0}-下午菜鳥圓通-票.xlsx", dataDate);
                    }
                    break;
                case "3":
                    if (vm.company == "新竹物流")
                    {
                        fileName = string.Format("{0}-菜鳥當配-票.xlsx", dataDate);
                    }
                    else if (vm.company == "圓通自取")
                    {
                        fileName = string.Format("{0}-菜鳥圓通-票.xlsx", dataDate);
                    }
                    break;
            }

            try
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reportResult = _downloadEtlNewService.GetEtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "N");
                    msg = reportResult.msg;

                    if (reportResult.status == Status.success)
                    {
                        var workbook = GetEtlWorkbook(reportResult.Rows);
                        fileName = fileName.Replace("票", string.Format("{0}票", reportResult.Rows.Count));

                        using (var fileStream = new MemoryStream())
                        {
                            workbook.Write(fileStream);
                            TempData[handle] = fileStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return Json(new { fileGuid = handle, fileName = AppendTestSuffix(fileName), msg = msg }, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult EtlErrorExcel(DownloadEtlViewModel vm)
        {
            var reportResult = _downloadEtlNewService.GetEtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, string.Empty);
            var dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            var workbook = GetEtlWorkbook(reportResult.Rows);
            var handle = Guid.NewGuid().ToString();
            var fileName = string.Format("{0}-空運-無客戶-{1}票.xlsx", dataDate, reportResult.Rows.Count);

            using (var fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return Json(new { fileGuid = handle, fileName = AppendTestSuffix(fileName), msg = reportResult.msg }, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult EtlSpecialDExcel(DownloadEtlViewModel vm)
        {
            var dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            var handle = Guid.NewGuid().ToString();
            var fileName = string.Empty;

            switch (vm.timeBetween)
            {
                case "1":
                    fileName = string.Format("{0}-菜鳥-特殊客戶(收客匯款)-票.xlsx", dataDate);
                    break;
                case "2":
                    fileName = string.Format("{0}-下午菜鳥-特殊客戶(收客匯款)-票.xlsx", dataDate);
                    break;
                case "3":
                    fileName = string.Format("{0}-菜鳥當配-特殊客戶(收客匯款)-票.xlsx", dataDate);
                    break;
            }

            var reportResult = new DownloadEtlNewReportResult();
            try
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    reportResult = _downloadEtlNewService.GetEtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "D");
                    if (reportResult.status == Status.success)
                    {
                        var workbook = GetEtlSpecialWorkbook(reportResult.Rows);
                        fileName = fileName.Replace("票", string.Format("{0}票", reportResult.Rows.Count));

                        using (var fileStream = new MemoryStream())
                        {
                            workbook.Write(fileStream);
                            TempData[handle] = fileStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                reportResult.msg = ex.Message;
            }

            return Json(new { fileGuid = handle, fileName = AppendTestSuffix(fileName), msg = reportResult.msg }, JsonRequestBehavior.AllowGet);
        }

        private static string AppendTestSuffix(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }

            return fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? fileName.Replace(".xlsx", "(測試).xlsx")
                : fileName + "(測試)";
        }

        private IWorkbook GetEtlWorkbook(IReadOnlyList<DownloadEtlNewReportItem> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("報表");
            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("清關袋號");
            row.CreateCell(2).SetCellValue("運單號");
            row.CreateCell(3).SetCellValue("稅金");
            row.CreateCell(4).SetCellValue("納稅義務人");
            row.CreateCell(5).SetCellValue("電話");
            row.CreateCell(6).SetCellValue("派件公司");
            row.CreateCell(7).SetCellValue("稅金類別");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);

            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(item.BagNumber ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.DlvInv ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.ToDlvCod);
                row.CreateCell(4).SetCellValue(item.Recipient ?? string.Empty);
                row.CreateCell(5).SetCellValue(item.RecPhone ?? string.Empty);
                row.CreateCell(6).SetCellValue(item.TransName ?? string.Empty);
                row.CreateCell(7).SetCellValue(_globalService.GetTaxType(item.IncludeTax ?? string.Empty));
            }

            return workbook;
        }

        private IWorkbook GetEtlSpecialWorkbook(IReadOnlyList<DownloadEtlNewReportItem> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("報表");
            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("清關袋號");
            row.CreateCell(2).SetCellValue("運單號");
            row.CreateCell(3).SetCellValue("稅金1");
            row.CreateCell(4).SetCellValue("稅金2");
            row.CreateCell(5).SetCellValue("手續費");
            row.CreateCell(6).SetCellValue("到付款");
            row.CreateCell(7).SetCellValue("小計");
            row.CreateCell(8).SetCellValue("納稅義務人");
            row.CreateCell(9).SetCellValue("電話");
            row.CreateCell(10).SetCellValue("派件公司");
            row.CreateCell(11).SetCellValue("稅金類別");

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

            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(item.BagNumber ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.DlvInv ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.Tax1);
                row.CreateCell(4).SetCellValue(item.Tax2);
                row.CreateCell(5).SetCellValue(item.Fee);
                row.CreateCell(6).SetCellValue(item.Cod);
                row.CreateCell(7).SetCellValue(item.Tax1 + item.Tax2 + item.Fee + item.Cod);
                row.CreateCell(8).SetCellValue(item.Recipient ?? string.Empty);
                row.CreateCell(9).SetCellValue(item.RecPhone ?? string.Empty);
                row.CreateCell(10).SetCellValue(item.TransName ?? string.Empty);
                row.CreateCell(11).SetCellValue(_globalService.GetTaxType(item.IncludeTax ?? string.Empty));
            }

            return workbook;
        }
    }
}