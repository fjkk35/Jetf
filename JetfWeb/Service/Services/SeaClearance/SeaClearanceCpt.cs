using Dapper;
using Service.EnumTax;
using Service.Extensions;
using Service.Helpers;
using Service.Models.CptTradeVan;
using Service.Services.SeaClearance.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance
{
    public partial class SeaClearanceService
    {
        /// <summary>
        /// 取得關貿GB301、GB321資料
        /// </summary>
        public SeaClearanceCptModel GetCptData(GetCptDataRequest request)
        {
            var cacheKey = $"{CacheName.SeaClearanceCptData.ToString()}_{request.SeaClearanceDetailId}";

            string declNo = string.Empty;
            if (CacheHelper.Exist(cacheKey) == false)
            {
                var parameters = new Dictionary<string, string>
                   {
                       { "transType", "S" },
                       { "mawb", request.MainNumber },
                       { "hawb", request.TrackingNo }
                   };

                var gb301Result = _cptPortalApi.GetGb301(parameters);
                var gb321Result = _cptPortalApi.GetGb321(parameters);

                // 檢查DB筆數並更新Gb301、Gb321
                UpdateCptDataToDb(request, gb301Result, gb321Result, cacheKey);

                //報單號碼
                declNo = gb301Result?.DeclNo;

                // 從資料庫取得關貿資料
                var result = GetCptDataFromDb(request.SeaClearanceDetailId);

                //更新報單傳輸日、報單號碼
                var updateResult = UpdateProDateTimeDeclNo(request, result, declNo);
                result.IsUpdate = updateResult.IsUpdate;
                result.UpdatedDeclNo = updateResult.UpdatedDeclNo;
                result.UpdatedProDateTime = updateResult.UpdatedProDateTime;

                return result;
            }

            // 快取存在時，直接從資料庫取得資料
            return GetCptDataFromDb(request.SeaClearanceDetailId);
        }

        /// <summary>
        /// 從資料庫取得關貿資料
        /// </summary>
        private SeaClearanceCptModel GetCptDataFromDb(int seaClearanceDetailId)
        {
            var model = new SeaClearanceCptModel();

            //取得Gb301放行附帶條件
            var relCondCdSql = @"
                SELECT TOP 1 RelCondCd 
                FROM jetf.dbo.SeaClearanceGb301
                WHERE Id = @SeaClearanceDetailId
            ";
            model.Gb301RelCondCd = conn.QueryFirstOrDefault<string>(relCondCdSql, new { SeaClearanceDetailId = seaClearanceDetailId });

            // 取得 Gb301 資料
            var gb301Sql = @"
             SELECT ProDateTime, ProcEventCodeStr, ProgDesc 
             FROM jetf.dbo.SeaClearanceDetailGb301 
             WHERE SeaClearanceDetailId = @SeaClearanceDetailId
             ORDER BY ProDateTime DESC
             ";

            var gb301Data = conn.Query<dynamic>(gb301Sql, new { SeaClearanceDetailId = seaClearanceDetailId }).ToList();

            if (gb301Data.Any())
            {
                model.Gb301GridModel = gb301Data.Select(x => new CptGb301GridModel
                {
                    ProDateTime = x.ProDateTime,
                    ProcEventCodeStr = x.ProcEventCodeStr,
                    ProgDesc = x.ProgDesc
                }).ToList();
            }

            // 取得 Gb321 資料
            var gb321Sql = @"
                    SELECT ProDateTime, ProType 
                            FROM jetf.dbo.SeaClearanceDetailGb321 
                    WHERE SeaClearanceDetailId = @SeaClearanceDetailId
                    ORDER BY ProDateTime DESC
                  ";

            var gb321Data = conn.Query<dynamic>(gb321Sql, new { SeaClearanceDetailId = seaClearanceDetailId }).ToList();

            if (gb321Data.Any())
            {
                model.Gb321GridModel = gb321Data.Select(x => new CptGb321GridModel
                {
                    ProDateTime = x.ProDateTime,
                    ProType = x.ProType
                }).ToList();
            }

            return model;
        }

        /// <summary>
        /// 更新關貿資料到資料庫
        /// </summary>
        private void UpdateCptDataToDb(GetCptDataRequest request, Gb301Model gb301Result, Gb321Model gb321Result,string cacheKey)
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 處理 Gb321 資料
                    if (gb321Result?.GridModel?.Any() == true)
                    {
                        // 檢查 DB 筆數
                        var gb321CountSql = @"
                           SELECT COUNT(*) 
                           FROM jetf.dbo.SeaClearanceDetailGb321 
                           WHERE SeaClearanceDetailId = @SeaClearanceDetailId
                          ";
                        var gb321DbCount = conn.QuerySingle<int>(gb321CountSql,
                                          new
                                          {
                                              SeaClearanceDetailId = request.SeaClearanceDetailId
                                          }, transaction);

                        // 筆數不一樣才更新
                        if (gb321DbCount != gb321Result.GridModel.Count)
                        {
                            // 先刪除舊資料
                            var deleteGb321Sql = @"
                                  DELETE FROM jetf.dbo.SeaClearanceDetailGb321 
                                  WHERE SeaClearanceDetailId = @SeaClearanceDetailId
                                 ";
                            conn.Execute(deleteGb321Sql,
                                new
                                {
                                    SeaClearanceDetailId = request.SeaClearanceDetailId
                                }, transaction);

                            // 新增資料
                            var insertGb321Sql = @"
                           INSERT INTO jetf.dbo.SeaClearanceDetailGb321 
                                       (SeaClearanceDetailId, ProDateTime, ProType)
                            VALUES (@SeaClearanceDetailId, @ProDateTime, @ProType)";

                            foreach (var item in gb321Result.GridModel)
                            {
                                DateTime? proDateTime = null;
                                if (!string.IsNullOrEmpty(item.ProDate) && !string.IsNullOrEmpty(item.ProTime))
                                {
                                    proDateTime = $"{item.ProDate}{item.ProTime}".ToDateTime("yyyyMMddHHmmss");
                                }

                                conn.Execute(insertGb321Sql, new
                                {
                                    SeaClearanceDetailId = request.SeaClearanceDetailId,
                                    ProDateTime = proDateTime,
                                    ProType = item.ProType,
                                }, transaction);
                            }
                        }
                    }

                    //Gb301 放行附帶條件
                    if (!string.IsNullOrEmpty(gb301Result?.RelCondCd))
                    {
                        // 檢查是否已存在資料
                        var checkRelCondCdSql = @"
                                            SELECT RelCondCd FROM jetf.dbo.SeaClearanceGb301 
                                            WHERE Id = @SeaClearanceDetailId";
                        var existingRelCondCd = conn.QueryFirstOrDefault<string>(checkRelCondCdSql, new
                        {
                            SeaClearanceDetailId = request.SeaClearanceDetailId
                        }, transaction);

                        // 查無資料，新增一筆新的
                        if (string.IsNullOrEmpty(existingRelCondCd))
                        {
                            var insertRelCondCdSql = "INSERT INTO jetf.dbo.SeaClearanceGb301 (Id, RelCondCd) VALUES (@SeaClearanceDetailId, @RelCondCd)";
                            conn.Execute(insertRelCondCdSql, new
                            {
                                SeaClearanceDetailId = request.SeaClearanceDetailId,
                                RelCondCd = gb301Result.RelCondCd
                            }, transaction);
                        }
                        else if (existingRelCondCd != gb301Result.RelCondCd)
                        {
                            // 有資料但不同，才需要更新
                            var updateRelCondCdSql = "UPDATE jetf.dbo.SeaClearanceGb301 SET RelCondCd = @RelCondCd WHERE Id = @SeaClearanceDetailId";
                            conn.Execute(updateRelCondCdSql, new
                            {
                                SeaClearanceDetailId = request.SeaClearanceDetailId,
                                RelCondCd = gb301Result.RelCondCd
                            }, transaction);
                        }
                    }
                    // 處理 Gb301 資料
                    if (gb301Result?.GridModel?.Any() == true)
                    {
                        // 檢查 DB 筆數
                        var gb301CountSql = @"
                              SELECT COUNT(*) 
                              FROM jetf.dbo.SeaClearanceDetailGb301 
                              WHERE SeaClearanceDetailId = @SeaClearanceDetailId";
                        var gb301DbCount = conn.QuerySingle<int>(gb301CountSql,
                        new
                        {
                            SeaClearanceDetailId = request.SeaClearanceDetailId
                        }, transaction);

                        // 筆數不一樣才更新
                        if (gb301DbCount != gb301Result.GridModel.Count)
                        {
                            // 先刪除舊資料
                            var deleteGb301Sql = @"
                             DELETE FROM jetf.dbo.SeaClearanceDetailGb301 
                              WHERE SeaClearanceDetailId = @SeaClearanceDetailId";

                            conn.Execute(deleteGb301Sql,
                            new
                            {
                                SeaClearanceDetailId = request.SeaClearanceDetailId
                            }, transaction);

                            // 新增資料
                            var insertGb301Sql = @"
                            INSERT INTO jetf.dbo.SeaClearanceDetailGb301 
                            (SeaClearanceDetailId, ProDateTime, ProcEventCodeStr, ProgDesc)
                            VALUES (@SeaClearanceDetailId, @ProDateTime, @ProcEventCodeStr, @ProgDesc)";

                            foreach (var item in gb301Result.GridModel)
                            {
                                var proDateTime = item.ProDateTime.ToDateTime("yyyyMMddHHmmss");

                                conn.Execute(insertGb301Sql, new
                                {
                                    SeaClearanceDetailId = request.SeaClearanceDetailId,
                                    ProDateTime = proDateTime,
                                    ProcEventCodeStr = item.ProcEventCodeStr,
                                    ProgDesc = item.ProgDesc?.ToString(),
                                }, transaction);
                            }
                        }
                    }
                    transaction.Commit();

                    //加入快取，5分鐘不重複執行，其中一個有資料再加入快取
                    if (gb301Result?.GridModel?.Any() == true || gb321Result?.GridModel?.Any() == true)
                    {
                        var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) };
                        var result = CacheHelper.GetOrAdd(cacheKey, () => DateTime.Now, policy);
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 更新報單傳輸日、報單號碼
        /// X2、X3 => Gb321
        /// </summary>
        private (bool IsUpdate, string UpdatedDeclNo, DateTime? UpdatedProDateTime) UpdateProDateTimeDeclNo(GetCptDataRequest request, SeaClearanceCptModel cpt, string declNo)
        {
            var detail = GetSeaClearanceDetailById(request.SeaClearanceDetailId);
            if (detail == null)
                return (false, null, null);

            var gb321PostEntry = new List<string> { "X2", "X3" };

            // 根據報關方式取得報單傳輸日
            DateTime? proDateTime = gb321PostEntry.Contains(detail.Post_Entry)
                ? cpt.Gb321GridModel?.FirstOrDefault(r => r.ProType.Contains("連線收單建檔"))?.ProDateTime
                : cpt.Gb301GridModel?.FirstOrDefault(r => r.ProcEventCodeStr.Contains("E1 收單建檔"))?.ProDateTime;

            // 判斷需要更新的欄位
            var needUpdateDeclNo = string.IsNullOrEmpty(detail.DeclNo) && !string.IsNullOrEmpty(declNo);
            var needUpdateProDateTime = !detail.ProDateTime.HasValue && proDateTime.HasValue;

            if (!needUpdateDeclNo && !needUpdateProDateTime)
                return (false, detail.DeclNo, detail.ProDateTime);

            // 建立更新 SQL
            var updateFields = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("SeaClearanceDetailId", request.SeaClearanceDetailId);

            var updatedDeclNo = detail.DeclNo;
            var updatedProDateTime = detail.ProDateTime;

            if (needUpdateDeclNo)
            {
                updateFields.Add("DeclNo = @DeclNo");
                parameters.Add("DeclNo", declNo);
                updatedDeclNo = declNo;
            }

            if (needUpdateProDateTime)
            {
                updateFields.Add("ProDateTime = @ProDateTime");
                parameters.Add("ProDateTime", proDateTime.Value);
                updatedProDateTime = proDateTime.Value;
            }

            var updateSql = $"UPDATE [jetf].[dbo].[SeaClearanceDetail] SET {string.Join(", ", updateFields)} WHERE Id = @SeaClearanceDetailId";
            conn.Execute(updateSql, parameters);

            return (true, updatedDeclNo, updatedProDateTime);
        }
    }
}
