using Service.Models;
using Service.Models.ScanCargoArrivalTime;
using Service.Services.ScanCargoCustomer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class ScanCargoArrivalTimeService
    {
        private SqlConnection conn;

        private readonly ScanCargoCustomerService _scanCargoCustomerService;

        public ScanCargoArrivalTimeService(ScanCargoCustomerService scanCargoCustomerService)
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
            _scanCargoCustomerService = scanCargoCustomerService;
        }

        /// <summary>
        /// 取得輸入外車交倉時間資料
        /// </summary>
        /// <returns></returns>
        public ResopnseModel GetData(string trans, string dataType, string sDate, string eDate, string userId) 
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                List<ScanCargoArrivalTimeModel> list = new List<ScanCargoArrivalTimeModel>();

                DataTable dt = _scanCargoCustomerService.GetScanCargoCustomerDetailsPdf(trans, dataType, sDate, eDate).Item1;

                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dt.AsEnumerable().ToList().ForEach(r =>
                {
                    list.Add(new ScanCargoArrivalTimeModel()
                    {
                        PdtScanCargoUploadId = r.Field<int>("Id"),
                        ArrivalTime = r.Field<string>("ArrivalTime"),
                        UploadTime = r.Field<string>("UploadTime"),
                        TransName = r.Field<string>("TransName"),
                        SearchTime = now,
                        SearchOpe = userId
                    }); ;
                });

                list = list.Where(r => !string.IsNullOrEmpty(r.TransName)).ToList();

                //寫入資料
                resopnseModel = InsertScanCargoArrivalTime(list);

                if (resopnseModel.status != Status.success)
                {
                    return resopnseModel;
                }

                resopnseModel.ReturnObject = list.GroupBy(r => new { r.TransName, r.SearchTime, r.SearchOpe })
                                          .Select(r => new
                                          {
                                              TransName = r.Key.TransName,
                                              UploadTimeCount = r.Count(),
                                              ArrivalTimeCount = r.Count(x => !string.IsNullOrEmpty(x.ArrivalTime)),
                                              ArrivalTime = "",
                                              SearchTime = r.Key.SearchTime,
                                              SearchOpe = r.Key.SearchOpe,
                                          }).ToList();
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        public ResopnseModel UpdateScanCargoArrivalTime(string arrivalTime,string transName, string searchTime, string searchOpe) 
        {
            var result = CheckArrivalTime(arrivalTime, transName, searchTime, searchOpe);

            if (result.status != Status.success)
                return result;

            try
            {
                conn.Open();
                string sql = @"
                            update [jetf].[dbo].[ScanCargoArrivalTime]  set ArrivalTime=@arrivalTime
                            where TransName =@transName and SearchTime =@searchTime and searchOpe=@searchOpe 
                            update [jetf].[dbo].[PdtScanCargoUpload] set ArrivalTime=@arrivalTime,STATUS='N',UpdateArrivalTime=getdate(),UpdateArrivalTimeOpe=@searchOpe
                            where exists
                            (
                            select PdtScanCargoUploadId from [jetf].[dbo].[ScanCargoArrivalTime] 
                            where TransName =@transName and SearchTime =@searchTime and searchOpe=@searchOpe and PdtScanCargoUploadId=PdtScanCargoUpload.Id
                            )";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@transName", SqlDbType.NVarChar).Value = transName;
                    cmd.Parameters.Add("@searchTime", SqlDbType.NVarChar).Value = searchTime;
                    cmd.Parameters.Add("@searchOpe", SqlDbType.NVarChar).Value = searchOpe;
                    cmd.Parameters.Add("@arrivalTime", SqlDbType.NVarChar).Value = arrivalTime;
                    cmd.ExecuteNonQuery();
                }

                result.status = Status.success;
                result.msg = "更新成功";
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = ex.Message;
            }
            finally {
                conn.Close();
            }

            return result;
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private ResopnseModel InsertScanCargoArrivalTime(List<ScanCargoArrivalTimeModel> list) 
        {
            ResopnseModel resopnseModel = new ResopnseModel();

            string sql = @"insert [jetf].[dbo].[ScanCargoArrivalTime](PdtScanCargoUploadId, ArrivalTime, UploadTime, TransName, SearchTime, SearchOpe)
                           values(@PdtScanCargoUploadId, @ArrivalTime, @UploadTime, @TransName, @SearchTime, @SearchOpe)";

            try
            {
                conn.Open();

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
                                cmd.Parameters.Add("@PdtScanCargoUploadId", SqlDbType.NVarChar).Value = r.PdtScanCargoUploadId;
                                cmd.Parameters.Add("@ArrivalTime", SqlDbType.NVarChar).Value = string.IsNullOrEmpty(r.ArrivalTime) ? DBNull.Value : (object)Convert.ToDateTime(r.ArrivalTime).ToString("yyyy-MM-dd HH:mm:ss");
                                cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = r.UploadTime;
                                cmd.Parameters.Add("@TransName", SqlDbType.NVarChar).Value = r.TransName;
                                cmd.Parameters.Add("@SearchTime", SqlDbType.NVarChar).Value = r.SearchTime;
                                cmd.Parameters.Add("@SearchOpe", SqlDbType.NVarChar).Value = r.SearchOpe;
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
            }
            catch (Exception ex)
            {
                resopnseModel.msg = ex.Message;
            }
            finally
            {
                conn.Close();
            }
            
            return resopnseModel;
        }

        /// <summary>
        /// 檢查入庫時間
        /// </summary>
        /// <param name="arrivalTime"></param>
        /// <param name="transName"></param>
        /// <param name="searchTime"></param>
        /// <param name="searchOpe"></param>
        /// <returns></returns>
        private ResopnseModel CheckArrivalTime(string arrivalTime, string transName, string searchTime, string searchOpe) 
        {
            ResopnseModel result = new ResopnseModel { status = Status.success };
            

            List<ScanCargoArrivalTimeModel> list = new List<ScanCargoArrivalTimeModel>();
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[ScanCargoArrivalTime] where TransName=@TransName and SearchTime =@SearchTime and SearchOpe =@SearchOpe", conn))
            {
                da.SelectCommand.Parameters.Add("@TransName", SqlDbType.NVarChar).Value = transName;
                da.SelectCommand.Parameters.Add("@SearchTime", SqlDbType.NVarChar).Value = searchTime;
                da.SelectCommand.Parameters.Add("@SearchOpe", SqlDbType.NVarChar).Value = searchOpe;
                da.Fill(dt);
            }

            dt.AsEnumerable().ToList().ForEach(r =>
            {
                list.Add(new ScanCargoArrivalTimeModel()
                {
                    PdtScanCargoUploadId = r.Field<int>("Id"),
                    UploadTime = r.Field<string>("UploadTime"),
                });
            });

            //1.輸入的入庫時間不得小於掃讀時間
            //2.輸入的入庫時間不得大於掃讀時間 + 3天

            if (list.Any(r => Convert.ToDateTime(r.UploadTime) > Convert.ToDateTime(arrivalTime))) {
                result.status = Status.error;
                result.msg = "輸入的入庫時間不得小於掃讀時間";
            }

            if (list.Any(r => Convert.ToDateTime(r.UploadTime).AddDays(+3) < Convert.ToDateTime(arrivalTime)))
            {
                result.status = Status.error;
                result.msg = "輸入的入庫時間不得大於掃讀時間 + 3天";
            }

            return result;
        }
    }
}
