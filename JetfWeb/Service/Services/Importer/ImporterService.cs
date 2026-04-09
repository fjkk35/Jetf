using CompanyRegistrationLibrary;
using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Models.BusinessRegistryNew;
using Service.Models.Importer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;

namespace Service.Services.Importer
{
    public class ImporterService: _BaseService
    {
        public async Task<ResponseModel> Search(ImporterSearchType type ,List<string> list)
        {
            var response = await GetList(type , list);

            return new ResponseModel() { ReturnObject = response };
        }

        /// <summary>
        /// 匯出Excel
        /// </summary>
        /// <param name="type">查詢類型</param>
        /// <param name="list">查詢資料列表</param>
        /// <returns>匯出結果</returns>
        public async Task<ExportResult> ExportExcel(ImporterSearchType type, List<string> list)
        {
            try
            {
                // 取得資料
                var dataList = await GetList(type, list);
                
                // 建立Excel檔案
                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("申報人查詢結果");

                // 設定欄位標題
                IRow headerRow = sheet.CreateRow(0);
                ICell headerCell1 = headerRow.CreateCell(0);
                ICell headerCell2 = headerRow.CreateCell(1);
                headerCell1.SetCellValue(type == ImporterSearchType.Phone ? "手機" : "身份證");
                headerCell2.SetCellValue("申報人");

                // 設定標題樣式
                ICellStyle headerStyle = workbook.CreateCellStyle();
                IFont headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);
                headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                headerStyle.BorderTop = BorderStyle.Thin;
                headerStyle.BorderBottom = BorderStyle.Thin;
                headerStyle.BorderLeft = BorderStyle.Thin;
                headerStyle.BorderRight = BorderStyle.Thin;
                
                headerCell1.CellStyle = headerStyle;
                headerCell2.CellStyle = headerStyle;

                // 設定欄寬
                sheet.SetColumnWidth(0, 5000);
                sheet.SetColumnWidth(1, 8000);

                // 填入資料
                ICellStyle dataStyle = workbook.CreateCellStyle();
                dataStyle.BorderTop = BorderStyle.Thin;
                dataStyle.BorderBottom = BorderStyle.Thin;
                dataStyle.BorderLeft = BorderStyle.Thin;
                dataStyle.BorderRight = BorderStyle.Thin;

                for (int i = 0; i < dataList.Count; i++)
                {
                    IRow dataRow = sheet.CreateRow(i + 1);
                    ICell dataCell1 = dataRow.CreateCell(0);
                    ICell dataCell2 = dataRow.CreateCell(1);
                    
                    dataCell1.SetCellValue(dataList[i].PhoneOrId ?? "");
                    dataCell2.SetCellValue(dataList[i].E_Importer ?? "");
                    
                    dataCell1.CellStyle = dataStyle;
                    dataCell2.CellStyle = dataStyle;
                }

                // 產生檔名
                var fileName = $"申報人查詢結果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // 將Excel轉為byte陣列
                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    
                    return new ExportResult
                    {
                        success = true,
                        fileName = fileName,
                        fileData = fileStream.ToArray(),
                        recordCount = dataList.Count,
                        message = "匯出成功"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    success = false,
                    message = $"匯出失敗：{ex.Message}"
                };
            }
        }

