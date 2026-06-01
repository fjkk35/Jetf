using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Services.Ezway.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Service.Services.Ezway
{
    /// <summary>
    /// Ezway 海運頁面使用的服務 facade。
    /// </summary>
    public class EzwaySeaService : EzwayApiService
    {
        /// <summary>
        /// 建立 Ezway 海運服務實例。
        /// </summary>
        public EzwaySeaService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 建立海運單筆查詢 payload。
        /// </summary>
        protected override object BuildSingleQueryPayload(string hawbNumber, EzwayQueryRequest request)
        {
            var payload = new Dictionary<string, object>
            {
                { "authorizeStatus", "A" },
                { "brokerBan", GetStoredBrokerBan() },
                { "declType", "TX" },
                { "hawbNo", hawbNumber },
                { "lang", "TW" },
                { "manual", "Y" },
                { "status", "A" },
                { "userId", GetStoredUserId() }
            };

            AddSeaQueryFields(payload, request);
            return payload;
        }

        /// <summary>
        /// 建立海運整批查詢 multipart/form-data 內容。
        /// </summary>
        protected override MultipartFormDataContent CreateBatchMultipartContent(byte[] fileBytes, int batchNumber, EzwayQueryRequest request)
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

            AddSeaQueryFields(multipartContent, request);
            return multipartContent;
        }

        /// <summary>
        /// 建立海運查詢結果匯出活頁簿。
        /// </summary>
        protected override XSSFWorkbook CreateExportWorkbook(List<EzwayQueryResult> exportResults)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Ezway查詢結果");

            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 11, true);
            ICellStyle textStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);
            ICellStyle centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);

            bool includeConsolidatorName = exportResults.Any(item => !string.IsNullOrWhiteSpace(item?.ConsolidatorName));
            bool includeGroupBrokerUser = exportResults.Any(item => !string.IsNullOrWhiteSpace(item?.GroupBrokerUser));
            var headers = new List<string>();

            if (includeConsolidatorName)
            {
                headers.Add("集運商");
            }

            if (includeGroupBrokerUser)
            {
                headers.Add("群組報關業者");
            }

            headers.AddRange(new[]
            {
                "報關業者",
                "預報關日期",
                "報單號碼",
                "主提單號碼",
                "分提單號碼",
                "電話號碼",
                "證件號碼",
                "推播狀態",
                "實名委任日期",
                "認證結果",
                "核准文號",
                "海關回覆結果",
                "海關回覆日期",
                "申報金額",
                "阻擋原因"
            });

            var headerRow = sheet.CreateRow(0);
            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                NpoiCell.CreateCell(headerRow, columnIndex, headers[columnIndex], headerStyle);
            }

            for (int index = 0; index < exportResults.Count; index++)
            {
                EzwayQueryResult item = exportResults[index];
                var row = sheet.CreateRow(index + 1);
                int columnIndex = 0;

                if (includeConsolidatorName)
                {
                    NpoiCell.CreateCell(row, columnIndex++, item.ConsolidatorName ?? string.Empty, textStyle);
                }

                if (includeGroupBrokerUser)
                {
                    NpoiCell.CreateCell(row, columnIndex++, item.GroupBrokerUser ?? string.Empty, textStyle);
                }

                NpoiCell.CreateCell(row, columnIndex++, item.BrokerUser ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.ImportDate ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.DeclNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.MawbNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.HawbNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.TelNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.IdNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.NotificationFlag ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, columnIndex++, BuildReplyDateTime(item), centerStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.IsReply ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.AuthorizeDocNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.AuthorizeReply ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.AuthorizeDatm ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, columnIndex++, item.TotCustomsValueAmt ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, columnIndex, item.BlockReason ?? string.Empty, textStyle);
            }

            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                sheet.AutoSizeColumn(columnIndex);
                if (sheet.GetColumnWidth(columnIndex) < 4000)
                {
                    sheet.SetColumnWidth(columnIndex, 4000);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 加入海運單筆查詢欄位。
        /// </summary>
        private void AddSeaQueryFields(Dictionary<string, object> payload, EzwayQueryRequest request)
        {
            string groupUserId = ResolveGroupUserId(request);
            if (!string.IsNullOrWhiteSpace(groupUserId))
            {
                payload["groupUserId"] = groupUserId;
            }

            string brokerUserId = ResolveBrokerUserId(request);
            if (!string.IsNullOrWhiteSpace(brokerUserId))
            {
                payload["brokerUserId"] = brokerUserId;
            }

            string consolidator = ResolveConsolidator(request);
            if (!string.IsNullOrWhiteSpace(consolidator))
            {
                payload["consolidator"] = consolidator;
            }

            payload["consolidatorUserId"] = ResolveConsolidatorUserId(request, consolidator);
        }

        /// <summary>
        /// 加入海運整批查詢欄位。
        /// </summary>
        private void AddSeaQueryFields(MultipartFormDataContent multipartContent, EzwayQueryRequest request)
        {
            string groupUserId = ResolveGroupUserId(request);
            if (!string.IsNullOrWhiteSpace(groupUserId))
            {
                multipartContent.Add(new StringContent(groupUserId), "groupUserId");
            }

            string brokerUserId = ResolveBrokerUserId(request);
            if (!string.IsNullOrWhiteSpace(brokerUserId))
            {
                multipartContent.Add(new StringContent(brokerUserId), "brokerUserId");
            }

            string consolidator = ResolveConsolidator(request);
            if (!string.IsNullOrWhiteSpace(consolidator))
            {
                multipartContent.Add(new StringContent(consolidator), "consolidator");
            }

            string consolidatorUserId = ResolveConsolidatorUserId(request, consolidator);
            if (consolidatorUserId != null)
            {
                multipartContent.Add(new StringContent(consolidatorUserId), "consolidatorUserId");
            }
        }

        /// <summary>
        /// 依查詢條件回傳海運簡易查詢用的 groupUserId。
        /// </summary>
        private string ResolveGroupUserId(EzwayQueryRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request?.GroupUserId))
            {
                return request.GroupUserId.Trim();
            }

            string storedUserId = GetStoredUserId();
            return !string.IsNullOrWhiteSpace(storedUserId)
                && storedUserId.Trim().StartsWith("CUSTOMER", StringComparison.OrdinalIgnoreCase)
                ? "全部"
                : string.Empty;
        }

        /// <summary>
        /// 依查詢條件回傳海運簡易查詢用的 brokerUserId。
        /// </summary>
        private static string ResolveBrokerUserId(EzwayQueryRequest request)
        {
            return request?.BrokerUserId?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 依查詢條件回傳海運簡易查詢用的 consolidator。
        /// </summary>
        private static string ResolveConsolidator(EzwayQueryRequest request)
        {
            return !string.IsNullOrWhiteSpace(request?.Consolidator)
                ? request.Consolidator.Trim()
                : "全部_A";
        }

        /// <summary>
        /// 依查詢條件回傳海運簡易查詢用的 consolidatorUserId。
        /// </summary>
        private static string ResolveConsolidatorUserId(EzwayQueryRequest request, string consolidator)
        {
            if (request != null && !string.IsNullOrWhiteSpace(request.Consolidator))
            {
                if (!string.IsNullOrWhiteSpace(request.ConsolidatorUserId))
                {
                    return request.ConsolidatorUserId.Trim();
                }

                return string.Equals(request.Consolidator.Trim(), "null", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : "ALL";
            }

            if (string.IsNullOrWhiteSpace(consolidator)
                || string.Equals(consolidator, "null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return "ALL";
        }
    }
}
