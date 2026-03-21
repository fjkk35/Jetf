using Dapper;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.DeliveryAssistant.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Service.Services.DeliveryAssistant
{
    public class DeliveryAssistantService : _BaseService
    {
        //private const string BearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIzNTI0IiwiYXBpVXNlcklkIjoiaXRyaSIsInN5c1VzZXJJZCI6IjM1MjQiLCJzeXNDb3JwSWQiOiI2OCIsInVzZXJJZCI6Iml0cmkiLCJ1c2VyTmFtZSI6Iuezu-e1seS4suaOpeeuoeeQhiIsInN5c0dyb3VwSWQiOiI0IiwiZ3JvdXBJZCI6IjA0MCIsImNvcnBTaG9ydE5hbWUiOiLmjbfnqanpgJrnianmtYEiLCJuYmYiOjE3NTU2NzQ1NDMsImV4cCI6MjA3MTIwNzM0MywiaWF0IjoxNzU1Njc0NTQzLCJpc3MiOiJKd3RBdXRoRGVtbyJ9.cjdnYHSR4wZ8-6hiMcShFEgjSpv3jotmf_AxlQxzaB0";
        private const string BearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIzNTI0IiwiYXBpVXNlcklkIjoiaXRyaSIsInN5c1VzZXJJZCI6IjM1MjQiLCJzeXNDb3JwSWQiOiI2OCIsInVzZXJJZCI6Iml0cmkiLCJ1c2VyTmFtZSI6Iuezu-e1seS4suaOpeeuoeeQhiIsInN5c0dyb3VwSWQiOiI0IiwiZ3JvdXBJZCI6IjA0MCIsImNvcnBTaG9ydE5hbWUiOiLmjbfnqanpgJrnianmtYEiLCJuYmYiOjE3NzMxOTg0OTksImV4cCI6MjA4ODgxNzY5OSwiaWF0IjoxNzczMTk4NDk5LCJpc3MiOiJKd3RBdXRoRGVtbyJ9.FE4fnJB_jEOyShb4rOUsPVlWFJdBN5UYL5FR9Fo-xHY";
        private const string UploadApiUrl = "https://gcp.dasgo.com.tw/api/Common/Upload_OrderInfo";
        private const string EstablishDcShipApiUrl = "https://gcp.dasgo.com.tw/api/Common/Establish_DcShip";

        // 欄位索引對應（依照 Excel 表頭順序）
        private static readonly int ColDcShip = 0;             // 車次編號
        private static readonly int ColCusOrder = 1;           // 客戶單號
        private static readonly int ColCusOwnerName = 2;       // 客戶名稱
        private static readonly int ColArriveDate = 3;         // 到貨日期
        private static readonly int ColDriverName = 4;         // 駕駛
        private static readonly int ColContactPerson = 6;     // 聯絡人
        private static readonly int ColContactTel = 7;        // 連絡電話
        private static readonly int ColAccountsReceivable = 8; // 應收款
        private static readonly int ColAddr = 10;              // 住址
        private static readonly int ColCases = 24;             // 件數
        private static readonly int ColWgt = 26;               // 重量

        /// <summary>
        /// 取得作業地區清單
        /// </summary>
        public List<DataTypeModel> GetDataTypeList()
        {
            string sql = @"
                SELECT [DataType], [Sort]
                FROM [jetf].[dbo].[PdtDataType]
                ORDER BY [Sort]";

            return conn.Query<DataTypeModel>(sql).ToList();
        }

        /// <summary>
        /// 取得派件公司清單
        /// </summary>
        public List<PdtTransModel> GetTransList()
        {
            string sql = @"
                SELECT [TransNo], [TransName], [Sort]
                FROM [jetf].[dbo].[PdtTrans]
                ORDER BY [Sort]";

            return conn.Query<PdtTransModel>(sql).ToList();
        }

        /// <summary>
        /// 匯出派送助理 Excel
        /// </summary>
        public byte[] ExportExcel(DeliveryAssistantRequest request)
        {
            string sql = @"
with FilteredUpload as (
SELECT UploadTime, Data
FROM [jetf].[dbo].[PdtScanCargoUpload]
WHERE DataType = @DataType
AND TransNo = @TransNo
AND UploadTime >= @StartDate
AND UploadTime <  @EndDate
)
SELECT
a.UploadTime,
a.Data,
b.Importer,
b.ImporterPhone,
b.ImporterAddr,
b.GW,
isnull(c.TO_DLV_COD,b.Cod) as TO_DLV_COD
FROM FilteredUpload a
LEFT JOIN jetf.dbo.SjlShippingData b ON a.Data = b.JetfSerial
LEFT JOIN jetf.dbo.FEE_MASTER c ON a.Data = c.DLV_INV AND c.Download = 1";

            DateTime startDate = DateTime.Parse(request.StartDate);
            DateTime endDate = DateTime.Parse(request.EndDate).AddDays(1);

            var data = conn.Query<DeliveryAssistantExportModel>(sql, new
            {
                DataType = request.DataType,
                TransNo = request.TransNo,
                StartDate = startDate,
                EndDate = endDate
            }, commandTimeout: 300)
            .GroupBy(x => x.Data)
            .Select(g => g.OrderByDescending(x => x.GW).First())
            .ToList();

            return BuildExcel(data, request);
        }

        /// <summary>
        /// 讀取 Excel 並呼叫外部 API 上傳訂單資料
        /// </summary>
        public ResopnseModel UploadOrderInfo(string filePath)
        {
            var items = ReadExcel(filePath);

            if (items.Count == 0)
            {
                return new ResopnseModel { status = Status.error, msg = "Excel 無有效資料" };
            }

            DeliveryAssistantApiResult uploadOrderInfoResult;
            DeliveryAssistantApiResult establishDcShipResult;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", BearerToken);

                uploadOrderInfoResult = CallUploadOrderInfoApi(client, items);
                establishDcShipResult = CallEstablishDcShipApi(client, items);
            }

            bool isSuccess = uploadOrderInfoResult.success && establishDcShipResult.success;
            string msg = string.Format(
                "託運資料：{0}；車次：{1}",
                uploadOrderInfoResult.success ? "成功" : "失敗",
                establishDcShipResult.success ? "成功" : "失敗");

            return new ResopnseModel
            {
                status = isSuccess ? Status.success : Status.error,
                msg = msg,
                ReturnObject = new DeliveryAssistantUploadResult
                {
                    UploadOrderInfo = uploadOrderInfoResult,
                    EstablishDcShip = establishDcShipResult
                }
            };
        }

        private DeliveryAssistantApiResult CallUploadOrderInfoApi(HttpClient client, List<UploadOrderInfoItem> items)
        {
            try
            {
                string json = JsonConvert.SerializeObject(items);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(UploadApiUrl, content).Result;
                string responseBody = response.Content.ReadAsStringAsync().Result;

                var apiResult = JsonConvert.DeserializeObject<UploadOrderInfoResponse>(responseBody);

                if (apiResult == null)
                {
                    return new DeliveryAssistantApiResult
                    {
                        title = "託運資料",
                        success = false,
                        msg = "API 回傳格式錯誤",
                        rows = new List<UploadOrderInfoRow>()
                    };
                }

                var rows = apiResult.rows ?? new List<UploadOrderInfoRow>();
                bool success = apiResult.resultCode == "10";

                return new DeliveryAssistantApiResult
                {
                    title = "託運資料",
                    success = success,
                    resultCode = apiResult.resultCode,
                    msg = success ? string.Format("上傳成功，共 {0} 筆", items.Count) : (apiResult.error ?? "上傳失敗"),
                    rows = rows
                };
            }
            catch (Exception ex)
            {
                return new DeliveryAssistantApiResult
                {
                    title = "託運資料",
                    success = false,
                    msg = ex.Message,
                    rows = new List<UploadOrderInfoRow>()
                };
            }
        }

        private DeliveryAssistantApiResult CallEstablishDcShipApi(HttpClient client, List<UploadOrderInfoItem> items)
        {
            var firstItem = items.FirstOrDefault();
            if (firstItem == null)
            {
                return new DeliveryAssistantApiResult
                {
                    title = "車次",
                    success = false,
                    msg = "Excel 無有效資料",
                    rows = new List<EstablishDcShipResponseOrderRow>()
                };
            }

            try
            {
                var request = new EstablishDcShipRequest
                {
                    dcShip = firstItem.dcShip,
                    arriveDate = firstItem.arriveDate,
                    driverData = new EstablishDcShipDriverData
                    {
                        driverId = "jetf005",
                    },
                    cusOrderInfoList = items.Select(x => new EstablishDcShipCusOrderInfo
                    {
                        cusOrder = x.cusOrder,
                        arriveDate = x.arriveDate
                    }).ToList()
                };

                string json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(EstablishDcShipApiUrl, content).Result;
                string responseBody = response.Content.ReadAsStringAsync().Result;

                var apiResult = JsonConvert.DeserializeObject<EstablishDcShipResponse>(responseBody);

                if (apiResult == null)
                {
                    return new DeliveryAssistantApiResult
                    {
                        title = "車次",
                        success = false,
                        msg = "API 回傳格式錯誤",
                        rows = new List<EstablishDcShipResponseOrderRow>()
                    };
                }

                var rows = apiResult.rows ?? new List<EstablishDcShipResponseOrderRow>();
                bool success = apiResult.resultCode == "10";

                return new DeliveryAssistantApiResult
                {
                    title = "車次",
                    success = success,
                    resultCode = apiResult.resultCode,
                    msg = success ? "建立車次成功" : (apiResult.error ?? "建立車次失敗"),
                    row = apiResult.row,
                    rows = rows
                };
            }
            catch (Exception ex)
            {
                return new DeliveryAssistantApiResult
                {
                    title = "車次",
                    success = false,
                    msg = ex.Message,
                    rows = new List<EstablishDcShipResponseOrderRow>()
                };
            }
        }

        /// <summary>
        /// 讀取 Excel 並依欄位對應轉為 UploadOrderInfoItem 清單（略過表頭第一列）
        /// </summary>
        private List<UploadOrderInfoItem> ReadExcel(string filePath)
        {
            var list = new List<UploadOrderInfoItem>();

            IWorkbook workbook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(fs);
            }

            var sheet = workbook.GetSheetAt(0);

            // 第 0 列為表頭，從第 1 列開始讀
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) 
                    continue;

                string cusOrder = row.GetCellData(ColCusOrder);
                if (string.IsNullOrWhiteSpace(cusOrder)) continue;

                list.Add(new UploadOrderInfoItem
                {
                    dcShip             = row.GetCellData(ColDcShip),
                    cusOrder           = cusOrder,
                    cusOwnerName       = row.GetCellData(ColCusOwnerName),
                    arriveDate         = row.GetCellData(ColArriveDate),
                    driverName         = row.GetCellData(ColDriverName),
                    contactPerson     = row.GetCellData(ColContactPerson),
                    contactTel        = row.GetCellData(ColContactTel),
                    accountsReceivable = row.GetCellData(ColAccountsReceivable),
                    addr               = row.GetCellData(ColAddr),
                    cases              = row.GetCellData(ColCases),
                    wgt                = row.GetCellData(ColWgt)
                });
            }

            return list;
        }

        private byte[] BuildExcel(List<DeliveryAssistantExportModel> data, DeliveryAssistantRequest request)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("派送助理");

            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 11, true);
            ICellStyle dataStyle = NpoiStyle.CreateDataStyle(workbook);

            var headers = new List<string>
            {
                "車次編號", "客戶單號", "客戶名稱", "到貨日期", "駕駛",
                "客服電話", "聯絡人", "連絡電話", "應收款", "付款方式",
                "住址(與經緯度則一必填)", "是否提供經緯度(與地址則一必填)(YES/NO)",
                "經度", "緯度", "家用電話", "車次預計打卡時間", "車號",
                "序號", "已排順序", "訂單號", "客戶代碼", "店別代碼",
                "路線", "區", "件數", "才積(才數)", "重量",
                "溫別代碼(C:空調 N:常溫 L:冷藏 F:冷凍C+F:空調+冷凍 …)",
                "冷凍件數", "常溫件數", "冷藏件數", "銷售總金額", "發票號碼",
                "指定簽收人", "指定簽收方式", "配送等級", "急單(YES/NO)",
                "可收貨時間起(若有急單則時間起迄則一必填)", "可收貨時間迄(若有急單則時間起迄則一必填)",
                "時間1起", "時間1迄", "時間2起", "時間2迄", "時間3起", "時間3迄",
                "收貨客戶類型(需先在後台建立資料填代號即可)", "作業時間(Min)", "備註事項"
            };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            string dateStr = string.Empty;
            if (!string.IsNullOrEmpty(request.StartDate) &&
                DateTime.TryParse(request.StartDate, out DateTime dt))
            {
                dateStr = dt.ToString("yyyyMMdd");
            }

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                IRow row = sheet.CreateRow(i + 1);
                int col = 0;

                NpoiCell.CreateCell(row, col++, $"{dateStr}-05", dataStyle);
                NpoiCell.CreateCell(row, col++, item.Data ?? "", dataStyle);
                NpoiCell.CreateCell(row, col++, item.Importer ?? "", dataStyle);
                NpoiCell.CreateCell(row, col++, item.UploadTime.HasValue ? item.UploadTime.Value.ToString("yyyy-MM-dd") : "", dataStyle);
                NpoiCell.CreateCell(row, col++, "捷穩通05", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, item.Importer ?? "", dataStyle);
                NpoiCell.CreateCell(row, col++, item.ImporterPhone ?? "", dataStyle);
                NpoiCell.CreateDoubleCell(row, col++, (double?)item.TO_DLV_COD ?? 0, dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, item.ImporterAddr ?? "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateIntCell(row, col++, 1, dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateDoubleCell(row, col++, (double?)item.GW, dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
                NpoiCell.CreateCell(row, col++, "", dataStyle);
            }

            sheet.AutoSizeColumns(headers.Count, scale: 1.2, minWidth: 8);

            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }
    }
}
