using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.Ftz.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz
{
    public partial class FtzService
    {
        /// <summary>
        /// 查詢併袋號
        /// </summary>
        public async Task<ResponseModel> QueryBagAsync(FtzBagQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BagNoList))
                {
                    return new ResponseModel("請輸入查詢袋號");
                }

                var bagNoList = request.BagNoList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (!bagNoList.Any())
                {
                    return new ResponseModel("請輸入查詢袋號");
                }

                var allResults = new List<RowItem>();

                using (var httpClient = GetHttpClient())
                {
                    foreach (var bagNo in bagNoList)
                    {
                        try
                        {
                            var results = await QuerySingleBagAsync(httpClient, bagNo);
                            if (results != null && results.Any())
                            {
                                allResults.AddRange(results);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 如果查詢失敗，添加一筆錯誤記錄
                            allResults.Add(new RowItem
                            {
                                bagNo = bagNo,
                                message = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                return new ResponseModel
                {
                    ReturnObject = allResults
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆併袋號資料
        /// </summary>
        private async Task<List<RowItem>> QuerySingleBagAsync(HttpClient httpClient, string bagNo)
        {
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var url = $"{EXPBAGNO_QUERY_URL}?ieType=I&mwb=&eid=0335&bno={bagNo}&_search=false&nd={timestamp}&rows=500&page=1&sidx=&sord=asc";

            var response = await httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<RowItem>();
            }

            var result = JsonConvert.DeserializeObject<FtzBagQueryResult>(json);
            result?.rows?.ForEach(r =>
            {
                r.bagNo = bagNo;
            });
            if (result == null || result.rows == null || !result.rows.Any())
            {
                return new List<RowItem>
                {
                    new RowItem
                    {
                        bagNo = bagNo,
                        message = "查無資料"
                    }
                };
            }

            return result.rows;
        }

        /// <summary>
        /// 匯出併袋號 Excel
        /// </summary>
        public async Task<IWorkbook> ExportBagExcel(FtzBagQueryRequest request)
        {
            // 先查詢資料
            var queryResult = await QueryBagAsync(request);

            if (queryResult.status != Status.success || queryResult.ReturnObject == null)
            {
                throw new Exception(queryResult.msg ?? "查詢失敗");
            }

            var results = queryResult.ReturnObject as List<RowItem>;

            // 建立 Excel
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("併袋號查詢結果");

            // 建立樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            // 建立表頭
            string[] headers = new string[]
            {
                "袋號", "報單號碼", "主號", "分號", "航班", "重量", 
                "申報", "進倉", "出倉", "通關方式", "驗貨窗口", "備註","錯誤訊息"
            };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            // 設定欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            // 填入資料
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                IRow dataRow = sheet.CreateRow(i + 1);

                NpoiCell.CreateCell(dataRow, 0, item.bagNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.declNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.mwb ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.hwb ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.flightNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 5, item.gciWeight ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 6, item.piece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 7, item.gciPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 8, item.gcoPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 9, item.clearanceType ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 10, item.examinationNote ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 11, item.remarks ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 12, item.message ?? "", dataStyle);
            }

            return workbook;
        }
    }
}
