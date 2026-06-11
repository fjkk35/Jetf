using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Services.ShipmentInboundCommon;
using Service.Services.ShipmentInboundExceptionRecord.Domain;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Service.Services.ShipmentInboundExceptionRecord
{
    /// <summary>
    /// 貨件回倉異常紀錄查詢與匯出服務。
    /// </summary>
    public class ShipmentInboundExceptionRecordService : _BaseService
    {
        private readonly ShipmentInboundExceptionImageStorageService _imageStorageService;

        public ShipmentInboundExceptionRecordService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, ShipmentInboundExceptionImageStorageService imageStorageService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _imageStorageService = imageStorageService;
        }

        /// <summary>
        /// 依查詢條件取得異常紀錄分頁資料。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>異常紀錄查詢結果與總筆數。</returns>
        public ShipmentInboundExceptionRecordResponse GetData(ShipmentInboundExceptionRecordRequest request)
        {
            request = NormalizeRequest(request);

            {
                var query = BuildQuery(JetfDb, request);
                var totalCount = query.Count();
                var data = query
                    .OrderByDescending(x => x.InboundDate)
                    .ThenBy(x => x.MainNumber)
                    .ThenBy(x => x.TrackingNo)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                FillCustomerNames(data);

                return new ShipmentInboundExceptionRecordResponse
                {
                    Data = data,
                    TotalCount = totalCount
                };
            }
        }

        /// <summary>
        /// 取得異常原因下拉選單資料。
        /// </summary>
        /// <returns>異常原因清單。</returns>
        public List<SelectListModel> GetExceptionReasonList()
        {
            {
                return JetfDb.ShipmentInboundExceptionReasons
                    .AsNoTracking()
                    .OrderBy(x => x.Reason)
                    .Select(x => new SelectListModel
                    {
                        Value = x.Reason,
                        Text = x.Reason
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// 匯出異常件紀錄 Excel 與異常圖片 ZIP。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>ZIP 檔名與檔案內容。</returns>
        public ShipmentInboundExceptionRecordExportResult ExportExcelZip(ShipmentInboundExceptionRecordRequest request)
        {
            request = NormalizeRequest(request);
            request.Page = 1;
            request.PageSize = 100000;

            List<ShipmentInboundExceptionRecordModel> data;
            {
                data = BuildQuery(JetfDb, request)
                    .OrderByDescending(x => x.InboundDate)
                    .ThenBy(x => x.MainNumber)
                    .ThenBy(x => x.TrackingNo)
                    .ToList();

                FillCustomerNames(data);
            }

            var excelBytes = CreateExcel(data);
            var zipBytes = CreateZip(data, excelBytes);

            return new ShipmentInboundExceptionRecordExportResult
            {
                FileName = $"{DateTime.Now:yyyyMMddHHmmss}異常件紀錄.zip",
                FileBytes = zipBytes
            };
        }

        /// <summary>
        /// 建立異常紀錄查詢 IQueryable。
        /// </summary>
        /// <param name="db">Jetf 資料庫內容。</param>
        /// <param name="request">查詢條件。</param>
        /// <returns>套用條件後的異常紀錄查詢。</returns>
        private IQueryable<ShipmentInboundExceptionRecordModel> BuildQuery(
            Data.JetfDbContext db,
            ShipmentInboundExceptionRecordRequest request)
        {
            // 先限制為有異常紀錄的入庫貨件，再套用入庫日期、主號、單號與客戶條件。
            var shipments = db.ShipmentInbounds.AsNoTracking()
                .Where(x => db.ShipmentInboundExceptions.Any(e => e.ShipmentInboundId == x.Id));

            shipments = ApplyShipmentWhereConditions(shipments, request);

            // 異常原因只顯示最新一筆；CreatedTime 相同時用 Id 倒序決定最新資料。
            var query = shipments.Select(x => new ShipmentInboundExceptionRecordModel
            {
                Id = x.Id,
                InboundDate = x.InboundDate,
                DataType = x.DataType,
                MainNumber = x.MainNumber,
                TrackingNo = x.TrackingNo,
                CustCode = x.CustCode,
                ExceptionReasonId = db.ShipmentInboundExceptions
                    .Where(e => e.ShipmentInboundId == x.Id)
                    .OrderByDescending(e => e.CreatedTime)
                    .ThenByDescending(e => e.Id)
                    .Select(e => e.ExceptionReasonId)
                    .FirstOrDefault(),
                ExceptionReason = db.ShipmentInboundExceptions
                    .Where(e => e.ShipmentInboundId == x.Id)
                    .OrderByDescending(e => e.CreatedTime)
                    .ThenByDescending(e => e.Id)
                    .Select(e => e.ExceptionReason.Reason)
                    .FirstOrDefault()
            });

            if (!string.IsNullOrWhiteSpace(request.ExceptionReason))
            {
                query = query.Where(x => x.ExceptionReason.Contains(request.ExceptionReason));
            }

            return query;
        }

        private Dictionary<string, string> GetAirCustNames(IEnumerable<string> custCodes)
        {
            return GetAirCustomerNames(custCodes);
        }

        private Dictionary<string, string> GetSeaCustNames(IEnumerable<string> custCodes)
        {
            return GetSeaCustomerNames(custCodes);
        }

        private void FillCustomerNames(List<ShipmentInboundExceptionRecordModel> data)
        {
            if (data == null || !data.Any())
            {
                return;
            }

            var airCustCodes = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.CustCode))
                .Select(x => x.CustCode)
                .Distinct()
                .ToList();

            var seaCustCodes = data.Where(x => x.DataType == "海運" && !string.IsNullOrWhiteSpace(x.CustCode))
                .Select(x => x.CustCode)
                .Distinct()
                .ToList();

            var airCustNames = GetAirCustNames(airCustCodes);
            var seaCustNames = GetSeaCustNames(seaCustCodes);

            foreach (var item in data)
            {
                if (string.IsNullOrWhiteSpace(item.CustCode))
                {
                    continue;
                }

                if (item.DataType == "空運" && airCustNames.ContainsKey(item.CustCode))
                {
                    item.CustName = airCustNames[item.CustCode];
                    continue;
                }

                if (item.DataType == "海運" && seaCustNames.ContainsKey(item.CustCode))
                {
                    item.CustName = seaCustNames[item.CustCode];
                    continue;
                }

                item.CustName = item.CustCode;
            }
        }

        /// <summary>
        /// 套用入庫貨件欄位查詢條件。
        /// </summary>
        /// <param name="query">入庫貨件查詢。</param>
        /// <param name="request">查詢條件。</param>
        /// <returns>套用條件後的入庫貨件查詢。</returns>
        private IQueryable<Data.ShipmentInboundEntity> ApplyShipmentWhereConditions(
            IQueryable<Data.ShipmentInboundEntity> query,
            ShipmentInboundExceptionRecordRequest request)
        {
            if (DateTime.TryParse(request.InboundDateStart, out var startDate))
            {
                query = query.Where(x => x.InboundDate >= startDate);
            }

            if (DateTime.TryParse(request.InboundDateEnd, out var endDate))
            {
                var inboundDateEnd = endDate.AddDays(1);
                query = query.Where(x => x.InboundDate < inboundDateEnd);
            }

            if (!string.IsNullOrWhiteSpace(request.MainNumber))
            {
                query = query.Where(x => x.MainNumber.Contains(request.MainNumber));
            }

            if (!string.IsNullOrWhiteSpace(request.TrackingNo))
            {
                var trackingNos = SplitLines(request.TrackingNo);

                if (trackingNos.Count == 1)
                {
                    var trackingNo = trackingNos[0];
                    query = query.Where(x => x.TrackingNo.Contains(trackingNo));
                }
                else if (trackingNos.Count > 1)
                {
                    query = query.Where(x => trackingNos.Contains(x.TrackingNo));
                }
            }

            // 客戶多選優先；若未傳多選清單，才使用單一客戶代碼條件。
            var custCodes = request.CustCodes?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (custCodes?.Any() == true)
            {
                query = query.Where(x => custCodes.Contains(x.CustCode));
            }
            else if (!string.IsNullOrWhiteSpace(request.CustCode))
            {
                query = query.Where(x => x.CustCode.Contains(request.CustCode));
            }

            return query;
        }

        /// <summary>
        /// 正規化查詢條件，補上預設分頁值與空集合。
        /// </summary>
        /// <param name="request">原始查詢條件。</param>
        /// <returns>正規化後的查詢條件。</returns>
        private ShipmentInboundExceptionRecordRequest NormalizeRequest(ShipmentInboundExceptionRecordRequest request)
        {
            request = request ?? new ShipmentInboundExceptionRecordRequest();
            request.Page = request.Page <= 0 ? 1 : request.Page;
            request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            request.CustCodes = request.CustCodes ?? new List<string>();
            return request;
        }

        private List<string> SplitLines(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            return value
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 建立異常件紀錄 Excel 檔案內容。
        /// </summary>
        /// <param name="data">匯出資料。</param>
        /// <returns>Excel 檔案內容。</returns>
        private byte[] CreateExcel(List<ShipmentInboundExceptionRecordModel> data)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("異常件紀錄");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            var headers = new List<string> { "客戶", "主號", "單號", "異常原因" };
            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var row = sheet.CreateRow(i + 1);
                NpoiCell.CreateCell(row, 0, item.CustName, dataStyle);
                NpoiCell.CreateCell(row, 1, item.MainNumber, dataStyle);
                NpoiCell.CreateCell(row, 2, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, 3, item.ExceptionReason, dataStyle);
            }

            sheet.AutoSizeColumns(headers.Count, scale: 1.2, minWidth: 18);

            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 建立包含 Excel 與異常圖片資料夾的 ZIP 檔案。
        /// </summary>
        /// <param name="data">匯出資料。</param>
        /// <param name="excelBytes">Excel 檔案內容。</param>
        /// <returns>ZIP 檔案內容。</returns>
        private byte[] CreateZip(
            List<ShipmentInboundExceptionRecordModel> data,
            byte[] excelBytes)
        {
            using (var zipStream = new MemoryStream())
            {
                using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    AddBytes(zip, "異常件紀錄.xlsx", excelBytes);
                    AddImages(zip, data);
                }

                return zipStream.ToArray();
            }
        }

        /// <summary>
        /// 將符合匯出資料的異常圖片加入 ZIP。
        /// </summary>
        /// <param name="zip">目標 ZIP 封存。</param>
        /// <param name="data">匯出資料。</param>
        private void AddImages(ZipArchive zip, List<ShipmentInboundExceptionRecordModel> data)
        {
            if (data == null || !data.Any())
            {
                return;
            }

            var shipmentIds = data.Select(x => x.Id).Distinct().ToList();
            List<ShipmentInboundExceptionImageExportModel> images;

            {
                images = JetfDb.ShipmentInboundExceptions
                    .AsNoTracking()
                    .Where(x => shipmentIds.Contains(x.ShipmentInboundId) && !string.IsNullOrEmpty(x.FilePath))
                    .OrderBy(x => x.CreatedTime)
                    .ThenBy(x => x.Id)
                    .Select(x => new ShipmentInboundExceptionImageExportModel
                    {
                        ShipmentInboundId = x.ShipmentInboundId,
                        ExceptionReasonId = x.ExceptionReasonId,
                        FilePath = x.FilePath
                    })
                    .ToList();
            }

            var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in data)
            {
                // 需求只顯示最新異常原因，因此圖片也優先匯出同一個異常原因的圖片。
                var itemImages = images
                    .Where(x => x.ShipmentInboundId == item.Id && x.ExceptionReasonId == item.ExceptionReasonId)
                    .ToList();

                if (!itemImages.Any())
                {
                    // 歷史資料可能沒有異常原因 Id，找不到同原因圖片時退回該貨件全部圖片。
                    itemImages = images
                        .Where(x => x.ShipmentInboundId == item.Id)
                        .ToList();
                }

                for (int i = 0; i < itemImages.Count; i++)
                {
                    var image = itemImages[i];
                    var fileBytes = _imageStorageService.ReadAllBytes(image.FilePath);
                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        continue;
                    }

                    var extension = _imageStorageService.GetExtension(image.FilePath);
                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        extension = ".jpg";
                    }

                    var entryName = BuildImageEntryName(item, i + 1, extension);
                    entryName = EnsureUniqueEntryName(entryName, usedEntryNames);

                    AddBytes(zip, entryName, fileBytes);
                }
            }
        }

        /// <summary>
        /// 建立異常圖片在 ZIP 內的相對路徑。
        /// </summary>
        /// <param name="item">異常紀錄資料。</param>
        /// <param name="index">同一單號圖片序號。</param>
        /// <param name="extension">圖片副檔名。</param>
        /// <returns>ZIP 內圖片路徑。</returns>
        private string BuildImageEntryName(
            ShipmentInboundExceptionRecordModel item,
            int index,
            string extension)
        {
            var inboundDate = item.InboundDate.ToString("yyyyMMdd");
            var mainNumber = SanitizeZipSegment(item.MainNumber);
            var trackingNo = SanitizeZipSegment(item.TrackingNo);
            var reason = SanitizeZipSegment(item.ExceptionReason);
            var fileName = $"{mainNumber}#{trackingNo}#{reason}({index}){extension}";

            return $"異常圖片/{inboundDate}/{mainNumber}/{fileName}";
        }

        /// <summary>
        /// 清理 ZIP 路徑片段中的非法檔名字元。
        /// </summary>
        /// <param name="value">原始路徑片段。</param>
        /// <returns>可用於 ZIP 檔名的路徑片段。</returns>
        private string SanitizeZipSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "未填";
            }

            // ZIP entry 名稱同時要避開 Windows 檔名非法字元與目錄分隔字元。
            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[] { '/', '\\' })
                .Distinct()
                .ToArray();

            var result = value.Trim();
            foreach (var invalidChar in invalidChars)
            {
                result = result.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(result) ? "未填" : result;
        }

        /// <summary>
        /// 確保 ZIP entry 名稱不重複。
        /// </summary>
        /// <param name="entryName">原始 ZIP entry 名稱。</param>
        /// <param name="usedEntryNames">已使用的 ZIP entry 名稱集合。</param>
        /// <returns>不重複的 ZIP entry 名稱。</returns>
        private string EnsureUniqueEntryName(string entryName, HashSet<string> usedEntryNames)
        {
            if (usedEntryNames.Add(entryName))
            {
                return entryName;
            }

            var directory = Path.GetDirectoryName(entryName)?.Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(entryName);
            var extension = Path.GetExtension(entryName);
            var suffix = 2;
            string candidate;

            do
            {
                candidate = string.IsNullOrWhiteSpace(directory)
                    ? $"{fileName}_{suffix}{extension}"
                    : $"{directory}/{fileName}_{suffix}{extension}";
                suffix++;
            }
            while (!usedEntryNames.Add(candidate));

            return candidate;
        }

        /// <summary>
        /// 將位元組內容寫入 ZIP entry。
        /// </summary>
        /// <param name="zip">目標 ZIP 封存。</param>
        /// <param name="entryName">ZIP entry 名稱。</param>
        /// <param name="bytes">要寫入的檔案內容。</param>
        private void AddBytes(ZipArchive zip, string entryName, byte[] bytes)
        {
            var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
            using (var entryStream = entry.Open())
            {
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
