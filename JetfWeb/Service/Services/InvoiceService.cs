using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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
    public class InvoiceService
    {
        private SqlConnection conn;
        private CustomerService customerService = new CustomerService();
        /// <summary>
        /// 建構式
        /// </summary>
        public InvoiceService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 開立電子發票作業上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InvoiceWork(string filePath, string fileName, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            //讀取檔案
            DataTable dt_Upload = ReadExcelInvoiceWork(filePath);

            //新增
            if (dt_Upload.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                resopnseModel = InsertInvoiceWork(dt_Upload, upload_time, userId);

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

            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 讀取開立電子發票作業上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelInvoiceWork(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("Seq", typeof(string));
            dt_Data.Columns.Add("InvoiceDate", typeof(string));
            dt_Data.Columns.Add("InvoiceNo", typeof(string));
            dt_Data.Columns.Add("TrackingNo", typeof(string));
            dt_Data.Columns.Add("Amount", typeof(string));
            dt_Data.Columns.Add("Tax", typeof(string));
            dt_Data.Columns.Add("TotalAmount", typeof(string));
            dt_Data.Columns.Add("ProductName", typeof(string));
            dt_Data.Columns.Add("VATTitle", typeof(string));
            dt_Data.Columns.Add("VATNo", typeof(string));
            dt_Data.Columns.Add("Email", typeof(string));

            bool read = false;
            string seq, invoiceDate, invoiceNo, trackingNo, amount, tax, totalAmount, productName, vatTitle, vatNo, email;
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
                    seq = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    invoiceDate = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    invoiceNo = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    trackingNo = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    amount = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    tax = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    totalAmount = sheet.GetRow(i).GetCell(6) == null ? "" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                    productName = sheet.GetRow(i).GetCell(7) == null ? "" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                    vatTitle = sheet.GetRow(i).GetCell(8) == null ? "" : sheet.GetRow(i).GetCell(8).ToString().Trim();
                    vatNo = sheet.GetRow(i).GetCell(9) == null ? "" : sheet.GetRow(i).GetCell(9).ToString().Trim();
                    email = sheet.GetRow(i).GetCell(10) == null ? "" : sheet.GetRow(i).GetCell(10).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "發票號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && invoiceNo != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["Seq"] = seq;
                        dr["InvoiceDate"] = invoiceDate;
                        dr["InvoiceNo"] = invoiceNo;
                        dr["TrackingNo"] = trackingNo;
                        dr["Amount"] = amount;
                        dr["Tax"] = tax;
                        dr["TotalAmount"] = totalAmount;
                        dr["ProductName"] = productName;
                        dr["VATTitle"] = vatTitle;
                        dr["VATNo"] = vatNo;
                        dr["Email"] = email;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 新增開立電子發票作業
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel InsertInvoiceWork(DataTable dt_Upload, string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";

            DateTime date = DateTime.Now;
            string dataDate = date.ToString("yyyyMMdd");
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[InvoiceWork](Seq, InvoiceDate, InvoiceNo, TrackingNo, Amount, Tax, TotalAmount, ProductName, VATTitle, VATNo, Email, UPLOAD_OPE, UPLOAD_TIME) ");
            sb.Append("values(@Seq ,@InvoiceDate ,@InvoiceNo ,@TrackingNo ,@Amount ,@Tax ,@TotalAmount ,@ProductName ,@VATTitle ,@VATNo,@Email ,@UPLOAD_OPE ,@UPLOAD_TIME) ");

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
                            cmd.Parameters.Add("@Seq", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["Seq"].ToString();
                            cmd.Parameters.Add("@InvoiceDate", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["InvoiceDate"].ToString();
                            cmd.Parameters.Add("@InvoiceNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["InvoiceNo"].ToString();
                            cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TrackingNo"].ToString();
                            cmd.Parameters.Add("@Amount", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["Amount"].ToString();
                            cmd.Parameters.Add("@Tax", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["Tax"].ToString();
                            cmd.Parameters.Add("@TotalAmount", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TotalAmount"].ToString();
                            cmd.Parameters.Add("@ProductName", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["ProductName"].ToString();
                            cmd.Parameters.Add("@VATTitle", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["VATTitle"].ToString();
                            cmd.Parameters.Add("@VATNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["VATNo"].ToString();
                            cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["Email"].ToString();
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
        /// 取得開立電子發票作業
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public DataTableModel GetInvoiceWork(string upload_time, string user_Id)
        {
            DataTable dt = new DataTable();
            DataTableModel dataTableModel = new DataTableModel();
            dataTableModel.status = Status.success;
            dataTableModel.msg = "成功";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("select * from [dbo].[InvoiceWork] ");
                sb.Append("where Upload_Ope=@Upload_Ope and Upload_Time=@Upload_Time ");
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
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
    }
}
