using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.Tact.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Tact
{
    public partial class TactService
    {
        /// <summary>
        /// 併袋號查詢
        /// </summary>
        public async Task<ResopnseModel> QueryBagAsync(TactBagQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BagNoList))
                {
                    return new ResopnseModel("請輸入併袋號");
                }

                var bagNoList = request.BagNoList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (!bagNoList.Any())
                {
                    return new ResopnseModel("請輸入併袋號");
                }

                var results = new List<TactBagNoModel>();

                using (var httpClient = GetHttpClient())
                {
                    foreach (var bagNo in bagNoList)
                    {
                        try
                        {
                            var resultList = await QuerySingleBagAsync(httpClient, bagNo);
                            results.AddRange(resultList);
                        }
                        catch (Exception ex)
                        {
                            results.Add(new TactBagNoModel
                            {
                                BagNumber = bagNo,
                                Remark = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                return new ResopnseModel(results);
            }
            catch (Exception ex)
            {
                return new ResopnseModel($"查詢錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆併袋號資料
        /// </summary>
        private async Task<List<TactBagNoModel>> QuerySingleBagAsync(HttpClient httpClient, string bagNo)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ie_rad", "I"),
                new KeyValuePair<string, string>("bag_no", bagNo)
            });

            var response = await httpClient.PostAsync(BAG_QUERY_URL, formContent);
            var html = await response.Content.ReadAsStringAsync();

            return ParseBagHtml(bagNo, html);
        }

        /// <summary>
        /// 解析併袋號查詢 HTML
        /// </summary>
        private List<TactBagNoModel> ParseBagHtml(string bagNo, string htmlContent)
        {
            List<TactBagNoModel> list = new List<TactBagNoModel>();
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 取得第2個 table
            HtmlAgilityPack.HtmlNode table = doc.DocumentNode.SelectSingleNode("(//table)[2]");

            if (table != null && table.SelectNodes(".//tr[2]") != null)
            {
                HtmlAgilityPack.HtmlNode title = table.SelectNodes(".//tr[2]")[0];

                // 取得併袋號
                string extractedBagNo = bagNo;
                var bagNoNode = table.SelectNodes(".//tr[1]");
                if (bagNoNode != null && bagNoNode.Count > 0)
                {
                    var fontNode = bagNoNode[0].SelectNodes(".//td[1]//font");
                    if (fontNode != null && fontNode.Count > 0)
                    {
                        extractedBagNo = fontNode[0].InnerText.Trim();
                    }
                }

                foreach (HtmlAgilityPack.HtmlNode row in table.SelectNodes(".//tr[position() > 2]"))
                {
                    TactBagNoModel tableData = new TactBagNoModel();
                    tableData.BagNumber = extractedBagNo;

                    // 提取單元格內容
                    HtmlAgilityPack.HtmlNodeCollection cells = row.SelectNodes("td");
                    if (cells != null && cells.Count >= 14)
                    {
                        var titleCells = title.SelectNodes("td");
                        if (titleCells != null)
                        {
                            for (int i = 0; i < titleCells.Count && i < cells.Count; i++)
                            {
                                string cellValue = cells[i].InnerText.Trim();
                                switch (titleCells[i].InnerText.Trim())
                                {
                                    case "主號":
                                        tableData.Mwb = cellValue;
                                        break;
                                    case "分提單號":
                                        tableData.TrackingNo = cellValue;
                                        break;
                                    case "報關類別":
                                        tableData.DeclType = cellValue;
                                        break;
                                    case "報單號碼":
                                        tableData.DeclNo = cellValue;
                                        break;
                                    case "申報件數":
                                        tableData.Piece = cellValue;
                                        break;
                                    case "進倉件數":
                                        tableData.GciPiece = cellValue;
                                        break;
                                    case "出倉件數":
                                        tableData.GcoPiece = cellValue;
                                        break;
                                    case "申報重量":
                                        tableData.Weight = cellValue;
                                        break;
                                    case "進倉重量":
                                        tableData.GciWeight = cellValue;
                                        break;
                                    case "進倉時間":
                                        tableData.GciDate1 = cellValue;
                                        break;
                                    case "出倉時間":
                                        tableData.GcoDate1 = cellValue;
                                        break;
                                    case "航機班次":
                                        tableData.FlightNo = cellValue;
                                        break;
                                    case "更改後報單號":
                                        tableData.UpdateDecl = cellValue;
                                        break;
                                    case "計費金額":
                                        tableData.Amount = cellValue;
                                        break;
                                }
                            }
                        }
                        list.Add(tableData);
                    }
                }
            }
            else
            {
                // 查無資料或錯誤
                TactBagNoModel tableData = new TactBagNoModel();
                tableData.BagNumber = bagNo;
                if (table != null && table.SelectNodes(".//tr") != null && table.SelectNodes(".//tr").Count > 0)
                {
                    tableData.Remark = table.SelectNodes(".//tr")[0].InnerText.Trim();
                }
                else
                {
                    tableData.Remark = "查無資料";
                }
                list.Add(tableData);
            }

            return list;
        }

        /// <summary>
        /// 併袋號查詢匯出 Excel
        /// </summary>
        public async Task<IWorkbook> ExportBagExcel(TactBagQueryRequest request)
        {
            // 先查詢資料
            var queryResult = await QueryBagAsync(request);

            if (queryResult.status != Status.success || queryResult.ReturnObject == null)
            {
                throw new Exception(queryResult.msg ?? "查詢失敗");
            }

            var results = queryResult.ReturnObject as List<TactBagNoModel>;

            // 建立 Excel
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Tact併袋號查詢結果");

            // 建立樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            // 建立表頭
            string[] headers = new string[]
            {
                "併袋號", "主號", "分提單號", "報關類別", "報單號碼", "通關方式",
                "申報件數", "進倉件數", "出倉件數", "申報重量", "進倉重量",
                "進倉時間", "出倉時間", "航機班次", "更改後報單號", "計費金額", "備註"
            };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            // 設定欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, 4000);
            }

            // 填入資料
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                IRow dataRow = sheet.CreateRow(i + 1);

                NpoiCell.CreateCell(dataRow, 0, item.BagNumber ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.Mwb ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.TrackingNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.DeclType ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.DeclNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 5, item.ClearanceType ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 6, item.Piece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 7, item.GciPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 8, item.GcoPiece ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 9, item.Weight ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 10, item.GciWeight ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 11, item.GciDate1 ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 12, item.GcoDate1 ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 13, item.FlightNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 14, item.UpdateDecl ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 15, item.Amount ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, 16, item.Remark ?? "", dataStyle);
            }

            return workbook;
        }
    }
}
