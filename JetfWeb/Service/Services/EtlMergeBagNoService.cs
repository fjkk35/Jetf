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
    public class EtlMergeBagNoService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public EtlMergeBagNoService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel Upload(string filePath, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //讀取Csv
            DataTable dt = ReadCsv(filePath);

            //新增
            if (dt.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                resopnseModel = InsertEtlMergeBagNo(dt, upload_time, userId);

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
        /// 讀取Csv
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadCsv(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("BagNo", typeof(string));
            dt_Data.Columns.Add("MergeBagNo", typeof(string));

            // 讀取CSV檔案
            var lines = File.ReadLines(filePath).Skip(1);

            // 處理CSV檔案的每一行
            foreach (var line in lines)
            {
                var data = line.Split(',');
                if (data.Length > 3)
                {
                    dr = dt_Data.NewRow();
                    dr["BagNo"] = data[0].Trim();
                    dr["MergeBagNo"] = data[3].Trim();
                    dt_Data.Rows.Add(dr);
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 寫入上傳檔案
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertEtlMergeBagNo(DataTable dt_Upload, string upload_Time, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            StringBuilder sb = new StringBuilder();
            sb.Append("insert jetf.dbo.EtlMergeBagNo(BagNo, MergeBagNo, UploadOpe, UploadTime) ");
            sb.Append("values(@BagNo, @MergeBagNo, @UploadOpe, @UploadTime) ");

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
                            cmd.Parameters.Add("@BagNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["BagNo"].ToString();
                            cmd.Parameters.Add("@MergeBagNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["MergeBagNo"].ToString();
                            cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = userId;
                            cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = upload_Time;
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

    }
}
