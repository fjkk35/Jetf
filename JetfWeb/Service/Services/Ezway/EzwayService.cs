using Service.Services.Ezway.Domain;
using System.Collections.Generic;
using System.Net.Http;

namespace Service.Services.Ezway
{
    /// <summary>
    /// Ezway 空運頁面使用的服務 facade。
    /// </summary>
    public class EzwayService : EzwayApiService
    {
        /// <summary>
        /// 建立 Ezway 空運服務實例。
        /// </summary>
        public EzwayService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 建立空運單筆查詢 payload。
        /// </summary>
        protected override object BuildSingleQueryPayload(string hawbNumber, EzwayQueryRequest request)
        {
            return IsX4QueryApi(request)
                ? BuildX4SingleQueryPayload(hawbNumber)
                : BuildSimpleSingleQueryPayload(hawbNumber);
        }

        /// <summary>
        /// 建立空運整批查詢 multipart/form-data 內容。
        /// </summary>
        protected override MultipartFormDataContent CreateBatchMultipartContent(byte[] fileBytes, int batchNumber, EzwayQueryRequest request)
        {
            return IsX4QueryApi(request)
                ? CreateX4BatchMultipartContent(fileBytes, batchNumber)
                : CreateSimpleBatchMultipartContent(fileBytes, batchNumber);
        }

        /// <summary>
        /// 建立「預先委任確認查詢(X4)」單筆查詢 payload。
        /// </summary>
        private Dictionary<string, object> BuildX4SingleQueryPayload(string hawbNumber)
        {
            return new Dictionary<string, object>
            {
                { "authorizeStatus", "A" },
                { "brokerBan", GetStoredBrokerBan() },
                { "declType", "G1" },
                { "hawbNo", hawbNumber },
                { "lang", "TW" },
                { "manual", "Y" },
                { "status", "A" },
                { "userId", GetStoredUserId() }
            };
        }

        /// <summary>
        /// 建立「預先委任確認查詢(簡易)」單筆查詢 payload。
        /// </summary>
        private Dictionary<string, object> BuildSimpleSingleQueryPayload(string hawbNumber)
        {
            return new Dictionary<string, object>
            {
                { "authorizeStatus", "A" },
                { "brokerBan", GetStoredBrokerBan() },
                { "declType", "TX" },
                { "groupUserId", "全部" },
                { "hawbNo", hawbNumber },
                { "lang", "TW" },
                { "manual", "Y" },
                { "status", "A" },
                { "userId", GetStoredUserId() }
            };
        }

        /// <summary>
        /// 建立「預先委任確認查詢(X4)」整批查詢 multipart/form-data 內容。
        /// </summary>
        private MultipartFormDataContent CreateX4BatchMultipartContent(byte[] fileBytes, int batchNumber)
        {
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent("N"), "manual");
            multipartContent.Add(CreateBatchFileContent(fileBytes), "file", $"EzwayBatch_{batchNumber:000}.xlsx");
            multipartContent.Add(new StringContent(GetStoredUserId()), "userId");
            multipartContent.Add(new StringContent("G1"), "declType");
            multipartContent.Add(new StringContent(GetStoredBrokerBan()), "brokerBan");
            multipartContent.Add(new StringContent("A"), "status");
            multipartContent.Add(new StringContent("TW"), "lang");
            multipartContent.Add(new StringContent("A"), "authorizeStatus");
            return multipartContent;
        }

        /// <summary>
        /// 建立「預先委任確認查詢(簡易)」整批查詢 multipart/form-data 內容。
        /// </summary>
        private MultipartFormDataContent CreateSimpleBatchMultipartContent(byte[] fileBytes, int batchNumber)
        {
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent("N"), "manual");
            multipartContent.Add(CreateBatchFileContent(fileBytes), "file", $"EzwayBatch_{batchNumber:000}.xlsx");
            multipartContent.Add(new StringContent(GetStoredUserId()), "userId");
            multipartContent.Add(new StringContent("TX"), "declType");
            multipartContent.Add(new StringContent(GetStoredBrokerBan()), "brokerBan");
            multipartContent.Add(new StringContent("A"), "status");
            multipartContent.Add(new StringContent("A"), "authorizeStatus");
            multipartContent.Add(new StringContent("TW"), "lang");
            multipartContent.Add(new StringContent("全部"), "groupUserId");
            return multipartContent;
        }
    }
}
