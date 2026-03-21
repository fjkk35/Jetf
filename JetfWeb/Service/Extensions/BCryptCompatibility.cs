using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Security.Cryptography;

namespace BCrypt.Net
{
    public static class BCrypt
    {
        /// <summary>
        /// 依 bcrypt 規則產生雜湊密碼。
        /// </summary>
        /// <param name="inputKey">明文密碼。</param>
        /// <returns>bcrypt 雜湊結果。</returns>
        public static string HashPassword(string inputKey)
        {
            return HashPassword(inputKey, 12);
        }

        /// <summary>
        /// 依指定成本值產生 bcrypt 雜湊密碼。
        /// </summary>
        /// <param name="inputKey">明文密碼。</param>
        /// <param name="workFactor">bcrypt 成本值。</param>
        /// <returns>bcrypt 雜湊結果。</returns>
        public static string HashPassword(string inputKey, int workFactor)
        {
            if (string.IsNullOrWhiteSpace(inputKey))
            {
                throw new ArgumentException("密碼不可為空白", nameof(inputKey));
            }

            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return OpenBsdBCrypt.Generate("2a", inputKey.ToCharArray(), salt, workFactor);
        }
    }
}