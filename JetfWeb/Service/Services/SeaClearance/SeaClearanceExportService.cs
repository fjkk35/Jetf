using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Helpers;
using Service.Models.SeaClearance;
using Service.Models.SeaClearanceCreate;
using System.Collections.Generic;
using System.Linq;

namespace Service.Services.SeaClearance
{
    /// <summary>
    /// 海運通關匯出服務。
    /// </summary>
    public partial class SeaClearanceService
    {
        /// <summary>
        /// 依海運通關主檔 ID 匯出 Excel。
        /// </summary>
        public IWorkbook SeaClearanceForIdExcel(int id)
        {
            var request = new SeaClearanceRequest
            {
                SeaClearanceId = id
            };

            return Excel(GetSeaClearance(request));
        }

        /// <summary>
        /// 依查詢條件匯出 Excel。
        /// </summary>
        public IWorkbook SeaClearanceExcel(SeaClearanceRequest request)
        {
            var ids = GetFilteredSeaClearanceDetailIds(request);
            if (!ids.Any())
            {
                return Excel(new List<SeaClearanceDetailQueryModel>());
            }

            var list = GetSeaClearance(new SeaClearanceRequest
            {
                SeaClearanceDetailIds = ids
            });

            //簽審類別
            var approvalCategoriesDic = GetDetailApprovalCategories(ids);
            var allSteps = GetAllSteps().OrderBy(r => r.Sort)
                .ToDictionary(r => r.Id, r => r.StepName);

            foreach (var item in list)
            {
                item.ApprovalCategoryName = approvalCategoriesDic.TryGetValue(item.Id, out var approvalCategoryName)
                    ? approvalCategoryName
                    : string.Empty;

                item.CurrentStepName = item.CurrentStepId.HasValue && allSteps.TryGetValue(item.CurrentStepId.Value, out var step)
                    ? step
                    : allSteps.FirstOrDefault().Value;
            }

            return Excel(list);
        }

        /// <summary>
        /// 產生海運通關 Excel。
        /// </summary>
        private IWorkbook Excel(List<SeaClearanceDetailQueryModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("明細");

            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            ICellStyle dataStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);
            ICellStyle dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/mm/dd");
            ICellStyle numberStyle = NpoiStyle.CreateNumberStyle(workbook);

            var headers = new[]
            {
                "備註", "建檔日期", "原單是否上傳", "報驗公司", "代理報驗", "簽審類別", "步驟", "異常狀態", "客戶", "主號",
                "分提單號碼", "報單號碼", "倉別", "報關方式", "派件", "件數", "進口人統一編號 (原單)",
                "原單申報人", "原單人電話", "聯繫人異動資料", "聯繫人信箱",
                "收單通知日期", "預計到港日", "艙單到港日", "入倉日期", "出倉日期", "報單傳輸日",
                "報單傳輸截止日", "要求客戶截止日", "強制結案日", "滯報費", "到倉天數", "扣倉", "扣倉項次",
                "稅金1","稅金2", "稅金收費方式", "報關費用1", "報關費用2", "報關費收費方式", "報驗費用(含稅)",
                "到付款", "材積數", "三聯稅單", "四聯稅單", "滯報費減免", "倉租天數減免", "實際交派日", "海關進度"
            };

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            var specialWidths = new Dictionary<int, int>
            {
                { 4, 8000 },
                { 5, 8000 },
                { 7, 8000 },
                { 13, 8000 },
                { 33, 8000 }
            };

            for (int index = 0; index < headers.Length; index++)
            {
                sheet.SetColumnWidth(index, specialWidths.ContainsKey(index) ? specialWidths[index] : 5000);
            }

            var rowIndex = 1;
            list.ForEach(r =>
            {
                IRow row = sheet.CreateRow(rowIndex++);
                var seaOrderOriginal = r.SeaOrderOriginals.FirstOrDefault(x => x.Gw > 0);

                int colIndex = 0;

                // 備註: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 建檔日期: SeaClearanceDetailQueryModel.CrtDateTime
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.CrtDateTime, dateStyle);

                // 原單是否上傳: SeaClearanceDetailQueryModel.IsSeaOrderOriginal
                NpoiCell.CreateCell(row, colIndex++, r.IsSeaOrderOriginal ? "是" : "否", dataStyle);

                // 報驗公司名稱: SeaClearanceDetailQueryModel.CustomsBrokerName
                NpoiCell.CreateCell(row, colIndex++, r.CustomsBrokerName, dataStyle);

                // 代理報驗名稱: SeaClearanceDetailQueryModel.CustomsBrokerageName
                NpoiCell.CreateCell(row, colIndex++, r.CustomsBrokerageName, dataStyle);

                // 簽審類別: SeaClearanceDetailQueryModel.ApprovalCategoryName
                NpoiCell.CreateCell(row, colIndex++, r.ApprovalCategoryName, dataStyle);

                // 步驟名稱: SeaClearanceDetailQueryModel.CurrentStepName
                NpoiCell.CreateCell(row, colIndex++, r.CurrentStepName, dataStyle);

                // 異常狀態名稱: SeaClearanceDetailQueryModel.CurrentAbnormalStateName
                NpoiCell.CreateCell(row, colIndex++, r.CurrentAbnormalStateName, dataStyle);

                // 客戶: SeaOrderOriginalModel.Cust_Name
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Cust_Name, dataStyle);

                // 主號: SeaClearanceDetailQueryModel.MainNumber
                NpoiCell.CreateCell(row, colIndex++, r.MainNumber, dataStyle);

