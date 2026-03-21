using JETFTAX.Models.AuthorityGroup;
using Newtonsoft.Json;
using Service.EnumTax;
using Service.Models;
using Service.Services.AuthorityGroup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class AuthorityGroupController : Controller
    {
        private readonly AuthorityGroupService _authorityGroupService;

        public AuthorityGroupController(AuthorityGroupService authorityGroupService) 
        {
            _authorityGroupService = authorityGroupService;
        }

        // GET: AuthorityGroup
        [UserAuthorize(Authority.AuthorityGroup)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有權限清單
        /// </summary>
        /// <returns>權限清單 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.AuthorityGroup)]
        public ActionResult GetAuthorities()
        {
            try
            {
                var authorities = _authorityGroupService.GetAuthorities();
                return Json(authorities, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得所有權限群組清單
        /// </summary>
        /// <returns>群組清單 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.AuthorityGroup)]
        public ActionResult GetGroups()
        {
            try
            {
                var groups = _authorityGroupService.GetGroups();
                return Json(groups, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得單一權限群組資料
        /// </summary>
        /// <param name="id">群組ID</param>
        /// <returns>群組資料 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.AuthorityGroup)]
        public ActionResult GetGroup(int id)
        {
            try
            {
                var group = _authorityGroupService.GetGroup(id);
                return Json(group, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增權限群組
        /// </summary>
        /// <param name="req">新增請求資料</param>
        /// <returns>處理結果 JSON</returns>
        [HttpPost]
        [UserAuthorize(Authority.AuthorityGroup)]
        public ActionResult Create(SaveGroupRequest req)
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

                var result = _authorityGroupService.Create(
                    req.GroupName, 
                    req.Memo, 
                    req.AuthorityIds ?? new List<string>());

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 修改權限群組
        /// </summary>
        /// <param name="req">修改請求資料</param>
        /// <returns>處理結果 JSON</returns>
        [HttpPost]
        [UserAuthorize(Authority.AuthorityGroup)]
        public ActionResult Update(SaveGroupRequest req)
        {
            try
            {
                if (req == null || !req.Id.HasValue)
                {
                    return Json(new ResopnseModel("請求資料不完整，缺少群組ID"), JsonRequestBehavior.AllowGet);
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

                var result = _authorityGroupService.Update(
                    req.Id.Value, 
                    req.GroupName, 
                    req.Memo, 
                    req.AuthorityIds ?? new List<string>());

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 刪除權限群組
        /// </summary>
        /// <param name="id">群組ID</param>
        /// <returns>處理結果 JSON</returns>
        [HttpPost]
        [UserAuthorize(Authority.AuthorityGroup)]
        public ActionResult Delete(int id)
        {
            try
            {
                var result = _authorityGroupService.Delete(id);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }
    }
}