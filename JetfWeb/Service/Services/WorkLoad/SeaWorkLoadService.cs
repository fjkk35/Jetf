using NPOI.OpenXmlFormats.Dml.Chart;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Services.WorkDay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.WorkLoad
{
    public partial class WorkLoadService
    {
        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent, cs_Percent2, dateStyle, date2Style;

        /// <summary>
        /// 海快錯單作業-Excel-Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public IWorkbook GetSeaBagNoWorkWorkbook(string source, string sDate, string eDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得海快錯單袋號資料
            DataTable dt = GetSeaBagNoWork(source, sDate, eDate, true).dt;

            //取得海快錯單作業-倉單筆數
            DataTable dt_Count = GetSeaManifestCount(source, sDate, eDate).dt;

            //取得海快錯單作業-主號須預委筆數
            var mainNumbers = dt.AsEnumerable().Select(r => r.Field<string>("MAINNUMBER")).Distinct().ToArray();
            DataTable dt_ReturnCount = GetSeaMainNumberReturnCount(mainNumbers);

            var returnCount = dt_ReturnCount.AsEnumerable().Select(r => new
            {
                MainNumber = r.Field<string>("MAIN_NUMBER"),
                TotalCount = r.Field<int>("TotalCount"),
            }).ToDictionary(r => r.MainNumber, r => r.TotalCount);

            //產生EXCEL
            //海快錯單統計
            GetSeaBagNoWorkReportSheet(workbook, dt, dt_Count, returnCount, "海快錯單統計");

            //海快錯單明細
            GetSeaBagNoWorkDetailsSheet(workbook, dt, "海快錯單明細");
            return workbook;
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-海快錯單明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="sheetName"></param>
        void GetSeaBagNoWorkDetailsSheet(IWorkbook workbook, DataTable dt, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            int piece;
            double gw, amount, price;
            DateTime eta;
            DateTime date = DateTime.Now;

            //工作天
            var workDay = _workDayService.GetWorkDay();

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("客戶");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("船班");
            row.CreateCell(3).SetCellValue("航班主號");
            row.CreateCell(4).SetCellValue("到港日");
            row.CreateCell(5).SetCellValue("最後傳輸日");
            row.CreateCell(6).SetCellValue("應回倉日期");
            row.CreateCell(7).SetCellValue("報單未傳訊息");
            row.CreateCell(8).SetCellValue("分提單號碼");
            row.CreateCell(9).SetCellValue("艙單號碼");
            row.CreateCell(10).SetCellValue("毛重");
            row.CreateCell(11).SetCellValue("件數");
            row.CreateCell(12).SetCellValue("貨物名稱");
            row.CreateCell(13).SetCellValue("單價金額");
            row.CreateCell(14).SetCellValue("發票總金額");
            row.CreateCell(15).SetCellValue("進口人統一編號");
            row.CreateCell(16).SetCellValue("進口人英文名稱");
            row.CreateCell(17).SetCellValue("現場有貨");
            row.CreateCell(18).SetCellValue("傳輸狀態");
            row.CreateCell(19).SetCellValue("客服提供日期");
            row.CreateCell(20).SetCellValue("正確ID");
            row.CreateCell(21).SetCellValue("正確姓名");
            row.CreateCell(22).SetCellValue("正確進口人電話");
            row.CreateCell(23).SetCellValue("客服狀態");
            row.CreateCell(24).SetCellValue("進口人電話");
            row.CreateCell(25).SetCellValue("進口人英文地址");
            row.CreateCell(26).SetCellValue("貨櫃種類");
            row.CreateCell(27).SetCellValue("貨櫃號碼");
            row.CreateCell(28).SetCellValue("封條號碼");
            row.CreateCell(29).SetCellValue("派件公司");
            row.CreateCell(30).SetCellValue("配送單號");
            row.CreateCell(31).SetCellValue("LP NO");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;

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

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["DESPATCH_NAME"].ToString());//客戶
                row.CreateCell(1).SetCellValue(dt.Rows[i]["MODIFYBY"].ToString());//倉儲
                row.CreateCell(2).SetCellValue(dt.Rows[i]["VESSEL"].ToString());//船班
                row.CreateCell(3).SetCellValue(dt.Rows[i]["MAINNUMBER"].ToString());//航班主號
                if (DateTime.TryParse(dt.Rows[i]["ETA"].ToString(), out eta))
                {
                    row.CreateCell(4).SetCellValue(eta);//到港日
                    row.GetCell(4).CellStyle = date2Style;

                    if (dt.Rows[i]["MODIFYBY"].ToString().IndexOf("高雄郵聯") > -1)
                    {
                        //清關業者為高雄時到港日+6天，其餘清關業者則空白
                        row.CreateCell(5).SetCellValue(_workDayService.AddWorkDays(workDay.Item1, workDay.Item2, eta, 3));//報單最後傳輸日
                        row.GetCell(5).CellStyle = date2Style;
                    }
                    else if (dt.Rows[i]["MODIFYBY"].ToString().IndexOf("tpct", StringComparison.OrdinalIgnoreCase) > -1 ||
                             dt.Rows[i]["MODIFYBY"].ToString().IndexOf("基隆港務", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        row.CreateCell(5).SetCellValue(eta.AddDays(+6));//最後傳輸日
                        row.GetCell(5).CellStyle = date2Style;
                    }
                }
                //row.CreateCell(6).SetCellValue("");//應回倉日期
                row.CreateCell(7).SetCellValue(dt.Rows[i]["MESSAGE"].ToString());//報單未傳訊息
                row.CreateCell(8).SetCellValue(dt.Rows[i]["BL_NO"].ToString());//分提單號碼
                row.CreateCell(9).SetCellValue(dt.Rows[i]["MANIFEST"].ToString());//艙單號碼
                if (double.TryParse(dt.Rows[i]["GW"].ToString().ToString(), out gw))
                {
                    row.CreateCell(10).SetCellValue(gw);//毛重
                }
                else
                {
                    row.CreateCell(10).SetCellValue(dt.Rows[i]["GW"].ToString());//毛重
                }
                if (int.TryParse(dt.Rows[i]["PIECE"].ToString(), out piece))
                {
                    row.CreateCell(11).SetCellValue(piece);//件數
                }
                else
                {
                    row.CreateCell(11).SetCellValue(dt.Rows[i]["PIECE"].ToString());//件數
                }
                row.CreateCell(12).SetCellValue(dt.Rows[i]["ITEM_NAME"].ToString());//貨物名稱
                if (double.TryParse(dt.Rows[i]["UNIT_PRICE"].ToString(), out price))
                {
                    row.CreateCell(13).SetCellValue(price);//單價金額
                }
                else
                {
                    row.CreateCell(13).SetCellValue(dt.Rows[i]["UNIT_PRICE"].ToString());//單價金額
                }
                if (double.TryParse(dt.Rows[i]["INVOICE_AMOUNT"].ToString(), out amount))
                {
                    row.CreateCell(14).SetCellValue(amount);//發票總金額
                }
                else
                {
                    row.CreateCell(14).SetCellValue(dt.Rows[i]["INVOICE_AMOUNT"].ToString());//發票總金額
                }

                row.CreateCell(15).SetCellValue(dt.Rows[i]["IMPORTER_ID"].ToString());//進口人統一編號
                row.CreateCell(16).SetCellValue(dt.Rows[i]["IMPORTER"].ToString());//進口人英文名稱
                                                                                   //row.CreateCell(17).SetCellValue("");//現場有貨

                if (dt.Rows[i]["MESSAGE"].ToString().Trim() == "B6A" || dt.Rows[i]["MESSAGE"].ToString().Trim() == "N" || dt.Rows[i]["APPOINT"].ToString() == "V")
                {
                    //錯單訊息若為B6A或N，則代入當天日期
                    row.CreateCell(18).SetCellValue(date);//傳輸狀態
                    row.CreateCell(19).SetCellValue(date);//客服提供日期
                    //錯單訊息若為B6A或N，代入進口人統一編號欄位值
                    row.CreateCell(20).SetCellValue(dt.Rows[i]["IMPORTER_ID"].ToString());//正確ID
                    //錯單訊息若為B6A或N，代入進口人英文名稱欄位值
                    row.CreateCell(21).SetCellValue(dt.Rows[i]["IMPORTER"].ToString());//正確姓名

                    row.GetCell(18).CellStyle = date2Style;
                    row.GetCell(19).CellStyle = date2Style;
                }

                //row.CreateCell(22).SetCellValue("");//正確進口人電話
                //row.CreateCell(23).SetCellValue("");//客服狀態
                row.CreateCell(24).SetCellValue(dt.Rows[i]["IM_PHONENO"].ToString());//進口人電話
                row.CreateCell(25).SetCellValue(dt.Rows[i]["IM_ADD"].ToString());//進口人英文地址
                if (dt.Rows[i]["E_CONT_NO"].ToString() != "")
                {
                    //製單資料
                    row.CreateCell(26).SetCellValue(dt.Rows[i]["E_CONT_TYPE"].ToString());//貨櫃種類
                    row.CreateCell(27).SetCellValue(dt.Rows[i]["E_CONT_NO"].ToString());//貨櫃號碼
                    row.CreateCell(28).SetCellValue(dt.Rows[i]["E_SEALNO"].ToString());//封條號碼
                }
                else
                {
                    //原單資料
                    row.CreateCell(26).SetCellValue(dt.Rows[i]["O_CONT_TYPE"].ToString());//貨櫃種類
                    row.CreateCell(27).SetCellValue(dt.Rows[i]["O_CONT_NO"].ToString());//貨櫃號碼
                    row.CreateCell(28).SetCellValue(dt.Rows[i]["O_SEALNO"].ToString());//封條號碼
                }

                row.CreateCell(29).SetCellValue(dt.Rows[i]["TRANS_NAME"].ToString());//派件公司
                row.CreateCell(30).SetCellValue(dt.Rows[i]["JETF_SERIAL"].ToString());//配送單號
                row.CreateCell(31).SetCellValue(dt.Rows[i]["LPNO"].ToString());
            }
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-海快錯單統計
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="sheetName"></param>
        void GetSeaBagNoWorkReportSheet(IWorkbook workbook, DataTable dt, DataTable dt_Count, Dictionary<string, int> returnCount, string sheetName)
        {
            //工作天
            var workDay = _workDayService.GetWorkDay();

            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            DateTime eta;
            DataRow[] dr;
            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("客戶");
            row.CreateCell(1).SetCellValue("船班");
            row.CreateCell(2).SetCellValue("主提單號");
            row.CreateCell(3).SetCellValue("船掛");
            row.CreateCell(4).SetCellValue("清關業者");
            row.CreateCell(5).SetCellValue("到港日");
            row.CreateCell(6).SetCellValue("分艙單傳輸日");
            row.CreateCell(7).SetCellValue("報單傳輸狀態");
            row.CreateCell(8).SetCellValue("參考日期");
            row.CreateCell(9).SetCellValue("最後傳輸日");
            row.CreateCell(10).SetCellValue("短到日期");
            row.CreateCell(11).SetCellValue("是否開立短到單");
            row.CreateCell(12).SetCellValue("短到票數");
            row.CreateCell(13).SetCellValue("溢卸票數");
            row.CreateCell(14).SetCellValue("異常比例");
            row.CreateCell(15).SetCellValue("接收日期");
            row.CreateCell(16).SetCellValue("B6A");
            row.CreateCell(17).SetCellValue("B6B");
            row.CreateCell(18).SetCellValue("B6D");
            row.CreateCell(19).SetCellValue("B6E");
            row.CreateCell(20).SetCellValue("B6F(已收單)");
            row.CreateCell(21).SetCellValue("B6F(未收單)");
            row.CreateCell(22).SetCellValue("A03");
            row.CreateCell(23).SetCellValue("N檔");
            row.CreateCell(24).SetCellValue("身分證錯誤");
            row.CreateCell(25).SetCellValue("其他");
            row.CreateCell(26).SetCellValue("無法傳輸總票數");
            row.CreateCell(27).SetCellValue("傳輸總票數");
            row.CreateCell(28).SetCellValue("錯單比例(實際)");
            row.CreateCell(29).SetCellValue("錯單比例(含B6F收單)");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;

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
            sheet.SetColumnWidth(15, 5000);
            //sheet.SetColumnWidth(16, 5000);
            //sheet.SetColumnWidth(17, 5000);
            //sheet.SetColumnWidth(18, 5000);
            //sheet.SetColumnWidth(19, 4000);
            sheet.SetColumnWidth(20, 7000);
            sheet.SetColumnWidth(21, 5000);
            //sheet.SetColumnWidth(22, 5000);
            //sheet.SetColumnWidth(23, 5000);
            sheet.SetColumnWidth(24, 5000);
            sheet.SetColumnWidth(25, 5000);
            sheet.SetColumnWidth(26, 5000);
            sheet.SetColumnWidth(27, 5000);
            sheet.SetColumnWidth(28, 5000);
            sheet.SetColumnWidth(29, 7000);

            var dt_Group = from t in dt.AsEnumerable()
                               //where t["DeclType"].ToString() == "X2" || t["DeclType"].ToString() == "X3"
                           group t by new
                           {
                               DATADATE = t.Field<string>("DATADATE"),
                               DESPATCH_NAME = t.Field<string>("DESPATCH_NAME"),
                               //VESSEL = t.Field<string>("VESSEL"),
                               MAINNUMBER = t.Field<string>("MAINNUMBER"),
                               MODIFYBY = t.Field<string>("MODIFYBY"),
                               ETA = t.Field<DateTime?>("ETA"),
                           } into g
                           orderby g.Key.MAINNUMBER
                           select new
                           {
                               DATADATE = g.Key.DATADATE,
                               DESPATCH_NAME = g.Key.DESPATCH_NAME,
                               VESSEL = g.Max(t => t.Field<string>("VESSEL")),
                               MAINNUMBER = g.Key.MAINNUMBER,
                               MODIFYBY = g.Key.MODIFYBY,
                               ETA = g.Key.ETA,
                               B6A = g.Count(t => t.Field<string>("MESSAGE") == "B6A"),
                               B6B = g.Count(t => t.Field<string>("MESSAGE") == "B6B"),
                               B6D = g.Count(t => t.Field<string>("MESSAGE") == "B6D"),
                               B6E = g.Count(t => t.Field<string>("MESSAGE") == "B6E"),
                               B6FYES = g.Count(t => t.Field<string>("MESSAGE") == "B6F" && t.Field<string>("APPOINT") == "V"),
                               B6FNO = g.Count(t => t.Field<string>("MESSAGE") == "B6F" && t.Field<string>("APPOINT") == ""),
                               A03 = g.Count(t => t.Field<string>("MESSAGE") == "A03"),
                               N = g.Count(t => t.Field<string>("MESSAGE") == "N"),
                               IDERROR = g.Count(t => t.Field<string>("MESSAGE") == "身分證錯誤請提供正確" || t.Field<string>("MESSAGE") == "在裝貨港前兩碼為CN、HK、MO時，須填列中文姓名"),
                               OTHER = g.Count(t => t.Field<string>("MESSAGE") != "B6A" && t.Field<string>("MESSAGE") != "B6B" && t.Field<string>("MESSAGE") != "B6D" && t.Field<string>("MESSAGE") != "B6E" && t.Field<string>("MESSAGE") != "B6F" && t.Field<string>("MESSAGE") != "A03" && t.Field<string>("MESSAGE") != "N" && t.Field<string>("MESSAGE") != "身分證錯誤請提供正確" && t.Field<string>("MESSAGE") != "在裝貨港前兩碼為CN、HK、MO時，須填列中文姓名"),
                           };

            int irow = 1;
            IFormatProvider ifp = new CultureInfo("zh-TW", true);
            foreach (var item in dt_Group)
            {
                //上傳倉單主號總筆數
                dr = dt_Count.Select($"MAINNUMBER='{item.MAINNUMBER}'");
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(item.DESPATCH_NAME);//客戶
                row.CreateCell(1).SetCellValue(item.VESSEL);//船班
                row.CreateCell(2).SetCellValue(item.MAINNUMBER);//主提單號
                //row.CreateCell(3).SetCellValue("");//船掛
                row.CreateCell(4).SetCellValue(item.MODIFYBY);//清關業者
                if (DateTime.TryParse(item.ETA.ToString(), out eta))
                {
                    row.CreateCell(5).SetCellValue(eta);//到港日
                    row.GetCell(5).CellStyle = date2Style;
                }


                //row.CreateCell(6).SetCellValue("");//分艙單傳輸日
                row.CreateCell(7).SetCellValue("V");//報單傳輸狀態
                row.CreateCell(8).SetCellValue("");// 參考日期

                if (DateTime.TryParse(item.ETA.ToString(), out eta))
                {
                    // 參考日期
                    row.CreateCell(8).SetCellValue(eta.AddDays(-1));
                    row.GetCell(8).CellStyle = date2Style;

                    if (item.MODIFYBY != null && item.MODIFYBY.IndexOf("高雄郵聯") > -1)
                    {
                        row.CreateCell(9).SetCellValue(_workDayService.AddWorkDays(workDay.Item1, workDay.Item2, eta, 3));//最後傳輸日
                        row.GetCell(9).CellStyle = date2Style;
                    }
                    else if (item.MODIFYBY != null && 
                            (
                             item.MODIFYBY.IndexOf("TPCT", StringComparison.OrdinalIgnoreCase) > -1 ||
                             item.MODIFYBY.IndexOf("基隆港務", StringComparison.OrdinalIgnoreCase) > -1
                            ))
                    {
                        row.CreateCell(9).SetCellValue(eta.AddDays(+6));//最後傳輸日
                        row.GetCell(9).CellStyle = date2Style;
                    }
                }

                //row.CreateCell(10).SetCellValue("");//短到日期
                //row.CreateCell(11).SetCellValue("");//是否開立短到單
                //row.CreateCell(12).SetCellValue("");//短到票數
                //row.CreateCell(13).SetCellValue("");//溢卸票數
                //row.CreateCell(14).SetCellValue("");//異常比例
                row.CreateCell(15).SetCellValue(DateTime.ParseExact(item.DATADATE, "yyyyMMdd", ifp));//接收日期
                row.GetCell(15).CellStyle = date2Style;
                row.CreateCell(16).SetCellValue(item.B6A);//B6A
                row.CreateCell(17).SetCellValue(item.B6B);//B6B
                row.CreateCell(18).SetCellValue(item.B6D);//B6D
                row.CreateCell(19).SetCellValue(item.B6E);//B6E

                //此主號需預委票數
                if (returnCount.TryGetValue(item.MAINNUMBER, out int totalCount))
                {
                    row.CreateCell(20).SetCellValue(totalCount - item.B6FNO);
                }

                row.CreateCell(21).SetCellValue(item.B6FNO);//B6F 未收單
                row.CreateCell(22).SetCellValue(item.A03);//A03
                row.CreateCell(23).SetCellValue(item.N);//N檔
                row.CreateCell(24).SetCellValue(item.IDERROR);//身分證錯誤
                row.CreateCell(25).SetCellValue(item.OTHER);//其他
                row.CreateCell(26).CellFormula = $"SUM(R{irow + 1}:T{irow + 1},V{irow + 1}:Z{irow + 1})";//無法傳輸總票數
                if (dr.Length > 0)
                {
                    row.CreateCell(27).SetCellValue(Convert.ToInt32(dr[0]["TOTAL"]));//傳輸總票數
                }
                //錯單比例(實際)
                row.CreateCell(28).CellFormula = $"AA{irow + 1}/AB{irow + 1}";
                row.GetCell(28).CellStyle = cs_Percent;

                //錯單比例(含B6F收單)
                row.CreateCell(29).CellFormula = $"SUM(R{irow + 1}:Z{irow + 1})/AB{irow + 1}";
                row.GetCell(29).CellStyle = cs_Percent;
                irow++;
            }
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook(預委任)
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public IWorkbook GetSeaBagNoWorkAppointWorkbook(string source, string sDate, string eDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            DataRow[] dr;
            //取得海快錯單袋號資料
            DataTable dt = GetSeaBagNoWork(source, sDate, eDate, false).dt;
            //排序
            dt.Columns.Add("ITEM_NO_SORT", typeof(int), "Convert(ITEM_NO,'System.Int32')");
            //申報金額
            dt.Columns.Add("INVOICE_AMOUNT_DOUBLE", typeof(double), "Convert(INVOICE_AMOUNT,'System.Double')");
            //產生EXCEL
            var dt_Group = from t in dt.AsEnumerable()
                           group t by new
                           {
                               MODIFYBY = t.Field<string>("MODIFYBY")
                           } into g
                           select new
                           {
                               MODIFYBY = g.Key.MODIFYBY
                           };

            foreach (var item in dt_Group)
            {
                //取得預委任頁籤
                if (item.MODIFYBY == null)
                {
                    GetSeaBagNoWorkAppointSheet(workbook, dt, "", "無倉儲", false);
                    GetSeaBagNoWorkAppointSheet(workbook, dt, "", "無倉儲(無上傳日期)", true);
                }
                else
                {
                    //GetSeaBagNoWorkAppointSheet(workbook, dt, item.MODIFYBY, $"{item.MODIFYBY}", false);
                    GetSeaBagNoWorkAppointSheet(workbook, dt, item.MODIFYBY, $"{item.MODIFYBY}(無上傳日期)", true);
                }
            }
            return workbook;
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-預委任
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="sheetName"></param>
        void GetSeaBagNoWorkAppointSheet(IWorkbook workbook, DataTable dt, string modifyby, string sheetName, bool noModifyDate)
        {
            DataRow[] dr, dr_Amount;
            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            string phone, mainnumber, blNo, itemNo;
            DateTime eta, date;
            double amount;

            ISheet sheet;
            //無上傳日期
            if (noModifyDate)
            {
                if (modifyby == "")
                {
                    sheet = workbook.CreateSheet(sheetName);
                    //預委任資料
                    dr = dt.Select($"MODIFYBY is null and MESSAGE='B6F' and MODIFY_TIME is null ", "MAINNUMBER,BL_NO,ITEM_NO_SORT");
                }
                else
                {
                    sheet = workbook.CreateSheet(sheetName);
                    //預委任資料
                    dr = dt.Select($"MODIFYBY='{modifyby}' and MESSAGE='B6F' and MODIFY_TIME is null ", "MAINNUMBER,BL_NO,ITEM_NO_SORT");
                }
            }
            else
            {
                if (modifyby == "")
                {
                    sheet = workbook.CreateSheet(sheetName);
                    //預委任資料
                    dr = dt.Select($"MODIFYBY is null and MESSAGE='B6F' ", "MAINNUMBER,BL_NO,ITEM_NO_SORT");
                }
                else
                {
                    sheet = workbook.CreateSheet(sheetName);
                    //預委任資料
                    dr = dt.Select($"MODIFYBY='{modifyby}' and MESSAGE='B6F' ", "MAINNUMBER,BL_NO,ITEM_NO_SORT");
                }
            }


            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("報單號碼");
            row.CreateCell(1).SetCellValue("主提單號碼");
            row.CreateCell(2).SetCellValue("分提單號碼");
            row.CreateCell(3).SetCellValue("進口日期");
            row.CreateCell(4).SetCellValue("統編/身分證字號");
            row.CreateCell(5).SetCellValue("電話");
            row.CreateCell(6).SetCellValue("申報金額");
            row.CreateCell(7).SetCellValue("項次");
            row.CreateCell(8).SetCellValue("貨物名稱");
            row.CreateCell(9).SetCellValue("上傳日期");
            row.CreateCell(10).SetCellValue("狀態");
            row.CreateCell(11).SetCellValue("回覆代碼");
            row.CreateCell(12).SetCellValue("預先委任");

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

            for (int i = 0; i < dr.Length; i++)
            {
                row = sheet.CreateRow(i + 1);
                //電話
                phone = dr[i]["IM_PHONENO"].ToString().ToUpper().Replace("TEL", "");
                //主提單號碼
                mainnumber = dr[i]["MAINNUMBER"].ToString();
                //分提單號碼
                blNo = dr[i]["BL_NO"].ToString();
                //項次
                itemNo = dr[i]["ITEM_NO"].ToString().Trim();

                //row.CreateCell(0).SetCellValue(dt.Rows[i][""].ToString());//報單號碼
                row.CreateCell(1).SetCellValue(mainnumber);//主提單號碼
                row.CreateCell(2).SetCellValue(blNo);//分提單號碼
                if (DateTime.TryParse(dr[i]["ETA"].ToString(), out eta))
                {
                    row.CreateCell(3).SetCellValue(eta.ToString("yyyyMMdd"));//進口日期
                                                                             //row.GetCell(3).CellStyle = date2Style;
                }
                else
                {
                    row.CreateCell(3).SetCellValue(dr[i]["ETA"].ToString());//進口日期
                }
                row.CreateCell(4).SetCellValue(dr[i]["IMPORTER_ID"].ToString());//統編/身分證字號
                row.CreateCell(5).SetCellValue(phone);//電話
                                                      //申報金額放入項次1
                if (itemNo == "1")
                {
                    dr_Amount = dt.Select($"MODIFYBY='{modifyby}' and MESSAGE='B6F' and MAINNUMBER='{mainnumber}' and BL_NO='{blNo}' ");
                    amount = dr_Amount.Sum(x => x.Field<double>("INVOICE_AMOUNT_DOUBLE"));
                    row.CreateCell(6).SetCellValue(amount);//申報金額
                }
                else
                {
                    row.CreateCell(6).SetCellValue(0);//申報金額
                }

                if (int.TryParse(itemNo, out var num))
                {
                    row.CreateCell(7).SetCellValue(num);//項次
                }

                row.CreateCell(8).SetCellValue(dr[i]["ITEM_NAME"].ToString());//貨物名稱
                if (DateTime.TryParse(dr[i]["MODIFY_TIME"].ToString(), out date))
                {
                    row.CreateCell(9).SetCellValue(date);//上傳日期
                    row.GetCell(9).CellStyle = dateStyle;
                }
                else
                {
                    row.CreateCell(9).SetCellValue(dr[i]["MODIFY_TIME"].ToString());//上傳日期
                }
                row.CreateCell(10).SetCellValue(dr[i]["STATUS"].ToString());//狀態
                row.CreateCell(11).SetCellValue(dr[i]["REPLY_CODE"].ToString());//回覆代碼
                row.CreateCell(12).SetCellValue(dr[i]["APPOINT"].ToString());//預先委任
            }
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook(具結)
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public IWorkbook GetSeaBagNoWorkBindOverWorkbook(string source, string sDate, string eDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            DataTable dt_Order;
            //取得海快錯單作業資料
            DataTable dt = GetSeaBagNoWork(source, sDate, eDate, false).dt;
            //排序
            dt.Columns.Add("ITEM_NO_SORT", typeof(int), "Convert(ITEM_NO,'System.Int32')");
            //申報金額
            //dt.Columns.Add("INVOICE_AMOUNT_DOUBLE", typeof(double), "Convert(INVOICE_AMOUNT,'System.Double')");
            //產生EXCEL
            var dt_Group = from t in dt.AsEnumerable()
                           group t by new
                           {
                               DESPATCH_NAME = t.Field<string>("DESPATCH_NAME"),
                               MAINNUMBER = t.Field<string>("MAINNUMBER"),
                           } into g
                           select new
                           {
                               DESPATCH_NAME = g.Key.DESPATCH_NAME,
                               MAINNUMBER = g.Key.MAINNUMBER
                           };

            //取得具結總表頁籤
            GetSeaBagNoWorkBindOverReportSheet(workbook, dt);
            foreach (var item in dt_Group)
            {
                if (item.DESPATCH_NAME != null && item.DESPATCH_NAME.ToString().IndexOf("菜鳥") > -1)
                {
                    //取得具結明細頁籤
                    GetSeaBagNoWorkBindOverDetailsSheet(workbook, dt, item.DESPATCH_NAME, item.MAINNUMBER);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-具結主號總表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="despatch_name"></param>
        /// <param name="mainnumber"></param>
        void GetSeaBagNoWorkBindOverReportSheet(IWorkbook workbook, DataTable dt)
        {
            DataRow[] dr;

            dr = dt.Select($"(MODIFYBY='台北貨櫃' or MODIFYBY like '%TPCT%' or MODIFYBY like '%基隆港務%') and (MESSAGE='B6D' or MESSAGE='B6E') and DESPATCH_NAME like '%菜鳥%' ", "MAINNUMBER,BL_NO,ITEM_NO_SORT");

            if (dr.Length > 0)
            {
                //取得EXCEL格式
                GetWorkbookStyle(workbook);
                string blNo, itemNo;
                DateTime eta;
                double gw, price, amount, nw;
                int piece, qty;

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

                for (int i = 0; i < dr.Length; i++)
                {
                    row = sheet.CreateRow(i + 4);
                    //分提單號碼
                    blNo = dr[i]["BL_NO"].ToString();
                    //項次
                    itemNo = dr[i]["ITEM_NO"].ToString().Trim();

                    if (itemNo == "1")
                    {
                        row.CreateCell(0).SetCellValue(dr[i]["MAINNUMBER"].ToString());//主提單號碼
                        row.CreateCell(1).SetCellValue(blNo);//分提單號碼
                        row.CreateCell(2).SetCellValue(dr[i]["MANIFEST"].ToString());//艙單號碼
                        row.CreateCell(3).SetCellValue(dr[i]["JETF_ID"].ToString());//快遞業者統一編號
                        row.CreateCell(4).SetCellValue(dr[i]["TERMSOFPRICE"].ToString());//單價條件
                        row.CreateCell(5).SetCellValue(dr[i]["CURRENCY"].ToString());//單價幣別代碼
                    }
                    if (double.TryParse(dr[i]["GW"].ToString(), out gw))
                    {
                        row.CreateCell(6).SetCellValue(gw);//毛重
                    }
                    else
                    {
                        row.CreateCell(6).SetCellValue(dr[i]["GW"].ToString());//毛重
                    }

                    if (int.TryParse(dr[i]["PIECE"].ToString(), out piece))
                    {
                        row.CreateCell(7).SetCellValue(piece);//件數
                    }
                    else
                    {
                        row.CreateCell(7).SetCellValue(dr[i]["PIECE"].ToString());//件數
                    }

                    row.CreateCell(8).SetCellValue(dr[i]["PIECE_UNIT"].ToString());//件數單位
                    row.CreateCell(9).SetCellValue(dr[i]["MARKS"].ToString());//標記
                    row.CreateCell(10).SetCellValue(itemNo);//貨物編號
                    row.CreateCell(11).SetCellValue(dr[i]["ITEM_NAME"].ToString());// 貨物名稱
                    row.CreateCell(12).SetCellValue(dr[i]["CCC_CODE"].ToString());//貨品分類號列
                    row.CreateCell(13).SetCellValue(dr[i]["TRADEMARK"].ToString());//商標(牌名)
                    row.CreateCell(14).SetCellValue(dr[i]["II_SPEC"].ToString());//成分及規格

                    if (double.TryParse(dr[i]["NW"].ToString(), out nw))
                    {
                        row.CreateCell(15).SetCellValue(nw);//淨重
                    }
                    else
                    {
                        row.CreateCell(15).SetCellValue(dr[i]["NW"].ToString());//淨重
                    }

                    if (int.TryParse(dr[i]["QUANTITY"].ToString(), out qty))
                    {
                        row.CreateCell(16).SetCellValue(qty);//數量
                    }
                    else
                    {
                        row.CreateCell(16).SetCellValue(dr[i]["QUANTITY"].ToString());//數量
                    }
                    row.CreateCell(17).SetCellValue(dr[i]["QUANTITY_UNIT"].ToString());//數量單位
                    if (double.TryParse(dr[i]["UNIT_PRICE"].ToString(), out price))
                    {
                        row.CreateCell(18).SetCellValue(price);//單價金額
                    }
                    else
                    {
                        row.CreateCell(18).SetCellValue(dr[i]["UNIT_PRICE"].ToString());//單價金額
                    }

                    if (double.TryParse(dr[i]["INVOICE_AMOUNT"].ToString(), out amount))
                    {
                        row.CreateCell(19).SetCellValue(amount);//發票總金額
                    }
                    else
                    {
                        row.CreateCell(19).SetCellValue(dr[i]["INVOICE_AMOUNT"].ToString());//發票總金額
                    }
                    //row.CreateCell(20).SetCellValue("");//完稅價格
                    row.CreateCell(21).SetCellValue(dr[i]["MEASUREMENT"].ToString());//體積
                    row.CreateCell(22).SetCellValue(dr[i]["CBM"].ToString());//體積單位
                    row.CreateCell(23).SetCellValue(dr[i]["MADEIN"].ToString());//生產國別

                    if (itemNo == "1")
                    {
                        row.CreateCell(24).SetCellValue(dr[i]["EXPORTER"].ToString());// 出口人英文名稱
                        row.CreateCell(25).SetCellValue(dr[i]["EX_COUNRTYCODE"].ToString());// 出口人國家代碼
                        row.CreateCell(26).SetCellValue(dr[i]["EX_ADD"].ToString());//出口人英文地址
                        row.CreateCell(27).SetCellValue(dr[i]["PARTY_IDENTIFIER"].ToString());//進口人身分識別碼
                        row.CreateCell(28).SetCellValue(dr[i]["IMPORTER_ID"].ToString());//進口人統一編號
                        row.CreateCell(29).SetCellValue(dr[i]["IMPORTER"].ToString());//進口人英文名稱
                        row.CreateCell(30).SetCellValue(dr[i]["IM_PHONENO"].ToString());//進口人電話
                        row.CreateCell(31).SetCellValue(dr[i]["IM_ADD"].ToString());//進口人英文地址
                        row.CreateCell(36).SetCellValue("POA=Y"); //其他申報事項1
                        row.CreateCell(37).SetCellValue(dr[i]["DECLARATION_2"].ToString());//其他申報事項2
                        row.CreateCell(38).SetCellValue(dr[i]["TAXFEE_DECLARED"].ToString());//主動申報繳納稅款註記
                        row.CreateCell(39).SetCellValue(dr[i]["TRANS_NAME"].ToString());//派件公司
                        row.CreateCell(40).SetCellValue(dr[i]["JETF_SERIAL"].ToString());//配送單號
                        //row.CreateCell(41).SetCellValue("CC款");
                        //row.CreateCell(42).SetCellValue("後段報關\n/一般倉");
                        //row.CreateCell(43).SetCellValue("發票金額");
                        //row.CreateCell(44).SetCellValue("備註");
                        row.CreateCell(45).SetCellValue(dr[i]["SIZE"].ToString());//尺寸（單位：CM）
                        row.CreateCell(46).SetCellValue(dr[i]["CONSOL_CODE"].ToString());//電商或集運商編號
                        row.CreateCell(47).SetCellValue(dr[i]["CONSOL_TYPE"].ToString());//貨物識別代碼
                        row.CreateCell(48).SetCellValue(dr[i]["CONSOL_NAME"].ToString());//電商或集運商名稱
                        row.CreateCell(49).SetCellValue(dr[i]["CONSOL_URL"].ToString());//電商或集運商網址
                    }

                    if (dr[i]["E_CONT_NO"].ToString() != "")
                    {
                        //製單資料
                        row.CreateCell(32).SetCellValue(dr[i]["E_CONT_TYPE"].ToString());//貨櫃種類
                        row.CreateCell(33).SetCellValue(dr[i]["E_CONT_NO"].ToString());//貨櫃號碼
                        //row.CreateCell(34).SetCellValue(dr[i]["E_CONT_TRANSMODEL"].ToString());//貨櫃裝運方式
                        row.CreateCell(34).SetCellValue("2");//貨櫃裝運方式
                        row.CreateCell(35).SetCellValue(dr[i]["E_SEALNO"].ToString());//封條號碼
                    }
                    else
                    {
                        //原單資料
                        row.CreateCell(32).SetCellValue(dr[i]["O_CONT_TYPE"].ToString());//貨櫃種類
                        row.CreateCell(33).SetCellValue(dr[i]["O_CONT_NO"].ToString());//貨櫃號碼
                        //row.CreateCell(34).SetCellValue(dr[i]["O_CONT_TRANSMODEL"].ToString());//貨櫃裝運方式
                        row.CreateCell(34).SetCellValue("2");//貨櫃裝運方式
                        row.CreateCell(35).SetCellValue(dr[i]["O_SEALNO"].ToString());//封條號碼
                    }
                }
            }
        }

        /// <summary>
        /// 海快錯單作業-Excel-Workbook-頁籤-具結主號明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt"></param>
        /// <param name="despatch_name"></param>
        /// <param name="mainnumber"></param>
        void GetSeaBagNoWorkBindOverDetailsSheet(IWorkbook workbook, DataTable dt, string despatch_name, string mainnumber)
        {
            DataRow[] dr;

            dr = dt.Select($"(MODIFYBY='台北貨櫃' or MODIFYBY like '%TPCT%' or MODIFYBY like '%基隆港務%' ) and (MESSAGE='B6D' or MESSAGE='B6E') and DESPATCH_NAME='{despatch_name}' and MAINNUMBER='{mainnumber}' ", "MAINNUMBER,BL_NO,ITEM_NO_SORT");

            if (dr.Length > 0)
            {
                //取得EXCEL格式
                GetWorkbookStyle(workbook);
                string blNo, itemNo;
                DateTime eta;
                double gw, price, amount, nw;
                int piece, qty;
                //取得具結主號訂單明細
                DataTable dt_Order = GetCesMainOrder(mainnumber).dt;

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

                for (int i = 0; i < dr.Length; i++)
                {
                    row = sheet.CreateRow(i + 4);
                    //分提單號碼
                    blNo = dr[i]["BL_NO"].ToString();
                    //項次
                    itemNo = dr[i]["ITEM_NO"].ToString().Trim();

                    if (itemNo == "1")
                    {
                        row.CreateCell(0).SetCellValue(blNo);//分提單號碼
                        row.CreateCell(1).SetCellValue(dr[i]["MANIFEST"].ToString());//艙單號碼
                        row.CreateCell(2).SetCellValue(dr[i]["JETF_ID"].ToString());//快遞業者統一編號
                        row.CreateCell(3).SetCellValue(dr[i]["TERMSOFPRICE"].ToString());//單價條件
                        row.CreateCell(4).SetCellValue(dr[i]["CURRENCY"].ToString());//單價幣別代碼
                    }
                    if (double.TryParse(dr[i]["GW"].ToString(), out gw))
                    {
                        row.CreateCell(5).SetCellValue(gw);//毛重
                    }
                    else
                    {
                        row.CreateCell(5).SetCellValue(dr[i]["GW"].ToString());//毛重
                    }

                    if (int.TryParse(dr[i]["PIECE"].ToString(), out piece))
                    {
                        row.CreateCell(6).SetCellValue(piece);//件數
                    }
                    else
                    {
                        row.CreateCell(6).SetCellValue(dr[i]["PIECE"].ToString());//件數
                    }

                    row.CreateCell(7).SetCellValue(dr[i]["PIECE_UNIT"].ToString());//件數單位
                    row.CreateCell(8).SetCellValue(dr[i]["MARKS"].ToString());//標記
                    row.CreateCell(9).SetCellValue(itemNo);//貨物編號
                    row.CreateCell(10).SetCellValue(dr[i]["ITEM_NAME"].ToString());// 貨物名稱
                    row.CreateCell(11).SetCellValue(dr[i]["CCC_CODE"].ToString());//貨品分類號列
                    row.CreateCell(12).SetCellValue(dr[i]["TRADEMARK"].ToString());//商標(牌名)
                    row.CreateCell(13).SetCellValue(dr[i]["II_SPEC"].ToString());//成分及規格

                    if (double.TryParse(dr[i]["NW"].ToString(), out nw))
                    {
                        row.CreateCell(14).SetCellValue(nw);//淨重
                    }
                    else
                    {
                        row.CreateCell(14).SetCellValue(dr[i]["NW"].ToString());//淨重
                    }

                    if (int.TryParse(dr[i]["QUANTITY"].ToString(), out qty))
                    {
                        row.CreateCell(15).SetCellValue(qty);//數量
                    }
                    else
                    {
                        row.CreateCell(15).SetCellValue(dr[i]["QUANTITY"].ToString());//數量
                    }
                    row.CreateCell(16).SetCellValue(dr[i]["QUANTITY_UNIT"].ToString());//數量單位
                    if (double.TryParse(dr[i]["UNIT_PRICE"].ToString(), out price))
                    {
                        row.CreateCell(17).SetCellValue(price);//單價金額
                    }
                    else
                    {
                        row.CreateCell(17).SetCellValue(dr[i]["UNIT_PRICE"].ToString());//單價金額
                    }

                    if (double.TryParse(dr[i]["INVOICE_AMOUNT"].ToString(), out amount))
                    {
                        row.CreateCell(18).SetCellValue(amount);//發票總金額
                    }
                    else
                    {
                        row.CreateCell(18).SetCellValue(dr[i]["INVOICE_AMOUNT"].ToString());//發票總金額
                    }
                    //row.CreateCell(19).SetCellValue("");//完稅價格
                    row.CreateCell(20).SetCellValue(dr[i]["MEASUREMENT"].ToString());//體積
                    row.CreateCell(21).SetCellValue(dr[i]["CBM"].ToString());//體積單位
                    row.CreateCell(22).SetCellValue(dr[i]["MADEIN"].ToString());//生產國別

                    if (itemNo == "1")
                    {
                        row.CreateCell(23).SetCellValue(dr[i]["EXPORTER"].ToString());// 出口人英文名稱
                        row.CreateCell(24).SetCellValue(dr[i]["EX_COUNRTYCODE"].ToString());// 出口人國家代碼
                        row.CreateCell(25).SetCellValue(dr[i]["EX_ADD"].ToString());//出口人英文地址
                        row.CreateCell(26).SetCellValue(dr[i]["PARTY_IDENTIFIER"].ToString());//進口人身分識別碼
                        row.CreateCell(27).SetCellValue(dr[i]["IMPORTER_ID"].ToString());//進口人統一編號
                        row.CreateCell(28).SetCellValue(dr[i]["IMPORTER"].ToString());//進口人英文名稱
                        row.CreateCell(29).SetCellValue(dr[i]["IM_PHONENO"].ToString());//進口人電話
                        row.CreateCell(30).SetCellValue(dr[i]["IM_ADD"].ToString());//進口人英文地址
                        row.CreateCell(35).SetCellValue("POA=Y"); //其他申報事項1
                        row.CreateCell(36).SetCellValue(dr[i]["DECLARATION_2"].ToString());//其他申報事項2
                        row.CreateCell(37).SetCellValue(dr[i]["TAXFEE_DECLARED"].ToString());//主動申報繳納稅款註記
                        row.CreateCell(38).SetCellValue(dr[i]["TRANS_NAME"].ToString());//派件公司
                        row.CreateCell(39).SetCellValue(dr[i]["JETF_SERIAL"].ToString());//配送單號
                        //row.CreateCell(40).SetCellValue("CC款");
                        //row.CreateCell(41).SetCellValue("後段報關\n/一般倉");
                        //row.CreateCell(42).SetCellValue("發票金額");
                        //row.CreateCell(43).SetCellValue("備註");
                        row.CreateCell(44).SetCellValue(dr[i]["SIZE"].ToString());//尺寸（單位：CM）
                        row.CreateCell(45).SetCellValue(dr[i]["CONSOL_CODE"].ToString());//電商或集運商編號
                        row.CreateCell(46).SetCellValue(dr[i]["CONSOL_TYPE"].ToString());//貨物識別代碼
                        row.CreateCell(47).SetCellValue(dr[i]["CONSOL_NAME"].ToString());//電商或集運商名稱
                        row.CreateCell(48).SetCellValue(dr[i]["CONSOL_URL"].ToString());//電商或集運商網址
                    }

                    if (dr[i]["E_CONT_NO"].ToString() != "")
                    {
                        //製單資料
                        row.CreateCell(31).SetCellValue(dr[i]["E_CONT_TYPE"].ToString());//貨櫃種類
                        row.CreateCell(32).SetCellValue(dr[i]["E_CONT_NO"].ToString());//貨櫃號碼
                        //row.CreateCell(33).SetCellValue(dr[i]["E_CONT_TRANSMODEL"].ToString());//貨櫃裝運方式
                        row.CreateCell(33).SetCellValue("2");//貨櫃裝運方式
                        row.CreateCell(34).SetCellValue(dr[i]["E_SEALNO"].ToString());//封條號碼
                    }
                    else
                    {
                        //原單資料
                        row.CreateCell(31).SetCellValue(dr[i]["O_CONT_TYPE"].ToString());//貨櫃種類
                        row.CreateCell(32).SetCellValue(dr[i]["O_CONT_NO"].ToString());//貨櫃號碼
                        //row.CreateCell(33).SetCellValue(dr[i]["O_CONT_TRANSMODEL"].ToString());//貨櫃裝運方式
                        row.CreateCell(33).SetCellValue("2");//貨櫃裝運方式
                        row.CreateCell(34).SetCellValue(dr[i]["O_SEALNO"].ToString());//封條號碼
                    }
                }
            }
        }



    }
}
