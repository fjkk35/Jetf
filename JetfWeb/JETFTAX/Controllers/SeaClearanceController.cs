using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.SeaClearance;
using Service.Services.SeaClearance.Domain;
using Service.Services.SeaClearanceDetailEditHistory;
using Service.Services.Step;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaClearanceController : Controller
    {
        private readonly SeaClearanceService _seaClearanceService;
        private readonly SeaClearanceDetailEditHistoryService _editHistoryService;
        private readonly StepService _stepService;

        public SeaClearanceController(SeaClearanceService seaClearanceService, SeaClearanceDetailEditHistoryService editHistoryService, StepService stepService)
        {
            _seaClearanceService = seaClearanceService;
            _editHistoryService = editHistoryService;
            _stepService = stepService;
        }

        // GET: SeaClearance
        [UserAuthorize(Authority.SeaClearance)]
        public ActionResult Index()
        {
            return View();
        }

        [UserAuthorize(Authority.SeaClearance)]
        public ActionResult Detail()
        {
            return View();
        }

        /// <summary>
        /// 海運客戶
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.SeaClearance)]
        public ActionResult GetSeaCustomerList()
        {
            var result = _seaClearanceService.GetSeaCustomerList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// DataTable 服務端分頁查詢
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult SearchData(SeaClearanceRequest searchRequest)
        {
            try
            {
                var result = _seaClearanceService.GetData(searchRequest);

                // 回傳 DataTable 格式的資料
                return Json(new
                {
                    Data = result.Data,
                    TotalCount = result.TotalCount
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult GetDetail(int id)
        {
            try
            {
                var result = _seaClearanceService.GetDetail(id);
                return Json(new ResopnseModel
                {
                    ReturnObject = result
                });
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel
                {
                    msg = ex.Message
                });
            }
        }

        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult GetCustomsBrokerageOptions()
        {
            try
            {
                var result = _seaClearanceService.GetCustomsBrokerageOptions();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得關貿資料
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="mainNumber"></param>
        /// <param name="trackingNo"></param>
        /// <param name="postEntry"></param>
        /// <returns></returns>
        public JsonResult GetCptData(GetCptDataRequest request)
        {
            try
            {
                var result = _seaClearanceService.GetCptData(request);
                return Json(new ResopnseModel
                {
                    ReturnObject = result
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel
                {
                    msg = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 更新欄位值
        /// </summary>
        /// <param name="id"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult UpdateField(int id, SeaClearanceEditField field, string newValue)
        {
            try
            {
                var result = _seaClearanceService.UpdateField(id, field, newValue);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得編輯紀錄
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult GetEditHistory(int seaClearanceDetailId)
        {
            try
            {
                var result = _editHistoryService.GetEditHistory(seaClearanceDetailId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 下載
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SeaClearance)]
        public ActionResult Excel(SeaClearanceRequest request)
        {
            var workbook = _seaClearanceService.SeaClearanceExcel(request);

            string handle = Guid.NewGuid().ToString();
            string fileName = $"海快後段報關_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = "" }
            };
        }

        /// <summary>
        /// 取得指定明細的簽審類別
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public JsonResult GetDetailApprovalCategories(int seaClearanceDetailId)
        {
            try
            {
                var result = _seaClearanceService.GetDetailApprovalCategories(seaClearanceDetailId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新明細的簽審類別
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="categoryIds"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult UpdateDetailApprovalCategories(int seaClearanceDetailId, int[] categoryIds)
        {
            try
            {
                var userId = Session["user_id"]?.ToString() ?? "system";
                var categoryList = categoryIds?.ToList() ?? new List<int>();
                var result = _seaClearanceService.UpdateDetailApprovalCategories(seaClearanceDetailId, categoryList, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得指定明細的授權表單
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="type">1=收到正本選單、2=寄文件選單</param>
        /// <returns></returns>
        public JsonResult GetDetailAuthorizationForms(int seaClearanceDetailId, int type)
        {
            try
            {
                var result = _seaClearanceService.GetDetailAuthorizationForms(seaClearanceDetailId, type);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新明細的授權表單
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="type">1=收到正本選單、2=寄文件選單</param>
        /// <param name="formIds"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult UpdateDetailAuthorizationForms(int seaClearanceDetailId, int type, int[] formIds)
        {
            try
            {
                var formList = formIds?.ToList() ?? new List<int>();
                var result = _seaClearanceService.UpdateDetailAuthorizationForms(seaClearanceDetailId, type, formList);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得授權表單歷史記錄
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="type">1=收到正本選單、2=寄文件選單，null=全部</param>
        /// <returns></returns>
        public JsonResult GetAuthorizationFormHistory(int seaClearanceDetailId, int type)
        {
            try
            {
                var result = _seaClearanceService.GetAuthorizationFormHistory(seaClearanceDetailId, type);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        #region 步驟相關 API

        /// <summary>
        /// 取得所有步驟（用於下拉選單）
        /// </summary>
        /// <returns></returns>
        public JsonResult GetAllSteps()
        {
            try
            {
                var result = _stepService.GetAllSteps();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 根據步驟ID取得步驟詳細
        /// </summary>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public JsonResult GetStepDetails(int stepId)
        {
            try
            {
                var result = _stepService.GetStepDetails(stepId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 儲存海運通關步驟（包含步驟詳細）
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="stepId"></param>
        /// <param name="stepDetailIds"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult SaveSeaClearanceStep(int seaClearanceDetailId, int stepId, int[] stepDetailIds)
        {
            try
            {
                var stepDetailList = stepDetailIds?.ToList() ?? new List<int>();
                var result = _seaClearanceService.SaveSeaClearanceStep(seaClearanceDetailId, stepId, stepDetailList);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得海運通關的當前步驟
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public JsonResult GetSeaClearanceStepHistory(int seaClearanceDetailId)
        {
            try
            {
                var result = _seaClearanceService.GetSeaClearanceStepHistory(seaClearanceDetailId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region 異常狀態相關 API

        /// <summary>
        /// 根據異常狀態ID取得異常狀態詳細
        /// </summary>
        public JsonResult GetAbnormalStateDetails(int abnormalStateId)
        {
            try
            {
                var abnormalStateService = new Service.Services.AbnormalState.AbnormalStateService();
                var result = abnormalStateService.GetAbnormalStateDetails(abnormalStateId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 儲存海運通關異常狀態(含詳細) - 新流程
        /// </summary>
        [HttpPost]
        public JsonResult SaveSeaClearanceAbnormalState(int seaClearanceDetailId, int abnormalStateId, int[] abnormalStateDetailIds)
        {
            try
            {
                var detailList = abnormalStateDetailIds?.ToList() ?? new List<int>();
                var result = _seaClearanceService.SaveSeaClearanceAbnormalState(seaClearanceDetailId, abnormalStateId, detailList);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得海運通關的異常狀態歷史
        /// </summary>
        public JsonResult GetSeaClearanceAbnormalStateHistory(int seaClearanceDetailId)
        {
            try
            {
                var result = _seaClearanceService.GetSeaClearanceAbnormalStateHistory(seaClearanceDetailId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region 備註相關 API

        /// <summary>
        /// 新增海運通關備註
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AddSeaClearanceRemark(int seaClearanceDetailId, string remark)
        {
            try
            {
                var result = _seaClearanceService.AddSeaClearanceRemark(seaClearanceDetailId, remark);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得海運通關的所有備註
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public JsonResult GetSeaClearanceRemarks(int seaClearanceDetailId)
        {
            try
            {
                var result = _seaClearanceService.GetSeaClearanceRemarks(seaClearanceDetailId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region 步驟跳轉規則 API

        /// <summary>
        /// 取得可用的步驟列表（基於跳轉規則）
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public JsonResult GetAvailableSteps(int? stepId)
        {
            try
            {
                var result = _seaClearanceService.GetAvailableSteps(stepId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region 負責人相關 API

        /// <summary>
        /// 取得負責人
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public JsonResult GetProcessor(int seaClearanceDetailId)
        {
            try
            {
                var result = _seaClearanceService.GetProcessor(seaClearanceDetailId);
                return Json(new ResopnseModel { ReturnObject = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region 更新 ETA

        /// <summary>
        /// 更新 ETA
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult UpdateEta(int id)
        {
            try
            {
                var result = _seaClearanceService.UpdateEta(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel
                {
                    msg = ex.Message
                });
            }
        }

        /// <summary>
        /// 更新艙單到港日
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult UpdateImportDate(int id)
        {
            try
            {
                var result = _seaClearanceService.UpdateImportDate(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel
                {
                    msg = ex.Message
                });
            }
        }

        #endregion

        #region 更新入倉出倉時間

        /// <summary>
        /// 更新入倉與出倉時間
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearance)]
        public JsonResult UpdateSignInOutTime(int id)
        {
            try
            {
                var result = _seaClearanceService.UpdateSignInOutTime(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel
                {
                    msg = ex.Message
                });
            }
        }

        #endregion
    }
}