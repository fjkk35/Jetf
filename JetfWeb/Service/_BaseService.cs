using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Service.Data;
using Service.Services;

namespace Service
{
    public class _BaseService
    {
        private readonly string key= "JETFJETFJETFJETFJETFJETFJETFJETF";
        public SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public _BaseService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 取得當前使用者ID
        /// </summary>
        /// <param name="defaultValue">預設值，當無法取得使用者ID時使用</param>
        /// <returns>使用者ID</returns>
        protected string GetUserId()
        {
            return UserContextService.GetUserId();
        }

        protected JetfDbContext CreateJetfDbContext()
        {
            return new JetfDbContext();
        }

        protected DataCenterDbContext CreateDataCenterDbContext()
        {
            return new DataCenterDbContext();
        }

        protected Dictionary<string, string> GetAirCustomerNames(IEnumerable<string> custCodes)
        {
            var codes = (custCodes ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (!codes.Any())
            {
                return new Dictionary<string, string>();
            }

            using (var db = CreateDataCenterDbContext())
            {
                return db.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == "AIR" && !string.IsNullOrEmpty(x.OldCode) && codes.Contains(x.OldCode))
                    .GroupBy(x => x.OldCode)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.CustName).FirstOrDefault() ?? string.Empty);
            }
        }

        protected Dictionary<string, string> GetSeaCustomerNames(IEnumerable<string> custCodes)
        {
            var codes = (custCodes ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (!codes.Any())
            {
                return new Dictionary<string, string>();
            }

            using (var db = CreateDataCenterDbContext())
            {
                return db.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == "SEA" && codes.Contains(x.CustCode))
                    .GroupBy(x => x.CustCode)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.CustName).FirstOrDefault() ?? string.Empty);
            }
        }

        protected Dictionary<string, string> GetAirTransNames(IEnumerable<string> transNos)
        {
            var codes = (transNos ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (!codes.Any())
            {
                return new Dictionary<string, string>();
            }

            using (var db = CreateJetfDbContext())
            {
                return db.CustomerMasters
                    .AsNoTracking()
                    .Where(x => x.TranType == "空運" && codes.Contains(x.TransNo))
                    .GroupBy(x => x.TransNo)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.TransName).FirstOrDefault() ?? string.Empty);
            }
        }


        /// <summary>
        ///  AES 加密
        /// </summary>
        /// <param name="str">明文（待加密）</param>
        /// <param name="key">密文</param>
        /// <returns></returns>
        public string AesEncrypt(string str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray);
        }

        /// <summary>
        ///  AES 解密
        /// </summary>
        /// <param name="str">明文（待解密）</param>
        /// <param name="key">密文</param>
        /// <returns></returns>
        public string AesDecrypt(string str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] toEncryptArray = Convert.FromBase64String(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
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
