using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Service.Services.ShipmentInboundRecord
{
    /// <summary>
    /// 建立 HCT 貨號查詢網址的服務。
    /// </summary>
    public sealed class HctTrackingQueryService
    {
        private const string QueryUrl = "https://hctapiweb.hct.com.tw/phone/searchGoods_Main.aspx";

        // 請以 HCT 正式文件值取代下列三個佔位值，不要填入測試範例參數。
        private const string QueryIv = "PWNKUJHR";
        private const string QueryValue = "6EFA0898993F4CF9CC1E0CE73D7A241B";
        private const int QueryKeyOffsetDays = 56;

        /// <summary>
        /// 建立 HCT 貨號查詢網址。
        /// </summary>
        /// <param name="trackingNo">貨號。</param>
        /// <returns>包含加密貨號與 v 參數的 HCT 查詢網址。</returns>
        public string BuildQueryUrl(string trackingNo)
        {
            if (string.IsNullOrWhiteSpace(trackingNo))
            {
                throw new ArgumentException("貨號不可為空白。", nameof(trackingNo));
            }

            var iv = QueryIv;
            var vValue = QueryValue;
            var offsetDays = QueryKeyOffsetDays;
            var key = DateTime.Today
                .AddDays(offsetDays)
                .ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            var encryptedTrackingNo = EncryptTrackingNo(trackingNo.Trim(), key, iv);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}?no={1}&v={2}",
                QueryUrl.TrimEnd('?'),
                Uri.EscapeDataString(encryptedTrackingNo),
                Uri.EscapeDataString(vValue));
        }

        private static string EncryptTrackingNo(string trackingNo, string key, string iv)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var ivBytes = Encoding.UTF8.GetBytes(iv);

            var inputByteArray = Encoding.UTF8.GetBytes(trackingNo);
            using (var des = new DESCryptoServiceProvider())
            using (var mStream = new MemoryStream())
            using (var cStream = new CryptoStream(
                mStream,
                des.CreateEncryptor(keyBytes, ivBytes),
                CryptoStreamMode.Write))
            {
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
        }

    }
}
