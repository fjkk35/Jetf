using PDTWebAPI.Models.ScanCargo;
using PDTWebAPI.Models.Unpacking;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace PDTWebAPI.Services
{
    public class ScanCargoService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public ScanCargoService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        ~ScanCargoService()
        {
            conn.Dispose();
        }

        public UnpackingResponseModel PostUpload(UploadModel body)
        {
            UnpackingResponseModel response = new UnpackingResponseModel();
            response.Status = "Success";
            conn.Open();
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("[jetf].[dbo].[USP_Insert_PdtScanCargoUpload]", conn))
                    {
                        cmd.Transaction = tran;
                        cmd.CommandType = CommandType.StoredProcedure;
                        for (int i = 0; i < body.DataList.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@TransNo", SqlDbType.NVarChar).Value = body.TransNo;
                            cmd.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = body.DataType;
                            cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = body.UploadOpe;
                            cmd.Parameters.Add("@Data", SqlDbType.NVarChar).Value = body.DataList[i].Data;
                            cmd.Parameters.Add("@CarNo", SqlDbType.NVarChar).Value = body.CarNo ?? "";
                            cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = (object)DateTime.ParseExact(body.DataList[i].UploadTime, "yyyyMMddHHmmss", new System.Globalization.CultureInfo("zh-TW")).ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value;
                            if (body.DataList[i].Data.ToString() != "")
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        response.ResultCode = "Y";
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    response.Status = "Fail";
                    response.ResultMessage = ex.Message;
                }
            }
            conn.Close();
            return response;
        }
    }
}