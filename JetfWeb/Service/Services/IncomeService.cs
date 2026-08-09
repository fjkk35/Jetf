using Service.Models;
using Service.Models.IncomeReport;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class IncomeService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public IncomeService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 轉入資料
        /// </summary>
        public void Insert_Income_Report(string sDate, string eDate)
        {
            int days = Convert.ToInt32((Convert.ToDateTime(eDate) - Convert.ToDateTime(sDate)).TotalDays) + 1;
            DateTime date = Convert.ToDateTime(sDate);
            try
            {
                conn.Open();
                for (int i = 0; i < days; i++)
                {
                    using (SqlCommand cmd = new SqlCommand("jetf.dbo.SP_Insert_Income_Report", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Clear();
                        cmd.Parameters.Add("@DataDate", SqlDbType.NVarChar).Value = date.AddDays(i).ToString("yyyyMMdd");
                        cmd.Parameters.Add("@SDate_ETL", SqlDbType.DateTime).Value = $"{date.AddDays(i).ToString("yyyy-MM-dd")} 09:00:00";
                        cmd.Parameters.Add("@EDate_ETL", SqlDbType.DateTime).Value = $"{date.AddDays(i + 1).ToString("yyyy-MM-dd")} 08:59:59";
                        cmd.Parameters.Add("@SDate", SqlDbType.DateTime).Value = $"{date.AddDays(i).ToString("yyyy-MM-dd")} 00:00:00";
                        cmd.Parameters.Add("@EDate", SqlDbType.DateTime).Value = $"{date.AddDays(i).ToString("yyyy-MM-dd")} 23:59:59";
                        cmd.CommandTimeout = 600;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                conn.Close();
            }
        }

        public DataTableModel IncomeReport_Details(string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT * FROM [jetf].[dbo].[Income_Report] ");
                sb.Append("where DataDate between @sDate and @eDate ");
                sb.Append("order by DataDate,TRAN_TYPE,DATA_TYPE,DESPATCH_NO ");

                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }

                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        public DataTableModel IncomeReport_Day(string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Report_Day]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        public DataTableModel IncomeReport_Day2(string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Report_Day2]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        /// <summary>
        /// 空快、海快營收客戶去年比
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public Tuple<List<IncomeReportCustomerRateModel>, List<IncomeReportCustomerRateModel>> IncomeReportCustomerRate(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Report_Day2_Rate]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }

            var list = dt.AsEnumerable().Select(r => new IncomeReportCustomerRateModel()
            {
                TranType = r.Field<string>("TranType").Trim(),
                DespatchName = r.Field<string>("DespatchName").Trim(),
                CC = r.Field<decimal?>("CC") ?? 0,
                FEE2 = r.Field<int?>("FEE2") ?? 0,
                Gw = r.Field<decimal?>("Gw") ?? 0,
                BagNumberCount = r.Field<int?>("BagNumberCount") ?? 0,
                Count = r.Field<int?>("TotalCount") ?? 0,
                TotalCC = r.Field<decimal?>("TotalCC") ?? 0,
                TotalFEE2 = r.Field<int?>("TotalFEE2") ?? 0,
                TotalGw = r.Field<decimal?>("TotalGw") ?? 0,
                TotalBagNumberCount = r.Field<int?>("TotalBagNumberCount") ?? 0,
                TotalCount = r.Field<int?>("TotalCount") ?? 0,
            }).ToList();

            var etlList = list.Where(r => r.TranType == "進口空快").ToList();
            var seaList = list.Where(r => r.TranType == "進口海快").ToList();
            return  new Tuple<List<IncomeReportCustomerRateModel>, List<IncomeReportCustomerRateModel>>(etlList, seaList);
        }

        /// <summary>
        /// 轉入資料-到港日
        /// </summary>
        public void Insert_Income_ETA_Report(string sDate, string eDate)
        {
            int days = Convert.ToInt32((Convert.ToDateTime(eDate) - Convert.ToDateTime(sDate)).TotalDays) + 1;
            DateTime date = Convert.ToDateTime(sDate);
            try
            {
                conn.Open();
                for (int i = 0; i < days; i++)
                {
                    using (SqlCommand cmd = new SqlCommand("jetf.dbo.SP_Insert_Income_ETA_Report", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Clear();
                        cmd.Parameters.Add("@DataDate", SqlDbType.NVarChar).Value = date.AddDays(i).ToString("yyyyMMdd");
                        cmd.CommandTimeout = 600;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                conn.Close();
            }
        }

        public DataTableModel IncomeETAReport(string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_ETA_Report]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        public DataTableModel IncomeETAReport_Type(string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_ETA_Report_Type]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        public DataTableModel IncomeETAReport_Day(string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_ETA_Report_Day]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        public DataTableModel IncomeETAReport_Day2(string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_ETA_Report_Day2]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        public DataTableModel IncomeDetails(string originl, string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Details]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@ORIGINAL", SqlDbType.NVarChar).Value = originl;
                    da.SelectCommand.Parameters.Add("@SDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{sDate}000000", "yyyyMMddHHmmss", null);
                    da.SelectCommand.Parameters.Add("@EDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{eDate}235959", "yyyyMMddHHmmss", null);
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        public DataTableModel IncomeDetailsReport(string originl, string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Details_Report]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@ORIGINAL", SqlDbType.NVarChar).Value = originl;
                    da.SelectCommand.Parameters.Add("@SDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{sDate}000000", "yyyyMMddHHmmss", null);
                    da.SelectCommand.Parameters.Add("@EDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{eDate}235959", "yyyyMMddHHmmss", null);
                    da.Fill(dt);
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }
    }
}
