using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Renci.SshNet.Messages;
using Spire.Xls;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;

namespace Service.Services.Job.IncomeJob
{
    public class IncomeJobService : _BaseService
    {
        private readonly TelegramBot _telegramBot;

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Letf, cs_Center_Thick, cs_Center_Blue, cs_Center_Blue_Thick, cs_Int, cs_Int_Thick, cs_Int_Blue, cs_Int_Blue_Thick, cs_Double, cs_Percent, cs_Percent2;
        
        public IncomeJobService(TelegramBot telegramBot)
        {
            _telegramBot = telegramBot;
        }

        /// <summary>
        /// 營收發送訊息
        /// </summary>
        public async Task RunIncomeJobAsync()
        {
            try
            {
                DateTime date = DateTime.Now;
                string sDate = date.AddDays(-1).ToString("yyyyMM") + "01";
                string eDate = date.AddDays(-1).ToString("yyyyMMdd");
                string sendDate = date.ToString("yyyyMMdd");
                string sendTime = date.ToString("HHmm");

                //營收總表
                await SendLineIncomeReport("營收總表", sendDate, sendTime, sDate, eDate);

                //營收總表2
                //await SendLineIncomeReport2("營收總表2", sendDate, sendTime, sDate, eDate);

                //海運-營收總表及明細表
                await SendLineIncomeDetailsSeaReportAsync("海運-營收總表及明細表", sendDate, sendTime, sendDate, sendDate);

                //空運-營收總表及明細表0800-2000
                await SendLineIncomeDetailsEtlReportAsync("空運-營收總表及明細表0800-2000", sendDate, sendTime, sendDate, sendDate);

                //空運-營收總表及明細表2000-0800
                await SendLineIncomeDetailsEtlReport2Async("空運-營收總表及明細表2000-0800", sendDate, sendTime, date.AddDays(-1).ToString("yyyyMMdd"), sendDate);

                //稅金統計表
                await SendLineTaxReportAsync("海空快稅金統計表", sendDate, sendTime, sDate, eDate);

                //營收去年比
                await SendLineIncomeRateReportAsync("營收去年比", sendDate, sendTime, sDate, eDate);
            }
            catch (Exception ex)
            {
                WriteJobErrorLog("營收報表", ex);
            }
        }

