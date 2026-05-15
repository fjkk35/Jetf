using Dapper;
using Service.Extensions;
using Service.Models;
using Service.Models.CustomsBroker;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CustomsBroker
{
    public class CustomsBrokerService : _BaseService
    {
        public CustomsBrokerService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得報驗公司列表 (含聯絡人)
        /// </summary>
        public CustomsBrokerResponse GetData(CustomsBrokerRequest request)
        {
            var parameters = new DynamicParameters();

            var sql = new StringBuilder();
            sql.AppendLine(@"
                SELECT 
                    cb.Id,
                    cb.Name,
                    cb.PortArea,
                    cbc.Category,
                    cbc.Id AS ContactId,
                    cbc.ContactPerson,
                    cbc.Email,
                    cbc.Phone,
                    COALESCE(cbc.UpdateDateTime, cb.UpdateDateTime) AS UpdateDateTime,
                    COALESCE(cbc.UpdateOperator, cb.UpdateOperator) AS UpdateOperator
                FROM [jetf].[dbo].[CustomsBroker] cb
                LEFT JOIN [jetf].[dbo].[CustomsBrokerContact] cbc ON cb.Id = cbc.CustomsBrokerId
                WHERE cb.IsDelete = 0");

            sql.WhereIf(!string.IsNullOrEmpty(request.Name), 
                "cb.Name LIKE @Name", 
                parameters, 
                p => p.Add("Name", $"%{request.Name}%"));

            sql.WhereIf(!string.IsNullOrEmpty(request.ContactPerson), 
                "cbc.ContactPerson LIKE @ContactPerson", 
                parameters, 
                p => p.Add("ContactPerson", $"%{request.ContactPerson}%"));

            sql.WhereIf(!string.IsNullOrEmpty(request.PortArea), 
                "cb.PortArea = @PortArea", 
                parameters, 
                p => p.Add("PortArea", request.PortArea));

            sql.WhereIf(!string.IsNullOrEmpty(request.Category), 
                "cb.Category LIKE @Category", 
                parameters, 
                p => p.Add("Category", $"%{request.Category}%"));

            sql.WhereIf(!string.IsNullOrEmpty(request.Email), 
                "cbc.Email LIKE @Email", 
                parameters, 
                p => p.Add("Email", $"%{request.Email}%"));

            sql.WhereIf(!string.IsNullOrEmpty(request.Phone), 
                "cbc.Phone LIKE @Phone", 
                parameters, 
                p => p.Add("Phone", $"%{request.Phone}%"));

            var countSql = $@"
                SELECT COUNT(*) 
                FROM ({sql.ToString()}) AS SubQuery";

            var dataSql = $@"
                {sql.ToString()}
                ORDER BY cb.Id DESC, cbc.Id
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("Offset", (request.Page - 1) * request.PageSize);
            parameters.Add("PageSize", request.PageSize);

            using (var query = conn.QueryMultiple($"{countSql}; {dataSql}", parameters))
            {
                var totalCount = query.ReadFirst<int>();
                var data = query.Read<CustomsBrokerWithContactModel>().ToList();

                return new CustomsBrokerResponse
                {
                    TotalCount = totalCount,
                    Data = data
                };
            }
        }

        /// <summary>
        /// 根據 ID 取得報驗公司資料
        /// </summary>
        public CustomsBrokerModel GetById(int id)
        {
            var sql = "SELECT * FROM [jetf].[dbo].[CustomsBroker] WHERE Id = @Id";
            return conn.QueryFirstOrDefault<CustomsBrokerModel>(sql, new { Id = id });
        }

        /// <summary>
        /// 新增報驗公司
        /// </summary>
        public ResponseModel Insert(CustomsBrokerModel model)
        {
            try
            {
                var sql = @"
                    INSERT INTO [jetf].[dbo].[CustomsBroker]
                    (Name, PortArea, UpdateDateTime, UpdateOperator, IsDelete)
                    VALUES
                    (@Name, @PortArea, @UpdateDateTime, @UpdateOperator, 0)";

                var parameters = new
                {
                    Name = model.Name,
                    PortArea = model.PortArea,
                    UpdateDateTime = DateTime.Now,
                    UpdateOperator = GetUserId(),
                };

                conn.Execute(sql, parameters);
                return new ResponseModel();
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 更新報驗公司
        /// </summary>
        public ResponseModel Update(CustomsBrokerModel model, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                var sql = @"
                    UPDATE [jetf].[dbo].[CustomsBroker] 
                    SET Name = @Name, 
                        PortArea = @PortArea, 
                        UpdateDateTime = @UpdateDateTime, 
                        UpdateOperator = @UpdateOperator
                    WHERE Id = @Id";

                var parameters = new
                {
                    Id = model.Id,
                    Name = model.Name,
                    PortArea = model.PortArea,
                    UpdateDateTime = DateTime.Now,
                    UpdateOperator = userId
                };

                var rowsAffected = conn.Execute(sql, parameters);
                if (rowsAffected > 0)
                {
                    response.msg = "更新成功";
                }
                else
                {
                    response = new ResponseModel("更新失敗，找不到指定的記錄");
                }
            }
            catch (Exception ex)
            {
                response = new ResponseModel(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// 刪除報驗公司 (軟刪除)
        /// </summary>
        public ResponseModel Delete(int id, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                var sql = @"
                    UPDATE [jetf].[dbo].[CustomsBroker] 
                    SET IsDelete = 1,
                        UpdateDateTime = GETDATE(),
                        UpdateOperator = @UserId
                    WHERE Id = @Id";

                var rowsAffected = conn.Execute(sql, new { Id = id, UserId = userId });
                
                if (rowsAffected > 0)
                {
                    response.msg = "刪除成功";
                }
                else
                {
                    response = new ResponseModel("刪除失敗，找不到指定的記錄");
                }
            }
            catch (Exception ex)
            {
                response = new ResponseModel(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// 取得所有報驗公司 (用於下拉選單)
        /// </summary>
        public List<CustomsBrokerDropdownModel> GetAllForDropdown()
        {
            var sql = @"
                SELECT Id AS Value, Name AS Text 
                FROM [jetf].[dbo].[CustomsBroker] 
                WHERE IsDelete = 0 
                ORDER BY Name";
            return conn.Query<CustomsBrokerDropdownModel>(sql).ToList();
        }

        #region 聯絡人相關方法

        /// <summary>
        /// 根據 ID 取得聯絡人資料
        /// </summary>
        public CustomsBrokerContactModel GetContactById(int id)
        {
            var sql = "SELECT * FROM [jetf].[dbo].[CustomsBrokerContact] WHERE Id = @Id";
            return conn.QueryFirstOrDefault<CustomsBrokerContactModel>(sql, new { Id = id });
        }

        /// <summary>
        /// 新增聯絡人
        /// </summary>
        public ResponseModel InsertContact(CustomsBrokerContactModel model)
        {
            try
            {
                var sql = @"
                    INSERT INTO [jetf].[dbo].[CustomsBrokerContact]
                    (CustomsBrokerId, ContactPerson, Email, Phone, Category, UpdateDateTime, UpdateOperator)
                    VALUES
                    (@CustomsBrokerId, @ContactPerson, @Email, @Phone, @Category, @UpdateDateTime, @UpdateOperator)";

                var parameters = new
                {
                    CustomsBrokerId = model.CustomsBrokerId,
                    ContactPerson = model.ContactPerson,
                    Email = model.Email,
                    Phone = model.Phone,
                    Category = model.Category,
                    UpdateDateTime = DateTime.Now,
                    UpdateOperator = GetUserId(),
                };

                conn.Execute(sql, parameters);
                return new ResponseModel();
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 更新聯絡人
        /// </summary>
        public ResponseModel UpdateContact(CustomsBrokerContactModel model, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                var sql = @"
                    UPDATE [jetf].[dbo].[CustomsBrokerContact] 
                    SET ContactPerson = @ContactPerson, 
                        Email = @Email, 
                        Phone = @Phone, 
                        Category = @Category,
                        UpdateDateTime = @UpdateDateTime, 
                        UpdateOperator = @UpdateOperator
                    WHERE Id = @Id";

                var parameters = new
                {
                    Id = model.Id,
                    ContactPerson = model.ContactPerson,
                    Email = model.Email,
                    Phone = model.Phone,
                    Category = model.Category,
                    UpdateDateTime = DateTime.Now,
                    UpdateOperator = userId
                };

                var rowsAffected = conn.Execute(sql, parameters);
                if (rowsAffected > 0)
                {
                    response.msg = "更新成功";
                }
                else
                {
                    response = new ResponseModel("更新失敗，找不到指定的記錄");
                }
            }
            catch (Exception ex)
            {
                response = new ResponseModel(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// 刪除聯絡人 (實體刪除)
        /// </summary>
        public ResponseModel DeleteContact(int id)
        {
            var response = new ResponseModel();
            
            try
            {
                var sql = "DELETE FROM [jetf].[dbo].[CustomsBrokerContact] WHERE Id = @Id";

                var rowsAffected = conn.Execute(sql, new { Id = id });
                
                if (rowsAffected > 0)
                {
                    response.msg = "刪除成功";
                }
                else
                {
                    response = new ResponseModel("刪除失敗，找不到指定的記錄");
                }
            }
            catch (Exception ex)
            {
                response = new ResponseModel(ex.Message);
            }

            return response;
        }

        #endregion
    }
}
