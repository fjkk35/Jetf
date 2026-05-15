using Dapper;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Polly;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Models.CptSeaMainNumberJob;
using Service.Models.CptTradeVan;
using Service.Services.CptTradeVan;
using Service.Services.SeaWorkErrorOrderReport;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using TelegramLibrary.Model;

namespace Service.Services
{
    public class CptTradeVanService : _BaseService
    {
        private readonly HttpClient _httpClient;
        private readonly CptPortalApi _cptPortalApi;

        public CptTradeVanService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, IHttpClientFactory httpClientFactory, CptPortalApi cptPortalApi)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _httpClient = httpClientFactory.CreateClient();
            _cptPortalApi = cptPortalApi;
        }

        public async Task<ResponseModel> UploadAsync(string filePath, CptTradeVanEnum source,string data, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            try
            {
                IWorkbook workbook = new XSSFWorkbook();
                //上傳時間
                var uploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                switch (source)
                {
                    case CptTradeVanEnum.ReceiveOrder:
                        var receiveOrderList = ReadReceiveOrderExcel(filePath);
                        Search(receiveOrderList);
                        workbook = GetReceiveOrderSearchExcel(receiveOrderList);
                        break;
                    case CptTradeVanEnum.ErrorOrder:
                        var errorOrderList = ReadErrorOrderExcel(filePath);
                        Search(errorOrderList);
                        workbook = GetErrorOrderSearchExcel(errorOrderList);
                        break;
                    case CptTradeVanEnum.CargoManifest:
                        var cargoManifestList = ReadCargoManifestExcel(filePath);
                        //查詢Api
                        Search(cargoManifestList);
                        //新增結果
                        InsertCptGb378(cargoManifestList, uploadTime, userId);
                        //取得結果包含預委票數
                        cargoManifestList = GetCptGb378(uploadTime, userId);
                        //取得須預委未按明細
                        var notReplyDetailList = GetNotReplyDetail(uploadTime, userId);

                        GetCargoManifestSheet(workbook, cargoManifestList);
                        GetNotReplyDetailSheet(workbook, notReplyDetailList);
                        break;
                    case CptTradeVanEnum.SeaMainNumber:
                        var mainNumberList = ReadData(data);
                        resopnseModel = InsertCptSeaMainNumber(mainNumberList, uploadTime, userId);
                        if (resopnseModel.status == Status.success)
                        {
                            //執行查詢Gb321、Gb353
                            RunCptSeaMainNumberJobAsync(uploadTime, userId);

                            var cptSeaMainNumberDetailList = GetCptSeaMainNumberDetails(uploadTime, userId);
                            workbook = GetCptSeaMainNumberDetailExcel(cptSeaMainNumberDetailList);
                        }
                        break;
                    case CptTradeVanEnum.SeaReceiveOrderWork:
                        var trackingNoList = ReadData(data);
                        var list = GetCptSeaMainNumberDetails(trackingNoList);
                        SearchGb321(list);
                        workbook = GetCptReceiveOrderSearchWorkExcel(list);
                        break;
                    case CptTradeVanEnum.ErrorOrderWork:
                        trackingNoList = ReadData(data);
                        var gb353List = GetCptSeaMainNumberDetails(trackingNoList);
                        SearchGb353(gb353List);
                        workbook = GetCptErrorOrderSearchWorkExcel(gb353List);
                        break;
                    case CptTradeVanEnum.DeleteSeaMainNumber:
                        var deleteMainNumbers = ReadData(data);
                        if (deleteMainNumbers.Count > 1)
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "刪除海運主號只能一筆";
                            break;
                        }
                        var mainNumber = deleteMainNumbers.First();
                        resopnseModel = DeleteCptSeaMainNumber(mainNumber);
                        break;
                    case CptTradeVanEnum.EtlErrorOrder:
                        var etlErrorOrderList = ReadEtlErrorOrderExcel(filePath);
                        Search(etlErrorOrderList);
                        workbook = GetEtlErrorOrderSearchExcel(etlErrorOrderList);
                        break;
                    case CptTradeVanEnum.EtlClearanceOrder:
                        var etlClearanceOrder = ReadEtlClearanceOrderExcel(filePath);
                        SearchEtlClearanceOrder(etlClearanceOrder);
                        GetAirApprovalG(etlClearanceOrder);
                        workbook = GetEtlClearanceOrderExcel(etlClearanceOrder);
                        break;
                }

                resopnseModel.ReturnObject = workbook;

                conn.Close();
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        /// <summary>
        /// 收單查詢
        /// </summary>
        /// <param name="list"></param>
        void Search(List<ReceiveOrderModel> list)
        {
            //重新執行次數
            int retryCount;

            list.ForEach(r =>
            {
                var parameters = new Dictionary<string, string>
                    {
                        { "transType", "S" },
                        { "mawb", string.IsNullOrEmpty(r.MainNumber) ? "" : r.MainNumber },
                        { "hawb", r.TrackingNo }
                    };

                var result = GetGb321(parameters);

                r.CptNo = "Gb321";
                r.Msg = result.Msg;
                if (result.Status == "ok")
                {
                    var last = result.GridModel.OrderByDescending(x => x.ProDate).ThenByDescending(x => x.ProTime).FirstOrDefault();
                    r.ProDate = last.ProDate;
                    r.ProTime = last.ProTime;
                    r.ProType = last.ProType;

                    var data = result.GridModel
                        .OrderByDescending(x => x.ProDate)
                        .ThenByDescending(x => x.ProTime)
                        .Select(
                        x => new
                        {
                            x.ProDate,
                            x.ProTime,
                            x.ProType,
                        }).ToList();

                    var otherProType = data.Select(x => new
                    {
                        ProDate = DateTime.TryParseExact($"{x.ProDate.Trim()}{x.ProTime.Trim()}", "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime) ? dateTime.ToString("yyyy/MM/dd") : "",
                        ProTime = DateTime.TryParseExact($"{x.ProDate.Trim()}{x.ProTime.Trim()}", "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime) ? dateTime.ToString("HH:mm:ss") : "",
                        ProType = x.ProType
                    }).ToList();

                    r.OtherProType = string.Join("\r\n", otherProType
                        .OrderByDescending(x => x.ProDate)
                        .ThenByDescending(x => x.ProTime)
                        .Select(x =>
                            $"{x.ProDate}，{x.ProTime}，{x.ProType}"
                        ).ToList());
                }
                else
                {
                    //海快原單資料
                    DataTable dt = GetSeaOrderOriginal(r.TrackingNo);
                    if (IsPostEntry(dt))
                    {
                        parameters = new Dictionary<string, string>
                            {
                                { "mawb", string.IsNullOrEmpty(r.MainNumber) ? dt.Rows[0]["MAINNUMBER"].ToString() : r.MainNumber },
                                { "hawb", r.TrackingNo }
                            };

                        var gb301Result = GetGb301(parameters);

                        //重新執行次數
                        retryCount = 0;
                        do
                        {
                            if (result.Msg.Contains("發生一或多項錯誤。"))
                            {
                                Thread.Sleep(1000);
                                gb301Result = GetGb301(parameters);
                                retryCount++;
                            }
                            else
                            {
                                break;
                            }
                        } while (retryCount < 3);

                        r.CptNo = "Gb301";
                        r.Msg = gb301Result.Msg;
                        if (gb301Result.Status == "ok")
                        {
                            var last = gb301Result.GridModel?.OrderByDescending(x => x.ProDateTime).FirstOrDefault();
                            if (last != null)
                            {
                                if (DateTime.TryParseExact($"{last.ProDateTime}", "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                                {
                                    r.ProDate = dateTime.ToString("yyyyMMdd");
                                    r.ProTime = dateTime.ToString("HHmmss");
                                }
                                r.ProType = last.ProcEventCodeStr;

                                var data = gb301Result.GridModel
                                  .OrderByDescending(x => x.ProDateTime)
                                  .Select(
                                  x => new
                                  {
                                      x.ProDateTime,
                                      x.ProcEventCodeStr
                                  }).ToList();

                                var otherProType = data.Select(x => new
                                {
                                    ProDateTime = DateTime.TryParseExact($"{x.ProDateTime.Trim()}", "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime) ? dateTime.ToString("yyyy/MM/dd HH:mm:ss") : "",
                                    ProcEventCodeStr = x.ProcEventCodeStr
                                }).ToList();

                                r.OtherProType = string.Join("\r\n",
                                    otherProType
                                    .OrderByDescending(x => x.ProDateTime)
                                    .Select(x => $"{x.ProDateTime}，{x.ProcEventCodeStr}").ToList());
                            }
                        }
                    }
                }
                Thread.Sleep(300);
            });
        }

        /// <summary>
        /// 錯單查詢
        /// </summary>
        /// <param name="list"></param>
        void Search(List<ErrorOrderModel> list)
        {
            //重新執行次數
            int retryCount;

            list.ForEach(r =>
            {
                var parameters = new Dictionary<string, string>
                    {
                        //海空運別
                        { "transType", "S" },
                        //報單號碼
                        { "declno", "" }, 
                        //選擇報單號碼D、分提單號碼H
                        { "queryType", "H" },
                        //分提單號碼
                        { "hawb", r.TrackingNo }
                    };

                var result = GetGb353(parameters);

                retryCount = 0;
                do
                {
                    if (result.Msg.Contains("發生一或多項錯誤。"))
                    {
                        Thread.Sleep(1000);
                        result = GetGb353(parameters);
                        retryCount++;
                    }
                    else
                    {
                        break;
                    }
                } while (retryCount < 3);

                r.CptNo = "Gb353";
                r.Msg = result.Msg;
                if (result.Status == "ok")
                {
                    var last = result.Data.OrderByDescending(x => x.IssueDate).ThenByDescending(x => x.IssueTime).FirstOrDefault();
                    r.IssueDate = last.IssueDate;
                    r.IssueTime = last.IssueTime;
                    r.RejReasonCode = last.RejReasonCode;
                    r.RejReasonDesc = last.RejReasonDesc;

                    var data = result.Data
                        .OrderByDescending(x => x.IssueDate)
                        .ThenByDescending(x => x.IssueTime)
                        .Select(
                        x => new
                        {
                            x.IssueDate,
                            x.IssueTime,
                            x.RejReasonCode,
                            x.RejReasonDesc,
                        }).ToList();

                    var otherProType = data.Select(x =>
                    {
                        if (DateTime.TryParseExact($"{x.IssueDate}{x.IssueTime}", "yyyyMMddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                        {
                            return new
                            {
                                IssueDateTime = dateTime.ToString("yyyy/MM/dd HH:mm:ss"),
                                RejReasonCode = x.RejReasonCode,
                                RejReasonDesc = x.RejReasonDesc,
                            };
                        }
                        else
                        {
                            return new
                            {
                                IssueDateTime = "",
                                RejReasonCode = x.RejReasonCode,
                                RejReasonDesc = x.RejReasonDesc,
                            };
                        }
                    }).ToList();


                    r.OtherProType = string.Join("\r\n", otherProType
                        .OrderByDescending(x => x.IssueDateTime)
                        .Select(x =>
                            $"{x.IssueDateTime}，{x.RejReasonCode}，{x.RejReasonDesc}"
                        ).ToList());
                }
                Thread.Sleep(300);
            });
        }

        /// <summary>
        /// 空運錯單查詢
        /// </summary>
        /// <param name="list"></param>
        void Search(List<EtlErrorOrderModel> list)
        {
            //重新執行次數
            int retryCount;

            list.ForEach(r =>
            {
                var parameters = new Dictionary<string, string>
                    {
                        //海空運別
                        { "transType", "A" },
                        //報單號碼
                        { "declno", r.ClearanceNo }, 
                        //選擇報單號碼D、分提單號碼H
                        { "queryType", "D" },
                        //分提單號碼
                        { "hawb", "" }
                    };

                var result = GetGb353(parameters);

                retryCount = 0;
                do
                {
                    if (result.Msg.Contains("發生一或多項錯誤。"))
                    {
                        Thread.Sleep(1000);
                        result = GetGb353(parameters);
                        retryCount++;
                    }
                    else
                    {
                        break;
                    }
                } while (retryCount < 3);

                r.CptNo = "Gb353";
                r.Msg = result.Msg;
                if (result.Status == "ok")
                {
                    var last = result.Data.OrderByDescending(x => x.IssueDate).ThenByDescending(x => x.IssueTime).FirstOrDefault();
                    r.IssueDate = last.IssueDate;
                    r.IssueTime = last.IssueTime;
                    r.TrackingNo = last.Hawb;
                    r.RejReasonCode = last.RejReasonCode;
                    r.RejReasonDesc = last.RejReasonDesc;

                    var data = result.Data
                        .OrderByDescending(x => x.IssueDate)
                        .ThenByDescending(x => x.IssueTime)
                        .Select(
                        x => new
                        {
                            x.IssueDate,
                            x.IssueTime,
                            x.Hawb,
                            x.RejReasonCode,
                            x.RejReasonDesc,
                        }).ToList();

                    var otherProType = data.Select(x =>
                    {
                        if (DateTime.TryParseExact($"{x.IssueDate}{x.IssueTime}", "yyyyMMddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                        {
                            return new
                            {
                                IssueDateTime = dateTime.ToString("yyyy/MM/dd HH:mm:ss"),
                                Hawb = x.Hawb,
                                RejReasonCode = x.RejReasonCode,
                                RejReasonDesc = x.RejReasonDesc,
                            };
                        }
                        else
                        {
                            return new
                            {
                                IssueDateTime = "",
                                Hawb = x.Hawb,
                                RejReasonCode = x.RejReasonCode,
                                RejReasonDesc = x.RejReasonDesc,
                            };
                        }
                    }).ToList();


                    r.OtherProType = string.Join("\r\n", otherProType
                        .OrderByDescending(x => x.IssueDateTime)
                        .Select(x =>
                            $"{x.Hawb}，{x.IssueDateTime}，{x.RejReasonCode}，{x.RejReasonDesc}"
                        ).ToList());
                }
                Thread.Sleep(300);
            });
        }

        /// <summary>
        /// 讀取空運-正式報單查詢Excel
        /// </summary>
        void SearchEtlClearanceOrder(List<EtlClearanceOrderModel> list) 
        {
            list.ForEach(r =>
            {
                var parameters = new Dictionary<string, string>
                            {
                                { "declNo", r.ClearanceNumber},
                            };

                var result = _cptPortalApi.GetGb301(parameters);

                r.Gb301Msg = result.Msg;
                r.Gb301ReceiveOrder = result.GridModel?.Any(x => x.ProcEventCodeStr.Contains("收單建檔")) == true ? "收單建檔" : "";
                
                if (string.IsNullOrEmpty(r.Gb301ReceiveOrder))
                {
                    //查詢Gb302
                    var gb302Result = _cptPortalApi.GetGb302(parameters);
                    r.Gb302Msg = gb302Result.Msg;

                    r.GridModel = gb302Result?.GridModel;
                }
            });


        }

        /// <summary>
        /// 銷艙率查詢
        /// </summary>
        /// <param name="list"></param>
        void Search(List<CargoManifestModel> list)
        {
            list.ForEach(r =>
            {
                var parameters = new Dictionary<string, string>
                    {
                        //海關通關號碼
                        { "vslRegNo", r.VslRegNo }, 
                        //卸存地代碼
                        { "storWareCd", r.StorWareCd },
                        //貨櫃號碼
                        { "containerNo", r.ContainerNo },
                    };

                var result = GetGb378(parameters);
                r.Gb378Msg = result.msg;
                var firstItem = result.data?.FirstOrDefault();
                if (firstItem != null)
                {
                    r.ImCmRate = firstItem.imCmRate?.ToString();
                }
                Thread.Sleep(300);
            });
        }

        /// <summary>
        /// 新增上傳海運主號
        /// </summary>
        /// <param name="list"></param>
        /// <param name="uploadTime"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertCptSeaMainNumber(List<string> list, string uploadTime, string userId)
        {
            var sql = @"
                        insert [jetf].[dbo].[CptSeaMainNumber](MainNumber, UploadOpe, UploadTime)
                        values (@MainNumber, @UploadOpe, @UploadTime)";

            //新增上傳主號的分提單號
            var sql2 = @"
                            insert [jetf].[dbo].CptSeaMainNumberDetail(MainNumber,BagNumber,JetfSerial)
                            select a.MainNumber,b.BL_NO,b.JETF_SERIAL as BagNumber from [jetf].[dbo].[CptSeaMainNumber] a
                            join DATA_CENTER.dbo.SEA_ORDER_ORIGINAL b on a.MainNumber=b.MAINNUMBER
                            where UploadOpe=@UploadOpe and UploadTime=@UploadTime
                            and b.GW > 0 and b.STATUS<>'E'
                            and not exists (
                            select 1 from  [jetf].[dbo].CptSeaMainNumberDetail
                            where MainNumber = b.MainNumber and BagNumber = b.BL_NO
                            )
                        ";

            //使用dapper
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    list.ForEach(r =>
                    {
                        conn.Execute(sql, new { MainNumber = r, UploadOpe = userId, UploadTime = uploadTime }, tran);
                    });

                    conn.Execute(sql2, new { UploadOpe = userId, UploadTime = uploadTime }, tran, commandTimeout: 600);

                    tran.Commit();

                    return new ResponseModel();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return new ResponseModel(ex.Message);
                }
            }
        }

        /// <summary>
        /// 新增Gb378結果
        /// </summary>
        /// <param name="list"></param>
        /// <param name="uploadTime"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertCptGb378(List<CargoManifestModel> list, string uploadTime, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            string sql = @"
                            insert [jetf].[dbo].[CptGb378](MainNumber, ContainerNo, VslRegNo, StorWareCd, ImCmRate, Gb378Msg, UploadOpe, UploadTime)
                            values (@MainNumber, @ContainerNo, @VslRegNo, @StorWareCd, @ImCmRate, @Gb378Msg, @UploadOpe, @UploadTime)";

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
                            cmd.Parameters.Add("@MainNumber", SqlDbType.NVarChar).Value = (object)r.MainNumber ?? DBNull.Value;
                            cmd.Parameters.Add("@ContainerNo", SqlDbType.NVarChar).Value = r.ContainerNo;
                            cmd.Parameters.Add("@VslRegNo", SqlDbType.NVarChar).Value = r.VslRegNo;
                            cmd.Parameters.Add("@StorWareCd", SqlDbType.NVarChar).Value = r.StorWareCd;
                            cmd.Parameters.Add("@ImCmRate", SqlDbType.NVarChar).Value = (object)r.ImCmRate ?? DBNull.Value;
                            cmd.Parameters.Add("@Gb378Msg", SqlDbType.NVarChar).Value = (object)r.Gb378Msg ?? DBNull.Value;
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

        /// <summary>
        /// 取得Gb378結果
        /// </summary>
        /// <param name="uploadTime"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<CargoManifestModel> GetCptGb378(string uploadTime, string userId)
        {
            string sql = @"
                            with Cte_SeaMainCount as
                            (
	                            select 
	                            MAIN_NUMBER,CUST_CODE,
	                            count(MAIN_NUMBER) as 'TotalCount',
	                            sum(RESULT_4_VALUE) as 'ResultCount',
	                            sum(REPLY_VALUE) as 'ReplyCount',
	                            sum(case when RESULT_4_VALUE = 1 and REPLY_VALUE = 0 then 1 else 0 end) as 'NotReplyCount',
	                            sum(case when RESULT_4_VALUE = 1 and REPLY_VALUE = 0 then PIECE else 0 end) as 'NotPieceCount'
	                            from [DATA_CENTER].[dbo].[VIEW_SEA_MAIN_COUNT] a
	                            where exists (select 1 from [jetf].[dbo].[CptGb378] where UploadOpe=@UploadOpe and UploadTime =@UploadTime and MainNumber=a.MAIN_NUMBER )
	                            group by MAIN_NUMBER,CUST_CODE
                            )
                            select a.MainNumber, a.ContainerNo, a.VslRegNo, a.StorWareCd, a.ImCmRate, a.Gb378Msg,
	                               b.CUST_CODE,b.TotalCount,b.ResultCount,b.ReplyCount,b.NotReplyCount,b.NotPieceCount,c.CUST_NAME from  [jetf].[dbo].[CptGb378] a
                            left join Cte_SeaMainCount b on a.MainNumber =b.MAIN_NUMBER
                            left join [DATA_CENTER].[dbo].SYS_CUST c on b.CUST_CODE=c.CUST_CODE
                            where UploadOpe=@UploadOpe and UploadTime =@UploadTime ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = uploadTime;
                da.SelectCommand.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = userId;
                da.Fill(dt);
            }

            List<CargoManifestModel> list = new List<CargoManifestModel>();

            dt.AsEnumerable().ToList().ForEach(r =>
            {
                list.Add(new CargoManifestModel()
                {
                    MainNumber = r.Field<string>("MainNumber"),
                    ContainerNo = r.Field<string>("ContainerNo"),
                    VslRegNo = r.Field<string>("VslRegNo"),
                    StorWareCd = r.Field<string>("StorWareCd"),
                    ImCmRate = r.Field<string>("ImCmRate"),
                    Gb378Msg = r.Field<string>("Gb378Msg"),
                    CustName = r.Field<string>("CUST_NAME"),
                    TotalCount = r.Field<int?>("TotalCount"),
                    ResultCount = r.Field<int?>("ResultCount"),
                    ReplyCount = r.Field<int?>("ReplyCount"),
                    NotReplyCount = r.Field<int?>("NotReplyCount"),
                    NotPieceCount = r.Field<int?>("NotPieceCount"),
                }); ;
            });

            return list;
        }

        /// <summary>
        /// 取得須預委未按明細
        /// </summary>
        /// <param name="uploadTime"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<NotReplyDetailModel> GetNotReplyDetail(string uploadTime, string userId)
        {
            string sql = @"
                            select FIELD_DATE,MAIN_NUMBER,HAWB_NO,REPLY_CODE from [DATA_CENTER].[dbo].[VIEW_SEA_MAIN_COUNT] a
                            where RESULT_4_VALUE = 1 and REPLY_VALUE = 0 and exists 
                            (select 1 from [jetf].[dbo].[CptGb378] where UploadOpe = @UploadOpe and UploadTime = @UploadTime 
                            and MainNumber=a.MAIN_NUMBER )";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = uploadTime;
                da.SelectCommand.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = userId;
                da.Fill(dt);
            }

            List<NotReplyDetailModel> list = new List<NotReplyDetailModel>();
            dt.AsEnumerable().ToList().ForEach(r =>
            {
                list.Add(new NotReplyDetailModel()
                {
                    MainNumber = r.Field<string>("MAIN_NUMBER"),
                    TrackingNo = r.Field<string>("HAWB_NO"),
                    EtaDate = r.Field<DateTime?>("FIELD_DATE"),
                    Status = r.Field<string>("REPLY_CODE"),
                }); ;
            });

            return list;
        }

        /// <summary>
        /// 取得總票數
        /// </summary>
        /// <returns></returns>
        void GetSeaMainCount(List<CargoManifestModel> list)
        {
            DataTable dt = new DataTable();

            string sql = $@"
                             select 
                             MAIN_NUMBER,CUST_CODE,
                             count(MAIN_NUMBER) as 'TotalCount',
                             sum(RESULT_4_VALUE) as 'ResultCount',
                             sum(REPLY_VALUE) as 'ReplyCount',
                             sum(case when RESULT_4_VALUE = 1 and REPLY_VALUE = 0 then 1 else 0 end) as 'NotReplyCount',
                             sum(case when RESULT_4_VALUE = 1 and REPLY_VALUE = 0 then PIECE else 0 end) as 'NotPieceCount'
                             from [DATA_CENTER].[dbo].[VIEW_SEA_MAIN_COUNT]
                             where MAIN_NUMBER in (${string.Join(",", list.Select(r => r.MainNumber).ToList())})
                             group by MAIN_NUMBER,CUST_CODE"
                           ;
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.CommandTimeout = 300;
                da.Fill(dt);
            }
        }

        /// <summary>
        /// 取得收單查詢Excel
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        IWorkbook GetReceiveOrderSearchExcel(List<ReceiveOrderModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("收單查詢");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("CPT代碼");
            row.CreateCell(1).SetCellValue("主提單號");
            row.CreateCell(2).SetCellValue("分提單號");
            row.CreateCell(3).SetCellValue("狀態列");
            row.CreateCell(4).SetCellValue("事件發生日期(最新)");
            row.CreateCell(5).SetCellValue("時間");
            row.CreateCell(6).SetCellValue("處理狀況(最新)");
            row.CreateCell(7).SetCellValue("其他處理狀況(依新-->舊)、分隔 ");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 20000);


            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.CptNo);
                row.CreateCell(1).SetCellValue(r.MainNumber);
                row.CreateCell(2).SetCellValue(r.TrackingNo);
                row.CreateCell(3).SetCellValue(r.Msg);
                if (DateTime.TryParseExact($"{r.ProDate}{r.ProTime}", "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                {
                    row.CreateCell(4).SetCellValue(dateTime.ToString("yyyy/MM/dd"));
                    row.CreateCell(5).SetCellValue(dateTime.ToString("HH:mm"));
                }
                row.CreateCell(6).SetCellValue(r.ProType);
                row.CreateCell(7).SetCellValue(r.OtherProType);
                row.GetCell(7).CellStyle = styleWrapText;
                iRow++;
            });


            return workbook;

        }

        /// <summary>
        /// 取得錯單查詢Excel
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        IWorkbook GetErrorOrderSearchExcel(List<ErrorOrderModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("錯單查詢");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主提單號");
            row.CreateCell(1).SetCellValue("分提單號");
            row.CreateCell(2).SetCellValue("狀態列");
            row.CreateCell(3).SetCellValue("處理日期時間(最新)");
            row.CreateCell(4).SetCellValue("錯誤原因代碼(最新)");
            row.CreateCell(5).SetCellValue("錯誤原因說明(最新)");
            row.CreateCell(6).SetCellValue("錯誤原因說明(依新-->舊)、分隔 ");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 20000);
            sheet.SetColumnWidth(6, 30000);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.MainNumber);
                row.CreateCell(1).SetCellValue(r.TrackingNo);
                row.CreateCell(2).SetCellValue(r.Msg);
                if (DateTime.TryParseExact($"{r.IssueDate}{r.IssueTime}", "yyyyMMddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                {
                    row.CreateCell(3).SetCellValue(dateTime.ToString("yyyy/MM/dd HH:mm:ss"));
                }
                row.CreateCell(4).SetCellValue(r.RejReasonCode);
                row.CreateCell(5).SetCellValue(r.RejReasonDesc);
                row.CreateCell(6).SetCellValue(r.OtherProType);
                row.GetCell(6).CellStyle = styleWrapText;
                iRow++;
            });

            return workbook;
        }

        /// <summary>
        /// 取得空運-正式報單查詢Excel
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        IWorkbook GetEtlClearanceOrderExcel(List<EtlClearanceOrderModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行

            ISheet sheet = workbook.CreateSheet("正式報單查詢");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主提單號");
            row.CreateCell(1).SetCellValue("分提單號");
            row.CreateCell(2).SetCellValue("報單號碼");
            row.CreateCell(3).SetCellValue("狀態");
            row.CreateCell(4).SetCellValue("不受理原因(項次)");
            row.CreateCell(5).SetCellValue("不受理原因(時間)");
            row.CreateCell(6).SetCellValue("不受理原因(狀態)");
            row.CreateCell(7).SetCellValue("品名");
            row.CreateCell(8).SetCellValue("稅則");
            row.CreateCell(9).SetCellValue("Gb301查詢結果");
            row.CreateCell(10).SetCellValue("Gb302查詢結果");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 12000);
            sheet.SetColumnWidth(7, 12000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 6000);
            sheet.SetColumnWidth(10, 6000);

            int iRow = 1;
            list.ForEach(r =>
            {
                if (r.GridModel != null && r.GridModel.Any())
                {
                    foreach (var x in r.GridModel)
                    {
                        row = sheet.CreateRow(iRow);
                        row.CreateCell(0).SetCellValue(r.MainNumber);
                        row.CreateCell(1).SetCellValue(r.TrackingNo);
                        row.CreateCell(2).SetCellValue(r.ClearanceNumber);
                        row.CreateCell(3).SetCellValue(r.Gb301ReceiveOrder);

                        // GridModel 的欄位
                        row.CreateCell(4).SetCellValue(x.ItemNo);
                        row.CreateCell(5).SetCellValue(x.NoticeDateTime);
                        row.CreateCell(6).SetCellValue(x.Reason);
                        row.CreateCell(7).SetCellValue(x.Item);
                        row.CreateCell(8).SetCellValue(x.CCCCode);

                        row.CreateCell(9).SetCellValue(r.Gb301Msg);
                        row.CreateCell(10).SetCellValue(r.Gb302Msg);

                        iRow++;
                    }
                }
                else
                {
                    // 沒有 GridModel，就單獨一列
                    row = sheet.CreateRow(iRow);
                    row.CreateCell(0).SetCellValue(r.MainNumber);
                    row.CreateCell(1).SetCellValue(r.TrackingNo);
                    row.CreateCell(2).SetCellValue(r.ClearanceNumber);
                    row.CreateCell(3).SetCellValue(r.Gb301ReceiveOrder);

                    row.CreateCell(9).SetCellValue(r.Gb301Msg);
                    row.CreateCell(10).SetCellValue(r.Gb302Msg);

                    iRow++;
                }
            });

            return workbook;
        }


        /// <summary>
        /// 取得空運錯單查詢Excel
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        IWorkbook GetEtlErrorOrderSearchExcel(List<EtlErrorOrderModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("錯單查詢");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主提單號");
            row.CreateCell(1).SetCellValue("報單號碼");
            row.CreateCell(2).SetCellValue("狀態列");
            row.CreateCell(3).SetCellValue("分提單號(最新)");
            row.CreateCell(4).SetCellValue("處理日期時間(最新)");
            row.CreateCell(5).SetCellValue("錯誤原因代碼(最新)");
            row.CreateCell(6).SetCellValue("錯誤原因說明(最新)");
            row.CreateCell(7).SetCellValue("錯誤原因說明(依新-->舊)、分隔 ");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 20000);
            sheet.SetColumnWidth(7, 30000);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.MainNumber);
                row.CreateCell(1).SetCellValue(r.ClearanceNo);
                row.CreateCell(2).SetCellValue(r.Msg);
                row.CreateCell(3).SetCellValue(r.TrackingNo);
                if (DateTime.TryParseExact($"{r.IssueDate}{r.IssueTime}", "yyyyMMddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                {
                    row.CreateCell(4).SetCellValue(dateTime.ToString("yyyy/MM/dd HH:mm:ss"));
                }
                row.CreateCell(5).SetCellValue(r.RejReasonCode);
                row.CreateCell(6).SetCellValue(r.RejReasonDesc);
                row.CreateCell(7).SetCellValue(r.OtherProType);
                row.GetCell(7).CellStyle = styleWrapText;
                iRow++;
            });

            return workbook;
        }

        /// <summary>
        /// 取得海運主號查詢Excel
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
       public IWorkbook GetCptSeaMainNumberDetailExcel(List<CptSeaMainNumberDetailModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("海運主號查詢");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主提單號");
            row.CreateCell(1).SetCellValue("分提單號");
            row.CreateCell(2).SetCellValue("是否連線收單建檔");
            row.CreateCell(3).SetCellValue("Gb321查詢狀態");
            row.CreateCell(4).SetCellValue("Gb321訊息");
            row.CreateCell(5).SetCellValue("Gb321時間(最新)");
            row.CreateCell(6).SetCellValue("Gb321處理狀況(最新)");
            row.CreateCell(7).SetCellValue("Gb321執行時間");
            row.CreateCell(8).SetCellValue("Gb353查詢狀態");
            row.CreateCell(9).SetCellValue("Gb353訊息");
            row.CreateCell(10).SetCellValue("GB353錯單時間(最新)");
            row.CreateCell(11).SetCellValue("錯單原因代碼");
            row.CreateCell(12).SetCellValue("錯單原因說明");
            row.CreateCell(13).SetCellValue("Gb353執行時間");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 6000);
            sheet.SetColumnWidth(10, 6000);
            sheet.SetColumnWidth(11, 6000);
            sheet.SetColumnWidth(12, 16000);
            sheet.SetColumnWidth(13, 6000);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.MainNumber);
                row.CreateCell(1).SetCellValue(r.BagNumber);
                row.CreateCell(2).SetCellValue(r.IsReceiveOrder ? "連線收單建檔" : "");
                row.CreateCell(3).SetCellValue(r.Gb321Status);
                row.CreateCell(4).SetCellValue(r.Gb321Msg);
                row.CreateCell(5).SetCellValue(r.Gb321ProDateTime);
                row.CreateCell(6).SetCellValue(r.Gb321ProType);
                row.CreateCell(7).SetCellValue(r.UpdateGb321Time?.ToString("yyyy-MM-dd HH:mm:ss"));
                row.CreateCell(8).SetCellValue(r.Gb353Status);
                row.CreateCell(9).SetCellValue(r.Gb353Msg);
                row.CreateCell(10).SetCellValue(r.Gb353IssueDateTime);
                row.CreateCell(11).SetCellValue(r.Gb353RejReasonCode);
                row.CreateCell(12).SetCellValue(r.Gb353RejReasonDesc);
                row.CreateCell(13).SetCellValue(r.UpdateGb353Time?.ToString("yyyy-MM-dd HH:mm:ss"));

                row.GetCell(11).CellStyle = styleWrapText;
                row.GetCell(12).CellStyle = styleWrapText;
                iRow++;
            });

            return workbook;
        }

        /// <summary>
        /// 取得海運收單查詢(海快作業)Excel
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public IWorkbook GetCptReceiveOrderSearchWorkExcel(List<CptSeaMainNumberDetailModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("海運收單查詢");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主提單號");
            row.CreateCell(1).SetCellValue("分提單號");
            row.CreateCell(2).SetCellValue("是否連線收單建檔");
            row.CreateCell(3).SetCellValue("Gb321查詢狀態");
            row.CreateCell(4).SetCellValue("Gb321訊息");
            row.CreateCell(5).SetCellValue("Gb321時間(最新)");
            row.CreateCell(6).SetCellValue("Gb321處理狀況(最新)");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.MainNumber);
                row.CreateCell(1).SetCellValue(r.BagNumber);
                row.CreateCell(2).SetCellValue(r.IsReceiveOrder ? "連線收單建檔" : "");
                row.CreateCell(3).SetCellValue(r.Gb321Status);
                row.CreateCell(4).SetCellValue(r.Gb321Msg);
                row.CreateCell(5).SetCellValue(r.Gb321ProDateTime);
                row.CreateCell(6).SetCellValue(r.Gb321ProType);
                iRow++;
            });

            return workbook;
        }

        /// <summary>
        /// 取得海運錯單查詢(海快作業)Excel
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public IWorkbook GetCptErrorOrderSearchWorkExcel(List<CptSeaMainNumberDetailModel> list)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("海運主號查詢");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("主提單號");
            row.CreateCell(1).SetCellValue("分提單號");
            row.CreateCell(2).SetCellValue("Gb353查詢狀態");
            row.CreateCell(3).SetCellValue("Gb353訊息");
            row.CreateCell(4).SetCellValue("GB353錯單時間(最新)");
            row.CreateCell(5).SetCellValue("錯單原因代碼");
            row.CreateCell(6).SetCellValue("錯單原因說明");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 16000);

            // 設置儲存格樣式
            ICellStyle styleWrapText = workbook.CreateCellStyle();
            styleWrapText.WrapText = true; // 啟用文字換行

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.MainNumber);
                row.CreateCell(1).SetCellValue(r.BagNumber);
                row.CreateCell(2).SetCellValue(r.Gb353Status);
                row.CreateCell(3).SetCellValue(r.Gb353Msg);
                row.CreateCell(4).SetCellValue(r.Gb353IssueDateTime);
                row.CreateCell(5).SetCellValue(r.Gb353RejReasonCode);
                row.CreateCell(6).SetCellValue(r.Gb353RejReasonDesc);

                row.GetCell(5).CellStyle = styleWrapText;
                row.GetCell(6).CellStyle = styleWrapText;
                iRow++;
            });

            return workbook;
        }

        /// <summary>
        /// 取得銷艙率Sheet
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        void GetCargoManifestSheet(IWorkbook workbook, List<CargoManifestModel> list)
        {
            ISheet sheet = workbook.CreateSheet("銷艙率");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("客戶");
            row.CreateCell(1).SetCellValue("航班主號");
            row.CreateCell(2).SetCellValue("海關通關號碼");
            row.CreateCell(3).SetCellValue("船名");
            row.CreateCell(4).SetCellValue("船舶航次");
            row.CreateCell(5).SetCellValue("貨櫃號碼");
            row.CreateCell(6).SetCellValue("卸存地代碼");
            row.CreateCell(7).SetCellValue("主號總票數");
            row.CreateCell(8).SetCellValue("需預委票數");
            row.CreateCell(9).SetCellValue("已按預委票數");
            row.CreateCell(10).SetCellValue("未按預票數");
            row.CreateCell(11).SetCellValue("未按預委件數");
            row.CreateCell(12).SetCellValue("銷倉率");
            row.CreateCell(13).SetCellValue("Gb378狀態");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 6000);
            sheet.SetColumnWidth(10, 6000);
            sheet.SetColumnWidth(11, 6000);
            sheet.SetColumnWidth(12, 6000);
            sheet.SetColumnWidth(13, 6000);

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.CustName);
                row.CreateCell(1).SetCellValue(r.MainNumber);
                row.CreateCell(2).SetCellValue(r.VslRegNo);
                row.CreateCell(5).SetCellValue(r.ContainerNo);
                row.CreateCell(6).SetCellValue(r.StorWareCd);
                row.CreateCell(7).SetCellValue(r.TotalCount.HasValue ? r.TotalCount.Value : 0);
                row.CreateCell(8).SetCellValue(r.ResultCount.HasValue ? r.ResultCount.Value : 0);
                row.CreateCell(9).SetCellValue(r.ReplyCount.HasValue ? r.ReplyCount.Value : 0);
                row.CreateCell(10).SetCellValue(r.NotReplyCount.HasValue ? r.NotReplyCount.Value : 0);
                row.CreateCell(11).SetCellValue(r.NotPieceCount.HasValue ? r.NotPieceCount.Value : 0);
                if (!string.IsNullOrEmpty(r.ImCmRate))
                    row.CreateCell(12).SetCellValue($"{r.ImCmRate}%");
                row.CreateCell(13).SetCellValue(r.Gb378Msg);
                iRow++;
            });
        }

        /// <summary>
        /// 取得須預委未按明細
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        void GetNotReplyDetailSheet(IWorkbook workbook, List<NotReplyDetailModel> list)
        {
            ISheet sheet = workbook.CreateSheet("須預委未按明細");

            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("到港日");
            row.CreateCell(1).SetCellValue("主號");
            row.CreateCell(2).SetCellValue("分提單號");
            row.CreateCell(3).SetCellValue("狀態");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);

            int iRow = 1;
            list.ForEach(r =>
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(r.EtaDate.HasValue ? r.EtaDate.Value.ToString("yyyy/MM/dd") : "");
                row.CreateCell(1).SetCellValue(r.MainNumber);
                row.CreateCell(2).SetCellValue(r.TrackingNo);
                row.CreateCell(3).SetCellValue(r.Status);
                iRow++;
            });
        }

        /// <summary>
        /// 讀取收單Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<ReceiveOrderModel> ReadReceiveOrderExcel(string filePath)
        {
            bool read = false;

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }


            var list = new List<ReceiveOrderModel>();
            var sheet = workbook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    var item = new ReceiveOrderModel();
                    item.MainNumber = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    item.TrackingNo = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主提單號") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "分提單號"))
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

        /// <summary>
        /// 讀取錯單Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<ErrorOrderModel> ReadErrorOrderExcel(string filePath)
        {
            bool read = false;

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }


            var list = new List<ErrorOrderModel>();
            var sheet = workbook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    var item = new ErrorOrderModel();
                    item.MainNumber = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    item.TrackingNo = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主提單號") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "分提單號"))
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

        /// <summary>
        /// 讀取空運-正式報單查詢Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<EtlClearanceOrderModel> ReadEtlClearanceOrderExcel(string filePath)
        {
            bool read = false;

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }


            var list = new List<EtlClearanceOrderModel>();
            var sheet = workbook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    var item = new EtlClearanceOrderModel();
                    item.MainNumber = sheet.GetRow(i).GetCellData(0);
                    item.TrackingNo = sheet.GetRow(i).GetCellData(1);
                    item.ClearanceNumber = sheet.GetRow(i).GetCellData(2);

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主提單號") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "分提單號"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && !string.IsNullOrEmpty(item.ClearanceNumber))
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }


        /// <summary>
        /// 讀取空運錯單Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<EtlErrorOrderModel> ReadEtlErrorOrderExcel(string filePath)
        {
            bool read = false;

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }

            var list = new List<EtlErrorOrderModel>();
            var sheet = workbook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    var item = new EtlErrorOrderModel();
                    item.MainNumber = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    item.ClearanceNo = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "主提單號"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && !string.IsNullOrEmpty(item.ClearanceNo))
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 讀取銷艙率Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<CargoManifestModel> ReadCargoManifestExcel(string filePath)
        {
            bool read = false;

            IWorkbook workbook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fs);
            }

            var list = new List<CargoManifestModel>();
            var sheet = workbook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    var item = new CargoManifestModel();
                    item.VslRegNo = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    item.ContainerNo = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    item.StorWareCd = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    item.MainNumber = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "海關通關號碼") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "貨櫃號碼") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "卸存地代碼"))
                    {
                        read = true;
                        continue;
                    }
                    if (read && !string.IsNullOrEmpty(item.VslRegNo))
                    {
                        item.ContainerNo = item.ContainerNo.Length > 11 ? item.ContainerNo.Substring(0, 11) : item.ContainerNo;

                        list.Add(item);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 讀取資料
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<string> ReadData(string data)
        {
            return data
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct()
            .ToList();
        }

        /// <summary>
        /// 是否為後段報關
        /// </summary>
        /// <returns></returns>
        bool IsPostEntry(DataTable dt)
        {
            var result = dt.AsEnumerable().Any(r => r.Field<string>("POST_ENTRY") != null &&
                                                    r.Field<string>("POST_ENTRY").Contains("G1"));
            return result;
        }

        /// <summary>
        /// 取得海快原單資料
        /// </summary>
        /// <returns></returns>
        DataTable GetSeaOrderOriginal(string blNo)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select POST_ENTRY,MAINNUMBER,MODIFYBY from DATA_CENTER.[dbo].[SEA_ORDER_ORIGINAL] where BL_NO=@BL_NO", conn))
            {
                da.SelectCommand.Parameters.Add("@BL_NO", SqlDbType.NVarChar).Value = blNo;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得海快主號查詢資料
        /// </summary>
        /// <param name="uploadOpe"></param>
        /// <param name="uploadTime"></param>
        /// <returns></returns>
        public List<CptSeaMainNumberDetailModel> GetCptSeaMainNumberDetails(string uploadTime, string uploadOpe)
        {
            var sql = @"select * from [jetf].[dbo].[CptSeaMainNumberDetail] a
                        where exists 
                        (
                          select * from [jetf].[dbo].[CptSeaMainNumber]
                          where MainNumber = a.MainNumber and UploadOpe=@UploadOpe and UploadTime=@UploadTime
                        )";

            return conn.Query<CptSeaMainNumberDetailModel>(sql,
                new
                {
                    UploadOpe = uploadOpe,
                    UploadTime = uploadTime
                }).ToList();
        }

        /// <summary>
        /// 11.(GB321)進口簡易申報收單作業結果查詢
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb321Model GetGb321(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 20;
            Gb321Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB321!query";

                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb321Model>(response.Content.ReadAsStringAsync().Result);
                        }
                        else
                        {
                            result = new Gb321Model() { Msg = "(GB321)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Gb321Model() { Msg = ex.Message };
                }

                if (result != null && (result.Msg.Contains("執行成功") || result.Msg.Contains("查無資料")))
                {
                    break;
                }

                retryCount++;
                Thread.Sleep(1000);
            } while (retryCount < maxRetries);

            return result;
        }

        /// <summary>
        /// 1.(GB301)進口報單通關流程查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb301Model GetGb301(Dictionary<string, string> parameters)
        {
            try
            {
                //關貿Api
                string url = "https://portal.sw.nat.gov.tw/APGQ/GB301!queryAir";

                using (HttpClient client = new HttpClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    //將參數轉換成 FormUrlEncodedContent
                    var content = new FormUrlEncodedContent(parameters);
                    //發送 POST 請求
                    HttpResponseMessage response = client.PostAsync(url, content).Result;

                    // 檢查請求是否成功
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<Gb301Model>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        return new Gb301Model() { Msg = "(GB301)進口報單通關流程查詢失敗，請重新查詢" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new Gb301Model() { Msg = ex.Message };
            }
        }

        /// <summary>
        /// 1.(GB330)海關通關號碼查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb330Model GetGb330(Dictionary<string, string> parameters)
        {
            //var parameters = new Dictionary<string, string>
            //        {
            //            { "vslRegNo", "" },
            //            { "choice", "N" },
            //            { "shipName", r.ShipName },
            //            { "voyageFlightNo", r.VoyageFlightNo },
            //            { "voyageNo", "" },
            //            { "estArDate", "" },
            //            { "shipCoCd", "" },
            //            { "estArDateB", "" },
            //            { "estArDateE", "" },
            //            { "custCd", "" },
            //            { "clearanceDateB", "" },
            //            { "clearanceDateE", "" },
            //            { "shipClass", "A" },
            //            { "custCd_P", "" },
            //            { "estArDateB_P", "" },
            //            { "estArDateE_P", "" },
            //            { "shipCoCd_P", "" }
            //        };

            //關貿Api
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB330!query";
            int retryCount = 3;

            Gb330Model result = new Gb330Model();
            do
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb330Model>(response.Content.ReadAsStringAsync().Result);
                            if (result.Msg.Contains("發生一或多項錯誤。"))
                            {

                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            result.Msg = "(GB330)海關通關號碼查詢，請重新查詢";
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Msg = ex.Message;
                }

                retryCount--;
                Thread.Sleep(1000);
            } while (retryCount > 0);

            return result;
        }

        /// <summary>
        /// (GB378)海運貨櫃銷艙比例查詢作業
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb378Model GetGb378(Dictionary<string, string> parameters)
        {
            //關貿Api
            string url = "https://portal.sw.nat.gov.tw/APGQ/GB378!query";
            int retryCount = 3;

            Gb378Model result = new Gb378Model();
            do
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb378Model>(response.Content.ReadAsStringAsync().Result);
                            if (result.msg.Contains("發生一或多項錯誤。"))
                            {

                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            result.msg = "(GB378)海運貨櫃銷艙比例查詢作業，請重新查詢";
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.msg = ex.Message;
                }

                retryCount--;
                Thread.Sleep(1000);
            } while (retryCount > 0);

            return result;
        }

        /// <summary>
        /// 17.(GB353)進口簡易報單錯單查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb353Model GetGb353(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 20;
            Gb353Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB353!query";

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb353Model>(response.Content.ReadAsStringAsync().Result);
                        }
                        else
                        {
                            result = new Gb353Model() { Msg = "(GB353)進口簡易報單錯單查詢，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Gb353Model() { Msg = ex.Message };
                }

                if (result != null && (result.Msg.Contains("執行成功") || result.Msg.Contains("查無資料")))
                {
                    break;
                }
                retryCount++;
                Thread.Sleep(1000);
            } while (retryCount < maxRetries);

            return result;
        }

        /// <summary>
        /// (GB350)空運進口貨物新艙單資料查詢
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb350Model GetGb350(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 3;
            Gb350Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryMawb";

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //將參數轉換成 FormUrlEncodedContent
                        var content = new FormUrlEncodedContent(parameters);
                        //發送 POST 請求
                        HttpResponseMessage response = client.PostAsync(url, content).Result;

                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb350Model>(response.Content.ReadAsStringAsync().Result);
                        }
                        else
                        {
                            result = new Gb350Model() { Msg = "(GB350)空運進口貨物新艙單資料查詢，請重新查詢" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Gb350Model() { Msg = ex.Message };
                }

                if (result != null && result.Msg.Contains("查詢成功"))
                {
                    break;
                }

                retryCount++;
            } while (retryCount < maxRetries);

            return result;
        }

        public async Task<Gb321Model> GetGb321Async(Dictionary<string, string> parameters)
        {
            int retryCount = 0;
            const int maxRetries = 5;
            Gb321Model result = null;
            do
            {
                try
                {
                    //關貿Api
                    string url = "https://portal.sw.nat.gov.tw/APGQ/GB321!query";
                    //將參數轉換成 FormUrlEncodedContent
                    var content = new FormUrlEncodedContent(parameters);
                    //發送 POST 請求
                    //_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (HttpResponseMessage response = await _httpClient.PostAsync(url, content))
                    {
                        // 檢查請求是否成功
                        if (response.IsSuccessStatusCode)
                        {
                            result = JsonConvert.DeserializeObject<Gb321Model>(await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            result = new Gb321Model() { Msg = "(GB321)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                        }
                    }

                }
                catch (Exception ex)
                {
                    result = new Gb321Model() { Msg = ex.Message };
                }

                if (result != null && result.Msg.Contains("執行成功"))
                {
                    break;
                }

                retryCount++;
                await Task.Delay(5000); 
            } while (retryCount < maxRetries);

            return result;
        }

        public void RunCptSeaMainNumberJobAsync(string uploadTime, string uploadOpe)
        {
            var gb321List = GetGb321CptSeaMainNumberDetails(uploadTime, uploadOpe);
            SearchGb321(gb321List);

            var gb353List = GetGb353CptSeaMainNumberDetails(uploadTime, uploadOpe);
            SearchGb353(gb353List);

            var gb326List = GetGb326CptSeaMainNumberDetails(uploadTime, uploadOpe);
            SearchGb326(gb326List);
        }

        public List<string> GetGb326CptSeaMainNumberDetails(string uploadTime, string uploadOpe)
        {
            var sql = @"SELECT distinct MainNumber FROM [jetf].[dbo].CptSeaMainNumberDetail a 
                        where Gb326ImportDate is null
                        and exists 
                        (
                             select * from [jetf].[dbo].[CptSeaMainNumber]
                             where MainNumber = a.MainNumber and UploadOpe=@UploadOpe and UploadTime=@UploadTime
                        )";

            return conn.Query<string>(sql,
                new
                {
                    UploadTime = uploadTime,
                    UploadOpe = uploadOpe
                }).ToList();
        }

        public List<CptSeaMainNumberDetailModel> GetGb321CptSeaMainNumberDetails(string uploadTime, string uploadOpe)
        {
            var sql = @"SELECT * FROM [jetf].[dbo].CptSeaMainNumberDetail a 
                        where IsReceiveOrder='0'
                        and exists 
                        (
                          select * from [jetf].[dbo].[CptSeaMainNumber]
                          where MainNumber = a.MainNumber and UploadOpe=@UploadOpe and UploadTime=@UploadTime
                        )";

            return conn.Query<CptSeaMainNumberDetailModel>(sql,
                new
                {
                    UploadTime = uploadTime,
                    UploadOpe = uploadOpe
                }).ToList();

        }

        public List<CptSeaMainNumberDetailModel> GetGb353CptSeaMainNumberDetails(string uploadTime, string uploadOpe)
        {
            var sql = @"SELECT * FROM [jetf].[dbo].CptSeaMainNumberDetail a 
                        where IsReceiveOrder='0'
                        and exists 
                        (
                          select * from [jetf].[dbo].[CptSeaMainNumber]
                          where MainNumber = a.MainNumber and UploadOpe=@UploadOpe and UploadTime=@UploadTime
                        )";

            return conn.Query<CptSeaMainNumberDetailModel>(sql,
               new
               {
                   UploadTime = uploadTime,
                   UploadOpe = uploadOpe
               }).ToList();
        }

        /// <summary>
        /// 使用分提單號取得CptSeaMainNumberDetail資料
        /// </summary>
        /// <param name="trackingNoList"></param>
        /// <returns></returns>
        public List<CptSeaMainNumberDetailModel> GetCptSeaMainNumberDetails(List<string> bagNumberList)
        {
            var sql = @"
                       declare @BagNumberTable Table
                                             ( 
	                                               BagNumber nvarchar(100)
                                             )

                                            {0};
                        SELECT a.BagNumber,Id,MainNumber FROM @BagNumberTable a
                        LEFT JOIN [jetf].[dbo].CptSeaMainNumberDetail b on a.BagNumber= b.BagNumber
                    ";
            var sb = new StringBuilder();
            foreach (var item in bagNumberList.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @BagNumberTable VALUES {string.Join(",",
                item.Select(r => $"('{r}')"))};");
            }

            sql = string.Format(sql, sb.ToString());

            return conn.Query<CptSeaMainNumberDetailModel>(sql).ToList();
        }

        /// <summary>
        /// 查詢GB321
        /// </summary>
        /// <param name="list"></param>
        public void SearchGb321(List<CptSeaMainNumberDetailModel> gb321List) 
        {
            foreach (var item in gb321List)
            {
                var mainNumber = item.MainNumber.Replace("溢卸","");
                var parameters = new Dictionary<string, string>
                    {
                        { "transType", "S" },
                        { "mawb", mainNumber },
                        { "hawb", item.BagNumber }
                    };
                var result = GetGb321(parameters);

                item.Gb321Status = result.Status;
                item.Gb321Msg = result.Msg;

                //是否收單
                item.IsReceiveOrder = result.GridModel?.FirstOrDefault(x => x.ProType.Contains("連線收單建檔")) != null ? true : false;

                //最新的一筆資料
                if (result.GridModel != null)
                {
                    var last = result.GridModel.OrderByDescending(x => x.ProDate).ThenByDescending(x => x.ProTime).FirstOrDefault();
                    item.Gb321ProDateTime = DateTime.TryParseExact($"{last.ProDate.Trim()}{last.ProTime.Trim()}", "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime) ? dateTime.ToString("yyyy-MM-dd HH:mm:ss") : null;
                    item.Gb321ProType = last.ProType;
                }

                //更新資料
                UpdateGb321Result(item);
            }

        }

        /// <summary>
        /// 查詢GB353
        /// </summary>
        /// <param name="gb353List"></param>
        public void SearchGb353(List<CptSeaMainNumberDetailModel> gb353List) 
        {
            foreach (var item in gb353List)
            {
                var parameters = new Dictionary<string, string>
                        {
                            //海空運別
                            { "transType", "S" },
                            //報單號碼
                            { "declno", "" }, 
                            //選擇報單號碼D、分提單號碼H
                            { "queryType", "H" },
                            //分提單號碼
                            { "hawb", item.BagNumber }
                        };

                var result = GetGb353(parameters);
                item.Gb353Status = result.Status;
                item.Gb353Msg = result.Msg;

                if (result.Data != null)
                {
                    var data = result.Data.OrderByDescending(x => x.IssueDate).ThenByDescending(x => x.IssueTime);
                    var rejReason = data.Select(r => new
                    {
                        RejReasonCode = r.RejReasonCode,
                        IssueDateTime = DateTime.TryParseExact($"{r.IssueDate.Trim()}{r.IssueTime.Trim()}", "yyyyMMddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime) ? dateTime.ToString("yyyy-MM-dd HH:mm:ss") : null,
                    }).ToList();

                    item.Gb353RejReason = JsonConvert.SerializeObject(rejReason);
                    item.Gb353IssueDateTime = rejReason.
                        OrderByDescending(r => r.IssueDateTime).
                        Select(r => r.IssueDateTime).FirstOrDefault();
                    item.Gb353RejReasonCode = string.Join("\r\n", data.Select(r => r.RejReasonCode).ToList());
                    item.Gb353RejReasonDesc = string.Join("\r\n", data.Select(r => r.RejReasonDesc).ToList()).Truncate(200);
                }
                //更新資料
                UpdateGb353Result(item);
            }
        }

        /// <summary>
        /// 查詢GB326
        /// </summary>
        /// <param name="gb353List"></param>
        public void SearchGb326(List<string> gb326List)
        {
            var seaMainNumberFieldA = GetSeaMainNumberFieldA(gb326List);

            foreach (var mainNumber in gb326List)
            {
                var mftNo = seaMainNumberFieldA.ContainsKey(mainNumber) ? seaMainNumberFieldA[mainNumber] : string.Empty;
               
                var parameters = new Dictionary<string, string>
                   {
                       { "tab1.currentPage", "1" },
                       { "tab1.rowNum", "10" },
                       { "tab1.hideDeclNo", "" },
                       { "tab1.vslRegNo", mftNo },
                       { "tab1.mftNo", "" },
                       { "choice", "B" },
                       { "tab1.mawb", mainNumber },
                       { "tab1.hawb", "" }
                   };

                var result = _cptPortalApi.GetGb326(parameters);

                //更新資料
                if(result != null)
                    UpdateGb326Result(mainNumber, result?.ImportDate);
            }
        }

        /// <summary>
        /// 更新GB321結果
        /// </summary>
        /// <param name="item"></param>
        public void UpdateGb321Result(CptSeaMainNumberDetailModel item)
        {
            if (item.Id == 0)
                return;

            var sql = @"UPDATE [jetf].[dbo].CptSeaMainNumberDetail SET 
                    IsReceiveOrder = @IsReceiveOrder,
                    Gb321Status = @Gb321Status,
                    Gb321Msg = @Gb321Msg,
                    Gb321ProDateTime = @Gb321ProDateTime,
                    Gb321ProType = @Gb321ProType,
                    UpdateGb321Time = GETDATE() 
                    WHERE Id=@Id";

            conn.Execute(sql, item);
        }

        /// <summary>
        /// 更新GB353結果
        /// </summary>
        /// <param name="item"></param>
        public void UpdateGb353Result(CptSeaMainNumberDetailModel item)
        {
            var sql = @"UPDATE [jetf].[dbo].CptSeaMainNumberDetail SET 
                    Gb353Status = @Gb353Status,
                    Gb353Msg = @Gb353Msg,
                    Gb353RejReason = @Gb353RejReason,
                    Gb353IssueDateTime = @Gb353IssueDateTime,
                    Gb353RejReasonCode = @Gb353RejReasonCode,
                    Gb353RejReasonDesc = @Gb353RejReasonDesc,
                    UpdateGb353Time = GETDATE()
                    WHERE Id=@Id";

            conn.Execute(sql, item);
        }

        /// <summary>
        /// 更新GB326結果
        /// </summary>
        /// <param name="item"></param>
        public void UpdateGb326Result(string mainNumber,string importDate)
        {
            var sql = @"UPDATE [jetf].[dbo].CptSeaMainNumberDetail SET 
                    Gb326ImportDate = @ImportDate,
                    UpdateGb326Time = GETDATE()
                    WHERE MainNumber = @MainNumber";

            conn.Execute(sql, new
            {
                ImportDate = importDate,
                MainNumber = mainNumber
            });
        }

        /// <summary>
        /// 取得船舶航次
        /// </summary>
        /// <param name="mainNumbers"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetSeaMainNumberFieldA(List<string> mainNumberList)
        {
            if (mainNumberList.Any() == false)
                return new Dictionary<string, string>();

            var sql = $@"
                            SELECT MAIN_NUMBER,max(FIELD_A) as FIELD_A
                            FROM [DATA_CENTER].[dbo].[CES_MAIN_ORDER]
                            WHERE MAIN_NUMBER in ({string.Join(",", mainNumberList.Select(r => $"'{r}'"))})
                            GROUP BY MAIN_NUMBER
                       ";

            return conn.Query<(string MAIN_NUMBER, string FIELD_A)>(sql)
                 .ToDictionary(x => x.MAIN_NUMBER, x => x.FIELD_A);
        }

        /// <summary>
        /// 刪除海運主號
        /// </summary>
        /// <param name="mainNumber">主號</param>
        /// <param name="userId">使用者ID</param>
        /// <returns></returns>
        public ResponseModel DeleteCptSeaMainNumber(string mainNumber)
        {
            // 先檢查主號是否存在
            var checkSql = @"SELECT COUNT(*) FROM [jetf].[dbo].[CptSeaMainNumber] WHERE MainNumber = @MainNumber";
            var existsCount = conn.QueryFirstOrDefault<int>(checkSql, new { MainNumber = mainNumber });
            
            if (existsCount == 0)
            {
                return new ResponseModel()
                {
                    status = Status.error,
                    msg = $"主號 {mainNumber} 不存在，無法刪除"
                };
            }

            // 先刪除 CptSeaMainNumberDetail 明細資料
            var deleteDetailSql = @"DELETE FROM [jetf].[dbo].[CptSeaMainNumberDetail] WHERE MainNumber = @MainNumber";

            // 再刪除 CptSeaMainNumber 主檔資料
            var deleteMainSql = @"DELETE FROM [jetf].[dbo].[CptSeaMainNumber] WHERE MainNumber = @MainNumber";

            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    var parameter = new { MainNumber = mainNumber };
                    
                    // 先刪除明細資料
                    var detailDeletedCount = conn.Execute(deleteDetailSql, parameter, tran, commandTimeout: 600);
                    
                    // 再刪除主檔資料
                    var mainDeletedCount = conn.Execute(deleteMainSql, parameter, tran, commandTimeout: 600);

                    tran.Commit();
                    
                    return new ResponseModel()
                    {
                        status = Status.success,
                        msg = $"主號 {mainNumber} 刪除成功，明細資料 {detailDeletedCount} 筆"
                    };
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return new ResponseModel(ex.Message);
                }
            }
        }

        //取得品名、稅則

        private void GetAirApprovalG(List<EtlClearanceOrderModel> list) 
        {
            var trackingNos = list.Where(r => r.GridModel?.Any() == true)
                .Select(r => r.TrackingNo).Distinct().ToList();

            var sql = @"
                    select HAWB_NO, ITEM_NO, ITEM, CCCCODE 
                    from DATA_CENTER.dbo.AIR_APPROVAL_G
                    where HAWB_NO in @TrackingNos";

            var result = conn.Query<(string HawbNo, string ItemNo, string Item, string CccCode)>(
                             sql,
                             new { TrackingNos = trackingNos }
                         );


            var dic = result
                .GroupBy(x => (x.HawbNo, x.ItemNo))
                .ToDictionary(
                    g => g.Key,
                    g => (g.First().Item, g.First().CccCode)
                );

            list.ForEach(r =>
            {
                if (r.GridModel?.Any() == true)
                {
                    r.GridModel.ForEach(x =>
                    {
                        if (dic.TryGetValue((r.TrackingNo, x.ItemNo), out var val))
                        {
                            x.Item = val.Item;
                            x.CCCCode = val.CccCode;
                        }
                    });
                }
            });



        }
    }
}
