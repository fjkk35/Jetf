using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearance;
using Service.Services.SeaClearanceDetailEditHistory;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.SeaClearance
{
    public partial class SeaClearanceService
    {
        /// <summary>
        /// 新增海運通關備註
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="remark"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel AddSeaClearanceRemark(int seaClearanceDetailId, string remark)
        {
            if (string.IsNullOrWhiteSpace(remark))
            {
                return new ResponseModel("備註內容不可為空");
            }

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var insertSql = @"
                        INSERT INTO jetf.dbo.SeaClearanceRemark 
                        (SeaClearanceDetailId, Remark, CrtUser, CreateTime)
                        VALUES (@SeaClearanceDetailId, @Remark, @CrtUser, @CreateTime);
                        SELECT CAST(SCOPE_IDENTITY() as int);
                    ";

                    var id = conn.QuerySingle<int>(insertSql, new
                    {
                        SeaClearanceDetailId = seaClearanceDetailId,
                        Remark = remark.Trim(),
                        CrtUser = GetUserId(),
                        CreateTime = DateTime.Now
                    }, transaction);

                    //編輯紀錄
                    _editHistoryService.RecordEdit(
                       transaction,
                       conn,
                       seaClearanceDetailId,
                       SeaClearanceEditField.Remark,
                       remark.Trim(),
                       ""
                    );

                    transaction.Commit();
                    return new ResponseModel { ReturnObject = id };
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
        /// 取得海運通關的所有備註
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public List<SeaClearanceRemarkModel> GetSeaClearanceRemarks(int seaClearanceDetailId)
        {
            var sql = @"
                SELECT 
                    Id,
                    SeaClearanceDetailId,
                    Remark,
                    CrtUser,
                    CreateTime
                FROM jetf.dbo.SeaClearanceRemark
                WHERE SeaClearanceDetailId = @SeaClearanceDetailId
                ORDER BY CreateTime DESC
            ";

            return conn.Query<SeaClearanceRemarkModel>(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId
            }).ToList();
        }
    }
}
