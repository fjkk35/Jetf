using JETFTAX.Models.EtlClearanceDetails;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.EtlClearanceDetails;
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
    public class EtlClearanceDetailsController : Controller
    {
        private readonly EtlClearanceDetailsService _etlClearanceDetailsService;

        public EtlClearanceDetailsController(EtlClearanceDetailsService etlClearanceDetailsService) 
        {
            _etlClearanceDetailsService = etlClearanceDetailsService;
        }

        GlobalService globalService = new GlobalService();

        /// <summary>
        /// 空快清關明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.EtlClearanceDetails)]
        public ActionResult Index()
        {
            EtlClearanceDetailsViewModel vm = new EtlClearanceDetailsViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd") + " 00:00";
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd") + " 23:59";
            vm.dataTime = "CrtDateTime";
            return View(vm);
        }

        /// <summary>
        /// 空快清關明細表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "4", "6")]
        [UserAuthorize(Authority.EtlClearanceDetails)]
        public ActionResult EtlClearanceDetailsExcel(EtlClearanceDetailsViewModel vm)
        {
            string sDate = vm.sDate;
            string eDate = vm.eDate;
            string dataTime = vm.dataTime;
            string fileName = $"{sDate}～{eDate}-空快清關明細表.zip";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                var bytes = _etlClearanceDetailsService.GetExcels(sDate, eDate, dataTime);
                TempData[handle] = bytes;

                //紀錄LOG
                _etlClearanceDetailsService.InsertLog_ClearanceWork(new LogClearanceWork()
                {
                    WorkName = "空快清關明細表",
                    DownloadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Ip = globalService.GetIPAddress(),
                    UserId = Session["user_id"].ToString()
                });
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
    }
}