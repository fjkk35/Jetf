using JETFTAX.Models.EtlErrorG;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Services.EtlErrorG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class EtlErrorGController : Controller
    {

        private readonly EtlErrorGService _etlErrorGService;

        public EtlErrorGController(EtlErrorGService etlErrorGService) 
        {
            _etlErrorGService = etlErrorGService;
        }

        // GET: EtlErrorG
        public ActionResult Index()
        {
            EtlErrorGViewModel vm = new EtlErrorGViewModel()
            {
                SDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 00:00:00",
                EDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 23:59:59",
            };
            return View(vm);
        }

        /// <summary>
        /// 空快B6F錯單G報表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.EtlErrorG)]
        public ActionResult EtlErrorGExcel(EtlErrorGViewModel vm)
        {
            string sDate = vm.SDate;
            string eDate = vm.EDate;
            string fileName = $"{sDate}~{eDate}-空快B6F錯單G報表.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = _etlErrorGService.GetEtlErrorGWorkbook(sDate, eDate,vm.IsSearch);
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

    }
}