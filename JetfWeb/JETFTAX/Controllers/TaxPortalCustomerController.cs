using Service.EnumTax;
using Service.Models;
using Service.Services.TaxPortalCustomerService;
using Service.Services.TaxPortalCustomerService.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class TaxPortalCustomerController : Controller
    {
        private readonly TaxPortalCustomerService _taxPortalCustomerService;

        public TaxPortalCustomerController(TaxPortalCustomerService taxPortalCustomerService)
        {
            _taxPortalCustomerService = taxPortalCustomerService;
        }

        [UserAuthorize(Authority.TaxPortalCustomer)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [UserAuthorize(Authority.TaxPortalCustomer)]
        public JsonResult GetCustomerGroups()
        {
            try
            {
                var result = _taxPortalCustomerService.GetCustomerGroups();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [UserAuthorize(Authority.TaxPortalCustomer)]
        public JsonResult QueryUsers(TaxPortalUserQueryRequest request)
        {
            try
            {
                var result = _taxPortalCustomerService.QueryUsers(request);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [UserAuthorize(Authority.TaxPortalCustomer)]
        public JsonResult GetUserDetail(int id)
        {
            try
            {
                var result = _taxPortalCustomerService.GetUserDetail(id);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.TaxPortalCustomer)]
        public JsonResult GeneratePassword()
        {
            try
            {
                var result = _taxPortalCustomerService.GeneratePassword();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.TaxPortalCustomer)]
        public JsonResult CreateUser(TaxPortalUserCreateRequest request)
        {
            try
            {
                string createOpe = Session["user_id"]?.ToString() ?? "system";
                var result = _taxPortalCustomerService.CreateUser(request, createOpe);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.TaxPortalCustomer)]
        public JsonResult UpdateUser(TaxPortalUserUpdateRequest request)
        {
            try
            {
                string createOpe = Session["user_id"]?.ToString() ?? "system";
                var result = _taxPortalCustomerService.UpdateUser(request, createOpe);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}