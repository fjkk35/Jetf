using Dapper;
using HtmlAgilityPack;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.Tact.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.Services.Tact
{
    public partial class TactService
    {
        /// <summary>
        /// 主號查詢
        /// </summary>
        public async Task<ResponseModel> MainQueryAsync(TactMainQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Mwb))
                {
                    return new ResponseModel("請輸入主號");
                }

                var mwbList = request.Mwb.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
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
                            var result = await QuerySingleMainAsync(httpClient, mwb);
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            results.Add(new TactMainQueryViewModel
                            {
                                Mwb = mwb,
                                ErrorMessage = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                // 取得派件公司
                GetTransName(results);

                return new ResponseModel(results);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆主號資料
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
        /// 解析主號查詢 HTML
        /// </summary>
        private TactMainQueryViewModel ParseMainHtml(string mwb, string htmlContent)
        {
            var model = new TactMainQueryViewModel();
            model.Mwb = mwb;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            HtmlNode divNode = doc.DocumentNode.SelectSingleNode("//div[@id='id_contain']");
            if (divNode != null)
            {
                int value;
                double dvalue;
                // 使用正則表達式來找到冒號後的數值
                string pattern = @"(?<=:)\s*\d+(\.\d+)?";

                // 以分號申報
                HtmlNodeCollection trNodeTrankNumber = divNode.SelectNodes(".//table//tr/td[@colspan='15' and contains(., '以分號申報')]");
                if (trNodeTrankNumber != null && trNodeTrankNumber.Count > 0)
                {
                    HtmlNode td = trNodeTrankNumber[0];
                    MatchCollection matches = Regex.Matches(td.InnerText, pattern);
                    if (matches.Count > 6)
                    {
                        // 以分號申報
                        model.TrackingNo = int.TryParse(matches[0].Value, out value) ? (int?)value : null;
                        // 未進倉
                        model.NotGciPiece = int.TryParse(matches[2].Value, out value) ? (int?)value : null;
                        // 申報
                        model.Piece = int.TryParse(matches[3].Value, out value) ? (int?)value : null;
                        // 進倉
                        model.GciPiece = int.TryParse(matches[4].Value, out value) ? (int?)value : null;
                        // 出倉
                        model.GcoPiece = int.TryParse(matches[5].Value, out value) ? (int?)value : null;
                        // 進倉重量
                        model.GciWeight = double.TryParse(matches[6].Value, out dvalue) ? (double?)dvalue : null;
                    }
                }

                // 以併袋申報
                HtmlNodeCollection trNodeBagNumber = divNode.SelectNodes(".//table//tr/td[@colspan='15' and contains(., '以併袋申報')]");
                if (trNodeBagNumber != null && trNodeBagNumber.Count > 0)
                {
                    HtmlNode td = trNodeBagNumber[0];
                    MatchCollection matches = Regex.Matches(td.InnerText, pattern);
                    if (matches.Count > 3)
                    {
                        // 併袋
                        model.BagNumber = int.TryParse(matches[0].Value, out value) ? (int?)value : null;
                        // 進倉袋
                        model.GciBagNumber = int.TryParse(matches[1].Value, out value) ? (int?)value : null;
                        // 出倉袋
                        model.GcoBagNumber = model.GciBagNumber;
                        // 未進倉袋
                        model.NotGciBagNumber = int.TryParse(matches[2].Value, out value) ? (int?)value : null;
                    }
                }

                model.NotGciPieceCount = (model.NotGciPiece ?? 0) + (model.NotGciBagNumber ?? 0);

                // 找出未入倉明細
                HtmlNodeCollection trList = divNode.SelectNodes(".//table//tr[position() >= 3 and .//td[11][text()='未進倉']]");
                if (trList != null)
                {
                    foreach (var tr in trList)
                    {
                        HtmlNodeCollection tdNodes = tr.SelectNodes(".//td");
                        if (tdNodes != null && tdNodes.Count >= 15)
                        {
                            model.NotGciDetails.Add(new TactMainNoDetailModel()
                            {
                                // 分提單號
                                TrackingNo = tdNodes[0].InnerText.Trim(),
                                // 報關類別
                                DeclType = tdNodes[1].InnerText.Trim(),
                                // 併袋號
                                BagNumber = tdNodes[2].InnerText.Trim(),
                                // 報單號碼
                                DeclNo = tdNodes[3].InnerText.Trim(),
                                // 通關方式
                                ClearanceType = tdNodes[4].InnerText.Trim(),
                                // 申報件數
                                Piece = int.TryParse(tdNodes[5].InnerText.Trim(), out value) ? value : 0,
                                // 進倉件數
                                GciPiece = int.TryParse(tdNodes[6].InnerText.Trim(), out value) ? value : 0,
                                // 出倉件數
                                GcoPiece = int.TryParse(tdNodes[7].InnerText.Trim(), out value) ? value : 0,
                                // 申報重量
                                Weight = tdNodes[8].InnerText.Trim(),
                                // 進倉重量
                                GciWeight = tdNodes[9].InnerText.Trim(),
                                // 進倉時間
                                GciDate1 = tdNodes[10].InnerText.Trim(),
                                // 出倉時間
                                GcoDate1 = tdNodes[11].InnerText.Trim(),
                                // 航機班次
                                FlightNo = tdNodes[12].InnerText.Trim(),
                                // 更改後報單號
                                UpdateDecl = tdNodes[13].InnerText.Trim(),
                                // 稅費金額
                                Amount = tdNodes[14].InnerText.Trim(),
                            });
                        }
                    }

                    if (model.NotGciDetails.Count > 0)
                    {
                        model.NotGciDetails.ForEach(x =>
                        {
                            x.B6F = IsB6F(x.BagNumber, x.DeclNo);
                        });

                        model.B6FCount = model.NotGciDetails.Where(x => x.B6F).Count();
                        model.B6FTrackingNo = string.Join(",", model.NotGciDetails.Where(x => x.B6F).Select(s => s.TrackingNo).ToList());

                        // 未進倉件不含B6F分號
                        model.NotGciPieceNotB6F = string.Join(",", model.NotGciDetails.Where(x => !x.B6F && string.IsNullOrEmpty(x.BagNumber) == true).Select(s => s.TrackingNo).ToList());

                        // 未進倉申報袋號
                        model.NotGciPieceBagNumber = string.Join(",",
                            model.NotGciDetails.Where(x => string.IsNullOrEmpty(x.BagNumber) == false && !x.DeclNo.Contains("0H4W"))
                                  .Select(s => s.BagNumber).Distinct().ToList());
                    }
                }
            }

            return model;
        }

        /// <summary>
        /// 確認是否為 B6F
        /// </summary>
        private bool IsB6F(string bagNo, string clearanceNo)
        {
            int index;
            string clearanceBagNo = "";
            if ((index = clearanceNo.IndexOf("0H4W")) > -1)
            {
                clearanceBagNo = clearanceNo.Substring(index);
            }
            else
            {
                return false;
            }

            // 查詢客代
            string custNo = GetSysAirBag(clearanceBagNo);

            bool result = string.IsNullOrEmpty(bagNo) &&
                     clearanceNo.Contains("0H4W") &&
                     custNo != "CN00121" ? true : false;

            return result;
        }

        /// <summary>
        /// 查詢客代
        /// </summary>
        private string GetSysAirBag(string bagNo)
        {
            string custNo = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select CUST_CODE from DATA_CENTER.dbo.SYS_AIR_BAG where BAG_NUMBER=@BAG_NUMBER", conn))
            {
                da.SelectCommand.Parameters.Add("@BAG_NUMBER", SqlDbType.NVarChar).Value = bagNo;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                custNo = dt.Rows[0]["CUST_CODE"].ToString().Trim();
            }

            return custNo;
        }

        /// <summary>
        /// 取得派件公司
        /// </summary>
        private void GetTransName(List<TactMainQueryViewModel> list)
        {
            // 收集所有需要查詢的主號、分號、袋號
            var allMwbs = list.Select(r => r.Mwb).Distinct().ToList();
            var allHwbs = list.SelectMany(r => r.NotGciDetails ?? new List<TactMainNoDetailModel>())
                             .Where(x => !x.B6F && string.IsNullOrEmpty(x.BagNumber))
                             .Select(x => x.TrackingNo)
                             .Distinct()
                             .ToList();
            var allBagNos = list.SelectMany(r => r.NotGciDetails ?? new List<TactMainNoDetailModel>())
                               .Where(x => !x.B6F && !string.IsNullOrEmpty(x.BagNumber))
                               .Select(x => x.BagNumber)
                               .Distinct()
                               .ToList();

            // 建立字典存放查詢結果
            var customerDict = new Dictionary<string, string>();
            var flightDict = new Dictionary<string, string>();
            var trackingNoDict = new Dictionary<string, string>();
            var bagNoDict = new Dictionary<string, string>();

            // 1. 批次查詢客戶名稱
            if (allMwbs.Any())
            {
                var sql = @"
                        SELECT a.MAINNUMBER, b.DESPATCHNAME 
                        FROM [DATA_CENTER].[dbo].[MAINORDERINFO] a 
                        JOIN [DATA_CENTER].[dbo].[DESPATCHFROM] b ON a.DELIVERYFROM = b.DESPATCHNO 
                        WHERE a.MAINNUMBER IN @Mwbs 
                        GROUP BY a.MAINNUMBER, b.DESPATCHNAME";

                customerDict = conn.Query<(string MAINNUMBER, string DESPATCHNAME)>(sql, new { Mwbs = allMwbs })
                    .ToDictionary(r => r.MAINNUMBER, r => r.DESPATCHNAME);
            }

            // 2. 批次查詢班機
            if (allMwbs.Any())
            {
                var sql = @"
                        SELECT MAINNUMBER, FLIGHTNUMBER 
                        FROM [DATA_CENTER].[dbo].[MAINORDERINFO] 
                        WHERE MAINNUMBER IN @Mwbs";

                var flights = conn.Query<(string MAINNUMBER, string FLIGHTNUMBER)>(sql, new { Mwbs = allMwbs });

                flightDict = flights
                     .GroupBy(x => x.MAINNUMBER)
                     .ToDictionary(
                         g => g.Key,
                         g => g.First().FLIGHTNUMBER
                     );
            }

            // 3. 批次查詢分提單號的派件公司
            if (allHwbs.Any())
            {
                var sql = @"
                        SELECT TRACKINGNO, jetf.dbo.GetTRANS_NAME(CLEARANCEWAREHOUSING) AS TRANS_NAME 
                        FROM DATA_CENTER.[dbo].[ORIGINALLIST] 
                        WHERE TRACKINGNO IN @TrackingNos 
                        union all
                        SELECT BAGNO, jetf.dbo.GetTRANS_NAME(CLEARANCEWAREHOUSING) AS TRANS_NAME 
                        FROM DATA_CENTER.[dbo].[ORIGINALLIST] 
                        WHERE BAGNO IN @TrackingNos
                ";

                var trackings = conn.Query<(string TRACKINGNO, string TRANS_NAME)>(sql, new { TrackingNos = allHwbs });

                trackingNoDict = trackings
                    .GroupBy(x => x.TRACKINGNO)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().TRANS_NAME
                        );
            }

            // 4. 批次查詢袋號的派件公司
            if (allBagNos.Any())
            {
                var sql = @"
                        SELECT BAGNO, jetf.dbo.GetTRANS_NAME(CLEARANCEWAREHOUSING) AS TRANS_NAME 
                        FROM DATA_CENTER.[dbo].[ORIGINALLIST] 
                        WHERE BAGNO IN @BagNos";

                var bags = conn.Query<(string BAGNO, string TRANS_NAME)>(sql, new { BagNos = allBagNos });
                bagNoDict = bags
                    .GroupBy(x => x.BAGNO)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().TRANS_NAME);
            }

            // 使用字典填充資料
            list.ForEach(r =>
            {
                // 設定客戶名稱
                if (customerDict.ContainsKey(r.Mwb))
                {
                    r.Customer = customerDict[r.Mwb];
                }

                // 設定班機
                if (flightDict.ContainsKey(r.Mwb))
                {
                    r.FlightNumber = flightDict[r.Mwb];
                }

                // 設定派件公司
                r.NotGciDetails?.ForEach(x =>
                {
                    if (x.B6F == false)
                    {
                        if (string.IsNullOrEmpty(x.BagNumber))
                        {
                            // 用分提單號查詢
                            if (trackingNoDict.ContainsKey(x.TrackingNo))
                            {
                                x.TransName = trackingNoDict[x.TrackingNo];
                            }
                        }
                        else
                        {
                            // 用袋號查詢
                            if (bagNoDict.ContainsKey(x.BagNumber))
                            {
                                x.TransName = bagNoDict[x.BagNumber];
                            }
                        }
                    }
                });
            });
        }

        /// <summary>
        /// 主號查詢匯出 Excel
        /// </summary>
        public async Task<IWorkbook> ExportMainExcel(TactMainQueryRequest request)
        {
            // 先查詢資料
            var queryResult = await MainQueryAsync(request);

            if (queryResult.status != Status.success || queryResult.ReturnObject == null)
            {
                throw new Exception(queryResult.msg ?? "查詢失敗");
            }

            var results = queryResult.ReturnObject as List<TactMainQueryViewModel>;

            // 建立 Excel
            IWorkbook workbook = new XSSFWorkbook();

            // 第一個頁簽：主號查詢結果
            ISheet sheet1 = workbook.CreateSheet("主號查詢結果");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            // 建立表頭
            var headers1 = new List<string>
            {
                "主號", "客戶名稱", "航班", "申報", "進倉", "未進倉件",
                "B6F", "B6F分號", "未進倉件不含B6F分號", "併袋", "進倉袋",
                "未進倉袋", "未進倉小計", "未進倉申報袋號", "錯誤訊息"
            };

            // 取得所有派件公司，加入表頭
            var transNames = results.Where(r => r.NotGciDetails != null)
                            .SelectMany(r => r.NotGciDetails)
                            .Where(r => r.B6F == false && r.TransName != null)
                            .Select(r => r.TransName)
                            .Distinct()
                            .ToList();

            headers1.AddRange(transNames);

            IRow headerRow1 = sheet1.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow1, headers1, headerStyle);

            // 設定欄寬
            for (int i = 0; i < headers1.Count; i++)
            {
                sheet1.SetColumnWidth(i, 4000);
            }

            // 填入資料
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                IRow dataRow = sheet1.CreateRow(i + 1);
                var column = 0;

                NpoiCell.CreateCell(dataRow, column++, item.Mwb ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.Customer ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.FlightNumber ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.Piece?.ToString() ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.GciPiece?.ToString() ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.NotGciPiece?.ToString() ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.B6FCount.ToString(), dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.B6FTrackingNo ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.NotGciPieceNotB6F ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.BagNumber?.ToString() ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.GciBagNumber?.ToString() ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.NotGciBagNumber?.ToString() ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.NotGciPieceCount.ToString(), dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.NotGciPieceBagNumber ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.ErrorMessage ?? "", dataStyle);

                // 派件公司
                foreach (var transName in transNames)
                {
                    // 件數
                    var trackingnoCount = item.NotGciDetails?.Where(r => r.B6F == false && string.IsNullOrEmpty(r.BagNumber) && r.TransName == transName).Count() ?? 0;
                    // 袋數
                    var bagnoCount = item.NotGciDetails?.Where(r => r.B6F == false && !string.IsNullOrEmpty(r.BagNumber) && r.TransName == transName)
                                              .Select(r => new
                                              {
                                                  r.BagNumber,
                                                  r.TransName
                                              }).Distinct().Count() ?? 0;

                    var totalCount = trackingnoCount + bagnoCount;

                    NpoiCell.CreateIntCell(dataRow, column++, totalCount, dataStyle);
                }
            }

            // 第二個頁簽：未進倉明細
            ISheet sheet2 = workbook.CreateSheet("未進倉明細");
            string[] headers2 = new string[]
            {
                "項次", "分提單號", "報關類別", "併袋號", "報單號碼", "通關方式",
                "申報件數", "進倉件數", "出倉件數", "申報重量", "進倉重量",
                "進倉時間", "出倉時間", "航機班次", "更改後報單號", "稅費金額", "B6F", "派件公司"
            };

            IRow headerRow2 = sheet2.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow2, headers2, headerStyle);

            // 設定欄寬
            for (int i = 0; i < headers2.Length; i++)
            {
                sheet2.SetColumnWidth(i, 4000);
            }

            // 填入明細資料
            int rowIndex = 1;
            foreach (var result in results)
            {
                foreach (var detail in result.NotGciDetails)
                {
                    IRow dataRow = sheet2.CreateRow(rowIndex);

                    NpoiCell.CreateCell(dataRow, 0, rowIndex.ToString(), dataStyle);
                    NpoiCell.CreateCell(dataRow, 1, detail.TrackingNo ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 2, detail.DeclType ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 3, detail.BagNumber ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 4, detail.DeclNo ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 5, detail.ClearanceType ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 6, detail.Piece.ToString(), dataStyle);
                    NpoiCell.CreateCell(dataRow, 7, detail.GciPiece.ToString(), dataStyle);
                    NpoiCell.CreateCell(dataRow, 8, detail.GcoPiece.ToString(), dataStyle);
                    NpoiCell.CreateCell(dataRow, 9, detail.Weight ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 10, detail.GciWeight ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 11, detail.GciDate1 ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 12, detail.GcoDate1 ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 13, detail.FlightNo ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 14, detail.UpdateDecl ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 15, detail.Amount ?? "", dataStyle);
                    NpoiCell.CreateCell(dataRow, 16, detail.B6F ? "是" : "否", dataStyle);
                    NpoiCell.CreateCell(dataRow, 17, detail.TransName ?? "", dataStyle);

                    rowIndex++;
                }
            }

            return workbook;
        }
    }
}
