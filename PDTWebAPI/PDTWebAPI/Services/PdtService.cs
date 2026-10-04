using PDTWebAPI.Models.Pdt;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace PDTWebAPI.Services
{
    public class PdtService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public PdtService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        ~PdtService()
        {
            conn.Dispose();
        }

        public LoginResponseModel PostLogin(LoginModel body)
        {
            LoginResponseModel response = new LoginResponseModel();
            response.Status = "Success";
            try
            {
                DataTable dt = GetPdtVersion();
                //需要更新
                if (dt.Rows[0]["Version"].ToString().Trim() != body.Version.Trim())
                {
                    response.ResultCode = "Y";
                    response.Url = $"{dt.Rows[0]["Url"].ToString()}?token={GetToken("Login",body.UploadOpe)}&uploadOpe={body.UploadOpe}";
                }
                else {
                    response.ResultCode = "N";
                }
                //取得派件公司
                response.TransList = GetTrans();
                //取得作業區域清單
                response.DataTypeList = GetDataType();
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }

            return response;
        }

        /// <summary>
        /// 取得派件公司
        /// </summary>
        /// <returns></returns>
        List<TransItem> GetTrans() {
           List<TransItem> transList = new List<TransItem>();
           DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select TransNo,TransName from  [jetf].[dbo].[PdtTrans] order by Sort", conn))
            {
                da.Fill(dt);
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                transList.Add(new TransItem()
                {
                    TransNo= dt.Rows[i]["TransNo"].ToString().Trim(),
                    TransName = dt.Rows[i]["TransName"].ToString().Trim()
                });
            }
            return transList;
        }

        /// <summary>
        /// 取得作業區域清單
        /// </summary>
        /// <returns></returns>
        List<DataTypeItem> GetDataType()
        {
            List<DataTypeItem> dataTypeList = new List<DataTypeItem>();
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from [jetf].[dbo].[PdtDataType] order by Sort", conn))
            {
                da.Fill(dt);
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dataTypeList.Add(new DataTypeItem()
                {
                    DataType = dt.Rows[i]["DataType"].ToString().Trim()
                });
            }
            return dataTypeList;
        }

       public DataTable GetPdtVersion() {
            DataTable dt_PdtVersion = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from jetf.dbo.[PdtVersion] ", conn))
            {
                da.Fill(dt_PdtVersion);
            }
            return dt_PdtVersion;
        }

        public bool CheckToken(string api, string body, string token)
        {
#if DEBUG
            return true;
#endif

            bool result = false;
            string check = GetToken(api, body);
            if (check == token.ToUpper())
            {
                result = true;
            }
            return result;
        }

        public string GetToken(string api, string body)
        {
            string key = "edqcZ5QGkK9VwNv6b5WNcdPfSHyZfnMs";
            string token = ToMD5($"{key}{api}{body}");
            return token;
        }

        public string ToMD5(string str)
        {
            using (var cryptoMD5 = System.Security.Cryptography.MD5.Create())
            {
                //將字串編碼成 UTF8 位元組陣列
                var bytes = Encoding.UTF8.GetBytes(str);

                //取得雜湊值位元組陣列
                var hash = cryptoMD5.ComputeHash(bytes);

                //取得 MD5
                var md5 = BitConverter.ToString(hash)
                  .Replace("-", String.Empty)
                  .ToUpper();
                return md5;
            }
        }
    }
}