                // 分提單號碼: SeaClearanceDetailQueryModel.TrackingNo
                NpoiCell.CreateCell(row, colIndex++, r.TrackingNo, dataStyle);

                // 報單號碼: SeaClearanceDetailQueryModel.DeclNo
                NpoiCell.CreateCell(row, colIndex++, r.DeclNo, dataStyle);

                // 倉別: SeaOrderOriginalModel.Modifyby
                NpoiCell.CreateCell(row, colIndex++, r.SeaOrderOriginals.FirstOrDefault()?.Modifyby, dataStyle);

                // 報關方式: SeaOrderOriginalModel.Post_Entry
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Post_Entry, dataStyle);

                // 派件: SeaOrderOriginalModel.Jetf_Serial
                NpoiCell.CreateCell(row, colIndex++, string.Join("、", r.SeaOrderOriginals.Select(x => x.Jetf_Serial).Distinct().ToArray()), dataStyle);

                // 件數: SeaOrderOriginalModel.Piece
                NpoiCell.CreateIntCell(row, colIndex++, seaOrderOriginal?.Piece, numberStyle);

                // 進口人統一編號: SeaOrderOriginalModel.Importer_Id
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Importer_Id, dataStyle);

                // 原單申報人: SeaOrderOriginalModel.Importer
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Importer, dataStyle);

                // 原單人電話: SeaOrderOriginalModel.Im_Phoneno
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Im_Phoneno, dataStyle);

                // 聯繫人異動資料: SeaClearanceDetailQueryModel.ContactChangeData
                NpoiCell.CreateCell(row, colIndex++, r.ContactChangeData, dataStyle);

                // 聯繫人信箱: SeaClearanceDetailQueryModel.ContactEmail
                NpoiCell.CreateCell(row, colIndex++, r.ContactEmail, dataStyle);

                // 收單通知日期: SeaOrderOriginalModel.CreateDate
                NpoiCell.CreateDateTimeCell(row, colIndex++, seaOrderOriginal?.CreateDate, dateStyle);

                // 預計到港日: SeaOrderOriginalModel.Eta
                NpoiCell.CreateDateTimeCell(row, colIndex++, seaOrderOriginal?.Eta, dateStyle);

                // 進口日期: SeaClearanceDetailQueryModel.ImportDate
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.ImportDate.ToDateTime("yyyyMMdd"), dateStyle);

                // 入倉日期: SeaClearanceDetailQueryModel.SignInTime
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.SignInTime, dateStyle);

                // 出倉日期: SeaClearanceDetailQueryModel.SignOutTime
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.SignOutTime, dateStyle);

                // 報單傳輸日: SeaClearanceDetailQueryModel.ProDateTime
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.ProDateTime, dateStyle);

                // 報單傳輸截止日: SeaClearanceDetailQueryModel.ProDateTimeDeadline
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.ProDateTimeDeadline, dateStyle);

                // 要求客戶截止日: SeaClearanceDetailQueryModel.CustomerDeadline
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.CustomerDeadline, dateStyle);

                // 強制結案日: SeaClearanceDetailQueryModel.CloseDate
                NpoiCell.CreateDateTimeCell(row, colIndex++, r.CloseDate, dateStyle);

                // 滯報費
                if (r.LateDeclarationFee < 0)
                {
                    NpoiCell.CreateCell(row, colIndex++, "無", dataStyle);
                }
                else
                {
                    NpoiCell.CreateIntCell(row, colIndex++, r.LateDeclarationFee, numberStyle);
                }
                // 到倉天數
                if (r.WarehouseDays.HasValue && r.WarehouseDays.Value > 0)
                {
                    NpoiCell.CreateIntCell(row, colIndex++, r.WarehouseDays, numberStyle);
                }
                else
                {
                    NpoiCell.CreateCell(row, colIndex++, "未入倉", dataStyle);
                }

                // 扣倉: SeaClearanceDetailQueryModel.IsCustomsHold
                NpoiCell.CreateCell(row, colIndex++, r.IsCustomsHold ? "是" : "否", dataStyle);

                // 扣倉項次: SeaClearanceDetailQueryModel.CustomsHold
                NpoiCell.CreateCell(row, colIndex++, r.CustomsHold, dataStyle);

                // 稅金: SeaClearanceDetailQueryModel.Tax
                NpoiCell.CreateDoubleCell(row, colIndex++, r.Tax, numberStyle);

                // 稅金2: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 稅金收費方式: SeaOrderOriginalModel.Tax_Payment
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Tax_Payment, dataStyle);

                // 報關費用: SeaClearanceDetailQueryModel.ClearanceFee
                NpoiCell.CreateIntCell(row, colIndex++, r.ClearanceFee, numberStyle);

                // 報關費用2: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 報關費收費方式: SeaOrderOriginalModel.Tax_Payment
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Tax_Payment, dataStyle);

                // 報驗費用(含稅): SeaOrderOriginalModel.Tax_Payment
                NpoiCell.CreateCell(row, colIndex++, seaOrderOriginal?.Tax_Payment, dataStyle);

                // 到付款: SeaOrderOriginalModel.CC
                NpoiCell.CreateDoubleCell(row, colIndex++, seaOrderOriginal?.CC, numberStyle);

                // 材積數: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 三聯稅單: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 四聯稅單: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 滯報費減免: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 倉租天數減免: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 實際交派日: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);

                // 海關進度: 保留欄位
                NpoiCell.CreateCell(row, colIndex++, string.Empty, dataStyle);
            });

            return workbook;
        }
    }
}
