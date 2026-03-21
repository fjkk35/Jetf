using Service.EnumTax;
using Service.Models;
using Service.Services.Authority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JETFTAX.Models.Authority;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class AuthorityController : Controller
    {
        private readonly AuthorityService _authorityService;

        public AuthorityController(AuthorityService authorityService)
        {
            _authorityService = authorityService;
        }

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有權限清單
        /// </summary>
        /// <returns>權限清單 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.Authority)]
        public ActionResult GetAuthorities()
        {
            try
            {
                var authorities = _authorityService.GetAuthorities();
                return Json(authorities, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得單一權限資料
        /// </summary>
        /// <param name="id">權限ID</param>
        /// <returns>權限資料 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.Authority)]
        public ActionResult GetAuthority(string id)
        {
            try
            {
                var authority = _authorityService.GetAuthority(id);
                if (authority == null)
                {
                    return Json(new ResopnseModel("權限不存在"), JsonRequestBehavior.AllowGet);
                }
                return Json(authority, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得權限分類選項
        /// </summary>
        /// <returns>權限分類清單 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.Authority)]
        public ActionResult GetPartnerOptions()
        {
            try
            {
                var options = _authorityService.GetPartnerOptions();
                return Json(options, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增權限
        /// </summary>
        /// <param name="req">新增請求資料</param>
        /// <returns>處理結果 JSON</returns>
        [HttpPost]
        [UserAuthorize(Authority.Authority)]
        public ActionResult Create(SaveAuthorityRequest req)
        {
            try
            {
                if (req == null)
                {
                    return Json(new ResopnseModel("請求資料不能為空"), JsonRequestBehavior.AllowGet);
                }

                // 使用 ModelState 驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .FirstOrDefault();
                    return Json(new ResopnseModel(errors ?? "資料驗證失敗"), JsonRequestBehavior.AllowGet);
                }

                var result = _authorityService.Create(req.Id, req.Text, req.PartnerId, req.Sort);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 修改權限
        /// </summary>
        /// <param name="req">修改請求資料</param>
        /// <returns>處理結果 JSON</returns>
        [HttpPost]
        [UserAuthorize(Authority.Authority)]
        public ActionResult Update(SaveAuthorityRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Id))
                {
                    return Json(new ResopnseModel("請求資料不完整，缺少權限ID"), JsonRequestBehavior.AllowGet);
                }

                // 使用 ModelState 驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .FirstOrDefault();
                    return Json(new ResopnseModel(errors ?? "資料驗證失敗"), JsonRequestBehavior.AllowGet);
                }

                var result = _authorityService.Update(req.Id, req.Text, req.PartnerId, req.Sort);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }
    }
}