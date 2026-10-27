using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.WorkDay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Services.WorkLoad
{
    public partial class WorkLoadService : _BaseService
    {
        private readonly WorkDayService _workDayService;
        private readonly CustomerService _customerService;

        public WorkLoadService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, WorkDayService workDayService, CustomerService customerService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _workDayService = workDayService;
            _customerService = customerService;
        }

        /// <summary>
        /// 上傳檔案 空快錯單袋號
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFileEtlBagNo(string filePath, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //讀取Excel空快錯單袋號
            DataTable dt = ReadExcelEtlBagNo(filePath);
            //新增
            if (dt.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                resopnseModel = InsertEtl_BagNo_Upload(dt, upload_time, userId);

                if (resopnseModel.status == Status.success)
                {
                    //resopnseModel.msg = $"上傳檔案筆數：{dt.Rows.Count }";
                    resopnseModel.msg = $"{upload_time}︿{userId}";
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
        /// 上傳檔案 海快錯單袋號
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFileSeaBagNo(string filePath, string dataDate, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //讀取Excel 海快錯單袋號
            DataTable dt = ReadExcelSeaBagNo(filePath);
            //新增
            if (dt.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dataDate = Convert.ToDateTime(dataDate).ToString("yyyyMMdd");
                resopnseModel = InsertSea_BagNo_Upload(dt, dataDate, upload_time, userId);

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
        /// 上傳檔案 海快艙單號碼
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFileSeaManifest(string filePath, string dataDate, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //讀取Excel 海快艙單號碼
            DataTable dt = ReadExcelSeaManifest(filePath);
            //新增
            if (dt.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dataDate = Convert.ToDateTime(dataDate).ToString("yyyyMMdd");
                resopnseModel = InsertSea_MANIFEST_Upload(dt, dataDate, upload_time, userId);

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
        /// 讀取Excel空快錯單袋號
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelEtlBagNo(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("BAGNO", typeof(string));

            bool read = false;
            string bagNo;

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
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //袋號
                    bagNo = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "袋號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && bagNo != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["BAGNO"] = bagNo;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取Excel 海快錯單袋號
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelSeaBagNo(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("MAINNUMBER", typeof(string));
            dt_Data.Columns.Add("BL_NO", typeof(string));
            dt_Data.Columns.Add("MESSAGE", typeof(string));

            bool read = false;
            string mainnumber, bl_no, message;

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
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //主號
                    mainnumber = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //分號
                    bl_no = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //錯單訊息
                    message = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主號") && (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "分號") && (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "錯單訊息"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && mainnumber != "" && bl_no != "" && message != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["MAINNUMBER"] = mainnumber;
                        dr["BL_NO"] = bl_no;
                        dr["MESSAGE"] = message;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取Excel 海快艙單號碼
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelSeaManifest(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("MAINNUMBER", typeof(string));
            dt_Data.Columns.Add("BL_NO", typeof(string));
            dt_Data.Columns.Add("MANIFEST", typeof(string));
            dt_Data.Columns.Add("VESSEL", typeof(string));

            bool read = false;
            string mainnumber, bl_no, manifest, vessel;

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
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //主號
                    mainnumber = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //分號
                    bl_no = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //艙單號碼
                    manifest = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //船班
                    vessel = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主號") && (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "分號") && (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "艙單號碼") && (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(3).ToString().Trim() == "船班"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && mainnumber != "" && bl_no != "" && manifest != "" && vessel != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["MAINNUMBER"] = mainnumber;
                        dr["BL_NO"] = bl_no;
                        dr["MANIFEST"] = manifest;
                        dr["VESSEL"] = vessel;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 寫入上傳檔案
        /// </summary>
        /// <summary>
        /// 寫入上傳檔案 空快錯單袋號
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertEtl_BagNo_Upload(DataTable dt_Upload, string upload_Time, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            //新增SEA_TAX_UPLOAD
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[ETL_BAGNO_UPLOAD](BAGNO,UPLOAD_TIME,UPLOAD_OPE) ");
            sb.Append("values(@BAGNO,@UPLOAD_TIME,@UPLOAD_OPE) ");
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
                            cmd.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["BAGNO"].ToString();
                            cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                            cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();
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
        /// 寫入上傳檔案 海快錯單袋號
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertSea_BagNo_Upload(DataTable dt_Upload, string dataDate, string upload_Time, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            //新增SEA_TAX_UPLOAD
            StringBuilder sb = new StringBuilder();
            sb.Append("select MAINNUMBER from  [jetf].[dbo].[SEA_BAGNO_UPLOAD] ");
            sb.Append("where MAINNUMBER=@MAINNUMBER and BL_NO=@BL_NO and MESSAGE=@MESSAGE ");
            sb.Append("if @@ROWCOUNT>0 ");
            sb.Append("begin ");
            sb.Append("	    update [jetf].[dbo].[SEA_BAGNO_UPLOAD] set DATADATE=@DATADATE,MAINNUMBER=@MAINNUMBER,APPOINT='',BL_NO=@BL_NO,MESSAGE=@MESSAGE,UPLOAD_TIME=@UPLOAD_TIME,UPLOAD_OPE=@UPLOAD_OPE ");
            sb.Append("	    where MAINNUMBER=@MAINNUMBER and BL_NO=@BL_NO and MESSAGE=@MESSAGE ");
            sb.Append("end ");
            sb.Append("else ");
            sb.Append("begin ");
            sb.Append("     insert [jetf].[dbo].[SEA_BAGNO_UPLOAD](DATADATE,MAINNUMBER,BL_NO,MESSAGE,UPLOAD_TIME,UPLOAD_OPE) ");
            sb.Append("     values(@DATADATE,@MAINNUMBER,@BL_NO,@MESSAGE,@UPLOAD_TIME,@UPLOAD_OPE) ");
            sb.Append("end ");


            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        //刪除當天上傳海快錯單
                        //cmd.CommandText = "delete [jetf].[dbo].[SEA_BAGNO_UPLOAD] where DATADATE=@DATADATE";
                        //cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                        //cmd.ExecuteNonQuery();

                        //新增海快錯單
                        //cmd.CommandText = sb.ToString();
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@MAINNUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MAINNUMBER"].ToString();
                            cmd.Parameters.Add("@BL_NO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["BL_NO"].ToString();
                            cmd.Parameters.Add("@MESSAGE", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MESSAGE"].ToString();
                            cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                            cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();
                        }

                        //新增上傳主號，無分提單號的資料
                        sb = new StringBuilder();
                        sb.Append("with ");
                        sb.Append("ETL_PRE_APPROVAL as ");
                        sb.Append("( ");
                        sb.Append("	    select * from [DATA_CENTER].[dbo].[ETL_PRE_APPROVAL] a ");
                        sb.Append("	    where MODEL='SEA' and SEQUENCE_NUMERIC='1' ");
                        sb.Append("	    and exists ");
                        sb.Append("	    (");
                        sb.Append(" 	select MAINNUMBER from [jetf].[dbo].[SEA_BAGNO_UPLOAD] ");
                        sb.Append(" 	where UPLOAD_TIME=@UPLOAD_TIME and UPLOAD_OPE=@UPLOAD_OPE and MAINNUMBER=a.MAWB_NO ");
                        sb.Append("	    group by MAINNUMBER ");
                        sb.Append(" 	) ");
                        sb.Append(") ");
                        sb.Append("insert [jetf].[dbo].[SEA_BAGNO_UPLOAD](DATADATE, MAINNUMBER, BL_NO, MESSAGE, APPOINT, UPLOAD_TIME, UPLOAD_OPE) ");
                        sb.Append("select @DATADATE,MAWB_NO,HAWB_NO,'B6F' as MESSAGE,'V',@UPLOAD_TIME,@UPLOAD_OPE from ETL_PRE_APPROVAL a ");
                        sb.Append("where not exists ");
                        sb.Append("( ");
                        sb.Append("	select MAINNUMBER from [jetf].[dbo].[SEA_BAGNO_UPLOAD] ");
                        sb.Append("	where MESSAGE='B6F' and MAINNUMBER=a.MAWB_NO and BL_NO=a.HAWB_NO ");
                        sb.Append(") ");
                        cmd.CommandText = sb.ToString();
                        cmd.Parameters.Clear();
                        cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                        cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                        cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                        cmd.CommandTimeout = 600;
                        cmd.ExecuteNonQuery();

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
        /// 寫入上傳檔案 海快艙單號碼
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertSea_MANIFEST_Upload(DataTable dt_Upload, string dataDate, string upload_Time, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            //新增SEA_TAX_UPLOAD
            StringBuilder sb = new StringBuilder();
            sb.Append("select MAINNUMBER from [jetf].[dbo].[SEA_MANIFEST_UPLOAD] ");
            sb.Append("where MAINNUMBER=@MAINNUMBER and BL_NO=@BL_NO ");
            sb.Append("if @@ROWCOUNT>0 ");
            sb.Append("begin ");
            sb.Append("	    update [jetf].[dbo].[SEA_MANIFEST_UPLOAD] set DATADATE=@DATADATE,MANIFEST=@MANIFEST,VESSEL=@VESSEL,UPLOAD_TIME=@UPLOAD_TIME,UPLOAD_OPE=@UPLOAD_OPE ");
            sb.Append("	    where MAINNUMBER=@MAINNUMBER and BL_NO=@BL_NO ");
            sb.Append("end ");
            sb.Append("else ");
            sb.Append("begin ");
            sb.Append("     insert [jetf].[dbo].[SEA_MANIFEST_UPLOAD](DATADATE,MAINNUMBER,BL_NO,MANIFEST,VESSEL,UPLOAD_TIME,UPLOAD_OPE) ");
            sb.Append("     values(@DATADATE,@MAINNUMBER,@BL_NO,@MANIFEST,@VESSEL,@UPLOAD_TIME,@UPLOAD_OPE) ");
            sb.Append("end ");

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
                            cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@MAINNUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MAINNUMBER"].ToString();
                            cmd.Parameters.Add("@BL_NO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["BL_NO"].ToString();
                            cmd.Parameters.Add("@MANIFEST", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MANIFEST"].ToString();
                            cmd.Parameters.Add("@VESSEL", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["VESSEL"].ToString();
                            cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                            cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();
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
        /// 取得空快錯單袋號(主號、貨號)
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public DataTableModel GetEtlBagNo(string upload_time, string upload_ope)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("select distinct a.BAGNO,b.MAINNUMBER,b.TRACKINGNO from [jetf].[dbo].[ETL_BAGNO_UPLOAD] a ");
                sb.Append("left join DATA_CENTER.dbo.ORIGINALLIST b on a.BAGNO=b.BAGNO ");
                sb.Append("where UPLOAD_TIME=@UPLOAD_TIME and UPLOAD_OPE=@UPLOAD_OPE ");
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_time;
                    da.SelectCommand.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = upload_ope;
                    da.Fill(dt);
                }
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
        /// 取得海快錯單作業
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public DataTableModel GetSeaBagNoWork(string source, string sDate, string eDate, bool report)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT a.DATADATE,jetf.dbo.GetCUSTOMER('海運',c.DESPATCH_NAME) as DESPATCH_NAME,c.JETF_ID,c.TERMSOFPRICE,c.CURRENCY,a.MAINNUMBER,a.BL_NO,a.APPOINT,b.MANIFEST,c.CORRECT_ID,c.ETA,a.DATADATE,a.MESSAGE,b.VESSEL,b.MANIFEST,c.GW,c.PIECE,c.PIECE_UNIT,c.MARKS,c.ITEM_NO,c.ITEM_NAME,c.CCC_CODE,c.TRADEMARK,c.II_SPEC,c.NW,c.QUANTITY,c.QUANTITY_UNIT,c.UNIT_PRICE,c.INVOICE_AMOUNT,c.MEASUREMENT,c.CBM,c.MADEIN,c.EXPORTER,c.EX_COUNRTYCODE,c.EX_ADD,c.PARTY_IDENTIFIER,c.IMPORTER_ID,c.IMPORTER,c.IM_PHONENO,c.IM_ADD,c.DECLARATION_2,c.TAXFEE_DECLARED,c.TRANS_NAME,c.JETF_SERIAL,c.LPNO,c.SIZE,d.MODIFY_TIME,d.STATUS,d.REPLY_CODE,c.CONT_TYPE as E_CONT_TYPE,c.CONT_NO as E_CONT_NO,c.SEALNO as E_SEALNO,c.CONT_TRANSMODEL as E_CONT_TRANSMODEL,e.CONT_TYPE as O_CONT_TYPE,e.CONT_NO as O_CONT_NO,e.SEALNO as O_SEALNO,e.CONT_TRANSMODEL as O_CONT_TRANSMODEL,g.NAME as MODIFYBY, ");
                sb.Append("h.CONSOL_CODE,h.CONSOL_TYPE,h.CONSOL_NAME,h.CONSOL_URL ");
                sb.Append("FROM [jetf].[dbo].[SEA_BAGNO_UPLOAD] a ");
                sb.Append("left join [jetf].[dbo].[SEA_MANIFEST_UPLOAD] b on a.MAINNUMBER=b.MAINNUMBER and a.BL_NO=b.BL_NO ");
                sb.Append("left join [DATA_CENTER].[dbo].[SEA_ORDER_EDIT] c on a.MAINNUMBER=c.MAINNUMBER and a.BL_NO = c.BL_NO ");
                if (report)
                {
                    sb.Append("and c.GW>0 ");
                }
                sb.Append("left join [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] e on a.MAINNUMBER=e.MAINNUMBER and a.BL_NO = e.BL_NO and e.GW>0 ");

                sb.Append("left join [DATA_CENTER].[dbo].[ETL_PRE_APPROVAL] d on a.BL_NO=HAWB_NO and MODEL='SEA' and SEQUENCE_NUMERIC='1' ");
                sb.Append("left join [DATA_CENTER].[dbo].[CES_MAIN_ORDER] f on a.MAINNUMBER=f.MAIN_NUMBER and f.TYPE='ER' ");
                sb.Append("left join [DATA_CENTER].[dbo].[SYS_PARAM] g on f.CLEARANCE_CP=g.CODE and g.TYPE='CLEARANCE_CP' ");
                sb.Append("left join [DATA_CENTER].[dbo].[Sys_cust] h on c.DESPATCH_NAME = h.CUST_CODE ");
                if (source == "日期")
                {
                    sb.Append("where a.DATADATE between @sDate and @eDate ");
                }
                else
                {
                    sb.Append("where a.UPLOAD_TIME between @sDate and @eDate ");
                }

                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }

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
        /// 取得海快錯單作業-具結-主號訂單明細
        /// </summary>
        /// <param name="mainnumber"></param>
        /// <returns></returns>
        public DataTableModel GetCesMainOrder(string mainnumber)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
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
        /// 取得海快錯單作業-倉單筆數
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public DataTableModel GetSeaManifestCount(string source, string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("select MAINNUMBER,count(MAINNUMBER) as TOTAL from [jetf].[dbo].[SEA_MANIFEST_UPLOAD]  a ");
                sb.Append("where exists ( ");
                sb.Append("	select 1 from [jetf].[dbo].[SEA_BAGNO_UPLOAD] ");
                sb.Append("	where MAINNUMBER=a.MAINNUMBER ");
                if (source == "日期")
                {
                    sb.Append("and DATADATE between @sDate and @eDate ");
                }
                else
                {
                    sb.Append("and UPLOAD_TIME between @sDate and @eDate ");
                }
                sb.Append(") ");
                sb.Append("group by MAINNUMBER ");


                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                    da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                    da.Fill(dt);
                }
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
        /// 取得海快錯單作業-主號須預委筆數
        /// </summary>
        /// <param name="mainNumbers"></param>
        /// <returns></returns>
        public DataTable GetSeaMainNumberReturnCount(string[] mainNumbers)
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

            sql = string.Format(sql, $"INSERT INTO @MainNumber VALUES {string.Join(",", mainNumbers.Select(r => $"('{r}')"))};");

            DataTable dt = new DataTable();

            if (mainNumbers.Length > 0)
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 取得空快錯單統計表
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public DataTableModel GetEtlErrorWorkReport(string sDate, string eDate, bool isNoCust)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("select * from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] a ");
                sb.Append("where ");
                sb.Append("exists ");
                sb.Append("(select 1 from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR_CODE] ");
                sb.Append("where REMARK='空快錯單統計' and REASON=a.REASON ) and ");
                sb.Append("ISSUEDATE between @sDate and @eDate ");

                //無客戶
                if (isNoCust)
                {
                    sb.Append("and CUST is null ");
                }

                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = $"{sDate} 00:00:00";
                    da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = $"{eDate} 23:59:59";
                    da.Fill(dt);
                }
                //日期
                dt.Columns.Add("DATADATE", typeof(string));
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dt.Rows[i]["DATADATE"] = Convert.ToDateTime(dt.Rows[i]["ISSUEDATE"]).ToString("yyyy/MM/dd");

                    if (isNoCust)
                    {
                        dt.Rows[i]["CUST"] = "無客戶";
                    }
                }

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
        /// 取得空快錯單統計表-傳輸筆數
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public DataTableModel GetEtlErrorWorkReportCount(string sDate, string eDate, bool isNoCust)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("with ");
                sb.Append("ETL_PLINK_ERROR as ");
                sb.Append("( ");
                sb.Append("select CONVERT(nvarchar(20),ISSUEDATE,23) as ISSUEDATE,CUST,MAWB from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] a ");
                sb.Append("where exists ");
                sb.Append("(select 1 from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR_CODE] ");
                sb.Append("where REMARK='空快錯單統計' and REASON=a.REASON ) and ");
                sb.Append("ISSUEDATE between @sDate and @eDate ");
                sb.Append("group by ISSUEDATE,CUST,MAWB ");
                sb.Append(") ");
                sb.Append("select CUST,ISSUEDATE,MAWB,count(distinct TRACKINGNO) as TOTAL from ETL_PLINK_ERROR a ");
                sb.Append("join (select CONVERT(nvarchar(20),sign_in_time,23) as sign_in_date,MAINNUMBER,TRACKINGNO from [DATA_CENTER].[dbo].[MAKELIST]) b on a.MAWB=b.MAINNUMBER and a.ISSUEDATE=b.sign_in_date ");
                sb.Append("group by CUST,ISSUEDATE,MAWB ");

                //無客戶
                if (isNoCust)
                {
                    sb.Append("having CUST is null ");
                }
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = $"{sDate} 00:00:00";
                    da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = $"{eDate} 23:59:59";
                    da.Fill(dt);
                }
                //日期
                dt.Columns.Add("DATADATE", typeof(string));
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dt.Rows[i]["DATADATE"] = Convert.ToDateTime(dt.Rows[i]["ISSUEDATE"]).ToString("yyyy/MM/dd");

                    if (isNoCust)
                    {
                        dt.Rows[i]["CUST"] = "無客戶";
                    }
                }

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
        /// 取得空快錯單明細
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public DataTableModel GetEtlErrorWorkDetails(string sDate, string eDate, bool isNoCust)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("select a.CUST,a.OUT_TIME,b.sign_in_time,b.sign_out_time,a.HAWB,d.RECIPIENT,d.RECPHONE,a.REASON,a.MAWB,a.BAG_NO,c.DELIVERYDATE,d.FIELD_X,d.ORDER_NO from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] a ");
                sb.Append("left join [DATA_CENTER].[dbo].[MAKELIST] b on a.MAWB=b.MAINNUMBER and a.HAWB=b.TRACKINGNO ");
                sb.Append("left join [DATA_CENTER].[dbo].[MAINORDERINFO] c on a.MAWB=c.MAINNUMBER ");
                sb.Append("left join [DATA_CENTER].[dbo].[ORIGINALLIST] d on a.HAWB=d.TRACKINGNO ");
                sb.Append("where exists ");
                sb.Append("(select 1 from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR_CODE]  ");
                sb.Append("where REMARK='空快錯單統計' and REASON=a.REASON ) and ");
                sb.Append("ISSUEDATE between @sDate and @eDate ");

                if (isNoCust)
                {
                    sb.Append("and a.CUST is null");
                }

                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = $"{sDate} 00:00:00";
                    da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = $"{eDate} 23:59:59";
                    da.Fill(dt);
                }


                if (isNoCust)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dt.Rows[i]["CUST"] = "無客戶";
                    }
                }

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