        /// <summary>
        /// LINE發送 海運-營收總表及明細表
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        async Task SendLineIncomeDetailsSeaReportAsync(string sendName, string sendDate, string sendTime, string sDate, string eDate)
        {
            try
            {
                DataTable dt = checkSend(sendName, sendDate, sendTime);
                if (dt.Rows.Count > 0)
                {
                    bool success = false;
                    string id, token;
                    string fileName = $"{sDate}~{eDate}-海運-營收總表及明細表";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{fileName}.xlsx";
                    string jpg = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}海運-營收總表及明細表.jpg";
                    //取得總表及明細表Excel
                    GetIncomeDetailsSeaReportExcel(sDate, eDate, filePath);
                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(filePath);
                    Worksheet sheet = workbook.Worksheets[0];
                    sheet.SaveToImage(jpg);
                    sheet.Dispose();

                    id = dt.Rows[0]["Id"].ToString();
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                    for (int i = 0; i < groupId.Length; i++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[i]);
                        var result = await _telegramBot.SendPhotoAsync(chatId, $"{eDate}海快通關狀態彙總表", jpg);

                        if (result.Ok)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        //更新LINE發送日期
                        UpdateTelegramSendMessage(id, sendDate);
                    }
                }
            }
            catch (Exception ex)
            {
                await _telegramBot.SendTextMessageAsync(_telegramBot.GetChatId("TEST"), $"{sendName}：{ex.Message}");
                //logger.Error($"{sendName}：{ex.Message}");
            }
        }

        /// <summary>
        /// LINE發送 空運-營收總表及明細表(0800-2000)
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        async Task SendLineIncomeDetailsEtlReportAsync(string sendName, string sendDate, string sendTime, string sDate, string eDate)
        {
            try
            {
                DataTable dt = checkSend(sendName, sendDate, sendTime);
                if (dt.Rows.Count > 0)
                {
                    bool success = false;
                    string id, token;
                    string fileName = $"{sDate}~{eDate}-空運-營收總表及明細表";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{fileName}.xlsx";
                    string jpg = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}空運-營收總表及明細表.jpg";
                    DataTable dt_Report = IncomeDetailsReport("ETL2", sDate + "080000", eDate + "200000");
                    //取得總表及明細表Excel
                    GetIncomeDetailsEtlReportExcel(dt_Report, sDate, eDate, filePath);
                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(filePath);
                    Worksheet sheet = workbook.Worksheets[0];
                    sheet.SaveToImage(jpg);
                    sheet.Dispose();

                    id = dt.Rows[0]["Id"].ToString();
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                    for (int i = 0; i < groupId.Length; i++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[i]);
                        var result = await _telegramBot.SendPhotoAsync(chatId, "\n" + $"{eDate}空快通關狀態彙總表(08:00-20:00)", jpg);
                        if (result.Ok)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        //更新LINE發送日期
                        UpdateTelegramSendMessage(id, sendDate);
                    }
                }
            }
            catch (Exception ex)
            {
                await _telegramBot.SendTextMessageAsync(_telegramBot.GetChatId("TEST"), $"{sendName}：{ex.Message}");
                //logger.Error($"{sendName}：{ex.Message}");
            }
        }

        /// <summary>
        /// LINE發送 空運-營收總表及明細表(2000-0800)
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        async Task SendLineIncomeDetailsEtlReport2Async(string sendName, string sendDate, string sendTime, string sDate, string eDate)
        {
            try
            {
                DataTable dt = checkSend(sendName, sendDate, sendTime);
                if (dt.Rows.Count > 0)
                {
                    bool success = false;
                    string id, token;
                    string fileName = $"{sDate}~{eDate}-空運-營收總表及明細表";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{fileName}.xlsx";
                    string jpg = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}空運-營收總表及明細表.jpg";
                    DataTable dt_Report = IncomeDetailsReport("ETL2", sDate + "200000", eDate + "080000");
                    //取得總表及明細表Excel
                    GetIncomeDetailsEtlReportExcel(dt_Report, sDate, eDate, filePath);
                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(filePath);
                    Worksheet sheet = workbook.Worksheets[0];
                    sheet.SaveToImage(jpg);
                    sheet.Dispose();

                    id = dt.Rows[0]["Id"].ToString();
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                    for (int i = 0; i < groupId.Length; i++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[i]);
                        var result = await _telegramBot.SendPhotoAsync(chatId, "\n" + $"{sDate}空快通關狀態彙總表(20:00-08:00)", jpg);
                        if (result.Ok)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        //更新LINE發送日期
                        UpdateTelegramSendMessage(id, sendDate);
                    }
                }
            }
            catch (Exception ex)
            {
                await _telegramBot.SendTextMessageAsync(_telegramBot.GetChatId("TEST"), $"{sendName}：{ex.Message}");
                //logger.Error($"{sendName}：{ex.Message}");
            }
        }

        /// <summary>
        /// LINE發送 營收報表
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        async Task SendLineIncomeReport(string sendName, string sendDate, string sendTime, string sDate, string eDate)
        {
            try
            {
                DataTable dt = checkSend(sendName, sendDate, sendTime);
                if (dt.Rows.Count > 0)
                {
                    bool success = false;
                    string id, token;
                    string fileName = $"{sDate}~{eDate}-營收報表";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{fileName}.xlsx";
                    string message_Sea = $"{eDate}海快營收日統計表(未稅)";
                    string message_Etl = $"{eDate}空快營收日統計表(未稅)";
                    //取得營收總表Excel
                    GetIncomeReportExcel(sDate, eDate, filePath);
                    string jpg = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}海空快日倉儲營收報表(未稅).jpg";
                    string jpg2 = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}海空快日倉儲營收報表(未稅)2.jpg";
                    string jpg_Sea = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{message_Sea}.jpg";
                    string jpg_Etl = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{message_Etl}.jpg";

                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(filePath);
                    Worksheet sheet = workbook.Worksheets[0];
                    sheet.SaveToImage(jpg);
                    sheet.Dispose();
                    Worksheet sheet2 = workbook.Worksheets[1];
                    sheet2.SaveToImage(jpg_Sea);
                    sheet2.Dispose();
                    Worksheet sheet3 = workbook.Worksheets[2];
                    sheet3.SaveToImage(jpg_Etl);
                    sheet3.Dispose();
                    workbook.Dispose();

                    id = dt.Rows[0]["Id"].ToString();
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                    for (int i = 0; i < groupId.Length; i++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[i]);
                        var result = await _telegramBot.SendDocumentAsync(chatId, $"{eDate}海空快日倉儲營收報表(未稅)", jpg);
                        var result_Sea = await _telegramBot.SendDocumentAsync(chatId, message_Sea, jpg_Sea);
                        var result_Etl = await _telegramBot.SendDocumentAsync(chatId, message_Etl, jpg_Etl);
                        if (result.Ok)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        //更新LINE發送日期
                        UpdateTelegramSendMessage(id, sendDate);
                    }
                }
            }
            catch (Exception ex)
            {
                await _telegramBot.SendTextMessageAsync(_telegramBot.GetChatId("TEST"), $"{sendName}：{ex.Message}");
                //logger.Error($"{sendName}：{ex.Message}");
            }
        }


        /// <summary>
        /// LINE發送 營收報表2
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        async Task SendLineIncomeReport2(string sendName, string sendDate, string sendTime, string sDate, string eDate)
        {
            try
            {
                DataTable dt = checkSend(sendName, sendDate, sendTime);
                if (dt.Rows.Count > 0)
                {
                    bool success = false;
                    string id, token;
                    string fileName = $"{sDate}~{eDate}-營收報表";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{fileName}.xlsx";
                    string jpg = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}海空快日倉儲營收報表(未稅)2.jpg";
                    if (File.Exists(filePath))
                    {
                        Workbook workbook = new Workbook();
                        workbook.LoadFromFile(filePath);
                        Worksheet sheet4 = workbook.Worksheets[0];
                        sheet4.HideColumn(3);
                        sheet4.HideColumn(4);
                        sheet4.HideColumn(5);
                        sheet4.HideColumn(6);
                        sheet4.HideColumn(7);
                        sheet4.HideColumn(12);
                        sheet4.HideColumn(13);
                        sheet4.HideColumn(14);
                        sheet4.HideColumn(15);
                        sheet4.HideColumn(16);
                        sheet4.SaveToImage(jpg);
                        sheet4.Dispose();

                        id = dt.Rows[0]["Id"].ToString();
                        //取得發送群組token
                        string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                        for (int i = 0; i < groupId.Length; i++)
                        {
                            var chatId = _telegramBot.GetChatId(groupId[i]);
                            var result = await _telegramBot.SendPhotoAsync(chatId, "\n" + $"{eDate}海空快日倉儲營收報表(未稅)", jpg);

                            if (result.Ok)
                            {
                                success = true;
                            }
                        }
                        if (success)
                        {
                            //更新LINE發送日期
                            UpdateTelegramSendMessage(id, sendDate);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _telegramBot.SendTextMessageAsync(_telegramBot.GetChatId("TEST"), $"{sendName}：{ex.Message}");
                //logger.Error($"{sendName}：{ex.Message}");
            }
        }

        /// <summary>
        /// LINE發送 營收報表-去年比
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        async Task SendLineIncomeRateReportAsync(string sendName, string sendDate, string sendTime, string sDate, string eDate)
        {
            try
            {
                DataTable dt = checkSend(sendName, sendDate, sendTime);
                if (dt.Rows.Count > 0)
                {
                    bool success = false;
                    string id, token;
                    string fileName = $"{sDate}~{eDate}-日統計營收去年比報表";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{fileName}.xlsx";
                    string message_Sea = $"{eDate}海快日統計營收去年比報表(未稅)";
                    string message_Etl = $"{eDate}空快日統計營收去年比報表(未稅)";

                    //取得營收去年比Excel
                    IncomeRateReport.GetIncomeRateReportExcel(sDate, eDate, filePath);

                    string jpg_Sea = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{message_Sea}.jpg";
                    string jpg_Etl = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{message_Etl}.jpg";

                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(filePath);
                    Worksheet sheet = workbook.Worksheets[0];
                    sheet.SaveToImage(jpg_Etl);
                    sheet.Dispose();
                    Worksheet sheet2 = workbook.Worksheets[1];
                    sheet2.SaveToImage(jpg_Sea);
                    sheet2.Dispose();

                    id = dt.Rows[0]["Id"].ToString();
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                    for (int i = 0; i < groupId.Length; i++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[i]);
                        var result_Sea = await _telegramBot.SendDocumentAsync(chatId, message_Sea, jpg_Sea);
                        var result_Etl = await _telegramBot.SendDocumentAsync(chatId, message_Etl, jpg_Etl);
                        if (result_Etl.Ok && result_Sea.Ok)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        //更新LINE發送日期
                        UpdateTelegramSendMessage(id, sendDate);
                    }
                }
            }
            catch (Exception ex)
            {
                await _telegramBot.SendTextMessageAsync(_telegramBot.GetChatId("TEST"), $"{sendName}：{ex.Message}");
                //logger.Error($"{sendName}：{ex.Message}");
            }
        }

        /// <summary>
        /// 稅金統計表
        /// </summary>
        /// <param name="sendName"></param>
        /// <param name="sendDate"></param>
        /// <param name="sendTime"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        async Task SendLineTaxReportAsync(string sendName, string sendDate, string sendTime, string sDate, string eDate)
        {
            try
            {
                DataTable dt = checkSend(sendName, sendDate, sendTime);
                if (dt.Rows.Count > 0)
                {
                    bool success = false;
                    string id, token;
                    string fileName = $"{eDate}-海空快稅金統計表";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{fileName}.xlsx";
                    //取得稅金統計表Excel
                    GetTaxReportExcel(sDate, eDate, filePath);

                    string jpgEtl = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}空快稅金統計表.jpg";
                    string jpgSea = AppDomain.CurrentDomain.BaseDirectory + $"Excel\\{eDate}海快稅金統計表.jpg";

                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(filePath);
                    Worksheet sheetEtl = workbook.Worksheets[0];
                    sheetEtl.SaveToImage(jpgEtl);
                    sheetEtl.Dispose();
                    Worksheet sheetSea = workbook.Worksheets[1];
                    sheetSea.SaveToImage(jpgSea);
                    sheetSea.Dispose();

                    id = dt.Rows[0]["Id"].ToString();
                    //取得發送群組token
                    string[] groupId = dt.Rows[0]["GroupId"].ToString().Split(',');
                    for (int i = 0; i < groupId.Length; i++)
                    {
                        var chatId = _telegramBot.GetChatId(groupId[i]);
                        var result_Sea = await _telegramBot.SendPhotoAsync(chatId, $"{eDate}空快稅金統計表", jpgEtl);
                        var result_Etl = await _telegramBot.SendPhotoAsync(chatId, $"{eDate}海快稅金統計表", jpgSea);
                        if (result_Sea.Ok && result_Etl.Ok)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        //更新LINE發送日期
                        UpdateTelegramSendMessage(id, sendDate);
                    }
                }
            }
            catch (Exception ex)
            {
                await _telegramBot.SendTextMessageAsync(_telegramBot.GetChatId("TEST"), $"{sendName}：{ex.Message}");
                //logger.Error($"{sendName}：{ex.Message}");
            }
        }


        /// <summary>
        /// 取得EXCEL
        /// </summary>
        /// <param name="filePath"></param>
        void GetIncomeReportExcel(string sDate, string eDate, string filePath)
        {
            //轉入資料
            //InsertIncomeReport(sDate, eDate);

            IWorkbook workbook = new XSSFWorkbook();
            //日倉儲營收
            GetIncomeReportDaySheet(workbook, sDate, eDate);

            DataTable dt = IncomeReport_Day2(sDate, eDate);
            DataRow[] dr_Sea = dt.Select("TRAN_TYPE='進口海快'");
            DataRow[] dr_Etl = dt.Select("TRAN_TYPE='進口空快'");
            //營收日統計表-海快
            GetIncomeReportDay2Sheet(dr_Sea, workbook, sDate, eDate, eDate + "海快營收日統計表(未稅)");
            //營收日統計表-空快
            GetIncomeReportDay2Sheet(dr_Etl, workbook, sDate, eDate, eDate + "空快營收日統計表(未稅)");
            FileStream file = new FileStream(filePath, FileMode.Create);
            workbook.Write(file);
            file.Close();
        }

        /// <summary>
        /// 日倉儲營收
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public void GetIncomeReportDaySheet(IWorkbook workbook, string sDate, string eDate)
        {
            CellRangeAddress cra;
            DataTable dt = IncomeReport_Day(sDate, eDate);

            int rowCount = 0, subCount = 0, total_fee2, total_bag_number, total_count, total_fee2Add, total_bag_numberAdd, total_countAdd, total_tax_N, total_tax_Y, total_tax_Nadd, total_tax_Yadd, total_ccfee, total_ccfeeadd, total_diff, total_diffadd, total_income, total_incomeadd;
            double total_cc, total_gw, total_ccAdd, total_gwAdd, total_tariff, total_tariffadd;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet("日倉儲營收");
            sheet.DefaultRowHeight = 30 * 20;
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            cra = new CellRangeAddress(0, 1, 0, 19);
            sheet.AddMergedRegion(cra);
            RegionUtil.SetBorderBottom(1, cra, sheet); // 下邊框
            RegionUtil.SetBorderLeft(1, cra, sheet); // 左邊框
            RegionUtil.SetBorderRight(1, cra, sheet); // 有邊框
            RegionUtil.SetBorderTop(1, cra, sheet); // 上邊框

            row.CreateCell(0).SetCellValue(eDate + "海空快日倉儲營收報表(未稅)");
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("分類");
            row.GetCell(0).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 0, 0));

            row.CreateCell(1).SetCellValue("倉儲");
            row.GetCell(1).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 1, 1));

            row.CreateCell(2).SetCellValue($"當日({eDate})");
            row.GetCell(2).CellStyle = cs_Center_Blue;
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 10));
            row.CreateCell(11).SetCellValue($"累計({sDate}－{eDate})");
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 11, 19));
            row.GetCell(11).CellStyle = cs_Center_Blue;

            row = sheet.CreateRow(3);
            row.CreateCell(2).SetCellValue("清關收入");
            cra = new CellRangeAddress(3, 4, 2, 2);
            sheet.AddMergedRegion(cra);

            row.CreateCell(3).SetCellValue("手續費");
            cra = new CellRangeAddress(3, 4, 3, 3);
            sheet.AddMergedRegion(cra);

            row.CreateCell(4).SetCellValue("包稅稅金差額收入");
            cra = new CellRangeAddress(3, 3, 4, 6);
            sheet.AddMergedRegion(cra);

            row.CreateCell(7).SetCellValue("營收小計");
            cra = new CellRangeAddress(3, 4, 7, 7);
            sheet.AddMergedRegion(cra);
            row.CreateCell(8).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 8, 8));
            row.CreateCell(9).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 9, 9));
            row.CreateCell(10).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 10, 10));
            row.CreateCell(11).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 11, 11));
            row.CreateCell(12).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 12, 12));
            row.CreateCell(13).SetCellValue("包稅稅金差額收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 13, 15));
            row.CreateCell(16).SetCellValue("營收小計");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 16, 16));
            row.CreateCell(17).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 17, 17));
            row.CreateCell(18).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 18, 18));
            row.CreateCell(19).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 19, 19));

            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center_Blue;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center_Blue;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;

            row = sheet.CreateRow(4);
            row.CreateCell(4).SetCellValue("應收關稅");
            row.CreateCell(5).SetCellValue("應付稅金");
            row.CreateCell(6).SetCellValue("差額");

            row.CreateCell(13).SetCellValue("應收關稅");
            row.CreateCell(14).SetCellValue("應付稅金");
            row.CreateCell(15).SetCellValue("差額");

            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;

            //合併儲存格框線
            sheet.GetRow(2).CreateCell(5).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(6).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(14).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(15).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(19).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(0).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(1).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(0).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(1).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(3).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(7).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(8).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(9).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(10).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(11).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(12).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(16).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(17).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(18).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(19).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 5500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);
            sheet.SetColumnWidth(11, 5500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 5000);
            sheet.SetColumnWidth(17, 4500);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);

            var dt_Group = from t in dt.AsEnumerable()
                           group t by new { TRAN_TYPE = t.Field<string>("TRAN_TYPE") } into g
                           orderby g.Key.TRAN_TYPE
                           select new
                           {
                               TRAN_TYPE = g.Key.TRAN_TYPE,
                               DATA_TYPE = "小計",
                               TOTAL_CC = g.Sum(r => r.Field<decimal?>("TOTAL_CC")),
                               TOTAL_FEE2 = g.Sum(r => r.Field<int?>("TOTAL_FEE2")),
                               TOTAL_TARIFF = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFF")),
                               TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                               TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                               TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                               TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                               TOTAL_CCAdd = g.Sum(r => r.Field<decimal?>("TOTAL_CCAdd")),
                               TOTAL_FEE2Add = g.Sum(r => r.Field<int?>("TOTAL_FEE2Add")),
                               TOTAL_TARIFFAdd = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFFAdd")),
                               TOTAL_TAX_YAdd = g.Sum(r => r.Field<int?>("TOTAL_TAX_YAdd")),
                               TOTAL_GWAdd = g.Sum(r => r.Field<decimal?>("TOTAL_GWAdd")),
                               TOTAL_BAG_NUMBERAdd = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBERAdd")),
                               TOTAL_COUNTAdd = g.Sum(r => r.Field<int?>("TOTAL_COUNTAdd")),
                           };

            rowCount = 5;
            //小計
            foreach (var item in dt_Group)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_fee2Add = 0;
                total_bag_numberAdd = 0;
                total_countAdd = 0;
                total_tax_N = 0;
                total_tax_Nadd = 0;
                total_tax_Y = 0;
                total_tax_Yadd = 0;
                total_ccfee = 0;
                total_ccfeeadd = 0;

                total_cc = 0;
                total_gw = 0;
                total_ccAdd = 0;
                total_gwAdd = 0;
                total_tariff = 0;
                total_tariffadd = 0;

                int.TryParse(item.TOTAL_FEE2.ToString(), out total_fee2);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_FEE2Add.ToString(), out total_fee2Add);
                int.TryParse(item.TOTAL_BAG_NUMBERAdd.ToString(), out total_bag_numberAdd);
                int.TryParse(item.TOTAL_COUNTAdd.ToString(), out total_countAdd);

                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_YAdd.ToString(), out total_tax_Yadd);
                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                double.TryParse(item.TOTAL_CCAdd.ToString(), out total_ccAdd);
                double.TryParse(item.TOTAL_GWAdd.ToString(), out total_gwAdd);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);
                double.TryParse(item.TOTAL_TARIFFAdd.ToString(), out total_tariffadd);

                //差額
                total_diff = Convert.ToInt32(Math.Ceiling(total_tariff)) - total_tax_Y;
                total_diffadd = Convert.ToInt32(Math.Ceiling(total_tariffadd)) - total_tax_Yadd;
                //營收小計
                total_income = Convert.ToInt32(Math.Ceiling(total_cc)) + total_fee2 + total_diff;
                total_incomeadd = Convert.ToInt32(Math.Ceiling(total_ccAdd)) + total_fee2Add + total_diffadd;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.TRAN_TYPE.ToString()); //分類
                row.CreateCell(1).SetCellValue(item.DATA_TYPE.ToString()); //倉儲
                row.CreateCell(2).SetCellValue(Math.Ceiling(total_cc));  //清關收入
                row.CreateCell(3).SetCellValue(total_fee2);//手續費
                row.CreateCell(4).SetCellValue(Math.Ceiling(total_tariff));//應收
                row.CreateCell(5).SetCellValue(total_tax_Y); //包稅應付稅金
                row.CreateCell(6).SetCellValue(total_diff); //差額
                row.CreateCell(7).SetCellValue(total_income); //營收小計
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw)); //重量
                row.CreateCell(9).SetCellValue(total_bag_number); //袋數
                row.CreateCell(10).SetCellValue(total_count); //筆數
                row.CreateCell(11).SetCellValue(Math.Ceiling(total_ccAdd));//清關收入
                row.CreateCell(12).SetCellValue(total_fee2Add);//手續費
                row.CreateCell(13).SetCellValue(Math.Ceiling(total_tariffadd));//應收
                row.CreateCell(14).SetCellValue(total_tax_Yadd);//包稅應付稅金
                row.CreateCell(15).SetCellValue(total_diffadd);//差額
                row.CreateCell(16).SetCellValue(total_incomeadd);//營收小計
                row.CreateCell(17).SetCellValue(Math.Ceiling(total_gwAdd));//重量
                row.CreateCell(18).SetCellValue(total_bag_numberAdd);//袋數
                row.CreateCell(19).SetCellValue(total_countAdd);//筆數

                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int_Blue;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int_Blue;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;

                rowCount++;

                subCount++;
            }
            //合計
            row = sheet.CreateRow(rowCount);
            row.CreateCell(0).SetCellValue("");
            row.CreateCell(1).SetCellValue("合計"); //合計
            row.CreateCell(2).CellFormula = $"SUM(C6:C{6 + subCount - 1})";
            row.CreateCell(3).CellFormula = $"SUM(D6:D{6 + subCount - 1})";
            row.CreateCell(4).CellFormula = $"SUM(E6:E{6 + subCount - 1})";
            row.CreateCell(5).CellFormula = $"SUM(F6:F{6 + subCount - 1})";
            row.CreateCell(6).CellFormula = $"SUM(G6:G{6 + subCount - 1})";
            row.CreateCell(7).CellFormula = $"SUM(H6:H{6 + subCount - 1})";
            row.CreateCell(8).CellFormula = $"SUM(I6:I{6 + subCount - 1})";
            row.CreateCell(9).CellFormula = $"SUM(J6:J{6 + subCount - 1})";
            row.CreateCell(10).CellFormula = $"SUM(K6:K{6 + subCount - 1})";
            row.CreateCell(11).CellFormula = $"SUM(L6:L{6 + subCount - 1})";
            row.CreateCell(12).CellFormula = $"SUM(M6:M{6 + subCount - 1})";
            row.CreateCell(13).CellFormula = $"SUM(N6:N{6 + subCount - 1})";
            row.CreateCell(14).CellFormula = $"SUM(O6:O{6 + subCount - 1})";
            row.CreateCell(15).CellFormula = $"SUM(P6:P{6 + subCount - 1})";
            row.CreateCell(16).CellFormula = $"SUM(Q6:Q{6 + subCount - 1})";
            row.CreateCell(17).CellFormula = $"SUM(R6:R{6 + subCount - 1})";
            row.CreateCell(18).CellFormula = $"SUM(S6:S{6 + subCount - 1})";
            row.CreateCell(19).CellFormula = $"SUM(T6:T{6 + subCount - 1})";

            row.GetCell(0).CellStyle = cs_Center_Blue;
            row.GetCell(1).CellStyle = cs_Center_Blue;
            row.GetCell(2).CellStyle = cs_Int_Blue;
            row.GetCell(3).CellStyle = cs_Int_Blue;
            row.GetCell(4).CellStyle = cs_Int_Blue;
            row.GetCell(5).CellStyle = cs_Int_Blue;
            row.GetCell(6).CellStyle = cs_Int_Blue;
            row.GetCell(7).CellStyle = cs_Int_Blue;
            row.GetCell(8).CellStyle = cs_Int_Blue;
            row.GetCell(9).CellStyle = cs_Int_Blue;
            row.GetCell(10).CellStyle = cs_Int_Blue;
            row.GetCell(11).CellStyle = cs_Int_Blue;
            row.GetCell(12).CellStyle = cs_Int_Blue;
            row.GetCell(13).CellStyle = cs_Int_Blue;
            row.GetCell(14).CellStyle = cs_Int_Blue;
            row.GetCell(15).CellStyle = cs_Int_Blue;
            row.GetCell(16).CellStyle = cs_Int_Blue;
            row.GetCell(17).CellStyle = cs_Int_Blue;
            row.GetCell(18).CellStyle = cs_Int_Blue;
            row.GetCell(19).CellStyle = cs_Int_Blue;
            rowCount++;

            // 排序
            DataView dv = dt.DefaultView;
            dv.Sort = "TRAN_TYPE,DATA_TYPE";
            dt = dv.ToTable();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_fee2Add = 0;
                total_bag_numberAdd = 0;
                total_countAdd = 0;
                total_tax_N = 0;
                total_tax_Nadd = 0;
                total_tax_Y = 0;
                total_tax_Yadd = 0;
                total_ccfee = 0;
                total_ccfeeadd = 0;


                total_cc = 0;
                total_gw = 0;
                total_ccAdd = 0;
                total_gwAdd = 0;
                total_tariff = 0;
                total_tariffadd = 0;

                int.TryParse(dt.Rows[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dt.Rows[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dt.Rows[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dt.Rows[i]["TOTAL_FEE2Add"].ToString(), out total_fee2Add);
                int.TryParse(dt.Rows[i]["TOTAL_BAG_NUMBERAdd"].ToString(), out total_bag_numberAdd);
                int.TryParse(dt.Rows[i]["TOTAL_COUNTAdd"].ToString(), out total_countAdd);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_NADD"].ToString(), out total_tax_Nadd);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dt.Rows[i]["TOTAL_TAX_YADD"].ToString(), out total_tax_Yadd);
                int.TryParse(dt.Rows[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dt.Rows[i]["TOTAL_CCFEEADD"].ToString(), out total_ccfeeadd);


                double.TryParse(dt.Rows[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dt.Rows[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dt.Rows[i]["TOTAL_CCAdd"].ToString(), out total_ccAdd);
                double.TryParse(dt.Rows[i]["TOTAL_GWAdd"].ToString(), out total_gwAdd);
                double.TryParse(dt.Rows[i]["TOTAL_TARIFF"].ToString(), out total_tariff);
                double.TryParse(dt.Rows[i]["TOTAL_TARIFFADD"].ToString(), out total_tariffadd);

                //差額
                total_diff = Convert.ToInt32(Math.Ceiling(total_tariff)) - total_tax_Y;
                total_diffadd = Convert.ToInt32(Math.Ceiling(total_tariffadd)) - total_tax_Yadd;
                //營收小計
                total_income = Convert.ToInt32(Math.Ceiling(total_cc)) + total_fee2 + total_diff;
                total_incomeadd = Convert.ToInt32(Math.Ceiling(total_ccAdd)) + total_fee2Add + total_diffadd;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["TRAN_TYPE"].ToString()); //分類
                row.CreateCell(1).SetCellValue(dt.Rows[i]["DATA_TYPE"].ToString()); //倉儲
                row.CreateCell(2).SetCellValue(Math.Ceiling(total_cc));  //清關收入
                row.CreateCell(3).SetCellValue(total_fee2);//手續費
                row.CreateCell(4).SetCellValue(Math.Ceiling(total_tariff));//應收
                row.CreateCell(5).SetCellValue(total_tax_Y); //包稅應付稅金
                row.CreateCell(6).SetCellValue(total_diff); //差額
                row.CreateCell(7).SetCellValue(total_income); //營收小計
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw)); //重量
                row.CreateCell(9).SetCellValue(total_bag_number); //袋數
                row.CreateCell(10).SetCellValue(total_count); //筆數
                row.CreateCell(11).SetCellValue(Math.Ceiling(total_ccAdd));//清關收入
                row.CreateCell(12).SetCellValue(total_fee2Add);//手續費
                row.CreateCell(13).SetCellValue(Math.Ceiling(total_tariffadd));//應收
                row.CreateCell(14).SetCellValue(total_tax_Yadd);//包稅應付稅金
                row.CreateCell(15).SetCellValue(total_diffadd);//差額
                row.CreateCell(16).SetCellValue(total_incomeadd);//營收小計
                row.CreateCell(17).SetCellValue(Math.Ceiling(total_gwAdd));//重量
                row.CreateCell(18).SetCellValue(total_bag_numberAdd);//袋數
                row.CreateCell(19).SetCellValue(total_countAdd);//筆數

                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int_Blue;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int_Blue;
                row.GetCell(17).CellStyle = cs_Int;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                rowCount++;
            }


        }

        /// <summary>
        /// 營收日統計表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public void GetIncomeReportDay2Sheet(DataRow[] dr, IWorkbook workbook, string sDate, string eDate, string title)
        {
            CellRangeAddress cra;
            int rowCount = 0, total_fee2, total_bag_number, total_count, total_fee2Add, total_bag_numberAdd, total_countAdd, total_tax_N, total_tax_Y, total_tax_Nadd, total_tax_Yadd, total_ccfee, total_ccfeeadd, total_diff, total_diffadd, total_income, total_incomeadd;
            double total_cc, total_gw, total_ccAdd, total_gwAdd, total_tariff, total_tariffadd;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(title);
            sheet.DefaultRowHeight = 30 * 20;
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            cra = new CellRangeAddress(0, 1, 0, 20);
            sheet.AddMergedRegion(cra);
            RegionUtil.SetBorderBottom(1, cra, sheet); // 下邊框
            RegionUtil.SetBorderLeft(1, cra, sheet); // 左邊框
            RegionUtil.SetBorderRight(1, cra, sheet); // 有邊框
            RegionUtil.SetBorderTop(1, cra, sheet); // 上邊框

            row.CreateCell(0).SetCellValue(title);
            row.GetCell(0).CellStyle = cs_Title;

            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("分類");
            row.GetCell(0).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 0, 0));
            row.CreateCell(1).SetCellValue("倉儲");
            row.GetCell(1).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 1, 1));
            row.CreateCell(2).SetCellValue("客戶");
            row.GetCell(2).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 4, 2, 2));
            row.CreateCell(3).SetCellValue($"當日({eDate})");
            row.GetCell(3).CellStyle = cs_Center_Blue;
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 3, 11));
            row.CreateCell(12).SetCellValue($"累計({sDate}－{eDate})");
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 12, 20));
            row.GetCell(12).CellStyle = cs_Center_Blue;

            row = sheet.CreateRow(3);
            row.CreateCell(3).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 3, 3));
            row.CreateCell(4).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 4, 4));
            row.CreateCell(5).SetCellValue("包稅稅金差額收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 5, 7));

            row.CreateCell(8).SetCellValue("營收小計");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 8, 8));
            row.CreateCell(9).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 9, 9));
            row.CreateCell(10).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 10, 10));
            row.CreateCell(11).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 11, 11));
            row.CreateCell(12).SetCellValue("清關收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 12, 12));
            row.CreateCell(13).SetCellValue("手續費");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 13, 13));

            row.CreateCell(14).SetCellValue("包稅稅金差額收入");
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 14, 16));

            row.CreateCell(17).SetCellValue("營收小計");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 17, 17));
            row.CreateCell(18).SetCellValue("重量");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 18, 18));
            row.CreateCell(19).SetCellValue("袋數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 19, 19));
            row.CreateCell(20).SetCellValue("筆數");
            sheet.AddMergedRegion(new CellRangeAddress(3, 4, 20, 20));

            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center_Blue;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center_Blue;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;

            row = sheet.CreateRow(4);
            row.CreateCell(5).SetCellValue("應收關稅");
            row.CreateCell(6).SetCellValue("應付稅金");
            row.CreateCell(7).SetCellValue("差額");

            row.CreateCell(14).SetCellValue("應收關稅");
            row.CreateCell(15).SetCellValue("應付稅金");
            row.CreateCell(16).SetCellValue("差額");

            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;




            //合併儲存格框線
            sheet.GetRow(2).CreateCell(6).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(7).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(15).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(16).CellStyle = cs_Center;
            sheet.GetRow(2).CreateCell(20).CellStyle = cs_Center;

            sheet.GetRow(3).CreateCell(0).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(1).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(2).CellStyle = cs_Center;

            sheet.GetRow(4).CreateCell(0).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(1).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(2).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(3).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(4).CellStyle = cs_Center;

            sheet.GetRow(4).CreateCell(8).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(9).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(10).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(11).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(12).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(13).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(17).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(18).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(19).CellStyle = cs_Center;
            sheet.GetRow(4).CreateCell(20).CellStyle = cs_Center;


            sheet.SetColumnWidth(0, 5500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 11000);
            sheet.SetColumnWidth(3, 3500);
            sheet.SetColumnWidth(4, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);
            sheet.SetColumnWidth(11, 4500);
            sheet.SetColumnWidth(12, 4500);
            sheet.SetColumnWidth(13, 4500);
            sheet.SetColumnWidth(14, 4500);
            sheet.SetColumnWidth(15, 4500);
            sheet.SetColumnWidth(16, 4500);
            sheet.SetColumnWidth(17, 5000);
            sheet.SetColumnWidth(18, 4500);
            sheet.SetColumnWidth(19, 4500);
            sheet.SetColumnWidth(20, 4500);

            for (int i = 0; i < dr.Length; i++)
            {
                total_fee2 = 0;
                total_bag_number = 0;
                total_count = 0;
                total_fee2Add = 0;
                total_bag_numberAdd = 0;
                total_countAdd = 0;
                total_tax_N = 0;
                total_tax_Nadd = 0;
                total_tax_Y = 0;
                total_tax_Yadd = 0;
                total_ccfee = 0;
                total_ccfeeadd = 0;

                total_cc = 0;
                total_gw = 0;
                total_ccAdd = 0;
                total_gwAdd = 0;
                total_tariff = 0;
                total_tariffadd = 0;

                int.TryParse(dr[i]["TOTAL_FEE2"].ToString(), out total_fee2);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBER"].ToString(), out total_bag_number);
                int.TryParse(dr[i]["TOTAL_COUNT"].ToString(), out total_count);
                int.TryParse(dr[i]["TOTAL_FEE2Add"].ToString(), out total_fee2Add);
                int.TryParse(dr[i]["TOTAL_BAG_NUMBERAdd"].ToString(), out total_bag_numberAdd);
                int.TryParse(dr[i]["TOTAL_COUNTAdd"].ToString(), out total_countAdd);
                int.TryParse(dr[i]["TOTAL_TAX_N"].ToString(), out total_tax_N);
                int.TryParse(dr[i]["TOTAL_TAX_NADD"].ToString(), out total_tax_Nadd);
                int.TryParse(dr[i]["TOTAL_TAX_Y"].ToString(), out total_tax_Y);
                int.TryParse(dr[i]["TOTAL_TAX_YADD"].ToString(), out total_tax_Yadd);
                int.TryParse(dr[i]["TOTAL_CCFEE"].ToString(), out total_ccfee);
                int.TryParse(dr[i]["TOTAL_CCFEEADD"].ToString(), out total_ccfeeadd);


                double.TryParse(dr[i]["TOTAL_CC"].ToString(), out total_cc);
                double.TryParse(dr[i]["TOTAL_GW"].ToString(), out total_gw);
                double.TryParse(dr[i]["TOTAL_CCAdd"].ToString(), out total_ccAdd);
                double.TryParse(dr[i]["TOTAL_GWAdd"].ToString(), out total_gwAdd);
                double.TryParse(dr[i]["TOTAL_TARIFF"].ToString(), out total_tariff);
                double.TryParse(dr[i]["TOTAL_TARIFFADD"].ToString(), out total_tariffadd);

                //差額
                total_diff = Convert.ToInt32(Math.Ceiling(total_tariff)) - total_tax_Y;
                total_diffadd = Convert.ToInt32(Math.Ceiling(total_tariffadd)) - total_tax_Yadd;
                //營收小計
                total_income = Convert.ToInt32(Math.Ceiling(total_cc)) + total_fee2 + total_diff;
                total_incomeadd = Convert.ToInt32(Math.Ceiling(total_ccAdd)) + total_fee2Add + total_diffadd;

                rowCount = i + 5;
                row = sheet.CreateRow(i + 5);
                row.CreateCell(0).SetCellValue(dr[i]["TRAN_TYPE"].ToString()); //分類
                row.CreateCell(1).SetCellValue(dr[i]["DATA_TYPE"].ToString()); //倉儲
                row.CreateCell(2).SetCellValue(dr[i]["DESPATCH_NAME"].ToString());//客戶
                row.CreateCell(3).SetCellValue(Math.Ceiling(total_cc));  //清關收入
                row.CreateCell(4).SetCellValue(total_fee2);//手續費
                row.CreateCell(5).SetCellValue(Math.Ceiling(total_tariff));//應收
                row.CreateCell(6).SetCellValue(total_tax_Y); //包稅應付稅金
                row.CreateCell(7).SetCellValue(total_diff); //差額
                row.CreateCell(8).SetCellValue(total_income); //營收小計
                row.CreateCell(9).SetCellValue(Math.Ceiling(total_gw)); //重量
                row.CreateCell(10).SetCellValue(total_bag_number); //袋數
                row.CreateCell(11).SetCellValue(total_count); //筆數

                row.CreateCell(12).SetCellValue(Math.Ceiling(total_ccAdd));//清關收入
                row.CreateCell(13).SetCellValue(total_fee2Add);//手續費
                row.CreateCell(14).SetCellValue(Math.Ceiling(total_tariffadd));//應收
                row.CreateCell(15).SetCellValue(total_tax_Yadd);//包稅應付稅金
                row.CreateCell(16).SetCellValue(total_diffadd);//差額
                row.CreateCell(17).SetCellValue(total_incomeadd);//營收小計
                row.CreateCell(18).SetCellValue(Math.Ceiling(total_gwAdd));//重量
                row.CreateCell(19).SetCellValue(total_bag_numberAdd);//袋數
                row.CreateCell(20).SetCellValue(total_countAdd);//筆數

                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Center;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Int_Blue;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                row.GetCell(11).CellStyle = cs_Int;
                row.GetCell(12).CellStyle = cs_Int;
                row.GetCell(13).CellStyle = cs_Int;
                row.GetCell(14).CellStyle = cs_Int;
                row.GetCell(15).CellStyle = cs_Int;
                row.GetCell(16).CellStyle = cs_Int;
                row.GetCell(17).CellStyle = cs_Int_Blue;
                row.GetCell(18).CellStyle = cs_Int;
                row.GetCell(19).CellStyle = cs_Int;
                row.GetCell(20).CellStyle = cs_Int;
            }

            row = sheet.CreateRow(rowCount + 1);
            row.CreateCell(2).SetCellValue("合計");//合計
            row.CreateCell(3).CellFormula = $"SUM(D6:D{rowCount + 1})";
            row.CreateCell(4).CellFormula = $"SUM(E6:E{rowCount + 1})";
            row.CreateCell(5).CellFormula = $"SUM(F6:F{rowCount + 1})";
            row.CreateCell(6).CellFormula = $"SUM(G6:G{rowCount + 1})";
            row.CreateCell(7).CellFormula = $"SUM(H6:H{rowCount + 1})";
            row.CreateCell(8).CellFormula = $"SUM(I6:I{rowCount + 1})";
            row.CreateCell(9).CellFormula = $"SUM(J6:J{rowCount + 1})";
            row.CreateCell(10).CellFormula = $"SUM(K6:K{rowCount + 1})";
            row.CreateCell(11).CellFormula = $"SUM(L6:L{rowCount + 1})";
            row.CreateCell(12).CellFormula = $"SUM(M6:M{rowCount + 1})";
            row.CreateCell(13).CellFormula = $"SUM(N6:N{rowCount + 1})";
            row.CreateCell(14).CellFormula = $"SUM(O6:O{rowCount + 1})";
            row.CreateCell(15).CellFormula = $"SUM(P6:P{rowCount + 1})";
            row.CreateCell(16).CellFormula = $"SUM(Q6:Q{rowCount + 1})";
            row.CreateCell(17).CellFormula = $"SUM(R6:R{rowCount + 1})";
            row.CreateCell(18).SetCellValue(
                Math.Ceiling(
                    dr.AsEnumerable()
                    .Sum(r => Convert.ToDouble(r["total_gwAdd"] == DBNull.Value ? 0 : r["total_gwAdd"]))
                )
            );
            //row.CreateCell(18).CellFormula = $"SUM(S6:S{rowCount + 1})";
            row.CreateCell(19).CellFormula = $"SUM(T6:T{rowCount + 1})";
            row.CreateCell(20).CellFormula = $"SUM(U6:U{rowCount + 1})";

            row.GetCell(2).CellStyle = cs_Int_Blue;
            row.GetCell(3).CellStyle = cs_Int_Blue;
            row.GetCell(4).CellStyle = cs_Int_Blue;
            row.GetCell(5).CellStyle = cs_Int_Blue;
            row.GetCell(6).CellStyle = cs_Int_Blue;
            row.GetCell(7).CellStyle = cs_Int_Blue;
            row.GetCell(8).CellStyle = cs_Int_Blue;
            row.GetCell(9).CellStyle = cs_Int_Blue;
            row.GetCell(10).CellStyle = cs_Int_Blue;
            row.GetCell(11).CellStyle = cs_Int_Blue;
            row.GetCell(12).CellStyle = cs_Int_Blue;
            row.GetCell(13).CellStyle = cs_Int_Blue;
            row.GetCell(14).CellStyle = cs_Int_Blue;
            row.GetCell(15).CellStyle = cs_Int_Blue;
            row.GetCell(16).CellStyle = cs_Int_Blue;
            row.GetCell(17).CellStyle = cs_Int_Blue;
            row.GetCell(18).CellStyle = cs_Int_Blue;
            row.GetCell(19).CellStyle = cs_Int_Blue;
            row.GetCell(20).CellStyle = cs_Int_Blue;
        }

        /// <summary>
        /// 取得稅金EXCEL
        /// </summary>
        /// <param name="filePath"></param>
        void GetTaxReportExcel(string sDate, string eDate, string filePath)
        {
            IWorkbook workbook = new XSSFWorkbook();
            DataTable dt_Source = TaxSourceReport(sDate, eDate);
            DataTable dt_Customer = TaxCustomerReport(sDate, eDate);
            //空運稅金
            GetTaxReportSheet("空快", dt_Source, dt_Customer, workbook, sDate, eDate);
            //海運稅金
            GetTaxReportSheet("海快", dt_Source, dt_Customer, workbook, sDate, eDate);
            FileStream file = new FileStream(filePath, FileMode.Create);
            workbook.Write(file);
            file.Close();
        }

        /// <summary>
        /// 取得稅金頁籤
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public void GetTaxReportSheet(string tranType, DataTable dt_Source, DataTable dt_Customer, IWorkbook workbook, string sDate, string eDate)
        {
            CellRangeAddress cra;
            int rowCount = 0, totalCount, totalTax, totalCountAdd, totalTaxAdd;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet($"{tranType}稅金");
            sheet.DefaultRowHeight = 30 * 20;
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            cra = new CellRangeAddress(0, 1, 0, 5);
            sheet.AddMergedRegion(cra);
            RegionUtil.SetBorderBottom(1, cra, sheet); // 下邊框
            RegionUtil.SetBorderLeft(1, cra, sheet); // 左邊框
            RegionUtil.SetBorderRight(1, cra, sheet); // 有邊框
            RegionUtil.SetBorderTop(1, cra, sheet); // 上邊框

            row.CreateCell(0).SetCellValue($"{eDate.Substring(4, 4)}{tranType}稅金統計表");
            row.GetCell(0).CellStyle = cs_Title;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("倉儲");
            row.GetCell(0).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 0, 0));

            row.CreateCell(1).SetCellValue("客戶");
            row.GetCell(1).CellStyle = cs_Center;
            sheet.AddMergedRegion(new CellRangeAddress(2, 3, 1, 1));

            row.CreateCell(2).SetCellValue($"當日({eDate.Substring(4, 4)})");
            row.GetCell(2).CellStyle = cs_Center_Blue;
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 3));
            row.CreateCell(4).SetCellValue($"累計({sDate.Substring(4, 4)}－{eDate.Substring(4, 4)})");
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 4, 5));
            row.GetCell(4).CellStyle = cs_Center_Blue;

            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;

            row = sheet.CreateRow(3);
            row.CreateCell(2).SetCellValue("筆數");
            row.CreateCell(3).SetCellValue("稅金");
            row.CreateCell(4).SetCellValue("筆數");
            row.CreateCell(5).SetCellValue("稅金");
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            //合併儲存格框線
            sheet.GetRow(2).CreateCell(5).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(0).CellStyle = cs_Center;
            sheet.GetRow(3).CreateCell(1).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 9000);
            sheet.SetColumnWidth(2, 4000);
            sheet.SetColumnWidth(3, 5500);
            sheet.SetColumnWidth(4, 4000);
            sheet.SetColumnWidth(5, 5500);


            //倉儲合計
            var taxSum = new
            {
                TotalCount = dt_Source.AsEnumerable().Where(t => t.Field<string>("TranType") == tranType).Sum(t => t.Field<int?>("TotalCount")),
                TotalTax = dt_Source.AsEnumerable().Where(t => t.Field<string>("TranType") == tranType).Sum(t => t.Field<int?>("TotalTax")),
                TotalCountAdd = dt_Source.AsEnumerable().Where(t => t.Field<string>("TranType") == tranType).Sum(t => t.Field<int?>("TotalCountAdd")),
                TotalTaxAdd = dt_Source.AsEnumerable().Where(t => t.Field<string>("TranType") == tranType).Sum(t => t.Field<int?>("TotalTaxAdd")),
            };

            //row = sheet.CreateRow(4);
            //row.CreateCell(0).SetCellValue("合計"); 
            //row.CreateCell(1).SetCellValue("合計");
            //row.CreateCell(2).SetCellValue((int)taxSum.TotalCount);
            //row.CreateCell(3).SetCellValue((int)taxSum.TotalTax);
            //row.CreateCell(4).SetCellValue((int)taxSum.TotalCountAdd);
            //row.CreateCell(5).SetCellValue((int)taxSum.TotalTaxAdd);
            //row.GetCell(0).CellStyle = cs_Center_Blue_Thick;
            //row.GetCell(1).CellStyle = cs_Center_Blue_Thick;
            //row.GetCell(2).CellStyle = cs_Int_Blue_Thick;
            //row.GetCell(3).CellStyle = cs_Int_Blue_Thick;
            //row.GetCell(4).CellStyle = cs_Int_Blue_Thick;
            //row.GetCell(5).CellStyle = cs_Int_Blue_Thick;

            rowCount = 4;
            var sourceList = dt_Source.AsEnumerable().Where(t => t.Field<string>("TranType") == tranType);
            var customerList = dt_Customer.AsEnumerable().Where(t => t.Field<string>("TranType") == tranType);
            foreach (var item in sourceList)
            {
                totalCount = item.Field<int?>("TotalCount") ?? 0;
                totalTax = item.Field<int?>("TotalTax") ?? 0;
                totalCountAdd = item.Field<int?>("TotalCountAdd") ?? 0;
                totalTaxAdd = item.Field<int?>("TotalTaxAdd") ?? 0;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.Field<string>("SOURCE").ToString()); //倉儲
                row.CreateCell(1).SetCellValue("小計");
                row.CreateCell(2).SetCellValue(totalCount); //當日筆數
                row.CreateCell(3).SetCellValue(totalTax);//當日稅金
                row.CreateCell(4).SetCellValue(totalCountAdd);//累計筆數
                row.CreateCell(5).SetCellValue(totalTaxAdd); //累計稅金

                row.GetCell(0).CellStyle = cs_Center_Thick;
                row.GetCell(1).CellStyle = cs_Center_Thick;
                row.GetCell(2).CellStyle = cs_Int_Thick;
                row.GetCell(3).CellStyle = cs_Int_Thick;
                row.GetCell(4).CellStyle = cs_Int_Thick;
                row.GetCell(5).CellStyle = cs_Int_Thick;
                rowCount++;
            }

            foreach (var item in customerList)
            {
                totalCount = item.Field<int?>("TotalCount") ?? 0;
                totalTax = item.Field<int?>("TotalTax") ?? 0;
                totalCountAdd = item.Field<int?>("TotalCountAdd") ?? 0;
                totalTaxAdd = item.Field<int?>("TotalTaxAdd") ?? 0;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.Field<string>("TranType")); //倉儲
                row.CreateCell(1).SetCellValue(item.Field<string>("CUST_NAME")); //客戶
                row.CreateCell(2).SetCellValue(totalCount); //當日筆數
                row.CreateCell(3).SetCellValue(totalTax);//當日稅金
                row.CreateCell(4).SetCellValue(totalCountAdd);//累計筆數
                row.CreateCell(5).SetCellValue(totalTaxAdd); //累計稅金

                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Letf;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                rowCount++;
            }
        }

        public DataTable IncomeReport_Day(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Report_Day]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt);
            }
            return dt;
        }

        public DataTable IncomeReport_Day2(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Report_Day2]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@sDataDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDataDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            return dt;
        }

        public DataTable TaxCustomerReport(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("[jetf].[dbo].[SP_Select_Tax_Customer_Report]");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            return dt;
        }

        public DataTable TaxSourceReport(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("[jetf].[dbo].[SP_Select_Tax_Source_Report]");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 營收轉檔
        /// </summary>
        public void InsertIncomeReport()
        {
            try
            {
                DateTime now = DateTime.Now;
                string sDate = now.AddDays(-1).ToString("yyyyMM") + "01";
                string eDate = now.AddDays(-1).ToString("yyyyMMdd");

                int days = Convert.ToInt32((DateTime.ParseExact(eDate, "yyyyMMdd", null) - DateTime.ParseExact(sDate, "yyyyMMdd", null)).TotalDays) + 1;
                DateTime date = DateTime.ParseExact(eDate, "yyyyMMdd", null);
                conn.Open();
                for (int i = 0; i < days; i++)
                {
                    using (SqlCommand cmd = new SqlCommand("jetf.dbo.SP_Insert_Income_Report", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Clear();
                        cmd.Parameters.Add("@DataDate", SqlDbType.NVarChar).Value = date.AddDays(-i).ToString("yyyyMMdd");
                        cmd.Parameters.Add("@SDate_ETL", SqlDbType.DateTime).Value = $"{date.AddDays(-i).ToString("yyyy-MM-dd")} 09:00:00";
                        cmd.Parameters.Add("@EDate_ETL", SqlDbType.DateTime).Value = $"{date.AddDays(-i + 1).ToString("yyyy-MM-dd")} 08:59:59";
                        cmd.Parameters.Add("@SDate", SqlDbType.DateTime).Value = $"{date.AddDays(-i).ToString("yyyy-MM-dd")} 00:00:00";
                        cmd.Parameters.Add("@EDate", SqlDbType.DateTime).Value = $"{date.AddDays(-i).ToString("yyyy-MM-dd")} 23:59:59";
                        cmd.CommandTimeout = 600;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJobErrorLog("營收轉檔", ex);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 營收總表及明細表-總表-海運
        /// </summary>
        public void GetIncomeDetailsSeaReportExcel(string sDate, string eDate, string filePath)
        {
            IWorkbook workbook = new XSSFWorkbook();
            DataTable dt_Report = IncomeDetailsReport("SEA", sDate + "000000", eDate + "235959");
            //日倉儲總表
            GetIncomeDetailsSeaReportSheet(workbook, dt_Report, "海快通關狀態彙總表", sDate, eDate);
            FileStream file = new FileStream(filePath, FileMode.Create);
            workbook.Write(file);
            file.Close();
        }

        /// <summary>
        /// 營收總表及明細表-總表-空運
        /// </summary>
        public void GetIncomeDetailsEtlReportExcel(DataTable dt_Report, string sDate, string eDate, string filePath)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //日倉儲總表
            GetIncomeDetailsEtlReportSheet(workbook, dt_Report, "空快通關狀態彙總表", sDate, eDate);
            FileStream file = new FileStream(filePath, FileMode.Create);
            workbook.Write(file);
            file.Close();
        }

        public DataTable IncomeDetailsReport(string originl, string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Income_Details_Report]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@ORIGINAL", SqlDbType.NVarChar).Value = originl;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.DateTime).Value = DateTime.ParseExact(sDate, "yyyyMMddHHmmss", null);
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.DateTime).Value = DateTime.ParseExact(eDate, "yyyyMMddHHmmss", null);
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 營收總表及明細表-總表-海運
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public void GetIncomeDetailsSeaReportSheet(IWorkbook workbook, DataTable dt_Report, string sheetName, string sDate, string eDate)
        {
            int rowCount, total_fee, total_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            sheet.DefaultRowHeight = 30 * 20;
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            CellRangeAddress cra = new CellRangeAddress(0, 1, 0, 10);
            sheet.AddMergedRegion(cra);
            RegionUtil.SetBorderBottom(1, cra, sheet); // 下邊框
            RegionUtil.SetBorderLeft(1, cra, sheet); // 左邊框
            RegionUtil.SetBorderRight(1, cra, sheet); // 有邊框
            RegionUtil.SetBorderTop(1, cra, sheet); // 上邊框

            row.CreateCell(0).SetCellValue(eDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title_Left;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("通關主號數");
            row.CreateCell(3).SetCellValue("入倉件數\nA");
            row.CreateCell(4).SetCellValue("出倉件數\nB");
            row.CreateCell(5).SetCellValue("C3件數\nC");
            row.CreateCell(6).SetCellValue("留庫件數\nD=A-B");
            row.CreateCell(7).SetCellValue("查驗比率\n(C+D)/A");
            row.CreateCell(8).SetCellValue("入倉毛重");
            row.CreateCell(9).SetCellValue("袋數");
            row.CreateCell(10).SetCellValue("筆數");
            row.Height = 30 * 40;
            //row.CreateCell(10).SetCellValue("清關收入");
            //row.CreateCell(11).SetCellValue("手續費");
            //row.CreateCell(12).SetCellValue("應收關稅");
            //row.CreateCell(13).SetCellValue("包稅應付稅金");
            //row.CreateCell(14).SetCellValue("出貨人應付稅金");
            //row.CreateCell(15).SetCellValue("收件人應付稅金");
            //row.CreateCell(16).SetCellValue("應付報關費");

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
            //row.GetCell(12).CellStyle = cs_Center;
            //row.GetCell(13).CellStyle = cs_Center;
            //row.GetCell(14).CellStyle = cs_Center;
            //row.GetCell(15).CellStyle = cs_Center;
            //row.GetCell(16).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 4500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 3500);
            sheet.SetColumnWidth(4, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);
            //sheet.SetColumnWidth(11, 4500);
            //sheet.SetColumnWidth(12, 4500);
            //sheet.SetColumnWidth(13, 6500);
            //sheet.SetColumnWidth(14, 6500);
            //sheet.SetColumnWidth(15, 6500);
            //sheet.SetColumnWidth(16, 5500);

            var dt_Group_Type = from t in dt_Report.AsEnumerable()
                                group t by new { DATADATE = t.Field<string>("DATADATE"), I_DATA_TYPE = t.Field<string>("I_DATA_TYPE") } into g
                                orderby g.Key.DATADATE
                                select new
                                {
                                    DATADATE = g.Key.DATADATE,
                                    I_DATA_TYPE = g.Key.I_DATA_TYPE,
                                    TOTAL_MAINNUMBER = g.Count(),
                                    TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                    TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                    TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                    TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                    TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                    TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                    TOTAL_CC = g.Sum(r => r.Field<decimal?>("TOTAL_CC")),
                                    TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                    TOTAL_TARIFF = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFF")),
                                    TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                    TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                    TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                    TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                                };

            var dt_Group_Day = from t in dt_Report.AsEnumerable()
                               group t by new { DATADATE = t.Field<string>("DATADATE") } into g
                               orderby g.Key.DATADATE
                               select new
                               {
                                   DATADATE = g.Key.DATADATE,
                                   I_DATA_TYPE = "日小計",
                                   TOTAL_MAINNUMBER = g.Count(),
                                   TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                   TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                   TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                   TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                   TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                   TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                   TOTAL_CC = g.Sum(r => r.Field<decimal?>("TOTAL_CC")),
                                   TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                   TOTAL_TARIFF = g.Sum(r => r.Field<decimal?>("TOTAL_TARIFF")),
                                   TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                   TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                   TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                   TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                               };

            //for (int i = 0; i < dr.Length; i++)
            rowCount = 3;
            foreach (var item in dt_Group_Day)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_piece_c3);
                row.CreateCell(6).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(7).CellFormula = $"(F{rowCount + 1}+G{rowCount + 1})/D{rowCount + 1}";
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(9).SetCellValue(total_bag_number);
                row.CreateCell(10).SetCellValue(total_count);
                //row.CreateCell(10).SetCellValue(Math.Ceiling(total_cc));
                //row.CreateCell(11).SetCellValue(total_fee);
                //row.CreateCell(12).SetCellValue(Math.Ceiling(total_tariff));
                //row.CreateCell(13).SetCellValue(total_tax_Y);
                //row.CreateCell(14).SetCellValue(total_tax_C);
                //row.CreateCell(15).SetCellValue(total_tax_N);
                //row.CreateCell(16).SetCellValue(total_ccfee);


                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Percent;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                //row.GetCell(11).CellStyle = cs_Int;
                //row.GetCell(12).CellStyle = cs_Int;
                //row.GetCell(13).CellStyle = cs_Int;
                //row.GetCell(14).CellStyle = cs_Int;
                //row.GetCell(15).CellStyle = cs_Int;
                //row.GetCell(16).CellStyle = cs_Int;
                rowCount++;
            }

            foreach (var item in dt_Group_Type)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_piece_c3);
                row.CreateCell(6).SetCellValue(total_piece - total_out_piece);
                row.CreateCell(7).CellFormula = $"(F{rowCount + 1}+G{rowCount + 1})/D{rowCount + 1}";
                row.CreateCell(8).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(9).SetCellValue(total_bag_number);
                row.CreateCell(10).SetCellValue(total_count);
                //row.CreateCell(10).SetCellValue(Math.Ceiling(total_cc));
                //row.CreateCell(11).SetCellValue(total_fee);
                //row.CreateCell(12).SetCellValue(Math.Ceiling(total_tariff));
                //row.CreateCell(13).SetCellValue(total_tax_Y);
                //row.CreateCell(14).SetCellValue(total_tax_C);
                //row.CreateCell(15).SetCellValue(total_tax_N);
                //row.CreateCell(16).SetCellValue(total_ccfee);


                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Percent;
                row.GetCell(8).CellStyle = cs_Int;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                //row.GetCell(11).CellStyle = cs_Int;
                //row.GetCell(12).CellStyle = cs_Int;
                //row.GetCell(13).CellStyle = cs_Int;
                //row.GetCell(14).CellStyle = cs_Int;
                //row.GetCell(15).CellStyle = cs_Int;
                //row.GetCell(16).CellStyle = cs_Int;
                rowCount++;
            }
        }

        /// <summary>
        /// 營收總表及明細表-總表-空運
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        public void GetIncomeDetailsEtlReportSheet(IWorkbook workbook, DataTable dt_Report, string sheetName, string sDate, string eDate)
        {
            int rowCount, total_fee, total_bag_number, total_out_bag_number, total_count, total_piece, total_out_piece, total_piece_all, total_piece_c3, total_tax_N, total_tax_Y, total_tax_C, total_ccfee;
            double total_cc, total_gw, total_gw_all, total_tariff;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            sheet.DefaultRowHeight = 30 * 20;
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            CellRangeAddress cra = new CellRangeAddress(0, 1, 0, 10);
            sheet.AddMergedRegion(cra);
            RegionUtil.SetBorderBottom(1, cra, sheet); // 下邊框
            RegionUtil.SetBorderLeft(1, cra, sheet); // 左邊框
            RegionUtil.SetBorderRight(1, cra, sheet); // 有邊框
            RegionUtil.SetBorderTop(1, cra, sheet); // 上邊框
            row.CreateCell(0).SetCellValue(sDate + sheetName);
            row.GetCell(0).CellStyle = cs_Title_Left;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("入倉日");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("通關主號數");
            row.CreateCell(3).SetCellValue("入倉件數\nA");
            row.CreateCell(4).SetCellValue("出倉件數\nB");
            row.CreateCell(5).SetCellValue("入倉袋數\nC");
            row.CreateCell(6).SetCellValue("出倉袋數\nD");
            row.CreateCell(7).SetCellValue("查驗袋數\nE=C-D");
            row.CreateCell(8).SetCellValue("查驗比率\nE/A");
            row.CreateCell(9).SetCellValue("入倉毛重");
            row.CreateCell(10).SetCellValue("筆數");
            row.Height = 30 * 40;
            //row.CreateCell(10).SetCellValue("清關收入");
            //row.CreateCell(11).SetCellValue("手續費");
            //row.CreateCell(12).SetCellValue("應收關稅");
            //row.CreateCell(13).SetCellValue("包稅應付稅金");
            //row.CreateCell(14).SetCellValue("出貨人應付稅金");
            //row.CreateCell(15).SetCellValue("收件人應付稅金");
            //row.CreateCell(16).SetCellValue("應付報關費");

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
            //row.GetCell(11).CellStyle = cs_Center;
            //row.GetCell(12).CellStyle = cs_Center;
            //row.GetCell(13).CellStyle = cs_Center;
            //row.GetCell(14).CellStyle = cs_Center;
            //row.GetCell(15).CellStyle = cs_Center;
            //row.GetCell(16).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 4500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 3500);
            sheet.SetColumnWidth(4, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 4500);
            sheet.SetColumnWidth(6, 4500);
            sheet.SetColumnWidth(7, 4500);
            sheet.SetColumnWidth(8, 4500);
            sheet.SetColumnWidth(9, 4500);
            sheet.SetColumnWidth(10, 4500);
            //sheet.SetColumnWidth(10, 4500);
            //sheet.SetColumnWidth(11, 4500);
            //sheet.SetColumnWidth(12, 4500);
            //sheet.SetColumnWidth(13, 6500);
            //sheet.SetColumnWidth(14, 6500);
            //sheet.SetColumnWidth(15, 6500);
            //sheet.SetColumnWidth(16, 5500);

            var dt_Group_Type = from t in dt_Report.AsEnumerable()
                                group t by new { DATADATE = t.Field<string>("DATADATE"), I_DATA_TYPE = t.Field<string>("I_DATA_TYPE") } into g
                                orderby g.Key.DATADATE
                                select new
                                {
                                    DATADATE = g.Key.DATADATE,
                                    I_DATA_TYPE = g.Key.I_DATA_TYPE,
                                    TOTAL_MAINNUMBER = g.Count(),
                                    TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                    TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                    TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                    TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                    TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                    TOTAL_OUT_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_OUT_BAG_NUMBER")),
                                    TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                    TOTAL_CC = g.Sum(r => r.Field<int?>("TOTAL_CC")),
                                    TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                    TOTAL_TARIFF = g.Sum(r => r.Field<int?>("TOTAL_TARIFF")),
                                    TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                    TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                    TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                    TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                                };

            var dt_Group_Day = from t in dt_Report.AsEnumerable()
                               group t by new { DATADATE = t.Field<string>("DATADATE") } into g
                               orderby g.Key.DATADATE
                               select new
                               {
                                   DATADATE = g.Key.DATADATE,
                                   I_DATA_TYPE = "日小計",
                                   TOTAL_MAINNUMBER = g.Count(),
                                   TOTAL_PIECE = g.Sum(r => r.Field<int?>("TOTAL_PIECE")),
                                   TOTAL_OUT_PIECE = g.Sum(r => r.Field<int?>("TOTAL_OUT_PIECE")),
                                   TOTAL_PIECE_C3 = g.Sum(r => r.Field<int?>("TOTAL_PIECE_C3")),
                                   TOTAL_GW = g.Sum(r => r.Field<decimal?>("TOTAL_GW")),
                                   TOTAL_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_BAG_NUMBER")),
                                   TOTAL_OUT_BAG_NUMBER = g.Sum(r => r.Field<int?>("TOTAL_OUT_BAG_NUMBER")),
                                   TOTAL_COUNT = g.Sum(r => r.Field<int?>("TOTAL_COUNT")),
                                   TOTAL_CC = g.Sum(r => r.Field<int?>("TOTAL_CC")),
                                   TOTAL_FEE = g.Sum(r => r.Field<int?>("TOTAL_FEE")),
                                   TOTAL_TARIFF = g.Sum(r => r.Field<int?>("TOTAL_TARIFF")),
                                   TOTAL_TAX_Y = g.Sum(r => r.Field<int?>("TOTAL_TAX_Y")),
                                   TOTAL_TAX_C = g.Sum(r => r.Field<int?>("TOTAL_TAX_C")),
                                   TOTAL_TAX_N = g.Sum(r => r.Field<int?>("TOTAL_TAX_N")),
                                   TOTAL_CCFEE = g.Sum(r => r.Field<int?>("TOTAL_CCFEE")),
                               };

            //for (int i = 0; i < dr.Length; i++)
            rowCount = 3;
            foreach (var item in dt_Group_Day)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_out_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_OUT_BAG_NUMBER.ToString(), out total_out_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_bag_number);
                row.CreateCell(6).SetCellValue(total_out_bag_number);
                row.CreateCell(7).CellFormula = $"F{rowCount + 1}-G{rowCount + 1}";
                row.CreateCell(8).CellFormula = $"H{rowCount + 1}/D{rowCount + 1}";
                row.CreateCell(9).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(10).SetCellValue(total_count);
                //row.CreateCell(10).SetCellValue(Math.Ceiling(total_cc));
                //row.CreateCell(11).SetCellValue(total_fee);
                //row.CreateCell(12).SetCellValue(Math.Ceiling(total_tariff));
                //row.CreateCell(13).SetCellValue(total_tax_Y);
                //row.CreateCell(14).SetCellValue(total_tax_C);
                //row.CreateCell(15).SetCellValue(total_tax_N);
                //row.CreateCell(16).SetCellValue(total_ccfee);


                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Percent2;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                //row.GetCell(10).CellStyle = cs_Int;
                //row.GetCell(11).CellStyle = cs_Int;
                //row.GetCell(12).CellStyle = cs_Int;
                //row.GetCell(13).CellStyle = cs_Int;
                //row.GetCell(14).CellStyle = cs_Int;
                //row.GetCell(15).CellStyle = cs_Int;
                //row.GetCell(16).CellStyle = cs_Int;
                rowCount++;
            }

            foreach (var item in dt_Group_Type)
            {
                total_fee = 0;
                total_bag_number = 0;
                total_out_bag_number = 0;
                total_count = 0;
                total_tax_N = 0;
                total_tax_Y = 0;
                total_tax_C = 0;
                total_ccfee = 0;
                total_piece = 0;
                total_out_piece = 0;
                total_piece_all = 0;
                total_piece_c3 = 0;

                total_cc = 0;
                total_gw = 0;
                total_gw_all = 0;
                total_tariff = 0;

                int.TryParse(item.TOTAL_FEE.ToString(), out total_fee);
                int.TryParse(item.TOTAL_BAG_NUMBER.ToString(), out total_bag_number);
                int.TryParse(item.TOTAL_OUT_BAG_NUMBER.ToString(), out total_out_bag_number);
                int.TryParse(item.TOTAL_COUNT.ToString(), out total_count);
                int.TryParse(item.TOTAL_TAX_N.ToString(), out total_tax_N);
                int.TryParse(item.TOTAL_TAX_Y.ToString(), out total_tax_Y);
                int.TryParse(item.TOTAL_TAX_C.ToString(), out total_tax_C);
                int.TryParse(item.TOTAL_CCFEE.ToString(), out total_ccfee);
                int.TryParse(item.TOTAL_PIECE.ToString(), out total_piece);
                int.TryParse(item.TOTAL_OUT_PIECE.ToString(), out total_out_piece);
                //int.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[1], out total_piece_all);
                int.TryParse(item.TOTAL_PIECE_C3.ToString(), out total_piece_c3);

                double.TryParse(item.TOTAL_CC.ToString(), out total_cc);
                double.TryParse(item.TOTAL_GW.ToString(), out total_gw);
                //double.TryParse(item.TOTAL_GW_PIECE_All.ToString().Split(',')[0], out total_gw_all);
                double.TryParse(item.TOTAL_TARIFF.ToString(), out total_tariff);

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.DATADATE.ToString());
                row.CreateCell(1).SetCellValue(item.I_DATA_TYPE ?? "");
                row.CreateCell(2).SetCellValue(item.TOTAL_MAINNUMBER);
                row.CreateCell(3).SetCellValue(total_piece);
                row.CreateCell(4).SetCellValue(total_out_piece);
                row.CreateCell(5).SetCellValue(total_bag_number);
                row.CreateCell(6).SetCellValue(total_out_bag_number);
                row.CreateCell(7).CellFormula = $"F{rowCount + 1}-G{rowCount + 1}";
                row.CreateCell(8).CellFormula = $"H{rowCount + 1}/D{rowCount + 1}";
                row.CreateCell(9).SetCellValue(Math.Ceiling(total_gw));
                row.CreateCell(10).SetCellValue(total_count);
                //row.CreateCell(10).SetCellValue(Math.Ceiling(total_cc));
                //row.CreateCell(11).SetCellValue(total_fee);
                //row.CreateCell(12).SetCellValue(Math.Ceiling(total_tariff));
                //row.CreateCell(13).SetCellValue(total_tax_Y);
                //row.CreateCell(14).SetCellValue(total_tax_C);
                //row.CreateCell(15).SetCellValue(total_tax_N);
                //row.CreateCell(16).SetCellValue(total_ccfee);


                row.GetCell(0).CellStyle = cs_Center;
                row.GetCell(1).CellStyle = cs_Center;
                row.GetCell(2).CellStyle = cs_Int;
                row.GetCell(3).CellStyle = cs_Int;
                row.GetCell(4).CellStyle = cs_Int;
                row.GetCell(5).CellStyle = cs_Int;
                row.GetCell(6).CellStyle = cs_Int;
                row.GetCell(7).CellStyle = cs_Int;
                row.GetCell(8).CellStyle = cs_Percent2;
                row.GetCell(9).CellStyle = cs_Int;
                row.GetCell(10).CellStyle = cs_Int;
                //row.GetCell(11).CellStyle = cs_Int;
                //row.GetCell(12).CellStyle = cs_Int;
                //row.GetCell(13).CellStyle = cs_Int;
                //row.GetCell(14).CellStyle = cs_Int;
                //row.GetCell(15).CellStyle = cs_Int;
                //row.GetCell(16).CellStyle = cs_Int;
                rowCount++;
            }
        }

        public void GetWorkbookStyle(IWorkbook workbook)
        {
            //藍色的Style
            fontB = workbook.CreateFont();
            fontB.FontName = "微軟正黑體";
            fontB.Color = NPOI.SS.UserModel.IndexedColors.Blue.Index;
            fontB.FontHeightInPoints = 18;

            font1 = (XSSFFont)workbook.CreateFont();
            font1.FontName = "微軟正黑體";
            font1.FontHeightInPoints = 18;

            //標題
            cs_Title = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Title.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title.SetFont(font1);
            //標題
            cs_Title_Left = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title_Left.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Title_Left.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Title_Left.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Title_Left.SetFont(font1);


            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center.SetFont(font1);

            cs_Letf = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Letf.WrapText = true;//設置換行這個要先設置
            cs_Letf.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Letf.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Letf.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Letf.SetFont(font1);


            cs_Center_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Thick.WrapText = true;//設置換行這個要先設置
            cs_Center_Thick.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Thick.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Thick.SetFont(font1);

            cs_Center_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center_Blue.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Center_Blue.SetFont(fontB);

            cs_Center_Blue_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue_Thick.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue_Thick.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            cs_Center_Blue_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Center_Blue_Thick.SetFont(fontB);

            format = (XSSFDataFormat)workbook.CreateDataFormat();

            cs_Int = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int.DataFormat = format.GetFormat("#,##0");
            cs_Int.SetFont(font1);

            cs_Int_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Thick.DataFormat = format.GetFormat("#,##0");
            cs_Int_Thick.SetFont(font1);

            cs_Int_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int_Blue.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Int_Blue.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue.SetFont(fontB);

            cs_Int_Blue_Thick = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Int_Blue_Thick.BorderTop = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.BorderRight = NPOI.SS.UserModel.BorderStyle.Thick;
            cs_Int_Blue_Thick.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue_Thick.SetFont(fontB);

            cs_Double = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Double.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Double.DataFormat = format.GetFormat("#,##0.000");
            cs_Double.SetFont(font1);

            cs_Percent = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.DataFormat = format.GetFormat("0.00%");
            cs_Percent.SetFont(font1);

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Percent2.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0.000%");
            cs_Percent2.SetFont(font1);


        }

        /// <summary>
        /// 取得是否要發送LINE
        /// </summary>
        /// <param name="sendName"></param>
        /// <returns></returns>
        public DataTable checkSend(string sendName, string sendDate, string sendTime)
        {
            DateTime date = DateTime.Now;
            string week = DateTime.Now.DayOfWeek.ToString("d");
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM [jetf].[dbo].[TelegramSendMessageIncome] ");
            sb.Append("where SendName=@SendName and SendTime<=@SendTime and SendDate<@SendDate ");
            switch (week)
            {
                case "1":
                    sb.Append("and W1='1' ");
                    break;
                case "2":
                    sb.Append("and W2='1' ");
                    break;
                case "3":
                    sb.Append("and W3='1' ");
                    break;
                case "4":
                    sb.Append("and W4='1' ");
                    break;
                case "5":
                    sb.Append("and W5='1' ");
                    break;
                case "6":
                    sb.Append("and W6='1' ");
                    break;
                case "0":
                    sb.Append("and W7='1' ");
                    break;
            }

            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@SendName", SqlDbType.NVarChar).Value = sendName;
                da.SelectCommand.Parameters.Add("@SendTime", SqlDbType.NVarChar).Value = sendTime;
                da.SelectCommand.Parameters.Add("@SendDate", SqlDbType.NVarChar).Value = sendDate;
                da.Fill(dt);
            }
            return dt;
        }

        public void UpdateTelegramSendMessage(string id, string sendDate)
        {
            using (SqlCommand cmd = new SqlCommand("update [jetf].[dbo].[TelegramSendMessageIncome] set SendDate=@SendDate where Id=@Id", conn))
            {
                cmd.Parameters.Add("@SendDate", SqlDbType.NVarChar).Value = sendDate;
                cmd.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

    }
}
