using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.CoupangReportForm
{
    public class CoupangReportFormService : _BaseService
    {
        public CoupangReportFormService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public IWorkbook BuildWorkbook(string filePath)
        {
            IWorkbook workbook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(fs);
            }

            // 先定位各工作表的表頭與資料列位置，後續直接回寫原 workbook，避免改變上傳檔案排序。
            var sheets = GetReportSheets(workbook);
            if (!sheets.Any())
            {
                throw new Exception("找不到必要欄位：INVOICEID、CCOUT出艙時間");
            }

            var rows = sheets
                .SelectMany(x => x.Rows)
                .Where(x => !string.IsNullOrWhiteSpace(x.InvoiceId))
                .ToList();

            if (!rows.Any())
            {
                return workbook;
            }

            // 只補需要查詢的欄位值，最後再依原 Sheet/RowIndex 回填到 Excel。
            FillRows(rows);
            ApplyRows(workbook, sheets);
            return workbook;
        }

        private List<CoupangReportSheet> GetReportSheets(IWorkbook workbook)
        {
            var result = new List<CoupangReportSheet>();

            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                // 表頭不固定在第 1 列，所以逐列掃描到必要欄位為止。
                var header = FindHeader(sheet);
                if (header == null)
                {
                    continue;
                }

                var rows = new List<CoupangReportRow>();
                for (var rowIndex = header.HeaderRowIndex + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null)
                    {
                        continue;
                    }

                    var invoiceId = row.GetCellData(header.InvoiceIdColumn);
                    if (string.IsNullOrWhiteSpace(invoiceId))
                    {
                        continue;
                    }

                    // 保留原本 RowIndex，下載時只更新儲存格內容，不搬動資料列。
                    rows.Add(new CoupangReportRow
                    {
                        RowIndex = rowIndex,
                        InvoiceId = invoiceId.Trim(),
                        Ccout = row.GetCellData(header.CcoutColumn),
                        Name = header.NameColumn >= 0 ? row.GetCellData(header.NameColumn) : string.Empty,
                        Phone = header.PhoneColumn >= 0 ? row.GetCellData(header.PhoneColumn) : string.Empty
                    });
                }

                result.Add(new CoupangReportSheet
                {
                    SheetIndex = i,
                    Header = header,
                    Rows = rows
                });
            }

            return result;
        }

        private CoupangReportHeader FindHeader(ISheet sheet)
        {
            if (sheet == null)
            {
                return null;
            }

            for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var header = new CoupangReportHeader
                {
                    HeaderRowIndex = rowIndex,
                    InvoiceIdColumn = -1,
                    CcoutColumn = -1,
                    NameColumn = -1,
                    PhoneColumn = -1
                };

                for (var cellIndex = 0; cellIndex < row.LastCellNum; cellIndex++)
                {
                    // 欄名可能含換行或空白，正規化後比對，避免 Excel 表頭換行造成抓不到欄位。
                    var text = NormalizeHeader(row.GetCellData(cellIndex));
                    if (text == "INVOICEID")
                    {
                        header.InvoiceIdColumn = cellIndex;
                    }
                    else if (text == "CCOUT出艙時間")
                    {
                        header.CcoutColumn = cellIndex;
                    }
                    else if (text == "姓名")
                    {
                        header.NameColumn = cellIndex;
                    }
                    else if (text == "電話")
                    {
                        header.PhoneColumn = cellIndex;
                    }
                }

                if (header.InvoiceIdColumn >= 0 && header.CcoutColumn >= 0)
                {
                    return header;
                }
            }

            return null;
        }

        private void FillRows(List<CoupangReportRow> rows)
        {
            // 需求只要求 CCOUT 出艙時間空白時才查詢，原本有值的列不覆蓋。
            var ccoutEmptyRows = rows
                .Where(x => string.IsNullOrWhiteSpace(x.Ccout))
                .ToList();

            var invoiceIds = ccoutEmptyRows
                .Select(x => x.InvoiceId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!invoiceIds.Any())
            {
                return;
            }

            // 第一優先以 CLEARANCE_INFO.MERGE_NUMBER = INVOICEID 查 SIGN_OUT_TIME。
            var clearanceLookup = GetClearanceLookup(invoiceIds);
            // CLEARANCE_INFO 找不到時，才改查 ORIGINALLIST.TRACKINGNO = INVOICEID。
            var originalInvoiceIds = invoiceIds
                .Where(x => !clearanceLookup.ContainsKey(x))
                .ToList();
            var originalLookup = GetOriginalLookup(originalInvoiceIds);

            foreach (var row in ccoutEmptyRows)
            {
                DateTime? signOutTime;
                if (clearanceLookup.TryGetValue(row.InvoiceId, out signOutTime) && signOutTime.HasValue)
                {
                    row.CcoutTime = signOutTime.Value;
                    continue;
                }

                CoupangOriginalInfo original;
                if (originalLookup.TryGetValue(row.InvoiceId, out original))
                {
                    // ORIGINALLIST 的出艙時間不可用；CLEARANCE_INFO 找不到時，CCOUT 維持空白。
                    // 姓名、電話只補空白欄位；上傳檔原本有值時保留客戶提供的內容。
                    if (string.IsNullOrWhiteSpace(row.Name))
                    {
                        row.Name = original.Recipient;
                    }

                    if (string.IsNullOrWhiteSpace(row.Phone))
                    {
                        row.Phone = original.RecPhone;
                    }
                }
            }
        }

        private Dictionary<string, DateTime?> GetClearanceLookup(List<string> invoiceIds)
        {
            // 使用 WhereBulkContains 建暫存表批次比對，避免大量 IN 條件或逐筆 DB round trip。
            return DataCenterDb.ClearanceInfos
                .AsNoTracking()
                .Where(x => x.SignOutTime != null)
                .WhereBulkContains(DataCenterDb, invoiceIds, x => x.MergeNumber, x => x)
                .Where(x => !string.IsNullOrWhiteSpace(x.MergeNumber))
                .GroupBy(x => x.MergeNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.SignOutTime).Select(y => y.SignOutTime).FirstOrDefault(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, CoupangOriginalInfo> GetOriginalLookup(List<string> invoiceIds)
        {
            if (!invoiceIds.Any())
            {
                return new Dictionary<string, CoupangOriginalInfo>(StringComparer.OrdinalIgnoreCase);
            }

            // 備援查詢只帶回姓名、電話；ORIGINALLIST.SignOutTime 固定空白，不用來補 CCOUT。
            return DataCenterDb.OriginalLists
                .AsNoTracking()
                .WhereBulkContains(DataCenterDb, invoiceIds, x => x.TrackingNo, x => x)
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x =>
                    {
                        var first = x.OrderByDescending(y => y.Id).First();
                        return new CoupangOriginalInfo
                        {
                            Recipient = first.Recipient,
                            RecPhone = first.RecPhone
                        };
                    },
                    StringComparer.OrdinalIgnoreCase);
        }

        private void ApplyRows(IWorkbook workbook, List<CoupangReportSheet> sheets)
        {
            var dateStyle = workbook.CreateCellStyle();
            dateStyle.DataFormat = workbook.CreateDataFormat().GetFormat("yyyy/mm/dd hh:mm:ss");

            // 依原本 SheetIndex/RowIndex 回寫欄位，保留上傳檔案既有排序與其他欄位內容。
            foreach (var sheetData in sheets)
            {
                var sheet = workbook.GetSheetAt(sheetData.SheetIndex);
                foreach (var data in sheetData.Rows)
                {
                    var row = sheet.GetRow(data.RowIndex);
                    if (row == null)
                    {
                        continue;
                    }

                    if (data.CcoutTime.HasValue)
                    {
                        var ccoutCell = row.GetCell(sheetData.Header.CcoutColumn) ?? row.CreateCell(sheetData.Header.CcoutColumn);
                        ccoutCell.SetCellValue(data.CcoutTime.Value);
                        ccoutCell.CellStyle = dateStyle;
                    }

                    if (sheetData.Header.NameColumn >= 0 && !string.IsNullOrWhiteSpace(data.Name))
                    {
                        var nameCell = row.GetCell(sheetData.Header.NameColumn) ?? row.CreateCell(sheetData.Header.NameColumn);
                        nameCell.SetCellValue(data.Name);
                    }

                    if (sheetData.Header.PhoneColumn >= 0 && !string.IsNullOrWhiteSpace(data.Phone))
                    {
                        var phoneCell = row.GetCell(sheetData.Header.PhoneColumn) ?? row.CreateCell(sheetData.Header.PhoneColumn);
                        phoneCell.SetCellValue(data.Phone);
                    }
                }
            }
        }

        private static string NormalizeHeader(string value)
        {
            return new string((value ?? string.Empty)
                .Where(x => !char.IsWhiteSpace(x))
                .ToArray())
                .Trim()
                .ToUpperInvariant();
        }

        private sealed class CoupangReportSheet
        {
            public int SheetIndex { get; set; }
            public CoupangReportHeader Header { get; set; }
            public List<CoupangReportRow> Rows { get; set; }
        }

        private sealed class CoupangReportHeader
        {
            public int HeaderRowIndex { get; set; }
            public int InvoiceIdColumn { get; set; }
            public int CcoutColumn { get; set; }
            public int NameColumn { get; set; }
            public int PhoneColumn { get; set; }
        }

        private sealed class CoupangReportRow
        {
            public int RowIndex { get; set; }
            public string InvoiceId { get; set; }
            public string Ccout { get; set; }
            public DateTime? CcoutTime { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
        }

        private sealed class CoupangOriginalInfo
        {
            public string Recipient { get; set; }
            public string RecPhone { get; set; }
        }
    }
}
