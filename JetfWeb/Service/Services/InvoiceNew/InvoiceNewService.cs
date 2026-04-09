using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using Service.Models;
using Service.Extensions;
using Dapper;
using Service.Services.InvoiceNew.Domain;

namespace Service.Services.InvoiceNew
{
    public class InvoiceNewService : _BaseService
    {
        /// <summary>
        /// 開立電子發票作業New上傳並產生Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InvoiceWorkNew(string filePath, string fileName, string userId)
        {
            var resopnseModel = new ResponseModel();

            try
            {
                //讀取檔案
                List<InvoiceWorkNewModel> uploadList = ReadExcelInvoiceWorkNew(filePath);

                //驗證資料
                if (uploadList.Count > 0)
                {
                    // 直接產生 Excel 檔案
                    IWorkbook workbook = GenerateInvoiceWorkNewExcel(uploadList);
                    
                    resopnseModel.status = Status.success;
                    resopnseModel.ReturnObject = workbook;
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = $"上傳檔案筆數：{uploadList.Count}";
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        /// <summary>
        /// 讀取開立電子發票作業New上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<InvoiceWorkNewModel> ReadExcelInvoiceWorkNew(string filePath)
        {
            List<InvoiceWorkNewModel> dataList = new List<InvoiceWorkNewModel>();
            bool read = false;

            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    IRow row = sheet.GetRow(i);
                    if (row.GetCell(0) != null)
                    {
                        if (row.GetCellData(0) == "序號")
                        {
                            read = true;
                            continue;
                        }

                        if (read)
                        {
                            string seq = row.GetCellData(0);

                            if (!string.IsNullOrEmpty(seq))
                            {
                                var model = new InvoiceWorkNewModel
                                {
                                    Seq = row.GetCellData(0),
                                    InvoiceDate = row.GetCellData(1),
                                    InvoiceNo = row.GetCellData(2),
                                    TrackingNo = row.GetCellData(3),
                                    Amount = row.GetCellData(4),
                                    Tax = row.GetCellData(5),
                                    TotalAmount = row.GetCellData(6),
                                    ProductName = row.GetCellData(7),
                                    VATTitle = row.GetCellData(8),
                                    VATNo = row.GetCellData(9),
                                    Email = row.GetCellData(10)
                                };

                                dataList.Add(model);
                            }
                        }
                    }
                }
            }

            return dataList;
        }

        /// <summary>
        /// 產生開立電子發票作業New Excel（從 Model List）
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        public IWorkbook GenerateInvoiceWorkNewExcel(List<InvoiceWorkNewModel> dataList)
        {
            // 建立 Workbook
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("開立電子發票作業New");

            // 建立樣式
            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            ICellStyle dataStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);

            // 定義表頭（新順序）
            var headers = new List<string>
            {
                "序號", "資料代號", "發票號碼", "發票日期", "發票時間",
                "應稅發票總銷售額(不含稅金額)", "免稅發票總銷售額", "零稅率發票總銷售額",
                "發票總稅額", "發票總金額(含稅)", "發票列印方式", "發票開立通知方式",
                "收件人Email", "收件人手機", "統一編號", "統編抬頭",
                "發票收件地址-郵遞區號", "發票收件地址-街道路名", "發票收件人",
                "銷售單交易識別碼", "銷售單交易編號", "銷售單交易日期", "銷售單交易時間",
                "發票第一聯說明文字", "發票備註", "通關方式註記", "買受人註記欄",
                "買受人簽署適用零稅率註記", "零稅率原因", "歷史發票傳輸代碼",
                "會員登入帳號", "個人/公司識別碼", "沖帳別", "相關號碼",
                "彙開註記", "扣抵金額", "原幣金額", "匯率", "幣別",
                "銷售項目序號", "銷售品名", "銷售數量", "未稅單價", "品項未稅銷售額",
                "銷稅稅別", "產品描述", "單位", "單一欄位備註", "相關號碼"
            };

            // 建立表頭列
            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            // 設定欄位寬度
            for (int i = 0; i < headers.Count; i++)
            {
                sheet.AutoSizeColumn(i);
            }
            sheet.SetColumnWidth(2, 6000); // 發票號碼欄位

            // 填入資料
            int rowIndex = 1;
            foreach (var data in dataList)
            {
                // M 列（主要資料）
                IRow mRow = sheet.CreateRow(rowIndex);
                NpoiCell.CreateCell(mRow, 0, data.Seq, dataStyle);                    // 序號
                NpoiCell.CreateCell(mRow, 1, "M", dataStyle);                         // 資料代號
                NpoiCell.CreateCell(mRow, 2, data.InvoiceNo, dataStyle);              // 發票號碼
                NpoiCell.CreateCell(mRow, 3, data.InvoiceDate, dataStyle);            // 發票日期
                NpoiCell.CreateCell(mRow, 4, "", dataStyle);                          // 發票時間
                NpoiCell.CreateCell(mRow, 5, data.Amount, dataStyle);                 // 應稅發票總銷售額(不含稅金額)
                NpoiCell.CreateCell(mRow, 6, "0", dataStyle);                         // 免稅發票總銷售額
                NpoiCell.CreateCell(mRow, 7, "0", dataStyle);                         // 零稅率發票總銷售額
                NpoiCell.CreateCell(mRow, 8, data.Tax, dataStyle);                    // 發票總稅額
                NpoiCell.CreateCell(mRow, 9, data.TotalAmount, dataStyle);            // 發票總金額(含稅)
                NpoiCell.CreateCell(mRow, 10, "4", dataStyle);                        // 發票列印方式
                NpoiCell.CreateCell(mRow, 11, "0", dataStyle);                        // 發票開立通知方式
                NpoiCell.CreateCell(mRow, 12, data.Email, dataStyle);                 // 收件人Email
                NpoiCell.CreateCell(mRow, 13, "", dataStyle);                         // 收件人手機
                NpoiCell.CreateCell(mRow, 14, data.VATNo, dataStyle);                 // 統一編號
                NpoiCell.CreateCell(mRow, 15, data.VATTitle, dataStyle);              // 統編抬頭
                
                rowIndex++;

                // D 列（明細資料）
                IRow dRow = sheet.CreateRow(rowIndex);
                NpoiCell.CreateCell(dRow, 0, data.Seq, dataStyle);                    // 序號
                NpoiCell.CreateCell(dRow, 1, "D", dataStyle);                         // 資料代號
                NpoiCell.CreateCell(dRow, 39, "0001", dataStyle);                     // 銷售項目序號
                NpoiCell.CreateCell(dRow, 40, data.ProductName, dataStyle);           // 銷售品名
                NpoiCell.CreateCell(dRow, 41, "1", dataStyle);                        // 銷售數量
                NpoiCell.CreateCell(dRow, 42, data.Amount, dataStyle);                // 未稅單價
                NpoiCell.CreateCell(dRow, 43, data.Amount, dataStyle);                // 品項未稅銷售額
                NpoiCell.CreateCell(dRow, 44, "T", dataStyle);                        // 銷稅稅別
                NpoiCell.CreateCell(dRow, 45, "", dataStyle);                         // 產品描述
                NpoiCell.CreateCell(dRow, 46, "", dataStyle);                         // 單位
                NpoiCell.CreateCell(dRow, 47, data.TrackingNo, dataStyle);            // 單一欄位備註
                NpoiCell.CreateCell(dRow, 48, "", dataStyle);                         // 相關號碼
                
                rowIndex++;
            }

            return workbook;
        }
    }
}
