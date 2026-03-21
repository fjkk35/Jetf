using Autofac.Features.Metadata;
using Dapper;
using iTextSharp.text;
using Newtonsoft.Json;
using NPOI.SS.Formula.PTG;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Models.CptSeaMainNumberJob;
using Service.Models.SeaUnreceivedOrder;
using Service.Services.WorkDay;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.UserModel.HeaderFooter;

namespace Service.Services.SeaUnreceivedOrder
{
    public class SeaUnreceivedOrderService : _BaseService
    {
        private readonly WorkDayService _workDayService;

        public SeaUnreceivedOrderService(WorkDayService workDayService)
        {
            _workDayService = workDayService;
        }

        public ResopnseModel GetExecl(string mainNumber, SeaErrorReportEnum dataType)
        {
            try
            {
                IWorkbook workbook = new XSSFWorkbook();

                var mainNumberList = mainNumber
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct()
                .ToList();

                var list = new List<SeaUnreceivedOrderModel>();
                switch (dataType)
                {
                    case SeaErrorReportEnum.UnreceivedOrder:
                        var result = GetSeaUnreceivedOrderList(mainNumberList);
                        list = result.Where(r => !r.ShortCargoDataDate.HasValue && r.Merge_Over_Flag !="O").ToList();
                        //未收單明細
                        GetUnreceivedOrderSheet(workbook, list, dataType.ToDescription());
                        //短溢卸明細
                        var shortList = result.Where(r => r.ShortCargoDataDate.HasValue || r.Merge_Over_Flag == "O").ToList();
                        GetUnreceivedOrderSheet(workbook, shortList, "短溢卸");
                        break;
                    case SeaErrorReportEnum.Transmittable:
                        list = GetSeaTransmittableList(mainNumberList);
                        GetTransmittableSheet(workbook, list, dataType);
                        var transmittableDetail = list.Where(r => r.IsImport).ToList();
                        GetDetailSheet(workbook, transmittableDetail);
                        break;
                    case SeaErrorReportEnum.Declare:
                        list = GetSeaDeclareList(mainNumberList);
                        GetDetailSheet(workbook, list);
                        break;
                }

                return new ResopnseModel() { ReturnObject = workbook };
            }
            catch (Exception ex)
            {
                return new ResopnseModel(ex.Message);
            }
        }


        /// <summary>
        /// Excel 可傳輸明細
        /// </summary>
        void GetTransmittableSheet(IWorkbook workbook, List<SeaUnreceivedOrderModel> list, SeaErrorReportEnum dataType)
        {
            ISheet sheet = workbook.CreateSheet(dataType.ToDescription());
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("航班主號");
            row.CreateCell(1).SetCellValue("分提單號碼");
            row.CreateCell(2).SetCellValue("客戶");
            row.CreateCell(3).SetCellValue("倉儲");
            row.CreateCell(4).SetCellValue("預計到港日");
            row.CreateCell(5).SetCellValue("主號拆櫃日");
            row.CreateCell(6).SetCellValue("最後傳輸日");
            row.CreateCell(7).SetCellValue("現場有貨日期");
            row.CreateCell(8).SetCellValue("錯誤原因代碼(最新)");
            row.CreateCell(9).SetCellValue("錯誤原因說明(依新-->舊)");
            row.CreateCell(10).SetCellValue("錯單次數");
            row.CreateCell(11).SetCellValue("進口人英文名稱");
            row.CreateCell(12).SetCellValue("進口人統一編號");
            row.CreateCell(13).SetCellValue("進口人電話");
            row.CreateCell(14).SetCellValue("毛重");
            row.CreateCell(15).SetCellValue("件數");
            row.CreateCell(16).SetCellValue("貨物名稱");
            row.CreateCell(17).SetCellValue("單價金額");
            row.CreateCell(18).SetCellValue("發票總金額");
            row.CreateCell(19).SetCellValue("進口人英文地址");
            row.CreateCell(20).SetCellValue("派件公司");
            row.CreateCell(21).SetCellValue("配送單號");
            row.CreateCell(22).SetCellValue("LP NO");
            row.CreateCell(23).SetCellValue("客服提供日期");
            row.CreateCell(24).SetCellValue("正確姓名");
            row.CreateCell(25).SetCellValue("正確ID");
            row.CreateCell(26).SetCellValue("正確進口人電話");
            row.CreateCell(27).SetCellValue("正確品名");
            row.CreateCell(28).SetCellValue("正確金額");
            row.CreateCell(29).SetCellValue("今天客服狀態");
            row.CreateCell(30).SetCellValue("累積處置說明");
            row.CreateCell(31).SetCellValue("預委日期");
            row.CreateCell(32).SetCellValue("是否需重匯關貿");
            row.CreateCell(33).SetCellValue("電商或集運商編號");
            row.CreateCell(34).SetCellValue("貨物識別代碼");
            row.CreateCell(35).SetCellValue("電商或集運商名稱");
            row.CreateCell(36).SetCellValue("電商或集運商網址");

            for (int i = 0; i < 38; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            sheet.SetColumnWidth(8, 7000);
            sheet.SetColumnWidth(9, 10000);
            sheet.SetColumnWidth(30, 7000);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行
            //styleWrapText.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            styleWrapText.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.MainNumber);
                row.CreateCell(1).SetCellValue(r.BagNumber);
                row.CreateCell(2).SetCellValue(r.Despatch_Name);
                row.CreateCell(3).SetCellValue(r.ModifyBy);
                if (r.Eta.HasValue)
                    row.CreateCell(4).SetCellValue(r.Eta.Value.ToString("MM/dd"));

                row.CreateCell(5).SetCellValue(r.UnboxingDataDate?.ToString("MM/dd"));

                row.CreateCell(6).SetCellValue(r.LastDataDate?.ToString("MM/dd"));

                row.CreateCell(7).SetCellValue(r.SiteCargoDataDate?.ToString("MM/dd"));

                if (r.Gb353RejReasonList != null && r.Gb353RejReasonList.Any())
                {
                    row.CreateCell(8).SetCellValue(string.Join("\r\n", r.LastGb353RejReasonCode));
                    row.CreateCell(9).SetCellValue(string.Join("\r\n", r.Gb353RejReasonList.Select(x => $"{x.IssueDateTime}，{x.RejReasonCode}")));
                    row.CreateCell(10).SetCellValue(r.Gb353Count);

                    row.GetCell(8).CellStyle = styleWrapText;
                    row.GetCell(9).CellStyle = styleWrapText;
                }

                row.CreateCell(11).SetCellValue(r.Importer);
                row.CreateCell(12).SetCellValue(r.Importer_Id);
                row.CreateCell(13).SetCellValue(r.Im_PhoneNo);
                row.CreateCell(14).SetCellValue(r.Gw);
                row.CreateCell(15).SetCellValue(r.Piece);
                row.CreateCell(16).SetCellValue(r.Item_Name);
                row.CreateCell(17).SetCellValue(r.Unit_Price);
                row.CreateCell(18).SetCellValue(r.Invoice_Amount);
                row.CreateCell(19).SetCellValue(r.Im_Add);
                row.CreateCell(20).SetCellValue(r.Trans_Name);
                row.CreateCell(21).SetCellValue(r.Jetf_Serial);
                row.CreateCell(22).SetCellValue(r.LpNo);
                row.CreateCell(23).SetCellValue(r.ServiceDate?.ToString("MM/dd"));
                row.CreateCell(24).SetCellValue(r.CorrectImporterName);
                row.CreateCell(25).SetCellValue(r.CorrectImporterId);
                row.CreateCell(26).SetCellValue(r.CorrectImporterPhone);
                row.CreateCell(27).SetCellValue(r.CorrectItemName);
                row.CreateCell(28).SetCellValue(r.CorrectInvoiceAmount);
                row.CreateCell(29).SetCellValue(r.ServiceStatus);
                row.CreateCell(30).SetCellValue(r.ProcessRemark);
                row.CreateCell(31).SetCellValue(r.Format_Customs_Approval_DateTime);
                //是否需重匯關貿
                row.CreateCell(32).SetCellValue(r.IsImport ? "Y" : "N");
                row.CreateCell(33).SetCellValue(r.Consol_Code);//電商或集運商編號
                row.CreateCell(34).SetCellValue(r.Consol_Type);//貨物識別代碼
                row.CreateCell(35).SetCellValue(r.Consol_Name);//電商或集運商名稱
                row.CreateCell(36).SetCellValue(r.Consol_Url);//電商或集運商網址

                row.GetCell(30).CellStyle = styleWrapText;

                iRow++;
            });
        }

