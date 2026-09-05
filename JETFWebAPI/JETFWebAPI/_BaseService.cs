using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace JETFWebAPI
{
    public class _BaseService
    {
        public SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public _BaseService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        public bool CheckToken(string api, string body, string token)
        {
            //測試不驗證token
            //return true;

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
            string key = "bVAW8U9Pci9kNCu68qC1IEQGCtz3DRfO";
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