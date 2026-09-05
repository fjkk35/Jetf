using JETFWebAPI.Models.Global;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Services
{
    public class GlobalService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public GlobalService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

       public void InsertWebAPILog(WebAPILogModel model)
        {
            conn.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand("insert [DATA_CENTER].[dbo].[WebAPILog](ControlNmae,ActionName,RequestData,ResponseData,Remark,ClientIP) values(@ControlNmae,@ActionName,@RequestData,@ResponseData,@Remark,@ClientIP)", conn))
                {
                    cmd.Parameters.Add("@ControlNmae", SqlDbType.NVarChar).Value = model.ControlNmae;
                    cmd.Parameters.Add("@ActionName", SqlDbType.NVarChar).Value=model.ActionName;
                    cmd.Parameters.Add("@RequestData", SqlDbType.NVarChar).Value = model.RequestData;
                    cmd.Parameters.Add("@ResponseData", SqlDbType.NVarChar).Value = model.ResponseData;
                    cmd.Parameters.Add("@Remark", SqlDbType.NVarChar).Value = model.Remark;
                    cmd.Parameters.Add("@ClientIP", SqlDbType.NVarChar).Value = GetIPAddress();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {

            }
            conn.Close();
        }

        //取得IP
        public string GetIPAddress()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string sIPAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(sIPAddress))
            {
                return context.Request.ServerVariables["REMOTE_ADDR"];
            }
            else
            {
                string[] ipArray = sIPAddress.Split(new Char[] { ',' });
                return ipArray[0];
            }
        }
    }
}