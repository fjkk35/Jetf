using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundCommon
{
    /// <summary>
    /// 貨件回倉異常圖片讀取服務。
    /// </summary>
    public class ShipmentInboundExceptionImageStorageService
    {
        private const string DefaultSftpUserName = "USER_L1";
        private const string DefaultSftpPassword = "q'c2T^ZV";

        /// <summary>
        /// 讀取指定圖片路徑的檔案內容。
        /// </summary>
        /// <param name="filePath">資料庫儲存的 SFTP 圖片路徑。</param>
        /// <returns>檔案位元組內容；找不到或讀取失敗時回傳 null。</returns>
        public byte[] ReadAllBytes(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            if (!IsSftpPath(filePath))
            {
                return null;
            }

            return ReadSftpBytes(filePath);
        }

        public Dictionary<string, byte[]> ReadAllBytes(IEnumerable<string> filePaths)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            ReadAllBytes(filePaths, (filePath, fileBytes) => result[filePath] = fileBytes);
            return result;
        }

        public void ReadAllBytes(IEnumerable<string> filePaths, Action<string, byte[]> onFileRead)
        {
            if (filePaths == null)
            {
                return;
            }

            var pathInfos = filePaths
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x =>
                {
                    if (!Uri.TryCreate(x, UriKind.Absolute, out var uri)
                        || !string.Equals(uri.Scheme, "sftp", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    return new
                    {
                        FilePath = x,
                        Uri = uri,
                        Port = uri.Port > 0 ? uri.Port : 22,
                        RemotePath = Uri.UnescapeDataString(uri.AbsolutePath)
                    };
                })
                .Where(x => x != null)
                .ToList();

            foreach (var group in pathInfos.GroupBy(x => new { x.Uri.Host, x.Port }))
            {
                try
                {
                    using (var client = new SftpClient(group.Key.Host, group.Key.Port, DefaultSftpUserName, DefaultSftpPassword))
                    {
                        client.Connect();

                        foreach (var item in group)
                        {
                            try
                            {
                                if (!client.Exists(item.RemotePath))
                                {
                                    continue;
                                }

                                using (var stream = new MemoryStream())
                                {
                                    client.DownloadFile(item.RemotePath, stream);
                                    var fileBytes = stream.ToArray();
                                    if (fileBytes.Length > 0)
                                    {
                                        onFileRead?.Invoke(item.FilePath, fileBytes);
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        client.Disconnect();
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 取得圖片副檔名。
        /// </summary>
        /// <param name="filePath">資料庫儲存的 SFTP 圖片路徑。</param>
        /// <returns>副檔名；取不到時回傳空字串。</returns>
        public string GetExtension(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !IsSftpPath(filePath))
            {
                return string.Empty;
            }

            var uri = new Uri(filePath);
            return Path.GetExtension(Uri.UnescapeDataString(uri.AbsolutePath));
        }

        private bool IsSftpPath(string filePath)
        {
            return Uri.TryCreate(filePath, UriKind.Absolute, out var uri)
                && string.Equals(uri.Scheme, "sftp", StringComparison.OrdinalIgnoreCase);
        }

        private byte[] ReadSftpBytes(string filePath)
        {
            try
            {
                var uri = new Uri(filePath);
                var port = uri.Port > 0 ? uri.Port : 22;
                var remotePath = Uri.UnescapeDataString(uri.AbsolutePath);

                using (var client = new SftpClient(uri.Host, port, DefaultSftpUserName, DefaultSftpPassword))
                {
                    client.Connect();

                    if (!client.Exists(remotePath))
                    {
                        return null;
                    }

                    using (var stream = new MemoryStream())
                    {
                        client.DownloadFile(remotePath, stream);
                        client.Disconnect();
                        return stream.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
