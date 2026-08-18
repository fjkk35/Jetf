using HtmlAgilityPack;
using NPOI.SS.UserModel;
using Service.Models;
using Service.Services.AirMainComparison;
using Service.Services.AirMainComparison.Domain;
using Service.Services.Tact.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.Services.Tact
{
    public partial class TactService
    {
        /// <summary>
        /// 主號查詢；查詢完成後套用空運主號共用比對規則。
        /// </summary>
        public async Task<ResponseModel> MainQueryAsync(
            TactMainQueryRequest request,
            List<AirMainUploadExcelRow> uploadRows = null)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Mwb))
                {
                    return new ResponseModel("請輸入主號");
                }

                // 解析主號列表（支援換行分隔）。
                var mwbList = request.Mwb
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (!mwbList.Any())
                {
                    return new ResponseModel("請輸入主號");
                }

                var results = new List<TactMainQueryViewModel>();
                using (var httpClient = GetHttpClient())
                {
                    foreach (var mwb in mwbList)
                    {
                        try
                        {
                            results.Add(await QuerySingleMainAsync(httpClient, mwb));
                        }
                        catch (Exception ex)
                        {
                            // 查詢失敗時，建立錯誤記錄。
                            results.Add(new TactMainQueryViewModel
                            {
                                Mwb = mwb,
                                Piece = 0,
                                GciPiece = 0,
                                GcoPiece = 0,
                                NotGciPiece = 0,
                                BagNumber = 0,
                                GciBagNumber = 0,
                                GcoBagNumber = 0,
                                NotGciBagNumber = 0,
                                NotGciPieceCount = 0,
                                ErrorMessage = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                // 取得派件公司，並套用上傳明細的未收單、ZZZA 與 FTZ 錯單規則。
                _airMainComparisonService.ApplyComparison(
                    results.Cast<IAirMainComparisonItem>().ToList(),
                    uploadRows,
                    excludeZzzaFromUnreceivedB6F: true);

                return new ResponseModel(results);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆主號資料。
        /// </summary>
        private async Task<TactMainQueryViewModel> QuerySingleMainAsync(HttpClient httpClient, string mwb)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ie_rad", "I"),
                new KeyValuePair<string, string>("mwb_no", mwb)
            });
            var response = await httpClient.PostAsync(MAIN_QUERY_URL, formContent);
            var html = await response.Content.ReadAsStringAsync();
            return ParseMainHtml(mwb, html);
        }

        /// <summary>
        /// 解析主號查詢 HTML；同時支援 14 欄及 15 欄明細格式。
        /// </summary>
        private TactMainQueryViewModel ParseMainHtml(string mwb, string htmlContent)
        {
            var model = new TactMainQueryViewModel { Mwb = mwb };
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent ?? string.Empty);

            var divNode = doc.DocumentNode.SelectSingleNode("//div[@id='id_contain']");
            if (divNode == null)
            {
                return model;
            }

            // 使用正則表達式來找到冒號後的數值。
            var pattern = @"(?<=:)\s*\d+(\.\d+)?";
            // 以分號申報。
            var trackingSummary = divNode.SelectSingleNode(".//table//tr/td[contains(., '以分號申報')]");
            if (trackingSummary != null)
            {
                SetTrackingSummary(model, Regex.Matches(trackingSummary.InnerText, pattern));
            }

            // 以併袋申報。
            var bagSummary = divNode.SelectSingleNode(".//table//tr/td[contains(., '以併袋申報')]");
            if (bagSummary != null)
            {
                SetBagSummary(model, Regex.Matches(bagSummary.InnerText, pattern));
            }

            model.NotGciPieceCount = (model.NotGciPiece ?? 0) + (model.NotGciBagNumber ?? 0);

            // 取得分提單號與併袋號明細，並找出未入倉明細。
            var rows = divNode.SelectNodes(".//table//tr[position() >= 3 and count(.//td) >= 14]");
            if (rows == null)
            {
                return model;
            }

            foreach (var tr in rows)
            {
                var cells = tr.SelectNodes(".//td");
                if (cells == null || cells.Count < 14)
                {
                    continue;
                }

                model.Rows.Add(new MainRow
                {
                    Hwb = CellText(cells, 0),
                    ExpBagNo = CellText(cells, 2)
                });

                var offset = cells.Count >= 15 ? 1 : 0;
                if (!string.Equals(CellText(cells, 9 + offset), "未進倉", StringComparison.Ordinal))
                {
                    continue;
                }

                model.NotGciDetails.Add(new TactMainNoDetailModel
                {
                    // 分提單號。
                    TrackingNo = CellText(cells, 0),
                    // 報關類別。
                    DeclType = CellText(cells, 1),
                    // 併袋號。
                    BagNumber = CellText(cells, 2),
                    // 報單號碼。
                    DeclNo = CellText(cells, 3),
                    // 通關方式；14 欄格式沒有此欄。
                    ClearanceType = offset == 1 ? CellText(cells, 4) : string.Empty,
                    // 申報件數。
                    Piece = ParseCellInt(cells, 4 + offset),
                    // 進倉件數。
                    GciPiece = ParseCellInt(cells, 5 + offset),
                    // 出倉件數。
                    GcoPiece = ParseCellInt(cells, 6 + offset),
                    // 申報重量。
                    Weight = CellText(cells, 7 + offset),
                    // 進倉重量。
                    GciWeight = CellText(cells, 8 + offset),
                    // 進倉時間或狀態。
                    GciDate1 = CellText(cells, 9 + offset),
                    // 出倉時間。
                    GcoDate1 = CellText(cells, 10 + offset),
                    // 航機班次。
                    FlightNo = CellText(cells, 11 + offset),
                    // 更改後報單號。
                    UpdateDecl = CellText(cells, 12 + offset),
                    // 稅費金額。
                    Amount = CellText(cells, 13 + offset)
                });
            }

            return model;
        }

        /// <summary>
        /// 設定以分號申報摘要。
        /// </summary>
        private static void SetTrackingSummary(TactMainQueryViewModel model, MatchCollection matches)
        {
            if (matches.Count <= 6)
            {
                return;
            }

            // 以分號申報。
            model.TrackingNo = ParseNullableInt(matches[0].Value);
            // 未進倉。
            model.NotGciPiece = ParseNullableInt(matches[2].Value);
            // 申報。
            model.Piece = ParseNullableInt(matches[3].Value);
            // 進倉。
            model.GciPiece = ParseNullableInt(matches[4].Value);
            // 出倉。
            model.GcoPiece = ParseNullableInt(matches[5].Value);
            // 進倉重量。
            double parsedWeight;
            model.GciWeight = double.TryParse(matches[6].Value, out parsedWeight)
                ? (double?)parsedWeight
                : null;
        }

        /// <summary>
        /// 設定以併袋申報摘要。
        /// </summary>
        private static void SetBagSummary(TactMainQueryViewModel model, MatchCollection matches)
        {
            if (matches.Count <= 3)
            {
                return;
            }

            // 併袋。
            model.BagNumber = ParseNullableInt(matches[0].Value);
            // 進倉袋。
            model.GciBagNumber = ParseNullableInt(matches[1].Value);
            // 出倉袋。
            model.GcoBagNumber = model.GciBagNumber;
            // 未進倉袋。
            model.NotGciBagNumber = ParseNullableInt(matches[2].Value);
        }

        private static int? ParseNullableInt(string value)
        {
            int parsed;
            return int.TryParse((value ?? string.Empty).Trim(), out parsed) ? (int?)parsed : null;
        }

        private static int ParseCellInt(HtmlNodeCollection cells, int index)
        {
            return AirMainValueParser.ParseInt(CellText(cells, index));
        }

        private static string CellText(HtmlNodeCollection cells, int index)
        {
            return cells != null && index >= 0 && index < cells.Count
                ? HtmlEntity.DeEntitize(cells[index].InnerText ?? string.Empty).Trim()
                : string.Empty;
        }

        /// <summary>
        /// 讀取主號查詢上傳 Excel 的資料。
        /// </summary>
        /// <param name="uploadStream">上傳檔案串流。</param>
        /// <returns>上傳 Excel 的解析結果。</returns>
        public AirMainUploadExcelData ReadMainUploadData(Stream uploadStream)
        {
            return _airMainComparisonService.ReadUploadData(uploadStream);
        }

        /// <summary>
        /// 主號查詢匯出 Excel。
        /// </summary>
        public async Task<IWorkbook> ExportMainExcel(
            TactMainQueryRequest request,
            AirMainUploadExcelData uploadData = null)
        {
            uploadData = uploadData ?? new AirMainUploadExcelData();
            // 先查詢資料。
            var response = await MainQueryAsync(request, uploadData.DetailRows);
            if (response.status != Status.success || response.ReturnObject == null)
            {
                throw new Exception(response.msg ?? "查詢失敗");
            }

            var results = ((IEnumerable<TactMainQueryViewModel>)response.ReturnObject)
                .Cast<IAirMainComparisonItem>();
            return _airMainComparisonService.CreateExportWorkbook(
                "Tact主號查詢結果",
                results,
                uploadData);
        }

        // 舊版「確認是否為 B6F」與「查詢客代」已依需求移除；
        // 錯單統一由共用服務查詢 EtlPlinkErrors，並比對 FTZ 使用的五種 Reason 代碼。
        // 舊版「2. 批次查詢班機」也已移除；航班只由上傳檔「主號2」提供。
        // 設定班機。
        // 未進倉件不含B6F分號。

    }
}
