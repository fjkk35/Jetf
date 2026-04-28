using Dapper;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Models.EtlCustWorkLoad;
using Service.Services.EtlCustomerWorkLoadReport.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.EtlCustomerWorkLoadReport
{
    public class EtlCustomerWorkLoadReportService : _BaseService
    {
        #region 公開方法

        /// <summary>
        /// 取得客戶群組列表
        /// </summary>
        public List<object> GetCustomerGroupList()
        {
            try
            {
                string sql = @"
                    SELECT [Id], [GroupName]
                    FROM [jetf].[dbo].[CustomerGroup]
                    ORDER BY [GroupName]";

                var result = conn.Query<dynamic>(sql).Select(r => (object)new
                {
                    r.Id,
                    r.GroupName
                }).ToList();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"取得客戶群組列表失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得所有客戶群組明細 (一次撈完)
        /// </summary>
        public Dictionary<string, List<string>> GetAllCustomerGroupDetails()
        {
            try
            {
                string sql = @"
                    SELECT [CustomerGroupId], [Cust_Code]
                    FROM [jetf].[dbo].[CustomerGroupDetail]";

                var result = conn.Query<dynamic>(sql)
                    .GroupBy(x => (int)x.CustomerGroupId)
                    .ToDictionary(
                        g => g.Key.ToString(),
                        g => g.Select(x => (string)x.Cust_Code).ToList()
                    );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"取得所有客戶群組明細失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 產生空快客戶作業量報表 Excel (多客戶)
        /// </summary>
        public IWorkbook GetCustWorkLoadReportWorkbookMultiple(List<string> custIds, string custTypeId, string sDate, string eDate, List<string> mainNumbers)
        {
            IWorkbook workbook = new XSSFWorkbook();

            // 查詢客戶代碼對應表
            string sql = @"
                SELECT Cust_Code, OLD_CODE, Cust_Name 
                FROM DATA_CENTER.dbo.SYS_CUST
                WHERE CUST_TYPE='AIR'";

            var custMapping = conn.Query<dynamic>(sql).ToList();

            // 轉換客戶代碼並建立查詢列表
            var actualCustIds = new List<string>();
            var custNameMapping = new Dictionary<string, string>();

            foreach (var custId in custIds)
            {
                var custInfo = custMapping.FirstOrDefault(c => c.Cust_Code == custId);
                if (custInfo != null)
                {
                    string actualCustId = !string.IsNullOrEmpty(custInfo.OLD_CODE) ? custInfo.OLD_CODE : custId;
                    actualCustIds.Add(actualCustId);
                    custNameMapping[actualCustId] = custInfo.Cust_Name;
                }
            }

            // 一次查詢所有客戶的明細資料
            List<CustWorkLoadDetailModel> allDetails = GetCustWorkLoadDetails(actualCustIds, sDate, eDate, mainNumbers);

            // 將客戶名稱和代碼填入明細資料
            foreach (var detail in allDetails)
            {
                if (custNameMapping.ContainsKey(detail.CustCode))
                {
                    detail.CustName = custNameMapping[detail.CustCode];
                }
            }

            // 取得班次到達時間
            DataTable dt_Arrive = GetArriveList();

            // 建立頁簽並初始化表頭
            ISheet reportSheet = null;
            ISheet detailSheet = null;
            int reportRowCount = 3;
            int detailRowCount = 1;

            // 根據客戶格式建立頁簽
            if (custTypeId == "1")
            {
                reportSheet = CreateCustWorkLoadReportSheetHeader(workbook, "空快客戶作業量報表", sDate, eDate);
            }
            else if (custTypeId == "2")
            {
                reportSheet = CreateCustWorkLoadReport2SheetHeader(workbook, "空快客戶作業量報表", sDate, eDate);
            }

            // 建立袋號明細頁簽
            detailSheet = CreateCustWorkLoadDetailsSheetHeader(workbook, "袋號明細");

            // 按客戶分組處理
            var groupedDetails = allDetails.GroupBy(d => d.CustCode).OrderBy(g => g.Key);

            foreach (var custGroup in groupedDetails)
            {
                var details = custGroup.ToList();

                var blno = details.Where(r => r.BlNo == "0H4PZ7HM").ToList();

                // 根據客戶格式產生不同的報表
                if (custTypeId == "1")
                {
                    var reportRows = CalculateCustWorkLoadReportData(details, dt_Arrive);
                    WriteDataToCustWorkLoadReportSheet(reportSheet, reportRows, ref reportRowCount);
                }
                else if (custTypeId == "2")
                {
                    var reportRows = CalculateCustWorkLoadReport2Data(details, dt_Arrive, sDate, eDate);
                    WriteDataToCustWorkLoadReport2Sheet(reportSheet, reportRows, ref reportRowCount);
                }

                // 空快客戶作業量報表-袋號明細
                var detailRows = CalculateCustWorkLoadDetailsData(details);
                WriteDataToCustWorkLoadDetailsSheet(detailSheet, detailRows, ref detailRowCount);
            }

            return workbook;
        }

        #endregion

        #region 資料查詢方法

        /// <summary>
        /// 取得客戶作業量明細資料
        /// </summary>
        public List<CustWorkLoadDetailModel> GetCustWorkLoadDetails(List<string> custIds, string sDate, string eDate, List<string> mainNumbers)
        {
            DateTime maxTime = Convert.ToDateTime($"{eDate} 12:59:59");

            var custIdTable = new DataTable();
            custIdTable.Columns.Add("CustId", typeof(string));
            custIds.ForEach(custId =>
            {
                custIdTable.Rows.Add(custId);
            });

            var mainNumberTable = new DataTable();
            mainNumberTable.Columns.Add("MainNumber", typeof(string));
            mainNumbers?.ForEach(mainNumber =>
            {
                mainNumberTable.Rows.Add(mainNumber);
            });

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(
                   "[jetf].[dbo].[SP_Select_CustWorkLoadDetails_New_V4]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.CommandTimeout = 1200;

                //客戶
                var tvpParam = da.SelectCommand.Parameters.Add(
                    "@CustIds", SqlDbType.Structured);
                tvpParam.TypeName = "dbo.CustIdList";
                tvpParam.Value = custIdTable;

                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.DateTime)
                    .Value = DateTime.Parse($"{sDate} 13:00:00");

                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.DateTime)
                    .Value = DateTime.Parse($"{eDate} 12:59:59");

                //主號
                var tvpParam2 = da.SelectCommand.Parameters.Add(
                "@MainNumbers", SqlDbType.Structured);
                tvpParam2.TypeName = "dbo.MainNumberList";
                tvpParam2.Value = mainNumberTable;

                da.Fill(dt);
            }

            DataTable dt_Upload = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select distinct MAINNUMBER,BL_NO,REMARK,BAGCOUNT from A03_B6F_UPLOAD", conn))
            {
                da.Fill(dt_Upload);
            }

            var result = new List<CustWorkLoadDetailModel>();
            foreach (DataRow row in dt.Rows)
            {
                var model = new CustWorkLoadDetailModel
                {
                    CustCode = row["CUST_CODE"]?.ToString(),
                    TransNo = row["TRANS_NO"] != DBNull.Value ? (int?)Convert.ToInt32(row["TRANS_NO"]) : null,
                    TransName = row["TRANS_NAME"].ToString(),
                    Mainnumber = row["MAINNUMBER"].ToString(),
                    BlNo = row["BL_NO"].ToString(),
                    LineCode = row["LINE_CODE"].ToString(),
                    CargoPiece = row["I_CARGO_PIECE"] != DBNull.Value ? (int?)Convert.ToInt32(row["I_CARGO_PIECE"]) : null,
                    CargoWeight = row["I_CARGO_WEIGHT"] != DBNull.Value ? (double?)Convert.ToDouble(row["I_CARGO_WEIGHT"]) : null,
                    ArrivalTime = row.Table.Columns.Contains("ArrivalTime") ? row["ArrivalTime"]?.ToString() : null
                };

                if (string.IsNullOrEmpty(model.TransName))
                {
                    model.TransName = "無派件公司";
                }

                if (DateTime.TryParse(row["I_SIGN_IN_TIME"].ToString(), out DateTime signInTime))
                {
                    model.SignInTime = signInTime > maxTime ? null : (DateTime?)signInTime;
                }

                if (DateTime.TryParse(row["I_SIGN_OUT_TIME"].ToString(), out DateTime signOutTime))
                {
                    if (signOutTime > maxTime)
                    {
                        model.SignOutTime = null;
                    }
                    else
                    {
                        model.SignOutTime = signOutTime;
                        model.SignOutDate = signOutTime.AddHours(-13).ToString("yyyyMMdd");
                    }
                }

                string mainnumber = model.Mainnumber;
                string bl_no = model.BlNo;

                if (!model.SignInTime.HasValue && !model.SignOutTime.HasValue)
                {
                    DataRow[] dr = dt_Upload.Select($"MAINNUMBER='{mainnumber}' and BL_NO='{bl_no}'", "REMARK");
                    if (dr.Length > 0)
                    {
                        model.FormatBlNo = !string.IsNullOrEmpty(dr[0]["BAGCOUNT"].ToString())
                            ? $"{dr[0]["BL_NO"]}*{dr[0]["BAGCOUNT"]}"
                            : dr[0]["BL_NO"].ToString();
                        model.Remark = dr[0]["REMARK"].ToString();
                    }
                    else
                    {
                        model.Remark = "未見";
                    }
                }

                if (model.SignInTime.HasValue && !model.SignOutTime.HasValue)
                {
                    DataRow[] dr = dt_Upload.Select($"MAINNUMBER='{mainnumber}' and BL_NO='{bl_no}'", "REMARK");
                    if (dr.Length > 0)
                    {
                        model.FormatBlNo = !string.IsNullOrEmpty(dr[0]["BAGCOUNT"].ToString())
                            ? $"{dr[0]["BL_NO"]}*{dr[0]["BAGCOUNT"]}"
                            : dr[0]["BL_NO"].ToString();
                        model.Remark = dr[0]["REMARK"].ToString();
                    }
                    else
                    {
                        model.Remark = "C3";
                    }
                }

                result.Add(model);
            }

            return result.OrderBy(x => x.CustCode).ThenBy(x => x.Mainnumber).ThenBy(x => x.TransName).ToList();
        }

        /// <summary>
        /// 取得班次到達時間
        /// </summary>
        private DataTable GetArriveList()
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from [jetf].[dbo].[ARRIVE_UPLOAD]", conn))
            {
                da.Fill(dt);
            }
            return dt;
        }

        #endregion

        #region 資料計算方法 - 博豐格式

        /// <summary>
        /// 計算空快客戶作業量報表(博豐格式)資料
        /// </summary>
        private List<CustWorkLoadReportRowModel> CalculateCustWorkLoadReportData(
            List<CustWorkLoadDetailModel> details,
            DataTable dt_Arrive)
        {
            var result = new List<CustWorkLoadReportRowModel>();
            string custName = details.FirstOrDefault()?.CustName ?? "";

            var groups = details
                .GroupBy(t => new { t.TransNo, t.TransName, t.Mainnumber })
                .OrderBy(g => g.Key.Mainnumber)
                .ThenBy(g => g.Key.TransName);

            foreach (var group in groups)
            {
                var groupDetails = details.Where(t =>
                    t.Mainnumber == group.Key.Mainnumber &&
                    t.TransName == group.Key.TransName).ToList();

                var rowModel = new CustWorkLoadReportRowModel
                {
                    TransNo = group.Key.TransNo,
                    TransName = group.Key.TransName,
                    CustName = custName,
                    Mainnumber = group.Key.Mainnumber,
                    TotalBlNo = groupDetails.Select(t => t.BlNo).Distinct().Count(),
                    TotalInBlNo = groupDetails.Where(t => t.SignInTime.HasValue).Select(t => t.BlNo).Distinct().Count(),
                    TotalOutBlNo = groupDetails.Where(t => t.SignOutTime.HasValue).Select(t => t.BlNo).Distinct().Count(),
                    TotalPiece = groupDetails.Sum(t => t.CargoPiece ?? 0),
                    TotalGW = groupDetails.Sum(t => t.CargoWeight ?? 0)
                };

                var a03BlNoList = groupDetails.Where(t => t.Remark == "A03").Select(t => t.FormatBlNo ?? t.BlNo).Distinct().ToList();
                var b6fBlNoList = groupDetails.Where(t => t.Remark == "B6F").Select(t => t.BlNo).Distinct().ToList();
                var c3BlNoList = groupDetails.Where(t => t.Remark == "C3").Select(t => t.BlNo).Distinct().ToList();
                var noBlNoList = groupDetails.Where(t => t.Remark == "未見").Select(t => t.BlNo).Distinct().ToList();

                rowModel.TotalC3BlNo = c3BlNoList.Count;
                rowModel.TotalNoBlNo = noBlNoList.Count;
                rowModel.TotalA03BlNo = a03BlNoList.Count;
                rowModel.TotalB6FBlNo = b6fBlNoList.Count;

                var errorParts = new List<string>();
                if (c3BlNoList.Count > 0) errorParts.Add($"C3：{string.Join(",", c3BlNoList)}");
                if (noBlNoList.Count > 0) errorParts.Add($"未見：{string.Join(",", noBlNoList)}");
                if (a03BlNoList.Count > 0) errorParts.Add($"A03：{string.Join(",", a03BlNoList)}");
                if (b6fBlNoList.Count > 0) errorParts.Add($"B6F：{string.Join(",", b6fBlNoList)}");
                rowModel.ErrorBlNo = string.Join(" ", errorParts);

                rowModel.ArriveInfo = GetArriveInfoForReport1(dt_Arrive, group.Key.TransNo ?? 0, group.Key.Mainnumber);
                rowModel.SignOutDateList = GetSignOutDateList(groupDetails, group.Key.TransName, group.Key.Mainnumber);

                result.Add(rowModel);
            }

            return result;
        }

        /// <summary>
        /// 取得班次到達資訊 (博豐格式)
        /// </summary>
        private CustWorkLoadArriveInfo GetArriveInfoForReport1(DataTable dt_Arrive, int transNo, string mainnumber)
        {
            var rows = dt_Arrive.AsEnumerable()
                .Where(r => r.Field<string>("TRANS_NO") == transNo.ToString() &&
                           r.Field<string>("MAINNUMBER") == mainnumber)
                .OrderByDescending(r => r.Field<DateTime>("UPDATE_TIME"))
                .ToList();

            if (!rows.Any())
                return null;

            var row = rows.First();
            return new CustWorkLoadArriveInfo
            {
                Ori = row.Field<string>("ORI"),
                TransitAirport = row.Field<string>("TRANSIT_AIRPORT"),
                Dest = row.Field<string>("DEST"),
                FlightNumber = row.Field<string>("FLIGHTNUMBER"),
                FlightCount = row.Field<int?>("FLIGHT_COUNT"),
                ArriveDate1 = row.Field<DateTime?>("ARRIVE_DATE1"),
                ArriveDate2 = row.Field<DateTime?>("ARRIVE_DATE2"),
                ArriveDate3 = row.Field<DateTime?>("ARRIVE_DATE3"),
                ArriveDate4 = row.Field<DateTime?>("ARRIVE_DATE4"),
                ArriveDate5 = row.Field<DateTime?>("ARRIVE_DATE5"),
                TransDate1 = row.Field<DateTime?>("TRANS_DATE1"),
                TransDate2 = row.Field<DateTime?>("TRANS_DATE2"),
                TransDate3 = row.Field<DateTime?>("TRANS_DATE3"),
                TransDate4 = row.Field<DateTime?>("TRANS_DATE4"),
                TransDate5 = row.Field<DateTime?>("TRANS_DATE5"),
                TransDate6 = row.Field<DateTime?>("TRANS_DATE6"),
                TransDate7 = row.Field<DateTime?>("TRANS_DATE7"),
                TransDate8 = row.Field<DateTime?>("TRANS_DATE8"),
                TransDate9 = row.Field<DateTime?>("TRANS_DATE9"),
                TransDate10 = row.Field<DateTime?>("TRANS_DATE10"),
                TransCount1 = row.Field<int?>("TRANS_COUNT1"),
                TransCount2 = row.Field<int?>("TRANS_COUNT2"),
                TransCount3 = row.Field<int?>("TRANS_COUNT3"),
                TransCount4 = row.Field<int?>("TRANS_COUNT4"),
                TransCount5 = row.Field<int?>("TRANS_COUNT5"),
                TransCount6 = row.Field<int?>("TRANS_COUNT6"),
                TransCount7 = row.Field<int?>("TRANS_COUNT7"),
                TransCount8 = row.Field<int?>("TRANS_COUNT8"),
                TransCount9 = row.Field<int?>("TRANS_COUNT9"),
                TransCount10 = row.Field<int?>("TRANS_COUNT10")
            };
        }

        /// <summary>
        /// 取得出倉日期列表
        /// </summary>
        private List<SignOutTimeModel> GetSignOutDateList(List<CustWorkLoadDetailModel> details, string transName, string mainnumber)
        {
            return details
                    .Where(t => t.TransName == transName &&
                               t.Mainnumber == mainnumber &&
                               t.SignOutTime.HasValue)
                    .GroupBy(t => new
                    {
                        t.TransName,
                        t.Mainnumber,
                        TimeRange = t.TransName == "SPX"
                            ? (t.SignOutTime.Value.Hour >= 9 && t.SignOutTime.Value.Hour < 13 ? 1
                                : t.SignOutTime.Value.Hour >= 13 && t.SignOutTime.Value.Hour < 18 ? 2 : 3)
                            : (t.SignOutTime.Value.Hour >= 9 && t.SignOutTime.Value.Hour < 13 ? 1 : 2)
                    })
                    .Select(g => new SignOutTimeModel
                    {
                        SignOutTime = g.Min(t => t.SignOutTime.Value),
                        ArrivalTime = g.Where(t => !string.IsNullOrEmpty(t.ArrivalTime))
                                       .Min(m => m.ArrivalTime),
                        TotalBlNo = g.Select(t => t.BlNo).Distinct().Count()
                    })
                    .OrderBy(x => x.SignOutTime)
                    .ToList();
        }

        #endregion

        #region 資料計算方法 - 蝦皮格式

        /// <summary>
        /// 計算空快客戶作業量報表(蝦皮格式)資料
        /// </summary>
        private List<CustWorkLoadReport2RowModel> CalculateCustWorkLoadReport2Data(
            List<CustWorkLoadDetailModel> details,
            DataTable dt_Arrive,
            string sDate,
            string eDate)
        {
            DateTime startDate = Convert.ToDateTime($"{sDate} 13:00:00");
            DateTime endDate = Convert.ToDateTime($"{eDate} 12:59:59");
            var result = new List<CustWorkLoadReport2RowModel>();

            //找出最早袋號出艙時間，相同袋號可能會有兩次出艙
            //只需要計算最早的一次
            var mainnumbers = details
                .GroupBy(r => new { r.Mainnumber, r.BlNo })
                .Where(r => r.Min(x => x.SignOutTime) >= startDate && r.Min(x => x.SignOutTime) <= endDate)
                .Select(r => r.Key.Mainnumber)
                .Distinct()
                .ToList();

            // 依派件公司+主提單號+渠道代碼分組
            var groups = details
                .Where(t => mainnumbers.Contains(t.Mainnumber))
                .GroupBy(t => new { t.TransNo, t.TransName, t.Mainnumber, t.LineCode, t.BlNo })
                .Select(g => new { g.Key.TransNo, g.Key.TransName, g.Key.Mainnumber, g.Key.LineCode, CustName = g.First().CustName })
                .Distinct()
                .OrderBy(g => g.Mainnumber)
                .ThenBy(g => g.TransName);

            foreach (var item in groups)
            {
                var rowModel = new CustWorkLoadReport2RowModel
                {
                    TransNo = item.TransNo,
                    TransName = item.TransName,
                    CustName = item.CustName,
                    Mainnumber = item.Mainnumber,
                    LineCode = item.LineCode
                };

                // 計算各類袋數
                var itemDetails = details.Where(t =>
                    t.Mainnumber == item.Mainnumber &&
                    t.TransName == item.TransName &&
                    t.LineCode == item.LineCode).ToList();

                rowModel.TotalBlNo = itemDetails.Select(t => t.BlNo).Distinct().Count();
                // 取得班次到達資訊
                rowModel.ArriveInfo = GetArriveInfo(dt_Arrive, item.TransNo ?? 0, item.Mainnumber, item.LineCode);
                // 取得出倉時間列表
                rowModel.SignOutTimeList = GetSignOutTimeList(details, item.TransName, item.Mainnumber, item.LineCode);
                // 取得交倉時間列表
                //rowModel.ArrivalTimeList = GetArrivalTimeList(details, item.TransName, item.Mainnumber, item.LineCode);

                result.Add(rowModel);
            }

            //排序
            result = result.OrderBy(r => r.CustName)
                .ThenBy(r => r.Mainnumber)
                .ThenBy(r => r.TransName)
                .ThenBy(r => r.LineCode)
                .ToList();

            return result;
        }

        /// <summary>
        /// 取得班次到達資訊 (蝦皮格式)
        /// </summary>
        private CustWorkLoadArriveInfo GetArriveInfo(DataTable dt_Arrive, int transNo, string mainnumber, string lineCode)
        {
            return dt_Arrive.AsEnumerable()
                .Where(r => r.Field<string>("TRANS_NO") == transNo.ToString() &&
                           r.Field<string>("MAINNUMBER") == mainnumber.ToString() &&
                           (r.Field<string>("LINE_CODE") == lineCode.ToString() || string.IsNullOrEmpty(r.Field<string>("lINE_CODE"))))
                .OrderByDescending(r => r.Field<DateTime>("UPDATE_TIME"))
                .Select(r => new CustWorkLoadArriveInfo
                {
                    FlightNumber = r.Field<string>("FLIGHTNUMBER"),
                    ArriveDate1 = r.Field<DateTime?>("ARRIVE_DATE1"),
                    TransDate1 = r.Field<DateTime?>("TRANS_DATE1"),
                    TransDate2 = r.Field<DateTime?>("TRANS_DATE2"),
                    TransDate3 = r.Field<DateTime?>("TRANS_DATE3"),
                    TransDate4 = r.Field<DateTime?>("TRANS_DATE4"),
                    TransDate5 = r.Field<DateTime?>("TRANS_DATE5"),
                    TransDate6 = r.Field<DateTime?>("TRANS_DATE6"),
                    TransDate7 = r.Field<DateTime?>("TRANS_DATE7"),
                    TransDate8 = r.Field<DateTime?>("TRANS_DATE8"),
                    TransDate9 = r.Field<DateTime?>("TRANS_DATE9"),
                    TransDate10 = r.Field<DateTime?>("TRANS_DATE10"),
                    TransCount1 = r.Field<int?>("TRANS_COUNT1"),
                    TransCount2 = r.Field<int?>("TRANS_COUNT2"),
                    TransCount3 = r.Field<int?>("TRANS_COUNT3"),
                    TransCount4 = r.Field<int?>("TRANS_COUNT4"),
                    TransCount5 = r.Field<int?>("TRANS_COUNT5"),
                    TransCount6 = r.Field<int?>("TRANS_COUNT6"),
                    TransCount7 = r.Field<int?>("TRANS_COUNT7"),
                    TransCount8 = r.Field<int?>("TRANS_COUNT8"),
                    TransCount9 = r.Field<int?>("TRANS_COUNT9"),
                    TransCount10 = r.Field<int?>("TRANS_COUNT10"),
                })
                .FirstOrDefault();
        }

        /// <summary>
        /// 取得出艙時間區間
        /// </summary>
        private List<SignOutTimeModel> GetSignOutTimeList(List<CustWorkLoadDetailModel> details, string transName, string mainnumber, string lineCode)
        {
            // 袋號重覆的Group，取第一次的出倉時間
            var signOutTimeGroup = details
                .Where(t => t.TransName == transName &&
                            t.Mainnumber == mainnumber &&
                            t.LineCode == lineCode &&
                            t.SignOutTime.HasValue)
                .GroupBy(t => new { t.TransName, t.Mainnumber, t.BlNo })
                .Select(g => new
                {
                    TransName = g.Key.TransName,
                    Mainnumber = g.Key.Mainnumber,
                    BlNo = g.Key.BlNo,
                    SignOutTime = g.Min(m => m.SignOutTime.Value),
                    ArrivalTime = g.Where(m => !string.IsNullOrEmpty(m.ArrivalTime)).Min(m => m.ArrivalTime)
                })
                .ToList();
            // 出倉時間明細
            // SPX   第一個時段 9~13 第二個時段 13~18 第三個時段 18~隔天9
            // HL/FM 第一個時段 9~13 第二個時段 13~08
            var signOutTimeDetail = signOutTimeGroup.Select(t => new
            {
                DataDate = t.SignOutTime.AddHours(-9).ToString("yyyyMMdd"),
                t.TransName,
                t.Mainnumber,
                t.BlNo,
                t.SignOutTime,
                t.ArrivalTime,
                TimeRange = t.TransName == "SPX"
                    ? (t.SignOutTime.Hour >= 9 && t.SignOutTime.Hour < 13 ? 1
                        : t.SignOutTime.Hour >= 13 && t.SignOutTime.Hour < 18 ? 2 : 3)
                    : (t.SignOutTime.Hour >= 9 && t.SignOutTime.Hour < 13 ? 1 : 2)
            }).ToList();

            // 出倉時間結果
            return signOutTimeDetail
                .GroupBy(r => new { r.DataDate, r.TimeRange, r.TransName, r.Mainnumber })
                .Select(r => new SignOutTimeModel
                {
                    Mainnumber = r.Key.Mainnumber,
                    TransName = r.Key.TransName,
                    SignOutTime = r.Min(m => m.SignOutTime),
                    ArrivalTime = r.Where(m => !string.IsNullOrEmpty(m.ArrivalTime)).Min(m => m.ArrivalTime),
                    TotalBlNo = r.Select(m => m.BlNo).Distinct().Count()
                })
                .OrderBy(r => r.SignOutTime)
                .ToList();
        }

        /// <summary>
        /// 取得交倉時間區間
        /// </summary>
        private List<DateTime> GetArrivalTimeList(List<CustWorkLoadDetailModel> details, string transName, string mainnumber, string lineCode)
        {
            // 交倉時間
            var arrivalTimeData = details
                .Where(r => r.TransName == transName &&
                            r.Mainnumber == mainnumber &&
                            r.LineCode == lineCode &&
                            !string.IsNullOrEmpty(r.ArrivalTime))
                .Select(r => new
                {
                    r.TransName,
                    ArrivalTime = Convert.ToDateTime(r.ArrivalTime)
                })
                .Distinct()
                .ToList();

            // 出倉時間明細
            // SPX   第一個時段 9~12 第二個時段 12~21 第三個時段 21~隔天9
            // HL/FM 第一個時段 9~15 第二個時段 16~08
            var arrivalTimeDataDetail = arrivalTimeData.Select(r => new
            {
                DataDate = r.ArrivalTime.AddHours(-9).ToString("yyyyMMdd"),
                r.ArrivalTime,
                TimeRange = r.TransName == "SPX"
                    ? (r.ArrivalTime.Hour >= 9 && r.ArrivalTime.Hour < 12 ? 1
                        : r.ArrivalTime.Hour >= 12 && r.ArrivalTime.Hour < 21 ? 2 : 3)
                    : (r.ArrivalTime.Hour >= 9 && r.ArrivalTime.Hour < 16 ? 1 : 2)
            }).ToList();

            return arrivalTimeDataDetail
                .GroupBy(r => new { r.DataDate, r.TimeRange })
                .Select(r => r.Min(it => it.ArrivalTime))
                .OrderBy(r => r)
                .ToList();
        }

        #endregion

        #region 資料計算方法 - 袋號明細

        /// <summary>
        /// 計算袋號明細頁簽資料
        /// </summary>
        private List<CustWorkLoadDetailsSheetRowModel> CalculateCustWorkLoadDetailsData(
            List<CustWorkLoadDetailModel> details)
        {
            return details
                .GroupBy(t => new
                {
                    t.CustName,
                    t.TransName,
                    t.Mainnumber,
                    t.BlNo,
                    t.SignInTime,
                    t.SignOutTime,
                    t.Remark,
                    t.LineCode,
                    t.ArrivalTime
                })
                .Select(g => new CustWorkLoadDetailsSheetRowModel
                {
                    CustName = g.Key.CustName,
                    Mainnumber = g.Key.Mainnumber,
                    BlNo = g.Key.BlNo,
                    TransName = g.Key.TransName,
                    ClearanceType = "",
                    SignInTime = g.Key.SignInTime,
                    SignOutTime = g.Key.SignOutTime,
                    ArrivalTime = g.Key.ArrivalTime,
                    Remark = g.Key.Remark,
                    LineCode = g.Key.LineCode
                })
                .ToList();
        }

        #endregion

        #region Excel 頁簽建立方法

        /// <summary>
        /// 建立空快客戶作業量報表(博豐)頁簽表頭
        /// </summary>
        private ISheet CreateCustWorkLoadReportSheetHeader(IWorkbook workbook, string sheetName, string sDate, string eDate)
        {
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 10));
            row.CreateCell(0).SetCellValue($"{sDate} 13:00-{eDate}12:59{sheetName}");

            row = sheet.CreateRow(2);
            string[] headers = {
                "Fl", "Lm", "CC Agent", "EC/SC", "P/T", "Pu date", "Mawbs", "Service code",
                "原單袋數", "原單件數", "GW", "Ctns", "Pcs", "GW", "VW(RCL)", "Chargeble Weight",
                "Ori.", "Transit Airport", "Dest.", "ETA", "ATA",
                "Batch A", "CTNS", "ATA", "Batch B", "CTNS", "ATA", "Batch C", "CTNS", "ATA",
                "Batch D", "CTNS", "ATA", "Batch E", "CTNS", "ATA", "Batch F", "CTNS", "ATA",
                "Batch G", "CTNS", "ATA", "Batch B", "CTNS", "ATA", "Batch C", "CTNS", "ATA",
                "Batch D", "CTNS", "ATA", "Batch A", "CTNS", "ATA", "Batch B", "CTNS", "ATA",
                "Batch C", "CTNS", "ATA", "Batch D", "CTNS", "ATA",
                "1st release date", "1st releas", "2nd release date", "2nd releas", "3rd release date", "3rd release ctns",
                "4th release date", "4th releas", "5th release date", "5th relea", "6th release date", "6th releas",
                "7th release date", "7th release", "8th release date", "8th release", "9th release date", "9th release ctns",
                "10th release date", "10th release ctns", "10th release date", "10th release ctns", "11th release date", "11th release ctns",
                "1st DLV", "1st CTNS", "2nd DLV", "2nd CTNS", "3rd DLV", "3rd CTNS",
                "4th DLV", "4th CTNS", "5th DLV", "5th CTNS", "6th DLV", "6th CTNS",
                "7th DLV", "7th CTNS", "8th DLV", "8th CTNS", "9th DLV", "9th CTNS",
                "10th DLV", "10th CTNS", "10th DLV", "10th CTNS", "10th DLV", "10th CTNS",
                "C3", "未見", "B6D/A03", "B6E", "異常", "總件數", "落地", "清出", "交倉", "問題"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                row.CreateCell(i).SetCellValue(headers[i]);
            }

            int[] wideColumns = { 23, 26, 29, 32, 35, 38, 41, 44, 47, 50, 53, 56, 59, 62,
                                  63, 65, 67, 69, 71, 73, 75, 77, 79, 81,
                                  87, 89, 91, 93, 95, 97, 99, 101, 103, 105, 107, 109 };
            foreach (int col in wideColumns)
            {
                sheet.SetColumnWidth(col, 5000);
            }

            return sheet;
        }

        /// <summary>
        /// 建立空快客戶作業量報表(蝦皮)頁簽表頭
        /// </summary>
        private ISheet CreateCustWorkLoadReport2SheetHeader(IWorkbook workbook, string sheetName, string sDate, string eDate)
        {
            DateTime startDate = Convert.ToDateTime($"{sDate} 13:00");
            DateTime endDate = Convert.ToDateTime($"{eDate} 12:59");

            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 10));
            row.CreateCell(0).SetCellValue($"{startDate:yyyy-MM-dd HH:mm}-{endDate:yyyy-MM-dd HH:mm}{sheetName}");

            row = sheet.CreateRow(2);
            string[] headers = {
                "头程", "尾程", "清关行", "提单号", "渠道代码", "渠道箱数", "提单箱数", "提单重量",
                "第一批航班号", "箱数", "航班抵达时间", "第二批航班号", "箱数", "航班抵达时间",
                "Batch C", "CTNS", "ATA", "Batch D", "CTNS", "ATA",
                "第一批清关完成时间", "箱数", "第二批清关完成时间", "箱数", "第三批清关完成时间", "箱数",
                "第四批清关完成时间", "箱数", "第五批清關完成时间", "箱數", "第六批清關完成時間", "箱數",
                "第七批清關完成時間", "箱數", "第八批清關完成時間", "箱數", "第九批清關完成時間", "箱數",
                "第十批清關完成時間", "箱數", "第十一批清關完成時間", "箱數", "第十二批清關完成時間", "箱數",
                "第一批开始装车时间", "箱数", "第二批开始装车时间", "箱数", "第三批开始装车时间", "箱数",
                "第四批开始装车时间", "箱数", "第五批开始装车时间", "箱数", "第六批开始装车时间", "箱数",
                "第七批开始装车时间", "箱数", "第八批开始装车时间", "箱数", "第九批开始装车时间", "箱数",
                "第十批开始装车时间", "箱数",
                "第一批抵达尾程时间", "箱数", "第二批抵达尾程时间", "箱数", "第三批抵达尾程时间", "箱數",
                "第四批抵達尾程時間", "箱數", "第五批抵達尾程時間", "箱數", "第六批抵達尾程時間", "箱數",
                "第七批抵達尾程時間", "箱數", "第八批抵達尾程時間", "箱數", "第九批抵達尾程時間", "箱數",
                "第十批抵達尾程時間", "箱數",
                "第一批交倉完成時間", "箱数", "第二批交倉完成時間", "箱数", "第三批交倉完成時間", "箱数",
                "第四批交倉完成時間", "箱数", "第五批交倉完成時間", "箱数", "第六批交倉完成時間", "箱数",
                "第七批交倉完成時間", "箱数", "第八批交倉完成時間", "箱数", "第九批交倉完成時間", "箱数",
                "第十批交倉完成時間", "箱数", "第十一批交倉完成時間", "箱数", "第十二批交倉完成時間", "箱数"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                row.CreateCell(i).SetCellValue(headers[i]);
            }

            sheet.SetColumnWidth(0, 4000);
            sheet.SetColumnWidth(1, 4000);
            sheet.SetColumnWidth(2, 4000);
            sheet.SetColumnWidth(3, 5000);
            for (int i = 4; i < 132; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            return sheet;
        }

        /// <summary>
        /// 袋號明細頁簽表頭
        /// </summary>
        private ISheet CreateCustWorkLoadDetailsSheetHeader(IWorkbook workbook, string sheetName)
        {
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);

            string[] headers = { "客戶", "主提單號", "袋號", "派件公司", "通關方式", "進倉時間", "出倉時間", "異常類別", "渠道代码","交倉時間" };
            int[] widths = { 3500, 3500, 5000, 4500, 4500, 5500, 5500, 5500, 5500,5500 };

            for (int i = 0; i < headers.Length; i++)
            {
                row.CreateCell(i).SetCellValue(headers[i]);
                sheet.SetColumnWidth(i, widths[i]);
            }

            return sheet;
        }

        #endregion

        #region Excel 資料寫入方法

        /// <summary>
        /// 寫入資料到空快客戶作業量報表(博豐)頁簽
        /// </summary>
        void WriteDataToCustWorkLoadReportSheet(ISheet sheet, List<CustWorkLoadReportRowModel> rows, ref int rowCount)
        {
            foreach (var item in rows)
            {
                IRow row = sheet.CreateRow(rowCount);

                row.CreateCell(1).SetCellValue(item.TransName);
                row.CreateCell(2).SetCellValue(item.CustName);
                row.CreateCell(6).SetCellValue(item.Mainnumber);
                row.CreateCell(8).SetCellValue(item.TotalBlNo);
                row.CreateCell(9).SetCellValue(item.TotalPiece);
                row.CreateCell(10).SetCellValue(Math.Ceiling(item.TotalGW));
                row.CreateCell(22).SetCellValue(item.TotalBlNo);
                row.CreateCell(111).SetCellValue(item.TotalC3BlNo);
                row.CreateCell(112).SetCellValue(item.TotalNoBlNo);
                row.CreateCell(113).SetCellValue(item.TotalA03BlNo);
                row.CreateCell(114).SetCellValue(item.TotalB6FBlNo);
                row.CreateCell(115).SetCellValue(item.ErrorBlNo);
                row.CreateCell(116).SetCellValue(item.TotalBlNo);
                row.CreateCell(117).SetCellValue(item.TotalInBlNo);
                row.CreateCell(118).SetCellValue(item.TotalOutBlNo);

                if (item.ArriveInfo != null)
                {
                    row.CreateCell(16).SetCellValue(item.ArriveInfo.Ori ?? "");
                    row.CreateCell(17).SetCellValue(item.ArriveInfo.TransitAirport ?? "");
                    row.CreateCell(18).SetCellValue(item.ArriveInfo.Dest ?? "");
                    row.CreateCell(21).SetCellValue(item.ArriveInfo.FlightNumber ?? "");
                    row.CreateCell(23).SetCellValue(FormatDateTime(item.ArriveInfo.ArriveDate1));
                    row.CreateCell(26).SetCellValue(FormatDateTime(item.ArriveInfo.ArriveDate2));
                    row.CreateCell(29).SetCellValue(FormatDateTime(item.ArriveInfo.ArriveDate3));
                    row.CreateCell(32).SetCellValue(FormatDateTime(item.ArriveInfo.ArriveDate4));
                    row.CreateCell(35).SetCellValue(FormatDateTime(item.ArriveInfo.ArriveDate5));
                }

                if (item.SignOutDateList != null)
                {
                    int colCount = 0;
                    foreach (var signOutDate in item.SignOutDateList)
                    {
                        if (63 + colCount <= 81)
                        {
                            row.CreateCell(63 + colCount).SetCellValue(signOutDate.SignOutTime.ToString("yyyy/M/d HH:mm"));
                            row.CreateCell(64 + colCount).SetCellValue(signOutDate.TotalBlNo);
                        }
                        colCount += 2;
                    }

                    //交倉時間
                    for (int i = 0; i < Math.Min(10, item.SignOutDateList.Count); i++)
                    {
                        int timeColumn = 87 + (i * 2);
                        int countColumn = 88 + (i * 2);

                        var signOutDate = item.SignOutDateList[i];
                        bool hasArrivalTime = !string.IsNullOrEmpty(signOutDate.ArrivalTime);

                        var formattedLabel = GetBatchTimeText(signOutDate, item.TransName, 0);
                        row.CreateCell(timeColumn).SetCellValue(formattedLabel);
                        row.CreateCell(countColumn).SetCellValue(signOutDate.TotalBlNo.ToString());
                    }
                }

              

                row.CreateCell(120).SetCellValue(item.TotalC3BlNo + item.TotalNoBlNo + item.TotalA03BlNo + item.TotalB6FBlNo);

                rowCount++;
            }
        }

        /// <summary>
        /// 寫入資料到空快客戶作業量報表(蝦皮)頁簽
        /// </summary>
        private void WriteDataToCustWorkLoadReport2Sheet(ISheet sheet, List<CustWorkLoadReport2RowModel> rows, ref int rowCount)
        {
            foreach (var item in rows)
            {
                IRow row = sheet.CreateRow(rowCount);

                // 基本資訊
                row.CreateCell(0).SetCellValue(item.CustName);
                row.CreateCell(1).SetCellValue(item.TransName);
                row.CreateCell(3).SetCellValue(item.Mainnumber);
                row.CreateCell(4).SetCellValue(item.LineCode);
                row.CreateCell(5).SetCellValue(item.TotalBlNo);
                row.CreateCell(6).SetCellValue("" );
                //箱数
                row.CreateCell(9).SetCellValue(item.TotalBlNo);
                // 班次到達資訊
                if (item.ArriveInfo != null)
                {
                    row.CreateCell(8).SetCellValue(item.ArriveInfo.FlightNumber);
                    row.CreateCell(10).SetCellValue(FormatDateTime(item.ArriveInfo.ArriveDate1));
                }

                if (item.SignOutTimeList != null)
                {
                    int colCount = 0;
                    // 清关完成时间
                    foreach (var signOut in item.SignOutTimeList)
                    {
                        if (20 + colCount <= 43)
                        {
                            row.CreateCell(20 + colCount).SetCellValue(signOut.SignOutTime.ToString("yyyy/M/d HH:mm"));
                            row.CreateCell(21 + colCount).SetCellValue(signOut.TotalBlNo);
                        }
                        colCount += 2;
                    }

                    colCount = 0;
                    //开始装车时间-清關完成時間+0.5小時
                    foreach (var signOut in item.SignOutTimeList)
                    {
                        if (44 + colCount <= 63)
                        {
                            row.CreateCell(44 + colCount).SetCellValue(signOut.SignOutTime.AddMinutes(+30).ToString("yyyy/M/d HH:mm"));
                            row.CreateCell(45 + colCount).SetCellValue(signOut.TotalBlNo);
                        }
                        colCount += 2;
                    }
                }

                // 尾程時間 - 交倉完成時間-1小時 (第64欄開始)
                WriteBatchDataWithFallback(
                    row,
                    item.SignOutTimeList,
                    item.TransName,
                    64,
                    10,
                    -1
                );

                // 交倉完成時間 (第84欄開始)
                WriteBatchDataWithFallback(
                    row,
                    item.SignOutTimeList,
                    item.TransName,
                    84,
                    12,
                    0
                );

                rowCount++;
            }
        }

        /// <summary>
        /// 寫入批次資料（優先使用交倉時間，否則使用出倉時間）
        /// </summary>
        /// <param name="row">Excel 列</param>
        /// <param name="signOutTimeList">出倉時間列表</param>
        /// <param name="transName">派件公司名稱</param>
        /// <param name="startCol">起始欄位</param>
        /// <param name="maxBatchCount">最大批次數</param>
        /// <param name="hoursOffset">時間偏移量（小時）</param>
        private void WriteBatchDataWithFallback(
            IRow row,
            List<SignOutTimeModel> signOutTimeList,
            string transName,
            int startCol,
            int maxBatchCount,
            int hoursOffset)
        {
            int batchCount = Math.Min(signOutTimeList?.Count ?? 0, maxBatchCount);

            for (int i = 0; i < batchCount; i++)
            {
                int offset = i * 2;
                int timeCol = startCol + offset;
                int qtyCol = timeCol + 1;

                string timeText = GetBatchTimeText(
                    signOutTimeList[i],
                    transName,
                    hoursOffset
                );

                row.CreateCell(timeCol).SetCellValue(timeText);
                row.CreateCell(qtyCol).SetCellValue(signOutTimeList[i].TotalBlNo);
            }
        }

        /// <summary>
        /// 取得批次時間文字（優先使用交倉時間，否則使用出倉時間標籤）
        /// </summary>
        /// <param name="signOutTime">出倉時間</param>
        /// <param name="arrivalTimeList">交倉時間列表</param>
        /// <param name="index">批次索引</param>
        /// <param name="transName">派件公司名稱</param>
        /// <param name="hoursOffset">時間偏移量（小時）</param>
        /// <returns>格式化的時間文字</returns>
        private string GetBatchTimeText(
            SignOutTimeModel signOutTime,
            string transName,
            int hoursOffset)
        {
            bool hasArrivalTime = !string.IsNullOrEmpty(signOutTime.ArrivalTime);

            if (hasArrivalTime)
            {
                var adjustedTime = signOutTime.ArrivalTime.ToDateTime("yyyy-MM-dd HH:mm:ss")?.AddHours(hoursOffset);
                return adjustedTime?.ToString("yyyy/MM/dd HH:mm");
            }
            else
            {
                string label = FormatWarehouseTimeLabel(transName, signOutTime.SignOutTime);
                return hoursOffset < 0 ? $"尾程{label}" : label;
            }
        }

        /// <summary>
        /// 寫入資料到袋號明細頁簽
        /// </summary>
        private void WriteDataToCustWorkLoadDetailsSheet(ISheet sheet, List<CustWorkLoadDetailsSheetRowModel> rows, ref int rowCount)
        {
            foreach (var item in rows)
            {
                IRow row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(item.CustName);
                row.CreateCell(1).SetCellValue(item.Mainnumber);
                row.CreateCell(2).SetCellValue(item.BlNo);
                row.CreateCell(3).SetCellValue(item.TransName);
                row.CreateCell(4).SetCellValue(item.ClearanceType);

                if (item.SignInTime.HasValue)
                {
                    row.CreateCell(5).SetCellValue(item.SignInTime.Value.ToString("yyyy/M/d HH:mm"));
                }

                if (item.SignOutTime.HasValue)
                {
                    row.CreateCell(6).SetCellValue(item.SignOutTime.Value.ToString("yyyy/M/d HH:mm"));
                }

                row.CreateCell(7).SetCellValue(item.Remark);
                row.CreateCell(8).SetCellValue(item.LineCode);
                row.CreateCell(9).SetCellValue(item.ArrivalTime);
                rowCount++;
            }
        }

        /// <summary>
        /// 格式化日期時間為字串
        /// </summary>
        private string FormatDateTime(DateTime? dateTime)
        {
            return dateTime.HasValue ? dateTime.Value.ToString("yyyy/M/d HH:mm") : "";
        }

        /// <summary>
        /// 格式化交倉時間標籤
        /// SPX: 18:00-08:59=PM, 09:00-12:59=AM, 13:00-17:59=MM
        /// FM: 18:00-08:59=PM, 09:00-12:59=AM, 13:00-17:59=MM
        /// </summary>
        private string FormatWarehouseTimeLabel(string transName, DateTime dateTime)
        {
            var map = new Dictionary<string, string>
                {
                    { "SPX", "SPX" },
                    { "7-11", "7-11" },
                    { "萊爾富", "HL" },
                    { "黑貓", "黑貓" },
                    { "FM", "FM" }
                };

            foreach (var item in map)
            {
                if (transName.Contains(item.Key))
                {
                    transName = item.Value;
                    break;
                }
            }

            int hour = dateTime.Hour;
            string timePeriod;
            DateTime labelDate = dateTime; // 預設用當天

            if (hour >= 18 && hour <= 23)
            {
                // 18:00–23:59 → +1天 PM
                timePeriod = "PM";
                labelDate = dateTime.AddDays(+1);
            }
            else if (hour >= 0 && hour < 9)
            {
                // 00:00–08:59 → 當天 PM
                timePeriod = "PM";

            }
            else if (hour >= 9 && hour < 13)
            {
                // 09:00–12:59 → 當天 AM
                timePeriod = "AM";
            }
            else
            {
                // 13:00–17:59 → 當天 MM
                timePeriod = "MM";
            }

            string dateStr = labelDate.ToString("MM/dd");
            return $"{transName}-{dateStr} {timePeriod}";
        }

        #endregion
    }
}
