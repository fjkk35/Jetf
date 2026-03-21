using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary.Model;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TelegramLibrary
{
    public class TelegramBot
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        };

        private static readonly HttpClient httpClient = CreateHttpClient();
        private readonly SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);

        private readonly string botToken = "7833563595:AAEz_vvgY8l69AhiP8Wh3DMnnfz0MVIeOqg";
        //var chatId = "-1002741936670";

        static TelegramBot()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = Math.Max(ServicePointManager.DefaultConnectionLimit, 20);
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                Timeout = RequestTimeout
            };
        }

        public async Task<TelegramResponse> SendTextMessageAsync(string chatId, string message)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return CreateErrorResponse("chatId 不可為空。");
            }

            string url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(message)}";

            return await ExecuteWithRetryAsync(async () =>
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    string result = await response.Content.ReadAsStringAsync();
                    EnsureTransientStatus(response, result);
                    return DeserializeResponse(result, response);
                }
            }, swallowException: true);
        }

        public async Task<TelegramResponse> SendPhotoAsync(string chatId, string caption, string filePath)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return CreateErrorResponse("chatId 不可為空。");
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return CreateErrorResponse($"找不到圖片檔案：{filePath}");
            }

            string url = $"https://api.telegram.org/bot{botToken}/sendPhoto";

            return await ExecuteWithRetryAsync(async () =>
            {
                using (var form = new MultipartFormDataContent())
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var fileContent = new StreamContent(fileStream))
                {
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(filePath));

                    form.Add(fileContent, "photo", Path.GetFileName(filePath));
                    form.Add(new StringContent(chatId), "chat_id");
                    form.Add(new StringContent(caption ?? string.Empty), "caption");

                    using (HttpResponseMessage response = await httpClient.PostAsync(url, form))
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        EnsureTransientStatus(response, result);
                        return DeserializeResponse(result, response);
                    }
                }
            });
        }

        public async Task<TelegramResponse> SendDocumentAsync(string chatId, string caption, string filePath)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return CreateErrorResponse("chatId 不可為空。");
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return CreateErrorResponse($"找不到文件檔案：{filePath}");
            }

            string url = $"https://api.telegram.org/bot{botToken}/sendDocument";

            return await ExecuteWithRetryAsync(async () =>
            {
                using (var form = new MultipartFormDataContent())
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var fileContent = new StreamContent(fileStream))
                {
                    var fileExtension = Path.GetExtension(filePath);

                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    form.Add(fileContent, "document", $"{DateTime.Now:yyyyMMddHHmmss}{fileExtension}");
                    form.Add(new StringContent(chatId), "chat_id");
                    form.Add(new StringContent(caption ?? string.Empty), "caption");

                    using (HttpResponseMessage response = await httpClient.PostAsync(url, form))
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        EnsureTransientStatus(response, result);
                        return DeserializeResponse(result, response);
                    }
                }
            });
        }

        private static async Task<TelegramResponse> ExecuteWithRetryAsync(Func<Task<TelegramResponse>> sendAsync, bool swallowException = false)
        {
            Exception lastException = null;

            for (int attempt = 0; attempt <= RetryDelays.Length; attempt++)
            {
                try
                {
                    return await sendAsync();
                }
                catch (Exception ex) when (IsTransientException(ex) && attempt < RetryDelays.Length)
                {
                    lastException = ex;
                    await Task.Delay(RetryDelays[attempt]);
                }
                catch (Exception ex)
                {
                    if (swallowException)
                    {
                        return CreateErrorResponse(ex);
                    }

                    throw CreateSendFailedException(ex);
                }
            }

            if (swallowException)
            {
                return CreateErrorResponse(lastException);
            }

            throw CreateSendFailedException(lastException);
        }

        private static bool IsTransientException(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            if (ex is TaskCanceledException)
            {
                return true;
            }

            if (ex is HttpRequestException)
            {
                return true;
            }

            if (ex is WebException webException)
            {
                return webException.Status == WebExceptionStatus.Timeout
                    || webException.Status == WebExceptionStatus.ConnectFailure
                    || webException.Status == WebExceptionStatus.ConnectionClosed
                    || webException.Status == WebExceptionStatus.NameResolutionFailure
                    || webException.Status == WebExceptionStatus.ProxyNameResolutionFailure
                    || webException.Status == WebExceptionStatus.ReceiveFailure
                    || webException.Status == WebExceptionStatus.SendFailure;
            }

            if (ex is SocketException)
            {
                return true;
            }

            return IsTransientException(ex.InnerException);
        }

        private static void EnsureTransientStatus(HttpResponseMessage response, string responseBody)
        {
            int statusCode = (int)response.StatusCode;
            if (statusCode == 408 || statusCode == 429 || statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504)
            {
                throw new HttpRequestException($"Telegram API 暫時性錯誤：{statusCode} {response.ReasonPhrase}。{responseBody}");
            }
        }

        private static TelegramResponse DeserializeResponse(string result, HttpResponseMessage response)
        {
            TelegramResponse telegramResponse = null;

            if (!string.IsNullOrWhiteSpace(result))
            {
                telegramResponse = JsonConvert.DeserializeObject<TelegramResponse>(result);
            }

            if (telegramResponse != null)
            {
                if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(telegramResponse.Description))
                {
                    telegramResponse.Description = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                }

                return telegramResponse;
            }

            return new TelegramResponse
            {
                Ok = response.IsSuccessStatusCode,
                Error_code = (int)response.StatusCode,
                Description = string.IsNullOrWhiteSpace(result)
                    ? $"Telegram API 無回應內容，HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                    : result
            };
        }

        private static Exception CreateSendFailedException(Exception ex)
        {
            return new HttpRequestException($"Telegram API 連線失敗，已重試 {RetryDelays.Length + 1} 次。{ex?.Message}", ex);
        }

        private static TelegramResponse CreateErrorResponse(Exception ex)
        {
            return CreateErrorResponse(ex?.Message ?? "未知錯誤。");
        }

        private static TelegramResponse CreateErrorResponse(string description)
        {
            return new TelegramResponse
            {
                Ok = false,
                Error_code = 0,
                Description = description
            };
        }

        private static string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLowerInvariant();

            switch (extension)
            {
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".webp":
                    return "image/webp";
                default:
                    return "image/jpeg";
            }
        }


        public string GetChatId(string groupId)
        {
            string chatId = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from jetf.[dbo].[TelegramGroup] where GroupId=@GroupId", conn))
            {
                da.SelectCommand.Parameters.Add("@GroupId", SqlDbType.NVarChar).Value = groupId;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                chatId = dt.Rows[0]["ChatId"].ToString();
            }
            return chatId;
        }


    }
}
