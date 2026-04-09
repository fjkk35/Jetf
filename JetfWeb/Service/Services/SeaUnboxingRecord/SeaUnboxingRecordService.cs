using Service.Models.Shenzhen;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using Service.Models.SeaUnboxingRecord;
using iTextSharp.text;
using Renci.SshNet;
using static NPOI.HSSF.Util.HSSFColor;
using Service.Extensions;
using Dapper;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Service.Services.SeaUnboxingRecord
{
    public class SeaUnboxingRecordService : _BaseService
    {

        public ResponseModel Upload(string filePath, string userId)
        {
            try
            {
                //讀取Excel 
                ReadExcel(filePath, userId);
                return new ResponseModel();
            }
            catch (UploadValidationException ex)
            {
                return new ResponseModel
                {
                    status = Status.error,
                    msg = "上傳失敗，請修正以下資料：",
                    ReturnObject = ex.Errors
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        void ReadExcel(string filePath, string userId)
        {
            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }

            // 先驗證三個頁籤全部資料，任何一筆有問題就整批中止，不進資料庫。
            var errors = new List<string>();

            //主號拆櫃日
            var seaUnboxingRecordList = ReadSeaUnboxingRecordSheet(GetRequiredSheet(workbook, "主號拆櫃日"), errors);

            //現場有貨
            var seaSiteCargoList = ReadSeaSiteCargoSheet(GetRequiredSheet(workbook, "現場有貨"), errors);

            //短到
            var seaShortCargoList = ReadSeaShortCargoSheet(GetRequiredSheet(workbook, "短到"), errors);

            if (errors.Any())
            {
                throw new UploadValidationException(errors);
            }

            SaveSeaUnboxingData(seaUnboxingRecordList, seaSiteCargoList, seaShortCargoList, userId);
        }

        /// <summary>
        /// 讀取主號拆櫃日頁籤
        /// </summary>
        /// <returns></returns>
        public List<SeaUnboxingRecordModel> ReadSeaUnboxingRecordSheet(ISheet sheet)
        {
            return ReadSeaUnboxingRecordSheet(sheet, new List<string>());
        }

        private List<SeaUnboxingRecordModel> ReadSeaUnboxingRecordSheet(ISheet sheet, List<string> errors)
        {
            bool read = false;
            var list = new List<SeaUnboxingRecordModel>();
            int recordIndex = 0;

            for (int i = 0; i <= sheet.LastRowNum; i++)  // 直接使用 <=，避免 +1
            {
                var row = sheet.GetRow(i);
                if (row == null) continue; // 若為 null，跳過

                var mainNumber = row.GetCellData(0)?.Trim();
                var dataDate = row.GetCellData(1)?.Trim();

                // 判斷表頭行
                if (!read && mainNumber == "主號" && dataDate == "拆櫃日期")
                {
                    read = true;
                    continue;
                }

                if (!read)
                {
                    continue;
                }

                if (IsRowEmpty(row, 0, 1))
                {
                    continue;
                }

                recordIndex++;

                var missingFields = new List<string>();
                AddRequiredField(missingFields, "主號", mainNumber);
                AddRequiredField(missingFields, "拆櫃日期", dataDate);

                if (missingFields.Any())
                {
                    errors.Add(BuildRequiredFieldMessage("主號拆櫃日", recordIndex, missingFields));
                    continue;
                }

                // 必填欄位通過後，再檢查日期格式是否可被系統解析。
                AddInvalidDateError(errors, "主號拆櫃日", recordIndex, "拆櫃日期", dataDate);

                // 讀取有效資料
                if (!string.IsNullOrEmpty(mainNumber) && !string.IsNullOrEmpty(dataDate))
                {
                    list.Add(new SeaUnboxingRecordModel
                    {
                        MainNumber = mainNumber,
                        DataDate = dataDate
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// 讀取現場有貨
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        public List<SeaSiteCargoModel> ReadSeaSiteCargoSheet(ISheet sheet)
        {
            return ReadSeaSiteCargoSheet(sheet, new List<string>());
        }

        private List<SeaSiteCargoModel> ReadSeaSiteCargoSheet(ISheet sheet, List<string> errors)
        {
            bool read = false;
            var list = new List<SeaSiteCargoModel>();
            int recordIndex = 0;

            for (int i = 0; i <= sheet.LastRowNum; i++)  // 直接使用 <=，避免 +1
            {
                var row = sheet.GetRow(i);
                if (row == null) continue; // 若為 null，跳過

                var dataDate = row.GetCellData(0)?.Trim();
                var dataType = row.GetCellData(1)?.Trim();
                var mainNumber = row.GetCellData(2)?.Trim();
                var bagNumber = row.GetCellData(3)?.Trim();
                var jetfSerial = row.GetCellData(4)?.Trim();
                var piece = row.GetCellData(5)?.Trim();

                // 判斷表頭行
                if (!read && dataDate == "現場通知日期" && dataType == "倉儲")
                {
                    read = true;
                    continue;
                }

                if (!read)
                {
                    continue;
                }

                if (IsRowEmpty(row, 0, 1, 2, 3, 4, 5))
                {
                    continue;
                }

                recordIndex++;

                var missingFields = new List<string>();
                AddRequiredField(missingFields, "現場通知日期", dataDate);
                AddRequiredField(missingFields, "倉儲", dataType);
                AddRequiredField(missingFields, "主號", mainNumber);
                AddRequiredField(missingFields, "分號", bagNumber);

                if (missingFields.Any())
                {
                    errors.Add(BuildRequiredFieldMessage("現場有貨", recordIndex, missingFields));
                    continue;
                }

                AddInvalidDateError(errors, "現場有貨", recordIndex, "現場通知日期", dataDate);

                // 讀取有效資料
                if (!string.IsNullOrEmpty(mainNumber) && !string.IsNullOrEmpty(bagNumber))
                {
                    list.Add(new SeaSiteCargoModel
                    {
                        DataDate = dataDate,
                        DataType = dataType,
                        MainNumber = mainNumber,
                        BagNumber = bagNumber,
                        JetfSerial = jetfSerial,
                        Piece = piece,
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// 讀取短到
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        public List<SeaShortCargoModel> ReadSeaShortCargoSheet(ISheet sheet)
        {
            return ReadSeaShortCargoSheet(sheet, new List<string>());
        }

        private List<SeaShortCargoModel> ReadSeaShortCargoSheet(ISheet sheet, List<string> errors)
        {
            bool read = false;
            var list = new List<SeaShortCargoModel>();
            int recordIndex = 0;

            for (int i = 0; i <= sheet.LastRowNum; i++)  // 直接使用 <=，避免 +1
            {
                var row = sheet.GetRow(i);
                if (row == null) continue; // 若為 null，跳過

                var dataType = row.GetCellData(0)?.Trim();
                var mainNumber = row.GetCellData(1)?.Trim();
                var bagNumber = row.GetCellData(2)?.Trim();
                var dataDate = row.GetCellData(3)?.Trim();
                var piece = row.GetCellData(4)?.Trim();

                // 判斷表頭行
                if (!read && mainNumber == "主號" && bagNumber == "分號")
                {
                    read = true;
                    continue;
                }

                if (!read)
                {
                    continue;
                }

                if (IsRowEmpty(row, 0, 1, 2, 3, 4))
                {
                    continue;
                }

                recordIndex++;

                var missingFields = new List<string>();
                AddRequiredField(missingFields, "倉儲", dataType);
                AddRequiredField(missingFields, "主號", mainNumber);
                AddRequiredField(missingFields, "分號", bagNumber);
                AddRequiredField(missingFields, "開立短到單日期", dataDate);

                if (missingFields.Any())
                {
                    errors.Add(BuildRequiredFieldMessage("短到", recordIndex, missingFields));
                    continue;
                }

                AddInvalidDateError(errors, "短到", recordIndex, "開立短到單日期", dataDate);

                // 讀取有效資料
                if (!string.IsNullOrEmpty(mainNumber) && !string.IsNullOrEmpty(bagNumber))
                {
                    list.Add(new SeaShortCargoModel
                    {
                        DataType = dataType,
                        MainNumber = mainNumber,
                        BagNumber = bagNumber,
                        DataDate = dataDate,
                        Piece = piece,
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// 新增主號拆櫃日
        /// </summary>
        /// <param name="seaUnboxingRecordList"></param>
        /// <param name="userId"></param>
        private void SaveSeaUnboxingData(
            List<SeaUnboxingRecordModel> seaUnboxingRecordList,
            List<SeaSiteCargoModel> seaSiteCargoList,
            List<SeaShortCargoModel> seaShortCargoList,
            string userId)
        {
            // 三個頁籤共用同一個交易，避免只寫入部分資料。
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    InsertSeaUnboxingRecord(seaUnboxingRecordList, userId, transaction);
                    InsertSeaSiteCargo(seaSiteCargoList, userId, transaction);
                    InsertSeaShortCargo(seaShortCargoList, userId, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private void InsertSeaUnboxingRecord(List<SeaUnboxingRecordModel> seaUnboxingRecordList, string userId, IDbTransaction transaction)
        {
            var sql = @"
            IF EXISTS (SELECT 1 FROM jetf.dbo.SeaUnboxingRecord WHERE MainNumber = @MainNumber)
                UPDATE jetf.dbo.SeaUnboxingRecord SET DataDate = @DataDate, UploadOpe = @UploadOpe, UploadTime = GETDATE()
                WHERE MainNumber = @MainNumber;
            ELSE
                INSERT INTO jetf.dbo.SeaUnboxingRecord (MainNumber, DataDate, UploadOpe, UploadTime) 
                VALUES (@MainNumber, @DataDate, @UploadOpe, GETDATE());
            ";

            foreach (var item in seaUnboxingRecordList)
            {
                conn.Execute(sql, new
                {
                    MainNumber = item.MainNumber,
                    DataDate = item.DataDate,
                    UploadOpe = userId
                }, transaction: transaction);
            }
        }

        /// <summary>
        /// 新增現場有貨
        /// </summary>
        /// <param name="seaUnboxingRecordList"></param>
        /// <param name="userId"></param>
        private void InsertSeaSiteCargo(List<SeaSiteCargoModel> seaSiteCargoList, string userId, IDbTransaction transaction)
        {
            var sql = @"
            IF EXISTS (SELECT 1 FROM jetf.dbo.SeaSiteCargo WHERE MainNumber = @MainNumber and BagNumber = @BagNumber)
                UPDATE jetf.dbo.SeaSiteCargo SET DataDate = @DataDate,DataType = @DataType,JetfSerial = @JetfSerial,Piece = @Piece, UploadOpe = @UploadOpe, UploadTime = GETDATE()
                WHERE MainNumber = @MainNumber and BagNumber = @BagNumber;
            ELSE
                INSERT INTO jetf.dbo.SeaSiteCargo (DataDate, DataType, MainNumber, BagNumber, JetfSerial, Piece, UploadOpe, UploadTime) 
                VALUES (@DataDate, @DataType, @MainNumber, @BagNumber, @JetfSerial, @Piece, @UploadOpe, GETDATE());
            ";

            foreach (var item in seaSiteCargoList)
            {
                conn.Execute(sql, new
                {
                    MainNumber = item.MainNumber,
                    DataDate = item.DataDate,
                    DataType = item.DataType,
                    BagNumber = item.BagNumber,
                    JetfSerial = item.JetfSerial,
                    Piece = item.Piece,
                    UploadOpe = userId
                }, transaction: transaction);
            }
        }

        /// <summary>
        /// 新增短到
        /// </summary>
        /// <param name="seaUnboxingRecordList"></param>
        /// <param name="userId"></param>
        private void InsertSeaShortCargo(List<SeaShortCargoModel> seaShortCargoList, string userId, IDbTransaction transaction)
        {
            var sql = @"
            IF EXISTS (SELECT 1 FROM jetf.dbo.SeaShortCargo WHERE MainNumber = @MainNumber and BagNumber = @BagNumber)
                UPDATE jetf.dbo.SeaShortCargo SET DataDate = @DataDate,DataType = @DataType,Piece = @Piece, UploadOpe = @UploadOpe, UploadTime = GETDATE()
                WHERE MainNumber = @MainNumber and BagNumber = @BagNumber;
            ELSE
                INSERT INTO jetf.dbo.SeaShortCargo (DataDate, DataType, MainNumber, BagNumber, Piece, UploadOpe, UploadTime) 
                VALUES (@DataDate, @DataType, @MainNumber, @BagNumber, @Piece, @UploadOpe, GETDATE());
            ";

            foreach (var item in seaShortCargoList)
            {
                conn.Execute(sql, new
                {
                    MainNumber = item.MainNumber,
                    DataDate = item.DataDate,
                    DataType = item.DataType,
                    BagNumber = item.BagNumber,
                    Piece = item.Piece,
                    UploadOpe = userId
                }, transaction: transaction);
            }
        }

        private static ISheet GetRequiredSheet(IWorkbook workbook, string sheetName)
        {
            var sheet = workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                throw new Exception($"上傳失敗：找不到頁籤【{sheetName}】");
            }

            return sheet;
        }

        private static bool IsRowEmpty(IRow row, params int[] cellIndexes)
        {
            return cellIndexes.All(index => string.IsNullOrWhiteSpace(row.GetCellData(index)));
        }

        // 收集必填欄位缺漏，最後一次回傳給前端顯示完整錯誤清單。
        private static void AddRequiredField(List<string> missingFields, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                missingFields.Add(fieldName);
            }
        }

        private static string BuildRequiredFieldMessage(string sheetName, int recordIndex, List<string> missingFields)
        {
            return $"頁籤【{sheetName}】第{recordIndex}筆資料缺少必填欄位：{string.Join("、", missingFields)}";
        }

        private static void AddInvalidDateError(List<string> errors, string sheetName, int recordIndex, string fieldName, string value)
        {
            if (!IsValidDate(value))
            {
                errors.Add($"頁籤【{sheetName}】第{recordIndex}筆資料欄位【{fieldName}】日期格式錯誤：{value}");
            }
        }

        // 接受 Excel 常見日期格式，避免使用者填入文字或無效日期仍被寫入。
        private static bool IsValidDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            DateTime parsedDate;
            return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
        }

        private class UploadValidationException : Exception
        {
            public UploadValidationException(List<string> errors)
                : base("上傳失敗，請修正以下資料：")
            {
                Errors = errors ?? new List<string>();
            }

            public List<string> Errors { get; private set; }
        }


    }
}
