using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearance;
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
        #region 異常狀態相關方法

        /// <summary>
        /// 儲存海運通關異常狀態 (含詳細) - 交易一次完成
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="abnormalStateId"></param>
        /// <param name="abnormalStateDetailIds"></param>
        /// <returns></returns>
        public ResponseModel SaveSeaClearanceAbnormalState(int seaClearanceDetailId, int abnormalStateId, List<int> abnormalStateDetailIds)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var insertSql = @"
                        INSERT INTO jetf.dbo.SeaClearanceAbnormalState 
                        (DataDate, SeaClearanceDetailId, AbnormalStateId, CrtUser)
                        VALUES (@DataDate, @SeaClearanceDetailId, @AbnormalStateId, @CrtUser);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var today = DateTime.Now.ToString("yyyy-MM-dd");
                    var seaClearanceAbnormalStateId = conn.QuerySingle<int>(insertSql, new
                    {
                        DataDate = today,
                        SeaClearanceDetailId = seaClearanceDetailId,
                        AbnormalStateId = abnormalStateId,
                        CrtUser = GetUserId()
                    }, transaction);

                    if (abnormalStateDetailIds != null && abnormalStateDetailIds.Any())
                    {
                        var insertDetailSql = @"
                            INSERT INTO jetf.dbo.SeaClearanceAbnormalStateDetail 
                            (SeaClearanceAbnormalStateId, AbnormalStateDetailId)
                            VALUES (@SeaClearanceAbnormalStateId, @AbnormalStateDetailId)
                        ";

                        foreach (var abnormalStateDetailId in abnormalStateDetailIds)
                        {
                            conn.Execute(insertDetailSql, new
                            {
                                SeaClearanceAbnormalStateId = seaClearanceAbnormalStateId,
                                AbnormalStateDetailId = abnormalStateDetailId
                            }, transaction);
                        }
                    }

                    // 更新 SeaClearanceDetail 的 CurrentAbnormalStateId
                    var updateCurrentAbnormalStateSql = @"
                        UPDATE jetf.dbo.SeaClearanceDetail 
                        SET CurrentAbnormalStateId = @AbnormalStateId 
                        WHERE Id = @SeaClearanceDetailId
                        ";

                    conn.Execute(updateCurrentAbnormalStateSql, new
                    {
                        SeaClearanceDetailId = seaClearanceDetailId,
                        AbnormalStateId = abnormalStateId
                    }, transaction);

                    //記錄編輯歷史
                    var abnormalStateName = GetAbnormalStateNameById(abnormalStateId, transaction);
                    var abnormalStateDetailNames = GetAbnormalStateDetailNameByIds(abnormalStateDetailIds, transaction);
                    var memo = abnormalStateDetailNames.Any() ? string.Join(", ", abnormalStateDetailNames) : null;
                    _editHistoryService.RecordEdit(
                        transaction,
                        conn,
                        seaClearanceDetailId,
                        SeaClearanceEditField.AbnormalState,
                        abnormalStateName,
                        memo
                    );

                    transaction.Commit();
                    return new ResponseModel { ReturnObject = seaClearanceAbnormalStateId };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ResponseModel(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 依異常狀態 Id 取得名稱 (支援交易)
        /// </summary>
        private string GetAbnormalStateNameById(int abnormalStateId, SqlTransaction transaction = null)
        {
            var sql = @"SELECT AbnormalStateName FROM jetf.dbo.AbnormalState WHERE Id = @Id";
            return conn.Query<string>(sql, new { Id = abnormalStateId }, transaction).FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// 依異常狀態詳細 Id 清單取得名稱列表 (支援交易)
        /// </summary>
        private List<string> GetAbnormalStateDetailNameByIds(List<int> abnormalStateDetailIds, SqlTransaction transaction = null)
        {
            if (abnormalStateDetailIds == null || !abnormalStateDetailIds.Any())
                return new List<string>();

            var sql = @"SELECT AbnormalStateDetailName FROM jetf.dbo.AbnormalStateDetail WHERE Id IN @Ids ORDER BY Sort";
            return conn.Query<string>(sql, new { Ids = abnormalStateDetailIds }, transaction).ToList();
        }

        /// <summary>
        /// 取得海運通關的全部異常狀態歷史
        /// </summary>
        public List<SeaClearanceAbnormalStateModel> GetSeaClearanceAbnormalStateHistory(int seaClearanceDetailId)
        {
            var sql = @"
                SELECT 
                    scas.Id,
                    scas.AbnormalStateId,
                    a.AbnormalStateName,
                    scas.DataDate,
                    scas.CrtUser,
                    scas.CreateTime
                FROM jetf.dbo.SeaClearanceAbnormalState scas
                INNER JOIN jetf.dbo.AbnormalState a ON scas.AbnormalStateId = a.Id
                WHERE scas.SeaClearanceDetailId = @SeaClearanceDetailId
                ORDER BY scas.Id DESC
            ";

            var abnormalStates = conn.Query<SeaClearanceAbnormalStateModel>(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId
            }).ToList();

            if (!abnormalStates.Any())
                return abnormalStates;

            // 一次性查詢所有相關的異常狀態詳細資料
            var abnormalStateIds = abnormalStates.Select(s => s.AbnormalStateId).Distinct().ToList();
            var seaClearanceAbnormalStateIds = abnormalStates.Select(s => s.Id).ToList();

            var allAbnormalStateDetailsSql = @"
                SELECT 
                    ad.Id,
                    ad.AbnormalStateId,
                    ad.AbnormalStateDetailName,
                    ad.Sort,
                    scasd.SeaClearanceAbnormalStateId
                FROM jetf.dbo.AbnormalStateDetail ad
                JOIN jetf.dbo.SeaClearanceAbnormalStateDetail scasd ON ad.Id = scasd.AbnormalStateDetailId 
                    AND scasd.SeaClearanceAbnormalStateId IN @SeaClearanceAbnormalStateIds
                WHERE ad.AbnormalStateId IN @AbnormalStateIds
                ORDER BY ad.AbnormalStateId, ad.Sort
            ";

            var allAbnormalStateDetails = conn.Query(allAbnormalStateDetailsSql, new
            {
                AbnormalStateIds = abnormalStateIds,
                SeaClearanceAbnormalStateIds = seaClearanceAbnormalStateIds
            }).ToList();

            // 建立字典，以 (AbnormalStateId, SeaClearanceAbnormalStateId) 為Key組合資料
            var abnormalStateDetailsDict = allAbnormalStateDetails
                .GroupBy(x => new { AbnormalStateId = (int)x.AbnormalStateId, SeaClearanceAbnormalStateId = (int?)x.SeaClearanceAbnormalStateId })
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new SeaClearanceAbnormalStateDetailItemModel
                    {
                        Id = x.Id,
                        AbnormalStateDetailId = x.Id,
                        AbnormalStateDetailName = x.AbnormalStateDetailName,
                    }).ToList()
                );

            // 為每個異常狀態組合對應的異常狀態詳細
            foreach (var abnormalState in abnormalStates)
            {
                var key = new { AbnormalStateId = abnormalState.AbnormalStateId, SeaClearanceAbnormalStateId = (int?)abnormalState.Id };

                // 取得有對應SeaClearanceAbnormalStateId的詳細資料
                if (abnormalStateDetailsDict.TryGetValue(key, out var specificDetails) && specificDetails.Any())
                {
                    abnormalState.AbnormalStateDetails = specificDetails;
                }
            }

            return abnormalStates;
        }

        #endregion
    }
}
