using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Services.BatchUploadProcess
{
    public class BatchUploadProcessService : _BaseService
    {
        public BatchUploadProcessService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }


        /// <summary>
        /// 處置說明批次上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel BatchUploadProcess(ProcessStatus status, string filePath, string fileName, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            DataTable dt_Upload = ReadExcelBatchProcess(filePath, status);

            if (dt_Upload.Rows.Count == 0)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                return resopnseModel;
            }

            if (status == ProcessStatus.Process)
            {
                if (CkeckType(dt_Upload))
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = $"分類錯誤";
                    return resopnseModel;
                }
                //新增
                return InsertProcess(dt_Upload, fileName, userId);
            }

            if (status == ProcessStatus.Finish)
            {
                return InsertFinish(dt_Upload, fileName, userId);
            }

            return resopnseModel;
        }

        /// <summary>
        /// 讀取處置說明批次上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelBatchProcess(string filePath, ProcessStatus status)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("dlv_inv", typeof(string));
            dt_Data.Columns.Add("remark", typeof(string));
            dt_Data.Columns.Add("processType", typeof(string));

            bool read = false;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);

            if (ProcessStatus.Process == status)
            {
                for (int i = 0; i < sheet.LastRowNum + 1; i++)
                {
                    if (sheet.GetRow(i) != null)
                    {
                        //物流貨號
                        var dlv_inv = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                        //處置說明
                        var remark = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                        //分類
                        var processType = sheet.GetRow(i).GetCell(2) == null || string.IsNullOrWhiteSpace(sheet.GetRow(i).GetCell(2).ToString()) ? "1" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                        //讀到表頭 下一行開始讀取資料
                        if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "物流貨號" && sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "處置說明")
                        {
                            read = true;
                            continue;
                        }

                        if (read && dlv_inv != "" && remark != "")
                        {
                            dr = dt_Data.NewRow();
                            dr["dlv_inv"] = dlv_inv;
                            dr["remark"] = remark;
                            dr["processType"] = processType;
                            dt_Data.Rows.Add(dr);
                        }
                    }
                }
                return dt_Data;
            }

            //已結案
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //物流貨號
                    var dlv_inv = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "物流貨號")
                    {
                        read = true;
                        continue;
                    }

                    if (read && dlv_inv != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["dlv_inv"] = dlv_inv;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;

        }

        /// <summary>
        /// 新增處置說明(批次)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel InsertProcess(DataTable dt_Upload, string fileName, string user_Id)
        {
            var resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";

            DateTime date = DateTime.Now;
            string dataDate = date.ToString("yyyyMMdd");
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[Process]([DATADATE],[DLV_INV],[REMARK],[FILEPATH],[FILENAME],[BATCHFILENAME],[CRTDATETIME],[USER_ID],[PROCESS_TYPE]) ");
            sb.Append("values(@DATADATE,@DLV_INV,@REMARK,@FILEPATH,@FILENAME,@BATCHFILENAME,@CRTDATETIME,@USER_ID,@PROCESS_TYPE) ");

            if (conn.State != ConnectionState.Open)
                conn.Open();

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
                            cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["dlv_inv"].ToString();
                            cmd.Parameters.Add("@REMARK", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["remark"].ToString();
                            cmd.Parameters.Add("@FILEPATH", SqlDbType.NVarChar).Value = "";
                            cmd.Parameters.Add("@FILENAME", SqlDbType.NVarChar).Value = "";
                            cmd.Parameters.Add("@BATCHFILENAME", SqlDbType.NVarChar).Value = fileName;
                            cmd.Parameters.Add("@CRTDATETIME", SqlDbType.NVarChar).Value = date.ToString("yyyy-MM-dd HH:mm:ss");
                            cmd.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = user_Id;
                            cmd.Parameters.Add("@PROCESS_TYPE", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["processType"].ToString();
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入
                        tran.Commit();
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

            conn.Close();

            return resopnseModel;
        }

        /// <summary>
        /// 新增結案
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="fileName"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel InsertFinish(DataTable dt_Upload, string fileName, string user_Id)
        {
            var resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";

            try
            {
                string sql = @"
                            declare @Table Table
                            ( 
	                            DlvInv nvarchar(100)
                            )

                           {0}

                           update [jetf].[dbo].[Process] set FINISH='Y',FINISH_USER_ID=@FINISH_USER_ID,FINISH_DATETIME=getdate()
                           where exists (select 1 from @Table where DlvInv = DLV_INV ) and FINISH='N'
                        ";

                sql = string.Format(sql, $"INSERT INTO @Table VALUES {string.Join(",", dt_Upload.AsEnumerable().Select(r => $"('{r.Field<string>("dlv_inv")}')"))};");

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@FINISH_USER_ID", SqlDbType.NVarChar).Value = user_Id;
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        bool CkeckType(DataTable dt_Upload) 
        {
            //分類
            var types = new string[] { "1", "2", "3", "4", "5" };

            //檢查資料
            return dt_Upload.AsEnumerable()
                .Any(r => !string.IsNullOrWhiteSpace(r.Field<string>("processType")) &&
                !types.Contains(r.Field<string>("processType")));
        }

    }
}
