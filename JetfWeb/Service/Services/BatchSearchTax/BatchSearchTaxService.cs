using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.BatchSearchTax.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace Service.Services.BatchSearchTax
{
    public class BatchSearchTaxService : _BaseService
    {
        public BatchSearchTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

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

                // 使用 temp table 批量比對物流貨號，避免大量 IN 參數造成 SQL 解析與執行變慢。
                var feeMasters = JetfDb.FeeMasters
                    .AsNoTracking()
                    .Where(x => x.Download == "1")
                    .WhereBulkContains(
                        JetfDb,
                        dlvInvList,
                        row => row.DlvInv,
                        key => key);

                // customer_master 不併入主查詢，改用 FEE_MASTER 查出的客戶與派件代碼另批查回填。
                var customerMasters = GetCustomerMasters(feeMasters);

                // 將 EF entity 轉成前端與匯出共用的查詢結果模型。
                var result = feeMasters.Select(item =>
                {
                    customerMasters.TryGetValue(BuildCompositeKey(item.Customer, item.DlvCom), out var customerMaster);

                    return new FeeMasterModel
                    {
                        Source_Type = item.SourceType,
                        Dlv_Inv = item.DlvInv,
                        Cust_Code = item.Customer,
                        Bag_Number = item.BagNumber,
                        TrackingNo = item.TrackingNo,
                        Main_Number = item.MainNumber,
                        Clearance_Number = item.ClearanceNumber,
                        Tax_Number = item.TaxNumber,
                        Tax_Base = ToDecimal(item.TaxBase),
                        Tax1 = ToDecimal(item.Tax1),
                        Tax2 = ToDecimal(item.Tax2),
                        Ccfee = ToDecimal(item.Ccfee),
                        Cod = ToDecimal(item.Cod),
                        Fee = ToDecimal(item.Fee),
                        Trans_Cod = ToDecimal(item.TransCod),
                        Customer_Cod = ToDecimal(item.CustomerCod),
                        Include_Tax = item.IncludeTax,
                        Dlv_Com = item.DlvCom,
                        To_Dlv_Cod = ToDecimal(item.ToDlvCod),
                        Trans_Name = customerMaster?.TransName ?? item.DlvCom,
                        Customer = customerMaster?.Customer
                    };
                }).ToList();

                // customer_master 找不到客戶名稱時，再用 DATA_CENTER.SYS_CUST 補客戶名稱。
                var customers = GetCustomers(result
                    .Where(r => string.IsNullOrEmpty(r.Customer))
                    .Select(r => r.Cust_Code));

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

        /// <summary>
        /// 依 FEE_MASTER 中的客戶代號與派件物流代號，批量查出 customer_master 對照資料。
        /// </summary>
        /// <param name="feeMasters">本次物流貨號查出的費用主檔資料。</param>
        /// <returns>以「客戶代號|派件物流代號」為 key 的 customer_master 對照表。</returns>
        private Dictionary<string, CustomerMasterEntity> GetCustomerMasters(IEnumerable<FeeMasterEntity> feeMasters)
        {
            // customer_master 筆數不多，這裡只用本次出現的 CustId 一次撈回，再用 TransNo 在記憶體中對應。
            var custIds = (feeMasters ?? Enumerable.Empty<FeeMasterEntity>())
                .Select(x => x.Customer)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!custIds.Any())
            {
                return new Dictionary<string, CustomerMasterEntity>();
            }

            return JetfDb.CustomerMasters
                .AsNoTracking()
                .Where(x => custIds.Contains(x.CustId))
                .ToList()
                .GroupBy(x => BuildCompositeKey(x.CustId, x.TransNo))
                .ToDictionary(group => group.Key, group => group.First());
        }

        /// <summary>
        /// 依客戶代號批量查出 DATA_CENTER.SYS_CUST 的客戶名稱，作為 customer_master 找不到時的 fallback。
        /// </summary>
        /// <param name="custCodes">需補名稱的客戶代號。</param>
        /// <returns>以客戶代號為 key 的客戶名稱對照表。</returns>
        private Dictionary<string, string> GetCustomers(IEnumerable<string> custCodes)
        {
            var codes = (custCodes ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!codes.Any())
            {
                return new Dictionary<string, string>();
            }

            // 只查本次缺名稱的客戶代號，不掃整張 SYS_CUST。
            return DataCenterDb.SysCusts
                .AsNoTracking()
                .WhereBulkContains(
                    DataCenterDb,
                    codes,
                    row => row.CustCode,
                    key => key)
                .GroupBy(x => x.CustCode)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.CustName).FirstOrDefault() ?? string.Empty);
        }

        /// <summary>
        /// 將 FEE_MASTER 的整數金額轉成匯出模型使用的 decimal nullable。
        /// </summary>
        private static decimal? ToDecimal(int? value)
        {
            return value.HasValue ? value.Value : (decimal?)null;
        }

        /// <summary>
        /// 將資料庫中以字串儲存的金額轉成匯出模型使用的 decimal nullable。
        /// </summary>
        private static decimal? ToDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                ? number
                : (decimal?)null;
        }

        /// <summary>
        /// 建立多欄位查詢用的穩定字典 key。
        /// </summary>
        private static string BuildCompositeKey(string left, string right)
        {
            return string.Format("{0}|{1}", left ?? string.Empty, right ?? string.Empty);
        }

    }
}
