using JETFTAX.Models.UserMaster;
using Service.EnumTax;
using Service.Models;
using Service.Services.UserMaster;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JETFTAX.Extensions;
using static JETFTAX.Controllers.AccountController;
using Service.Extensions;

namespace JETFTAX.Controllers
{
    public class UserMasterController : Controller
    {
        private readonly UserMasterService _userMasterService;

        public UserMasterController(UserMasterService userMasterService)
        {
            _userMasterService = userMasterService;
        }

        [UserAuthorize(Authority.UserMaster)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有會員清單
        /// </summary>
        /// <returns>會員清單 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.UserMaster)]
        public ActionResult GetUsers(string userId = null, int? authorityGroupId = null)
        {
            try
            {
                var users = _userMasterService.GetUsers(userId, authorityGroupId);
                return Json(users, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得單一會員資料
        /// </summary>
        /// <param name="userId">會員ID</param>
        /// <returns>會員資料 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.UserMaster)]
        public ActionResult GetUser(string userId)
        {
            try
            {
                var user = _userMasterService.GetUser(userId);
                if (user == null)
                {
                    return Json(new ResponseModel("會員不存在"), JsonRequestBehavior.AllowGet);
                }
                return Json(user, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得權限群組選項
        /// </summary>
        /// <returns>權限群組選項清單 JSON</returns>
        [HttpGet]
        [UserAuthorize(Authority.UserMaster)]
        public ActionResult GetAuthorityGroupOptions()
        {
            try
            {
                var options = _userMasterService.GetAuthorityGroupOptions();
                return Json(options, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增會員
        /// </summary>
        /// <param name="req">新增請求資料</param>
        /// <returns>處理結果 JSON</returns>
        [HttpPost]
        [UserAuthorize(Authority.UserMaster)]
        public ActionResult Create(SaveUserRequest req)
        {
            try
            {
                if (req == null)
                {
                    return Json(new ResponseModel("請求資料不能為空"), JsonRequestBehavior.AllowGet);
                }

                // 新增時密碼為必填
                if (string.IsNullOrWhiteSpace(req.Password))
                {
                    return Json(new ResponseModel("請輸入密碼"), JsonRequestBehavior.AllowGet);
                }

                // 使用 ModelState 驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .FirstOrDefault();
                    return Json(new ResponseModel(errors ?? "資料驗證失敗"), JsonRequestBehavior.AllowGet);
                }

                var result = _userMasterService.Create(
                    req.UserId,
                    req.UserName,
                    req.Password,
                    req.UserStatus,
                    req.AuthorityGroupIds);

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 修改會員
        /// </summary>
        /// <param name="req">修改請求資料</param>
        /// <returns>處理結果 JSON</returns>
        [HttpPost]
        [UserAuthorize(Authority.UserMaster)]
        public ActionResult Update(SaveUserRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.UserId))
                {
                    return Json(new ResponseModel("請求資料不完整，缺少會員ID"), JsonRequestBehavior.AllowGet);
                }

                // 使用 ModelState 驗證（密碼在修改時為選填）
                var tempPassword = req.Password; // 暫存密碼
                if (string.IsNullOrWhiteSpace(req.Password))
                {
                    req.Password = "temp"; // 臨時設置以通過驗證
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0 && x.Key != "Password") // 排除密碼驗證
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .FirstOrDefault();
                    
                    if (!string.IsNullOrEmpty(errors))
                    {
                        return Json(new ResponseModel(errors), JsonRequestBehavior.AllowGet);
                    }
                }

                req.Password = tempPassword; // 還原密碼

                var result = _userMasterService.Update(
                    req.UserId,
                    req.UserName,
                    req.UserStatus,
                    req.AuthorityGroupIds,
                    req.Password);

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }
    }
}