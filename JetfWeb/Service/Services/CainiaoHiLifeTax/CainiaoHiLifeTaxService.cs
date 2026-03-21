using Renci.SshNet;
using Service.Models;
using Service.Models.CainiaoHiLifeTax;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CainiaoHiLifeTax
{
    public class CainiaoHiLifeTaxService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public CainiaoHiLifeTaxService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 取得萊爾富稅金
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public byte[] GetCainiaoHiLifeTax(string startDate, string endDate)
        {
            string sql = @"
                            select DLV_INV,TO_DLV_COD from jetf.[dbo].[FEE_MASTER] a
                            where DLV_COM in ('14','14C','14P') and a.INCLUDE_TAX='N' and OUT_DATETIME between @startDate and @endDate
                         ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@startDate", SqlDbType.NVarChar).Value = startDate;
                da.SelectCommand.Parameters.Add("@endDate", SqlDbType.NVarChar).Value = endDate;
                da.Fill(dt);
            }

            StringBuilder sb = new StringBuilder();

            int i = 1;
            foreach (DataRow item in dt.Rows)
            {
                var no = (i++).ToString().PadLeft(5, '0');
                var dlv_inv = item["DLV_INV"].ToString();
                var tax = Convert.ToInt32(item["TO_DLV_COD"]);
                sb.AppendLine($"{no},{dlv_inv},{tax}");
            }

            // 將文字內容轉換成位元組
            byte[] fileBytes = Encoding.UTF8.GetBytes(sb.ToString());

            return fileBytes;
        }


        public ResopnseModel Upload(string filePath, string userId)
        {
            var fileName = $"TB{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
            ResopnseModel resopnseModel = new ResopnseModel();
           
            //讀取Excel 
            List<CainiaoHiLifeTaxModel> modelList = ReadTxt(filePath);

            //新增資料
            resopnseModel = InsertCainiaoHiLifeTax(modelList, fileName, userId);

            //上傳SFTP
            UploadSftp(filePath, fileName);

            return resopnseModel;
        }

        /// <summary>
        /// 讀取檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<CainiaoHiLifeTaxModel> ReadTxt(string filePath)
        {
            List<CainiaoHiLifeTaxModel> modelList = new List<CainiaoHiLifeTaxModel>();

            using (StreamReader sr = new StreamReader(filePath, System.Text.Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var data = line.Split(',');
                    modelList.Add(new CainiaoHiLifeTaxModel()
                    {
                        Seq = data[0],
                        DlvInv = data[1],
                        Tax = data[2]
                    });
                }
            }
            return modelList;
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="list"></param>
        /// <param name="fileName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private ResopnseModel InsertCainiaoHiLifeTax(List<CainiaoHiLifeTaxModel> list, string fileName, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = $"上傳成功筆數：{list.Count}";

            string sql = @"
                            insert [jetf].[dbo].[CainiaoHiLifeTax](Seq, DlvInv, Tax, FileName, UploadOpe, UploadTime)
                            values (@Seq, @DlvInv, @Tax, @FileName, @UploadOpe, @UploadTime)";

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        var uploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        list.ForEach(r =>
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@Seq", SqlDbType.NVarChar).Value = r.Seq;
                            cmd.Parameters.Add("@DlvInv", SqlDbType.NVarChar).Value = r.DlvInv;
                            cmd.Parameters.Add("@Tax", SqlDbType.NVarChar).Value = r.Tax;
                            cmd.Parameters.Add("@FileName", SqlDbType.NVarChar).Value = fileName;
                            cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = uploadTime;
                            cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();
                        });

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
            return resopnseModel;
        }

        /// <summary>
        /// 上傳SFTP
        /// </summary>
        private void UploadSftp(string filePath, string fileName)
        {
            var host = "203.73.97.233";
            var username = "JETF";
            var password = "Qa5ZOBI2WU=y";

            using (var client = new SftpClient(host, username, password))
            {
                client.Connect();
                // 上傳文件到遠程目錄
                using (var fileStream = File.OpenRead(filePath))
                {
                    client.UploadFile(fileStream, Path.Combine("In/", fileName));
                }
                client.Disconnect();
            }
        }
    }
}
