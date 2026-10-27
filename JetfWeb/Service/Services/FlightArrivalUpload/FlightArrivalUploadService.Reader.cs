using NPOI.SS.UserModel;
using Service.Extensions;
using Service.Services.FlightArrivalUpload.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Service.Services.FlightArrivalUpload
{
    public partial class FlightArrivalUploadService
    {
        /// <summary>
        /// 檢查必要工作表，解析 A03、B6F、班機派件送達資料及待新增的處理紀錄。
        /// </summary>
        /// <param name="workbook">待匯入的 Excel 活頁簿。</param>
        /// <param name="today">本次上傳時間，供月份及處理備註日期使用。</param>
        /// <returns>原有上傳資料及依分提單號、處理類型去重的處理紀錄。</returns>
        /// <exception cref="InvalidDataException">缺少必要工作表。</exception>
        internal static FlightArrivalUploadData ReadWorkbook(IWorkbook workbook, DateTime today)
        {
            var a03 = workbook.GetSheet("A03");
            var b6f = workbook.GetSheet("B6F");
            if (a03 == null)
            {
                throw new InvalidDataException("上傳檔案無頁籤[A03]，請確認");
            }
            if (b6f == null)
            {
                throw new InvalidDataException("上傳檔案無頁籤[B6F]，請確認");
            }

            var data = new FlightArrivalUploadData
            {
                ClearanceRows = new List<FlightArrivalUploadClearanceModel>(),
                ArrivalRows = new List<FlightArrivalUploadArrivalModel>(),
                Processes = new List<FlightArrivalUploadProcessModel>()
            };
            ReadA03(a03, data.ClearanceRows);
            ReadB6F(b6f, data, today);
            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                if (sheet.SheetName != "A03" && sheet.SheetName != "B6F")
                {
                    ReadArrivals(sheet, data.ArrivalRows, today);
                }
            }
            return data;
        }

        /// <summary>
        /// 讀取 A03 表頭後的主號與袋號，合併袋號超過一袋時記錄袋數。
        /// </summary>
        /// <param name="sheet">A03 工作表。</param>
        /// <param name="rows">接收 A03 匯入資料的模型清單。</param>
        private static void ReadA03(ISheet sheet, List<FlightArrivalUploadClearanceModel> rows)
        {
            bool read;
            int bagcount;
            string mainnumber, bl_no, marge_bl_no;

            read = false;
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //主號
                    mainnumber = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //袋號
                    bl_no = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //合併袋號
                    marge_bl_no = sheet.GetRow(i).GetCell(10) == null ? "" : sheet.GetRow(i).GetCell(10).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(3) != null && sheet.GetRow(i).GetCell(3).ToString().Trim() == "主號" && sheet.GetRow(i).GetCell(4) != null && sheet.GetRow(i).GetCell(4).ToString().Trim() == "袋號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && mainnumber != "" && bl_no != "")
                    {
                        bagcount = 0;
                        //合併袋號出現幾次0H4
                        foreach (var item in Regex.Matches(marge_bl_no, "0H4"))
                        {
                            bagcount++;
                        }
                        var model = new FlightArrivalUploadClearanceModel
                        {
                            MainNumber = mainnumber,
                            BlNo = bl_no,
                            Remark = "A03"
                        };
                        if (bagcount > 1)
                        {
                            model.BagCount = bagcount.ToString();
                        }
                        rows.Add(model);
                    }
                }
            }
        }

        /// <summary>
        /// 沿用 B6F 固定欄位讀取原有資料，分提單號必填才依公司名義清關與 W 欄拆袋號碼產生處理紀錄。
        /// </summary>
        /// <param name="sheet">B6F 工作表。</param>
        /// <param name="data">接收 B6F 匯入資料及處理紀錄的解析結果。</param>
        /// <param name="today">供公司名義清關備註使用的上傳日期。</param>
        private static void ReadB6F(ISheet sheet, FlightArrivalUploadData data, DateTime today)
        {
            bool read;
            string datatype, mainnumber, bl_no, dlvInv;
            var processKeys = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

            read = false;
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //倉儲
                    datatype = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //主號
                    mainnumber = sheet.GetRow(i).GetCell(6) == null ? "" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                    //袋號
                    bl_no = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    //分提單號：H 欄，新增處理紀錄必填
                    dlvInv = sheet.GetRow(i).GetCell(7) == null ? "" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(6) != null && sheet.GetRow(i).GetCell(6).ToString().Trim() == "主提單號碼" && sheet.GetRow(i).GetCell(5) != null && sheet.GetRow(i).GetCell(5).ToString().Trim() == "報單號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && mainnumber != "" && bl_no != "")
                    {
                        data.ClearanceRows.Add(new FlightArrivalUploadClearanceModel
                        {
                            DataType = datatype,
                            MainNumber = mainnumber,
                            BlNo = bl_no,
                            Remark = "B6F"
                        });
                    }

                    if (!read || dlvInv == "") continue;

                    //公司名義清出：X 欄
                    var company = sheet.GetRow(i).GetCell(23) == null ? "" : sheet.GetRow(i).GetCell(23).ToString().Trim();
                    //已拆袋新建號碼：W 欄
                    var newBag = sheet.GetRow(i).GetCell(22) == null ? "" : sheet.GetRow(i).GetCell(22).ToString().Trim();
                    var companyClearance = company == "公司名義清關(中匯)" || company == "公司名義清關(捷豐)";
                    var splitBag = newBag.Contains("0H4");
                    if (!companyClearance && !splitBag) continue;
                    if (companyClearance)
                    {
                        //客戶：E 欄
                        var customer = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                        var remarkDate = today.ToString("M/d");
                        if (customer.Contains("菜鳥"))
                        {
                            AddProcess(data, processKeys, dlvInv, 5,
                                $"菜鳥公司名義收單-{remarkDate}");
                        }
                        else
                        {
                            AddProcess(data, processKeys, dlvInv, 3,
                                $"拆出小件貨公司名義收-{remarkDate}");
                        }
                    }
                    if (splitBag)
                    {
                        AddProcess(data, processKeys, dlvInv, 4, "已拆袋，新袋號:" + newBag);
                    }
                }
            }
        }

        /// <summary>
        /// 新增待寫入的處理紀錄，同一份檔案中的相同分提單號與處理類型只保留第一筆。
        /// </summary>
        /// <param name="data">接收處理紀錄的解析結果。</param>
        /// <param name="keys">本次檔案各分提單號已加入的處理類型。</param>
        /// <param name="dlvInv">分提單號。</param>
        /// <param name="type">處理類型。</param>
        /// <param name="remark">處理備註。</param>
        private static void AddProcess(FlightArrivalUploadData data, Dictionary<string, HashSet<int>> keys,
            string dlvInv, int type, string remark)
        {
            if (!keys.TryGetValue(dlvInv, out var processTypes))
            {
                processTypes = new HashSet<int>();
                keys.Add(dlvInv, processTypes);
            }

            if (processTypes.Add(type))
            {
                data.Processes.Add(new FlightArrivalUploadProcessModel
                {
                    DlvInv = dlvInv,
                    ProcessType = type,
                    Remark = remark
                });
            }
        }

        /// <summary>
        /// 讀取班機派件工作表的有效資料列，包含五組到達時間及十組送達時間與袋數。
        /// </summary>
        /// <param name="sheet">班機派件送達工作表。</param>
        /// <param name="rows">接收班機派件送達資料的模型清單。</param>
        /// <param name="today">供匯入月份使用的上傳日期。</param>
        private static void ReadArrivals(ISheet sheet, List<FlightArrivalUploadArrivalModel> rows, DateTime today)
        {
            var reading = false;
            for (var i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;
                var trans = row.GetCellData(1);
                var customer = row.GetCellData(2);
                var main = row.GetCellData(6);
                if (trans == "Lm" && customer == "CC Agent")
                {
                    reading = true;
                    continue;
                }
                if (!reading || trans.Length == 0 || customer.Length == 0 || main.Length == 0) continue;
                rows.Add(new FlightArrivalUploadArrivalModel
                {
                    Month = today.ToString("MM"),
                    CustId = customer,
                    TransNo = trans,
                    MainNumber = main,
                    LineCode = row.GetCellData(7),
                    FlightNumber = row.GetCellData(21),
                    FlightCount = row.GetCellData(22),
                    Ori = row.GetCellData(16),
                    TransitAirport = row.GetCellData(17),
                    Dest = row.GetCellData(18),
                    ArriveDate1 = row.GetCellData(23),
                    ArriveDate2 = row.GetCellData(26),
                    ArriveDate3 = row.GetCellData(29),
                    ArriveDate4 = row.GetCellData(32),
                    ArriveDate5 = row.GetCellData(35),
                    TransDate1 = row.GetCellData(87),
                    TransDate2 = row.GetCellData(89),
                    TransDate3 = row.GetCellData(91),
                    TransDate4 = row.GetCellData(93),
                    TransDate5 = row.GetCellData(95),
                    TransDate6 = row.GetCellData(97),
                    TransDate7 = row.GetCellData(99),
                    TransDate8 = row.GetCellData(101),
                    TransDate9 = row.GetCellData(103),
                    TransDate10 = row.GetCellData(105),
                    TransCount1 = row.GetCellData(88),
                    TransCount2 = row.GetCellData(90),
                    TransCount3 = row.GetCellData(92),
                    TransCount4 = row.GetCellData(94),
                    TransCount5 = row.GetCellData(96),
                    TransCount6 = row.GetCellData(98),
                    TransCount7 = row.GetCellData(100),
                    TransCount8 = row.GetCellData(102),
                    TransCount9 = row.GetCellData(104),
                    TransCount10 = row.GetCellData(106)
                });
            }
        }

        /// <summary>
        /// 檢查所有非空白的班機到達時間與派件送達時間是否可解析為日期。
        /// </summary>
        /// <param name="rows">待檢查的班機派件送達資料。</param>
        /// <returns>最後一個不合法日期的錯誤訊息；全部合法時回傳 null。</returns>
        internal static string ValidateArrivalDates(List<FlightArrivalUploadArrivalModel> rows)
        {
            string error = null;
            foreach (var row in rows)
            {
                var dates = new[]
                {
                    row.ArriveDate1, row.ArriveDate2, row.ArriveDate3, row.ArriveDate4, row.ArriveDate5,
                    row.TransDate1, row.TransDate2, row.TransDate3, row.TransDate4, row.TransDate5,
                    row.TransDate6, row.TransDate7, row.TransDate8, row.TransDate9, row.TransDate10
                };
                for (var i = 1; i <= 15; i++)
                {
                    var arrival = i <= 5;
                    var number = arrival ? i : i - 5;
                    var value = dates[i - 1] ?? string.Empty;
                    DateTime date;
                    if (value.Trim().Length > 0 && !DateTime.TryParse(value, out date))
                    {
                        error = $"{(arrival ? "班機到達時間" : "派件公司送達時間")}{number}：{value.Trim()}錯誤";
                    }
                }
            }
            return error;
        }
    }
}
