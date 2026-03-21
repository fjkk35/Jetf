using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CCLWorkService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public CCLWorkService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 取得空快清關主號明細
        /// </summary>
        /// <returns></returns>
        public DataTable GetOrder_Cargo_Manifest(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT a.*,b.Manifest_CrtDateTime FROM [DATA_CENTER].[dbo].[ORDER_CARGO_MANIFEST] a ");
            sb.Append("left join (select BagNo,max(CrtDateTime) as MANIFEST_CrtDateTime from [DATA_CENTER].[dbo].[ORDER_MANIFEST] group by BagNo) b on a.MasterBagNo=b.BagNo ");
            sb.Append("WHERE a.CrtDateTime between @SDate and @EDate ");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} :00";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} :59";
                da.Fill(dt);
            }

            return dt;
        }


        /// <summary>
        /// 上傳檔案 B6F拆袋資料
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel UploadFileB6F(string filePath, string source, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //讀取Excel B6F拆袋資料
            DataTable dt = new DataTable();
            if (source == "ETL")
            {
                dt = ReadExcelB6F(filePath);
            }
            else if (source == "SEA")
            {
                dt = ReadExcelSeaB6F(filePath);
            }

            //新增
            if (dt.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (source == "ETL")
                {
                    resopnseModel = InsertB6F_Unpacking_Upload(dt, upload_time, userId);
                }
                else if (source == "SEA")
                {
                    resopnseModel = InsertB6F_Sea_Unpacking_Upload(dt, upload_time, userId);
                }
                if (resopnseModel.status == Status.success)
                {
                    resopnseModel.msg = $"上傳檔案筆數：{dt.Rows.Count }";
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = "上傳檔案筆數：0";
            }
            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 讀取Excel B6F拆袋資料(空運)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelB6F(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("NO", typeof(string));
            dt_Data.Columns.Add("DATATYPE", typeof(string));
            dt_Data.Columns.Add("FLIGHTDATE", typeof(string));
            dt_Data.Columns.Add("CUSTOMER", typeof(string));
            dt_Data.Columns.Add("FLIGHTNUMBER", typeof(string));
            dt_Data.Columns.Add("MAINNUMBER", typeof(string));
            dt_Data.Columns.Add("BAGNO", typeof(string));
            dt_Data.Columns.Add("TRACKINGNO", typeof(string));
            dt_Data.Columns.Add("REMARK", typeof(string));

            bool read = false;
            string no,datatype, flightdate, customer, flightnumber, mainnumber, bagno, trackingno, remark;

            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                //if (fileType == ".xls")
                //{
                //    workBook = new HSSFWorkbook(fs);
                //}
                //else
                //{
                workBook = new XSSFWorkbook(fs);
                //}
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //項次
                    no = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //作業地區
                    datatype = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //航班日期
                    if (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(sheet.GetRow(i).GetCell(2)))
                    {
                        flightdate = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).DateCellValue.ToString("yyyyMMdd");
                    }
                    else
                    {
                        flightdate = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    }
                    //申請單位
                    customer = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //航班號
                    flightnumber = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //主號
                    mainnumber = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    //袋號
                    bagno = sheet.GetRow(i).GetCell(6) == null ? "" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                    //申請拆袋分提單號
                    trackingno = sheet.GetRow(i).GetCell(7) == null ? "" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                    //備註
                    remark = sheet.GetRow(i).GetCell(8) == null ? "" : sheet.GetRow(i).GetCell(8).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "項次") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "作業地區") &&
                        (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "航班日期") &&
                        (sheet.GetRow(i).GetCell(3) != null && sheet.GetRow(i).GetCell(3).ToString().Trim() == "申請單位") &&
                        (sheet.GetRow(i).GetCell(4) != null && sheet.GetRow(i).GetCell(4).ToString().Trim() == "航班號") &&
                        (sheet.GetRow(i).GetCell(5) != null && sheet.GetRow(i).GetCell(5).ToString().Trim() == "主號") &&
                        (sheet.GetRow(i).GetCell(6) != null && sheet.GetRow(i).GetCell(6).ToString().Trim() == "袋號") &&
                        (sheet.GetRow(i).GetCell(7) != null && sheet.GetRow(i).GetCell(7).ToString().Trim() == "申請拆袋分提單號"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && (no != "" && datatype !="" && flightdate != "" && customer != "" && flightnumber != "" && mainnumber != "" && bagno != "" && trackingno != "") || (mainnumber != "" && bagno != "" && trackingno != "" && remark == "刪除"))
                    {
                        dr = dt_Data.NewRow();
                        dr["NO"] = no;
                        dr["DATATYPE"] = datatype;
                        dr["FLIGHTDATE"] = flightdate;
                        dr["CUSTOMER"] = customer;
                        dr["FLIGHTNUMBER"] = flightnumber;
                        dr["MAINNUMBER"] = mainnumber;
                        dr["BAGNO"] = bagno;
                        dr["TRACKINGNO"] = trackingno;
                        dr["REMARK"] = remark;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取Excel B6F拆袋資料(海運)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelSeaB6F(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("DATATYPE", typeof(string));
            dt_Data.Columns.Add("MAINNUMBER", typeof(string));
            dt_Data.Columns.Add("TRACKINGNO", typeof(string));
            dt_Data.Columns.Add("PDTMESSAGE", typeof(string));

            bool read = false;
            string dataType, mainnumber, trackingno, pdtMessage;

            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                //if (fileType == ".xls")
                //{
                //    workBook = new HSSFWorkbook(fs);
                //}
                //else
                //{
                workBook = new XSSFWorkbook(fs);
                //}
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //作業地區
                    dataType = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //主號
                    mainnumber = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //分提單號
                    trackingno = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //PDT訊息
                    pdtMessage = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "作業地區") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "主號") &&
                        (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "分提單號") &&
                        (sheet.GetRow(i).GetCell(3) != null && sheet.GetRow(i).GetCell(3).ToString().Trim() == "PDT訊息"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && dataType != "" && trackingno != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["DATATYPE"] = dataType;
                        dr["MAINNUMBER"] = mainnumber;
                        dr["TRACKINGNO"] = trackingno;
                        dr["PDTMESSAGE"] = pdtMessage;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 寫入上傳檔案 B6F拆袋資料(空運)
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel InsertB6F_Unpacking_Upload(DataTable dt_Upload, string upload_Time, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            StringBuilder sb = new StringBuilder();
            sb.Append("select MAINNUMBER from [jetf].[dbo].[B6F_UNPACKING_UPLOAD] ");
            sb.Append("where MAINNUMBER=@MAINNUMBER and BAGNO=@BAGNO and TRACKINGNO=@TRACKINGNO ");
            sb.Append("if @@ROWCOUNT>0 ");
            sb.Append("begin ");
            sb.Append("	    update [jetf].[dbo].[B6F_UNPACKING_UPLOAD] set NO=@NO,DATATYPE=@DATATYPE,FLIGHTDATE=@FLIGHTDATE,CUSTOMER=@CUSTOMER,FLIGHTNUMBER=@FLIGHTNUMBER,REMARK=@REMARK,UPLOAD_TIME=@UPLOAD_TIME,UPLOAD_OPE=@UPLOAD_OPE ");
            sb.Append("	    where MAINNUMBER=@MAINNUMBER and BAGNO=@BAGNO and TRACKINGNO=@TRACKINGNO ");
            sb.Append("end ");
            sb.Append("else ");
            sb.Append("begin ");
            sb.Append("     insert [jetf].[dbo].[B6F_UNPACKING_UPLOAD](NO,DATATYPE, FLIGHTDATE, CUSTOMER, FLIGHTNUMBER, MAINNUMBER, BAGNO, TRACKINGNO, REMARK, UPLOAD_TIME, UPLOAD_OPE) ");
            sb.Append("     values(@NO,@DATATYPE, @FLIGHTDATE, @CUSTOMER, @FLIGHTNUMBER, @MAINNUMBER, @BAGNO, @TRACKINGNO, @REMARK, @UPLOAD_TIME, @UPLOAD_OPE) ");
            sb.Append("end ");

            //刪除
            StringBuilder sbDelete = new StringBuilder();
            sbDelete.Append("insert [jetf].[dbo].[B6F_UNPACKING_UPLOAD_LOG](NO,DATATYPE, FLIGHTDATE, CUSTOMER, FLIGHTNUMBER, MAINNUMBER, BAGNO, TRACKINGNO, REMARK, UPLOAD_OPE, UPLOAD_TIME, SCAN_UPLOAD_OPE, SCAN_UPLOAD_TIME, SCAN_UPLOAD_OPE2, SCAN_UPLOAD_TIME2, CRTDATETIME,DELETE_OPE,DELETE_TIME) ");
            sbDelete.Append("select NO,DATATYPE, FLIGHTDATE, CUSTOMER, FLIGHTNUMBER, MAINNUMBER, BAGNO, TRACKINGNO, REMARK, UPLOAD_OPE, UPLOAD_TIME, SCAN_UPLOAD_OPE, SCAN_UPLOAD_TIME, SCAN_UPLOAD_OPE2, SCAN_UPLOAD_TIME2, CRTDATETIME,@UPLOAD_OPE,@UPLOAD_TIME from [jetf].[dbo].[B6F_UNPACKING_UPLOAD] ");
            sbDelete.Append("where MAINNUMBER=@MAINNUMBER and BAGNO=@BAGNO and TRACKINGNO=@TRACKINGNO ");
            sbDelete.Append("delete from [jetf].[dbo].[B6F_UNPACKING_UPLOAD] where MAINNUMBER=@MAINNUMBER and BAGNO=@BAGNO and TRACKINGNO=@TRACKINGNO ");
            string remark;
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            remark = dt_Upload.Rows[i]["REMARK"].ToString().Trim();
                            cmd.Parameters.Clear();
                            if (remark == "刪除")
                            {
                                cmd.CommandText = sbDelete.ToString();
                                cmd.Parameters.Add("@MAINNUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MAINNUMBER"].ToString();
                                cmd.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["BAGNO"].ToString();
                                cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TRACKINGNO"].ToString();
                                cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                                cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                                cmd.ExecuteNonQuery();
                            }
                            else
                            {
                                cmd.CommandText = sb.ToString();
                                cmd.Parameters.Add("@NO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["NO"].ToString();
                                cmd.Parameters.Add("@DATATYPE", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["DATATYPE"].ToString();
                                cmd.Parameters.Add("@FLIGHTDATE", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["FLIGHTDATE"].ToString();
                                cmd.Parameters.Add("@CUSTOMER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["CUSTOMER"].ToString();
                                cmd.Parameters.Add("@FLIGHTNUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["FLIGHTNUMBER"].ToString();
                                cmd.Parameters.Add("@MAINNUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MAINNUMBER"].ToString();
                                cmd.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["BAGNO"].ToString();
                                cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TRACKINGNO"].ToString();
                                cmd.Parameters.Add("@REMARK", SqlDbType.NVarChar).Value = remark;
                                cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                                cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                    }
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 寫入上傳檔案 B6F拆袋資料(海運)
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel InsertB6F_Sea_Unpacking_Upload(DataTable dt_Upload, string upload_Time, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            StringBuilder sb = new StringBuilder();
            sb.Append("select TRACKINGNO from [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] ");
            sb.Append("where TRACKINGNO=@TRACKINGNO ");
            sb.Append("if @@ROWCOUNT>0 ");
            sb.Append("begin ");
            sb.Append("	    update [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] set DATATYPE=@DATATYPE,MAINNUMBER=@MAINNUMBER,PDTMESSAGE=@PDTMESSAGE,UPLOAD_TIME=@UPLOAD_TIME,UPLOAD_OPE=@UPLOAD_OPE ");
            sb.Append("	    where TRACKINGNO=@TRACKINGNO ");
            sb.Append("end ");
            sb.Append("else ");
            sb.Append("begin ");
            sb.Append("     insert [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD](DATATYPE, MAINNUMBER, TRACKINGNO, PDTMESSAGE, UPLOAD_OPE, UPLOAD_TIME) ");
            sb.Append("     values(@DATATYPE, @MAINNUMBER, @TRACKINGNO, @PDTMESSAGE, @UPLOAD_OPE, @UPLOAD_TIME) ");
            sb.Append("end ");

            //刪除
            StringBuilder sbDelete = new StringBuilder();
            sbDelete.Append("insert [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD_LOG](DATATYPE, MAINNUMBER, TRACKINGNO, PDTMESSAGE, UPLOAD_OPE, UPLOAD_TIME, SCAN_UPLOAD_OPE, SCAN_UPLOAD_TIME, CRTDATETIME,DELETE_OPE,DELETE_TIME) ");
            sbDelete.Append("select DATATYPE, MAINNUMBER, TRACKINGNO, PDTMESSAGE, UPLOAD_OPE, UPLOAD_TIME, SCAN_UPLOAD_OPE, SCAN_UPLOAD_TIME, CRTDATETIME,@UPLOAD_OPE,@UPLOAD_TIME from [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] ");
            sbDelete.Append("where TRACKINGNO=@TRACKINGNO ");
            sbDelete.Append("delete from [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] where TRACKINGNO=@TRACKINGNO ");
            string remark;
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            remark = dt_Upload.Rows[i]["PDTMESSAGE"].ToString().Trim();
                            cmd.Parameters.Clear();
                            if (remark == "刪除")
                            {
                                cmd.CommandText = sbDelete.ToString();
                                cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TRACKINGNO"].ToString();
                                cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                                cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                                cmd.ExecuteNonQuery();
                            }
                            else
                            {
                                cmd.CommandText = sb.ToString();
                                cmd.Parameters.Clear();
                                cmd.Parameters.Add("@DATATYPE", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["DATATYPE"].ToString();
                                cmd.Parameters.Add("@MAINNUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MAINNUMBER"].ToString();
                                cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TRACKINGNO"].ToString();
                                cmd.Parameters.Add("@PDTMESSAGE", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["PDTMESSAGE"].ToString();
                                cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                                cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                    }
                }
            }
            return resopnseModel;
        }


        /// <summary>
        /// 取得B6F已拆袋資料(空運)
        /// </summary>
        /// <returns></returns>
        public DataTable GetB6F_Unpacking_Upload(string sDate, string eDate,string dataType)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[B6F_UNPACKING_UPLOAD] WHERE DATATYPE=@DATATYPE and (SCAN_UPLOAD_TIME2 between @SDate and @EDate or SCAN_UPLOAD_OPE2='') order by [SCAN_UPLOAD_TIME] desc,UPLOAD_TIME,No ", conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate}";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate}";
                da.SelectCommand.Parameters.Add("@DATATYPE", SqlDbType.NVarChar).Value = dataType;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得B6F已拆袋資料(海運)
        /// </summary>
        /// <returns></returns>
        public DataTable GetB6F_Sea_Unpacking_Upload(string sDate, string eDate, string dataType)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] WHERE SCAN_UPLOAD_TIME between @SDate and @EDate and DataType=@DataType or (SCAN_UPLOAD_OPE='' and DataType=@DataType) order by [SCAN_UPLOAD_TIME] desc,UPLOAD_TIME,TRACKINGNO ", conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = dataType;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate}";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate}";
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得拆袋資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetPdtUnpacking(string dataType, string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[PdtUnpacking] WHERE DataType=@DataType and UploadTime between @SDate and @EDate order by UploadTime ", conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = dataType;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得作業地區
        /// </summary>
        /// <returns></returns>
        public DataTable GetPdtDataType()
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[PdtDataType] ", conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得派件公司
        /// </summary>
        /// <returns></returns>
        public DataTable GetPdtTrans()
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[PdtTrans] ", conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt);
            }
            return dt;
        }


        /// <summary>
        /// 取得掃貨上車PDT資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetScanCargoDetailsPdf(string trans, string dataType, string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            if (dataType == "TACT" || dataType == "FTZ")
            {
                sb.Append("select Data, [jetf].[dbo].[GetTRANS_NAME](CLEARANCEWAREHOUSING) as TransName,FIELD_X,CarNo from ( ");
                sb.Append("	    select ROW_NUMBER() OVER (PARTITION BY a.Data ORDER BY a.Data) as ROW_ID,a.Data,isnull(b.CLEARANCEWAREHOUSING,c.CLEARANCEWAREHOUSING) as CLEARANCEWAREHOUSING,isnull(b.FIELD_X,c.FIELD_X) as FIELD_X,CarNo from [jetf].[dbo].[PdtScanCargoUpload] a ");
                sb.Append("     left join DATA_CENTER.[dbo].[ORIGINALLIST] b on a.Data=b.BAGNO ");
                sb.Append("     left join DATA_CENTER.[dbo].[ORIGINALLIST] c on a.Data=c.TRACKINGNO ");
                sb.Append("	    where a.TransNo=@TransNo and a.DataType=@DataType and a.UploadTime between @SDate and @EDate ");
                sb.Append(") a where ROW_ID='1' ");
            }
            else
            {
                sb.Append("select distinct a.Data,b.TRANS_NAME as TransName,'' as FIELD_X,a.CarNo from [jetf].[dbo].[PdtScanCargoUpload] a ");
                sb.Append("left join [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] b on a.Data=b.BL_NO ");
                sb.Append("where a.TransNo=@TransNo and a.DataType=@DataType and a.UploadTime between @SDate and @EDate ");
            }

            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@TransNo", SqlDbType.NVarChar).Value = trans;
                da.SelectCommand.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = dataType;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} :00";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} :59";
                da.Fill(dt);
            }
            //掃貨上車是新竹物流的，派件公司名稱全部換成新竹物流
            if (trans == "1" || trans == "78")
            {
                string transName = "";
                if (trans == "1")
                {
                    transName = "新竹物流";
                }
                else if (trans == "78")
                {
                    transName = "新竹提速";
                }

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i]["TransName"].ToString().Trim() != "")
                    {
                        dt.Rows[i]["TransName"] = transName;
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// 取得拆袋作業差異表
        /// </summary>
        /// <returns></returns>
        public DataTable GetClearanceInfoScanCargoDetails(string dataType, string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("with cte as ");
            sb.Append("(    select MAIN_NUMBER,BAG_NUMBER,MERGE_NUMBER,SIGN_OUT_TIME from [DATA_CENTER].[dbo].[CLEARANCE_INFO] ");
            sb.Append("     where DATA_TYPE=@DataType and SIGN_IN_TIME between @SDate and @EDate ");
            sb.Append(") ");
            sb.Append("select a.MAIN_NUMBER,a.BAG_NUMBER,a.MERGE_NUMBER,a.SIGN_OUT_TIME,isnull(b.Data,c.Data) as Data from cte a ");
            sb.Append("left join (select * from [jetf].[dbo].[PdtScanCargoUpload] where DataType=@DataType and UploadTime between @SDate and @EDate) b on a.BAG_NUMBER =b.data ");
            sb.Append("left join (select * from [jetf].[dbo].[PdtScanCargoUpload] where DataType=@DataType and UploadTime between @SDate and @EDate) c on a.MERGE_NUMBER =c.data ");
            sb.Append("group by a.MAIN_NUMBER,a.BAG_NUMBER,a.MERGE_NUMBER,a.SIGN_OUT_TIME,b.Data,c.Data  ");

            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = dataType;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} :00";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} :59";
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得處置說明-拆袋作業差異表使用
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetProcess(List<string> list)
        {
            if(list.Count == 0)
                return new Dictionary<string, string>();

            var sb = new StringBuilder();

            var sql = @"
                        declare @TrackingNoTable Table
                        ( 
	                          TrackingNo nvarchar(100)
                        )

                        {0}

                        with Process as 
                        (
	                        select DLV_INV,MIN(PROCESS_TYPE) as ProcessType from jetf.dbo.Process
	                        where PROCESS_TYPE in (3,4) and DEL = 0
	                        group by DLV_INV
                        )
                        select a.TrackingNo,b.ProcessType from @TrackingNoTable a
                        join Process b on a.TrackingNo = b.DLV_INV
                        ";

            foreach (var item in list.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @TrackingNoTable VALUES {string.Join(",",
                item.Select(r => $"('{r}')"))};");
            }

            sql = string.Format(sql,sb.ToString());

            return conn.Query(sql)
                .ToDictionary(
                r => (string)r.TrackingNo,
                r => (string)r.ProcessType);
        }

        /// <summary>
        /// 取得拆袋作業差異表-客戶名稱
        /// </summary>
        /// <returns></returns>
        public DataTable GetClearanceInfoScanCargoCustomer(string dataType, string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("with cte as ");
            sb.Append("(    select  MAIN_NUMBER from [DATA_CENTER].[dbo].[CLEARANCE_INFO] ");
            sb.Append("     where DATA_TYPE=@DATA_TYPE and SIGN_IN_TIME between @SDate and @EDate ");
            sb.Append("     group by  MAIN_NUMBER ");
            sb.Append(") ");
            sb.Append("select a.MAIN_NUMBER,c.DESPATCHNAME from cte a ");
            sb.Append("join [DATA_CENTER].[dbo].[MAINORDERINFO] b on a.MAIN_NUMBER=b.MAINNUMBER ");
            sb.Append("join [DATA_CENTER].[dbo].[DESPATCHFROM] c on b.DELIVERYFROM=c.DESPATCHNO ");
            sb.Append("group by a.MAIN_NUMBER,c.DESPATCHNAME ");

            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@DATA_TYPE", SqlDbType.NVarChar).Value = dataType;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} :00";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} :59";
                da.Fill(dt);
            }
            return dt;
        }

    }
}
