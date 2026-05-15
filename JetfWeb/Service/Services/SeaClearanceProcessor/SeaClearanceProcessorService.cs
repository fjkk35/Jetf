using Dapper;
using Service.Models;
using Service.Services.SeaClearanceProcessor.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearanceProcessor
{
    public class SeaClearanceProcessorService : _BaseService
    {
        public SeaClearanceProcessorService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 查詢負責人列表
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <returns></returns>
        public ResponseModel GetProcessorList(SeaClearanceProcessorQueryModel query)
        {
            try
            {
                var sql = @"
                    SELECT 
                        p.[Id],
                        p.[StepId],
                        s.[StepName],
                        p.[Cust_Code],
                        c.[Cust_Name],
                        p.[X2],
                        p.[X3],
                        p.[G1],
                        p.[MoveWarehouse],
                        p.[TransferG1],
                        p.[TransferWarehouse]
                    FROM [jetf].[dbo].[SeaClearanceProcessor] p
                    LEFT JOIN [jetf].[dbo].[Step] s ON p.[StepId] = s.[Id]
                    LEFT JOIN [jetf].[dbo].[SeaClearanceCustomer] c ON p.[Cust_Code] = c.[Cust_Code]
                    WHERE 1=1
                ";

                var parameters = new DynamicParameters();

                if (query.StepId.HasValue)
                {
                    sql += " AND p.[StepId] = @StepId";
                    parameters.Add("StepId", query.StepId.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.Cust_Code))
                {
                    sql += " AND p.[Cust_Code] = @Cust_Code";
                    parameters.Add("Cust_Code", query.Cust_Code);
                }

                sql += " ORDER BY s.[Sort], c.[Cust_Code]";

                conn.Open();
                var result = conn.Query<SeaClearanceProcessorModel>(sql, parameters).ToList();
                conn.Close();

                return new ResponseModel { ReturnObject = result };
            }
            catch (Exception ex)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 根據ID取得負責人資料
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns></returns>
        public ResponseModel GetById(int id)
        {
            try
            {
                var sql = @"
                    SELECT 
                        p.[Id],
                        p.[StepId],
                        s.[StepName],
                        p.[Cust_Code],
                        c.[Cust_Name],
                        p.[X2],
                        p.[X3],
                        p.[G1],
                        p.[MoveWarehouse],
                        p.[TransferG1],
                        p.[TransferWarehouse]
                    FROM [jetf].[dbo].[SeaClearanceProcessor] p
                    LEFT JOIN [jetf].[dbo].[Step] s ON p.[StepId] = s.[Id]
                    LEFT JOIN [jetf].[dbo].[SeaClearanceCustomer] c ON p.[Cust_Code] = c.[Cust_Code]
                    WHERE p.[Id] = @Id
                ";

                conn.Open();
                var result = conn.QueryFirstOrDefault<SeaClearanceProcessorModel>(sql, new { Id = id });
                conn.Close();

                return new ResponseModel { ReturnObject = result };
            }
            catch (Exception ex)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 新增負責人
        /// </summary>
        /// <param name="model">負責人資料</param>
        /// <returns></returns>
        public ResponseModel CreateProcessor(SeaClearanceProcessorRequestModel model)
        {
            try
            {
                // 驗證必填欄位
                if (model.StepId <= 0)
                {
                    return new ResponseModel("步驟為必填");
                }

                if (string.IsNullOrWhiteSpace(model.Cust_Code))
                {
                    return new ResponseModel("客戶為必填");
                }

                // 驗證至少有一個負責人
                if (string.IsNullOrWhiteSpace(model.X2) &&
                    string.IsNullOrWhiteSpace(model.X3) &&
                    string.IsNullOrWhiteSpace(model.G1) &&
                    string.IsNullOrWhiteSpace(model.MoveWarehouse) &&
                    string.IsNullOrWhiteSpace(model.TransferG1) &&
                    string.IsNullOrWhiteSpace(model.TransferWarehouse))
                {
                    return new ResponseModel("至少需要填寫一個負責人");
                }

                conn.Open();

                // 檢查是否已存在相同的 StepId 和 Cust_Code 組合
                var checkSql = @"
                    SELECT COUNT(*) 
                    FROM [jetf].[dbo].[SeaClearanceProcessor] 
                    WHERE [StepId] = @StepId AND [Cust_Code] = @Cust_Code
                ";

                var exists = conn.ExecuteScalar<int>(checkSql, new { model.StepId, model.Cust_Code }) > 0;

                if (exists)
                {
                    conn.Close();
                    return new ResponseModel("此步驟與客戶的組合已存在，無法重複新增");
                }

                // 新增資料
                var insertSql = @"
                    INSERT INTO [jetf].[dbo].[SeaClearanceProcessor] 
                    ([StepId], [Cust_Code], [X2], [X3], [G1], [MoveWarehouse], [TransferG1], [TransferWarehouse])
                    VALUES 
                    (@StepId, @Cust_Code, @X2, @X3, @G1, @MoveWarehouse, @TransferG1, @TransferWarehouse)
                ";

                conn.Execute(insertSql, model);
                conn.Close();

                return new ResponseModel { msg = "新增成功" };
            }
            catch (Exception ex)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 更新負責人
        /// </summary>
        /// <param name="model">負責人資料</param>
        /// <returns></returns>
        public ResponseModel UpdateProcessor(SeaClearanceProcessorRequestModel model)
        {
            try
            {
                // 驗證必填欄位
                if (!model.Id.HasValue || model.Id.Value <= 0)
                {
                    return new ResponseModel("ID為必填");
                }

                if (model.StepId <= 0)
                {
                    return new ResponseModel("步驟為必填");
                }

                if (string.IsNullOrWhiteSpace(model.Cust_Code))
                {
                    return new ResponseModel("客戶為必填");
                }

                // 驗證至少有一個負責人
                if (string.IsNullOrWhiteSpace(model.X2) &&
                    string.IsNullOrWhiteSpace(model.X3) &&
                    string.IsNullOrWhiteSpace(model.G1) &&
                    string.IsNullOrWhiteSpace(model.MoveWarehouse) &&
                    string.IsNullOrWhiteSpace(model.TransferG1) &&
                    string.IsNullOrWhiteSpace(model.TransferWarehouse))
                {
                    return new ResponseModel("至少需要填寫一個負責人");
                }

                conn.Open();

                // 檢查是否有其他記錄使用相同的 StepId 和 Cust_Code 組合
                var checkSql = @"
                    SELECT COUNT(*) 
                    FROM [jetf].[dbo].[SeaClearanceProcessor] 
                    WHERE [StepId] = @StepId 
                    AND [Cust_Code] = @Cust_Code 
                    AND [Id] != @Id
                ";

                var exists = conn.ExecuteScalar<int>(checkSql, new { model.StepId, model.Cust_Code, model.Id }) > 0;

                if (exists)
                {
                    conn.Close();
                    return new ResponseModel("此步驟與客戶的組合已存在於其他記錄中");
                }

                // 更新資料
                var updateSql = @"
                    UPDATE [jetf].[dbo].[SeaClearanceProcessor] 
                    SET 
                        [StepId] = @StepId,
                        [Cust_Code] = @Cust_Code,
                        [X2] = @X2,
                        [X3] = @X3,
                        [G1] = @G1,
                        [MoveWarehouse] = @MoveWarehouse,
                        [TransferG1] = @TransferG1,
                        [TransferWarehouse] = @TransferWarehouse
                    WHERE [Id] = @Id
                ";

                var affected = conn.Execute(updateSql, new
                {
                    model.Id,
                    model.StepId,
                    model.Cust_Code,
                    model.X2,
                    model.X3,
                    model.G1,
                    model.MoveWarehouse,
                    model.TransferG1,
                    model.TransferWarehouse
                });

                conn.Close();

                if (affected == 0)
                {
                    return new ResponseModel("找不到要更新的資料");
                }

                return new ResponseModel { msg = "更新成功" };
            }
            catch (Exception ex)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 刪除負責人
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns></returns>
        public ResponseModel DeleteProcessor(int id)
        {
            try
            {
                var sql = @"
                    DELETE FROM [jetf].[dbo].[SeaClearanceProcessor] 
                    WHERE [Id] = @Id
                ";

                conn.Open();
                var affected = conn.Execute(sql, new { Id = id });
                conn.Close();

                if (affected == 0)
                {
                    return new ResponseModel("找不到要刪除的資料");
                }

                return new ResponseModel { msg = "刪除成功" };
            }
            catch (Exception ex)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                return new ResponseModel(ex.Message);
            }
        }
    }
}
