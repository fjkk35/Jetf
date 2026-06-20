using Dapper;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Models;
using Service.Models.SeaMainNumberShippingDetails;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Service.Services.SeaMainNumberShippingDetails
{
    public class SeaMainNumberShippingDetailsService : _BaseService
    {
        public SeaMainNumberShippingDetailsService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public SeaMainNumberShippingDetailsExportResult Export(string mainNumbersText)
        {
            var result = new SeaMainNumberShippingDetailsExportResult();

            try
            {
                var mainNumbers = ParseMainNumbers(mainNumbersText);
                if (!mainNumbers.Any())
                {
                    throw new InvalidOperationException("請輸入有效的主號");
                }

                var rows = GetRows(mainNumbers);
                if (!rows.Any())
                {
                    throw new InvalidOperationException("查無資料");
                }

                result.Rows = rows;
                var exportTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                result.FileName = string.Format(
                    "{0}-海運主號託運明細表(無稅金)-{1}筆.xlsx",
                    exportTime,
                    rows.Count);
                result.FileBytes = CreateWorkbookBytes(rows);
                result.Files.Add(new SeaMainNumberShippingDetailsDownloadFile
                {
                    FileName = result.FileName,
                    FileBytes = result.FileBytes
                });
                result.Files.Add(new SeaMainNumberShippingDetailsDownloadFile
                {
                    FileName = string.Format(
                        "{0}-海運主號託運明細表(無稅金)_ICms訂單-{1}筆.xlsx",
                        exportTime,
                        rows.Count),
                    FileBytes = CreateICmsOrderWorkbookBytes(rows)
                });
                result.status = Status.success;
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = GetInnermostExceptionMessage(ex);
            }

            return result;
        }

        private List<SeaMainNumberShippingDetailsRow> GetRows(List<string> mainNumbers)
        {
            const string sql = @"
select
    a.MAINNUMBER as MainNumber,
    a.BL_NO as BlNo,
    a.DESPATCH_NAME as DespatchName,
    a.IMPORTER as Importer,
    a.IM_PHONENO as ImPhoneNo,
    a.IM_ADD as ImAdd,
    a.CC as Cc,
    a.GW as Gw,
    a.NW as Nw,
    a.QUANTITY as Quantity,
    a.MEMO as Memo,
    a.JETF_SERIAL as JetfSerial,
    a.TRANS_TAXPAYMENT as TransName
from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL a
where a.MAINNUMBER in @MainNumbers
order by a.MAINNUMBER, a.BL_NO";

            var rows = conn.Query<SeaMainNumberShippingDetailsRow>(sql, new { MainNumbers = mainNumbers }).ToList();

            return GroupByJetfSerial(rows);
        }

        private static List<string> ParseMainNumbers(string mainNumbersText)
        {
            if (string.IsNullOrWhiteSpace(mainNumbersText))
            {
                return new List<string>();
            }

            return mainNumbersText
                .Split(new[] { '\r', '\n', ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<SeaMainNumberShippingDetailsRow> GroupByJetfSerial(List<SeaMainNumberShippingDetailsRow> rows)
        {
            return rows
                .GroupBy(x => x.JetfSerial ?? string.Empty)
                .Select(group => group
                    .OrderByDescending(x => x.Gw ?? 0)
                    .First())
                .ToList();
        }

        private static byte[] CreateWorkbookBytes(IReadOnlyList<SeaMainNumberShippingDetailsRow> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("海運主號託運明細表");
            var header = sheet.CreateRow(0);
            var titles = new[]
            {
                "序號",
                "物流貨號",
                "訂單號",
                "收件人姓名",
                "收件人地址",
                "收件人電話",
                "託運備註",
                "商品別編號",
                "商品數量",
                "才積/重量/總長",
                "代收貨款",
                "指定配送日期",
                "指定配送時間",
                "派件公司"
            };

            for (var column = 0; column < titles.Length; column++)
            {
                header.CreateCell(column).SetCellValue(titles[column]);
                sheet.SetColumnWidth(column, GetColumnWidth(column));
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var item = rows[index];
                var row = sheet.CreateRow(index + 1);
                row.CreateCell(0).SetCellValue(index + 1);
                row.CreateCell(1).SetCellValue(item.JetfSerial ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.BlNo ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.Importer ?? string.Empty);
                row.CreateCell(4).SetCellValue(item.ImAdd ?? string.Empty);
                row.CreateCell(5).SetCellValue(item.ImPhoneNo ?? string.Empty);
                row.CreateCell(6).SetCellValue(item.Memo ?? string.Empty);
                row.CreateCell(7).SetCellValue(string.Empty);
                row.CreateCell(8).SetCellValue(item.Quantity ?? 0);
                row.CreateCell(9).SetCellValue(GetExportGw(item.Gw));
                row.CreateCell(10).SetCellValue(GetCollectAmount(item));
                row.CreateCell(11).SetCellValue(string.Empty);
                row.CreateCell(12).SetCellValue(string.Empty);
                row.CreateCell(13).SetCellValue(item.TransName ?? string.Empty);
            }

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        private static byte[] CreateICmsOrderWorkbookBytes(IReadOnlyList<SeaMainNumberShippingDetailsRow> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("ICms訂單");
            var header = sheet.CreateRow(0);
            var titles = new[]
            {
                "配送單號",
                "客戶單號",
                "配送公司",
                "件數",
                "重量",
                "品名",
                "價值",
                "代收款",
                "發件人",
                "發件人電話",
                "發件人地址",
                "收件人",
                "收件人電話",
                "收件人手機",
                "收件人地址",
                "統編",
                "備註",
                "錯誤提示"
            };

            for (var column = 0; column < titles.Length; column++)
            {
                header.CreateCell(column).SetCellValue(titles[column]);
                sheet.SetColumnWidth(column, GetICmsOrderColumnWidth(column));
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var item = rows[index];
                var row = sheet.CreateRow(index + 1);
                row.CreateCell(0).SetCellValue(item.JetfSerial ?? string.Empty);
                row.CreateCell(1).SetCellValue(item.BlNo ?? string.Empty);
                row.CreateCell(2).SetCellValue(string.Empty);
                row.CreateCell(3).SetCellValue(1);
                SetDecimalCellValue(row, 4, item.Nw);
                row.CreateCell(5).SetCellValue(string.Empty);
                row.CreateCell(6).SetCellValue(string.Empty);
                row.CreateCell(7).SetCellValue(item.Cc ?? 0);
                row.CreateCell(8).SetCellValue(string.Empty);
                row.CreateCell(9).SetCellValue(string.Empty);
                row.CreateCell(10).SetCellValue(string.Empty);
                row.CreateCell(11).SetCellValue(item.Importer ?? string.Empty);
                row.CreateCell(12).SetCellValue(item.ImPhoneNo ?? string.Empty);
                row.CreateCell(13).SetCellValue(string.Empty);
                row.CreateCell(14).SetCellValue(item.ImAdd ?? string.Empty);
                row.CreateCell(15).SetCellValue(string.Empty);
                row.CreateCell(16).SetCellValue(string.Empty);
                row.CreateCell(17).SetCellValue(string.Empty);
            }

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        private static void SetDecimalCellValue(NPOI.SS.UserModel.IRow row, int column, decimal? value)
        {
            if (value.HasValue)
            {
                row.CreateCell(column).SetCellValue(Convert.ToDouble(value.Value));
                return;
            }

            row.CreateCell(column).SetCellValue(string.Empty);
        }

        private static int GetICmsOrderColumnWidth(int column)
        {
            switch (column)
            {
                case 10:
                case 14:
                    return 12000;
                default:
                    return 5000;
            }
        }

        private static int GetColumnWidth(int column)
        {
            switch (column)
            {
                case 0:
                    return 3000;
                case 4:
                    return 12000;
                case 6:
                    return 10000;
                default:
                    return 6000;
            }
        }

        private static double GetCollectAmount(SeaMainNumberShippingDetailsRow row)
        {
            return row.Cc ?? 0;
        }

        private static double GetExportGw(decimal? value)
        {
            if (!value.HasValue || value.Value <= 0)
            {
                return 0;
            }

            if (value.Value < 1)
            {
                return 1;
            }

            return Convert.ToDouble(Math.Floor(value.Value));
        }

        private static string GetInnermostExceptionMessage(Exception exception)
        {
            var current = exception;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }
    }
}
