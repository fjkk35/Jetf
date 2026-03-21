using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearance;
using Service.Models.SeaClearanceCreate;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance
{
    public partial class SeaClearanceService
    {
        #region 步驟跳轉規則相關方法

        /// <summary>
        /// 取得海運通關的全部步驟歷史
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public List<SeaClearanceCurrentStepModel> GetSeaClearanceStepHistory(int seaClearanceDetailId)
        {
            var sql = @"
                SELECT 
                    scs.Id as SeaClearanceStepId,
                    scs.StepId,
                    s.StepName,
                    scs.DataDate,
                    scs.CrtUser,
                    scs.CreateTime
                FROM jetf.dbo.SeaClearanceStep scs
                INNER JOIN jetf.dbo.Step s ON scs.StepId = s.Id
                WHERE scs.SeaClearanceDetailId = @SeaClearanceDetailId
                ORDER BY scs.Id DESC
            ";

            var steps = conn.Query<SeaClearanceCurrentStepModel>(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId
            }).ToList();

            if (!steps.Any())
                return steps;

            // 一次性查詢所有相關的步驟詳細資料
            var stepIds = steps.Select(s => s.StepId).Distinct().ToList();
            var seaClearanceStepIds = steps.Select(s => s.SeaClearanceStepId).ToList();

            var allStepDetailsSql = @"
                SELECT 
                    sd.Id,
                    sd.StepId,
                    sd.StepDetailName,
                    sd.Sort,
                    scsd.SeaClearanceStepId
                FROM jetf.dbo.StepDetail sd
                JOIN jetf.dbo.SeaClearanceStepDetail scsd ON sd.Id = scsd.StepDetailId 
                    AND scsd.SeaClearanceStepId IN @SeaClearanceStepIds
                WHERE sd.StepId IN @StepIds
                ORDER BY sd.StepId, sd.Sort
            ";

            var allStepDetails = conn.Query(allStepDetailsSql, new
            {
                StepIds = stepIds,
                SeaClearanceStepIds = seaClearanceStepIds
            }).ToList();

            // 建立字典，以 (StepId, SeaClearanceStepId) 為Key組合資料
            var stepDetailsDict = allStepDetails
                .GroupBy(x => new { StepId = (int)x.StepId, SeaClearanceStepId = (int?)x.SeaClearanceStepId })
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new SeaClearanceStepDetailModel
                    {
                        Id = x.Id,
                        StepDetailName = x.StepDetailName,
                    }).ToList()
                );

            // 為每個步驟組合對應的步驟詳細
            foreach (var step in steps)
            {
                var key = new { StepId = step.StepId, SeaClearanceStepId = (int?)step.SeaClearanceStepId };

                // 先嘗試取得有對應SeaClearanceStepId的詳細資料
                if (stepDetailsDict.TryGetValue(key, out var specificDetails) && specificDetails.Any())
                {
                    step.StepDetails = specificDetails;
                }
            }

            return steps;
        }

        /// <summary>
        /// 取得可用的步驟列表（基於跳轉規則）
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public List<StepModel> GetAvailableSteps(int? stepId)
        {
            // 1. 取得所有步驟
            var allSteps = GetAllSteps();

            var sort = allSteps.FirstOrDefault(s => s.Id == (stepId ?? 0))?.Sort ?? 1;
            var result = allSteps.Where(r => r.Sort <= sort).ToList();
            return result;
        }

        /// <summary>
        /// 取得所有步驟
        /// </summary>
        /// <returns></returns>
        private List<StepModel> GetAllSteps()
        {
            var sql = @"
                SELECT Id, StepName,IsMultiple, Sort 
                FROM jetf.dbo.Step 
                ORDER BY Sort
            ";

            return conn.Query<StepModel>(sql).ToList();
        }

        #endregion


        #region 步驟相關方法

        /// <summary>
        /// 儲存海運通關步驟（包含步驟詳細）`
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="stepId"></param>
        /// <param name="stepDetailIds"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel SaveSeaClearanceStep(int seaClearanceDetailId, int stepId, List<int> stepDetailIds)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 1. 新增步驟記錄
                    var insertStepSql = @"
                        INSERT INTO jetf.dbo.SeaClearanceStep 
                        (DataDate, SeaClearanceDetailId, StepId, CrtUser)
                        VALUES (@DataDate, @SeaClearanceDetailId, @StepId, @CrtUser);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var today = DateTime.Now.ToString("yyyy-MM-dd");
                    var seaClearanceStepId = conn.QuerySingle<int>(insertStepSql, new
                    {
                        DataDate = today,
                        SeaClearanceDetailId = seaClearanceDetailId,
                        StepId = stepId,
                        CrtUser = GetUserId()
                    }, transaction);

                    // 2. 新增步驟詳細記錄
                    if (stepDetailIds != null && stepDetailIds.Any())
                    {
                        var insertDetailSql = @"
                            INSERT INTO jetf.dbo.SeaClearanceStepDetail 
                            (SeaClearanceStepId, StepDetailId)
                            VALUES (@SeaClearanceStepId, @StepDetailId)
                        ";

                        foreach (var stepDetailId in stepDetailIds)
                        {
                            conn.Execute(insertDetailSql, new
                            {
                                SeaClearanceStepId = seaClearanceStepId,
                                StepDetailId = stepDetailId
                            }, transaction);
                        }
                    }

                    // 3. 取得目前的 CurrentStepId
                    var currentStepIdSql = @"
                        SELECT CurrentStepId 
                        FROM jetf.dbo.SeaClearanceDetail 
                        WHERE Id = @Id
                    ";
                    var currentStepId = conn.QueryFirstOrDefault<int?>(currentStepIdSql, new { Id = seaClearanceDetailId }, transaction);

                    // 4. 取得當前步驟和新步驟的排序
                    var stepSortSql = @"
                        SELECT Id, Sort 
                        FROM jetf.dbo.Step 
                        WHERE Id IN (@CurrentStepId, @NewStepId)
                    ";
                    var stepSorts = conn.Query<dynamic>(stepSortSql, new
                    {
                        CurrentStepId = currentStepId ?? 0,
                        NewStepId = stepId
                    }, transaction).ToList();

                    // 5.欄位是否驗證通過，可以跳轉下一步驟
                    var canAutoJump = ValidateStepRequirements(seaClearanceDetailId, stepId, stepDetailIds, transaction, out var requirementFailureMessage);

                    // 5-1. 計算跳轉下一步驟
                    string calculateFailureMessage = null;
                    var nextStepId = canAutoJump
                        ? CalculateNextAvailableStep(seaClearanceDetailId, stepId, stepDetailIds, transaction, out calculateFailureMessage)
                        : stepId;

                    var autoJumpMessage = BuildAutoJumpMessage(
                        stepId,
                        nextStepId,
                        canAutoJump,
                        requirementFailureMessage,
                        calculateFailureMessage,
                        transaction);

                    // 6. 更新下一步驟ID
                    var updateLastStepSql = @"
                        UPDATE jetf.dbo.SeaClearanceDetail 
                        SET CurrentStepId = @NextStepId 
                        WHERE Id = @Id
                    ";

                    conn.Execute(updateLastStepSql, new
                    {
                        Id = seaClearanceDetailId,
                        NextStepId = nextStepId
                    }, transaction);

                    // 7. 記錄編輯歷史
                    var stepName = GetStepNameById(stepId, transaction);
                    var stepDetailNames = GetStepDetailNameByIds(stepDetailIds, transaction);
                    var memo = stepDetailNames.Any() ? string.Join(", ", stepDetailNames) : null;
                    _editHistoryService.RecordEdit(
                        transaction,
                        conn,
                        seaClearanceDetailId,
                        SeaClearanceEditField.Step,
                        stepName,
                        memo
                    );

                    // 7-1. 記錄跳轉歷史
                    if (nextStepId != stepId)
                    {
                        //新增步驟記錄
                        conn.QuerySingle<int>(insertStepSql, new
                        {
                            DataDate = today,
                            SeaClearanceDetailId = seaClearanceDetailId,
                            StepId = nextStepId,
                            CrtUser = "系統自動轉至"
                        }, transaction);

                        var lastStepName = GetStepNameById(nextStepId, transaction);
                        _editHistoryService.RecordEdit(
                            transaction,
                            conn,
                            seaClearanceDetailId,
                            SeaClearanceEditField.Step,
                            lastStepName,
                            "",
                            "系統自動轉至"
                        );
                    }

                    transaction.Commit();
                    return new ResopnseModel
                    {
                        ReturnObject = new
                        {
                            NextStepId = nextStepId,
                            AutoJumped = nextStepId != stepId,
                            AutoJumpMessage = autoJumpMessage
                        }
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResopnseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 計算可用的下一步驟
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="currentStepId"></param>
        /// <param name="selectedStepDetailIds"></param>
        /// <param name="transaction"></param>
        /// <returns>如果可以跳到下一步，返回下一步驟ID；否則返回null</returns>
        private int CalculateNextAvailableStep(int seaClearanceDetailId, int currentStepId, List<int> selectedStepDetailIds, System.Data.SqlClient.SqlTransaction transaction, out string failureMessage)
        {
            failureMessage = null;

            // 1. 取得當前步驟的跳轉條件
            var conditionsSql = @"
            select  s.Id,
                sc.RequiredStepDetailId,
                s.NextStepId,
                s.ConditionType,
                sd.StepDetailName as RequiredStepDetailName,
                ns.StepName as NextStepName
            from jetf.dbo.Step s
            left join jetf.dbo.StepCondition sc on s.Id= sc.StepId
            left join jetf.dbo.StepDetail sd on sc.RequiredStepDetailId = sd.Id
            left join jetf.dbo.Step ns on s.NextStepId = ns.Id
            where s.Id=@StepId
            ";

            var conditions = conn.Query<StepConditionModel>(conditionsSql, new { StepId = currentStepId }, transaction).ToList();

            // 如果沒有條件設定，返回下一個排序的步驟
            if (conditions.Any(r => r.ConditionType == 0))
            {

                return conditions.First().NextStepId;
            }

            var missingConditionNames = new List<string>();

            // 2. 檢查條件是否滿足
            foreach (var condition in conditions)
            {
                bool canProceed = false;

                switch (condition.ConditionType)
                {
                    case 0: // 無需條件
                        canProceed = true;
                        break;

                    case 1: // 任一符合
                        canProceed = selectedStepDetailIds != null && selectedStepDetailIds.Contains(condition.RequiredStepDetailId.Value);
                        if (!canProceed && !string.IsNullOrWhiteSpace(condition.RequiredStepDetailName))
                        {
                            missingConditionNames.Add(condition.RequiredStepDetailName);
                        }
                        break;
                }

                // 只要有一個條件滿足，就返回下一步驟ID
                if (canProceed)
                {
                    return condition.NextStepId;
                }
            }

            if (missingConditionNames.Any())
            {
                failureMessage = $"尚未勾選可觸發跳轉的步驟詳細：{string.Join("、", missingConditionNames.Distinct())}。";
            }
            else if (conditions.Any())
            {
                failureMessage = "資料表已設定跳轉條件，但目前沒有符合的步驟詳細。";
            }

            // 所有條件都不滿足，返回目前Id
            return currentStepId;
        }

        private string BuildAutoJumpMessage(int stepId, int nextStepId, bool canAutoJump, string requirementFailureMessage, string calculateFailureMessage, SqlTransaction transaction)
        {
            if (nextStepId != stepId)
            {
                var nextStepName = GetStepNameById(nextStepId, transaction);
                return string.IsNullOrWhiteSpace(nextStepName)
                    ? "步驟儲存成功，系統已自動跳到下一步。"
                    : $"步驟儲存成功，系統已自動跳至「{nextStepName}」。";
            }

            var reason = !canAutoJump
                ? requirementFailureMessage
                : calculateFailureMessage;

            return string.IsNullOrWhiteSpace(reason)
                ? "步驟儲存成功，尚未符合自動跳到下一步的條件。"
                : $"步驟儲存成功，未自動跳到下一步：{reason}";
        }

        private bool ValidateStepRequirements(int seaClearanceDetailId, int stepId, List<int> stepDetailIds, SqlTransaction transaction, out string failureMessage)
        {
            failureMessage = null;

            switch (stepId)
            {
                case 2:
                    failureMessage = GetStep2RequirementFailureMessage(seaClearanceDetailId, transaction);
                    break;
                case 7:
                    failureMessage = GetStep7RequirementFailureMessage(seaClearanceDetailId, stepDetailIds, transaction);
                    break;
                case 17:
                    failureMessage = GetStep17RequirementFailureMessage(seaClearanceDetailId, transaction);
                    break;
                case 18:
                    failureMessage = GetStep18RequirementFailureMessage(seaClearanceDetailId, stepDetailIds, transaction);
                    break;
                case 20:
                    failureMessage = GetStep20RequirementFailureMessage(seaClearanceDetailId, transaction);
                    break;
                case 22:
                    failureMessage = GetStep22RequirementFailureMessage(seaClearanceDetailId, transaction);
                    break;
                default:
                    failureMessage = null;
                    break;
            }

            return string.IsNullOrWhiteSpace(failureMessage);
        }

        private string GetStep2RequirementFailureMessage(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var sql = @"
                SELECT TOP 1
                    d.ContactChangeData,
                    d.ContactEmail,
                    d.ImportDate,
                    c.Cust_Name,
                    o.Modifyby,
                    o.Post_Entry,
                    o.Jetf_Serial,
                    o.Piece,
                    o.Importer,
                    o.Im_Phoneno,
                    o.CreateDate,
                    o.Eta
                FROM jetf.dbo.SeaClearanceDetail d
                LEFT JOIN jetf.dbo.SeaClearanceDetailOriginalMapping o ON d.Id = o.SeaClearanceDetailId
                LEFT JOIN [DATA_CENTER].[dbo].[SYS_CUST] c ON o.Cust_Code = c.CUST_CODE
                WHERE d.Id = @SeaClearanceDetailId
                ORDER BY o.Gw DESC
            ";

            var detail = conn.QueryFirstOrDefault<dynamic>(sql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction);
            if (detail == null)
            {
                return "找不到此筆通關資料。";
            }

            var missingFields = new List<string>();
            if (string.IsNullOrEmpty(detail.Cust_Name)) missingFields.Add("客戶");
            if (string.IsNullOrEmpty(detail.Modifyby)) missingFields.Add("倉別");
            if (string.IsNullOrEmpty(detail.Post_Entry)) missingFields.Add("報關方式");
            if (string.IsNullOrEmpty(detail.Jetf_Serial)) missingFields.Add("派件");
            if (detail.Piece == null) missingFields.Add("件數");
            if (string.IsNullOrEmpty(detail.Importer)) missingFields.Add("原單申報人");
            if (string.IsNullOrEmpty(detail.Im_Phoneno)) missingFields.Add("原單人電話");
            if (string.IsNullOrEmpty(detail.ContactChangeData)) missingFields.Add("聯繫人異動資料");
            if (detail.CreateDate == null) missingFields.Add("收單通知日期");
            if (detail.Eta == null) missingFields.Add("預計到港日");
            if (detail.ImportDate == null) missingFields.Add("艙單到港日");
            if (string.IsNullOrEmpty(detail.ContactEmail)) missingFields.Add("聯繫人信箱");

            return missingFields.Any()
                ? $"以下欄位尚未完成：{string.Join("、", missingFields)}。"
                : null;
        }

        private string GetStep7RequirementFailureMessage(int seaClearanceDetailId, List<int> stepDetailIds, SqlTransaction transaction)
        {
            var postEntry = GetPostEntry(seaClearanceDetailId, transaction);
            if (string.IsNullOrWhiteSpace(postEntry))
            {
                return "尚未設定報關方式。";
            }

            var signTimes = GetSeaClearanceSignTimes(seaClearanceDetailId, transaction);
            var messages = new List<string>();

            if (postEntry == "移倉" || postEntry == "轉移倉")
            {
                if (stepDetailIds == null || !stepDetailIds.Any(id => id == 25 || id == 83))
                {
                    messages.Add("未勾選「補件已收到」或「無須補件」。");
                }

                if (signTimes?.SignInTime == null)
                {
                    messages.Add("尚未填寫入倉日期。");
                }
            }
            else if (postEntry == "X2" || postEntry == "X3" || postEntry == "G1" || postEntry == "轉G1")
            {
                if (signTimes?.SignInTime == null)
                {
                    messages.Add("尚未填寫入倉日期。");
                }

                if (signTimes?.SignOutTime == null)
                {
                    messages.Add("尚未填寫出倉日期。");
                }
            }

            return messages.Any() ? string.Join("", messages) : null;
        }

        private string GetStep18RequirementFailureMessage(int seaClearanceDetailId, List<int> stepDetailIds, SqlTransaction transaction)
        {
            var postEntry = GetPostEntry(seaClearanceDetailId, transaction);
            if (string.IsNullOrWhiteSpace(postEntry))
            {
                return "尚未設定報關方式。";
            }

            if ((postEntry == "移倉" || postEntry == "轉移倉") && (stepDetailIds == null || !stepDetailIds.Contains(74)))
            {
                return "尚未勾選「線上繳納」。";
            }

            return null;
        }

        private string GetStep17RequirementFailureMessage(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var postEntry = GetPostEntry(seaClearanceDetailId, transaction);
            if (string.IsNullOrWhiteSpace(postEntry))
            {
                return "尚未設定報關方式。";
            }

            if (postEntry == "X3" || postEntry == "X2")
            {
                return "GB321 尚未出現「連線收單建檔」。";
            }

            if (postEntry == "G1" || postEntry == "移倉" || postEntry == "轉移倉" || postEntry == "轉G1")
            {
                return "GB301 尚未出現「E1 收單建檔」。";
            }

            return "目前報關方式不符合自動跳轉條件。";
        }

        private string GetStep20RequirementFailureMessage(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var signTimes = GetSeaClearanceSignTimes(seaClearanceDetailId, transaction);
            return signTimes?.SignOutTime == null ? "尚未填寫出倉日期。" : null;
        }

        private string GetStep22RequirementFailureMessage(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var sql = @"
                SELECT IsCustomsHold
                FROM jetf.dbo.SeaClearanceDetail
                WHERE Id = @SeaClearanceDetailId
            ";

            var isCustomsHold = conn.QueryFirstOrDefault<bool?>(sql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction);
            return isCustomsHold == true ? "目前為扣倉狀態。" : null;
        }

        /// <summary>
        /// 取得步驟名稱 (支援交易)
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private string GetStepNameById(int stepId, SqlTransaction transaction = null)
        {
            var sql = @"
                SELECT StepName 
                FROM jetf.dbo.Step
                WHERE Id=@StepId
                ORDER BY Sort
            ";

            return conn.Query<string>(sql, new { StepId = stepId }, transaction).FirstOrDefault();
        }

        /// <summary>
        /// 取得步驟詳細名稱 (支援交易)
        /// </summary>
        /// <param name="stepDetailIds"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private List<string> GetStepDetailNameByIds(List<int> stepDetailIds, SqlTransaction transaction = null)
        {
            var sql = @"
                SELECT StepDetailName 
                FROM jetf.dbo.StepDetail
                WHERE Id IN @StepDetailIds
                ORDER BY Sort
            ";

            return conn.Query<string>(sql, new { StepDetailIds = stepDetailIds }, transaction).ToList();
        }

        /// <summary>
        /// 驗證步驟2跳轉條件
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool ValidateStep2Jump(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var sql = @"
                SELECT TOP 1
                    d.ContactChangeData,
                    d.ContactEmail,
                    d.ImportDate,
                    c.Cust_Name,
                    o.Modifyby,
                    o.Post_Entry,
                    o.Jetf_Serial,
                    o.Piece,
                    o.Importer,
                    o.Im_Phoneno,
                    o.CreateDate,
                    o.Eta
                FROM jetf.dbo.SeaClearanceDetail d
                LEFT JOIN jetf.dbo.SeaClearanceDetailOriginalMapping o ON d.Id = o.SeaClearanceDetailId
				LEFT JOIN [DATA_CENTER].[dbo].[SYS_CUST] c on o.Cust_Code = c.CUST_CODE
                WHERE d.Id = @SeaClearanceDetailId
                ORDER BY o.Gw DESC
            ";

            var detail = conn.QueryFirstOrDefault<dynamic>(sql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction);

            if (detail == null)
            {
                return false;
            }

            var missingFields = new List<string>();

            if (string.IsNullOrEmpty(detail.Cust_Name))
                missingFields.Add("客戶");

            if (string.IsNullOrEmpty(detail.Modifyby))
                missingFields.Add("倉別");

            if (string.IsNullOrEmpty(detail.Post_Entry))
                missingFields.Add("報關方式");

            if (string.IsNullOrEmpty(detail.Jetf_Serial))
                missingFields.Add("派件");

            if (detail.Piece == null)
                missingFields.Add("件數");

            if (string.IsNullOrEmpty(detail.Importer))
                missingFields.Add("原單申報人");

            if (string.IsNullOrEmpty(detail.Im_Phoneno))
                missingFields.Add("原單人電話");

            if (string.IsNullOrEmpty(detail.ContactChangeData))
                missingFields.Add("聯繫人異動資料");

            if (detail.CreateDate == null)
                missingFields.Add("收單通知日期");

            if (detail.Eta == null)
                missingFields.Add("預計到港日");

            if (detail.ImportDate == null)
                missingFields.Add("艙單到港日");

            if (string.IsNullOrEmpty(detail.ContactEmail))
                missingFields.Add("聯繫人信箱");

            return !missingFields.Any();
        }

        /// <summary>
        /// 驗證步驟7跳轉條件
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool ValidateStep7Jump(int seaClearanceDetailId, List<int> stepDetailIds, SqlTransaction transaction)
        {
            var postEntry = GetPostEntry(seaClearanceDetailId, transaction);

            if (string.IsNullOrWhiteSpace(postEntry))
            {
                return false;
            }

            var signTimes = GetSeaClearanceSignTimes(seaClearanceDetailId, transaction);
            DateTime? signInTime = signTimes?.SignInTime;
            DateTime? signOutTime = signTimes?.SignOutTime;

            if (postEntry == "移倉" || postEntry == "轉移倉")
            {
                //25:補件已收到
                //83:無須補件
                return stepDetailIds.Any(id => id == 25 || id == 83) && signInTime != null;
            }

            if (postEntry == "X2" || postEntry == "X3" || postEntry == "G1" || postEntry == "轉G1")
            {
                return signInTime != null && signOutTime != null;
            }

            return false;
        }

        /// <summary>
        /// 驗證步驟18跳轉條件
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool ValidateStep18Jump(int seaClearanceDetailId, List<int> stepDetailIds, SqlTransaction transaction)
        {
            var postEntry = GetPostEntry(seaClearanceDetailId, transaction);

            if (string.IsNullOrWhiteSpace(postEntry))
            {
                return false;
            }

            if (postEntry == "移倉" || postEntry == "轉移倉")
            {
                //74:線上繳納
                return stepDetailIds.Any(id => id == 74 );
            }

            return true;
        }

        /// <summary>
        /// 驗證步驟17跳轉條件
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool ValidateStep17Jump(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var postEntry = GetPostEntry(seaClearanceDetailId, transaction);

            if (string.IsNullOrWhiteSpace(postEntry))
            {
                return false;
            }

            if (postEntry == "X3" || postEntry == "X2")
            {
                var proTypeSql = @"
                    select ProType
                    from jetf.dbo.SeaClearanceDetailGb321
                    where SeaClearanceDetailId = @SeaClearanceDetailId
                ";

                var proTypeList = conn.Query<string>(proTypeSql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction).ToList();
                if (proTypeList?.Any(r => r.Contains("連線收單建檔")) == true)
                {
                    return true;
                }
            }

            if (postEntry == "G1" || postEntry =="移倉" || postEntry == "轉移倉" || postEntry == "轉G1")
            {
                var procEventCodeSql = @"
                    select ProcEventCodeStr
                    from jetf.dbo.SeaClearanceDetailGb301
                    where SeaClearanceDetailId = @SeaClearanceDetailId
                ";

                var procEventCodeStrList = conn.Query<string>(procEventCodeSql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction).ToList();
                if (procEventCodeStrList?.Any(r => r.Contains("E1 收單建檔")) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 驗證步驟20跳轉條件
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool ValidateStep20Jump(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var signTimes = GetSeaClearanceSignTimes(seaClearanceDetailId, transaction);
            DateTime? signOutTime = signTimes?.SignOutTime;

            return signOutTime != null;
        }

        /// <summary>
        /// 驗證步驟22跳轉條件
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool ValidateStep22Jump(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var sql = @"
                select IsCustomsHold
                from jetf.dbo.SeaClearanceDetail
                where Id = @SeaClearanceDetailId
            ";

            var isCustomsHold = conn.QueryFirstOrDefault<bool?>(sql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction);
            return isCustomsHold != true;
        }

        /// <summary>
        /// 取得最新的報關方式
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private string GetPostEntry(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var postEntrySql = @"
                select top 1 Post_Entry
                from jetf.dbo.SeaClearanceDetailOriginalMapping
                where SeaClearanceDetailId = @SeaClearanceDetailId
                order by Gw desc
            ";

            return conn.QueryFirstOrDefault<string>(postEntrySql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction);
        }

        /// <summary>
        /// 取得入倉與出倉時間
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private dynamic GetSeaClearanceSignTimes(int seaClearanceDetailId, SqlTransaction transaction)
        {
            var detailSql = @"
                select SignInTime, SignOutTime
                from jetf.dbo.SeaClearanceDetail
                where Id = @SeaClearanceDetailId
            ";

            return conn.QueryFirstOrDefault(detailSql, new { SeaClearanceDetailId = seaClearanceDetailId }, transaction);
        }

        #endregion
    }
}
