using Dapper;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.EtlErrorWork.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Service.Services.EtlErrorWork
{
    public class EtlErrorWorkService : _BaseService
    {
        #region 資料查詢方法

        /// <summary>
        /// 取得空快錯單統計報表資料
        /// </summary>
        public ResponseModel GetEtlErrorWorkReport(string sDate, string eDate)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT * FROM [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] a ");
                sb.Append("WHERE ");
                sb.Append("EXISTS ");
                sb.Append("(SELECT 1 FROM [DATA_CENTER].[dbo].[ETL_PLINK_ERROR_CODE] ");
                sb.Append("WHERE REMARK='空快錯單統計' AND REASON=a.REASON) AND ");
                sb.Append("ISSUEDATE BETWEEN @sDate AND @eDate ");

                var parameters = new
                {
                    sDate = $"{sDate} 00:00:00",
                    eDate = $"{eDate} 23:59:59"
                };

                var data = conn.Query<EtlErrorWorkReportModel>(sb.ToString(), parameters, commandTimeout: 600).ToList();

                foreach (var item in data)
                {
                    item.DATADATE = item.ISSUEDATE?.ToString("yyyy/MM/dd");
                    if (string.IsNullOrEmpty(item.CUST))
                    {
                        item.CUST = "無客戶";
                    }
                }

                return new ResponseModel { ReturnObject = data };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 取得空快錯單統計報表傳輸筆數
        /// </summary>
        public ResponseModel GetEtlErrorWorkReportCount(string sDate, string eDate)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("with ");
                sb.Append("ETL_PLINK_ERROR as ");
                sb.Append("( ");
                sb.Append("select CONVERT(nvarchar(20),ISSUEDATE,23) as ISSUEDATE,CUST,MAWB from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] a ");
                sb.Append("where exists ");
                sb.Append("(select 1 from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR_CODE] ");
                sb.Append("where REMARK='空快錯單統計' and REASON=a.REASON ) and ");
                sb.Append("ISSUEDATE between @sDate and @eDate ");
                sb.Append("group by ISSUEDATE,CUST,MAWB ");
                sb.Append(") ");
                sb.Append("select CUST,ISSUEDATE,MAWB,count(distinct TRACKINGNO) as TOTAL from ETL_PLINK_ERROR a ");
                sb.Append("join (select CONVERT(nvarchar(20),sign_in_time,23) as sign_in_date,MAINNUMBER,TRACKINGNO from [DATA_CENTER].[dbo].[MAKELIST]) b on a.MAWB=b.MAINNUMBER and a.ISSUEDATE=b.sign_in_date ");
                sb.Append("group by CUST,ISSUEDATE,MAWB ");

                var parameters = new
                {
                    sDate = $"{sDate} 00:00:00",
                    eDate = $"{eDate} 23:59:59"
                };

                var data = conn.Query<EtlErrorWorkReportCountModel>(sb.ToString(), parameters, commandTimeout: 600).ToList();

                foreach (var item in data)
                {
                    item.DATADATE = Convert.ToDateTime(item.ISSUEDATE).ToString("yyyy/MM/dd");
                    if (string.IsNullOrEmpty(item.CUST))
                    {
                        item.CUST = "無客戶";
                    }
                }

                return new ResponseModel { ReturnObject = data };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 取得空快錯單明細資料
        /// </summary>
        public ResponseModel GetEtlErrorWorkDetails(string sDate, string eDate)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT a.CUST,a.OUT_TIME,b.sign_in_time,b.sign_out_time,a.HAWB,d.RECIPIENT,d.RECPHONE,a.REASON,a.MAWB,a.BAG_NO,c.DELIVERYDATE,d.FIELD_X,d.ORDER_NO FROM [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] a ");
                sb.Append("LEFT JOIN [DATA_CENTER].[dbo].[MAKELIST] b ON a.MAWB=b.MAINNUMBER AND a.HAWB=b.TRACKINGNO ");
                sb.Append("LEFT JOIN [DATA_CENTER].[dbo].[MAINORDERINFO] c ON a.MAWB=c.MAINNUMBER ");
                sb.Append("LEFT JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] d ON a.HAWB=d.TRACKINGNO ");
                sb.Append("WHERE EXISTS ");
                sb.Append("(SELECT 1 FROM [DATA_CENTER].[dbo].[ETL_PLINK_ERROR_CODE] ");
                sb.Append("WHERE REMARK='空快錯單統計' AND REASON=a.REASON) AND ");
                sb.Append("ISSUEDATE BETWEEN @sDate AND @eDate ");

                var parameters = new
                {
                    sDate = $"{sDate} 00:00:00",
                    eDate = $"{eDate} 23:59:59"
                };

                var data = conn.Query<EtlErrorWorkDetailsModel>(sb.ToString(), parameters, commandTimeout: 600).ToList();

                foreach (var item in data)
                {
                    if (string.IsNullOrEmpty(item.CUST))
                    {
                        item.CUST = "無客戶";
                    }
                }

                return new ResponseModel { ReturnObject = data };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 取得客戶群組列表
        /// </summary>
        public List<object> GetCustomerGroupList()
        {
            try
            {
                string sql = @"
                    SELECT [Id], [GroupName]
                    FROM [jetf].[dbo].[CustomerGroup]
                    ORDER BY [GroupName]";

                var result = conn.Query<dynamic>(sql).Select(r => (object)new
                {
                    r.Id,
                    r.GroupName
                }).ToList();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"取得客戶群組列表失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得客戶群組明細
        /// </summary>
        public List<string> GetCustomerGroupDetail(int groupId)
        {
            try
            {
                string sql = @"
                    SELECT [Cust_Code]
                    FROM [jetf].[dbo].[CustomerGroupDetail]
                    WHERE [CustomerGroupId] = @groupId";

                var result = conn.Query<string>(sql, new { groupId }).ToList();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"取得客戶群組明細失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得所有客戶群組明細 (一次撈完)
        /// </summary>
        public Dictionary<string, List<string>> GetAllCustomerGroupDetails()
        {
            try
            {
                string sql = @"
                    SELECT [CustomerGroupId], [Cust_Code]
                    FROM [jetf].[dbo].[CustomerGroupDetail]";

                var result = conn.Query<dynamic>(sql)
                    .GroupBy(x => (int)x.CustomerGroupId)
                    .ToDictionary(
                        g => g.Key.ToString(),
                        g => g.Select(x => (string)x.Cust_Code).ToList()
                    );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"取得所有客戶群組明細失敗: {ex.Message}");
            }
        }

        #endregion

        #region Excel 產生方法

        /// <summary>
        /// 產生單一客戶的 Excel
        /// </summary>
        public IWorkbook GenerateEtlErrorWorkWorkbook(string custName, string sDate, string eDate)
        {
            IFormatProvider ifp = new CultureInfo("zh-TW", true);
            IWorkbook workbook = new XSSFWorkbook();

            var reportResult = GetEtlErrorWorkReport(sDate, eDate);
            var countResult = GetEtlErrorWorkReportCount(sDate, eDate);
            var detailsResult = GetEtlErrorWorkDetails(sDate, eDate);

            var reportData = reportResult.ReturnObject as List<EtlErrorWorkReportModel>;
            var countData = countResult.ReturnObject as List<EtlErrorWorkReportCountModel>;
            var detailsData = detailsResult.ReturnObject as List<EtlErrorWorkDetailsModel>;

            GenerateEtlErrorWorkReportSheet(workbook, reportData, countData, custName, DateTime.ParseExact(sDate, "yyyy-MM-dd", ifp), DateTime.ParseExact(eDate, "yyyy-MM-dd", ifp));
            GenerateEtlErrorWorkDetailsSheet(workbook, detailsData, custName);

            return workbook;
        }

        /// <summary>
        /// 產生全部客戶的 Excel
        /// </summary>
        public IWorkbook GenerateEtlErrorWorkWorkbookAll(string sDate, string eDate)
        {
            IFormatProvider ifp = new CultureInfo("zh-TW", true);
            IWorkbook workbook = new XSSFWorkbook();

            var reportResult = GetEtlErrorWorkReport(sDate, eDate);
            var countResult = GetEtlErrorWorkReportCount(sDate, eDate);

            var reportData = reportResult.ReturnObject as List<EtlErrorWorkReportModel>;
            var countData = countResult.ReturnObject as List<EtlErrorWorkReportCountModel>;

            var custNames = reportData.Select(x => x.CUST).Distinct().ToList();

            GenerateEtlErrorWorkReportSheetAll(workbook, reportData, countData, DateTime.ParseExact(sDate, "yyyy-MM-dd", ifp), DateTime.ParseExact(eDate, "yyyy-MM-dd", ifp));

            foreach (var custName in custNames)
            {
                GenerateEtlErrorWorkReportSheet(workbook, reportData, countData, custName, DateTime.ParseExact(sDate, "yyyy-MM-dd", ifp), DateTime.ParseExact(eDate, "yyyy-MM-dd", ifp));
            }

            return workbook;
        }

        /// <summary>
        /// 產生多客戶的 Excel
        /// </summary>
        public IWorkbook GenerateEtlErrorWorkWorkbookMultiple(List<string> custNames, string sDate, string eDate)
        {
            IFormatProvider ifp = new CultureInfo("zh-TW", true);
            IWorkbook workbook = new XSSFWorkbook();

            var reportResult = GetEtlErrorWorkReport(sDate, eDate);
            var countResult = GetEtlErrorWorkReportCount(sDate, eDate);
            var detailsResult = GetEtlErrorWorkDetails(sDate, eDate);

            var reportData = reportResult.ReturnObject as List<EtlErrorWorkReportModel>;
            var countData = countResult.ReturnObject as List<EtlErrorWorkReportCountModel>;
            var detailsData = detailsResult.ReturnObject as List<EtlErrorWorkDetailsModel>;

            // 篩選出選定的客戶資料
            var filteredReportData = reportData.Where(x => custNames.Contains(x.CUST)).ToList();
            var filteredCountData = countData.Where(x => custNames.Contains(x.CUST)).ToList();
            var filteredDetailsData = detailsData.Where(x => custNames.Contains(x.CUST)).ToList();

            // 產生統計工作表（包含所有選定的客戶）
            GenerateEtlErrorWorkReportSheetMultiple(workbook, filteredReportData, filteredCountData, custNames,
                DateTime.ParseExact(sDate, "yyyy-MM-dd", ifp), DateTime.ParseExact(eDate, "yyyy-MM-dd", ifp));

            // 產生明細工作表（包含所有選定的客戶）
            GenerateEtlErrorWorkDetailsSheetMultiple(workbook, filteredDetailsData, custNames);

            return workbook;
        }

        /// <summary>
        /// 產生空快錯單統計表（單一客戶）
        /// </summary>
        void GenerateEtlErrorWorkReportSheet(IWorkbook workbook, List<EtlErrorWorkReportModel> reportData, List<EtlErrorWorkReportCountModel> countData, string custName, DateTime sDate, DateTime eDate)
        {
            ISheet sheet = workbook.CreateSheet($"{custName}空快錯單統計");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
            var leftStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd");
            var percentStyle = NpoiStyle.CreateDecimalStyle(workbook, "0.00%");

            IRow row = sheet.CreateRow(0);
            NpoiCell.CreateCell(row, 0, $"捷豐{sDate.ToString("yyyy/MM")}錯單統計表", centerStyle);
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 8));

            row = sheet.CreateRow(1);
            NpoiCell.CreateCell(row, 0, "統計時間: 當日00:00:00-23:59:59", centerStyle);
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 0, 8));

            row = sheet.CreateRow(2);
            var headers = new List<string> { "日期", "客戶", "A03", "B6A", "B6D", "B6E", "B6F", "錯單總計", "傳輸筆數", "錯單%" };
            NpoiCell.CreateHeaderCells(row, headers, headerStyle);

            for (int i = 0; i <= 8; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            int days = Convert.ToInt32((eDate - sDate).TotalDays) + 1;
            int irow = 3;

            for (int i = 0; i < days; i++)
            {
                string dataDate = sDate.AddDays(i).ToString("yyyy/MM/dd");
                var dayData = reportData.Where(x => x.CUST == custName && x.DATADATE == dataDate).ToList();

                if (!dayData.Any())
                {
                    continue;
                }

                row = sheet.CreateRow(irow);
                NpoiCell.CreateDateTimeCell(row, 0, sDate.AddDays(i), dateStyle);
                NpoiCell.CreateCell(row, 1, custName, centerStyle);

                int a03Count = dayData.Count(x => x.REASON == "A03");
                int b6aCount = dayData.Count(x => x.REASON == "B6A");
                int b6dCount = dayData.Count(x => x.REASON == "B6D");
                int b6eCount = dayData.Count(x => x.REASON == "B6E");
                int b6fCount = dayData.Count(x => x.REASON == "B6F");

                NpoiCell.CreateIntCell(row, 2, a03Count, centerStyle);
                NpoiCell.CreateIntCell(row, 3, b6aCount, centerStyle);
                NpoiCell.CreateIntCell(row, 4, b6dCount, centerStyle);
                NpoiCell.CreateIntCell(row, 5, b6eCount, centerStyle);
                NpoiCell.CreateIntCell(row, 6, b6fCount, centerStyle);

                var cell = row.CreateCell(7);
                cell.CellFormula = $"SUM(C{irow + 1}:G{irow + 1})";
                cell.CellStyle = centerStyle;

                var dayCount = countData?.Where(x => x.CUST == custName && x.DATADATE == dataDate)?.Sum(x => x.TOTAL) ?? 0;
                NpoiCell.CreateIntCell(row, 8, dayCount, centerStyle);

                cell = row.CreateCell(9);
                cell.CellFormula = $"H{irow + 1}/I{irow + 1}";
                cell.CellStyle = percentStyle;

                irow++;
            }

            row = sheet.CreateRow(irow);
            NpoiCell.CreateCell(row, 1, "總計", centerStyle);
            for (int col = 2; col <= 8; col++)
            {
                var cell = row.CreateCell(col);
                cell.CellFormula = $"SUM({GetColumnLetter(col)}4:{GetColumnLetter(col)}{irow})";
                cell.CellStyle = centerStyle;
            }
            var percentCell = row.CreateCell(9);
            percentCell.CellFormula = $"H{irow + 1}/I{irow + 1}";
            percentCell.CellStyle = percentStyle;

            irow += 3;
            row = sheet.CreateRow(irow);
            NpoiCell.CreateCell(row, 0, "錯單代碼", leftStyle);
            NpoiCell.CreateCell(row, 1, "代碼定義", leftStyle);

            irow++;
            AddErrorCodeRow(sheet, irow++, "A03", "註冊電話人已經被戶政註銷，需提供其他家人名字及電話做報關，需提供正本委任書+身分證影本", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6A", "申報收貨人未實名或報關業者未具結申請免逐 案檢附報關委任文件；請通知收貨人辦理實名 認證或取得收貨人報關委任", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6D", "申報收貨人姓名與身分證號不符；請查明收貨人真實身分", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6E", "經通知辦理實名認證收貨人未實名或未申報具結申請免逐案檢附報關委任", leftStyle);
            AddErrorCodeRow(sheet, irow, "B6F", "須預先委任", leftStyle);
        }

        /// <summary>
        /// 產生空快錯單統計表（總計）
        /// </summary>
        void GenerateEtlErrorWorkReportSheetAll(IWorkbook workbook, List<EtlErrorWorkReportModel> reportData, List<EtlErrorWorkReportCountModel> countData, DateTime sDate, DateTime eDate)
        {
            ISheet sheet = workbook.CreateSheet("總計錯單");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
            var leftStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd");
            var percentStyle = NpoiStyle.CreateDecimalStyle(workbook, "0.00%");

            IRow row = sheet.CreateRow(0);
            NpoiCell.CreateCell(row, 0, $"捷豐{sDate.ToString("yyyy/MM")}錯單統計表", centerStyle);
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 8));

            row = sheet.CreateRow(1);
            NpoiCell.CreateCell(row, 0, "統計時間: 當日00:00:00-23:59:59", centerStyle);
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 0, 8));

            row = sheet.CreateRow(2);
            var headers = new List<string> { "日期", "客戶", "A03", "B6A", "B6D", "B6E", "B6F", "錯單總計", "傳輸筆數", "錯單%" };
            NpoiCell.CreateHeaderCells(row, headers, headerStyle);

            for (int i = 0; i <= 8; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            int days = Convert.ToInt32((eDate - sDate).TotalDays) + 1;
            int irow = 3;

            for (int i = 0; i < days; i++)
            {
                string dataDate = sDate.AddDays(i).ToString("yyyy/MM/dd");
                var dayData = reportData.Where(x => x.DATADATE == dataDate).ToList();

                if (!dayData.Any())
                {
                    continue;
                }

                row = sheet.CreateRow(irow);
                NpoiCell.CreateDateTimeCell(row, 0, sDate.AddDays(i), dateStyle);
                NpoiCell.CreateCell(row, 1, "合計", centerStyle);

                int a03Count = dayData.Count(x => x.REASON == "A03");
                int b6aCount = dayData.Count(x => x.REASON == "B6A");
                int b6dCount = dayData.Count(x => x.REASON == "B6D");
                int b6eCount = dayData.Count(x => x.REASON == "B6E");
                int b6fCount = dayData.Count(x => x.REASON == "B6F");

                NpoiCell.CreateIntCell(row, 2, a03Count, centerStyle);
                NpoiCell.CreateIntCell(row, 3, b6aCount, centerStyle);
                NpoiCell.CreateIntCell(row, 4, b6dCount, centerStyle);
                NpoiCell.CreateIntCell(row, 5, b6eCount, centerStyle);
                NpoiCell.CreateIntCell(row, 6, b6fCount, centerStyle);

                var cell = row.CreateCell(7);
                cell.CellFormula = $"SUM(C{irow + 1}:G{irow + 1})";
                cell.CellStyle = centerStyle;

                var dayCount = countData.Where(x => x.DATADATE == dataDate).Sum(x => x.TOTAL);
                NpoiCell.CreateIntCell(row, 8, dayCount, centerStyle);

                cell = row.CreateCell(9);
                cell.CellFormula = $"H{irow + 1}/I{irow + 1}";
                cell.CellStyle = percentStyle;

                irow++;
            }

            row = sheet.CreateRow(irow);
            NpoiCell.CreateCell(row, 1, "總計", centerStyle);
            for (int col = 2; col <= 8; col++)
            {
                var cell = row.CreateCell(col);
                cell.CellFormula = $"SUM({GetColumnLetter(col)}4:{GetColumnLetter(col)}{irow})";
                cell.CellStyle = centerStyle;
            }
            var percentCell = row.CreateCell(9);
            percentCell.CellFormula = $"H{irow + 1}/I{irow + 1}";
            percentCell.CellStyle = percentStyle;

            irow += 3;
            row = sheet.CreateRow(irow);
            NpoiCell.CreateCell(row, 0, "錯單代碼", leftStyle);
            NpoiCell.CreateCell(row, 1, "代碼定義", leftStyle);

            irow++;
            AddErrorCodeRow(sheet, irow++, "A03", "註冊電話人已經被戶政註銷，需提供其他家人名字及電話做報關，需提供正本委任書+身分證影本", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6A", "申報收貨人未實名或報關業者未具結申請免逐 案檢附報關委任文件；請通知收貨人辦理實名 認證或取得收貨人報關委任", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6D", "申報收貨人姓名與身分證號不符；請查明收貨人真實身分", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6E", "經通知辦理實名認證收貨人未實名或未申報具結申請免逐案檢附報關委任", leftStyle);
            AddErrorCodeRow(sheet, irow, "B6F", "須預先委任", leftStyle);
        }

        /// <summary>
        /// 產生空快錯單統計表（多客戶整合）
        /// </summary>
        void GenerateEtlErrorWorkReportSheetMultiple(IWorkbook workbook, List<EtlErrorWorkReportModel> reportData, List<EtlErrorWorkReportCountModel> countData, List<string> custNames, DateTime sDate, DateTime eDate)
        {
            ISheet sheet = workbook.CreateSheet("空快錯單統計");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
            var leftStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd");
            var percentStyle = NpoiStyle.CreateDecimalStyle(workbook, "0.00%");

            IRow row = sheet.CreateRow(0);
            NpoiCell.CreateCell(row, 0, $"捷豐{sDate.ToString("yyyy/MM")}錯單統計表", centerStyle);
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 8));

            row = sheet.CreateRow(1);
            NpoiCell.CreateCell(row, 0, "統計時間: 當日00:00:00-23:59:59", centerStyle);
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 0, 8));

            row = sheet.CreateRow(2);
            var headers = new List<string> { "日期", "客戶", "A03", "B6A", "B6D", "B6E", "B6F", "錯單總計", "傳輸筆數", "錯單%" };
            NpoiCell.CreateHeaderCells(row, headers, headerStyle);

            for (int i = 0; i <= 9; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            int days = Convert.ToInt32((eDate - sDate).TotalDays) + 1;
            int irow = 3;
            int totalStartRow = irow + 1;

            // 依照日期和客戶排序
            var sortedData = reportData.OrderBy(x => x.DATADATE).ThenBy(x => x.CUST).ToList();
            var groupedByDateAndCust = sortedData.GroupBy(x => new { x.DATADATE, x.CUST }).ToList();

            foreach (var group in groupedByDateAndCust)
            {
                string dataDate = group.Key.DATADATE;
                string custName = group.Key.CUST;
                var dayData = group.ToList();

                row = sheet.CreateRow(irow);
                NpoiCell.CreateCell(row, 0, dataDate, centerStyle);
                NpoiCell.CreateCell(row, 1, custName, centerStyle);

                int a03Count = dayData.Count(x => x.REASON == "A03");
                int b6aCount = dayData.Count(x => x.REASON == "B6A");
                int b6dCount = dayData.Count(x => x.REASON == "B6D");
                int b6eCount = dayData.Count(x => x.REASON == "B6E");
                int b6fCount = dayData.Count(x => x.REASON == "B6F");

                NpoiCell.CreateIntCell(row, 2, a03Count, centerStyle);
                NpoiCell.CreateIntCell(row, 3, b6aCount, centerStyle);
                NpoiCell.CreateIntCell(row, 4, b6dCount, centerStyle);
                NpoiCell.CreateIntCell(row, 5, b6eCount, centerStyle);
                NpoiCell.CreateIntCell(row, 6, b6fCount, centerStyle);

                var cell = row.CreateCell(7);
                cell.CellFormula = $"SUM(C{irow + 1}:G{irow + 1})";
                cell.CellStyle = centerStyle;

                var dayCount = countData?.Where(x => x.CUST == custName && x.DATADATE == dataDate)?.Sum(x => x.TOTAL) ?? 0;
                NpoiCell.CreateIntCell(row, 8, dayCount, centerStyle);

                cell = row.CreateCell(9);
                cell.CellFormula = $"H{irow + 1}/I{irow + 1}";
                cell.CellStyle = percentStyle;

                irow++;
            }

            // 總計行
            row = sheet.CreateRow(irow);
            NpoiCell.CreateCell(row, 1, "總計", centerStyle);
            for (int col = 2; col <= 8; col++)
            {
                var cell = row.CreateCell(col);
                cell.CellFormula = $"SUM({GetColumnLetter(col)}{totalStartRow}:{GetColumnLetter(col)}{irow})";
                cell.CellStyle = centerStyle;
            }
            var percentCell = row.CreateCell(9);
            percentCell.CellFormula = $"H{irow + 1}/I{irow + 1}";
            percentCell.CellStyle = percentStyle;

            // 錯單代碼說明
            irow += 3;
            row = sheet.CreateRow(irow);
            NpoiCell.CreateCell(row, 0, "錯單代碼", leftStyle);
            NpoiCell.CreateCell(row, 1, "代碼定義", leftStyle);

            irow++;
            AddErrorCodeRow(sheet, irow++, "A03", "註冊電話人已經被戶政註銷，需提供其他家人名字及電話做報關，需提供正本委任書+身分證影本", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6A", "申報收貨人未實名或報關業者未具結申請免逐 案檢附報關委任文件；請通知收貨人辦理實名 認證或取得收貨人報關委任", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6D", "申報收貨人姓名與身分證號不符；請查明收貨人真實身分", leftStyle);
            AddErrorCodeRow(sheet, irow++, "B6E", "經通知辦理實名認證收貨人未實名或未申報具結申請免逐案檢附報關委任", leftStyle);
            AddErrorCodeRow(sheet, irow, "B6F", "須預先委任", leftStyle);
        }

        /// <summary>
        /// 產生空快錯單明細表
        /// </summary>
        void GenerateEtlErrorWorkDetailsSheet(IWorkbook workbook, List<EtlErrorWorkDetailsModel> detailsData, string custName)
        {
            ISheet sheet = workbook.CreateSheet($"{custName}空快錯單明細");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var headerWrapStyle = NpoiStyle.CreateWrapTextStyle(workbook);
            
            var centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd");
            var dateTimeStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd hh:mm:ss");

            IRow row = sheet.CreateRow(0);
            var headers = new List<string>
            {
                "客戶名稱", "ATA(航班抵達日)", "客戶訂單號", "分提單號", "申報人名稱",
                "申報人電話", "客戶外箱號", "主提單號", "錯單代碼", "退運批次號"
            };
            
            for (int i = 0; i < headers.Count; i++)
            {
                NpoiCell.CreateCell(row, i, headers[i], headerStyle);
            }
            NpoiCell.CreateCell(row, 10, "清關結束\n(=出倉時間)", headerWrapStyle);

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 7000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 7000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);

            var custData = detailsData.Where(x => x.CUST == custName).ToList();

            int rowIndex = 1;
            foreach (var data in custData)
            {
                row = sheet.CreateRow(rowIndex++);
                NpoiCell.CreateCell(row, 0, data.CUST, centerStyle);
                NpoiCell.CreateDateTimeCell(row, 1, data.DELIVERYDATE, dateStyle);
                NpoiCell.CreateCell(row, 2, data.ORDER_NO, centerStyle);
                NpoiCell.CreateCell(row, 3, data.HAWB, centerStyle);
                NpoiCell.CreateCell(row, 4, data.RECIPIENT, centerStyle);
                NpoiCell.CreateCell(row, 5, data.RECPHONE, centerStyle);
                NpoiCell.CreateCell(row, 6, data.FIELD_X, centerStyle);
                NpoiCell.CreateCell(row, 7, data.MAWB, centerStyle);
                NpoiCell.CreateCell(row, 8, data.REASON, centerStyle);
                NpoiCell.CreateCell(row, 9, "", centerStyle);
                NpoiCell.CreateDateTimeCell(row, 10, data.sign_out_time, dateTimeStyle);
            }
        }

        /// <summary>
        /// 產生空快錯單明細表（多客戶整合）
        /// </summary>
        void GenerateEtlErrorWorkDetailsSheetMultiple(IWorkbook workbook, List<EtlErrorWorkDetailsModel> detailsData, List<string> custNames)
        {
            ISheet sheet = workbook.CreateSheet("空快錯單明細");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var headerWrapStyle = NpoiStyle.CreateHeaderWrapTextStyle(workbook);
            var centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd");
            var dateTimeStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd hh:mm:ss");

            IRow row = sheet.CreateRow(0);
            var headers = new List<string>
            {
                "客戶名稱", "ATA(航班抵達日)", "客戶訂單號", "分提單號", "申報人名稱",
                "申報人電話", "客戶外箱號", "主提單號", "錯單代碼", "退運批次號", "清關結束\n(=出倉時間)"
            };
            NpoiCell.CreateHeaderCells(row, headers, headerStyle);
            NpoiCell.GetCell(row, 10).CellStyle = headerWrapStyle;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 7000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 7000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);

            // 依照客戶排序
            var sortedData = detailsData.Where(x => custNames.Contains(x.CUST))
                                       .OrderBy(x => x.CUST)
                                       .ThenBy(x => x.DELIVERYDATE)
                                       .ToList();

            int rowIndex = 1;
            foreach (var data in sortedData)
            {
                row = sheet.CreateRow(rowIndex++);
                NpoiCell.CreateCell(row, 0, data.CUST, centerStyle);
                NpoiCell.CreateDateTimeCell(row, 1, data.DELIVERYDATE, dateStyle);
                NpoiCell.CreateCell(row, 2, data.ORDER_NO, centerStyle);
                NpoiCell.CreateCell(row, 3, data.HAWB, centerStyle);
                NpoiCell.CreateCell(row, 4, data.RECIPIENT, centerStyle);
                NpoiCell.CreateCell(row, 5, data.RECPHONE, centerStyle);
                NpoiCell.CreateCell(row, 6, data.FIELD_X, centerStyle);
                NpoiCell.CreateCell(row, 7, data.MAWB, centerStyle);
                NpoiCell.CreateCell(row, 8, data.REASON, centerStyle);
                NpoiCell.CreateCell(row, 9, "", centerStyle);
                NpoiCell.CreateDateTimeCell(row, 10, data.sign_out_time, dateTimeStyle);
            }
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 新增錯誤代碼說明行
        /// </summary>
        private void AddErrorCodeRow(ISheet sheet, int rowIndex, string code, string description, ICellStyle style)
        {
            IRow row = sheet.CreateRow(rowIndex);
            NpoiCell.CreateCell(row, 0, code, style);
            NpoiCell.CreateCell(row, 1, description, style);
        }

        /// <summary>
        /// 取得欄位字母 (例如: 0->A, 1->B, 25->Z, 26->AA)
        /// </summary>
        private string GetColumnLetter(int column)
        {
            string columnLetter = "";
            while (column >= 0)
            {
                columnLetter = (char)('A' + (column % 26)) + columnLetter;
                column = column / 26 - 1;
            }
            return columnLetter;
        }

        #endregion
    }
}
