using JETFTAX.Models;
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
    public class DownloadController : Controller
    {
        private readonly GlobalService _globalService;
        private readonly DownloadService _downloadService;
        private readonly DropDownListService _dropDownListService;

        public DownloadController(DropDownListService dropDownListService, GlobalService globalService, DownloadService downloadService)
        {
            _dropDownListService = dropDownListService;
            _globalService = globalService;
            _downloadService = downloadService;
        }

        /// <summary>
        /// 3-1.物流代收檔下載-海運
        //[UserAuthorize("1", "2")]
        //[UserAuthorize(Authority.DownloadSeaTax)]
        //public ActionResult DownloadSea()
        //{
        //    DownloadSeaViewModel vm = new DownloadSeaViewModel();
        //    vm.ddlTaxTypeList = _dropDownListService.GetSeaTaxTypeList();
        //    vm.date = DateTime.Now.ToString("yyyy-MM-dd");
        //    return View(vm);
        //}

        /// <summary>
        /// 3-1.物流代收檔下載-海運-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaExcel(DownloadSeaViewModel vm)
        {
            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            string date = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            string msg = "";

            DataTableModel dataTableModel = _downloadService.SeaReport(vm.date, vm.taxType, "N");
            msg = dataTableModel.msg;
            DataTable dt = dataTableModel.dt;

            IWorkbook workbook = GetSeaWorkbook(dt);

            switch (vm.taxType)
            {
                case "TPCT":
                    fileName = $"{date}-tpct-新竹-{dt.Rows.Count}票.xlsx";
                    break;
                case "TIPC":
                    fileName = $"{date}-港務新竹-{dt.Rows.Count}票.xlsx";
                    break;
                case "IPOST":
                    fileName = $"{date}-高雄新竹(億興)-{dt.Rows.Count}票.xlsx";
                    break;
                case "CHWN":
                    fileName = $"{date}-高雄新竹(全旺)-{dt.Rows.Count}票.xlsx";
                    break;
                case "JFKH":
                    fileName = $"{date}-高雄新竹(捷豐)-{dt.Rows.Count}票.xlsx";
                    break;
                case "WAHA":
                    fileName = $"{date}-萬海新竹-{dt.Rows.Count}票.xlsx";
                    break;
                case "UNIJ":
                    fileName = $"{date}-連捷-{dt.Rows.Count}票.xlsx";
                    break;
                case "JFKL":
                    fileName = $"{date}-基隆港務(捷豐)-{dt.Rows.Count}票.xlsx";
                    break;
            }
            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };
        }

        /// <summary>
        ///  3-1.物流代收檔下載-海運-Excel-頁籤-報表
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetSeaWorkbook(DataTable dt)
        {
            int to_dlv_cod;
            string remark;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("清關袋號");
            row.CreateCell(3).SetCellValue("運單號");
            row.CreateCell(4).SetCellValue("稅金");
            row.CreateCell(5).SetCellValue("納稅義務人");
            row.CreateCell(6).SetCellValue("電話");
            row.CreateCell(7).SetCellValue("備註");
            row.CreateCell(8).SetCellValue("派件公司");
            row.CreateCell(9).SetCellValue("稅金類別");

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


            for (int i = 0; i < dt.Rows.Count; i++)
            {

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["CUST_NAME"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                if (int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod))
                {
                    row.CreateCell(4).SetCellValue(to_dlv_cod);
                }
                row.CreateCell(5).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());
                remark = "單";
                if (dt.Rows[i]["COMBINE"].ToString() == "Y")
                {
                    remark = "併單";
                }
                else if (dt.Rows[i]["TYPE"].ToString() == "G")
                {
                    remark = "G類";
                }
                row.CreateCell(7).SetCellValue(remark);
                row.CreateCell(8).SetCellValue(dt.Rows[i]["DLV_COM"].ToString());
                row.CreateCell(9).SetCellValue(_globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString()));
            }

            return workbook;
        }

        /// <summary>
        /// 3-1.物流代收檔下載-海運-Excel-無客戶報表
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        ///[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaErrorExcel(DownloadSeaViewModel vm)
        {
            DataTableModel dataTableModel = _downloadService.SeaReport(vm.date, vm.taxType, "");
            DataTable dt = dataTableModel.dt;
            IWorkbook workbook = GetSeaWorkbook(dt);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            string date = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            switch (vm.taxType)
            {
                case "TPCT":
                    fileName = $"{date}-tpct-新竹-無客戶{dt.Rows.Count}-票.xlsx";
                    break;
                case "TIPC":
                    fileName = $"{date}-港務新竹-無客戶{dt.Rows.Count}-票.xlsx";
                    break;
                case "IPOST":
                    fileName = $"{date}-高雄新竹(億興)-無客戶{dt.Rows.Count}-票.xlsx";
                    break;
                case "CHWN":
                    fileName = $"{date}-高雄新竹(全旺)-無客戶{dt.Rows.Count}票.xlsx";
                    break;
                case "JFKH":
                    fileName = $"{date}-高雄新竹(捷豐)-無客戶{dt.Rows.Count}票.xlsx";
                    break;
                case "WAHA":
                    fileName = $"{date}-萬海新竹-無客戶{dt.Rows.Count}票.xlsx";
                    break;
                case "UNIJ":
                    fileName = $"{date}-連捷-無客戶{dt.Rows.Count}票.xlsx";
                    break;
            }

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 3-1.物流代收檔下載-海運-Excel-特殊客戶D報表
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaSpecialDExcel(DownloadSeaViewModel vm)
        {
            DataTableModel dataTableModel = _downloadService.SeaReport(vm.date, vm.taxType, "D");
            DataTable dt = dataTableModel.dt;
            IWorkbook workbook = GetSeaSpecialWorkbook(dt);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            string date = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            switch (vm.taxType)
            {
                case "TPCT":
                    fileName = $"{date}-tpct-新竹-特殊客戶(收客匯款){dt.Rows.Count}-票.xlsx";
                    break;
                case "TIPC":
                    fileName = $"{date}-港務新竹-特殊客戶(收客匯款){dt.Rows.Count}-票.xlsx";
                    break;
                case "IPOST":
                    fileName = $"{date}-高雄新竹(億興)-特殊客戶(收客匯款){dt.Rows.Count}-票.xlsx";
                    break;
                case "CHWN":
                    fileName = $"{date}-高雄新竹(全旺)-特殊客戶(收客匯款){dt.Rows.Count}票.xlsx";
                    break;
                case "JFKH":
                    fileName = $"{date}-高雄新竹(捷豐)-特殊客戶(收客匯款){dt.Rows.Count}票.xlsx";
                    break;
                case "WAHA":
                    fileName = $"{date}-萬海新竹-特殊客戶(收客匯款){dt.Rows.Count}票.xlsx";
                    break;
                case "UNIJ":
                    fileName = $"{date}-連捷-特殊客戶(收客匯款){dt.Rows.Count}票.xlsx";
                    break;
            }

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        ///  3-1.物流代收檔下載-海運-Excel-特殊客戶C報表
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaSpecialCExcel(DownloadSeaViewModel vm)
        {
            DataTableModel dataTableModel = _downloadService.SeaReport(vm.date, vm.taxType, "C");
            DataTable dt = dataTableModel.dt;
            IWorkbook workbook = GetSeaSpecialWorkbook(dt);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            string date = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            switch (vm.taxType)
            {
                case "TPCT":
                    fileName = $"{date}-tpct-新竹-特殊客戶(客戶付款){dt.Rows.Count}-票.xlsx";
                    break;
                case "TIPC":
                    fileName = $"{date}-港務新竹-特殊客戶(客戶付款){dt.Rows.Count}-票.xlsx";
                    break;
                case "IPOST":
                    fileName = $"{date}-高雄新竹-特殊客戶(客戶付款){dt.Rows.Count}-票.xlsx";
                    break;
            }

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        ///  3-1.物流代收檔下載-海運-Excel-特殊客戶報表-Workbook
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetSeaSpecialWorkbook(DataTable dt)
        {
            int tax1, tax2;
            string remark;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("清關袋號");
            row.CreateCell(3).SetCellValue("運單號");
            row.CreateCell(4).SetCellValue("稅金1");
            row.CreateCell(5).SetCellValue("稅金2");
            row.CreateCell(6).SetCellValue("納稅義務人");
            row.CreateCell(7).SetCellValue("電話");
            row.CreateCell(8).SetCellValue("備註");
            row.CreateCell(9).SetCellValue("稅金類別");

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


            for (int i = 0; i < dt.Rows.Count; i++)
            {
                tax1 = 0;
                tax2 = 0;
                int.TryParse(dt.Rows[i]["TAX1"].ToString(), out tax1);
                int.TryParse(dt.Rows[i]["TAX2"].ToString(), out tax2);

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["CUST_NAME"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(4).SetCellValue(tax1);
                row.CreateCell(5).SetCellValue(tax2);
                row.CreateCell(6).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                row.CreateCell(7).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());
                remark = "單";
                if (dt.Rows[i]["COMBINE"].ToString() == "Y")
                {
                    remark = "併單";
                }
                else if (dt.Rows[i]["TYPE"].ToString() == "G")
                {
                    remark = "G類";
                }
                row.CreateCell(8).SetCellValue(remark);
                row.CreateCell(9).SetCellValue(_globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString()));
            }

            return workbook;
        }

        /// <summary>
        /// 3-2.物流代收檔下載-空運
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "6")]
        //[UserAuthorize(Authority.DownloadEtlTax)]
        //public ActionResult DownloadEtl()
        //{
        //    DownloadEtlViewModel vm = new DownloadEtlViewModel();
        //    List<SelectListItem> timeBetweenList = new List<SelectListItem>();
        //    timeBetweenList.Add(new SelectListItem() { Text = "前一天22:00-當日08:00", Value = "1" });
        //    timeBetweenList.Add(new SelectListItem() { Text = "當日08:00-當日16:00", Value = "2" });
        //    timeBetweenList.Add(new SelectListItem() { Text = "當日21:00-當日22:00", Value = "3" });
        //    vm.ddlTimeBetweenList = timeBetweenList;

        //    vm.date = DateTime.Now.ToString("yyyy-MM-dd");
        //    return View(vm);
        //}

        /// <summary>
        ///3-2.物流代收檔下載-空運-稅金資料轉檔
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.DownloadEtlTax)]
        public JsonResult UploadEtl(DownloadEtlViewModel vm)
        {
            ResponseModel resopnseModel = new ResponseModel();

            try
            {
                resopnseModel = _downloadService.UploadEtl(vm.date, vm.timeBetween, vm.sTime, vm.eTime, UserContextService.GetUserId());
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 3-2.物流代收檔下載-空運-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "6")]
        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult EtlExcel(DownloadEtlViewModel vm)
        {
            string dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            string msg = "";
            switch (vm.timeBetween)
            {
                case "1":
                    if (vm.company == "新竹物流")
                    {
                        fileName = $"{dataDate}-菜鳥新竹-票.xlsx";
                    }
                    else if (vm.company == "新瑞宅配")
                    {
                        fileName = $"{dataDate}-菜鳥全速配-票.xlsx";
                    }
                    else if (vm.company == "圓通自取")
                    {
                        fileName = $"{dataDate}-菜鳥圓通-票.xlsx";
                    }
                    break;
                case "2":
                    if (vm.company == "新竹物流")
                    {
                        fileName = $"{dataDate}-下午菜鳥新竹-票.xlsx";
                    }
                    else if (vm.company == "新瑞宅配")
                    {
                        fileName = $"{dataDate}-下午菜鳥全速配-票.xlsx";
                    }
                    else if (vm.company == "圓通自取")
                    {
                        fileName = $"{dataDate}-下午菜鳥圓通-票.xlsx";
                    }
                    break;
                case "3":
                    if (vm.company == "新竹物流")
                    {
                        fileName = $"{dataDate}-菜鳥當配-票.xlsx";
                    }
                    else if (vm.company == "圓通自取")
                    {
                        fileName = $"{dataDate}-菜鳥圓通-票.xlsx";
                    }
                    break;
            }

            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                if (fileName != "")
                {
                    //取得資料
                    dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "N", UserContextService.GetUserId());
                    msg = dataTableModel.msg;

                    if (dataTableModel.status == Status.success)
                    {
                        DataTable dt = dataTableModel.dt;

                        IWorkbook workbook = GetEtlWorkbook(dt);

                        fileName = fileName.Replace("票", $"{dt.Rows.Count}票");
                        using (MemoryStream fileStream = new MemoryStream())
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


            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };
        }

        /// <summary>
        /// 3-2.物流代收檔下載-空運運-Excel-無客戶報表
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "6")]
        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult EtlErrorExcel(DownloadEtlViewModel vm)
        {
            DataTableModel dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "", UserContextService.GetUserId());
            DataTable dt = dataTableModel.dt;
            string dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            IWorkbook workbook = GetEtlWorkbook(dt);

            string handle = Guid.NewGuid().ToString();
            string fileName = $"{dataDate}-空運-無客戶-{dt.Rows.Count}票.xlsx"; ;

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 3-2.物流代收檔下載-空運-Excel-頁籤-報表
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetEtlWorkbook(DataTable dt)
        {
            int to_dlv_cod;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
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

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                to_dlv_cod = 0;
                int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod);
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["BAG_NUMBER"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(3).SetCellValue(to_dlv_cod);
                row.CreateCell(4).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["TRANS_NAME"].ToString());
                row.CreateCell(7).SetCellValue(_globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString()));
            }
            return workbook;
        }

        /// <summary>
        /// 3-2.物流代收檔下載-空運-Excel-頁籤-特殊客戶D報表
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "6")]
        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult EtlSpecialDExcel(DownloadEtlViewModel vm)
        {
            string dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            switch (vm.timeBetween)
            {
                case "1":
                    fileName = $"{dataDate}-菜鳥-特殊客戶(收客匯款)-票.xlsx";
                    break;
                case "2":
                    fileName = $"{dataDate}-下午菜鳥-特殊客戶(收客匯款)-票.xlsx";
                    break;
                case "3":
                    fileName = $"{dataDate}-菜鳥當配-特殊客戶(收客匯款)-票.xlsx";
                    break;
            }
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                if (fileName != "")
                {
                    //取得資料
                    dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "D", UserContextService.GetUserId());
                    if (dataTableModel.status == Status.success)
                    {
                        DataTable dt = dataTableModel.dt;

                        IWorkbook workbook = GetEtlSpecialWorkbook(dt);

                        fileName = fileName.Replace("票", $"{dt.Rows.Count}票");
                        using (MemoryStream fileStream = new MemoryStream())
                        {
                            workbook.Write(fileStream);
                            TempData[handle] = fileStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dataTableModel.msg = ex.Message;
            }
            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 3-2.物流代收檔下載-空運-Excel-頁籤-特殊客戶C報表
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "6")]
        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult EtlSpecialCExcel(DownloadEtlViewModel vm)
        {
            string dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            switch (vm.timeBetween)
            {
                case "1":
                    fileName = $"{dataDate}-菜鳥-特殊客戶(客戶付款)-票.xlsx";
                    break;
                case "2":
                    fileName = $"{dataDate}-下午菜鳥-特殊客戶(客戶付款)-票.xlsx";
                    break;
                case "3":
                    fileName = $"{dataDate}-菜鳥當配-特殊客戶(客戶付款)-票.xlsx";
                    break;
            }
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                if (fileName != "")
                {
                    //取得資料
                    dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "C", UserContextService.GetUserId());
                    if (dataTableModel.status == Status.success)
                    {
                        DataTable dt = dataTableModel.dt;

                        IWorkbook workbook = GetEtlSpecialWorkbook(dt);

                        fileName = fileName.Replace("票", $"{dt.Rows.Count}票");
                        using (MemoryStream fileStream = new MemoryStream())
                        {
                            workbook.Write(fileStream);
                            TempData[handle] = fileStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dataTableModel.msg = ex.Message;
            }
            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 3-2.物流代收檔下載-空運-Excel-頁籤-特殊客戶報表-Workbook
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public IWorkbook GetEtlSpecialWorkbook(DataTable dt)
        {
            int tax1, tax2, fee, cod;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
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

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                tax1 = 0;
                tax2 = 0;
                fee = 0;
                cod = 0;
                int.TryParse(dt.Rows[i]["TAX1"].ToString(), out tax1);
                int.TryParse(dt.Rows[i]["TAX2"].ToString(), out tax2);
                int.TryParse(dt.Rows[i]["FEE"].ToString(), out fee);
                int.TryParse(dt.Rows[i]["COD"].ToString(), out cod);

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["BAG_NUMBER"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(3).SetCellValue(tax1);
                row.CreateCell(4).SetCellValue(tax2);
                row.CreateCell(5).SetCellValue(fee);
                row.CreateCell(6).SetCellValue(cod);
                row.CreateCell(7).SetCellValue(tax1 + tax2 + fee + cod);
                row.CreateCell(8).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                row.CreateCell(9).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());
                row.CreateCell(10).SetCellValue(dt.Rows[i]["TRANS_NAME"].ToString());
                row.CreateCell(11).SetCellValue(_globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString()));
            }
            return workbook;
        }

        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadModifySeaTaxG)]
        public ActionResult DownloadSeaModifyG()
        {
            DownloadSeaModifyGViewModel vm = new DownloadSeaModifyGViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 3-5.G類稅金調整明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadModifySeaTaxG)]
        public ActionResult SeaModifyGExcel(DownloadSeaModifyGViewModel vm)
        {
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            DataTableModel dataTableModel = _downloadService.SeaModifyGReport(sDate, eDate);
            DataTable dt = dataTableModel.dt;

            IWorkbook workbook = GetSeaModifyGWorkbook(dt);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";

            fileName = $"{sDate}~{eDate}-G類稅金調整明細表-{dt.Rows.Count}票.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 3-5.G類稅金調整明細表-Excel-報表
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetSeaModifyGWorkbook(DataTable dt)
        {
            int tax1, tax2, ccfee, cod, fee, to_dlv_cod;
            string remark;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("狀態");
            row.CreateCell(2).SetCellValue("調整日");
            row.CreateCell(3).SetCellValue("作業日期");
            row.CreateCell(4).SetCellValue("倉儲");
            row.CreateCell(5).SetCellValue("客戶");
            row.CreateCell(6).SetCellValue("清關袋號");
            row.CreateCell(7).SetCellValue("運單號");
            row.CreateCell(8).SetCellValue("稅金1");
            row.CreateCell(9).SetCellValue("稅金2");
            row.CreateCell(10).SetCellValue("報關費");
            row.CreateCell(11).SetCellValue("到付款");
            row.CreateCell(12).SetCellValue("手續費");
            row.CreateCell(13).SetCellValue("代收貨款金額");
            row.CreateCell(14).SetCellValue("納稅義務人");
            row.CreateCell(15).SetCellValue("電話");
            row.CreateCell(16).SetCellValue("備註");
            row.CreateCell(17).SetCellValue("派件公司");
            row.CreateCell(18).SetCellValue("稅金類別");


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
            sheet.SetColumnWidth(12, 6000);
            sheet.SetColumnWidth(13, 6000);
            sheet.SetColumnWidth(14, 6000);
            sheet.SetColumnWidth(15, 6000);
            sheet.SetColumnWidth(16, 6000);
            sheet.SetColumnWidth(17, 6000);
            sheet.SetColumnWidth(18, 6000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {

                tax1 = 0;
                tax2 = 0;
                ccfee = 0;
                cod = 0;
                fee = 0;
                to_dlv_cod = 0;

                int.TryParse(dt.Rows[i]["TAX1"].ToString(), out tax1);
                int.TryParse(dt.Rows[i]["TAX2"].ToString(), out tax2);
                int.TryParse(dt.Rows[i]["CCFEE"].ToString(), out ccfee);
                int.TryParse(dt.Rows[i]["COD"].ToString(), out cod);
                int.TryParse(dt.Rows[i]["FEE"].ToString(), out fee);
                int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod);

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["MEMO"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["MODIFY_DATADATE"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["DATADATE"].ToString());
                row.CreateCell(4).SetCellValue(dt.Rows[i]["SOURCE"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["CUST_NAME"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                row.CreateCell(7).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(8).SetCellValue(tax1);
                row.CreateCell(9).SetCellValue(tax2);
                row.CreateCell(10).SetCellValue(ccfee);
                row.CreateCell(11).SetCellValue(cod);
                row.CreateCell(12).SetCellValue(fee);
                row.CreateCell(13).SetCellValue(to_dlv_cod);
                row.CreateCell(14).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                row.CreateCell(15).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());
                remark = "單";
                if (dt.Rows[i]["COMBINE"].ToString() == "Y")
                {
                    remark = "併單";
                }
                else if (dt.Rows[i]["TYPE"].ToString() == "G")
                {
                    remark = "G類";
                }
                row.CreateCell(16).SetCellValue(remark);
                row.CreateCell(17).SetCellValue(dt.Rows[i]["DLV_COM"].ToString());
                row.CreateCell(18).SetCellValue(_globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString()));
            }

            return workbook;
        }

        /// <summary>
        /// 3-6.海快TPCT及TIPC稅金調整明細表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadModifySeaTax)]
        public ActionResult DownloadSeaModify()
        {
            DownloadSeaModifyViewModel vm = new DownloadSeaModifyViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 3-6.海快TPCT及TIPC稅金調整明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadModifySeaTax)]
        public ActionResult SeaModifyExcel(DownloadSeaModifyViewModel vm)
        {
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            DataTableModel dataTableModel = _downloadService.SeaModifyReport(sDate, eDate);
            DataTable dt = dataTableModel.dt;

            IWorkbook workbook = GetSeaModifyWorkbook(dt);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";

            fileName = $"{sDate}~{eDate}-海快TPCT及TIPC稅金調整明細表-{dt.Rows.Count}票.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        ///  3-6.海快TPCT及TIPC稅金調整明細表-Excel-報表
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetSeaModifyWorkbook(DataTable dt)
        {
            int tax_Amount;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("調整日");
            row.CreateCell(2).SetCellValue("倉儲");
            row.CreateCell(3).SetCellValue("主提單號");
            row.CreateCell(4).SetCellValue("清關袋號");
            row.CreateCell(5).SetCellValue("併袋號");
            row.CreateCell(6).SetCellValue("稅單號碼");
            row.CreateCell(7).SetCellValue("運單號");
            row.CreateCell(8).SetCellValue("稅金");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                tax_Amount = 0;
                int.TryParse(dt.Rows[i]["TAX_AMOUNT"].ToString(), out tax_Amount);

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["MODIFY_DATADATE"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["DATA_TYPE"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["MAIN_NUMBER"].ToString());
                row.CreateCell(4).SetCellValue(dt.Rows[i]["BAG_NUMBER"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["MERGE_NUMBER"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["TAX_NUMBER"].ToString());
                row.CreateCell(7).SetCellValue(dt.Rows[i]["JETF_SERIAL"].ToString());
                row.CreateCell(8).SetCellValue(tax_Amount);

            }
            return workbook;
        }

        /// <summary>
        /// 4-2.物流代收金額差異表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadCollectibleAmount)]
        public ActionResult DownloadReceive()
        {
            DownloadReceiveViewModel vm = new DownloadReceiveViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 4-2.物流代收金額差異表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadCollectibleAmount)]
        public ActionResult ReceiveExcel(DownloadReceiveViewModel vm)
        {
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");

            string handle = Guid.NewGuid().ToString();
            string fileName = $"{sDate}-{eDate}物流代收金額差異表.xlsx";
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                //取得資料
                dataTableModel = _downloadService.ReceiveReport(sDate, eDate, UserContextService.GetUserId());
                if (dataTableModel.status == Status.success)
                {
                    int tax1, tax2, ccfee, cod, fee, to_dlv_cod, dlv_cod, diff;
                    DataTable dt = dataTableModel.dt;
                    IWorkbook workbook = new XSSFWorkbook();
                    ISheet sheet = workbook.CreateSheet("報表");
                    //表頭  
                    IRow row = sheet.CreateRow(0);
                    row.CreateCell(0).SetCellValue("項次");
                    row.CreateCell(1).SetCellValue("作業日");
                    row.CreateCell(2).SetCellValue("來源");
                    row.CreateCell(3).SetCellValue("報關類型");
                    row.CreateCell(4).SetCellValue("客戶名稱");
                    row.CreateCell(5).SetCellValue("是否包稅");
                    row.CreateCell(6).SetCellValue("查貨號碼");
                    row.CreateCell(7).SetCellValue("併單");
                    row.CreateCell(8).SetCellValue("稅金1");
                    row.CreateCell(9).SetCellValue("稅金2");
                    row.CreateCell(10).SetCellValue("報關費");
                    row.CreateCell(11).SetCellValue("到付款");
                    row.CreateCell(12).SetCellValue("手續費");
                    row.CreateCell(13).SetCellValue("代收貨款金額");
                    row.CreateCell(14).SetCellValue("派件公司");
                    row.CreateCell(15).SetCellValue("派件公司代收貨款");
                    row.CreateCell(16).SetCellValue("檢核碼");
                    row.CreateCell(17).SetCellValue("差額");
                    row.CreateCell(18).SetCellValue("檢核時間");

                    sheet.SetColumnWidth(0, 3000);
                    sheet.SetColumnWidth(1, 6000);
                    sheet.SetColumnWidth(2, 6000);
                    sheet.SetColumnWidth(3, 6000);
                    sheet.SetColumnWidth(4, 3000);
                    sheet.SetColumnWidth(5, 6000);
                    sheet.SetColumnWidth(6, 3000);
                    sheet.SetColumnWidth(7, 3000);
                    sheet.SetColumnWidth(8, 3000);
                    sheet.SetColumnWidth(9, 3000);
                    sheet.SetColumnWidth(10, 3000);
                    sheet.SetColumnWidth(11, 6000);
                    sheet.SetColumnWidth(12, 6000);
                    sheet.SetColumnWidth(13, 6000);
                    sheet.SetColumnWidth(14, 6000);
                    sheet.SetColumnWidth(15, 6000);
                    sheet.SetColumnWidth(16, 6000);
                    sheet.SetColumnWidth(17, 6000);
                    sheet.SetColumnWidth(18, 6000);
                    string dlv_cod_time;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (dt.Rows[i]["DLV_COD_TIME"].ToString() != "")
                        {
                            dlv_cod_time = Convert.ToDateTime(dt.Rows[i]["DLV_COD_TIME"]).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            dlv_cod_time = "";
                        }
                        tax1 = 0;
                        tax2 = 0;
                        ccfee = 0;
                        cod = 0;
                        fee = 0;
                        to_dlv_cod = 0;
                        dlv_cod = 0;

                        int.TryParse(dt.Rows[i]["TAX1"].ToString(), out tax1);
                        int.TryParse(dt.Rows[i]["TAX2"].ToString(), out tax2);
                        int.TryParse(dt.Rows[i]["CCFEE"].ToString(), out ccfee);
                        int.TryParse(dt.Rows[i]["COD"].ToString(), out cod);
                        int.TryParse(dt.Rows[i]["FEE"].ToString(), out fee);
                        int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod);
                        int.TryParse(dt.Rows[i]["DLV_COD"].ToString(), out dlv_cod);

                        //差額
                        diff = to_dlv_cod - dlv_cod;

                        row = sheet.CreateRow(i + 1);
                        row.CreateCell(0).SetCellValue(i + 1);
                        row.CreateCell(1).SetCellValue(dt.Rows[i]["DATADATE"].ToString());
                        row.CreateCell(2).SetCellValue(dt.Rows[i]["SOURCE"].ToString());
                        row.CreateCell(3).SetCellValue(dt.Rows[i]["TYPE"].ToString());
                        row.CreateCell(4).SetCellValue(dt.Rows[i]["CUSTOMER"].ToString());
                        row.CreateCell(5).SetCellValue(dt.Rows[i]["INCLUDE_TAX"].ToString());
                        row.CreateCell(6).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                        row.CreateCell(7).SetCellValue(dt.Rows[i]["COMBINE"].ToString());
                        row.CreateCell(8).SetCellValue(tax1);
                        row.CreateCell(9).SetCellValue(tax2);
                        row.CreateCell(10).SetCellValue(ccfee);
                        row.CreateCell(11).SetCellValue(cod);
                        row.CreateCell(12).SetCellValue(fee);
                        row.CreateCell(13).SetCellValue(to_dlv_cod);
                        row.CreateCell(14).SetCellValue(dt.Rows[i]["TRANS_NAME"].ToString());
                        row.CreateCell(15).SetCellValue(dlv_cod);
                        row.CreateCell(16).SetCellValue(dt.Rows[i]["DLV_COD_CODE"].ToString());
                        row.CreateCell(17).SetCellValue(diff);
                        row.CreateCell(18).SetCellValue(dlv_cod_time);
                    }
                    using (MemoryStream fileStream = new MemoryStream())
                    {
                        workbook.Write(fileStream);
                        TempData[handle] = fileStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                dataTableModel.msg = ex.Message;
            }
            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 5-2.物流代收匯款明細表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadCollectibleRemittanceDetails)]
        public ActionResult DownloadTransfer()
        {
            DownloadTransferViewModel vm = new DownloadTransferViewModel();
            vm.date = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 5-2.物流代收匯款明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadCollectibleRemittanceDetails)]
        public ActionResult TransferExcel(DownloadTransferViewModel vm)
        {
            string dataDate = Convert.ToDateTime(vm.date).ToString("yyyyMMdd");
            string handle = Guid.NewGuid().ToString();
            string fileName = $"{dataDate}物流代收匯款明細表.xlsx";
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                //取得資料
                dataTableModel = _downloadService.TransferReport(dataDate, UserContextService.GetUserId());
                if (dataTableModel.status == Status.success)
                {
                    DataTable dt = dataTableModel.dt;
                    IWorkbook workbook = new XSSFWorkbook();
                    ISheet sheet = workbook.CreateSheet("報表");
                    //表頭  
                    IRow row = sheet.CreateRow(0);
                    row.CreateCell(0).SetCellValue("項次");
                    row.CreateCell(1).SetCellValue("來源");
                    row.CreateCell(2).SetCellValue("日期");
                    row.CreateCell(3).SetCellValue("匯款日期");
                    row.CreateCell(4).SetCellValue("客戶");
                    row.CreateCell(5).SetCellValue("報單號碼");
                    row.CreateCell(6).SetCellValue("分號");
                    row.CreateCell(7).SetCellValue("運單號");
                    row.CreateCell(8).SetCellValue("姓名");
                    row.CreateCell(9).SetCellValue("稅金1");
                    row.CreateCell(10).SetCellValue("稅金2");
                    row.CreateCell(11).SetCellValue("報關費");
                    row.CreateCell(12).SetCellValue("到付款");
                    row.CreateCell(13).SetCellValue("代收稅金手續費");
                    row.CreateCell(14).SetCellValue("代收總金額");
                    row.CreateCell(15).SetCellValue("匯入金額");
                    row.CreateCell(16).SetCellValue("檢核碼");
                    row.CreateCell(17).SetCellValue("差額");
                    row.CreateCell(18).SetCellValue("代收貨款手續費");
                    row.CreateCell(19).SetCellValue("檢核時間");

                    sheet.SetColumnWidth(0, 3000);
                    sheet.SetColumnWidth(1, 3000);
                    sheet.SetColumnWidth(2, 3000);
                    sheet.SetColumnWidth(3, 3000);
                    sheet.SetColumnWidth(4, 6000);
                    sheet.SetColumnWidth(5, 6000);
                    sheet.SetColumnWidth(6, 6000);
                    sheet.SetColumnWidth(7, 6000);
                    sheet.SetColumnWidth(8, 6000);
                    sheet.SetColumnWidth(9, 6000);
                    sheet.SetColumnWidth(10, 6000);
                    sheet.SetColumnWidth(11, 6000);
                    sheet.SetColumnWidth(12, 6000);
                    sheet.SetColumnWidth(13, 6000);
                    sheet.SetColumnWidth(14, 6000);
                    sheet.SetColumnWidth(15, 6000);
                    sheet.SetColumnWidth(16, 6000);
                    sheet.SetColumnWidth(17, 6000);
                    sheet.SetColumnWidth(18, 6000);
                    sheet.SetColumnWidth(19, 6000);

                    string dlv_remit_time;
                    int tax1, tax2, ccfee, cod, fee, to_dlv_cod, dlv_remit_amout, dlv_remit_amout_fee, diff;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (dt.Rows[i]["DLV_REMIT_TIME"].ToString() != "")
                        {
                            dlv_remit_time = Convert.ToDateTime(dt.Rows[i]["DLV_REMIT_TIME"]).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            dlv_remit_time = "";
                        }
                        tax1 = 0;
                        tax2 = 0;
                        ccfee = 0;
                        cod = 0;
                        fee = 0;
                        to_dlv_cod = 0;
                        dlv_remit_amout = 0;
                        dlv_remit_amout_fee = 0;
                        int.TryParse(dt.Rows[i]["TAX1"].ToString(), out tax1);
                        int.TryParse(dt.Rows[i]["TAX2"].ToString(), out tax2);
                        int.TryParse(dt.Rows[i]["CCFEE"].ToString(), out ccfee);
                        int.TryParse(dt.Rows[i]["COD"].ToString(), out cod);
                        int.TryParse(dt.Rows[i]["FEE"].ToString(), out fee);
                        int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod);
                        int.TryParse(dt.Rows[i]["DLV_REMIT_AMOUT"].ToString(), out dlv_remit_amout);
                        int.TryParse(dt.Rows[i]["DLV_REMIT_AMOUT_FEE"].ToString(), out dlv_remit_amout_fee);

                        //差額
                        diff = to_dlv_cod - dlv_remit_amout;

                        row = sheet.CreateRow(i + 1);
                        row.CreateCell(0).SetCellValue(i + 1);
                        row.CreateCell(1).SetCellValue(dt.Rows[i]["SOURCE"].ToString());
                        row.CreateCell(2).SetCellValue(dt.Rows[i]["DATADATE"].ToString());
                        row.CreateCell(3).SetCellValue(dt.Rows[i]["DLV_REMIT_DATE"].ToString());
                        row.CreateCell(4).SetCellValue(dt.Rows[i]["CUSTOMER"].ToString());
                        row.CreateCell(5).SetCellValue(dt.Rows[i]["CLEARANCE_NUMBER"].ToString());
                        row.CreateCell(6).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                        row.CreateCell(7).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                        row.CreateCell(8).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                        row.CreateCell(9).SetCellValue(tax1);
                        row.CreateCell(10).SetCellValue(tax2);
                        row.CreateCell(11).SetCellValue(ccfee);
                        row.CreateCell(12).SetCellValue(cod);
                        row.CreateCell(13).SetCellValue(fee);
                        row.CreateCell(14).SetCellValue(to_dlv_cod);
                        row.CreateCell(15).SetCellValue(dlv_remit_amout);
                        row.CreateCell(16).SetCellValue(dt.Rows[i]["DLV_REMIT_CODE"].ToString());
                        row.CreateCell(17).SetCellValue(diff);
                        row.CreateCell(18).SetCellValue(dlv_remit_amout_fee);
                        row.CreateCell(19).SetCellValue(dlv_remit_time);
                    }
                    using (MemoryStream fileStream = new MemoryStream())
                    {
                        workbook.Write(fileStream);
                        TempData[handle] = fileStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                dataTableModel.msg = ex.Message;
            }
            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 5-3.物流代收未匯款明細表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadNotCollectibleRemittanceDetails)]
        public ActionResult DownloadNoTransfer()
        {
            DownloadNoTransferViewModel vm = new DownloadNoTransferViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 5-3.物流代收未匯款明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadNotCollectibleRemittanceDetails)]
        public ActionResult NoTransferExcel(DownloadReceiveViewModel vm)
        {
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");

            string handle = Guid.NewGuid().ToString();
            string fileName = $"{sDate}-{eDate}物流代收未匯款明細表.xlsx";
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                //取得資料
                dataTableModel = _downloadService.NoTransferReport(sDate, eDate, UserContextService.GetUserId());
                if (dataTableModel.status == Status.success)
                {
                    int tax1, tax2, ccfee, cod, fee, to_dlv_cod, dlv_cod;
                    DataTable dt = dataTableModel.dt;
                    IWorkbook workbook = new XSSFWorkbook();
                    ISheet sheet = workbook.CreateSheet("報表");
                    //表頭  
                    IRow row = sheet.CreateRow(0);
                    row.CreateCell(0).SetCellValue("項次");
                    row.CreateCell(1).SetCellValue("作業日");
                    row.CreateCell(2).SetCellValue("來源");
                    row.CreateCell(3).SetCellValue("報關類型");
                    row.CreateCell(4).SetCellValue("客戶名稱");
                    row.CreateCell(5).SetCellValue("是否包稅");
                    row.CreateCell(6).SetCellValue("查貨號碼");
                    row.CreateCell(7).SetCellValue("併單");
                    row.CreateCell(8).SetCellValue("稅金1");
                    row.CreateCell(9).SetCellValue("稅金2");
                    row.CreateCell(10).SetCellValue("報關費");
                    row.CreateCell(11).SetCellValue("到付款");
                    row.CreateCell(12).SetCellValue("手續費");
                    row.CreateCell(13).SetCellValue("代收貨款金額");
                    row.CreateCell(14).SetCellValue("派件公司");


                    sheet.SetColumnWidth(0, 3000);
                    sheet.SetColumnWidth(1, 6000);
                    sheet.SetColumnWidth(2, 6000);
                    sheet.SetColumnWidth(3, 6000);
                    sheet.SetColumnWidth(4, 3000);
                    sheet.SetColumnWidth(5, 6000);
                    sheet.SetColumnWidth(6, 3000);
                    sheet.SetColumnWidth(7, 3000);
                    sheet.SetColumnWidth(8, 3000);
                    sheet.SetColumnWidth(9, 3000);
                    sheet.SetColumnWidth(10, 3000);
                    sheet.SetColumnWidth(11, 6000);
                    sheet.SetColumnWidth(12, 6000);
                    sheet.SetColumnWidth(13, 6000);
                    sheet.SetColumnWidth(14, 6000);


                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        tax1 = 0;
                        tax2 = 0;
                        ccfee = 0;
                        cod = 0;
                        fee = 0;
                        to_dlv_cod = 0;

                        int.TryParse(dt.Rows[i]["TAX1"].ToString(), out tax1);
                        int.TryParse(dt.Rows[i]["TAX2"].ToString(), out tax2);
                        int.TryParse(dt.Rows[i]["CCFEE"].ToString(), out ccfee);
                        int.TryParse(dt.Rows[i]["COD"].ToString(), out cod);
                        int.TryParse(dt.Rows[i]["FEE"].ToString(), out fee);
                        int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod);

                        row = sheet.CreateRow(i + 1);
                        row.CreateCell(0).SetCellValue(i + 1);
                        row.CreateCell(1).SetCellValue(dt.Rows[i]["DATADATE"].ToString());
                        row.CreateCell(2).SetCellValue(dt.Rows[i]["SOURCE"].ToString());
                        row.CreateCell(3).SetCellValue(dt.Rows[i]["TYPE"].ToString());
                        row.CreateCell(4).SetCellValue(dt.Rows[i]["CUSTOMER"].ToString());
                        row.CreateCell(5).SetCellValue(dt.Rows[i]["INCLUDE_TAX"].ToString());
                        row.CreateCell(6).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                        row.CreateCell(7).SetCellValue(dt.Rows[i]["COMBINE"].ToString());
                        row.CreateCell(8).SetCellValue(tax1);
                        row.CreateCell(9).SetCellValue(tax2);
                        row.CreateCell(10).SetCellValue(ccfee);
                        row.CreateCell(11).SetCellValue(cod);
                        row.CreateCell(12).SetCellValue(fee);
                        row.CreateCell(13).SetCellValue(to_dlv_cod);
                        row.CreateCell(14).SetCellValue(dt.Rows[i]["TRANS_NAME"].ToString());
                    }
                    using (MemoryStream fileStream = new MemoryStream())
                    {
                        workbook.Write(fileStream);
                        TempData[handle] = fileStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                dataTableModel.msg = ex.Message;
            }
            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 下載Excel檔案
        /// </summary>
        /// <param name="fileGuid"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [LoginFilter]
        [HttpGet]
        public virtual ActionResult DownloadFile(string fileGuid, string fileName)
        {
            if (TempData[fileGuid] != null)
            {
                byte[] data = TempData[fileGuid] as byte[];
                return File(data, "application/octet-stream", fileName);
            }
            else
            {
                // Problem - Log the error, generate a blank file,
                //           redirect to another controller action - whatever fits with your application
                return new EmptyResult();
            }
        }

        /// <summary>
        /// 下載範例檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [LoginFilter]
        [HttpGet]
        public ActionResult DownloadExample(string filePath, string fileName)
        {
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/octet-stream", fileName);
        }
    }
}
