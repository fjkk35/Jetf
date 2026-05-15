using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.BatchSearchShenzhenCargo.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.BatchSearchShenzhenCargo
{
    public class BatchSearchShenzhenCargoService : _BaseService
    {
        public BatchSearchShenzhenCargoService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 批量查詢速派新遞物流貨號
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>查詢結果</returns>
        public ResponseModel QueryShenzhenCargo(BatchSearchShenzhenCargoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TrackingNoList))
                {
                    return new ResponseModel("請輸入分提單號");
                }

                // 分割分提單號列表（支援換行、逗號、空白分隔）
                var trackingNoList = request.TrackingNoList
                    .Split(new[] { '\r', '\n', ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                if (!trackingNoList.Any())
                {
                    return new ResponseModel("請輸入有效的分提單號");
                }

                // 查詢資料
                string sql = @"
                    SELECT 
                        TrackingNo,
                        DeliveryNo
                    FROM [jetf].[dbo].[ShenzhenCargo] 
                    WHERE TrackingNo IN @TrackingNoList";

                var result = conn.Query<ShenzhenCargoModel>(sql, new { TrackingNoList = trackingNoList }).ToList();

                // 建立查詢結果的分組字典（一對多關係）
                var resultGroup = result.GroupBy(x => x.TrackingNo)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 組合完整結果列表，包含未查詢到的分提單號
                var fullResult = new List<ShenzhenCargoModel>();
                foreach (var trackingNo in trackingNoList)
                {
                    if (resultGroup.ContainsKey(trackingNo))
                    {
                        // 有查詢到資料，加入所有對應的物流貨號
                        fullResult.AddRange(resultGroup[trackingNo]);
                    }
                    else
                    {
                        // 未查詢到資料，加入空的物流貨號
                        fullResult.Add(new ShenzhenCargoModel
                        {
                            TrackingNo = trackingNo,
                            DeliveryNo = ""
                        });
                    }
                }

                return new ResponseModel(fullResult);
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
        public IWorkbook ExportExcel(BatchSearchShenzhenCargoRequest request)
        {
            // 查詢資料
            var queryResult = QueryShenzhenCargo(request);
            if (queryResult.status != "success")
            {
                throw new Exception(queryResult.msg);
            }

            var dataList = queryResult.ReturnObject as List<ShenzhenCargoModel> ?? new List<ShenzhenCargoModel>();

            // 建立工作簿
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("批量查詢速派新遞物流貨號");

            // 建立樣式
            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            ICellStyle dataStyle = NpoiStyle.CreateDataStyle(workbook);

            // 建立標題列
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = new string[]
            {
                "序號", "分提單號", "速派新遞物流貨號"
            };

            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            // 填充資料
            int rowIndex = 1;
            int serialNo = 1;
            foreach (var item in dataList)
            {
                IRow row = sheet.CreateRow(rowIndex++);

                NpoiCell.CreateIntCell(row, 0, serialNo++, dataStyle);    // 序號
                NpoiCell.CreateCell(row, 1, item.TrackingNo, dataStyle);  // 分提單號
                NpoiCell.CreateCell(row, 2, item.DeliveryNo, dataStyle);  // 速派新遞物流貨號
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
    }
}
