using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Models;
using Service.Models.SeaCustomerShippingDetails;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.SeaCustomerShippingDetails
{
    public class SeaCustomerShippingDetailsService : _BaseService
    {
        public SeaCustomerShippingDetailsService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public SeaCustomerShippingDetailsExportResult Export(string dataType, string despatchName, string startDateText, string endDateText)
        {
            var result = new SeaCustomerShippingDetailsExportResult();

            try
            {
                if (string.IsNullOrWhiteSpace(dataType))
                {
                    throw new InvalidOperationException("請選擇倉別");
                }

                if (string.IsNullOrWhiteSpace(despatchName))
                {
                    throw new InvalidOperationException("請選擇客戶");
                }

                if (!DateTime.TryParse(startDateText, out var startDate) ||
                    !DateTime.TryParse(endDateText, out var endDate))
                {
                    throw new InvalidOperationException("請選擇出倉日");
                }

                if (startDate.Date > endDate.Date)
                {
                    throw new InvalidOperationException("出倉日起日不可大於迄日");
                }

                var custCode = despatchName.Trim();
                var rows = GetRows(dataType.Trim(), custCode, startDate.Date, endDate.Date.AddDays(1));
                var customerFileName = GetCustomerFileName(custCode);
                result.Rows = rows;
                var fileNamePrefix = string.Format(
                    "{0}~{1}",
                    startDate.ToString("yyyyMMdd"),
                    endDate.ToString("yyyyMMdd"));
                result.FileName = string.Format(
                    "{0}-海運客戶託運明細表-{1}-{2}筆.xlsx",
                    fileNamePrefix,
                    customerFileName,
                    rows.Count);
                result.FileBytes = CreateWorkbookBytes(rows);
                result.Files.Add(new SeaCustomerShippingDetailsDownloadFile
                {
                    FileName = result.FileName,
                    FileBytes = result.FileBytes
                });
                result.Files.Add(new SeaCustomerShippingDetailsDownloadFile
                {
                    FileName = string.Format(
                        "{0}-海運客戶託運明細表_ICms訂單-{1}-{2}筆.xlsx",
                        fileNamePrefix,
                        customerFileName,
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

        private List<SeaCustomerShippingDetailsRow> GetRows(string dataType, string despatchName, DateTime startTime, DateTime endTime)
        {
            const string sql = @"
select
    a.MAIN_NUMBER as MainNumber,
    a.BAG_NUMBER as BagNumber,
    b.DESPATCH_NAME as DespatchName,
    b.IMPORTER as Importer,
    b.IM_PHONENO as ImPhoneNo,
    b.IM_ADD as ImAdd,
    b.CC as Cc,
    b.GW as Gw,
    b.NW as Nw,
    b.QUANTITY as Quantity,
    b.MEMO as Memo,
    b.JETF_SERIAL as JetfSerial,
    b.TRANS_TAXPAYMENT as TransName,
    c.TO_DLV_COD as ToDlvCod
from DATA_CENTER.dbo.CLEARANCE_INFO a
left join DATA_CENTER.dbo.SEA_ORDER_ORIGINAL b on a.MAIN_NUMBER = b.MAINNUMBER and a.BAG_NUMBER = b.BL_NO
left join jetf.dbo.FEE_MASTER c on c.DLV_INV = b.JETF_SERIAL and c.Download=1
where a.SIGN_OUT_TIME between @StartTime and @EndTime
and a.DATA_TYPE = @DataType
and b.DESPATCH_NAME = @DespatchName
order by a.SIGN_OUT_TIME, a.MAIN_NUMBER, a.BAG_NUMBER";

            var rows = conn.Query<SeaCustomerShippingDetailsRow>(sql, new
            {
                StartTime = startTime,
                EndTime = endTime,
                DataType = dataType,
                DespatchName = despatchName
            }).ToList();

            return GroupByJetfSerial(rows);
        }

        private static List<SeaCustomerShippingDetailsRow> GroupByJetfSerial(List<SeaCustomerShippingDetailsRow> rows)
        {
            return rows
                .GroupBy(x => x.JetfSerial ?? string.Empty)
                .Select(group =>
                {
                    return group
                        .OrderByDescending(x => x.Gw ?? 0)
                        .First();
                })
                .ToList();
        }

        private string GetCustomerFileName(string custCode)
        {
            var custName = DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == "SEA" && x.CustCode == custCode)
                .Select(x => x.CustName)
                .FirstOrDefault();

            var fileName = string.IsNullOrWhiteSpace(custName)
                ? custCode
                : custName.Trim();

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }

        private static byte[] CreateWorkbookBytes(IReadOnlyList<SeaCustomerShippingDetailsRow> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("海運客戶託運明細表");
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
                row.CreateCell(2).SetCellValue(item.BagNumber ?? string.Empty);
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

        private static byte[] CreateICmsOrderWorkbookBytes(IReadOnlyList<SeaCustomerShippingDetailsRow> rows)
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
                row.CreateCell(1).SetCellValue(item.BagNumber ?? string.Empty);
                row.CreateCell(2).SetCellValue(string.Empty);
                row.CreateCell(3).SetCellValue(1);
                SetDecimalCellValue(row, 4, item.Nw);
                row.CreateCell(5).SetCellValue(string.Empty);
                row.CreateCell(6).SetCellValue(string.Empty);
                row.CreateCell(7).SetCellValue(GetCollectAmount(item));
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

        private static void SetDecimalCellValue(IRow row, int column, decimal? value)
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

        private static double GetCollectAmount(SeaCustomerShippingDetailsRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.ToDlvCod) &&
                double.TryParse(row.ToDlvCod, NumberStyles.Any, CultureInfo.InvariantCulture, out var toDlvCod))
            {
                return toDlvCod;
            }

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
