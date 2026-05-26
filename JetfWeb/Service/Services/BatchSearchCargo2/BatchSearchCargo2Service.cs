using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Services.BatchSearchCargo2.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.BatchSearchCargo2
{
    public class BatchSearchCargo2Service : _BaseService
    {
        public BatchSearchCargo2Service(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public IWorkbook ExportExcel(BatchSearchCargo2Request request, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("無法取得登入者帳號");
            }

            var trackingNos = ParseTrackingNos(request);
            if (!trackingNos.Any())
            {
                throw new ArgumentException("請輸入有效的分提單號");
            }

            var uploadTime = DateTime.Now;
            SaveBatchTrackingNos(trackingNos, uploadTime, userId);

            var reportTable = GetBatchSearchCargoReport(uploadTime, userId);
            return BuildWorkbook(reportTable);
        }

        private static List<string> ParseTrackingNos(BatchSearchCargo2Request request)
        {
            return (request?.TrackingNoList ?? string.Empty)
                .Split(new[] { '\r', '\n', ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void SaveBatchTrackingNos(IReadOnlyCollection<string> trackingNos, DateTime uploadTime, string userId)
        {
            var originalAutoDetectChanges = JetfDb.Configuration.AutoDetectChangesEnabled;

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    JetfDb.Configuration.AutoDetectChangesEnabled = false;
                    JetfDb.BatchSearchCargo2s.AddRange(trackingNos.Select(trackingNo => new BatchSearchCargo2Entity
                    {
                        TrackingNo = trackingNo,
                        UploadTime = uploadTime,
                        UploadOpe = userId
                    }));
                    JetfDb.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    JetfDb.Configuration.AutoDetectChangesEnabled = originalAutoDetectChanges;
                }
            }
        }

        private DataTable GetBatchSearchCargoReport(DateTime uploadTime, string userId)
        {
            var reportTable = new DataTable();

            using (var adapter = new SqlDataAdapter("[jetf].[dbo].[USP_GetBatchSearchCargo]", conn))
            {
                adapter.SelectCommand.CommandTimeout = 600;
                adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = userId;
                adapter.SelectCommand.Parameters.Add("@Upload_Time", SqlDbType.DateTime).Value = uploadTime;
                adapter.Fill(reportTable);
            }

            return reportTable;
        }

        private static IWorkbook BuildWorkbook(DataTable reportTable)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("批量貨況查詢明細表");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var textStyle = NpoiStyle.CreateDataStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook);
            var dateOnlyStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");

            var headers = new[]
            {
                "預計到港日", "倉儲類型", "客戶名稱", "主提單號", "清關袋號", "分提單號", "進倉時間", "出倉時間", "掃貨上車", "接駁公司",
                "拆袋狀態", "派件公司", "派件公司(新)", "物流貨號", "收件人名稱", "收件人電話", "作業時間", "配送進度(最新的)", "客戶外箱號", "客戶訂單號", "尾程單號"
            };
            var widths = new[]
            {
                5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000,
                5000, 5000, 5000, 5000, 5000, 5000, 5000, 15000, 6000, 6000, 5000
            };

            var headerRow = sheet.CreateRow(0);
            for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                NpoiCell.CreateCell(headerRow, columnIndex, headers[columnIndex], headerStyle);
                sheet.SetColumnWidth(columnIndex, widths[columnIndex]);
            }

            for (var rowIndex = 0; rowIndex < reportTable.Rows.Count; rowIndex++)
            {
                var sourceRow = reportTable.Rows[rowIndex];
                var row = sheet.CreateRow(rowIndex + 1);

                CreateDateCell(row, 0, sourceRow["ETA"], dateOnlyStyle, textStyle);
                NpoiCell.CreateCell(row, 1, sourceRow["I_DATA_TYPE"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 2, sourceRow["CUSTOMER"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 3, sourceRow["MAINNUMBER"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 4, sourceRow["BL_NO"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 5, sourceRow["TrackingNo"].ToString(), textStyle);
                CreateDateCell(row, 6, sourceRow["I_SIGN_IN_TIME"], dateStyle, textStyle);
                CreateDateCell(row, 7, sourceRow["I_SIGN_OUT_TIME"], dateStyle, textStyle);
                CreateDateCell(row, 8, sourceRow["CargoUploadTime"], dateStyle, textStyle);
                NpoiCell.CreateCell(row, 9, sourceRow["PdtTransName"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 10, GetSignOutStatus(sourceRow["SignOutTimeCount"]), textStyle);
                NpoiCell.CreateCell(row, 11, sourceRow["TRANS_NAME"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 12, sourceRow["TRANS_NAME_NEW"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 13, sourceRow["DELIVERYNO"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 14, sourceRow["IMPORTER"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 15, sourceRow["IM_PHONENO"].ToString(), textStyle);
                CreateDateCell(row, 16, sourceRow["TRANS_MODIFY_TIME"], dateStyle, textStyle);
                NpoiCell.CreateCell(row, 17, sourceRow["TRANS_STATUS_DESC"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 18, sourceRow["FIELD_X"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 19, sourceRow["ORDER_NO"].ToString(), textStyle);
                NpoiCell.CreateCell(row, 20, sourceRow["EXPRESS_NO"].ToString(), textStyle);
            }

            return workbook;
        }

        private static void CreateDateCell(IRow row, int columnIndex, object value, ICellStyle dateStyle, ICellStyle textStyle)
        {
            if (value != null && DateTime.TryParse(value.ToString(), out var dateValue))
            {
                NpoiCell.CreateDateTimeCell(row, columnIndex, dateValue, dateStyle);
                return;
            }

            NpoiCell.CreateCell(row, columnIndex, value?.ToString() ?? string.Empty, textStyle);
        }

        private static string GetSignOutStatus(object value)
        {
            if (value != null && int.TryParse(value.ToString(), out var signOutTimeCount))
            {
                return signOutTimeCount == 1 ? "未拆" : "有拆";
            }

            return "未拆";
        }
    }
}