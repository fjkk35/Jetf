using JETFTAX.Models.CptTradeVan;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class CptTradeVanController : Controller
    {
        private readonly CptTradeVanService _cptTradeVanService;

        private readonly DropDownListService _dropDownListService;

        public CptTradeVanController(CptTradeVanService cptTradeVanService, DropDownListService dropDownListService)
        {
            _cptTradeVanService = cptTradeVanService;
            _dropDownListService = dropDownListService;
        }

        // GET: CptTradeVan
        public ActionResult Index()
        {
            CptTradeVanViewModel vm = new CptTradeVanViewModel()
            {
                ddlSourceList = _dropDownListService.GetCptTradeVanEnumList(),
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<JsonResult> Upload(HttpPostedFileBase file, CptTradeVanViewModel vm)
        {
            string fileType, fileName = "", filePath="", msg = "";
            string handle = Guid.NewGuid().ToString();
            try
            {
                var cptTradeVans = new List<CptTradeVanEnum>
                { 
                    CptTradeVanEnum.SeaMainNumber,
                    CptTradeVanEnum.SeaReceiveOrderWork,
                    CptTradeVanEnum.ErrorOrderWork,
                    CptTradeVanEnum.DeleteSeaMainNumber
                };

                if (!cptTradeVans.Contains(vm.source))
                {
                    if (file == null)
                    {
                        return new JsonResult()
                        {
                            Data = new { fileGuid = handle, fileName = fileName, msg = "未選擇檔案" }
                        };
                    }

                    fileType = Path.GetExtension(file.FileName);
                    if (fileType != ".xlsx")
                    {
                        return new JsonResult()
                        {
                            Data = new { fileGuid = handle, fileName = fileName, msg = "副檔名需為xlsx" }
                        };
                    }

                    fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                    filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                    file.SaveAs(filePath);
                }

                if (cptTradeVans.Contains(vm.source))
                {
                    if (string.IsNullOrEmpty(vm.Data))
                    {
                        return new JsonResult()
                        {
                            Data = new { status = Status.error, fileGuid = handle, fileName = fileName, msg = "請輸入查詢資料" }
                        };
                    }
                }

                //查詢資料
                ResponseModel resopnseModel = await _cptTradeVanService.UploadAsync(filePath, vm.source,vm.Data, Session["user_id"].ToString());
                msg = resopnseModel.msg;

                if (vm.source != CptTradeVanEnum.DeleteSeaMainNumber && resopnseModel.status == Status.success)
                {
                    IWorkbook workbook = resopnseModel.ReturnObject as IWorkbook;
                    fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}{vm.source.ToDescription()}.xlsx";
                    using (MemoryStream fileStream = new MemoryStream())
                    {
                        workbook.Write(fileStream);
                        TempData[handle] = fileStream.ToArray();
                    }
                }

                return new JsonResult()
                {
                    Data = new 
                    { 
                        status = resopnseModel.status, 
                        fileGuid = handle, 
                        fileName = fileName, 
                        msg = msg 
                    }
                };
            }
            catch (Exception ex)
            {
                return new JsonResult()
                {
                    Data = new { status = Status.error, msg = ex.Message }
                };
            }
        }
    }
}