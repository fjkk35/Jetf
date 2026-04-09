using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Services.WorkDayArea.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Globalization;

namespace Service.Services.WorkDayArea
{
    public class WorkDayAreaService : _BaseService
    {
        /// <summary>
        /// 取得所有作業地區列表
        /// </summary>
        public ResponseModel GetWorkAreaList()
        {
            try
            {
                string sql = @"SELECT [Id], [AreaName] FROM jetf.[dbo].[WorkArea]";

                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    var result = connection.Query<WorkAreaModel>(sql).ToList();
                    return new ResponseModel(result);
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢作業地區失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢工作天資料
        /// </summary>
        public ResponseModel QueryWorkDayArea(WorkDayAreaQueryRequest request)
        {
            try
            {
                if (request.WorkAreaId <= 0)
                {
                    return new ResponseModel("請選擇作業地區");
                }

                if (request.StartDate > request.EndDate)
                {
                    return new ResponseModel("開始日期不能大於結束日期");
                }

                // 查詢資料庫中已設定的工作天資料
                string sql = @"
                    SELECT [WorkAreaId], [DataDate], [DateType] 
                    FROM jetf.[dbo].[WorkDayArea]
                    WHERE [WorkAreaId] = @WorkAreaId 
                    AND [DataDate] >= @StartDate 
                    AND [DataDate] <= @EndDate";

                Dictionary<string, int> dbData;
                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    var dbResult = connection.Query(sql, new
                    {
                        WorkAreaId = request.WorkAreaId,
                        StartDate = request.StartDate,
                        EndDate = request.EndDate
                    }).ToList();

                    dbData = dbResult.ToDictionary(
                        x => ((DateTime)x.DataDate).ToString("yyyy-MM-dd"),
                        x => (int)x.DateType
                    );
                }

                // 產生日期區間的每一天
                var result = new List<WorkDayAreaDisplayModel>();
                var currentDate = request.StartDate.Date;
                var endDate = request.EndDate.Date;

                var culture = new CultureInfo("zh-TW");

                while (currentDate <= endDate)
                {
                    string dateStr = currentDate.ToString("yyyy-MM-dd");
                    int dateType;

                    // 如果資料庫有設定，使用資料庫的設定
                    if (dbData.ContainsKey(dateStr))
                    {
                        dateType = dbData[dateStr];
                    }
                    else
                    {
                        // 否則根據星期幾預設：週一到週五為工作天，週六週日為假日
                        dateType = (currentDate.DayOfWeek >= DayOfWeek.Monday &&
                                   currentDate.DayOfWeek <= DayOfWeek.Friday)
                                   ? (int)DateType.WorkDay
                                   : (int)DateType.Holiday;
                    }

                    string dayOfWeek = GetDayOfWeekName(currentDate.DayOfWeek);
                    string dateTypeName = dateType == (int)DateType.WorkDay ? "工作天" : "假日";

                    result.Add(new WorkDayAreaDisplayModel
                    {
                        Date = dateStr,
                        DayOfWeek = dayOfWeek,
                        DateType = dateType,
                        DateTypeName = dateTypeName
                    });

                    currentDate = currentDate.AddDays(1);
                }

                return new ResponseModel(result);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢工作天資料失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新工作天類型
        /// </summary>
        public ResponseModel UpdateWorkDayType(WorkDayAreaUpdateRequest request)
        {
            try
            {
                if (request.WorkAreaId <= 0)
                {
                    return new ResponseModel("作業地區Id不正確");
                }

                if (request.DateType != (int)DateType.WorkDay && request.DateType != (int)DateType.Holiday)
                {
                    return new ResponseModel("日期類型不正確");
                }

                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 先檢查是否已存在
                            string checkSql = @"
                                SELECT COUNT(1) 
                                FROM jetf.[dbo].[WorkDayArea]
                                WHERE [WorkAreaId] = @WorkAreaId 
                                AND [DataDate] = @Date";

                            int count = connection.ExecuteScalar<int>(checkSql, new
                            {
                                WorkAreaId = request.WorkAreaId,
                                Date = request.Date.Date
                            }, transaction);

                            if (count > 0)
                            {
                                // 更新
                                string updateSql = @"
                                    UPDATE jetf.[dbo].[WorkDayArea]
                                    SET 
                                    [DateType] = @DateType,
                                    [UpdateTime] = GETDATE(),
                                    [UpdateOpe] =@UpdateOpe               
                                    WHERE [WorkAreaId] = @WorkAreaId 
                                    AND [DataDate] = @Date";

                                connection.Execute(updateSql, new
                                {
                                    DateType = request.DateType,
                                    WorkAreaId = request.WorkAreaId,
                                    UpdateOpe = GetUserId(),
                                    Date = request.Date.Date
                                }, transaction);
                            }
                            else
                            {
                                // 新增
                                string insertSql = @"
                                    INSERT INTO jetf.[dbo].[WorkDayArea] 
                                    ([WorkAreaId], [DataDate], [DateType],[UpdateOpe],[UpdateTime])
                                    VALUES (@WorkAreaId, @Date, @DateType,@UpdateOpe,GETDATE())";

                                connection.Execute(insertSql, new
                                {
                                    WorkAreaId = request.WorkAreaId,
                                    Date = request.Date.Date,
                                    DateType = request.DateType,
                                    UpdateOpe = GetUserId(),
                                }, transaction);
                            }

                            transaction.Commit();
                            return new ResponseModel();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel($"更新工作天類型失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 取得星期幾的中文名稱
        /// </summary>
        private string GetDayOfWeekName(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return "週一";
                case DayOfWeek.Tuesday: return "週二";
                case DayOfWeek.Wednesday: return "週三";
                case DayOfWeek.Thursday: return "週四";
                case DayOfWeek.Friday: return "週五";
                case DayOfWeek.Saturday: return "週六";
                case DayOfWeek.Sunday: return "週日";
                default: return "";
            }
        }
    }
}
