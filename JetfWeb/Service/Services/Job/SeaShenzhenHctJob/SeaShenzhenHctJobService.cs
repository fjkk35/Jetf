using Newtonsoft.Json;
using Service.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Service.Services.Job.SeaShenzhenHctJob
{
    /// <summary>
    /// 將新遞深圳託運資料傳送至 HCT EDI WebService。
    /// </summary>
    public class SeaShenzhenHctJobService : _BaseService
    {
        private const string JobName = "新遞深圳 HCT 託運傳送";
        private const string DefaultApiUrl = "https://hctrt.hct.com.tw/edi_webservice2/service1.asmx";
        private const string DefaultSoapAction = "http://tempuri.org/TransData_Json";
        private const string DefaultCompany = "test";
        private const string DefaultPassword = "test1";
        private const string DefaultCustomerCode = "04974971408";
        private static readonly XNamespace SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace ServiceNamespace = "http://tempuri.org/";

        public SeaShenzhenHctJobService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 執行待送新遞深圳 HCT 託運資料。
        /// </summary>
        public async Task RunSeaShenzhenHctJobAsync()
        {
            try
            {
                var pendingItems = JetfDb.SeaShenzhenOriginals
                    .Where(x => x.IsHct && !x.IsHctSuccess)
                    .OrderBy(x => x.Id)
                    .ToList();

                foreach (var original in pendingItems)
                {
                    await ProcessOriginalAsync(original);
                }
            }
            catch (Exception ex)
            {
                WriteJobErrorLog(JobName, ex);
            }
        }

        private async Task ProcessOriginalAsync(SeaShenzhenOriginalEntity original)
        {
            if (original == null)
            {
                return;
            }

            var requestItem = BuildRequestItem(original);
            var requestJson = JsonConvert.SerializeObject(new[] { requestItem });
            var validationMessage = ValidateRequestItem(requestItem);
            var successCode = "N";
            string errMsg = validationMessage;
            string responseJson = null;

            if (string.IsNullOrWhiteSpace(validationMessage))
            {
                var apiResult = await SendToHctAsync(requestJson);
                responseJson = !string.IsNullOrWhiteSpace(apiResult.ResponseJson)
                    ? apiResult.ResponseJson
                    : apiResult.RawResponse;

                if (!apiResult.IsSuccess)
                {
                    errMsg = apiResult.ErrorMessage;
                }
                else
                {
                    var businessResult = ParseBusinessResult(apiResult.ResponseJson);
                    successCode = businessResult.Success;
                    errMsg = businessResult.ErrMsg;

                    if (IsBusinessSuccess(successCode))
                    {
                        original.IsHctSuccess = true;
                        original.ModifiedUser = GetJobUserId();
                        original.ModifiedTime = DateTime.Now;
                    }
                }
            }

            JetfDb.SeaShenzhenHctSendLogs.Add(new SeaShenzhenHctSendLogEntity
            {
                SeaShenzhenOriginalId = original.Id,
                JetfSerial = original.JetfSerial,
                Success = NormalizeSuccess(successCode),
                ErrMsg = errMsg,
                RequestJson = requestJson,
                ResponseJson = responseJson,
                CreatedTime = DateTime.Now
            });

            await JetfDb.SaveChangesAsync();
        }

        private static HctRequestItem BuildRequestItem(SeaShenzhenOriginalEntity original)
        {
            return new HctRequestItem
            {
                Epino = FirstNotEmpty(original.TrackingNo, original.OrderNo, original.JetfSerial),
                Ercsig = NormalizeText(original.Importer),
                Ertel1 = NormalizeText(original.ImporterPhone),
                Eraddr = NormalizeText(original.ImporterAddress),
                Ejamt = "1",
                Eqamt = original.DlvGw.ToString("0.###", CultureInfo.InvariantCulture),
                Escsno = GetSetting("HctEdi.CustomerCode", DefaultCustomerCode),
                Edelno = NormalizeText(original.JetfSerial),
                Eqmny = original.Cc.HasValue
                    ? original.Cc.Value.ToString("0.##", CultureInfo.InvariantCulture)
                    : "0"
            };
        }

        private static string ValidateRequestItem(HctRequestItem item)
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(item.Epino))
            {
                missingFields.Add("epino");
            }

            if (string.IsNullOrWhiteSpace(item.Ercsig))
            {
                missingFields.Add("ercsig");
            }

            if (string.IsNullOrWhiteSpace(item.Ertel1))
            {
                missingFields.Add("ertel1");
            }

            if (string.IsNullOrWhiteSpace(item.Eraddr))
            {
                missingFields.Add("eraddr");
            }

            if (string.IsNullOrWhiteSpace(item.Eqamt))
            {
                missingFields.Add("eqamt");
            }

            if (string.IsNullOrWhiteSpace(item.Edelno))
            {
                missingFields.Add("edelno");
            }

            return missingFields.Count == 0
                ? null
                : "必要欄位不足：" + string.Join("、", missingFields);
        }

        private async Task<HctApiCallResult> SendToHctAsync(string requestJson)
        {
            var endpoint = GetSetting("HctEdi.ApiUrl", DefaultApiUrl);
            var company = GetSetting("HctEdi.Company", DefaultCompany);
            var password = GetSetting("HctEdi.Password", DefaultPassword);
            var soapEnvelope = BuildSoapEnvelope(company, password, requestJson);

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);

                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                using (var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml"))
                {
                    request.Content = content;
                    request.Headers.Add("SOAPAction", "\"" + DefaultSoapAction + "\"");

                    using (var response = await client.SendAsync(request))
                    {
                        var rawResponse = await response.Content.ReadAsStringAsync();
                        var parsedResponseJson = ExtractResponseJson(rawResponse);

                        if (!response.IsSuccessStatusCode)
                        {
                            return new HctApiCallResult
                            {
                                IsSuccess = false,
                                RawResponse = rawResponse,
                                ResponseJson = parsedResponseJson,
                                ErrorMessage = string.Format(
                                    CultureInfo.InvariantCulture,
                                    "HCT API HTTP {0} {1}",
                                    (int)response.StatusCode,
                                    response.ReasonPhrase)
                            };
                        }

                        if (string.IsNullOrWhiteSpace(parsedResponseJson))
                        {
                            return new HctApiCallResult
                            {
                                IsSuccess = false,
                                RawResponse = rawResponse,
                                ErrorMessage = "HCT API 回傳內容無法解析"
                            };
                        }

                        return new HctApiCallResult
                        {
                            IsSuccess = true,
                            RawResponse = rawResponse,
                            ResponseJson = parsedResponseJson
                        };
                    }
                }
            }
        }

        private static HctBusinessResult ParseBusinessResult(string responseJson)
        {
            try
            {
                var items = JsonConvert.DeserializeObject<List<HctResponseItem>>(responseJson) ?? new List<HctResponseItem>();
                if (items.Count == 0)
                {
                    return new HctBusinessResult
                    {
                        Success = "N",
                        ErrMsg = "HCT API 未回傳任何結果"
                    };
                }

                var firstItem = items[0];
                var successCode = NormalizeSuccess(firstItem.Success);
                var errorMessage = NormalizeText(firstItem.ErrMsg);

                if (!IsBusinessSuccess(successCode) && string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = "HCT API 回傳失敗";
                }

                return new HctBusinessResult
                {
                    Success = successCode,
                    ErrMsg = errorMessage
                };
            }
            catch (JsonException ex)
            {
                return new HctBusinessResult
                {
                    Success = "N",
                    ErrMsg = "HCT API 回應 JSON 解析失敗：" + ex.Message
                };
            }
        }

        private static string ExtractResponseJson(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return null;
            }

            try
            {
                var document = XDocument.Parse(rawResponse);
                var resultNode = document.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "TransData_JsonResult");

                return resultNode == null
                    ? null
                    : NormalizeText(resultNode.Value);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildSoapEnvelope(string company, string password, string requestJson)
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(SoapNamespace + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                    new XElement(SoapNamespace + "Body",
                        new XElement(ServiceNamespace + "TransData_Json",
                            new XElement(ServiceNamespace + "company", company),
                            new XElement(ServiceNamespace + "password", password),
                            new XElement(ServiceNamespace + "json", requestJson)))));

            var declaration = document.Declaration == null
                ? string.Empty
                : document.Declaration + Environment.NewLine;

            return declaration + document.ToString(SaveOptions.DisableFormatting);
        }

        private static bool IsBusinessSuccess(string successCode)
        {
            return string.Equals(successCode, "Y", StringComparison.OrdinalIgnoreCase)
                || string.Equals(successCode, "R", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSuccess(string successCode)
        {
            var value = NormalizeText(successCode);
            if (string.IsNullOrWhiteSpace(value))
            {
                return "N";
            }

            return value.Substring(0, 1).ToUpperInvariant();
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string FirstNotEmpty(params string[] values)
        {
            return values
                .Select(NormalizeText)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        private static string GetSetting(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value.Trim();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        private string GetJobUserId()
        {
            var userId = GetUserId();
            return string.IsNullOrWhiteSpace(userId)
                ? "SYSTEM"
                : userId.Trim();
        }

        private sealed class HctApiCallResult
        {
            public bool IsSuccess { get; set; }

            public string RawResponse { get; set; }

            public string ResponseJson { get; set; }

            public string ErrorMessage { get; set; }
        }

        private sealed class HctBusinessResult
        {
            public string Success { get; set; }

            public string ErrMsg { get; set; }
        }

        private sealed class HctRequestItem
        {
            [JsonProperty("epino")]
            public string Epino { get; set; }

            [JsonProperty("ercsig")]
            public string Ercsig { get; set; }

            [JsonProperty("ertel1")]
            public string Ertel1 { get; set; }

            [JsonProperty("eraddr")]
            public string Eraddr { get; set; }

            [JsonProperty("ejamt")]
            public string Ejamt { get; set; }

            [JsonProperty("eqamt")]
            public string Eqamt { get; set; }

            [JsonProperty("escsno")]
            public string Escsno { get; set; }

            [JsonProperty("edelno")]
            public string Edelno { get; set; }

            [JsonProperty("eqmny")]
            public string Eqmny { get; set; }
        }

        private sealed class HctResponseItem
        {
            [JsonProperty("success")]
            public string Success { get; set; }

            [JsonProperty("ErrMsg")]
            public string ErrMsg { get; set; }
        }
    }
}
