using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using Service.Models.Shenzhen;
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
    public class ShenzhenService
    {
        SqlConnection conn;
        public ShenzhenService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        public ResopnseModel Upload(string filePath, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //讀取Excel 
            List<ShenzhenCargoModel> modelList = ReadExcel(filePath);
            //新增
            if (modelList.Count > 0)
            {
                //寫入資料
                string uploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                resopnseModel = InsertShenzhenCargo(modelList, uploadTime, userId);
                resopnseModel.msg = $"上傳檔案筆數：{modelList.Count}";

            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = "上傳檔案筆數：0";
            }
            conn.Close();
            return resopnseModel;
        }

        public ResopnseModel InsertShenzhenCargo(List<ShenzhenCargoModel> list, string uploadTime, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();

            string sql = @"
                            insert [jetf].[dbo].[ShenzhenCargo](TrackingNo, DeliveryNo, UploadOpe, UploadTime)
                            values (@TrackingNo, @DeliveryNo, @UploadOpe, @UploadTime)";

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        list.ForEach(r =>
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = r.TrackingNo;
                            cmd.Parameters.Add("@DeliveryNo", SqlDbType.NVarChar).Value = r.DeliveryNo;
                            cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = uploadTime;
                            cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();
                        });

                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                        resopnseModel.msg = "新增成功";
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

        List<ShenzhenCargoModel> ReadExcel(string filePath)
        {
            bool read = false;

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }


            var list = new List<ShenzhenCargoModel>();
            var sheet = workbook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    var item = new ShenzhenCargoModel();
                    item.TrackingNo = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    item.DeliveryNo = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();


                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "分提單號") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "物流貨號"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && !string.IsNullOrEmpty(item.TrackingNo))
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }
    }
}
