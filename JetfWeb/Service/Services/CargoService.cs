using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
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
    public class CargoService
    {
        private readonly SqlConnection conn;
        private readonly GlobalService _globalService;
        /// <summary>
        /// 建構式
        /// </summary>
        public CargoService(GlobalService globalService)
        {
            _globalService = globalService;
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 取得貨況
        /// </summary>
        /// <returns></returns>
        public DataTable GetFee_Master(string invoice)
        {
            DateTime date;
            StringBuilder sb = new StringBuilder();
            sb.Append("select DATADATE,SOURCE,TYPE,isnull(b.CUST_NAME,a.CUSTOMER) as CUST_NAME,IN_DATETIME,OUT_DATETIME,MAIN_NUMBER,TRACKINGNO as 'BAG_NUMBER',DLV_INV,TAX_NUMBER,DLV_COM as 'TRANS_NAME',RECIPIENT,RECPHONE,RECADDRESS,a.INCLUDE_TAX,CCFEE,a.FEE,a.COD,a.TAX1,a.TAX2,TO_DLV_COD from [jetf].[dbo].[FEE_MASTER] a ");
            sb.Append("left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
            sb.Append("where [SOURCE_TYPE] in('1','2') and (DLV_INV=@DLV_INV or TRACKINGNO=@DLV_INV) ");
            sb.Append("union all ");
            sb.Append("select DATADATE,SOURCE,TYPE,b.CUSTOMER,IN_DATETIME,OUT_DATETIME,MAIN_NUMBER,BAG_NUMBER,DLV_INV,TAX_NUMBER,b.TRANS_NAME,RECIPIENT,RECPHONE,RECADDRESS,a.INCLUDE_TAX,CCFEE,a.FEE,a.COD,a.TAX1,a.TAX2,TO_DLV_COD from [jetf].[dbo].[FEE_MASTER] a ");
            sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO and b.TRAN_TYPE='空運' ");
            sb.Append("where[SOURCE_TYPE]='3' and (DLV_INV=@DLV_INV or BAG_NUMBER=@DLV_INV) ");

            DataTable dt = new DataTable();
            if (invoice != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = invoice;
                    da.Fill(dt);
                }

                //if (dt.Rows.Count == 0)
                //{
                //    dt = GetSource_Fee_Master(invoice);
                //}
            }

            dt.Columns.Add("Format_OUT_DATETIME", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (DateTime.TryParse(dt.Rows[i]["OUT_DATETIME"].ToString(), out date))
                {
                    dt.Rows[i]["Format_OUT_DATETIME"] = date.ToString("yyyy-MM-dd");
                }
                dt.Rows[i]["INCLUDE_TAX"] = _globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString());
            }

            return dt;
        }

        /// <summary>
        /// 取得物流配送狀態
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        public DataTable GetCargo_Status_Detail(string trans_number)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from [DATA_CENTER].[dbo].[CARGO_STATUS_DETAIL] (nolock) where TRANS_NUMBER=@TRANS_NUMBER order by TRANS_MODIFY_TIME desc  ");
            DataTable dt = new DataTable();
            if (trans_number != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@TRANS_NUMBER", SqlDbType.NVarChar).Value = trans_number;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 新增貨況查詢記錄
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel InsertLog_Cargo_Status(LogCargoStatusModel model)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[LOG_CARGO_STATUS]([DLV_INV],[SEARCH_TIME],[REMARK],[USER_ID],[USER_IP]) ");
            sb.Append("values(@DLV_INV,@SEARCH_TIME,@REMARK,@USER_ID,@USER_IP) ");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
            {
                try
                {
                    conn.Open();
                    cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = model.Dlv_Inv;
                    cmd.Parameters.Add("@SEARCH_TIME", SqlDbType.NVarChar).Value = model.Search_Time;
                    cmd.Parameters.Add("@REMARK", SqlDbType.NVarChar).Value = model.Remark;
                    cmd.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = model.User_Id;
                    cmd.Parameters.Add("@USER_IP", SqlDbType.NVarChar).Value = model.User_Ip;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                }
                finally
                {
                    conn.Close();
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 取得貨況查詢記錄
        /// </summary>
        /// <param name="dlv_inv"></param>
        /// <returns></returns>
        public DataTable GetLog_Cargo_Status(string dlv_inv)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select  * from  [jetf].[dbo].[LOG_CARGO_STATUS] where DLV_INV=@DLV_INV and REMARK='貨況查詢' ");
            DataTable dt = new DataTable();
            if (dlv_inv != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 新增批量貨況查詢明細表
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel InsertBatchSearchCargo2(DataTable dt_Upload, string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";

            DateTime date = DateTime.Now;
            string dataDate = date.ToString("yyyyMMdd");
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[BatchSearchCargo2](TrackingNo, Upload_Time, Upload_Ope) ");
            sb.Append("values(@TrackingNo, @Upload_Time, @Upload_Ope) ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TrackingNo"].ToString();
                            cmd.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                            cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                        //取消寫入
                        tran.Rollback();
                    }
                }
            }

            return resopnseModel;
        }

        /// <summary>
        /// 取得批量貨況查詢明細表
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public DataTableModel GetBatchSearchCargo2(string upload_time, string user_Id)
        {
            DataTable dt = new DataTable();
            DataTableModel dataTableModel = new DataTableModel();
            dataTableModel.status = Status.success;
            dataTableModel.msg = "成功";
            try
            {
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_GetBatchSearchCargo]", conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                    da.SelectCommand.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                    da.Fill(dt);
                }
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }

            return dataTableModel;
        }


        /// <summary>
        /// 處置說明下載
        /// </summary>
        /// <param name="sdate"></param>
        /// <param name="edate"></param>
        /// <returns></returns>
        public DataTableModel ProcessReport(string custId, string sdate, string edate, string processType,string finish)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                var sql = @"
                            with cte_Process as 
                            (
	                            SELECT  ROW_NUMBER() OVER (PARTITION BY a.ID ORDER BY a.ID ) as ROW_ID,a.DATADATE,b.ETA,b.DESPATCH_NAME as CUSD_ID,b.CUSTOMER,b.MAINNUMBER,b.BL_NO,a.DLV_INV,b.I_DATA_TYPE,b.I_SIGN_IN_TIME,b.I_SIGN_OUT_TIME,b.IMPORTER,b.IM_PHONENO,a.REMARK,c.[USER_NAME],a.[CRTDATETIME],a.DEL,a.FINISH,a.FINISH_USER_ID,d.[USER_NAME] as FINISH_USER_NAME,a.FINISH_DATETIME,a.PROCESS_TYPE FROM [jetf].[dbo].[Process] a (nolock) 
	                            left join [jetf].[dbo].MERGE_ORIGINALLIST b (nolock) on a.DLV_INV=b.JETF_SERIAL 
	                            left join [jetf].[dbo].[USER_MASTER] c on a.[USER_ID]=c.[USER_ID] 
	                            left join [jetf].[dbo].[USER_MASTER] d on a.[FINISH_USER_ID]=d.[USER_ID] 
	                            where DEL='0' and DATADATE between @sdate and @edate
                            )
                            select * from cte_Process
                            where ROW_ID='1' 
                            AND (@custId = 'All' OR CUSD_ID = @custId)
                            AND (@PROCESS_TYPE = 'All' OR PROCESS_TYPE = @PROCESS_TYPE)
                            AND (@FINISH = 'All' OR FINISH = @FINISH);
                          ";

            

                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate;
                    da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate;
                    da.SelectCommand.Parameters.Add("@custId", SqlDbType.NVarChar).Value = custId;
                    da.SelectCommand.Parameters.Add("@PROCESS_TYPE", SqlDbType.NVarChar).Value = processType;
                    da.SelectCommand.Parameters.Add("@FINISH", SqlDbType.NVarChar).Value = finish;
                    da.Fill(dt);
                }

                //排序
                DataView dv = dt.DefaultView;
                dv.Sort = "DATADATE,CRTDATETIME";
                dt = dv.ToTable();

                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
        }

        /// <summary>
        /// 批量貨況查詢明細表上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel BatchSearchCargo2(string filePath, string fileName, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                //讀取檔案
                DataTable dt_Upload = ReadExcelBatchSearchCargo2(filePath);

                //新增
                if (dt_Upload.Rows.Count > 0)
                {
                    //寫入資料
                    string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    resopnseModel = InsertBatchSearchCargo2(dt_Upload, upload_time, userId);

                    if (resopnseModel.status == Status.success)
                    {
                        //resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                        resopnseModel.msg = $"{upload_time}︿{userId}";
                    }
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                }
            }
            finally
            {
                conn.Close();
            }
            return resopnseModel;
        }

        /// <summary>
        /// 讀取批量貨況查詢明細表上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelBatchSearchCargo2(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("TrackingNo", typeof(string));

            bool read = false;
            string trackingno;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //分提單號
                    trackingno = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "分提單號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && trackingno != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["TrackingNo"] = trackingno;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 取得轉檔紀錄
        /// </summary>
        /// <returns></returns>
        public DataTable GetLog_Work()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select top 500 * from [jetf].[dbo].[LOG_WORK] order by StartTime desc ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            dt.Columns.Add("Format_StartTime", typeof(string));
            dt.Columns.Add("Format_EndTime", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["Format_StartTime"] = Convert.ToDateTime(dt.Rows[i]["StartTime"]).ToString("yyyy-MM-dd HH:mm:ss");
                dt.Rows[i]["Format_EndTime"] = Convert.ToDateTime(dt.Rows[i]["EndTime"]).ToString("yyyy-MM-dd HH:mm:ss");
            }
            return dt;
        }

        /// <summary>
        /// 取得稅單PDF路徑
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <returns></returns>
        public string GetClearance_Tax_Pdf(string taxNumber)
        {
            string filePath = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Clearance_Tax_Pdf] where TaxNumber=@TaxNumber ", conn))
            {
                da.SelectCommand.Parameters.Add("@TaxNumber", SqlDbType.NVarChar).Value = taxNumber;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                filePath = dt.Rows[0]["FilePath"].ToString();
            }
            dt.Dispose();
            return filePath;
        }

        /// <summary>
        /// 取得簽收單路徑
        /// </summary>
        /// <param name="cargoNumber"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public DataTable GetCargo_Sign_Receipt(string cargoNumber)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Cargo_Sign_Receipt] where Jetf_Serial=@Jetf_Serial ", conn))
            {
                da.SelectCommand.Parameters.Add("@Jetf_Serial", SqlDbType.NVarChar).Value = cargoNumber;
                da.Fill(dt);
            }
            dt.Dispose();
            return dt;
        }
    }
}
