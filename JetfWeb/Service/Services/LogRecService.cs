using Service.Models;
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

    public class LogRecService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public LogRecService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 新增上傳檔案LOG
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel InsertLog_Rec(LogRecModel model) {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[LOG_REC]([DATADATE],[FUN_INDEX],[FUN_DATADATE],[FUN_TYPE],[FUN_FILENAME],[FUN_MEMO],[USER_ID],[USER_IP],[LOG_TIME]) ");
            sb.Append("values(@DATADATE,@FUN_INDEX,@FUN_DATADATE,@FUN_TYPE,@FUN_FILENAME,@FUN_MEMO,@USER_ID,@USER_IP,@LOG_TIME) ");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(),conn))
            {
                try
                {
                    conn.Open();
                    cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = model.datadate;
                    cmd.Parameters.Add("@FUN_INDEX", SqlDbType.NVarChar).Value = model.fun_index;
                    cmd.Parameters.Add("@FUN_DATADATE", SqlDbType.NVarChar).Value = model.fun_datadate;
                    cmd.Parameters.Add("@FUN_TYPE", SqlDbType.NVarChar).Value = model.fun_type;
                    cmd.Parameters.Add("@FUN_FILENAME", SqlDbType.NVarChar).Value = model.fun_filename;
                    cmd.Parameters.Add("@FUN_MEMO", SqlDbType.NVarChar).Value = model.fun_memo;
                    cmd.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = model.user_id;
                    cmd.Parameters.Add("@USER_IP", SqlDbType.NVarChar).Value = model.user_ip;
                    cmd.Parameters.Add("@LOG_TIME", SqlDbType.NVarChar).Value = model.log_time;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                }
                finally {
                    conn.Close();
                }
            }
            return resopnseModel;
        }

        ///// <summary>
        ///// 紀錄LOG
        ///// </summary>
        ///// <param name="fun_index"></param>
        ///// <param name="fun_datadate"></param>
        ///// <param name="fun_type"></param>
        ///// <param name="fun_filename"></param>
        ///// <param name=""></param>
        //public void InsertLog_Rec(string fun_index, string fun_datadate, string fun_type, string fun_filename, string fun_memo)
        //{
        //    LogRecModel logRecModel = new LogRecModel()
        //    {
        //        datadate = DateTime.Now.ToString("yyyyMMdd"),
        //        fun_datadate = fun_datadate,
        //        fun_index = fun_index,
        //        fun_type = fun_type,
        //        fun_filename = fun_filename,
        //        fun_memo = fun_memo,
        //        user_ip = GetIPAddress(),
        //        user_id = Session["user_id"].ToString(),
        //        log_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        //    };
        //    logRecService.InsertLog_Rec(logRecModel);
        //}

       
    }
}
