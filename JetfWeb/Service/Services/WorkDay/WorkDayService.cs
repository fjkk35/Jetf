using Service.EnumTax;
using Service.Models;
using Service.Models.WorkDay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.WorkDay
{
    public class WorkDayService :_BaseService
    {
        public WorkDayService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public List<DateRequest> GetDate(DateTime startDate,DateTime endDate) 
        {
            var list = new List<DateRequest>();

            var result = GetWorkDay();

            var workDays = result.Item1;
            var holidays = result.Item2;

            while (startDate <= endDate)
            {
                list.Add(new DateRequest()
                {
                    Date = startDate.ToString("yyyy/MM/dd"),
                    DateType = GetDateType(startDate, workDays, holidays)
                });

                startDate = startDate.AddDays(1);
            }
            return list;
        }

        /// <summary>
        /// 修改日期類別
        /// </summary>
        /// <param name="date"></param>
        /// <param name="type"></param>
        public ResponseModel UpdateType(DateTime date, DateType type,string updateOpe)
        {
            var resopnseModel = new ResponseModel();

            try
            {
                var sql = @"
                        select DataDate from jetf.[dbo].[WorkDay] where DataDate =@DataDate

                        if(@@ROWCOUNT > 0)
                        begin
	                        update jetf.[dbo].[WorkDay] set DateType=@DateType,UpdateOpe=@UpdateOpe,UpdateTime=GETDATE()
	                        where DataDate =@DataDate 
                        end
                        else
                        begin
		                        insert jetf.[dbo].[WorkDay](DataDate,DateType,UpdateOpe,UpdateTime)
		                        values(@DataDate,@DateType,@UpdateOpe,GETDATE())
                        end
                        ";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();

                    cmd.Parameters.Add("@DataDate", SqlDbType.NVarChar).Value = date.ToString("yyyy-MM-dd");
                    cmd.Parameters.Add("@DateType", SqlDbType.NVarChar).Value = (int)type;
                    cmd.Parameters.Add("@UpdateOpe", SqlDbType.NVarChar).Value = updateOpe;

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }
          

            return resopnseModel;
        }

        /// <summary>
        /// 是否為假日
        /// </summary>
        /// <returns></returns>
        public DateType GetDateType(DateTime date,DateTime[] workDays, DateTime[] holidays) 
        {
            if (workDays.Any(r => r == date))
                return DateType.WorkDay;

            if (holidays.Any(r => r == date))
                return DateType.Holiday;

            var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            return  isWeekend ? DateType.Holiday : DateType.WorkDay;
        }


        public Tuple<DateTime[],DateTime[]> GetWorkDay() 
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from jetf.[dbo].[WorkDay]", conn))
            {
                da.Fill(dt);
            }

            var workDays = dt.AsEnumerable().Where(r => (DateType)r.Field<int>("DateType") == DateType.WorkDay)
                .Select(r => r.Field<DateTime>("DataDate"))
                .ToArray();

            var holidays = dt.AsEnumerable().Where(r => (DateType)r.Field<int>("DateType") == DateType.Holiday)
               .Select(r => r.Field<DateTime>("DataDate") )
               .ToArray();

            return new Tuple<DateTime[], DateTime[]>(workDays,holidays);

        }

        /// <summary>
        /// 計算X天後的工作天日期
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="workDays"></param>
        /// <returns></returns>
        public DateTime AddWorkDays(DateTime[] workDays, DateTime[] holidays, DateTime startDate, int days)
        {
            int addedDays = 0;

            while (addedDays < days)
            {
                // 移動到下一天
                startDate = startDate.AddDays(1);

                var type = GetDateType(startDate, workDays, holidays);

                // 如果不是週末且不是假日，計算為一個工作日
                if (type == DateType.WorkDay)
                {
                    addedDays++;
                }
            }

            return startDate;
        }
    }
}
