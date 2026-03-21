using JETFTAX.Models;
using JETFTAX.Models.CompanySeaTax;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.CompanySeaTax;
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
    public class CompanySeaTaxController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly CompanySeaTaxService _companySeaTaxService;

        public CompanySeaTaxController(DropDownListService dropDownListService, CompanySeaTaxService companySeaTaxService)
        {
            _dropDownListService = dropDownListService;
            _companySeaTaxService = companySeaTaxService;
        }

        // GET: CompanySeaTax
        public ActionResult Index()
        {
            var vm = new CompanySeaTaxViewModel()
            {
                DataDate = DateTime.Now.ToString("yyyy-MM-dd"),
                TaxTypeList = _dropDownListService.GetSeaTaxTypeList(),
                CompanyList = _dropDownListService.GetCompanyList()
            };

            return View(vm);
        }

        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaExcel(CompanySeaTaxViewModel vm)
        {
            string handle = Guid.NewGuid().ToString();
            string fileName = $"{vm.DataDate}{vm.Company}.xlsx";
            string msg = "";

            try
            {
                vm.DataDate = Convert.ToDateTime(vm.DataDate).ToString("yyyyMMdd");

                var workbook = _companySeaTaxService.GetWorkbook(vm.DataDate, vm.Company, vm.TaxType);

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