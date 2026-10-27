using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Models;
using Service.Services.FlightArrivalUpload.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.FlightArrivalUpload
{
    /// <summary>
    /// 匯入 A03、B6F、班機派件送達資料及 B6F 處理紀錄。
    /// </summary>
    public partial class FlightArrivalUploadService
    {
        private readonly JetfDbContext _jetfDb;

        /// <summary>
        /// 建立使用 EF 資料庫內容的檔案上傳服務。
        /// </summary>
        /// <param name="jetfDb">Jetf 資料庫內容。</param>
        public FlightArrivalUploadService(JetfDbContext jetfDb)
        {
            _jetfDb = jetfDb;
        }

        /// <summary>
        /// 先解析及驗證整份檔案，再於同一交易內寫入資料。
        /// </summary>
        /// <param name="filePath">xlsx 檔案路徑。</param>
        /// <param name="userId">上傳人員代碼。</param>
        /// <returns>匯入結果與原有資料筆數。</returns>
        public ResponseModel UploadFile(string filePath, string userId)
        {
            try
            {
                var today = DateTime.Now;
                Domain.FlightArrivalUploadData data;
                using (var stream = File.OpenRead(filePath))
                {
                    var workbook = new XSSFWorkbook(stream);
                    try
                    {
                        data = ReadWorkbook(workbook, today);
                    }
                    finally
                    {
                        workbook.Close();
                    }
                }

                var error = ValidateArrivalDates(data.ArrivalRows);
                if (error != null)
                {
                    return new ResponseModel(error);
                }

                var count = data.ClearanceRows.Count + data.ArrivalRows.Count;
                if (count == 0 && data.Processes.Count == 0)
                {
                    return new ResponseModel("上傳檔案筆數：0");
                }

                ConvertArrivalNames(data.ArrivalRows);
                SaveUpload(data, userId, today);
                return new ResponseModel
                {
                    status = Status.success,
                    IsSuccess = true,
                    msg = $"上傳檔案筆數：{count}"
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 將班機派件資料中的空運客戶及派件公司名稱轉為代碼，查無對應時保留原值。
        /// </summary>
        /// <param name="rows">待轉換客戶與派件公司名稱的班機派件資料。</param>
        private void ConvertArrivalNames(List<FlightArrivalUploadArrivalModel> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var customers = _jetfDb.CustomerMasters.AsNoTracking()
                .Where(x => x.TranType == "空運")
                .Select(x => new { x.CustId, x.Customer })
                .Distinct()
                .OrderBy(x => x.CustId)
                .ToList();
            var transNames = _jetfDb.CustomerMasters.AsNoTracking()
                .Where(x => x.TranType == "空運")
                .Select(x => new { x.TransNo, x.TransName })
                .Distinct()
                .OrderBy(x => x.TransNo)
                .ToList();
            var nameComparer = new FlightArrivalUploadNameComparer();
            var customerCodes = customers.ToLookup(x => x.Customer ?? string.Empty, nameComparer);
            var transCodes = transNames.ToLookup(x => x.TransName ?? string.Empty, nameComparer);
            foreach (var row in rows)
            {
                var customer = customerCodes[row.CustId].FirstOrDefault();
                var trans = transCodes[row.TransNo].FirstOrDefault();
                if (customer != null)
                {
                    row.CustId = (customer.CustId ?? string.Empty).Trim();
                }
                if (trans != null)
                {
                    row.TransNo = (trans.TransNo ?? string.Empty).Trim();
                }
            }
        }
    }
}
