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
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult DownloadSea()
        {
            DownloadSeaViewModel vm = new DownloadSeaViewModel();
            vm.ddlTaxTypeList = _dropDownListService.GetSeaTaxTypeList();
            vm.date = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

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
        [UserAuthorize(Authority.DownloadEtlTax)]
        public ActionResult DownloadEtl()
        {
            DownloadEtlViewModel vm = new DownloadEtlViewModel();
            List<SelectListItem> timeBetweenList = new List<SelectListItem>();
            timeBetweenList.Add(new SelectListItem() { Text = "前一天22:00-當日08:00", Value = "1" });
            timeBetweenList.Add(new SelectListItem() { Text = "當日08:00-當日16:00", Value = "2" });
            timeBetweenList.Add(new SelectListItem() { Text = "當日21:00-當日22:00", Value = "3" });
            vm.ddlTimeBetweenList = timeBetweenList;

            vm.date = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

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
                resopnseModel = _downloadService.UploadEtl(vm.date, vm.timeBetween, vm.sTime, vm.eTime, Session["user_id"].ToString());
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
                    dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "N", Session["user_id"].ToString());
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
            DataTableModel dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "", Session["user_id"].ToString());
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
                    dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "D", Session["user_id"].ToString());
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
                    dataTableModel = _downloadService.EtlReport(vm.date, vm.timeBetween, vm.sTime, vm.eTime, vm.company, "C", Session["user_id"].ToString());
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

        /// <summary>
        /// 3-3.空快稅金-回桃園倉庫明細表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadEtlWarehouse)]
        public ActionResult DownloadNoIncludeTax()
        {
            DownloadNoIncludeTaxViewModel vm = new DownloadNoIncludeTaxViewModel();
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "空快稅金-回桃園倉庫明細表", Value = "1" });
            vm.ddlSourceList = sourceList;
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 3-3.空快稅金-回桃園倉庫明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadEtlWarehouse)]
        public ActionResult NoIncludeTaxExcel(DownloadIncludeTaxViewModel vm)
        {
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            switch (vm.source)
            {
                case "1":
                    fileName = $"{sDate}~{eDate}-物流代收檔-空運-回桃園倉庫明細表-票.xlsx";
                    break;
            }
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                if (fileName != "")
                {
                    //取得資料
                    dataTableModel = _downloadService.NoIncludeTaxReport(vm.source, sDate, eDate, Session["user_id"].ToString());
                    if (dataTableModel.status == Status.success)
                    {
                        DataTable dt = dataTableModel.dt;

                        IWorkbook workbook = GetNoIncludeTaxWorkbook(dt);

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
        /// 3-3.空快稅金-回桃園倉庫明細表-Excel-Workbook
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetNoIncludeTaxWorkbook(DataTable dt)
        {
            string customer;
            DataRow[] dr;
            IWorkbook workbook = new XSSFWorkbook();
            if (dt.Rows.Count > 0)
            {
                //用客戶區分 sheet
                var dt_Group = from t in dt.AsEnumerable()
                               group t by new { customer = t.Field<string>("CUSTOMER") } into g
                               select new
                               {
                                   customer = g.Key.customer
                               };

                foreach (var item in dt_Group)
                {
                    customer = item.customer;
                    dr = dt.Select($"CUSTOMER='{customer}'", "DATADATE ");
                    //取得頁籤
                    GetNoIncludeTaxSheet(workbook, customer, dr);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 3-3.空快稅金-回桃園倉庫明細表-Excel-頁籤-客戶明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <param name="dr"></param>
        void GetNoIncludeTaxSheet(IWorkbook workbook, string sheetName, DataRow[] dr)
        {
            DateTime sign_in_time, sign_out_time;
            XSSFCellStyle dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            XSSFDataFormat format = (XSSFDataFormat)workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm:ss");

            int tax1, tax2, ccfee, cod, fee, to_dlv_cod;
            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("作業日");
            row.CreateCell(2).SetCellValue("來源");
            row.CreateCell(3).SetCellValue("報關類型");
            row.CreateCell(4).SetCellValue("客戶名稱");
            row.CreateCell(5).SetCellValue("是否包稅");
            row.CreateCell(6).SetCellValue("清關袋號");
            row.CreateCell(7).SetCellValue("分提單號");
            row.CreateCell(8).SetCellValue("入倉時間");
            row.CreateCell(9).SetCellValue("出倉時間");
            row.CreateCell(10).SetCellValue("姓名");
            row.CreateCell(11).SetCellValue("電話");
            row.CreateCell(12).SetCellValue("併單");
            row.CreateCell(13).SetCellValue("稅金1");
            row.CreateCell(14).SetCellValue("稅金2");
            row.CreateCell(15).SetCellValue("報關費");
            row.CreateCell(16).SetCellValue("到付款");
            row.CreateCell(17).SetCellValue("手續費");
            row.CreateCell(18).SetCellValue("代收貨款金額");
            row.CreateCell(19).SetCellValue("派件公司");
            row.CreateCell(20).SetCellValue("物流單號");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 3000);
            sheet.SetColumnWidth(2, 3000);
            sheet.SetColumnWidth(3, 3000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 3000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 6000);
            sheet.SetColumnWidth(10, 6000);
            sheet.SetColumnWidth(11, 6000);
            sheet.SetColumnWidth(12, 3000);
            sheet.SetColumnWidth(13, 3000);
            sheet.SetColumnWidth(14, 3000);
            sheet.SetColumnWidth(15, 3000);
            sheet.SetColumnWidth(16, 3000);
            sheet.SetColumnWidth(17, 3000);
            sheet.SetColumnWidth(18, 6000);
            sheet.SetColumnWidth(19, 6000);
            sheet.SetColumnWidth(20, 6000);

            for (int i = 0; i < dr.Length; i++)
            {
                tax1 = 0;
                tax2 = 0;
                ccfee = 0;
                cod = 0;
                fee = 0;
                to_dlv_cod = 0;

                int.TryParse(dr[i]["TAX1"].ToString(), out tax1);
                int.TryParse(dr[i]["TAX2"].ToString(), out tax2);
                int.TryParse(dr[i]["CCFEE"].ToString(), out ccfee);
                int.TryParse(dr[i]["COD"].ToString(), out cod);
                int.TryParse(dr[i]["FEE"].ToString(), out fee);
                int.TryParse(dr[i]["TO_DLV_COD"].ToString(), out to_dlv_cod);
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["SOURCE"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["TYPE"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["CUSTOMER"].ToString());
                row.CreateCell(5).SetCellValue(dr[i]["INCLUDE_TAX"].ToString());
                row.CreateCell(6).SetCellValue(dr[i]["BAG_NUMBER"].ToString());
                row.CreateCell(7).SetCellValue(dr[i]["TRACKINGNO"].ToString());
                if (DateTime.TryParse(dr[i]["IN_DATETIME"].ToString(), out sign_in_time))
                {
                    row.CreateCell(8).SetCellValue(sign_in_time);
                    row.GetCell(8).CellStyle = dateStyle;
                }
                if (DateTime.TryParse(dr[i]["OUT_DATETIME"].ToString(), out sign_out_time))
                {
                    row.CreateCell(9).SetCellValue(sign_out_time);
                    row.GetCell(9).CellStyle = dateStyle;
                }

                row.CreateCell(10).SetCellValue(dr[i]["RECIPIENT"].ToString());
                row.CreateCell(11).SetCellValue(dr[i]["RECPHONE"].ToString());
                row.CreateCell(12).SetCellValue(dr[i]["COMBINE"].ToString());
                row.CreateCell(13).SetCellValue(tax1);
                row.CreateCell(14).SetCellValue(tax2);
                row.CreateCell(15).SetCellValue(ccfee);
                row.CreateCell(16).SetCellValue(cod);
                row.CreateCell(17).SetCellValue(fee);
                row.CreateCell(18).SetCellValue(to_dlv_cod);
                row.CreateCell(19).SetCellValue(dr[i]["TRANS_NAME"].ToString());
                row.CreateCell(20).SetCellValue(dr[i]["DELIVERYNO"].ToString());
            }
        }

        /// <summary>
        /// 3-4.稅金總表及明細表
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadTaxReport)]
        public ActionResult DownloadIncludeTax()
        {
            DownloadIncludeTaxViewModel vm = new DownloadIncludeTaxViewModel();
            List<SelectListItem> sourceList = new List<SelectListItem>();
            sourceList.Add(new SelectListItem() { Text = "海運", Value = "1" });
            //sourceList.Add(new SelectListItem() { Text = "海運不包稅(不含新瑞或新竹)", Value = "2" });
            sourceList.Add(new SelectListItem() { Text = "空運", Value = "3" });
            //sourceList.Add(new SelectListItem() { Text = "空運不包稅(不含新瑞或新竹)", Value = "4" });
            vm.ddlSourceList = sourceList;
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        /// <summary>
        /// 3-4.稅金總表及明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.DownloadTaxReport)]
        public ActionResult IncludeTaxExcel(DownloadIncludeTaxViewModel vm)
        {
            string sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            string eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            string handle = Guid.NewGuid().ToString();
            string fileName = "";
            switch (vm.source)
            {
                case "1":
                    fileName = $"{sDate}~{eDate}-稅金總表及明細表-海運.xlsx";
                    break;
                //case "2":
                //    fileName = $"{sDate}~{eDate}-物流代收檔-海運-不包稅(非新瑞或新竹)-票.xlsx";
                //    break;
                case "3":
                    fileName = $"{sDate}~{eDate}-稅金總表及明細表-空運.xlsx";
                    break;
                    //case "4":
                    //    fileName = $"{sDate}~{eDate}-物流代收檔-空運-不包稅(非新瑞或新竹)-票.xlsx";
                    //    break;
            }
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                if (fileName != "")
                {
                    //取得資料
                    dataTableModel = _downloadService.IncludeTaxReport(vm.source, sDate, eDate, Session["user_id"].ToString());
                    if (dataTableModel.status == Status.success)
                    {
                        DataTable dt = dataTableModel.dt;

                        IWorkbook workbook = GetIncludeTaxWorkbook(dt);

                        //fileName = fileName.Replace("票", $"{dt.Rows.Count}票");
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
        /// 3-4.稅金總表及明細表-Excel-Workbook
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        IWorkbook GetIncludeTaxWorkbook(DataTable dt)
        {
            string customer, source;
            DataRow[] dr;
            IWorkbook workbook = new XSSFWorkbook();
            if (dt.Rows.Count > 0)
            {
                //用客戶區分 sheet
                var dt_Customer = from t in dt.AsEnumerable()
                                  group t by new { customer = t.Field<string>("CUST_NAME") } into g
                                  select new
                                  {
                                      customer = g.Key.customer
                                  };
                //用倉庫區分 sheet
                var dt_Source = from t in dt.AsEnumerable()
                                group t by new { source = t.Field<string>("SOURCE") } into g
                                select new
                                {
                                    source = g.Key.source
                                };

                //稅金總表
                DataTable dt_SourceGroup = dt.AsEnumerable()
               .GroupBy(r => new { SOURCE = r["SOURCE"], DATADATE = r["DATADATE"] })
               .Select(g =>
               {
                   var row = dt.NewRow();
                   row["SOURCE"] = g.Key.SOURCE;
                   row["DATADATE"] = g.Key.DATADATE;
                   row["TAX1"] = g.Sum(r => (Int64)r["TAX1"]);
                   row["TAX2"] = g.Sum(r => (Int64)r["TAX2"]).ToString();
                   row["COD"] = g.Sum(r => (Int64)r["COD"]).ToString();
                   return row;
               }).CopyToDataTable();

                //排序
                DataView dv_SourceGroup = dt_SourceGroup.DefaultView;
                dv_SourceGroup.Sort = "DATADATE,SOURCE";
                dt_SourceGroup = dv_SourceGroup.ToTable();

                //稅金總表
                GetIncludeTaxSheetSourceReport(workbook, "稅金總表", dt_SourceGroup);

                //客戶總表
                DataTable dt_CustomerGroup = dt.AsEnumerable()
               .GroupBy(r => new { SOURCE = r["SOURCE"], CUST_NAME = r["CUST_NAME"], DATADATE = r["DATADATE"] })
               .Select(g =>
               {
                   var row = dt.NewRow();
                   row["SOURCE"] = g.Key.SOURCE;
                   row["CUST_NAME"] = g.Key.CUST_NAME;
                   row["DATADATE"] = g.Key.DATADATE;
                   row["TAX1"] = g.Sum(r => r.Field<Int64>("TAX1"));
                   row["TAX2"] = g.Sum(r => r.Field<Int64>("TAX2"));
                   row["COD"] = g.Sum(r => r.Field<Int64>("COD"));
                   return row;
               }).CopyToDataTable();

                //客戶總表
                foreach (var item in dt_Customer)
                {
                    customer = item.customer;
                    if (customer == null)
                    {
                        customer = "無客戶";
                        dr = dt_CustomerGroup.Select($"CUST_NAME is null", "DATADATE,SOURCE ");
                    }
                    else
                    {
                        dr = dt_CustomerGroup.Select($"CUST_NAME='{customer}'", "DATADATE,SOURCE ");
                    }

                    //取得頁籤
                    GetIncludeTaxSheetCustomerReport(workbook, $"{customer}總表", dr);
                }

                //倉庫明細
                foreach (var item in dt_Source)
                {
                    source = item.source;
                    if (source == null)
                    {
                        source = "無倉庫";
                        dr = dt.Select($"SOURCE is null", "DATADATE ");
                    }
                    else
                    {
                        dr = dt.Select($"SOURCE='{source}'", "DATADATE ");
                    }

                    //取得頁籤
                    GetIncludeTaxSheetDetail(workbook, $"{source}明細", dr);
                }

                //客戶明細
                foreach (var item in dt_Customer)
                {
                    customer = item.customer;
                    if (customer == null)
                    {
                        customer = "無客戶";
                        dr = dt.Select($"CUST_NAME is null", "DATADATE ");
                    }
                    else
                    {
                        dr = dt.Select($"CUST_NAME='{customer}'", "DATADATE ");
                    }

                    //取得頁籤
                    GetIncludeTaxSheetDetail(workbook, $"{customer}明細", dr);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 3-4.稅金總表及明細表-Excel-Workbook-頁籤-稅金總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <param name="dr"></param>
        void GetIncludeTaxSheetSourceReport(IWorkbook workbook, string sheetName, DataTable dt)
        {
            Int64 tax1, tax2, cod, totalTax, allTax1 = 0, allTax2 = 0, allCod = 0, allTotalTax = 0;

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("日期");
            row.CreateCell(2).SetCellValue("資料來源");
            row.CreateCell(3).SetCellValue("稅金1");
            row.CreateCell(4).SetCellValue("稅金2");
            row.CreateCell(5).SetCellValue("稅金合計");
            row.CreateCell(6).SetCellValue("到付款");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                tax1 = 0;
                tax2 = 0;
                cod = 0;
                Int64.TryParse(dt.Rows[i]["TAX1"].ToString(), out tax1);
                Int64.TryParse(dt.Rows[i]["TAX2"].ToString(), out tax2);
                Int64.TryParse(dt.Rows[i]["COD"].ToString(), out cod);

                totalTax = tax1 + tax2;

                //合計
                allTax1 += tax1;
                allTax2 += tax2;
                allCod += cod;
                allTotalTax += totalTax;

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["DATADATE"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["SOURCE"].ToString());
                row.CreateCell(3).SetCellValue(tax1);
                row.CreateCell(4).SetCellValue(tax2);
                row.CreateCell(5).SetCellValue(totalTax);
                row.CreateCell(6).SetCellValue(cod);
            }

            //全部合計
            row = sheet.CreateRow(dt.Rows.Count + 1);
            row.CreateCell(2).SetCellValue("合計");
            row.CreateCell(3).SetCellValue(allTax1);
            row.CreateCell(4).SetCellValue(allTax2);
            row.CreateCell(5).SetCellValue(allTotalTax);
            row.CreateCell(6).SetCellValue(allCod);
        }

        /// <summary>
        /// 3-4.稅金總表及明細表-Excel-Workbook-頁籤-客戶總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <param name="dr"></param>
        void GetIncludeTaxSheetCustomerReport(IWorkbook workbook, string sheetName, DataRow[] dr)
        {
            Int64 tax1, tax2, cod, totalTax;

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("日期");
            row.CreateCell(2).SetCellValue("資料來源");
            row.CreateCell(3).SetCellValue("客戶");
            row.CreateCell(4).SetCellValue("稅金1");
            row.CreateCell(5).SetCellValue("稅金2");
            row.CreateCell(6).SetCellValue("稅金合計");
            row.CreateCell(7).SetCellValue("到付款");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);

            for (int i = 0; i < dr.Length; i++)
            {
                tax1 = 0;
                tax2 = 0;
                cod = 0;
                Int64.TryParse(dr[i]["TAX1"].ToString(), out tax1);
                Int64.TryParse(dr[i]["TAX2"].ToString(), out tax2);
                Int64.TryParse(dr[i]["COD"].ToString(), out cod);

                totalTax = tax1 + tax2;

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["SOURCE"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["CUST_NAME"].ToString());
                row.CreateCell(4).SetCellValue(tax1);
                row.CreateCell(5).SetCellValue(tax2);
                row.CreateCell(6).SetCellValue(totalTax);
                row.CreateCell(7).SetCellValue(cod);
            }

        }

        /// <summary>
        ///  3-4.稅金總表及明細表-Excel-Workbook-頁籤-客戶明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <param name="dr"></param>
        void GetIncludeTaxSheetDetail(IWorkbook workbook, string sheetName, DataRow[] dr)
        {
            string in_datetime, out_datetime;
            int tax_base, tax1, tax2, cod, fee, totalTax;

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("日期");
            row.CreateCell(2).SetCellValue("資料來源");
            row.CreateCell(3).SetCellValue("報關類別");
            row.CreateCell(4).SetCellValue("客戶");
            row.CreateCell(5).SetCellValue("清關袋號");
            row.CreateCell(6).SetCellValue("分提單號");
            row.CreateCell(7).SetCellValue("主號");
            row.CreateCell(8).SetCellValue("報單號碼");
            row.CreateCell(9).SetCellValue("稅單號碼");
            row.CreateCell(10).SetCellValue("進倉時間");
            row.CreateCell(11).SetCellValue("出倉時間");
            row.CreateCell(12).SetCellValue("稅基");
            row.CreateCell(13).SetCellValue("稅金1");
            row.CreateCell(14).SetCellValue("稅金2");
            row.CreateCell(15).SetCellValue("稅金合計");
            row.CreateCell(16).SetCellValue("跟派件收");
            row.CreateCell(17).SetCellValue("跟廠商收");
            row.CreateCell(18).SetCellValue("納稅義務人");
            row.CreateCell(19).SetCellValue("電話");
            row.CreateCell(20).SetCellValue("派件公司");
            row.CreateCell(21).SetCellValue("到付款");
            row.CreateCell(22).SetCellValue("CUST_ID");
            row.CreateCell(23).SetCellValue("TRANS_NO");
            row.CreateCell(24).SetCellValue("是否包稅");
            row.CreateCell(25).SetCellValue("手續費");
            row.CreateCell(26).SetCellValue("物流貨號");
            row.CreateCell(27).SetCellValue("制單納稅義務人");
            row.CreateCell(28).SetCellValue("制單統一編號");
            row.CreateCell(29).SetCellValue("菜鳥LP單號");
            row.CreateCell(30).SetCellValue("納稅義務人身分證號");

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
            sheet.SetColumnWidth(19, 6000);
            sheet.SetColumnWidth(20, 6000);
            sheet.SetColumnWidth(21, 6000);
            sheet.SetColumnWidth(22, 6000);
            sheet.SetColumnWidth(23, 6000);
            sheet.SetColumnWidth(24, 3000);
            sheet.SetColumnWidth(25, 3000);
            sheet.SetColumnWidth(26, 6000);
            sheet.SetColumnWidth(27, 6000);
            sheet.SetColumnWidth(28, 6000);
            sheet.SetColumnWidth(29, 6000);
            sheet.SetColumnWidth(30, 6000);

            for (int i = 0; i < dr.Length; i++)
            {
                if (dr[i]["IN_DATETIME"].ToString() != "")
                {
                    in_datetime = Convert.ToDateTime(dr[i]["IN_DATETIME"]).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    in_datetime = "";
                }

                if (dr[i]["OUT_DATETIME"].ToString() != "")
                {
                    out_datetime = Convert.ToDateTime(dr[i]["OUT_DATETIME"]).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    out_datetime = "";
                }
                tax_base = 0;
                tax1 = 0;
                tax2 = 0;
                cod = 0;
                fee = 0;
                int.TryParse(dr[i]["TAX_BASE"].ToString(), out tax_base);
                int.TryParse(dr[i]["TAX1"].ToString(), out tax1);
                int.TryParse(dr[i]["TAX2"].ToString(), out tax2);
                int.TryParse(dr[i]["COD"].ToString(), out cod);
                int.TryParse(dr[i]["FEE"].ToString(), out fee);

                int.TryParse(dr[i]["CUSTOMER_COD"].ToString(), out var customerCod);
                int.TryParse(dr[i]["TRANS_COD"].ToString(), out var transCod);

                totalTax = tax1 + tax2;

                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dr[i]["DATADATE"].ToString());
                row.CreateCell(2).SetCellValue(dr[i]["SOURCE"].ToString());
                row.CreateCell(3).SetCellValue(dr[i]["TYPE"].ToString());
                row.CreateCell(4).SetCellValue(dr[i]["CUST_NAME"].ToString());
                row.CreateCell(5).SetCellValue(dr[i]["BAG_NUMBER"].ToString());
                row.CreateCell(6).SetCellValue(dr[i]["TRACKINGNO"].ToString());
                row.CreateCell(7).SetCellValue(dr[i]["MAIN_NUMBER"].ToString());
                row.CreateCell(8).SetCellValue(dr[i]["CLEARANCE_NUMBER"].ToString());
                row.CreateCell(9).SetCellValue(dr[i]["TAX_NUMBER"].ToString());
                row.CreateCell(10).SetCellValue(in_datetime);
                row.CreateCell(11).SetCellValue(out_datetime);
                row.CreateCell(12).SetCellValue(tax_base);
                row.CreateCell(13).SetCellValue(tax1);
                row.CreateCell(14).SetCellValue(tax2);
                row.CreateCell(15).SetCellValue(totalTax);
                row.CreateCell(16).SetCellValue(transCod);
                row.CreateCell(17).SetCellValue(customerCod);
                row.CreateCell(18).SetCellValue(dr[i]["RECIPIENT"].ToString());
                row.CreateCell(19).SetCellValue(dr[i]["RECPHONE"].ToString());
                row.CreateCell(20).SetCellValue(dr[i]["TRANS_NAME"].ToString());
                row.CreateCell(21).SetCellValue(cod);
                row.CreateCell(22).SetCellValue(dr[i]["CUST_ID"].ToString());
                row.CreateCell(23).SetCellValue(dr[i]["TRANS_NO"].ToString());
                row.CreateCell(24).SetCellValue(dr[i]["INCLUDE_TAX"].ToString());
                row.CreateCell(25).SetCellValue(fee);
                row.CreateCell(26).SetCellValue(dr[i]["DLV_INV"].ToString());
                row.CreateCell(27).SetCellValue(dr[i]["IMPORTER"].ToString());
                row.CreateCell(28).SetCellValue(dr[i]["IMPORTER_ID"].ToString());
                row.CreateCell(29).SetCellValue(dr[i]["ARRIVAL"].ToString());
                row.CreateCell(30).SetCellValue(dr[i]["TAX_RECID"].ToString());
            }
        }

        /// <summary>
        /// 3-5.G類稅金調整明細表
        /// </summary>
        /// <returns></returns>
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
                dataTableModel = _downloadService.ReceiveReport(sDate, eDate, Session["user_id"].ToString());
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
                dataTableModel = _downloadService.TransferReport(dataDate, Session["user_id"].ToString());
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
                dataTableModel = _downloadService.NoTransferReport(sDate, eDate, Session["user_id"].ToString());
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