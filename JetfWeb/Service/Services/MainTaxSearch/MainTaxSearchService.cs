using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.MainTaxSearch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.MainTaxSearch
{
    public class MainTaxSearchService : _BaseService
    {
        public MainTaxSearchService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 查詢主號稅金資料
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>查詢結果</returns>
        public ResponseModel QueryData(MainTaxSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MainNumberList))
                {
                    return new ResponseModel("請輸入主號");
                }

                var mainNumbers = request.MainNumberList
                    .Split(new[] { '\r', '\n', ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                if (!mainNumbers.Any())
                {
                    return new ResponseModel("請輸入有效的主號");
                }

                string sql = @"
                    with MainNumberTax as (
    select MAIN_NUMBER, CUSTOMER, sum(TAX1+TAX2) as TotalTax from FEE_MASTER
    where MAIN_NUMBER in @MAIN_NUMBER and SOURCE_TYPE ='1'
    group by MAIN_NUMBER, CUSTOMER
),
CesMainOrder as (
	select MAIN_NUMBER,max(CLEARANCE_CP) as CLEARANCE_CP  from DATA_CENTER.dbo.CES_MAIN_ORDER
	where MAIN_NUMBER in @MAIN_NUMBER and type='O'
	group by MAIN_NUMBER
),
SysParam as
(
  SELECT CODE, Name as SourceName FROM [DATA_CENTER].[dbo].[SYS_PARAM]
  where Type='CLEARANCE_CP'
)
select a.MAIN_NUMBER,d.SourceName,CUST_NAME,TotalTax from MainNumberTax a
left join [DATA_CENTER].[dbo].[SYS_CUST] b on a.CUSTOMER = b.CUST_CODE
left join CesMainOrder c on a.MAIN_NUMBER = c.MAIN_NUMBER
left join SysParam d on c.CLEARANCE_CP=d.CODE
";

                var result = conn.Query<MainTaxSearchModel>(sql, new { MAIN_NUMBER = mainNumbers }).ToList();

                return new ResponseModel(result);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>Excel 工作簿</returns>
        public IWorkbook ExportExcel(MainTaxSearchRequest request)
        {
            var queryResult = QueryData(request);
            if (queryResult.status != "success")
            {
                throw new Exception(queryResult.msg);
            }

            var dataList = queryResult.ReturnObject as List<MainTaxSearchModel> ?? new List<MainTaxSearchModel>();

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("主號稅金查詢");

            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            ICellStyle dataStyle = NpoiStyle.CreateDataStyle(workbook);
            ICellStyle numberStyle = NpoiStyle.CreateNumberStyle(workbook);

            string[] headers = new string[] { "客戶", "清關業者", "主號", "稅金" };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            int rowIndex = 1;
            foreach (var item in dataList)
            {
                IRow row = sheet.CreateRow(rowIndex++);
                NpoiCell.CreateCell(row, 0, item.CUST_NAME, dataStyle);
                NpoiCell.CreateCell(row, 1, item.SourceName, dataStyle);
                NpoiCell.CreateCell(row, 2, item.MAIN_NUMBER, dataStyle);
                NpoiCell.CreateDoubleCell(row, 3, (double?)item.TotalTax, numberStyle);
            }

            sheet.AutoSizeColumns(headers.Length, minWidth: 12);

            return workbook;
        }
    }
}
