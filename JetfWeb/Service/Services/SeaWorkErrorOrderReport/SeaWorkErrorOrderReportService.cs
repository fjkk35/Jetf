using Dapper;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Dml.Chart;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Models.SeaUnreceivedOrder;
using Service.Models.SeaWorkErrorOrderReport;
using Service.Services.WorkDay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaWorkErrorOrderReport
{
    public class SeaWorkErrorOrderReportService : _BaseService
    {
        private readonly WorkDayService _workDayService;

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent, cs_Percent2, dateStyle, date2Style;

        public SeaWorkErrorOrderReportService(WorkDayService workDayService)
        {
            _workDayService = workDayService;
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public IWorkbook GetSeaWorkErrorOrderWorkbook(List<string> mainNumberList)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得海快錯單袋號資料
            var seaWorkErrorOrders = GetSeaWorkErrorOrders(mainNumberList);

            //取得海快錯單作業-倉單筆數
            var seaManifestCounts = GetSeaManifestCount(mainNumberList);

            //取得需預委任筆數
            var seaB6FCount = GetSeaB6FCount(mainNumberList);

            //取得船舶航次
            var fieldADic = GetSeaMainNumberFieldA(mainNumberList);

            //取得海快未有狀態
            var seaNoStatus = GetSeaNoStatus(mainNumberList);

            //產生EXCEL
            //海快錯單統計
            GetSeaBagNoWorkReportSheet(
                workbook, 
                seaWorkErrorOrders, 
                seaManifestCounts, 
                fieldADic,
                seaB6FCount,
                "海快錯單統計");

            //海快錯單明細
            GetSeaBagNoWorkDetailsSheet(workbook, seaWorkErrorOrders, "海快錯單明細");

            //海快未有狀態
            GetSeaNoStatusSheet(workbook, seaNoStatus);

            return workbook;
        }


        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-海快錯單統計
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="sheetName"></param>
        void GetSeaBagNoWorkReportSheet(
            IWorkbook workbook, 
            List<SeaWorkErrorOrderReportModel> list, 
            List<SeaManifestModel> seaManifestCount, 
            Dictionary<string, string> fieldADic,
            Dictionary<string, int> seaB6FCount,
            string sheetName)
        {
            var date = DateTime.Now;

            //工作天
            var workDay = _workDayService.GetWorkDay();

            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            DateTime eta;
            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("客戶");
            row.CreateCell(1).SetCellValue("船班");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("船掛");
            row.CreateCell(4).SetCellValue("清關業者");
            row.CreateCell(5).SetCellValue("分艙單傳輸日");
            row.CreateCell(6).SetCellValue("艙單到港日");
            row.CreateCell(7).SetCellValue("報單傳輸狀態");
            row.CreateCell(8).SetCellValue("最後傳輸日");

            row.CreateCell(9).SetCellValue("短到日期");
            row.CreateCell(10).SetCellValue("是否開立短到單");
            row.CreateCell(11).SetCellValue("短到票數");
            row.CreateCell(12).SetCellValue("溢卸票數");
            row.CreateCell(13).SetCellValue("異常比例");
            row.CreateCell(14).SetCellValue("接收日期");

            row.CreateCell(15).SetCellValue("B6F(已收單)");
            row.CreateCell(16).SetCellValue("B6A");
            row.CreateCell(17).SetCellValue("B6B");
            row.CreateCell(18).SetCellValue("B6D");
            row.CreateCell(19).SetCellValue("B6E");
            row.CreateCell(20).SetCellValue("B6F(未收單)");
            row.CreateCell(21).SetCellValue("A03");
            row.CreateCell(22).SetCellValue("身分證錯誤");
            row.CreateCell(23).SetCellValue("在裝貨港前兩碼為CN、HK、MO時，須填列中文姓名");
            row.CreateCell(24).SetCellValue("其他錯單");
            row.CreateCell(25).SetCellValue("無法傳輸總票數");
            row.CreateCell(26).SetCellValue("傳輸總票數");
            row.CreateCell(27).SetCellValue("錯單比例(實際)");
            row.CreateCell(28).SetCellValue("錯單比例(含B6F收單)");


            for (int i = 0; i < 29; i++)
            {
                row.GetCell(i).CellStyle = cs_Center;
                sheet.SetColumnWidth(i, 5000);
            }

            sheet.SetColumnWidth(28, 6000);

            var excludeCodes = new List<string>
            {
                "B6A",
                "B6B",
                "B6D",
                "B6E",
                "B6F",
                "A03",
                //"N",
                "身分證錯誤請提供正確",
                "在裝貨港前兩碼為CN、HK、MO時，須填列中文姓名"
            };

            var dt_Group = from t in list.AsEnumerable()
                           group t by new
                           {
                               DATADATE = t.DATADATE,
                               DESPATCH_NAME = t.DESPATCH_NAME,
                               MAINNUMBER = t.MAINNUMBER,
                               MODIFYBY =t.MODIFYBY,
                               ETA = t.ETA,
                               Gb326ImportDate = t.Gb326ImportDate,
                           } into g
                           let b6fNoCount = g.Count(t => t.ReasonCodeByReport == "B6F" && !t.IsReceiveOrder)
                           orderby g.Key.MAINNUMBER
                           select new
                           {
                               DATADATE = g.Key.DATADATE,
                               DESPATCH_NAME = g.Key.DESPATCH_NAME,
                               VESSEL = g.Max(t => t.VESSEL),
                               MAINNUMBER = g.Key.MAINNUMBER,
                               MODIFYBY = g.Key.MODIFYBY,
                               ETA = g.Key.ETA,
                               Gb326ImportDate = g.Key.Gb326ImportDate,
                               B6A = g.Count(t => t.ReasonCodeByReport == "B6A"),
                               B6B = g.Count(t => t.ReasonCodeByReport == "B6B"),
                               B6D = g.Count(t => t.ReasonCodeByReport == "B6D"),
                               B6E = g.Count(t => t.ReasonCodeByReport == "B6E"),
                               B6FYES = seaB6FCount.ContainsKey(g.Key.MAINNUMBER) ? seaB6FCount[g.Key.MAINNUMBER] - b6fNoCount :　0,
                               B6FNO = b6fNoCount,
                               A03 = g.Count(t => t.ReasonCodeByReport == "A03"),
                               //N = g.Count(t => t.ReasonCode == "N"),
                               IDERROR = g.Count(t => t.ReasonCodeByReport == "身分證錯誤請提供正確"),
                               CNHKMOName = g.Count(t => t.ReasonCodeByReport == "在裝貨港前兩碼為CN、HK、MO時，須填列中文姓名"),
                               OTHER = g.Count(t =>
                                          !string.IsNullOrEmpty(t.ReasonCodeByReport) &&
                                          !excludeCodes.Contains(t.ReasonCodeByReport))
                            };

            int irow = 1;
            IFormatProvider ifp = new CultureInfo("zh-TW", true);
            foreach (var item in dt_Group)
            {
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(item.DESPATCH_NAME);//客戶
                row.CreateCell(1).SetCellValue(item.VESSEL);//船班
                row.CreateCell(2).SetCellValue(item.MAINNUMBER);//主提單號

                //船掛
                if (fieldADic.ContainsKey(item.MAINNUMBER))
                    row.CreateCell(3).SetCellValue(fieldADic[item.MAINNUMBER]);

                row.CreateCell(4).SetCellValue(item.MODIFYBY);//清關業者
                row.CreateCell(5).SetCellValue(date.ToString("MM/dd"));//分艙單傳輸日

                //艙單到港日
                var importDate = item.Gb326ImportDate.ToDateTime("yyyyMMdd");
                if (importDate.HasValue)
                {
                    row.CreateCell(6).SetCellValue(importDate.Value.ToString("yyyy/MM/dd"));//艙單到港日
                    row.GetCell(6).CellStyle = date2Style;
                }

                row.CreateCell(7).SetCellValue("V");//報單傳輸狀態

                if (DateTime.TryParse(item.ETA, out eta))
                {
                    if (item.MODIFYBY != null && item.MODIFYBY.IndexOf("高雄郵聯") > -1)
                    {
                        row.CreateCell(8).SetCellValue(_workDayService.AddWorkDays(workDay.Item1, workDay.Item2, eta, 3));//最後傳輸日
                        row.GetCell(8).CellStyle = date2Style;
                    }
                    else if (item.MODIFYBY != null && 
                        (
                            item.MODIFYBY.IndexOf("TPCT", StringComparison.OrdinalIgnoreCase) > -1 ||
                            item.MODIFYBY.IndexOf("基隆港務", StringComparison.OrdinalIgnoreCase) > -1
                        ))
                    {
                        row.CreateCell(8).SetCellValue(eta.AddDays(+6));//最後傳輸日
                        row.GetCell(8).CellStyle = date2Style;
                    }
                }

                //row.CreateCell(9).SetCellValue("");//短到日期
                //row.CreateCell(10).SetCellValue("");//是否開立短到單
                //row.CreateCell(11).SetCellValue("");//短到票數
                //row.CreateCell(12).SetCellValue("");//溢卸票數
                //row.CreateCell(13).SetCellValue("");//異常比例

                //報單接收日期
                row.CreateCell(14).SetCellValue(date.ToString("MM/dd"));
                row.GetCell(14).CellStyle = date2Style;

                //B6F(已收單)
                row.CreateCell(15).SetCellValue(item.B6FYES);
                //B6A
                row.CreateCell(16).SetCellValue(item.B6A);
                //B6B
                row.CreateCell(17).SetCellValue(item.B6B);
                //B6D
                row.CreateCell(18).SetCellValue(item.B6D);
                //B6E
                row.CreateCell(19).SetCellValue(item.B6E);
                //B6F 未收單
                row.CreateCell(20).SetCellValue(item.B6FNO);
                //A03
                row.CreateCell(21).SetCellValue(item.A03);
                //身分證錯誤
                row.CreateCell(22).SetCellValue(item.IDERROR);
                //在裝貨港前兩碼為CN、HK、MO時，須填列中文姓名
                row.CreateCell(23).SetCellValue(item.CNHKMOName);
                //其他錯單
                row.CreateCell(24).SetCellValue(item.OTHER);
                //無法傳輸總票數
                row.CreateCell(25).CellFormula = $"SUM(R{irow + 1}:T{irow + 1},U{irow + 1}:Y{irow + 1})";

                //傳輸總票數
                //上傳倉單主號總筆數
                var count = seaManifestCount.Where(r => r.MAINNUMBER == item.MAINNUMBER).FirstOrDefault();
                if (count != null)
                    row.CreateCell(26).SetCellValue(count.TOTAL);//傳輸總票數
                //錯單比例(實際)
                row.CreateCell(27).CellFormula = $"Z{irow + 1}/AA{irow + 1}";
                row.GetCell(27).CellStyle = cs_Percent;

                //錯單比例(含B6F收單)
                row.CreateCell(28).CellFormula = $"SUM(P{irow + 1}:Y{irow + 1})/AA{irow + 1}";
                row.GetCell(28).CellStyle = cs_Percent;
                irow++;
            }
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-海快錯單明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="sheetName"></param>
        void GetSeaBagNoWorkDetailsSheet(IWorkbook workbook, List<SeaWorkErrorOrderReportModel> list, string sheetName)
        {
            //錯單資料
            var errorList = list.Where(r => string.IsNullOrEmpty(r.ReasonCodeByDetail) == false);

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行
            //styleWrapText.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            styleWrapText.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            int piece;
            double gw, amount, price;
            DateTime eta;
            DateTime date = DateTime.Now;

            //工作天
            var workDay = _workDayService.GetWorkDay();

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("航班主號");
            row.CreateCell(1).SetCellValue("分提單號碼");
            row.CreateCell(2).SetCellValue("客戶");
            row.CreateCell(3).SetCellValue("倉儲");
            row.CreateCell(4).SetCellValue("到港日");
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
            row.CreateCell(23).SetCellValue("是否需更新預委");
            row.CreateCell(24).SetCellValue("客服提供日期");
            row.CreateCell(25).SetCellValue("正確ID");
            row.CreateCell(26).SetCellValue("正確姓名");
            row.CreateCell(27).SetCellValue("正確進口人電話");
            row.CreateCell(28).SetCellValue("正確品名");
            row.CreateCell(29).SetCellValue("正確金額");
            row.CreateCell(30).SetCellValue("今天客服狀態");
            row.CreateCell(31).SetCellValue("累積處置說明");

            for (int i = 0; i < 32; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }
            sheet.SetColumnWidth(8, 7000);
            sheet.SetColumnWidth(9, 10000);


            var irow = 1;
            foreach (var item in errorList)
            {
                row = sheet.CreateRow(irow);
                //航班主號
                row.CreateCell(0).SetCellValue(item.MAINNUMBER);
                //分提單號碼
                row.CreateCell(1).SetCellValue(item.BAGNUMBER);
                //客戶
                row.CreateCell(2).SetCellValue(item.DESPATCH_NAME);
                //倉儲
                row.CreateCell(3).SetCellValue(item.MODIFYBY);
                if (DateTime.TryParse(item.ETA, out eta))
                {
                    //預計到港日
                    row.CreateCell(4).SetCellValue(eta.ToString("MM/dd"));

                    //最後傳輸日
                    if (item.MODIFYBY != null && item.MODIFYBY.IndexOf("高雄郵聯") > -1)
                    {
                        //清關業者為高雄郵聯，到港日+3天(工作天)
                        row.CreateCell(6).SetCellValue(_workDayService.AddWorkDays(workDay.Item1, workDay.Item2, eta, 3));//報單最後傳輸日
                        row.GetCell(6).CellStyle = date2Style;
                    }
                    else if (item.MODIFYBY != null &&
                            (
                                item.MODIFYBY.IndexOf("TPCT", StringComparison.OrdinalIgnoreCase) > -1 ||
                                item.MODIFYBY.IndexOf("基隆港務", StringComparison.OrdinalIgnoreCase) > -1
                            ))
                    {
                        row.CreateCell(6).SetCellValue(eta.AddDays(+6));//最後傳輸日
                        row.GetCell(6).CellStyle = date2Style;
                    }
                }

                //錯誤原因代碼(最新)
                row.CreateCell(8).SetCellValue(item.ReasonCodeByDetail);
                row.GetCell(8).CellStyle = styleWrapText;
                //錯誤原因說明(依新-->舊)
                if (item.Gb353RejReasonList.Any()) 
                {
                    row.CreateCell(9).SetCellValue(string.Join("\r\n", item.Gb353RejReasonList.Select(x => $"{x.IssueDateTime}，{x.RejReasonCode}")));
                    row.CreateCell(10).SetCellValue(item.Gb353Count);

                    row.GetCell(9).CellStyle = styleWrapText;
                }
                //進口人英文名稱
                row.CreateCell(11).SetCellValue(item.IMPORTER);
                //進口人統一編號
                row.CreateCell(12).SetCellValue(item.IMPORTER_ID);
                //進口人電話
                row.CreateCell(13).SetCellValue(item.IM_PHONENO);
                //毛重
                row.CreateCell(14).SetCellValue(item.GW);
                //件數
                row.CreateCell(15).SetCellValue(item.PIECE);
                //貨物名稱
                row.CreateCell(16).SetCellValue(item.ITEM_NAME);
                //單價金額
                row.CreateCell(17).SetCellValue(item.UNIT_PRICE);
                //發票總金額
                row.CreateCell(18).SetCellValue(item.INVOICE_AMOUNT);
                //進口人英文地址
                row.CreateCell(19).SetCellValue(item.IM_ADD);
                //派件公司
                row.CreateCell(20).SetCellValue(item.TRANS_NAME);
                //配送單號
                row.CreateCell(21).SetCellValue(item.JETF_SERIAL);
                //LPNO
                row.CreateCell(22).SetCellValue(item.LPNO);

                irow++;
            }
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-海快未有狀態
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="sheetName"></param>
        void GetSeaNoStatusSheet(IWorkbook workbook, List<SeaNoStatusModel> list)
        {
            ISheet sheet = workbook.CreateSheet("未有狀態");
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主提單號");
            row.CreateCell(1).SetCellValue("分提單號");
            row.CreateCell(2).SetCellValue("是否為後段報關");

            for (int i = 0; i < 32; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }
            sheet.SetColumnWidth(8, 7000);
            sheet.SetColumnWidth(9, 10000);


            var irow = 1;
            foreach (var item in list)
            {
                row = sheet.CreateRow(irow);
                //主提單號
                row.CreateCell(0).SetCellValue(item.MainNumber);
                //分提單號
                row.CreateCell(1).SetCellValue(item.BagNumber);
                //是否為後段報關
                row.CreateCell(2).SetCellValue(item.IsPostEntry);

                irow++;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        public List<SeaWorkErrorOrderReportModel> GetSeaWorkErrorOrders(List<string> mainNumberList)
        {
            if (mainNumberList == null || !mainNumberList.Any())
                return new List<SeaWorkErrorOrderReportModel>();

            // 1. 取得主要資料 (CptSeaMainNumberDetail)
            var sqlMain = @"
SELECT 
    a.MAINNUMBER,
    a.BagNumber,
    a.Gb353RejReasonCode,
    a.IsReceiveOrder,
    a.Gb353RejReasonDesc,
    a.Gb353RejReason,
    a.Gb326ImportDate
FROM [jetf].[dbo].[CptSeaMainNumberDetail] a 
WHERE a.MainNumber IN @MainNumberList";

            var mainData = conn.Query<SeaWorkErrorOrderReportModel>(sqlMain, 
                new { MainNumberList = mainNumberList }, 
                commandTimeout: 600).ToList();

            if (!mainData.Any())
                return mainData;

            // 2. 取得船舶資料 (SEA_MANIFEST_UPLOAD) - 建立字典
            var sqlVessel = @"
SELECT 
    b.MAINNUMBER,
    b.BL_NO,
    b.VESSEL,
    b.MANIFEST
FROM [jetf].[dbo].[SEA_MANIFEST_UPLOAD] b 
WHERE b.MAINNUMBER IN @MainNumberList";

            var vesselData = conn.Query<dynamic>(sqlVessel, 
                new { MainNumberList = mainNumberList }, 
                commandTimeout: 600).ToList();
            var vesselDict = vesselData
                .GroupBy(x => $"{(string)x.MAINNUMBER}_{(string)x.BL_NO}")
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            // 3. 取得訂單資料 (SEA_ORDER_EDIT) - 建立字典
            var sqlOrder = @"
SELECT 
    c.MAINNUMBER,
    c.BL_NO,
    jetf.dbo.GetCUSTOMER('海運',c.DESPATCH_NAME) as DESPATCH_NAME,
    c.ETA,
    c.GW,
    c.PIECE,
    c.ITEM_NO,
    c.ITEM_NAME,
    c.NW,
    c.UNIT_PRICE,
    c.INVOICE_AMOUNT,
    c.MADEIN,
    c.IMPORTER_ID,
    c.IMPORTER,
    c.IM_PHONENO,
    c.IM_ADD,
    c.TRANS_NAME,
    c.JETF_SERIAL,
    c.LPNO
FROM [DATA_CENTER].[dbo].[SEA_ORDER_EDIT] c 
WHERE c.MAINNUMBER IN @MainNumberList
  AND c.GW > 0";

            var orderData = conn.Query<dynamic>(sqlOrder, 
                new { MainNumberList = mainNumberList }, 
                commandTimeout: 600).ToList();
            var orderDict = orderData
                .GroupBy(x => $"{(string)x.MAINNUMBER}_{(string)x.BL_NO}")
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            // 4. 取得清關業者資料 (CES_MAIN_ORDER + SYS_PARAM) - 建立字典
            var sqlClearance = @"
SELECT 
    f.MAIN_NUMBER,
    g.NAME as MODIFYBY 
FROM [DATA_CENTER].[dbo].[CES_MAIN_ORDER] f 
LEFT JOIN [DATA_CENTER].[dbo].[SYS_PARAM] g 
    ON f.CLEARANCE_CP = g.CODE AND g.TYPE = 'CLEARANCE_CP' 
WHERE f.MAIN_NUMBER IN @MainNumberList
  AND f.TYPE = 'ER'";

            var clearanceData = conn.Query<dynamic>(sqlClearance, 
                new { MainNumberList = mainNumberList }, 
                commandTimeout: 600).ToList();
            var clearanceDict = clearanceData
                .GroupBy(x => (string)x.MAIN_NUMBER)
                .ToDictionary(
                    g => g.Key,
                    g => (string)g.First().MODIFYBY
                );

            // 5. 取得錯誤訂單資料 (SeaWorkErrorOrder) - 建立字典
            var sqlErrorOrder = @"
SELECT 
    h.MainNumber,
    h.BagNumber,
    h.Reason,
    h.UploadTime 
FROM [jetf].[dbo].[SeaWorkErrorOrder] h 
WHERE h.MainNumber IN @MainNumberList";

            var errorOrderData = conn.Query<dynamic>(sqlErrorOrder, 
                new { MainNumberList = mainNumberList }, 
                commandTimeout: 600).ToList();
            var errorOrderDict = errorOrderData
                .GroupBy(x => $"{(string)x.MainNumber}_{(string)x.BagNumber}")
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            // 6. 組合資料
            foreach (var item in mainData)
            {
                var key = $"{item.MAINNUMBER}_{item.BAGNUMBER}";

                // 組合船舶資料
                if (vesselDict.TryGetValue(key, out var vesselRecord))
                {
                    item.VESSEL = vesselRecord.VESSEL?.ToString();
                    item.MANIFEST = vesselRecord.MANIFEST?.ToString();
                }

                // 組合訂單資料
                if (orderDict.TryGetValue(key, out var orderRecord))
                {
                    item.DESPATCH_NAME = orderRecord.DESPATCH_NAME?.ToString();
                    item.ETA = orderRecord.ETA is DateTime etaDate 
                        ? etaDate.ToString("yyyy/MM/dd") 
                        : orderRecord.ETA?.ToString();
                    item.GW = orderRecord.GW?.ToString();
                    item.PIECE = orderRecord.PIECE?.ToString();
                    item.ITEM_NO = orderRecord.ITEM_NO?.ToString();
                    item.ITEM_NAME = orderRecord.ITEM_NAME?.ToString();
                    item.NW = orderRecord.NW?.ToString();
                    item.UNIT_PRICE = orderRecord.UNIT_PRICE?.ToString();
                    item.INVOICE_AMOUNT = orderRecord.INVOICE_AMOUNT?.ToString();
                    item.IMPORTER_ID = orderRecord.IMPORTER_ID?.ToString();
                    item.IMPORTER = orderRecord.IMPORTER?.ToString();
                    item.IM_PHONENO = orderRecord.IM_PHONENO?.ToString();
                    item.IM_ADD = orderRecord.IM_ADD?.ToString();
                    item.TRANS_NAME = orderRecord.TRANS_NAME?.ToString();
                    item.JETF_SERIAL = orderRecord.JETF_SERIAL?.ToString();
                    item.LPNO = orderRecord.LPNO?.ToString();
                }

                // 組合清關業者資料
                if (clearanceDict.TryGetValue(item.MAINNUMBER, out var modifyBy))
                {
                    item.MODIFYBY = modifyBy;
                }

                // 組合錯誤訂單資料
                if (errorOrderDict.TryGetValue(key, out var errorRecord))
                {
                    item.Reason = errorRecord.Reason?.ToString();
                    item.ReasonUploadTime = errorRecord.UploadTime is DateTime uploadTime
                        ? uploadTime.ToString("yyyy/MM/dd HH:mm:ss")
                        : errorRecord.UploadTime?.ToString();
                }
            }

            return mainData;
        }

        /// <summary>
        /// 取得海快未有狀態
        /// </summary>
        /// <param name="mainNumberList"></param>
        public List<SeaNoStatusModel> GetSeaNoStatus(List<string> mainNumberList) 
        {
            var sql = @"
select a.MainNumber,a.BagNumber,b.POST_ENTRY 
from [jetf].[dbo].[CptSeaMainNumberDetail] a
left join [DATA_CENTER].[dbo].SEA_ORDER_ORIGINAL b on a.MainNumber=b.MAINNUMBER and a.BagNumber=b.BL_NO
where a.MainNumber IN @MainNumberList
and a.IsReceiveOrder= 0 and a.Gb353Status !='ok'
and not exists(
	select 1 from [jetf].[dbo].[SeaWorkErrorOrder]
	where MainNumber = a.MainNumber and BagNumber=a.BagNumber
)";

            return conn.Query<SeaNoStatusModel>(sql, 
                new { MainNumberList = mainNumberList }, 
                commandTimeout: 600).ToList();
        }


        /// <summary>
        /// 取得海快錯單作業-倉單筆數
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public List<SeaManifestModel> GetSeaManifestCount(List<string> mainNumberList)
        {
            var sql = @"
select MAINNUMBER, count(MAINNUMBER) as TOTAL 
from [jetf].[dbo].[SEA_MANIFEST_UPLOAD] a 
where a.MAINNUMBER IN @MainNumberList
group by MAINNUMBER";

            return conn.Query<SeaManifestModel>(sql, 
                new { MainNumberList = mainNumberList }).ToList();
        }

        /// <summary>
        /// 取得需預委任筆數（逐筆查詢優化版本）
        /// </summary>
        /// <param name="mainNumberList"></param>
        /// <returns></returns>
        public Dictionary<string,int> GetSeaB6FCount(List<string> mainNumberList)
        {
            if (mainNumberList == null || !mainNumberList.Any())
                return new Dictionary<string, int>();

            var sql = @"
SELECT SUM(RESULT_4_VALUE) as Total
FROM [DATA_CENTER].[dbo].[VIEW_SEA_MAIN_COUNT]
WHERE MAIN_NUMBER = @MainNumber
GROUP BY MAIN_NUMBER";

            var result = new Dictionary<string, int>();
            var lockObject = new object();

            // 使用並行處理加速查詢
            Parallel.ForEach(mainNumberList, new ParallelOptions { MaxDegreeOfParallelism = 5 }, mainNumber =>
            {
                try
                {
                    // 每個執行緒使用自己的連線
                    using (var tempConn = new SqlConnection(conn.ConnectionString))
                    {
                        tempConn.Open();
                        var total = tempConn.QueryFirstOrDefault<int?>(sql, new { MainNumber = mainNumber }, commandTimeout: 60);
                        
                        if (total.HasValue && total.Value > 0)
                        {
                            lock (lockObject)
                            {
                                result[mainNumber] = total.Value;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // 記錄錯誤但繼續處理其他主號
                    // 可以根據需求決定是否要記錄錯誤
                }
            });

            return result;
        }

        /// <summary>
        /// 取得海快錯單作業-船舶航次
        /// </summary>
        /// <param name="mainNumberList"></param>
        /// <returns></returns>
        public Dictionary<string,string> GetSeaMainNumberFieldA(List<string> mainNumberList)
        {
            var sql = @"
SELECT MAIN_NUMBER, max(FIELD_A) as FIELD_A
FROM [DATA_CENTER].[dbo].[CES_MAIN_ORDER]
WHERE MAIN_NUMBER IN @MainNumberList
GROUP BY MAIN_NUMBER";

            return conn.Query<(string MAIN_NUMBER, string FIELD_A)>(sql, 
                new { MainNumberList = mainNumberList })
                .ToDictionary(x => x.MAIN_NUMBER, x => x.FIELD_A);
        }

        public DataTable GetSeaMainNumberReturnCount(List<string> mainNumberList)
        {
            string sql = @"
                            declare @MainNumber Table
                            ( 
	                            MainNumber nvarchar(100)
                            )

                           {0}

                            select MAIN_NUMBER,sum(RESULT_4_VALUE) as TotalCount from [DATA_CENTER].[dbo].[VIEW_SEA_MAIN_COUNT] as A
                            where exists (select 1 from @MainNumber as B where A.MAIN_NUMBER =b.MainNumber)
                            group by MAIN_NUMBER
                        ";

            sql = string.Format(sql, $"INSERT INTO @MainNumber VALUES {string.Join(",", mainNumberList.Select(r => $"('{r}')"))};");

            DataTable dt = new DataTable();

            if (mainNumberList.Count > 0)
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.Fill(dt);
                }
            }
            return dt;
        }


        void GetWorkbookStyle(IWorkbook workbook)
        {
            //藍色的Style
            fontB = workbook.CreateFont();
            fontB.Color = NPOI.SS.UserModel.IndexedColors.Blue.Index;

            font1 = (XSSFFont)workbook.CreateFont();
            //標題
            cs_Title = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //標題
            cs_Title_Left = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title_Left.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Title_Left.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Title.BorderTop = BorderStyle.Thin;
            //cs_Title.BorderBottom = BorderStyle.Thin;
            //cs_Title.BorderLeft = BorderStyle.Thin;
            //cs_Title.BorderRight = BorderStyle.Thin;
            //cs_Title.SetFont(font1);

            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center.BorderTop = BorderStyle.Thin;
            //cs_Center.BorderBottom = BorderStyle.Thin;
            //cs_Center.BorderLeft = BorderStyle.Thin;
            //cs_Center.BorderRight = BorderStyle.Thin;

            cs_Center_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center_Blue.BorderTop = BorderStyle.Thin;
            //cs_Center_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Center_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Center_Blue.BorderRight = BorderStyle.Thin;
            cs_Center_Blue.SetFont(fontB);

            format = (XSSFDataFormat)workbook.CreateDataFormat();
            cs_Int = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int.BorderTop = BorderStyle.Thin;
            //cs_Int.BorderBottom = BorderStyle.Thin;
            //cs_Int.BorderLeft = BorderStyle.Thin;
            //cs_Int.BorderRight = BorderStyle.Thin;
            cs_Int.DataFormat = format.GetFormat("#,##0");

            cs_Int_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int_Blue.BorderTop = BorderStyle.Thin;
            //cs_Int_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Int_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Int_Blue.BorderRight = BorderStyle.Thin;
            cs_Int_Blue.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue.SetFont(fontB);

            cs_Double = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Double.BorderTop = BorderStyle.Thin;
            //cs_Double.BorderBottom = BorderStyle.Thin;
            //cs_Double.BorderLeft = BorderStyle.Thin;
            //cs_Double.BorderRight = BorderStyle.Thin;
            cs_Double.DataFormat = format.GetFormat("#,##0.000");

            cs_Percent = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.DataFormat = format.GetFormat("0.00%");
            cs_Percent.SetFont(font1);

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0.000%");
            cs_Percent2.SetFont(font1);


            dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            dateStyle.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm:ss");

            date2Style = (XSSFCellStyle)workbook.CreateCellStyle();
            date2Style.DataFormat = format.GetFormat("yyyy/mm/dd");

        }
    }
}
