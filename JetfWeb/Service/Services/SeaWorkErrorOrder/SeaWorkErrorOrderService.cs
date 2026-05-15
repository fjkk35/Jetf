using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Models.CptTradeVan;
using Service.Models.SeaWorkErrorOrder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaWorkErrorOrder
{
    public class SeaWorkErrorOrderService : _BaseService
    {
        private readonly CptTradeVanService _cptTradeVanService;

        public SeaWorkErrorOrderService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, CptTradeVanService cptTradeVanService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _cptTradeVanService = cptTradeVanService;
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel Upload(string filePath, string dataDate, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            //讀取Excel
            var list = ReadExcel(filePath);
            //新增
            if (list.Count > 0)
            {
                //寫入資料
                dataDate = Convert.ToDateTime(dataDate).ToString("yyyyMMdd");
                resopnseModel = InsertData(list, dataDate, userId);

                if (resopnseModel.status == Status.success)
                {
                    var bagNumberList = list.Select(r => r.BagNumber).ToList();
                    //取得GB353
                    var gb353List = _cptTradeVanService.GetCptSeaMainNumberDetails(bagNumberList);
                    //查詢GB353
                    _cptTradeVanService.SearchGb353(gb353List);
                    resopnseModel.msg = $"上傳檔案筆數：{list.Count}";
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = "上傳檔案筆數：0";
            }
            return resopnseModel;
        }

        /// <summary>
        /// 寫入上傳檔案 海快錯單袋號
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertData(List<SeaWorkErrorOrderModel> list, string dataDate, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            var sql = @"
IF EXISTS (
    SELECT 1 FROM [jetf].[dbo].[SeaWorkErrorOrder]
    WHERE MainNumber = @MainNumber AND BagNumber = @BagNumber
)
BEGIN
    UPDATE [jetf].[dbo].[SeaWorkErrorOrder]
    SET DataDate = @DataDate,
        Reason = @Reason,
        UploadTime = @UploadTime,
        UploadOpe = @UploadOpe
    WHERE MainNumber = @MainNumber AND BagNumber = @BagNumber
END
ELSE
BEGIN
    INSERT INTO [jetf].[dbo].[SeaWorkErrorOrder]
        (DataDate, MainNumber, BagNumber, Reason, UploadTime, UploadOpe)
    VALUES
        (@DataDate, @MainNumber, @BagNumber, @Reason, @UploadTime, @UploadOpe)
END
";

            string uploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var item in list)
                    {
                        conn.Execute(sql, new
                        {
                            DataDate = dataDate,
                            MainNumber = item.MainNumber,
                            BagNumber = item.BagNumber,
                            Reason = item.Reason,
                            UploadTime = uploadTime,
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

            return resopnseModel;
        }

        /// <summary>
        /// 讀取Excel 海快錯單
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<SeaWorkErrorOrderModel> ReadExcel(string filePath)
        {
            bool read = false;
            var list =new List<SeaWorkErrorOrderModel>();
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
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //主號
                   var mainNumber = sheet.GetRow(i).GetCellData(0);
                    //分號
                   var bagNumber = sheet.GetRow(i).GetCellData(1);
                    //錯單訊息
                   var reason = sheet.GetRow(i).GetCellData(2);

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主號") && (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "分號") && (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "錯單訊息"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && mainNumber != "" && bagNumber != "" && reason != "")
                    {
                        list.Add(new SeaWorkErrorOrderModel()
                        {
                            MainNumber = mainNumber,
                            BagNumber = bagNumber,
                            Reason = reason
                        });
                    }
                }
            }
            return list;
        }

    }
}
