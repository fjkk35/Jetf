using Newtonsoft.Json;
using NPOI.SS.UserModel;
using Service.Models;
using Service.Services.AirMainComparison;
using Service.Services.AirMainComparison.Domain;
using Service.Services.Ftz.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Service.Services.Ftz
{
    public partial class FtzService
    {
        /// <summary>
        /// 主號查詢；查詢完成後套用空運主號共用比對規則。
        /// </summary>
        public async Task<ResponseModel> MainQueryAsync(
            FtzMainQueryRequest request,
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

                var results = new List<FtzMainQueryViewModel>();
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
                            results.Add(new FtzMainQueryViewModel
                            {
                                Mwb = mwb,
                                HwbCount = "0",
                                HwbPiece = "0",
                                HwbGciPiece = "0",
                                HwbGcoPiece = "0",
                                GciWeight = "0",
                                NotGciPiece = 0,
                                ExpBagCount = "0",
                                ExpBagGciCount = "0",
                                ExpBagGcoCount = "0",
                                NotGciBag = 0,
                                NotGciTotal = 0,
                                ErrorMessage = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                // 取得派件公司，並套用上傳明細的未收單與 ZZZA 統計。
                _airMainComparisonService.ApplyComparison(
                    results.Cast<IAirMainComparisonItem>().ToList(),
                    uploadRows);

                return new ResponseModel { ReturnObject = results };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢單筆主號資料。
        /// </summary>
        private async Task<FtzMainQueryViewModel> QuerySingleMainAsync(HttpClient httpClient, string mwb)
        {
            // 建立查詢 URL。
            var queryUrl = $"{MAIN_QUERY_URL}?ieType=I&mwb={Uri.EscapeDataString(mwb)}&eid=0335&boxno=0H4&_search=false&nd={DateTimeOffset.Now.ToUnixTimeMilliseconds()}&rows=10000&page=1&sidx=&sord=asc";
            var response = await httpClient.GetAsync(queryUrl);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new Exception("無回應資料");
            }

            // 解析 JSON 回應。
            var mainQueryResult = JsonConvert.DeserializeObject<FtzMainQueryResult>(jsonContent);
            if (mainQueryResult?.userdata == null)
            {
                throw new Exception("無法解析資料");
            }

            // 轉換為 ViewModel 並計算欄位。
            var model = ConvertToViewModel(mainQueryResult);
            model.Mwb = mwb;

            if (model.NotGciTotal > 0)
            {
                // 查詢申報未入倉明細。
                model.NotGciDetails = await QueryNotGciDetailsAsync(httpClient, mwb);
                // 未進倉申報袋號。
                model.NotGciPieceExpBagNo = string.Join(",", model.NotGciDetails
                    .Where(r => !string.IsNullOrEmpty(r.expBagNo) && !(r.declNo ?? "").Contains("0H4W"))
                    .Select(r => r.expBagNo)
                    .Distinct());
            }

            return model;
        }

        /// <summary>
        /// 將 API 回應轉換為 ViewModel 並計算相關欄位。
        /// </summary>
        private FtzMainQueryViewModel ConvertToViewModel(FtzMainQueryResult rawData)
        {
            var userData = rawData.userdata;
            // 解析數值（安全轉換）。
            var hwbPiece = AirMainValueParser.ParseInt(userData.hwbPiece);
            var hwbGciPiece = AirMainValueParser.ParseInt(userData.hwbGciPiece);
            var expBagCount = AirMainValueParser.ParseInt(userData.expBagCount);
            var expBagGciCount = AirMainValueParser.ParseInt(userData.expBagGciCount);
            // 計算未進倉 = 申報 - 進倉。
            var notGciPiece = hwbPiece - hwbGciPiece;
            // 計算未進倉袋 = 併袋 - 進倉袋。
            var notGciBag = expBagCount - expBagGciCount;

            return new FtzMainQueryViewModel
            {
                HwbCount = userData.hwbCount,
                HwbPiece = userData.hwbPiece,
                HwbGciPiece = userData.hwbGciPiece,
                HwbGcoPiece = userData.hwbGcoPiece,
                GciWeight = userData.gciWeight ?? userData.hwbGciWt,
                NotGciPiece = notGciPiece,
                ExpBagCount = userData.expBagCount,
                ExpBagGciCount = userData.expBagGciCount,
                ExpBagGcoCount = userData.expBagGcoCount,
                NotGciBag = notGciBag,
                // 計算未進倉小計 = 未進倉 + 未進倉袋。
                NotGciTotal = notGciPiece + notGciBag,
                RawData = rawData
            };
        }

        /// <summary>
        /// 查詢申報未入倉明細。
        /// </summary>
        private async Task<List<Row>> QueryNotGciDetailsAsync(HttpClient httpClient, string mwb)
        {
            try
            {
                // 取得當前時間。
                var now = DateTime.Now;
                var start = now.AddDays(-30);
                // 格式化參數並建立查詢 URL。
                var queryUrl = $"{NOGCI_QUERY_URL}?ieType=I&eid=0335&d1={start:yyyyMMdd}&t1={start:HHmm}&d2={now:yyyyMMdd}&t2={now:HHmm}&mwb={Uri.EscapeDataString(mwb)}&_search=false&nd={DateTimeOffset.Now.ToUnixTimeMilliseconds()}&rows=10000&page=1&sidx=&sord=asc";
                // 發送請求。
                var response = await httpClient.GetAsync(queryUrl);
                var jsonContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<Row>();
                }

                // 解析 JSON 回應。
                return JsonConvert.DeserializeObject<FtzNoGciQueryResult>(jsonContent)?.rows
                    ?? new List<Row>();
            }
            catch
            {
                // 查詢失敗時返回空列表。
                return new List<Row>();
            }
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
            FtzMainQueryRequest request,
            AirMainUploadExcelData uploadData = null)
        {
            uploadData = uploadData ?? new AirMainUploadExcelData();
            // 先查詢資料。
            var response = await MainQueryAsync(request, uploadData.DetailRows);
            if (response.status != Status.success || response.ReturnObject == null)
            {
                throw new Exception(response.msg ?? "查詢失敗");
            }

            var results = ((IEnumerable<FtzMainQueryViewModel>)response.ReturnObject)
                .Cast<IAirMainComparisonItem>();
            return _airMainComparisonService.CreateExportWorkbook(
                "Ftz主號查詢結果",
                results,
                uploadData);
        }

    }
}
