using Dapper;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Renci.SshNet.Messages;
using Service.Extensions;
using Service.Models;
using Service.Models.SeaTransRecord;
using Service.Models.SeaUnboxingRecord;
using Service.Models.SeaUnreceivedOrder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaTransRecord
{
    public class SeaTransRecordService : _BaseService
    {
        public ResponseModel Upload(string filePath, string userId)
        {
            try
            {
                //讀取Excel 
                var list = ReadExcel(filePath);
                InsertSeaUnboxingRecord(list, userId);
                return new ResponseModel();
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 讀取傳輸異動/紀錄
        /// </summary>
        /// <returns></returns>
        public List<SeaUnreceivedOrderModel> ReadExcel(string filePath)
        {
            bool read = false;
            var list = new List<SeaUnreceivedOrderModel>();

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }

            var sheet = workbook.GetSheetAt(0);

            for (int i = 0; i <= sheet.LastRowNum; i++)  // 直接使用 <=，避免 +1
            {
                var row = sheet.GetRow(i);
                if (row == null) continue; // 若為 null，跳過

                var mainNumber = row.GetCellData(0);
                var bagNumber = row.GetCellData(1);
                var isUpdateApproval = row.GetCellData(23);

                // 判斷表頭行
                if (!read && mainNumber == "航班主號" && bagNumber == "分提單號碼" && isUpdateApproval == "是否需更新預委")
                {
                    read = true;
                    continue;
                }

                // 讀取有效資料
                if (read && !string.IsNullOrEmpty(mainNumber) && !string.IsNullOrEmpty(bagNumber))
                {
                    list.Add(new SeaUnreceivedOrderModel
                    {
                        MainNumber = mainNumber,
                        BagNumber = bagNumber,
                        IsUpdateApproval = isUpdateApproval,
                        ServiceDate = DateTime.TryParse(row.GetCellData(24),out var date) ? date : (DateTime?)null,
                        CorrectImporterName = row.GetCellData(25),
                        CorrectImporterId = row.GetCellData(26),
                        CorrectImporterPhone = row.GetCellData(27),
                        CorrectItemName = row.GetCellData(28),
                        CorrectInvoiceAmount = row.GetCellData(29),
                        ServiceStatus = row.GetCellData(30),
                        ProcessRemark = row.GetCellData(31),
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seaTransRecordList"></param>
        /// <param name="userId"></param>
        private void InsertSeaUnboxingRecord(List<SeaUnreceivedOrderModel> seaTransRecordList, string userId)
        {
            // 有條件更新SQL - 當IsReceiveOrder=1時才更新特定欄位
            var conditionalSql = @"
                UPDATE jetf.dbo.CptSeaMainNumberDetail SET 
                    IsUpdateApproval = @IsUpdateApproval,
                    ServiceDate = CASE WHEN IsReceiveOrder = 0 THEN @ServiceDate ELSE ServiceDate END,
                    CorrectImporterName = CASE WHEN IsReceiveOrder = 0 THEN @CorrectImporterName ELSE CorrectImporterName END,
                    CorrectImporterId = CASE WHEN IsReceiveOrder = 0 THEN @CorrectImporterId ELSE CorrectImporterId END,
                    CorrectImporterPhone = CASE WHEN IsReceiveOrder = 0 THEN @CorrectImporterPhone ELSE CorrectImporterPhone END,
                    CorrectItemName = @CorrectItemName,
                    CorrectInvoiceAmount = @CorrectInvoiceAmount,
                    ServiceStatus = @ServiceStatus,
                    ProcessRemark = @ProcessRemark, 
                    UploadOpe = @UploadOpe, 
                    UploadTime = GETDATE()
                WHERE MainNumber = @MainNumber and BagNumber = @BagNumber;
                ";

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var item in seaTransRecordList)
                    {
                        conn.Execute(conditionalSql, new
                        {
                            MainNumber = item.MainNumber,
                            BagNumber = item.BagNumber,
                            IsUpdateApproval = item.IsUpdateApproval,
                            ServiceDate = item.ServiceDate,
                            CorrectImporterName = item.CorrectImporterName,
                            CorrectImporterId = item.CorrectImporterId,
                            CorrectImporterPhone = item.CorrectImporterPhone,
                            CorrectItemName = item.CorrectItemName,
                            CorrectInvoiceAmount = item.CorrectInvoiceAmount,
                            ServiceStatus = item.ServiceStatus,
                            ProcessRemark = item.ProcessRemark,
                            UploadOpe = userId
                        }, transaction: transaction);
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }

            conn.Close();
        }

    }
}