        /// <summary>
        /// Excel未收單、短到頁籤
        /// </summary>
        void GetUnreceivedOrderSheet(IWorkbook workbook, List<SeaUnreceivedOrderModel> list, string sheetName)
        {
            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("航班主號");
            row.CreateCell(1).SetCellValue("分提單號碼");
            row.CreateCell(2).SetCellValue("客戶");
            row.CreateCell(3).SetCellValue("倉儲");
            row.CreateCell(4).SetCellValue("預計到港日");
            row.CreateCell(5).SetCellValue("主號拆櫃日");
            row.CreateCell(6).SetCellValue("最後傳輸日");
            row.CreateCell(7).SetCellValue("現場有貨日期");
            row.CreateCell(8).SetCellValue("預委回覆代碼");
            row.CreateCell(9).SetCellValue("錯誤原因代碼(最新)");
            row.CreateCell(10).SetCellValue("錯誤原因說明(依新-->舊)");
            row.CreateCell(11).SetCellValue("錯單次數");
            row.CreateCell(12).SetCellValue("進口人英文名稱");
            row.CreateCell(13).SetCellValue("進口人統一編號");
            row.CreateCell(14).SetCellValue("進口人電話");
            row.CreateCell(15).SetCellValue("毛重");
            row.CreateCell(16).SetCellValue("件數");
            row.CreateCell(17).SetCellValue("貨物名稱");
            row.CreateCell(18).SetCellValue("單價金額");
            row.CreateCell(19).SetCellValue("發票總金額");
            row.CreateCell(20).SetCellValue("進口人英文地址");
            row.CreateCell(21).SetCellValue("派件公司");
            row.CreateCell(22).SetCellValue("配送單號");
            row.CreateCell(23).SetCellValue("LP NO");
            row.CreateCell(24).SetCellValue("是否需更新預委");
            row.CreateCell(25).SetCellValue("客服提供日期");
            row.CreateCell(26).SetCellValue("正確姓名");
            row.CreateCell(27).SetCellValue("正確ID");
            row.CreateCell(28).SetCellValue("正確進口人電話");
            row.CreateCell(29).SetCellValue("正確品名");
            row.CreateCell(30).SetCellValue("正確金額");
            row.CreateCell(31).SetCellValue("今天客服狀態");
            row.CreateCell(32).SetCellValue("累積處置說明");

            if (sheetName == "短溢卸")
            {
                row.CreateCell(33).SetCellValue("短溢卸");
                sheet.SetColumnWidth(33, 5000);
            }
               

            for (int i = 0; i < 33; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            sheet.SetColumnWidth(9, 7000);
            sheet.SetColumnWidth(10, 10000);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行
            //styleWrapText.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            styleWrapText.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.MainNumber);
                row.CreateCell(1).SetCellValue(r.BagNumber);
                row.CreateCell(2).SetCellValue(r.Despatch_Name);
                row.CreateCell(3).SetCellValue(r.ModifyBy);
                if (r.Eta.HasValue)
                    row.CreateCell(4).SetCellValue(r.Eta.Value.ToString("MM/dd"));

                row.CreateCell(5).SetCellValue(r.UnboxingDataDate?.ToString("MM/dd"));

                row.CreateCell(6).SetCellValue(r.LastDataDate?.ToString("MM/dd"));

                row.CreateCell(7).SetCellValue(r.SiteCargoDataDate?.ToString("MM/dd"));

                row.CreateCell(8).SetCellValue(r.Reply_Code);

                if (r.Gb353RejReasonList != null && r.Gb353RejReasonList.Any())
                {
                    row.CreateCell(9).SetCellValue(string.Join("\r\n", r.LastGb353RejReasonCode));
                    row.CreateCell(10).SetCellValue(string.Join("\r\n", r.Gb353RejReasonList.Select(x => $"{x.IssueDateTime}，{x.RejReasonCode}")));
                    row.CreateCell(11).SetCellValue(r.Gb353Count);

                    row.GetCell(9).CellStyle = styleWrapText;
                    row.GetCell(10).CellStyle = styleWrapText;
                }

                row.CreateCell(12).SetCellValue(r.Importer);
                row.CreateCell(13).SetCellValue(r.Importer_Id);
                row.CreateCell(14).SetCellValue(r.Im_PhoneNo);
                row.CreateCell(15).SetCellValue(r.Gw);
                row.CreateCell(16).SetCellValue(r.Piece);
                row.CreateCell(17).SetCellValue(r.Item_Name);
                row.CreateCell(18).SetCellValue(r.Unit_Price);
                row.CreateCell(19).SetCellValue(r.Invoice_Amount);
                row.CreateCell(20).SetCellValue(r.Im_Add);
                row.CreateCell(21).SetCellValue(r.Trans_Name);
                row.CreateCell(22).SetCellValue(r.Jetf_Serial);
                row.CreateCell(23).SetCellValue(r.LpNo);

                row.CreateCell(24).SetCellValue(r.IsUpdateApprovalNew ? "Y" : "N");
                row.CreateCell(25).SetCellValue(r.ServiceDate?.ToString("MM/dd"));
                row.CreateCell(26).SetCellValue(r.CorrectImporterName);
                row.CreateCell(27).SetCellValue(r.CorrectImporterId);
                row.CreateCell(28).SetCellValue(r.CorrectImporterPhone);
                row.CreateCell(29).SetCellValue(r.CorrectItemName);
                row.CreateCell(30).SetCellValue(r.CorrectInvoiceAmount);
                row.CreateCell(31).SetCellValue(r.ServiceStatus);
                row.CreateCell(32).SetCellValue(r.ProcessRemark);

                if (sheetName == "短溢卸")
                    row.CreateCell(33).SetCellValue(r.Merge_Over_Flag == "O" ? "溢卸" : "短到");

                row.GetCell(32).CellStyle = styleWrapText;
                iRow++;
            });
        }


        /// <summary>
        /// Excel 客戶+主號明細頁籤
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="list"></param>
        public void GetDetailSheet(IWorkbook workbook, List<SeaUnreceivedOrderModel> list)
        {
            //取得明細
            var details = GetCesMainOrderDetail(list);

            if (details.Any() == false)
            {
                workbook.CreateSheet($"無資料");
            }

            var despatchNames = details
                .GroupBy(r => new { r.MAINNUMBER, r.DESPATCH_NAME })
                .Select(r =>
                new
                {
                    r.Key.MAINNUMBER,
                    r.Key.DESPATCH_NAME,
                });

            foreach (var item in despatchNames)
            {
                var despatchNameList = details
                    .Where(r => r.MAINNUMBER == item.MAINNUMBER && r.DESPATCH_NAME == item.DESPATCH_NAME)
                    .OrderBy(r => r.MAINNUMBER)
                    .ThenBy(r => r.BL_NO)
                    .ThenBy(r => r.ITEM_NO_SORT)
                    .ToList();

                GetDespatchNameDetailSheet(workbook, despatchNameList);
            }
        }

        /// <summary>
        /// 客戶總表頁籤(具結)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="list"></param>
        public void GetDespatchNameReportSheet(IWorkbook workbook, List<CesMainOrderDetailModel> list)
        {
            XSSFCellStyle cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            #region 頁籤標題

            ISheet sheet = workbook.CreateSheet("總表");
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("海運快遞進口貨物清單");
            row.CreateCell(3).SetCellValue("文件版次：");

            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("主提單號碼");
            row.CreateCell(1).SetCellValue("海關通\n關號碼");
            row.CreateCell(2).SetCellValue("船舶航次");
            row.CreateCell(3).SetCellValue("船舶呼號");
            row.CreateCell(4).SetCellValue("船公司代碼");
            row.CreateCell(5).SetCellValue("卸存地\n點代碼");
            row.CreateCell(6).SetCellValue("裝貨港");
            row.CreateCell(7).SetCellValue("暫存地\n點代碼");
            row.CreateCell(8).SetCellValue("船機代碼");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;

            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("總表");

            row = sheet.CreateRow(3);
            row.CreateCell(0).SetCellValue("主提單號碼");
            row.CreateCell(1).SetCellValue("分提單號碼");
            row.CreateCell(2).SetCellValue("艙單號碼");
            row.CreateCell(3).SetCellValue("快遞業者\n統一編號");
            row.CreateCell(4).SetCellValue("單價條件");
            row.CreateCell(5).SetCellValue("單價幣別代碼");
            row.CreateCell(6).SetCellValue("毛重");
            row.CreateCell(7).SetCellValue("件數");
            row.CreateCell(8).SetCellValue("件數單位");
            row.CreateCell(9).SetCellValue("標記");
            row.CreateCell(10).SetCellValue("貨物編號");
            row.CreateCell(11).SetCellValue("貨物名稱");
            row.CreateCell(12).SetCellValue("貨品分類號列");
            row.CreateCell(13).SetCellValue("商標(牌名)");
            row.CreateCell(14).SetCellValue("成分及規格");
            row.CreateCell(15).SetCellValue("淨重");
            row.CreateCell(16).SetCellValue("數量");
            row.CreateCell(17).SetCellValue("數量單位");
            row.CreateCell(18).SetCellValue("單價金額");
            row.CreateCell(19).SetCellValue("發票總金額");
            row.CreateCell(20).SetCellValue("完稅價格");
            row.CreateCell(21).SetCellValue("體積");
            row.CreateCell(22).SetCellValue("體積單位");
            row.CreateCell(23).SetCellValue("生產國別");
            row.CreateCell(24).SetCellValue("出口人英文名稱");
            row.CreateCell(25).SetCellValue("出口人國家代碼");
            row.CreateCell(26).SetCellValue("出口人英文地址");
            row.CreateCell(27).SetCellValue("進口人身分識別碼");
            row.CreateCell(28).SetCellValue("進口人統一編號");
            row.CreateCell(29).SetCellValue("進口人英文名稱");
            row.CreateCell(30).SetCellValue("進口人電話");
            row.CreateCell(31).SetCellValue("進口人英文地址");
            row.CreateCell(32).SetCellValue("貨櫃種類");
            row.CreateCell(33).SetCellValue("貨櫃號碼");
            row.CreateCell(34).SetCellValue("貨櫃裝運方式");
            row.CreateCell(35).SetCellValue("封條號碼");
            row.CreateCell(36).SetCellValue("其他申報事項1");
            row.CreateCell(37).SetCellValue("其他申報事項2");
            row.CreateCell(38).SetCellValue("主動申報繳納稅款註記");
            row.CreateCell(39).SetCellValue("派件公司");
            row.CreateCell(40).SetCellValue("配送單號");
            row.CreateCell(41).SetCellValue("CC款");
            row.CreateCell(42).SetCellValue("後段報關\n/一般倉");
            row.CreateCell(43).SetCellValue("發票金額");
            row.CreateCell(44).SetCellValue("備註");
            row.CreateCell(45).SetCellValue("尺寸（單位：CM）");
            row.CreateCell(46).SetCellValue("電商或集運商編號");
            row.CreateCell(47).SetCellValue("貨物識別代碼");
            row.CreateCell(48).SetCellValue("電商或集運商名稱");
            row.CreateCell(49).SetCellValue("電商或集運商網址");


            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;
            row.GetCell(21).CellStyle = cs_Center;
            row.GetCell(22).CellStyle = cs_Center;
            row.GetCell(23).CellStyle = cs_Center;
            row.GetCell(24).CellStyle = cs_Center;
            row.GetCell(25).CellStyle = cs_Center;
            row.GetCell(26).CellStyle = cs_Center;
            row.GetCell(27).CellStyle = cs_Center;
            row.GetCell(28).CellStyle = cs_Center;
            row.GetCell(29).CellStyle = cs_Center;
            row.GetCell(30).CellStyle = cs_Center;
            row.GetCell(31).CellStyle = cs_Center;
            row.GetCell(32).CellStyle = cs_Center;
            row.GetCell(33).CellStyle = cs_Center;
            row.GetCell(34).CellStyle = cs_Center;
            row.GetCell(35).CellStyle = cs_Center;
            row.GetCell(36).CellStyle = cs_Center;
            row.GetCell(37).CellStyle = cs_Center;
            row.GetCell(38).CellStyle = cs_Center;
            row.GetCell(39).CellStyle = cs_Center;
            row.GetCell(40).CellStyle = cs_Center;
            row.GetCell(41).CellStyle = cs_Center;
            row.GetCell(42).CellStyle = cs_Center;
            row.GetCell(43).CellStyle = cs_Center;
            row.GetCell(44).CellStyle = cs_Center;
            row.GetCell(45).CellStyle = cs_Center;
            row.GetCell(46).CellStyle = cs_Center;
            row.GetCell(47).CellStyle = cs_Center;
            row.GetCell(48).CellStyle = cs_Center;
            row.GetCell(49).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 5000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 5000);
            sheet.SetColumnWidth(12, 5000);
            sheet.SetColumnWidth(13, 5000);
            sheet.SetColumnWidth(14, 5000);
            sheet.SetColumnWidth(15, 5000);
            sheet.SetColumnWidth(16, 5000);
            sheet.SetColumnWidth(17, 5000);
            sheet.SetColumnWidth(18, 5000);
            sheet.SetColumnWidth(19, 5000);
            sheet.SetColumnWidth(20, 5000);
            sheet.SetColumnWidth(21, 5000);
            sheet.SetColumnWidth(22, 5000);
            sheet.SetColumnWidth(23, 5000);
            sheet.SetColumnWidth(24, 5000);
            sheet.SetColumnWidth(25, 5000);
            sheet.SetColumnWidth(26, 5000);
            sheet.SetColumnWidth(27, 5000);
            sheet.SetColumnWidth(28, 5000);
            sheet.SetColumnWidth(29, 5000);
            sheet.SetColumnWidth(30, 5000);
            sheet.SetColumnWidth(31, 5000);
            sheet.SetColumnWidth(32, 5000);
            sheet.SetColumnWidth(33, 5000);
            sheet.SetColumnWidth(34, 5000);
            sheet.SetColumnWidth(35, 5000);
            sheet.SetColumnWidth(36, 5000);
            sheet.SetColumnWidth(37, 5000);
            sheet.SetColumnWidth(38, 5000);
            sheet.SetColumnWidth(39, 5000);
            sheet.SetColumnWidth(40, 5000);
            sheet.SetColumnWidth(41, 5000);
            sheet.SetColumnWidth(42, 5000);
            sheet.SetColumnWidth(43, 5000);
            sheet.SetColumnWidth(44, 5000);
            sheet.SetColumnWidth(45, 5000);
            sheet.SetColumnWidth(46, 5000);
            sheet.SetColumnWidth(47, 5000);
            sheet.SetColumnWidth(48, 5000);
            sheet.SetColumnWidth(49, 5000);
            #endregion

            var irow = 0;
            foreach (var item in list)
            {
                row = sheet.CreateRow(irow + 4);
                //分提單號碼
                var blNo = item.BL_NO;
                //項次
                var itemNo = item.ITEM_NO?.Trim();

                if (itemNo == "1")
                {
                    row.CreateCell(0).SetCellValue(item.MAINNUMBER);//主提單號碼
                    row.CreateCell(1).SetCellValue(blNo);//分提單號碼
                    row.CreateCell(2).SetCellValue(item.MANIFEST);//艙單號碼
                    row.CreateCell(3).SetCellValue(item.JETF_ID);//快遞業者統一編號
                    row.CreateCell(4).SetCellValue(item.TERMSOFPRICE);//單價條件
                    row.CreateCell(5).SetCellValue(item.CURRENCY);//單價幣別代碼
                }

                row.CreateCell(6).SetCellValue(item.GW);//毛重

                row.CreateCell(7).SetCellValue(item.PIECE);//件數

                row.CreateCell(8).SetCellValue(item.PIECE_UNIT);//件數單位
                row.CreateCell(9).SetCellValue(item.MARKS);//標記
                row.CreateCell(10).SetCellValue(itemNo);//貨物編號
                row.CreateCell(11).SetCellValue(item.ITEM_NAME);// 貨物名稱
                row.CreateCell(12).SetCellValue(item.CCC_CODE);//貨品分類號列
                row.CreateCell(13).SetCellValue(item.TRADEMARK);//商標(牌名)
                row.CreateCell(14).SetCellValue(item.II_SPEC);//成分及規格
                row.CreateCell(15).SetCellValue(item.NW);//淨重
                row.CreateCell(16).SetCellValue(item.QUANTITY);//數量
                row.CreateCell(17).SetCellValue(item.QUANTITY_UNIT);//數量單位
                row.CreateCell(18).SetCellValue(item.UNIT_PRICE);//單價金額
                row.CreateCell(19).SetCellValue(item.INVOICE_AMOUNT);//發票總金額
                //row.CreateCell(20).SetCellValue("");//完稅價格
                row.CreateCell(21).SetCellValue(item.MEASUREMENT);//體積
                row.CreateCell(22).SetCellValue(item.CBM);//體積單位
                row.CreateCell(23).SetCellValue(item.MADEIN);//生產國別

                if (itemNo == "1")
                {
                    row.CreateCell(24).SetCellValue(item.EXPORTER);// 出口人英文名稱
                    row.CreateCell(25).SetCellValue(item.EX_COUNRTYCODE);// 出口人國家代碼
                    row.CreateCell(26).SetCellValue(item.EX_ADD);//出口人英文地址
                    row.CreateCell(27).SetCellValue(item.PARTY_IDENTIFIER);//進口人身分識別碼
                    row.CreateCell(28).SetCellValue(item.IMPORTER_ID);//進口人統一編號
                    row.CreateCell(29).SetCellValue(item.IMPORTER);//進口人英文名稱
                    row.CreateCell(30).SetCellValue(item.IM_PHONENO);//進口人電話
                    row.CreateCell(31).SetCellValue(item.IM_ADD);//進口人英文地址
                    row.CreateCell(36).SetCellValue("POA=Y"); //其他申報事項1
                    row.CreateCell(37).SetCellValue(item.DECLARATION_2);//其他申報事項2
                    row.CreateCell(38).SetCellValue(item.TAXFEE_DECLARED);//主動申報繳納稅款註記
                    row.CreateCell(39).SetCellValue(item.TRANS_NAME);//派件公司
                    row.CreateCell(40).SetCellValue(item.JETF_SERIAL);//配送單號
                    //row.CreateCell(41).SetCellValue("CC款");
                    //row.CreateCell(42).SetCellValue("後段報關\n/一般倉");
                    //row.CreateCell(43).SetCellValue("發票金額");
                    //row.CreateCell(44).SetCellValue("備註");
                    row.CreateCell(45).SetCellValue(item.SIZE);//尺寸（單位：CM）
                    row.CreateCell(46).SetCellValue(item.CONSOL_CODE);//電商或集運商編號
                    row.CreateCell(47).SetCellValue(item.CONSOL_TYPE);//貨物識別代碼
                    row.CreateCell(48).SetCellValue(item.CONSOL_NAME);//電商或集運商名稱
                    row.CreateCell(49).SetCellValue(item.CONSOL_URL);//電商或集運商網址
                }

                if (item.E_CONT_NO != "")
                {
                    //製單資料
                    row.CreateCell(32).SetCellValue(item.E_CONT_TYPE);//貨櫃種類
                    row.CreateCell(33).SetCellValue(item.E_CONT_NO);//貨櫃號碼
                                                                                   //row.CreateCell(34).SetCellValue(dr[i]["E_CONT_TRANSMODEL"].ToString());//貨櫃裝運方式
                    row.CreateCell(34).SetCellValue("2");//貨櫃裝運方式
                    row.CreateCell(35).SetCellValue(item.E_SEALNO);//封條號碼
                }
                else
                {
                    //原單資料
                    row.CreateCell(32).SetCellValue(item.O_CONT_TYPE);//貨櫃種類
                    row.CreateCell(33).SetCellValue(item.O_CONT_NO);//貨櫃號碼
                                                                                   //row.CreateCell(34).SetCellValue(dr[i]["O_CONT_TRANSMODEL"].ToString());//貨櫃裝運方式
                    row.CreateCell(34).SetCellValue("2");//貨櫃裝運方式
                    row.CreateCell(35).SetCellValue(item.O_SEALNO);//封條號碼
                }

                irow++;
            }
        }

        /// <summary>
        /// 客戶分組頁籤(具結)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="list"></param>
        public void GetDespatchNameDetailSheet(IWorkbook workbook, List<CesMainOrderDetailModel> list, bool isOtherItem = false)
        {
            XSSFCellStyle cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            var mainnumber = list.FirstOrDefault().MAINNUMBER;
            var despatch_name = list.FirstOrDefault().DESPATCH_NAME;
            //取得具結主號訂單明細
            DataTable dt_Order = GetCesMainOrder(mainnumber);

            #region 頁籤標題

            ISheet sheet = workbook.CreateSheet($"{despatch_name}{mainnumber}");
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("海運快遞進口貨物清單");
            row.CreateCell(3).SetCellValue("文件版次：");

            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("主提單號碼");
            row.CreateCell(1).SetCellValue("海關通\n關號碼");
            row.CreateCell(2).SetCellValue("船舶航次");
            row.CreateCell(3).SetCellValue("船舶呼號");
            row.CreateCell(4).SetCellValue("船公司代碼");
            row.CreateCell(5).SetCellValue("卸存地\n點代碼");
            row.CreateCell(6).SetCellValue("裝貨港");
            row.CreateCell(7).SetCellValue("暫存地\n點代碼");
            row.CreateCell(8).SetCellValue("船機代碼");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;

            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue(mainnumber);
            if (dt_Order.Rows.Count > 0)
            {
                row.CreateCell(1).SetCellValue(dt_Order.Rows[0]["FIELD_A"].ToString());//海關通\n關號碼
                row.CreateCell(2).SetCellValue(dt_Order.Rows[0]["FIELD_B"].ToString());//船舶航次
                row.CreateCell(3).SetCellValue(dt_Order.Rows[0]["FIELD_C"].ToString());//船舶呼號
                row.CreateCell(4).SetCellValue(dt_Order.Rows[0]["FIELD_D"].ToString());//船公司代碼
                row.CreateCell(5).SetCellValue(dt_Order.Rows[0]["FIELD_E"].ToString());//卸存地\n點代碼
                row.CreateCell(6).SetCellValue(dt_Order.Rows[0]["FIELD_F"].ToString());//裝貨港
                                                                                       //row.CreateCell(7).SetCellValue("");//暫存地\n點代碼
                row.CreateCell(8).SetCellValue(dt_Order.Rows[0]["FIELD_G"].ToString());//船機代碼
                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Center;
                row.GetCell(3).CellStyle = cs_Center;
                row.GetCell(4).CellStyle = cs_Center;
                row.GetCell(5).CellStyle = cs_Center;
                row.GetCell(6).CellStyle = cs_Center;
                //row.GetCell(7).CellStyle = cs_Center;
                row.GetCell(8).CellStyle = cs_Center;
            }

            row = sheet.CreateRow(3);
            row.CreateCell(0).SetCellValue("分提單號碼");
            row.CreateCell(1).SetCellValue("艙單號碼");
            row.CreateCell(2).SetCellValue("快遞業者\n統一編號");
            row.CreateCell(3).SetCellValue("單價條件");
            row.CreateCell(4).SetCellValue("單價幣別代碼");
            row.CreateCell(5).SetCellValue("毛重");
            row.CreateCell(6).SetCellValue("件數");
            row.CreateCell(7).SetCellValue("件數單位");
            row.CreateCell(8).SetCellValue("標記");
            row.CreateCell(9).SetCellValue("貨物編號");
            row.CreateCell(10).SetCellValue("貨物名稱");
            row.CreateCell(11).SetCellValue("貨品分類號列");
            row.CreateCell(12).SetCellValue("商標(牌名)");
            row.CreateCell(13).SetCellValue("成分及規格");
            row.CreateCell(14).SetCellValue("淨重");
            row.CreateCell(15).SetCellValue("數量");
            row.CreateCell(16).SetCellValue("數量單位");
            row.CreateCell(17).SetCellValue("單價金額");
            row.CreateCell(18).SetCellValue("發票總金額");
            row.CreateCell(19).SetCellValue("完稅價格");
            row.CreateCell(20).SetCellValue("體積");
            row.CreateCell(21).SetCellValue("體積單位");
            row.CreateCell(22).SetCellValue("生產國別");
            row.CreateCell(23).SetCellValue("出口人英文名稱");
            row.CreateCell(24).SetCellValue("出口人國家代碼");
            row.CreateCell(25).SetCellValue("出口人英文地址");
            row.CreateCell(26).SetCellValue("進口人身分識別碼");
            row.CreateCell(27).SetCellValue("進口人統一編號");
            row.CreateCell(28).SetCellValue("進口人英文名稱");
            row.CreateCell(29).SetCellValue("進口人電話");
            row.CreateCell(30).SetCellValue("進口人英文地址");
            row.CreateCell(31).SetCellValue("貨櫃種類");
            row.CreateCell(32).SetCellValue("貨櫃號碼");
            row.CreateCell(33).SetCellValue("貨櫃裝運方式");
            row.CreateCell(34).SetCellValue("封條號碼");
            row.CreateCell(35).SetCellValue("其他申報事項1");
            row.CreateCell(36).SetCellValue("其他申報事項2");
            row.CreateCell(37).SetCellValue("主動申報繳納稅款註記");
            row.CreateCell(38).SetCellValue("派件公司");
            row.CreateCell(39).SetCellValue("配送單號");
            row.CreateCell(40).SetCellValue("CC款");
            row.CreateCell(41).SetCellValue("後段報關\n/一般倉");
            row.CreateCell(42).SetCellValue("發票金額");
            row.CreateCell(43).SetCellValue("備註");
            row.CreateCell(44).SetCellValue("尺寸（單位：CM）");
            row.CreateCell(45).SetCellValue("電商或集運商編號");
            row.CreateCell(46).SetCellValue("貨物識別代碼");
            row.CreateCell(47).SetCellValue("電商或集運商名稱");
            row.CreateCell(48).SetCellValue("電商或集運商網址");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;
            row.GetCell(21).CellStyle = cs_Center;
            row.GetCell(22).CellStyle = cs_Center;
            row.GetCell(23).CellStyle = cs_Center;
            row.GetCell(24).CellStyle = cs_Center;
            row.GetCell(25).CellStyle = cs_Center;
            row.GetCell(26).CellStyle = cs_Center;
            row.GetCell(27).CellStyle = cs_Center;
            row.GetCell(28).CellStyle = cs_Center;
            row.GetCell(29).CellStyle = cs_Center;
            row.GetCell(30).CellStyle = cs_Center;
            row.GetCell(31).CellStyle = cs_Center;
            row.GetCell(32).CellStyle = cs_Center;
            row.GetCell(33).CellStyle = cs_Center;
            row.GetCell(34).CellStyle = cs_Center;
            row.GetCell(35).CellStyle = cs_Center;
            row.GetCell(36).CellStyle = cs_Center;
            row.GetCell(37).CellStyle = cs_Center;
            row.GetCell(38).CellStyle = cs_Center;
            row.GetCell(39).CellStyle = cs_Center;
            row.GetCell(40).CellStyle = cs_Center;
            row.GetCell(41).CellStyle = cs_Center;
            row.GetCell(42).CellStyle = cs_Center;
            row.GetCell(43).CellStyle = cs_Center;
            row.GetCell(44).CellStyle = cs_Center;
            row.GetCell(45).CellStyle = cs_Center;
            row.GetCell(46).CellStyle = cs_Center;
            row.GetCell(47).CellStyle = cs_Center;
            row.GetCell(48).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 5000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 5000);
            sheet.SetColumnWidth(12, 5000);
            sheet.SetColumnWidth(13, 5000);
            sheet.SetColumnWidth(14, 5000);
            sheet.SetColumnWidth(15, 5000);
            sheet.SetColumnWidth(16, 5000);
            sheet.SetColumnWidth(17, 5000);
            sheet.SetColumnWidth(18, 5000);
            sheet.SetColumnWidth(19, 5000);
            sheet.SetColumnWidth(20, 5000);
            sheet.SetColumnWidth(21, 5000);
            sheet.SetColumnWidth(22, 5000);
            sheet.SetColumnWidth(23, 5000);
            sheet.SetColumnWidth(24, 5000);
            sheet.SetColumnWidth(25, 5000);
            sheet.SetColumnWidth(26, 5000);
            sheet.SetColumnWidth(27, 5000);
            sheet.SetColumnWidth(28, 5000);
            sheet.SetColumnWidth(29, 5000);
            sheet.SetColumnWidth(30, 5000);
            sheet.SetColumnWidth(31, 5000);
            sheet.SetColumnWidth(32, 5000);
            sheet.SetColumnWidth(33, 5000);
            sheet.SetColumnWidth(34, 5000);
            sheet.SetColumnWidth(35, 5000);
            sheet.SetColumnWidth(36, 5000);
            sheet.SetColumnWidth(37, 5000);
            sheet.SetColumnWidth(38, 5000);
            sheet.SetColumnWidth(39, 5000);
            sheet.SetColumnWidth(40, 5000);
            sheet.SetColumnWidth(41, 5000);
            sheet.SetColumnWidth(42, 5000);
            sheet.SetColumnWidth(43, 5000);
            sheet.SetColumnWidth(44, 5000);
            sheet.SetColumnWidth(45, 5000);
            sheet.SetColumnWidth(46, 5000);
            sheet.SetColumnWidth(47, 5000);
            sheet.SetColumnWidth(48, 5000);
            #endregion

            var irow = 0;
            foreach (var item in list)
            {
                row = sheet.CreateRow(irow + 4);
                //分提單號碼
                var blNo = item.BL_NO;
                //項次
                var itemNo = item.ITEM_NO?.Trim();

                if (itemNo == "1")
                {
                    row.CreateCell(0).SetCellValue(blNo);//分提單號碼
                    row.CreateCell(1).SetCellValue(item.MANIFEST);//艙單號碼
                    row.CreateCell(2).SetCellValue(item.JETF_ID);//快遞業者統一編號
                    row.CreateCell(3).SetCellValue(item.TERMSOFPRICE);//單價條件
                    row.CreateCell(4).SetCellValue(item.CURRENCY);//單價幣別代碼
                }
                if (double.TryParse(item.GW.ToString(), out var gw))
                {
                    row.CreateCell(5).SetCellValue(gw);//毛重
                }

                if (int.TryParse(item.PIECE.ToString(), out var piece))
                {
                    row.CreateCell(6).SetCellValue(piece);//件數
                }

                row.CreateCell(7).SetCellValue(item.PIECE_UNIT);//件數單位
                row.CreateCell(8).SetCellValue(item.MARKS);//標記
                row.CreateCell(9).SetCellValue(itemNo);//貨物編號
                row.CreateCell(10).SetCellValue(item.ITEM_NAME);// 貨物名稱
                row.CreateCell(11).SetCellValue(item.CCC_CODE);//貨品分類號列
                row.CreateCell(12).SetCellValue(item.TRADEMARK);//商標(牌名)
                row.CreateCell(13).SetCellValue(item.II_SPEC);//成分及規格

                if (double.TryParse(item.NW.ToString(), out var nw))
                {
                    row.CreateCell(14).SetCellValue(nw);//淨重
                }

                row.CreateCell(15).SetCellValue(item.QUANTITY);//數量

                row.CreateCell(16).SetCellValue(item.QUANTITY_UNIT);//數量單位

                if (double.TryParse(item.UNIT_PRICE.ToString(), out var unitAmount))
                {
                    row.CreateCell(17).SetCellValue(unitAmount);//單價金額
                }

                if (double.TryParse(item.INVOICE_AMOUNT.ToString(), out var invoiceAmount))
                {
                    row.CreateCell(18).SetCellValue(invoiceAmount);//發票總金額
                }

                row.CreateCell(20).SetCellValue(item.MEASUREMENT);//體積
                row.CreateCell(21).SetCellValue(item.CBM);//體積單位
                row.CreateCell(22).SetCellValue(item.MADEIN);//生產國別

                if (itemNo == "1")
                {
                    row.CreateCell(23).SetCellValue(item.EXPORTER);// 出口人英文名稱
                    row.CreateCell(24).SetCellValue(item.EX_COUNRTYCODE);// 出口人國家代碼
                    row.CreateCell(25).SetCellValue(item.EX_ADD);//出口人英文地址
                    row.CreateCell(26).SetCellValue(item.PARTY_IDENTIFIER);//進口人身分識別碼
                    row.CreateCell(27).SetCellValue(item.IMPORTER_ID);//進口人統一編號
                    row.CreateCell(28).SetCellValue(item.IMPORTER);//進口人英文名稱
                    row.CreateCell(29).SetCellValue(item.IM_PHONENO);//進口人電話
                    row.CreateCell(30).SetCellValue(item.IM_ADD);//進口人英文地址
                    row.CreateCell(35).SetCellValue(isOtherItem ? "POA=Y" : ""); //其他申報事項1
                    row.CreateCell(36).SetCellValue(item.IM_PHONENO);//其他申報事項2
                    row.CreateCell(37).SetCellValue(item.TAXFEE_DECLARED);//主動申報繳納稅款註記
                    row.CreateCell(38).SetCellValue(item.TRANS_NAME);//派件公司
                    row.CreateCell(39).SetCellValue(item.JETF_SERIAL);//配送單號
                    row.CreateCell(44).SetCellValue(item.SIZE);//尺寸（單位：CM）
                    row.CreateCell(45).SetCellValue(item.CONSOL_CODE);//電商或集運商編號
                    row.CreateCell(46).SetCellValue(item.CONSOL_TYPE);//貨物識別代碼
                    row.CreateCell(47).SetCellValue(item.CONSOL_NAME);//電商或集運商名稱
                    row.CreateCell(48).SetCellValue(item.CONSOL_URL);//電商或集運商網址
                }

                if (item.E_CONT_NO != "")
                {
                    //製單資料
                    row.CreateCell(31).SetCellValue(item.E_CONT_TYPE);//貨櫃種類
                    row.CreateCell(32).SetCellValue(item.E_CONT_NO);//貨櫃號碼
                    row.CreateCell(33).SetCellValue("2");//貨櫃裝運方式
                    row.CreateCell(34).SetCellValue(item.E_SEALNO);//封條號碼
                }
                else
                {
                    //原單資料
                    row.CreateCell(31).SetCellValue(item.O_CONT_TYPE);//貨櫃種類
                    row.CreateCell(32).SetCellValue(item.O_CONT_NO);//貨櫃號碼
                    row.CreateCell(33).SetCellValue("2");//貨櫃裝運方式
                    row.CreateCell(34).SetCellValue(item.O_SEALNO);//封條號碼
                }

                irow++;
            }
        }

        /// <summary>
        /// 取得未收單明細
        /// </summary>
        /// <param name="mainNumberList"></param>
        /// <returns></returns>
        private List<SeaUnreceivedOrderModel> GetSeaUnreceivedOrderList(List<string> mainNumberList)
        {
            var result = new List<SeaUnreceivedOrderModel>();
            
            foreach (var batch in mainNumberList.Batch(30))
            {
                var batchResult = GetSeaUnreceivedOrderBatch(batch);
                result.AddRange(batchResult);
            }

            //資料處理
            ProcessData(result, SeaErrorReportEnum.UnreceivedOrder);

            return result;
        }

        /// <summary>
        /// 取得未收單明細 - 分批處理
        /// </summary>
        /// <param name="mainNumberBatch"></param>
        /// <returns></returns>
        private List<SeaUnreceivedOrderModel> GetSeaUnreceivedOrderBatch(List<string> mainNumberBatch)
        {
            var sql = @"
                        declare @MainNumberTable Table
                        ( 
	                          MainNumber nvarchar(100)
                        )

                        {0};
                        
						with cte_ETL_PRE_APPROVAL as 
                        (
                            select HAWB_NO,REPLY_CODE from [DATA_CENTER].[dbo].ETL_PRE_APPROVAL
                            where MODEL='SEA' and SEQUENCE_NUMERIC='1'
                        ),
                        cte_CES_MAIN_ORDER as
                        (
	                        select distinct MAIN_NUMBER,b.NAME as MODIFYBY from [DATA_CENTER].[dbo].[CES_MAIN_ORDER] a
	                        join [DATA_CENTER].[dbo].[SYS_PARAM] b on a.CLEARANCE_CP=b.CODE
                        )
                        select 
                        a.MainNumber,a.BagNumber,a.IsReceiveOrder,a.Gb353RejReason,IsUpdateApproval,ServiceDate,CorrectImporterName,CorrectImporterId,CorrectImporterPhone,CorrectItemName,CorrectInvoiceAmount,ServiceStatus,ProcessRemark,a.UploadOpe,
                        c.CUST_NAME as DESPATCH_NAME,b.Importer,b.IMPORTER_ID,b.IM_PHONENO,b.IM_ADD,b.GW,b.Piece,b.ITEM_NAME,b.UNIT_PRICE,b.Invoice_Amount,b.TRANS_NAME,b.LPNO,b.ETA,b.JETF_SERIAL,b.MERGE_OVER_FLAG,
                        d.REPLY_CODE,                        
                        e.DataDate as UnboxingDataDate,
                        f.DataDate as SiteCargoDataDate,
                        g.DataDate as ShortCargoDataDate,
                        h.MODIFYBY
                        from [jetf].[dbo].CptSeaMainNumberDetail a
                        left join (select * from DATA_CENTER.dbo.SEA_ORDER_EDIT where GW > 0) b on a.MainNumber = b.MAINNUMBER and a.BagNumber = b.BL_NO
                        left join [DATA_CENTER].[dbo].[SYS_CUST] c on b.DESPATCH_NAME = c.CUST_CODE
						left join cte_ETL_PRE_APPROVAL d on a.BagNumber = d.HAWB_NO
                        left join [jetf].[dbo].SeaUnboxingRecord e on a.MainNumber=e.MainNumber
                        left join [jetf].[dbo].SeaSiteCargo f on a.MainNumber=e.MainNumber and a.BagNumber=f.BagNumber
                        left join [jetf].[dbo].SeaShortCargo g on a.MainNumber=g.MainNumber and a.BagNumber=g.BagNumber
                        left join cte_CES_MAIN_ORDER h on a.MainNumber = h.MAIN_NUMBER
                        where IsReceiveOrder = '0' and exists (select 1 from @MainNumberTable where MainNumber = a.MainNumber)
                        and exists (
                                    select 1 from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 
                                    where a.MainNumber = MainNumber and a.BagNumber = BL_NO
                                    and STATUS <> 'E' and GW > 0
                                   )
                        and (b.POST_ENTRY is null or b.POST_ENTRY ='')
                   ";

            var sb = new StringBuilder();
            sb.AppendLine($@"INSERT INTO @MainNumberTable VALUES {string.Join(",",
                mainNumberBatch.Select(r => $"('{r}')"))};");

            sql = string.Format(sql, sb.ToString());

            var result = conn.Query<SeaUnreceivedOrderModel>(sql, commandTimeout: 180).ToList();

            return result;
        }

        /// <summary>
        /// 取得可傳輸明細
        /// </summary>
        /// <param name="mainNumberList"></param>
        /// <returns></returns>
        private List<SeaUnreceivedOrderModel> GetSeaTransmittableList(List<string> mainNumberList)
        {
            var sql = @"
                         declare @MainNumberTable Table
                         ( 
	                          MainNumber nvarchar(100)
                         )

                        {0};

                        with cte_ETL_PRE_APPROVAL as 
                        (
                            select HAWB_NO,CUSTOMS_APPROVAL_DATETIME,REPLY_CODE,ID,TEL from [DATA_CENTER].[dbo].ETL_PRE_APPROVAL
                            where MODEL='SEA' and SEQUENCE_NUMERIC='1' --and REPLY_CODE in('00','14')
                        ),
                        cte_CES_MAIN_ORDER as
                        (
	                        select distinct MAIN_NUMBER,b.NAME as MODIFYBY from [DATA_CENTER].[dbo].[CES_MAIN_ORDER] a
	                        join [DATA_CENTER].[dbo].[SYS_PARAM] b on a.CLEARANCE_CP=b.CODE
                        )
                        select 
                        a.MainNumber,a.BagNumber,a.IsReceiveOrder,IsUpdateApproval,ServiceDate,CorrectImporterName,CorrectImporterId,CorrectImporterPhone,CorrectItemName,CorrectInvoiceAmount,ServiceStatus,ProcessRemark,
                        a.Gb353RejReason,
                        b.DESPATCH_NAME as CUST_CODE,b.SIHNO,b.Importer,b.IMPORTER_ID,b.IM_PHONENO,b.IM_ADD,b.GW,b.Piece,b.ITEM_NAME,b.UNIT_PRICE,b.Invoice_Amount,b.TRANS_NAME,b.LPNO,b.ETA,b.JETF_SERIAL,
                        c.CUST_NAME as DESPATCH_NAME,
                        d.CUSTOMS_APPROVAL_DATETIME,d.REPLY_CODE,d.ID,d.TEL,
                        e.DataDate as UnboxingDataDate,
                        f.DataDate as SiteCargoDataDate,
                        g.DataDate as ShortCargoDataDate,
                        h.MODIFYBY, a.UploadOpe,
                        i.CONSOL_CODE,i.CONSOL_TYPE,i.CONSOL_NAME,i.CONSOL_URL
                        from [jetf].[dbo].CptSeaMainNumberDetail a
                        left join (select * from DATA_CENTER.dbo.SEA_ORDER_EDIT where GW > 0) b on a.MainNumber = b.MAINNUMBER and a.BagNumber = b.BL_NO
                        left join [DATA_CENTER].[dbo].[SYS_CUST] c on b.DESPATCH_NAME = c.CUST_CODE
                        left join cte_ETL_PRE_APPROVAL d on a.BagNumber = d.HAWB_NO
                        left join [jetf].[dbo].SeaUnboxingRecord e on a.MainNumber=e.MainNumber
                        left join [jetf].[dbo].SeaSiteCargo f on a.MainNumber=e.MainNumber and a.BagNumber=f.BagNumber
                        left join [jetf].[dbo].SeaShortCargo g on a.MainNumber=e.MainNumber and a.BagNumber=g.BagNumber
                        left join cte_CES_MAIN_ORDER h on a.MainNumber = h.MAIN_NUMBER
                        left join [DATA_CENTER].[dbo].[Sys_cust] i on b.DESPATCH_NAME= i.CUST_CODE
                        where IsReceiveOrder = '0' 
                        and exists (
                                      select 1 from @MainNumberTable
                                      where MainNumber = a.MainNumber
                                   )
                        and exists (
                                    select 1 from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 
                                    where a.MainNumber = MainNumber and a.BagNumber = BL_NO
                                    and STATUS <> 'E' and GW > 0
                                   )
                        and (b.POST_ENTRY is null or b.POST_ENTRY ='')
                   ";

            var sb = new StringBuilder();
            foreach (var item in mainNumberList.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @MainNumberTable VALUES {string.Join(",",
                item.Select(r => $"('{r}')"))};");
            }

            sql = string.Format(sql, sb.ToString());

            var result = conn.Query<SeaUnreceivedOrderModel>(sql).ToList();

            //僅錯單B6F比對預委任，代碼為00才將資料顯示至可傳輸明細
            //有人工上傳異動記錄才顯示至可傳輸明細
            //有上傳異動資料並且正確姓名為空白資料不要出現
            result = result
                .Where(r =>
                {
                    var isUpload = !string.IsNullOrEmpty(r.UploadOpe);
                    var isCorrectName = !string.IsNullOrWhiteSpace(r.CorrectImporterName);
                    var isB6FSingle = r.Reply_Code == "00"
                                      && r.LastGb353RejReasonCode.Count == 1
                                      && r.LastGb353RejReasonCode.Contains("B6F");

                    return (isUpload && isCorrectName) || (!isUpload && isB6FSingle);
                })
                .ToList();

            // 當 CUST_CODE='CN00060' 且 SIHNO 有值時，將 CONSOL_NAME 設為 SIHNO
            result.Where(r => r.Cust_Code == "CN00060" && !string.IsNullOrEmpty(r.Sihno))
                  .ToList()
                  .ForEach(r => r.Consol_Name = r.Sihno);

            //資料處理
            ProcessData(result, SeaErrorReportEnum.Transmittable);

            return result;
        }

        /// <summary>
        /// 取得可申報明細
        /// </summary>
        /// <param name="mainNumberList"></param>
        /// <returns></returns>
        private List<SeaUnreceivedOrderModel> GetSeaDeclareList(List<string> mainNumberList)
        {
            var sql = @"
                         declare @BagNumberTable Table
                         ( 
	                          BagNumber nvarchar(100)
                         )

                        {0};

                        with cte_CES_MAIN_ORDER as
                        (
	                        select distinct MAIN_NUMBER,b.NAME as MODIFYBY from [DATA_CENTER].[dbo].[CES_MAIN_ORDER] a
	                        join [DATA_CENTER].[dbo].[SYS_PARAM] b on a.CLEARANCE_CP=b.CODE
                        )
                        select 
                        a.MainNumber,a.BagNumber,a.IsReceiveOrder,IsUpdateApproval,ServiceDate,CorrectImporterName,CorrectImporterId,CorrectImporterPhone,CorrectItemName,CorrectInvoiceAmount,ServiceStatus,ProcessRemark,
                        a.Gb353RejReason,
                        b.DESPATCH_NAME as CUST_CODE,b.SIHNO,b.Importer,b.IMPORTER_ID,b.IM_PHONENO,b.IM_ADD,b.GW,b.Piece,b.ITEM_NAME,b.UNIT_PRICE,b.Invoice_Amount,b.TRANS_NAME,b.LPNO,b.ETA,b.JETF_SERIAL,
                        c.CUST_NAME as DESPATCH_NAME,
                        e.DataDate as UnboxingDataDate,
                        f.DataDate as SiteCargoDataDate,
                        g.DataDate as ShortCargoDataDate,
                        h.MODIFYBY,a.UploadOpe,
                        i.CONSOL_CODE,i.CONSOL_TYPE,i.CONSOL_NAME,i.CONSOL_URL
                        from [jetf].[dbo].CptSeaMainNumberDetail a
                        left join (select * from DATA_CENTER.dbo.SEA_ORDER_EDIT where GW > 0) b on a.MainNumber = b.MAINNUMBER and a.BagNumber = b.BL_NO
                        left join [DATA_CENTER].[dbo].[SYS_CUST] c on b.DESPATCH_NAME = c.CUST_CODE
                        left join [jetf].[dbo].SeaUnboxingRecord e on a.MainNumber=e.MainNumber
                        left join [jetf].[dbo].SeaSiteCargo f on a.MainNumber=e.MainNumber and a.BagNumber=f.BagNumber
                        left join [jetf].[dbo].SeaShortCargo g on a.MainNumber=e.MainNumber and a.BagNumber=g.BagNumber
                        left join cte_CES_MAIN_ORDER h on a.MainNumber = h.MAIN_NUMBER
	                    left join [DATA_CENTER].[dbo].[Sys_cust] i on b.DESPATCH_NAME= i.CUST_CODE
                        where
                        exists (
                                      select 1 from @BagNumberTable
                                      where BagNumber = a.BagNumber
                                )
                        and exists (
                                    select 1 from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 
                                    where a.MainNumber = MainNumber and a.BagNumber = BL_NO
                                    and STATUS <> 'E' and GW > 0
                                   )
                   ";

            var sb = new StringBuilder();
            foreach (var item in mainNumberList.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @BagNumberTable VALUES {string.Join(",",
                item.Select(r => $"('{r}')"))};");
            }

            sql = string.Format(sql, sb.ToString());

            var result = conn.Query<SeaUnreceivedOrderModel>(sql).ToList();

            // 當 CUST_CODE='CN00060' 且 SIHNO 有值時，將 CONSOL_NAME 設為 SIHNO
            result.Where(r => r.Cust_Code == "CN00060" && !string.IsNullOrEmpty(r.Sihno))
                  .ToList()
                  .ForEach(r => r.Consol_Name = r.Sihno);

            //資料處理
            ProcessData(result, SeaErrorReportEnum.Declare);

            return result;
        }

        /// <summary>
        /// 取得海快錯單作業
        /// </summary>
        /// <param name="mainnumber"></param>
        /// <returns></returns>
        private DataTable GetCesMainOrder(string mainnumber)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from [DATA_CENTER].[dbo].[CES_MAIN_ORDER] where MAIN_NUMBER=@MAIN_NUMBER ");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@MAIN_NUMBER", SqlDbType.NVarChar).Value = mainnumber;
                da.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// 取得海快具結
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        private List<CesMainOrderDetailModel> GetCesMainOrderDetail(List<SeaUnreceivedOrderModel> list)
        {
            if (list.Count == 0)
                return new List<CesMainOrderDetailModel>();

            string sql = @"
                            declare @SeaTransmittable Table
                            ( 
	                            MainNumber nvarchar(100),
                                BL_NO nvarchar(100),
                                CorrectImporterName nvarchar(100),
                                CorrectImporterId nvarchar(100),
                                CorrectImporterPhone nvarchar(100)
                            )

                           {0}

                            SELECT 
                            a.BL_NO,a.MAINNUMBER,a.CorrectImporterName,a.CorrectImporterId,a.CorrectImporterPhone,
                            b.MANIFEST,
                            c.ITEM_NO,c.JETF_ID,c.TERMSOFPRICE,c.CURRENCY,c.GW,c.PIECE,c.PIECE_UNIT,c.MARKS,c.ITEM_NAME,c.CCC_CODE,c.TRADEMARK,c.II_SPEC,c.NW,c.QUANTITY,
                            c.QUANTITY_UNIT,c.UNIT_PRICE,c.INVOICE_AMOUNT,c.MEASUREMENT,c.CBM,c.MADEIN,c.EXPORTER,c.EX_COUNRTYCODE,c.EX_ADD,c.PARTY_IDENTIFIER,
                            c.IMPORTER_ID,c.IMPORTER,c.IM_PHONENO,
                            c.IM_ADD,c.CONT_TYPE as E_CONT_TYPE,c.CONT_NO as E_CONT_NO,c.SEALNO as E_SEALNO,c.DECLARATION_2,c.TAXFEE_DECLARED,c.TRANS_NAME,c.JETF_SERIAL,c.SIZE,
                            d.CUST_NAME as DESPATCH_NAME,
                            e.CONT_TYPE as O_CONT_TYPE,e.CONT_NO as O_CONT_NO,e.SEALNO as O_SEALNO
                            FROM @SeaTransmittable a
                            left join [jetf].[dbo].[SEA_MANIFEST_UPLOAD] b on a.MAINNUMBER=b.MAINNUMBER and a.BL_NO=b.BL_NO
                            left join [DATA_CENTER].[dbo].[SEA_ORDER_EDIT] c on a.MAINNUMBER=c.MAINNUMBER and a.BL_NO = c.BL_NO
                            left join [DATA_CENTER].[dbo].[SYS_CUST] d on c.DESPATCH_NAME = d.CUST_CODE
                            left join (select * from [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] where GW > 0 ) e on a.MAINNUMBER=e.MAINNUMBER and a.BL_NO = e.BL_NO
                        ";

            var sb = new StringBuilder();
            foreach (var item in list.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @SeaTransmittable VALUES {string.Join(",",
                   item.Select(r => $"('{r.MainNumber}','{r.BagNumber}',N'{r.CorrectImporterName}','{r.CorrectImporterId}','{r.CorrectImporterPhone}')"))};");
            }

            sql = string.Format(sql, sb.ToString());
            var result = conn.Query<CesMainOrderDetailModel>(sql, commandTimeout: 600).ToList();

            // 建立字典以便快速查找
            var consolInfoDict = list
                .GroupBy(r => new { r.MainNumber, r.BagNumber })
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            result.ForEach(r =>
            {
                var key = new { MainNumber = r.MAINNUMBER, BagNumber = r.BL_NO };
                if (consolInfoDict.TryGetValue(key, out var consolInfo))
                {
                    r.CONSOL_CODE = consolInfo.Consol_Code;
                    r.CONSOL_TYPE = consolInfo.Consol_Type;
                    r.CONSOL_NAME = consolInfo.Consol_Name;
                    r.CONSOL_URL = consolInfo.Consol_Url;
                }

                r.IMPORTER = string.IsNullOrEmpty(r.CorrectImporterName) ? r.IMPORTER : r.CorrectImporterName;
                r.IMPORTER_ID = string.IsNullOrEmpty(r.CorrectImporterName) ? r.IMPORTER_ID : r.CorrectImporterId;
                r.IM_PHONENO = string.IsNullOrEmpty(r.CorrectImporterName) ? r.IM_PHONENO : r.CorrectImporterPhone;
            });

            return result;
        }

        /// <summary>
        /// 未收單資料處理
        /// </summary>
        /// <param name="item"></param>
        void UnreceivedOrderProcessData(SeaUnreceivedOrderModel item) 
        {
            //當上傳作業人員(UploadOpe)不為空值且正確進口人姓名(CorrectImporterName)空值，當作沒上傳
            if (!string.IsNullOrEmpty(item.UploadOpe) && string.IsNullOrWhiteSpace(item.CorrectImporterName))
            {
                item.CorrectImporterName = string.Empty;
                item.CorrectImporterId = string.Empty;
                item.CorrectImporterPhone = string.Empty;
                item.ServiceDate = null;
            }

            //正確進口人姓名(CorrectImporterName)為空值
            //錯誤原因代碼(最新))為B6F時，且I欄(預委回覆代碼)為00，只能有一筆錯單
            if (string.IsNullOrWhiteSpace(item.CorrectImporterName)
                && item.LastGb353RejReasonCode.Count == 1
                && item.LastGb353RejReasonCode.Contains("B6F") 
                && item.Reply_Code == "00")
            {
                //使用製單資料
                item.CorrectImporterName = item.Importer;
                item.CorrectImporterId = item.Importer_Id;
                item.CorrectImporterPhone = item.Im_PhoneNo;
                item.ServiceDate = DateTime.Now;
            }

            //邏輯五:錯誤原因說明(依新-->舊))的最新日期，與客服提供日期相同
            //取消異動紀錄，不將資料填寫至Z(客服提供日期)、AA(正確姓名)、AB(正確ID)、AC(正確進口人電話)欄位
            var issueDateTime = item.Gb353RejReasonList.OrderByDescending(r => r.IssueDateTime).FirstOrDefault()?.IssueDateTime;
            if (item.ServiceDate.HasValue && DateTime.TryParse(issueDateTime,out var dateTime))
            {
                if (dateTime.Date == item.ServiceDate.Value.Date)
                {
                    item.CorrectImporterName = string.Empty;
                    item.CorrectImporterId = string.Empty;
                    item.CorrectImporterPhone = string.Empty;
                    item.ServiceDate = null;
                }
            }

            // 判斷是否需要更新預委 - 只要符合任一條件就是 false (N)，全部不符合才是 true (Y)
            bool needUpdate = true;

            // 條件1: 客服提供日期有沒有值 => N
            if (!item.ServiceDate.HasValue)
            {
                needUpdate = false;
            }
            // 條件2: 正確進口人電話非"TEL09"開頭 => N
            else if (string.IsNullOrEmpty(item.CorrectImporterPhone) || !item.CorrectImporterPhone.StartsWith("TEL09"))
            {
                needUpdate = false;
            }
            // 條件3: 正確ID以"NO"開頭 => N
            else if (!string.IsNullOrEmpty(item.CorrectImporterId) && item.CorrectImporterId.StartsWith("NO"))
            {
                needUpdate = false;
            }
            // 條件4: 正確ID有值且不等於10碼 => N
            else if (!string.IsNullOrEmpty(item.CorrectImporterId) && item.CorrectImporterId.Length != 10)
            {
                needUpdate = false;
            }
            // 條件5: 進口人統一編號及正確ID非空白且填寫值相同 => N
            else if (!string.IsNullOrEmpty(item.Importer_Id) && 
                     !string.IsNullOrEmpty(item.CorrectImporterId) && 
                     item.Importer_Id == item.CorrectImporterId)
            {
                needUpdate = false;
            }
            // 條件6: 進口人統一編號及正確ID皆為空白時，進口人電話=正確進口人電話 => N
            else if (string.IsNullOrEmpty(item.Importer_Id) && 
                     string.IsNullOrEmpty(item.CorrectImporterId) && 
                     item.Im_PhoneNo == item.CorrectImporterPhone)
            {
                needUpdate = false;
            }

            item.IsUpdateApprovalNew = needUpdate;
        }


        /// <summary>
        /// 可傳輸資料處理
        /// </summary>
        /// <param name="item"></param>
        void TransmittableProcessData(SeaUnreceivedOrderModel r)
        {
            //未收單 && 正確姓名有值，就使用上傳的資料
            //不用判斷上傳是不是空值使用製單資料
            if (r.IsReceiveOrder == false && string.IsNullOrEmpty(r.CorrectImporterName) == false)
            {
                //客服提供日期
                r.ServiceDate = r.ServiceDate.HasValue ? DateTime.Now : r.ServiceDate;
                //正確姓名
                //r.CorrectImporterName = string.IsNullOrEmpty(r.CorrectImporterName) ? r.Importer : r.CorrectImporterName;

                //正確ID
                //r.CorrectImporterId = string.IsNullOrEmpty(r.CorrectImporterId) ? r.Importer_Id : r.CorrectImporterId;

                //正確進口人電話
                //r.CorrectImporterPhone = string.IsNullOrEmpty(r.CorrectImporterPhone) ? r.Im_PhoneNo : r.CorrectImporterPhone;
            }
            else
            {
                //沒有異動資料才會到這邊
                //使用製單資料
                r.CorrectImporterName = r.Importer;
                r.CorrectImporterId = r.Importer_Id;
                r.CorrectImporterPhone = r.Im_PhoneNo;
                //客服提供日期
                r.ServiceDate = DateTime.Now;
            }

            //比對是否相符，任一邏輯比對不符，則顯示【Y】，所有邏輯皆比對相符，則顯示【N】
            r.IsImport = r.CorrectImporterName != r.Importer 
                        || r.CorrectImporterId != r.Importer_Id
                        || r.CorrectImporterPhone != r.Im_PhoneNo;
        }


        /// <summary>
        /// 資料處理
        /// </summary>
        public void ProcessData(List<SeaUnreceivedOrderModel> list, SeaErrorReportEnum report)
        {
            //工作天
            var workDay = _workDayService.GetWorkDay();

            list.ForEach(r =>
            {
                //取得最後傳輸日
                if (r.Eta.HasValue)
                {
                    switch (r.ModifyBy)
                    {
                        case "高雄郵聯(億興)":
                        case "高雄郵聯(全旺)":
                            r.LastDataDate = _workDayService.AddWorkDays(workDay.Item1, workDay.Item2, r.Eta.Value, 3);
                            break;
                        case "TPCT(捷豐)":
                            r.LastDataDate = r.Eta.Value.AddDays(6);
                            break;
                    }
                }

                switch (report)
                { 
                    //未收單
                    case SeaErrorReportEnum.UnreceivedOrder:
                        UnreceivedOrderProcessData(r);
                        break;
                    //可傳輸
                    case SeaErrorReportEnum.Transmittable:
                        TransmittableProcessData(r);
                        break;
                    case SeaErrorReportEnum.Declare:
                        //客服提供日期
                        r.ServiceDate = r.ServiceDate.HasValue ? DateTime.Now : r.ServiceDate;
                        //未收單 && 正確姓名有值，就使用上傳的資料
                        //不用判斷上傳是不是空值使用製單資料
                        if (r.IsReceiveOrder == false && string.IsNullOrEmpty(r.CorrectImporterName) == false)
                        {
                            //正確姓名
                            //r.CorrectImporterName = string.IsNullOrEmpty(r.CorrectImporterName) ? r.Importer : r.CorrectImporterName;

                            //正確ID
                            //r.CorrectImporterId = string.IsNullOrEmpty(r.CorrectImporterId) ? r.Importer_Id : r.CorrectImporterId;

                            //正確進口人電話
                            //r.CorrectImporterPhone = string.IsNullOrEmpty(r.CorrectImporterPhone) ? r.Im_PhoneNo : r.CorrectImporterPhone;
                        }
                        else
                        {
                            //使用製單資料
                            r.CorrectImporterName = r.Importer;
                            r.CorrectImporterId = r.Importer_Id;
                            r.CorrectImporterPhone = r.Im_PhoneNo;
                        }

                        r.IsMatch = (r.Id == r.CorrectImporterId && r.CorrectImporterPhone == $"TEL{r.Tel}") ? true : false;
                        break;
                }
            });
        }
    }
}
