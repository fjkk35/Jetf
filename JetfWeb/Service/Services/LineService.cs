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

    public class LineService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public LineService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 新增LINE群組
        /// </summary>
        /// <returns></returns>
        public ResponseModel InsertLineGroup(LineGroupModel model, string user_id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("insert [jetf].[dbo].[LineGroup](GroupId,GroupName,Token,CrtUser) ");
                sb.Append("values(@GroupId,@GroupName,@Token,@CrtUser) ");
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Parameters.Add("@GroupId", SqlDbType.NVarChar).Value = model.GroupId;
                    cmd.Parameters.Add("@GroupName", SqlDbType.NVarChar).Value = model.GroupName;
                    cmd.Parameters.Add("@Token", SqlDbType.NVarChar).Value = model.Token;
                    cmd.Parameters.Add("@CrtUser", SqlDbType.NVarChar).Value = user_id;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }
            finally
            {
                conn.Close();
            }
            return resopnseModel;
        }

        /// <summary>
        /// 刪除LINE群組
        /// </summary>
        /// <returns></returns>
        public ResponseModel DeleteLineGroup(string token)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "刪除成功";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("delete [jetf].[dbo].[LineGroup] where Token=@Token");
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Parameters.Add("@Token", SqlDbType.NVarChar).Value = token;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }
            finally
            {
                conn.Close();
            }
            return resopnseModel;
        }

        /// <summary>
        /// 取得LINE群組資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetLineGroup()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from [jetf].[dbo].[LineGroup] ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得LINE設定檔
        /// </summary>
        /// <returns></returns>
        public DataTable GetLineConfig()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from [jetf].[dbo].[LineConfig] ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            return dt;

        }

        /// <summary>
        /// 檢查LINE群組是否重複
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel CheckLineGroup(LineGroupModel model)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "";
            if (model.GroupId == null || model.GroupName == null || model.GroupId.Trim() == "" || model.GroupName.Trim() == "")
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = "請輸入群組代號和名稱";
            }
            else
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("select * from [jetf].[dbo].[LineGroup] ");
                sb.Append("where GroupId=@GroupId ");
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@GroupId", SqlDbType.NVarChar).Value = model.GroupId;

                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = $"{model.GroupId}已存在LINE群組代號";
                    }
                }
            }

            return resopnseModel;
        }

        public string GetToken(string groupId)
        {
            string token = "";
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from [jetf].[dbo].[LineGroup] where GroupId=@GroupId");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@GroupId", SqlDbType.NVarChar).Value = groupId;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                token = dt.Rows[0]["Token"].ToString();
            }

            return token;
        }




    }
}
