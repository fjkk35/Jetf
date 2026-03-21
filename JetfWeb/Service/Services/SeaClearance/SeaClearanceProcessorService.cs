using Dapper;
using System;

namespace Service.Services.SeaClearance
{
    public partial class SeaClearanceService
    {
        #region 負責人相關方法

        /// <summary>
        /// 取得負責人
        /// </summary>
        /// <param name="seaClearanceDetailId">海運通關明細ID</param>
        /// <returns></returns>
        public string GetProcessor(int seaClearanceDetailId)
        {
            try
            {
                // 1. 取得明細資料
                var sql = @"
                    SELECT 
                        a.CurrentStepId,
                        b.Post_Entry,
                        b.Cust_Code
                    FROM [jetf].[dbo].[SeaClearanceDetail] a
                    JOIN [jetf].[dbo].[SeaClearanceDetailOriginalMapping] b ON a.Id = b.SeaClearanceDetailId
                    WHERE a.Id = @SeaClearanceDetailId AND b.Gw > 0
                ";

                var detail = conn.QueryFirstOrDefault<dynamic>(sql, new { SeaClearanceDetailId = seaClearanceDetailId });

                if (detail == null)
                {
                    return string.Empty;
                }

                // 2. 判斷步驟ID，如果 CurrentStepId 為 null，則使用步驟 Id=2
                int stepId = detail.CurrentStepId ?? 2;
                string postEntry = detail.Post_Entry;
                string custCode = detail.Cust_Code;

                // 3. 查詢負責人
                var processorSql = @"
                    SELECT 
                        [X2], [X3], [G1], [MoveWarehouse], [TransferG1], [TransferWarehouse]
                    FROM [jetf].[dbo].[SeaClearanceProcessor]
                    WHERE [StepId] = @StepId AND [Cust_Code] = @Cust_Code
                ";

                var processor = conn.QueryFirstOrDefault<dynamic>(processorSql, new
                {
                    StepId = stepId,
                    Cust_Code = custCode
                });

                if (processor == null)
                {
                    return string.Empty;
                }

                // 4. 根據報關方式找出對應的負責人
                string processorName = string.Empty;

                switch (postEntry)
                {
                    case "X2":
                        processorName = processor.X2;
                        break;
                    case "X3":
                        processorName = processor.X3;
                        break;
                    case "G1":
                        processorName = processor.G1;
                        break;
                    case "移倉":
                        processorName = processor.MoveWarehouse;
                        break;
                    case "轉G1":
                        processorName = processor.TransferG1;
                        break;
                    case "轉移倉":
                        processorName = processor.TransferWarehouse;
                        break;
                }

                return processorName ?? string.Empty;
            }
            catch (Exception ex)
            {
                // 記錄錯誤但不影響其他功能
                return string.Empty;
            }
        }

        #endregion

    }
}
