using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using Service.Models.ExportClearance;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Service.Services
{
    public class ExportClearanceService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public ExportClearanceService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 上傳檔案主號航班
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel UploadExportFlight(string filePath, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;

            try
            {
                //讀取Excel 主號航班資料
                List<ExportFlightDetailModel> exportFlightDetailList = ReadExcelExportFlight(filePath);

                //讀取Excel 出口清關資料
                List<ExportClearanceInfoModel> excelExportClearanceInfoList = new List<ExportClearanceInfoModel>();

                for (int i = 1; i < GetSheetCount(filePath); i++)
                {
                    excelExportClearanceInfoList.AddRange(ReadExcelExportClearanceInfo(i, filePath));
                }

                // 使用 TransactionScope 包裹資料庫操作
                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Required, new TimeSpan(0, 10, 0)))
                {
                    //寫入資料
                    string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    //新增主號航班資料
                    InsertExportFlightDetail(exportFlightDetailList, upload_time, userId);

                    //新增 出口清關資料
                    InsertExportClearanceInfo(excelExportClearanceInfoList, upload_time, userId);

                    //新增貨況回覆
                    InsertExportCargo(upload_time, userId);

                    //確認寫入
                    transactionScope.Complete();
                    conn.Close();
                }

                StringBuilder sb = new StringBuilder();
                sb.Append($"上傳檔案筆數：<br>");
                sb.Append($"主號航班：{exportFlightDetailList.Count}筆<br>");
                sb.Append($"出入倉時間：{excelExportClearanceInfoList.Count}筆<br>");

                resopnseModel.msg = sb.ToString();
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        /// <summary>
        /// 讀取Excel 主號航班
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<ExportFlightDetailModel> ReadExcelExportFlight(string filePath)
        {
            List<ExportFlightDetailModel> list = new List<ExportFlightDetailModel>();

            ExportFlightDetailModel item;
            bool read = false;

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }

            var sheet = workbook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                item = new ExportFlightDetailModel();
                if (sheet.GetRow(i) != null)
                {
                    item.MawbNo = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    item.FltDate = sheet.GetRow(i).GetCell(1) == null ? "" : FormatDateCellValue(sheet.GetRow(i).GetCell(1));
                    item.FltNo = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    item.DepartureTime = sheet.GetRow(i).GetCell(3) == null ? "" : $"{item.FltDate} {FormatTimeCellValue(sheet.GetRow(i).GetCell(3))}";
                    item.ArrivalTime = sheet.GetRow(i).GetCell(4) == null ? "" : $"{item.FltDate} {FormatTimeCellValue(sheet.GetRow(i).GetCell(4))}";

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主號") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "航班日期") &&
                        (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "航班代號") &&
                        (sheet.GetRow(i).GetCell(3) != null && sheet.GetRow(i).GetCell(3).ToString().Trim() == "起飛時間") &&
                        (sheet.GetRow(i).GetCell(4) != null && sheet.GetRow(i).GetCell(4).ToString().Trim() == "降落時間"))
                    {
                        read = true;
                        continue;
                    }
                    if (read)
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 讀取Excel 出口清關資料
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<ExportClearanceInfoModel> ReadExcelExportClearanceInfo(int index, string filePath)
        {
            List<ExportClearanceInfoModel> list = new List<ExportClearanceInfoModel>();

            ExportClearanceInfoModel item;
            string mawbNo = "";

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }

            var sheet = workbook.GetSheetAt(index);
            if (sheet.GetRow(0) != null)
            {
                mawbNo = sheet.GetRow(0).GetCell(0) == null ? "" : sheet.GetRow(0).GetCell(0).ToString().Replace("主提單號：", "");
            }

            for (int i = 3; i < sheet.LastRowNum + 1; i++)
            {
                item = new ExportClearanceInfoModel();
                if (sheet.GetRow(i) != null)
                {
                    item.MawbNo = mawbNo;
                    item.CustHawbNo = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString();
                    item.ClearanceType = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString();
                    item.MergeNumber = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString();
                    item.ClearanceNumber = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString();
                    item.ClearanceModel = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString();
                    item.DeclaredPiece = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString();
                    item.InboundPiece = sheet.GetRow(i).GetCell(6) == null ? "" : sheet.GetRow(i).GetCell(6).ToString();
                    item.OutboundPiece = sheet.GetRow(i).GetCell(7) == null ? "" : sheet.GetRow(i).GetCell(7).ToString();
                    item.DeclaredWeight = sheet.GetRow(i).GetCell(8) == null ? "" : sheet.GetRow(i).GetCell(8).ToString();
                    item.InboundWeight = sheet.GetRow(i).GetCell(9) == null ? "" : sheet.GetRow(i).GetCell(9).ToString();
                    item.SignInTime = sheet.GetRow(i).GetCell(10) == null ? "" : FormatDateTimeCellValue(sheet.GetRow(i).GetCell(10)); ;
                    item.SignOutTime = sheet.GetRow(i).GetCell(11) == null ? "" : FormatDateTimeCellValue(sheet.GetRow(i).GetCell(11)); ;
                    item.FltNo = sheet.GetRow(i).GetCell(12) == null ? "" : sheet.GetRow(i).GetCell(12).ToString();
                    item.AmendClearanceNumber = sheet.GetRow(i).GetCell(13) == null ? "" : sheet.GetRow(i).GetCell(13).ToString();
                    item.Tax = sheet.GetRow(i).GetCell(14) == null ? "" : sheet.GetRow(i).GetCell(14).ToString();

                    //加入list
                    list.Add(item);
                }
            }
            return list;
        }

        /// <summary>
        /// 新增主號航班資料
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public void InsertExportFlightDetail(List<ExportFlightDetailModel> list, string upload_Time, string userId)
        {
            string sql = $@"
                              insert [jetf].[dbo].[EXPORT_FLIGHT_DETAIL]([MAWB_NO],[FLT_NO],[FLT_DATE],[DEPARTURE_TIME],[ARRIVAL_TIME],[UPDATE_TIME],[UPLOAD_OPE])
                              values(@MAWB_NO,@FLT_NO,@FLT_DATE,@DEPARTURE_TIME,@ARRIVAL_TIME,@UPDATE_TIME,@UPLOAD_OPE)
                         ";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                list.ForEach(r =>
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add("@MAWB_NO", SqlDbType.NVarChar).Value = r.MawbNo;
                    cmd.Parameters.Add("@FLT_NO", SqlDbType.NVarChar).Value = r.FltNo;
                    cmd.Parameters.Add("@FLT_DATE", SqlDbType.NVarChar).Value = r.FltDate;
                    cmd.Parameters.Add("@DEPARTURE_TIME", SqlDbType.NVarChar).Value = r.DepartureTime;
                    cmd.Parameters.Add("@ARRIVAL_TIME", SqlDbType.NVarChar).Value = r.ArrivalTime;
                    cmd.Parameters.Add("@UPDATE_TIME", SqlDbType.NVarChar).Value = upload_Time;
                    cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                    cmd.ExecuteNonQuery();
                });
            }
        }

        /// <summary>
        /// 新增出口清關資料
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public void InsertExportClearanceInfo(List<ExportClearanceInfoModel> list, string upload_Time, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();

            string sql = $@"
                              insert jetf.dbo.EXPORT_CLEARANCE_INFO(MAWB_NO,CUST_HAWB_NO,CLEARANCE_TYPE,MERGE_NUMBER,CLEARANCE_NUMBER,CLEARANCE_MODEL,DECLARED_PIECE,INBOUND_PIECE,OUTBOUND_PIECE,DECLARED_WEIGHT,INBOUND_WEIGHT,SIGN_IN_TIME,SIGN_OUT_TIME,FLT_NO,AMEND_CLEARANCE_NUMBER,TAX,UPDATE_TIME,UPLOAD_OPE)
                              values(@MAWB_NO,@CUST_HAWB_NO,@CLEARANCE_TYPE,@MERGE_NUMBER,@CLEARANCE_NUMBER,@CLEARANCE_MODEL,@DECLARED_PIECE,@INBOUND_PIECE,@OUTBOUND_PIECE,@DECLARED_WEIGHT,@INBOUND_WEIGHT,@SIGN_IN_TIME,@SIGN_OUT_TIME,@FLT_NO,@AMEND_CLEARANCE_NUMBER,@TAX,@UPDATE_TIME,@UPLOAD_OPE)
                         ";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                list.ForEach(r =>
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add("@MAWB_NO", SqlDbType.NVarChar).Value = r.MawbNo;
                    cmd.Parameters.Add("@CUST_HAWB_NO", SqlDbType.NVarChar).Value = r.CustHawbNo;
                    cmd.Parameters.Add("@CLEARANCE_TYPE", SqlDbType.NVarChar).Value = r.ClearanceType;
                    cmd.Parameters.Add("@MERGE_NUMBER", SqlDbType.NVarChar).Value = r.MergeNumber;
                    cmd.Parameters.Add("@CLEARANCE_NUMBER", SqlDbType.NVarChar).Value = r.ClearanceNumber;
                    cmd.Parameters.Add("@CLEARANCE_MODEL", SqlDbType.NVarChar).Value = r.ClearanceModel;
                    cmd.Parameters.Add("@DECLARED_PIECE", SqlDbType.Int).Value = r.DeclaredPiece;
                    cmd.Parameters.Add("@INBOUND_PIECE", SqlDbType.Int).Value = r.InboundPiece;
                    cmd.Parameters.Add("@OUTBOUND_PIECE", SqlDbType.Int).Value = r.OutboundPiece;
                    cmd.Parameters.Add("@DECLARED_WEIGHT", SqlDbType.Decimal).Value = r.DeclaredWeight;
                    cmd.Parameters.Add("@INBOUND_WEIGHT", SqlDbType.Decimal).Value = r.InboundWeight;
                    cmd.Parameters.Add("@SIGN_IN_TIME", SqlDbType.DateTime).Value = r.SignInTime;
                    cmd.Parameters.Add("@SIGN_OUT_TIME", SqlDbType.DateTime).Value = r.SignOutTime;
                    cmd.Parameters.Add("@FLT_NO", SqlDbType.NVarChar).Value = r.FltNo;
                    cmd.Parameters.Add("@AMEND_CLEARANCE_NUMBER", SqlDbType.NVarChar).Value = r.AmendClearanceNumber;
                    cmd.Parameters.Add("@TAX", SqlDbType.Decimal).Value = r.Tax;
                    cmd.Parameters.Add("@UPDATE_TIME", SqlDbType.NVarChar).Value = upload_Time;
                    cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                    cmd.ExecuteNonQuery();
                });
            }
        }

        /// <summary>
        /// 新增貨況回覆
        /// </summary>
        public void InsertExportCargo(string upload_Time, string userId) 
        {
            using (SqlCommand cmd = new SqlCommand("jetf.[dbo].[USP_Insert_Export_Cargo]", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UPDATE_TIME",SqlDbType.NVarChar).Value = upload_Time;
                cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                cmd.ExecuteNonQuery();
            }
        }

        public string FormatDateCellValue(ICell cell)
        {
            if (cell != null)
            {
                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue.ToString("yyyy-MM-dd");
                }
                else
                {
                    return cell.ToString().Trim();
                }
            }
            return string.Empty;
        }

        public string FormatTimeCellValue(ICell cell)
        {
            if (cell != null)
            {
                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue.ToString("HH:mm:ss");
                }
                else
                {
                    return cell.ToString().Trim();
                }
            }
            return string.Empty;
        }

        public string FormatDateTimeCellValue(ICell cell)
        {
            if (cell != null)
            {
                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    return cell.ToString().Trim();
                }
            }
            return string.Empty;
        }

        public int GetSheetCount(string filePath)
        {
            IWorkbook workbook;
            int sheetCount = 0;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
                fs.Close();
            }
            sheetCount = workbook.NumberOfSheets;
            workbook.Close();
            return sheetCount;
        }


    }
}