        private async Task<List<ImporterResponse>> GetList(ImporterSearchType type, List<string> list)
        {
            var sql1 = "";
            var sql2 = "";

            if (type == ImporterSearchType.Phone)
            {
                // 第一個 SQL - 從 CptSeaMainNumberDetail 查詢
                sql1 = $@"
                        declare @SearchTable Table
                        ( 
                            RowNum int identity(1,1),
                            PhoneOrId nvarchar(50)
                        )
                        {{0}};
                        select distinct RowNum, PhoneOrId, b.CorrectImporterName as E_IMPORTER 
                        from @SearchTable a
                        OUTER APPLY (
							SELECT TOP 1 *
							FROM jetf.dbo.CptSeaMainNumberDetail d
							WHERE d.CorrectImporterPhone = a.PhoneOrId
							  AND d.UploadOpe > ''
							ORDER BY d.Id DESC
						) b 
                ";

                sql1 = string.Format(sql1, $@"INSERT INTO @SearchTable(PhoneOrId) VALUES {string.Join(",",
                    list.Select(r => $"('{r}')"))};");

                // 第二個 SQL - 從 NAME_CERTIFICATION 查詢
                sql2 = $@"
                        declare @SearchTable Table
                        ( 
                            RowNum int identity(1,1),
                            PhoneOrId nvarchar(50)
                        )
                        {{0}};

                        select a.RowNum,a.PhoneOrId, b.E_IMPORTER 
                        from @SearchTable a
                        OUTER APPLY (
							SELECT TOP 1 *
							FROM DATA_CENTER.dbo.NAME_CERTIFICATION d
							WHERE d.E_IM_PHONENO = a.PhoneOrId
							 AND d.TEL_RESULT1= 'Y' and d.TEL_RESULT3='Y'
							ORDER BY d.CRTDATETIME DESC
						) b 
                ";

                sql2 = string.Format(sql2, $@"INSERT INTO @SearchTable(PhoneOrId) VALUES {string.Join(",",
                    list.Select(r => $"('{r}')"))};");
            }

            if (type == ImporterSearchType.ImporterId)
            {
                // 第一個 SQL - 從 CptSeaMainNumberDetail 查詢（身份證比對邏輯需調整）
                sql1 = $@"
                        declare @SearchTable Table
                        ( 
                            RowNum int identity(1,1),
                            PhoneOrId nvarchar(50)
                        )
                        {{0}};
                        select distinct RowNum, PhoneOrId, b.CorrectImporterName as E_IMPORTER 
                        from @SearchTable a
                        OUTER APPLY (
							SELECT TOP 1 *
							FROM jetf.dbo.CptSeaMainNumberDetail d
							WHERE d.CorrectImporterID = a.PhoneOrId
							  AND d.UploadOpe > ''
							ORDER BY d.Id DESC
						) b
                ";

                sql1 = string.Format(sql1, $@"INSERT INTO @SearchTable(PhoneOrId) VALUES {string.Join(",",
                    list.Select(r => $"('{r}')"))};");

                // 第二個 SQL - 從 NAME_CERTIFICATION 查詢
                sql2 = $@"
                        declare @SearchTable Table
                        ( 
                            RowNum int identity(1,1),
                            PhoneOrId nvarchar(50)
                        )
                        {{0}};

                        select a.RowNum,a.PhoneOrId, b.E_IMPORTER 
                        from @SearchTable a
                        OUTER APPLY (
							SELECT TOP 1 *
							FROM DATA_CENTER.dbo.NAME_CERTIFICATION d
							WHERE d.E_IMPORTER_ID = a.PhoneOrId
							 AND d.ID_RESULT1= 'Y' and d.ID_RESULT3='Y'
							ORDER BY d.CRTDATETIME DESC
						) b 
                    ";

                sql2 = string.Format(sql2, $@"INSERT INTO @SearchTable(PhoneOrId) VALUES {string.Join(",",
                    list.Select(r => $"('{r}')"))};");
            }

            // 執行第一個查詢
            var result1 = conn.Query<ImporterResponse>(sql1, commandTimeout: 300).ToList();

            // 執行第二個查詢
            var result2 = conn.Query<ImporterResponse>(sql2, commandTimeout: 300).ToList();

            // 合併結果，優先使用第一個查詢的結果
            result1.ForEach(r =>
            {
                if (string.IsNullOrEmpty(r.E_Importer))
                {
                    var item = result2.Where(x => r.PhoneOrId == x.PhoneOrId).FirstOrDefault();
                    r.E_Importer = item?.E_Importer;
                }
            });

            return result1;
        }
    }

    /// <summary>
    /// 匯出結果類別
    /// </summary>
    public class ExportResult
    {
        public bool success { get; set; }
        public string fileName { get; set; }
        public byte[] fileData { get; set; }
        public int recordCount { get; set; }
        public string message { get; set; }
    }
}
