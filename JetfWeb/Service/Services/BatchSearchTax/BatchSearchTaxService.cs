using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.BatchSearchTax.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.BatchSearchTax
{
    public class BatchSearchTaxService : _BaseService
    {
        /// <summary>
        /// 批量查詢稅金資料
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>查詢結果</returns>
        public ResponseModel QueryTaxData(BatchSearchTaxRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.DlvInvList))
                {
                    return new ResponseModel("請輸入物流貨號");
                }

                // 分割物流貨號列表（支援換行、逗號、空白分隔）
                var dlvInvList = request.DlvInvList
                    .Split(new[] { '\r', '\n', ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                if (!dlvInvList.Any())
                {
                    return new ResponseModel("請輸入有效的物流貨號");
                }

                // 查詢資料
                string sql = @"
                    SELECT 
                        SOURCE_TYPE,
                        DLV_INV as Dlv_Inv,
                        a.CUSTOMER as Cust_Code,
                        BAG_NUMBER as Bag_Number,
                        TRACKINGNO as TrackingNo,
                        MAIN_NUMBER as Main_Number,
                        CLEARANCE_NUMBER as Clearance_Number,
                        TAX_NUMBER as Tax_Number,
                        TAX_BASE as Tax_Base,
                        TAX1,
                        TAX2,
                        CCFEE as Ccfee,
                        COD as Cod,
                        FEE as Fee,
                        TRANS_COD as Trans_Cod,
                        CUSTOMER_COD as Customer_Cod,
                        a.INCLUDE_TAX as Include_Tax,
                        DLV_COM as Dlv_Com,
                        TO_DLV_COD as To_Dlv_Cod,
                        isnull(b.TRANS_NAME,DLV_COM) as Trans_Name,
                        b.CUSTOMER as Customer
                    FROM jetf.dbo.FEE_MASTER a
                    LEFT JOIN [jetf].[dbo].[customer_master] b ON a.CUSTOMER = b.CUST_ID AND a.DLV_COM = b.TRANS_NO
                    WHERE DLV_INV IN @DlvInvList AND Download='1'";

                var result = conn.Query<FeeMasterModel>(sql, new { DlvInvList = dlvInvList }).ToList();

                var customers = GetCustomers();

                //客戶
                foreach (var item in result.Where(r => string.IsNullOrEmpty(r.Customer)))
                {
                    item.Customer = customers.ContainsKey(item.Cust_Code) ? customers[item.Cust_Code] : item.Cust_Code;
                }

                //海運
                var type = new string[]{"1","2" };
                foreach (var item in result.Where(r => type.Contains(r.Source_Type)))
                {
                    item.Bag_Number = item.TrackingNo;
                    item.TrackingNo = item.Dlv_Inv;
                }

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
        public IWorkbook ExportExcel(BatchSearchTaxRequest request)
        {
            // 查詢資料
            var queryResult = QueryTaxData(request);
            if (queryResult.status != "success")
            {
                throw new Exception(queryResult.msg);
            }

            var dataList = queryResult.ReturnObject as List<FeeMasterModel> ?? new List<FeeMasterModel>();

            // 建立工作簿
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("批量稅金查詢");

            // 建立樣式
            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            ICellStyle dataStyle = NpoiStyle.CreateDataStyle(workbook);
            ICellStyle numberStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Right);

            // 建立標題列
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = new string[]
            {
                "物流貨號", "客戶", "清關袋號", "分提單號", "主號", "報單號碼", "稅單號碼",
                "稅基", "稅金1", "稅金2", "報關費", "到付款", "手續費",
                "跟派件收", "跟廠商收", "是否包稅", "派件公司", "物流代收貨款金額"
            };

            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            // 填充資料
            int rowIndex = 1;
            foreach (var item in dataList)
            {
                IRow row = sheet.CreateRow(rowIndex++);

                // 字串欄位
                NpoiCell.CreateCell(row, 0, item.Dlv_Inv, dataStyle);          // 物流貨號
                NpoiCell.CreateCell(row, 1, item.Customer, dataStyle);         // 客戶
                NpoiCell.CreateCell(row, 2, item.Bag_Number, dataStyle);       // 清關袋號
                NpoiCell.CreateCell(row, 3, item.TrackingNo, dataStyle);       // 分提單號
                NpoiCell.CreateCell(row, 4, item.Main_Number, dataStyle);      // 主號
                NpoiCell.CreateCell(row, 5, item.Clearance_Number, dataStyle); // 報單號碼
                NpoiCell.CreateCell(row, 6, item.Tax_Number, dataStyle);       // 稅單號碼

                // 數值欄位
                NpoiCell.CreateDoubleCell(row, 7, (double?)item.Tax_Base, numberStyle);      // 稅基
                NpoiCell.CreateDoubleCell(row, 8, (double?)item.Tax1, numberStyle);          // 稅金1
                NpoiCell.CreateDoubleCell(row, 9, (double?)item.Tax2, numberStyle);          // 稅金2
                NpoiCell.CreateDoubleCell(row, 10, (double?)item.Ccfee, numberStyle);        // 報關費
                NpoiCell.CreateDoubleCell(row, 11, (double?)item.Cod, numberStyle);          // 到付款
                NpoiCell.CreateDoubleCell(row, 12, (double?)item.Fee, numberStyle);          // 手續費
                NpoiCell.CreateDoubleCell(row, 13, (double?)item.Trans_Cod, numberStyle);    // 跟派件收
                NpoiCell.CreateDoubleCell(row, 14, (double?)item.Customer_Cod, numberStyle); // 跟廠商收

                // 字串欄位
                NpoiCell.CreateCell(row, 15, item.Include_Tax, dataStyle);     // 是否包稅
                NpoiCell.CreateCell(row, 16, item.Trans_Name, dataStyle);      // 派件公司

                // 數值欄位
                NpoiCell.CreateDoubleCell(row, 17, (double?)item.To_Dlv_Cod, numberStyle);  // 物流代收貨款金額
            }

            // 自動調整欄寬
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
                // 設定最小寬度
                if (sheet.GetColumnWidth(i) < 3000)
                {
                    sheet.SetColumnWidth(i, 3000);
                }
            }

            return workbook;
        }

        private Dictionary<string, string> GetCustomers()
        {
            var sql = @"select CUST_CODE, CUST_NAME from DATA_CENTER.dbo.SYS_CUST";
            var rows = conn.Query(sql); // IEnumerable<dynamic>

            return rows.ToDictionary(
                r => (string)r.CUST_CODE,
                r => (string)r.CUST_NAME
            );
        }

    }
}
