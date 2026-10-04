using Dapper;
using Newtonsoft.Json;
using PDTWebAPI.Models.Unpacking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TelegramLibrary;

namespace PDTWebAPI.Services
{
    public class UnpackingService
    {
        //A0006 現場異常查詢
        string chatId = "-1002397673126";
        string lineToken = "Zebv21cfYpzusZ8Xw1wQZb6i92SfLOftd331If9kvIz";

        TelegramBot _telegramBot;
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public UnpackingService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
            _telegramBot = new TelegramBot();
        }

        ~UnpackingService()
        {
            conn.Dispose();
        }

        public async Task<UnpackingResponseModel> PostBagNoAsync(BagNoModel body)
        {
            UnpackingResponseModel response = new UnpackingResponseModel();
            response.Status = "Success";
            conn.Open();
            try
            {
                //新增API紀錄
                InsertPdtUnpacking(new PdtUnpackingModel()
                {
                    API = "BagNo",
                    DataType = body.DataType,
                    BagNo = body.BagNo,
                    UploadOpe = body.UploadOpe,
                    UploadTime = DateTime.ParseExact(body.UploadTime, "yyyyMMddHHmmss", new System.Globalization.CultureInfo("zh-TW")).ToString("yyyy-MM-dd HH:mm:ss")
                });

                //作業選倉庫每個袋號都回Y
                if (body.DataType == "倉庫")
                {
                    response.ResultCode = "Y";
                    response.ResultMessage = "拆袋處理";
                }
                else if (body.DataType.ToUpper() == "TACT" || body.DataType.ToUpper() == "FTZ")
                {
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter("select BAGNO,TRACKINGNO,REMARK from [jetf].[dbo].[B6F_UNPACKING_UPLOAD] where BAGNO=@BAGNO and SCAN_UPLOAD_OPE2='' ", conn))
                    {
                        da.SelectCommand.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = body.BagNo;
                        da.Fill(dt);
                    }
                    //需要拆袋
                    if (dt.Rows.Count > 0)
                    {
                        var remarks = dt.AsEnumerable()
                            .Where(r => !string.IsNullOrWhiteSpace(r.Field<string>("REMARK")))
                            .Select(r => new
                            {
                                TrackingNo = r.Field<string>("TrackingNo"),
                                Remark = r.Field<string>("REMARK"),
                            }
                            ).ToList();

                        //有備註需要發送LINE
                        if (remarks.Any())
                        {
                            //發送LINE訊息
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine($"拆袋處理：{body.DataType.ToUpper()}");
                            sb.AppendLine($"袋號：{body.BagNo}");
                            sb.AppendLine("分提單號：");
                            remarks.ForEach(r =>
                            {
                                sb.AppendLine($"{r.TrackingNo}");
                                sb.AppendLine($"{r.Remark}");
                            });

                            //發送Line
                            await SendLineAsync(sb.ToString());
                        }

                        ArrayList list = new ArrayList();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            list.Add(dt.Rows[i]["TRACKINGNO"].ToString().Trim());
                        }
                        response.ResultCode = "Y";
                        response.ResultMessage = $"拆袋處理：{string.Join("，", list.ToArray())}";
                        //更新資料B6F_UNPACKING_UPLOAD
                        using (SqlCommand cmd = new SqlCommand("update [jetf].[dbo].[B6F_UNPACKING_UPLOAD] set SCAN_UPLOAD_OPE=@SCAN_UPLOAD_OPE,SCAN_UPLOAD_TIME=@SCAN_UPLOAD_TIME where BAGNO=@BAGNO ", conn))
                        {
                            cmd.Parameters.Add("@SCAN_UPLOAD_OPE", SqlDbType.NVarChar).Value = body.UploadOpe;
                            cmd.Parameters.Add("@SCAN_UPLOAD_TIME", SqlDbType.NVarChar).Value = DateTime.ParseExact(body.UploadTime, "yyyyMMddHHmmss", new System.Globalization.CultureInfo("zh-TW")).ToString("yyyy-MM-dd HH:mm:ss");
                            cmd.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = body.BagNo;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        response.ResultCode = "N";
                    }
                    dt.Dispose();
                }
                //else if (body.DataType.ToUpper() == "TPCT" || body.DataType.ToUpper() == "IPOST" || body.DataType.ToUpper() == "TIPC" || body.DataType.ToUpper() == "華儲通關" || body.DataType.ToUpper() == "空運無資料")
                else
                {
                    response.ResultCode = "Y";
                    response.ResultMessage = "按確認後掃分提單號";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }
            conn.Close();
            return response;
        }

        public async Task<UnpackingResponseModel> PostTrackingNoAsync(TrackingNoModel body)
        {
            string remark, trackingNo, dataType, pdtMessage;
            UnpackingResponseModel response = new UnpackingResponseModel();

            //作業地區
            dataType = body.DataType;
            //分提單號
            trackingNo = body.TrackingNo.Trim();

            conn.Open();
            try
            {
                //新增API紀錄
                InsertPdtUnpacking(new PdtUnpackingModel()
                {
                    API = "TrackingNo",
                    DataType = body.DataType,
                    BagNo = body.BagNo,
                    TrackingNo = body.TrackingNo,
                    UploadOpe = body.UploadOpe,
                    UploadTime = DateTime.ParseExact(body.UploadTime, "yyyyMMddHHmmss", new System.Globalization.CultureInfo("zh-TW")).ToString("yyyy-MM-dd HH:mm:ss")
                });

                if (body.DataType == "倉庫")
                {
                    response.ResultCode = "N";
                }
                else if (body.DataType.ToUpper() == "TACT" || body.DataType.ToUpper() == "FTZ")
                {
                    //空運
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter("select BAGNO,REMARK from [jetf].[dbo].[B6F_UNPACKING_UPLOAD] where BAGNO=@BAGNO and TRACKINGNO=@TRACKINGNO ", conn))
                    {
                        da.SelectCommand.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = body.BagNo;
                        da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = body.TrackingNo;
                        da.Fill(dt);
                    }
                    //需要更新
                    if (dt.Rows.Count > 0)
                    {
                        response.ResultCode = "Y";
                        response.ResultMessage = "拆出";

                        //備註不是空白，顯示table欄位訊息
                        remark = dt.Rows[0]["REMARK"].ToString().Trim();
                        if (remark != "")
                        {
                            response.ResultMessage = remark;
                        }
                        //更新資料B6F_UNPACKING_UPLOAD
                        using (SqlCommand cmd = new SqlCommand("update [jetf].[dbo].[B6F_UNPACKING_UPLOAD] set SCAN_UPLOAD_OPE2=@SCAN_UPLOAD_OPE2,SCAN_UPLOAD_TIME2=@SCAN_UPLOAD_TIME2 where BAGNO=@BAGNO and TRACKINGNO=@TRACKINGNO", conn))
                        {
                            cmd.Parameters.Add("@SCAN_UPLOAD_OPE2", SqlDbType.NVarChar).Value = body.UploadOpe;
                            cmd.Parameters.Add("@SCAN_UPLOAD_TIME2", SqlDbType.NVarChar).Value = DateTime.ParseExact(body.UploadTime, "yyyyMMddHHmmss", new System.Globalization.CultureInfo("zh-TW")).ToString("yyyy-MM-dd HH:mm:ss");
                            cmd.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = body.BagNo;
                            cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = body.TrackingNo;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        response.ResultCode = "N";
                    }
                }
                else if (body.DataType == "酷澎全家")
                {
                    if (trackingNo.Length == 11)
                    {
                        pdtMessage = GetB6F_SEA_UNPACKING_UPLOAD("倉庫轉出", trackingNo);
                        if (string.IsNullOrEmpty(pdtMessage))
                        {
                            //轉入掃貨上車
                            response = InsertPdtScanCargoUpload(body, "80", "TACT");
                        }
                        else
                        {
                            //顯示PDT訊息
                            response.ResultCode = "Y";
                            response.ResultMessage = pdtMessage;
                        }
                    }
                    else
                    {
                        response.ResultCode = "Y";
                        response.ResultMessage = "不是全家";
                    }
                }
                else if (body.DataType == "酷澎711")
                {
                    if (body.TrackingNo.Trim().Length == 8)
                    {
                        pdtMessage = GetB6F_SEA_UNPACKING_UPLOAD("倉庫轉出", trackingNo);
                        if (string.IsNullOrEmpty(pdtMessage))
                        {
                            //轉入掃貨上車
                            response = InsertPdtScanCargoUpload(body, "81", "TACT");
                        }
                        else
                        {
                            //顯示PDT訊息
                            response.ResultCode = "Y";
                            response.ResultMessage = pdtMessage;
                        }
                    }
                    else
                    {
                        response.ResultCode = "Y";
                        response.ResultMessage = "不是7-11";
                    }
                }
                else if (body.DataType == "酷澎黑貓")
                {
                    if (body.TrackingNo.Trim().Length == 12)
                    {
                        pdtMessage = GetB6F_SEA_UNPACKING_UPLOAD("倉庫轉出", trackingNo);
                        if (string.IsNullOrEmpty(pdtMessage))
                        {
                            //轉入掃貨上車
                            response = InsertPdtScanCargoUpload(body, "82", "TACT");
                        }
                        else
                        {
                            //顯示PDT訊息
                            response.ResultCode = "Y";
                            response.ResultMessage = pdtMessage;
                        }
                    }
                    else
                    {
                        response.ResultCode = "Y";
                        response.ResultMessage = "不是黑貓";
                    }
                }
                else if (body.DataType == "空異常袋號")
                {
                    response.ResultCode = "Y";

                    //查詢併袋袋號錯單資料
                    string reason = GetEtlMergeBagNoError(body.TrackingNo);

                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter("SELECT TRACKINGNO from DATA_CENTER.[dbo].[ORIGINALLIST] where BAGNO=@BAGNO ", conn))
                    {
                        da.SelectCommand.Parameters.Add("@BAGNO", SqlDbType.NVarChar).Value = body.TrackingNo;
                        da.Fill(dt);
                    }
                    if (dt.Rows.Count > 0)
                    {
                        var list = from item in dt.AsEnumerable()
                                   select item.Field<string>("TRACKINGNO");

                        trackingNo = string.Join("，", list);

                        //發送LINE訊息
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("空異常袋號");
                        sb.AppendLine($"工號：{body.UploadOpe}");
                        sb.AppendLine($"袋號：{body.TrackingNo}");
                        sb.AppendLine("分提單號：");
                        sb.AppendLine(string.Join("\r\n", list));
                        sb.AppendLine($"錯單：{reason}");

                        response.ResultMessage = $"{await SendLineAsync(sb.ToString())}{trackingNo}，錯單：{reason}";
                    }
                    else
                    {
                        //發送LINE訊息
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("空異常袋號");
                        sb.AppendLine($"工號：{body.UploadOpe}");
                        sb.AppendLine($"袋號：{body.TrackingNo}");
                        sb.AppendLine("分提單號：");
                        sb.AppendLine("查無分號");
                        sb.AppendLine($"錯單：{reason}");

                        response.ResultMessage = $"{await SendLineAsync(sb.ToString())}查無分號，錯單：{reason}";
                    }
                }
                else if (body.DataType == "空異常分號")
                {
                    response.ResultCode = "Y";

                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter("SELECT BAGNO from DATA_CENTER.[dbo].[ORIGINALLIST] where TRACKINGNO=@TRACKINGNO ", conn))
                    {
                        da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = body.TrackingNo;
                        da.Fill(dt);
                    }
                    if (dt.Rows.Count > 0)
                    {
                        var list = from item in dt.AsEnumerable()
                                   select item.Field<string>("BAGNO");

                        string bagNo = string.Join("，", list);

                        //發送LINE訊息
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("空異常分號");
                        sb.AppendLine($"工號：{body.UploadOpe}");
                        sb.AppendLine($"分提單號：{body.TrackingNo}");
                        sb.AppendLine("袋號：");
                        sb.AppendLine(string.Join("\r\n", list));

                        response.ResultMessage = $"{await SendLineAsync(sb.ToString())}{bagNo}";
                    }
                    else
                    {
                        //發送LINE訊息
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("空異常分號");
                        sb.AppendLine($"工號：{body.UploadOpe}");
                        sb.AppendLine($"分提單號：{body.TrackingNo}");
                        sb.AppendLine("袋號：");
                        sb.AppendLine("查無袋號");

                        response.ResultMessage = $"{await SendLineAsync(sb.ToString())}查無袋號";
                    }
                }
                else if (body.DataType == "吊牌查詢")
                {
                    response.ResultCode = "Y";

                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter("SELECT distinct BAGNO from DATA_CENTER.[dbo].[ORIGINALLIST] where FIELD_X=@FIELD_X ", conn))
                    {
                        da.SelectCommand.Parameters.Add("@FIELD_X", SqlDbType.NVarChar).Value = body.TrackingNo;
                        da.Fill(dt);
                    }
                    if (dt.Rows.Count > 0)
                    {
                        var list = from item in dt.AsEnumerable()
                                   select item.Field<string>("BAGNO");

                        string bagNo = string.Join("，", list);

                        //發送LINE訊息
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("吊牌查詢");
                        sb.AppendLine($"工號：{body.UploadOpe}");
                        sb.AppendLine($"吊牌：{body.TrackingNo}");
                        sb.AppendLine("袋號：");
                        sb.AppendLine(string.Join("\r\n", list));

                        response.ResultMessage = $"{await SendLineAsync(sb.ToString())}{bagNo}";
                    }
                    else
                    {
                        response.ResultMessage = "查無吊牌";
                    }
                }
                else if (body.DataType == "海快報單狀態")
                {
                    response = GetSeaDeclarationStatus(trackingNo);
                }
                else if (body.DataType == "海快報單狀態(資料庫)")
                {
                    response = GetSeaDeclarationStatusByDB(trackingNo, "TPCT");
                }
                else if (body.DataType == "海快報單狀態台中港(資料庫)")
                {
                    response = GetSeaDeclarationStatusByDB(trackingNo, "JSTC");
                }
                else if (body.DataType == "空快報單狀態")
                {
                    response = GetEtlDeclarationStatus(trackingNo);
                }
                else if (body.DataType == "TACT空快報單放行")
                {
                    response = GetEtlDeclarationRL(trackingNo);
                }
                else if (body.DataType == "FTZ空快報單放行")
                {
                    response = GetEtlFtzDeclarationRL(trackingNo);
                }
                else if (body.DataType == "拆袋貨件查詢")
                {

                    response = UnbaggingCargoSearch(body);

                }
                else
                {
                    //海運
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter("select TRACKINGNO,PDTMESSAGE from [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] where TRACKINGNO=@TRACKINGNO and DATATYPE=@DATATYPE ", conn))
                    {
                        da.SelectCommand.Parameters.Add("@DATATYPE", SqlDbType.NVarChar).Value = body.DataType;
                        da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = body.TrackingNo;
                        da.Fill(dt);
                    }
                    //需要更新
                    if (dt.Rows.Count > 0)
                    {
                        response.ResultCode = "Y";
                        response.ResultMessage = "可通關";
                        //備註不是空白，顯示table欄位訊息
                        remark = dt.Rows[0]["PDTMESSAGE"].ToString().Trim();
                        if (remark != "")
                        {
                            response.ResultMessage = remark;
                        }
                        //更新資料B6F_UNPACKING_UPLOAD
                        using (SqlCommand cmd = new SqlCommand("update [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] set SCAN_UPLOAD_OPE=@SCAN_UPLOAD_OPE,SCAN_UPLOAD_TIME=@SCAN_UPLOAD_TIME where TRACKINGNO=@TRACKINGNO and DATATYPE=@DATATYPE ", conn))
                        {
                            cmd.Parameters.Add("@SCAN_UPLOAD_OPE", SqlDbType.NVarChar).Value = body.UploadOpe;
                            cmd.Parameters.Add("@SCAN_UPLOAD_TIME", SqlDbType.NVarChar).Value = DateTime.ParseExact(body.UploadTime, "yyyyMMddHHmmss", new System.Globalization.CultureInfo("zh-TW")).ToString("yyyy-MM-dd HH:mm:ss");
                            cmd.Parameters.Add("@DATATYPE", SqlDbType.NVarChar).Value = body.DataType;
                            cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = body.TrackingNo;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        response.ResultCode = "N";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }
            conn.Close();
            return response;
        }

        /// <summary>
        /// 取得海快報單狀態
        /// </summary>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        public UnpackingResponseModel GetSeaDeclarationStatus(string trackingNo) 
        {
            var response = new UnpackingResponseModel()
            {
                ResultCode = "Y"
            };

            //海快原單資料
            DataTable dt = GetSeaOrderOriginal(trackingNo);
            var modifyByRow = dt.AsEnumerable().FirstOrDefault(r => (r.Field<string>("MODIFYBY") ?? "").Contains("TPCT"));

            // 先取 TPCT 的 MAINNUMBER
            var mawb = modifyByRow?.Field<string>("MAINNUMBER");

            // 如果是空或 null，就改抓第一筆
            if (string.IsNullOrEmpty(mawb))
            {
                mawb = dt.AsEnumerable()
                         .Select(r => r.Field<string>("MAINNUMBER"))
                         .FirstOrDefault();
            }

            var parameters = new Dictionary<string, string>
                    {
                        { "transType", "S" },
                        { "mawb", mawb },
                        { "hawb", trackingNo }
                    };

            var result = PostGb321(parameters);

            if (result.Status == "ok")
            {
                var grid = result.GridModel?.FirstOrDefault();

                var message = grid?.ProType ?? "";
                if (result.GridModel.Select(r => r.ProType).Contains("連線收單建檔"))
                {
                    //是否溢卸
                    var isUnload = IsUnload(dt);

                    message = isUnload ? "高雄港" : "可通關";
                }
                response.ResultMessage = message;
            }
            else if (result.Msg.Contains("分號資料重複，請同時輸入主號再重新查詢"))
            {
                response.ResultMessage = "可通關";
            }
            else
            {
                //是否後段報關
                var isPostEntry = IsPostEntry(dt);

                if (isPostEntry)
                {
                    parameters = new Dictionary<string, string>
                            {
                                { "mawb", mawb },
                                { "hawb", trackingNo }
                            };

                    var gb301Result = PostGb301(parameters);

                    var grid = gb301Result.GridModel?.FirstOrDefault();

                    var message = grid?.ProcEventCodeStr ?? "";

                    if (gb301Result.GridModel != null && gb301Result.GridModel.Select(r => r.ProcEventCodeStr).Contains("E1 收單建檔"))
                    {
                        message = "可通關";
                    }
                    response.ResultMessage = message;
                }
                else
                {
                    response.ResultCode = "N";
                }
            }

            return response;
        }

        /// <summary>
        /// 取得空快報單狀態
        /// </summary>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        public UnpackingResponseModel GetEtlDeclarationStatus(string trackingNo)
        {
            var response = new UnpackingResponseModel()
            {
                ResultCode = "Y",
                ResultMessage ="可通關",
            };

            if (IsPassGb321(trackingNo))
            {
                return response;
            }

            var mainNumber = GetEtlMainNumber(trackingNo);

            if (!string.IsNullOrEmpty(mainNumber))
            {
                if (IsPassGb301(mainNumber, trackingNo))
                {
                    return response;
                }

                //併分提單號
                var targetTrackingNo = GetEtlTargetTrackingNo(trackingNo);
                if(IsPassGb321(targetTrackingNo))
                {
                    return response;
                }

                if (IsPassGb301(mainNumber, targetTrackingNo))
                {
                    return response;
                }
            }

            var pdtMessage = GetB6F_SEA_UNPACKING_UPLOAD("空快報單狀態", trackingNo);
            if (!string.IsNullOrEmpty(pdtMessage))
            {
                response.ResultMessage = pdtMessage;
                return response;
            }

            return new UnpackingResponseModel() 
           {
               ResultCode = "N",
           };
        }

        /// <summary>
        /// 取得空快報單放行
        /// </summary>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        public UnpackingResponseModel GetEtlDeclarationRL(string trackingNo)
        {
            if (GetTactRelnonout(trackingNo))
            {
                return new UnpackingResponseModel()
                {
                    ResultCode = "Y",
                    ResultMessage = "RL 放行",
                };
            }

            return new UnpackingResponseModel()
            {
                ResultCode = "N",
            };
        }

        /// <summary>
        /// 取得空快報單放行(FTZ)
        /// </summary>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        public UnpackingResponseModel GetEtlFtzDeclarationRL(string trackingNo)
        {
            if (GetFtzRelnonout(trackingNo))
            {
                return new UnpackingResponseModel()
                {
                    ResultCode = "Y",
                    ResultMessage = "RL 放行",
                };
            }

            return new UnpackingResponseModel()
            {
                ResultCode = "N",
            };
        }

        /// <summary>
        /// 取得海快報單狀態
        /// </summary>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        public UnpackingResponseModel GetSeaDeclarationStatusByDB(string trackingNo, string portCode)
        {
            ///是否連線收單建檔
            var result = GetGb321ByDB(trackingNo);

            if (result.Item1.HasValue == false)
            { 
                return new UnpackingResponseModel() 
                { 
                    ResultCode = "N",
                    ResultMessage = "資料庫查無此分提單號" 
                };
            }

            //海快原單資料
            DataTable dt = GetSeaOrderOriginal(trackingNo);

            if (result.Item1.Value)
            {
                //是否溢卸
                var isUnload = IsUnload(dt, portCode);

                return new UnpackingResponseModel()
                {
                    ResultCode = "Y",
                    ResultMessage = isUnload ? "高雄港" : "可通關"
                };
            }

            //是否後段報關
            var isPostEntry = IsPostEntry(dt);

            if (isPostEntry)
            {
                var parameters = new Dictionary<string, string>
                            {
                                { "mawb", dt.Rows[0]["MAINNUMBER"].ToString() },
                                { "hawb", trackingNo }
                            };

                var gb301Result = PostGb301(parameters);

                var grid = gb301Result.GridModel?.FirstOrDefault();

                var message = grid?.ProcEventCodeStr ?? "";

                if (gb301Result.GridModel != null && gb301Result.GridModel.Select(r => r.ProcEventCodeStr).Contains("E1 收單建檔"))
                {
                    message = "可通關";
                }

                return new UnpackingResponseModel()
                {
                    ResultCode = "Y",
                    ResultMessage = message
                };
            }

            return new UnpackingResponseModel()
            {
                ResultCode = "N"
            };
        }

        /// <summary>
        /// 查詢空快併袋袋號代碼
        /// </summary>
        /// <returns></returns>
        public string GetEtlMergeBagNoError(string bagNo)
        {
            string sql = @"
                            declare @BagNo nvarchar(50)=@Data
                            select @BagNo = TARGET_CODE from [DATA_CENTER].[dbo].[CES_UPLOAD_RECORD]
                            where SOURCE_CODE=@BagNo and MODEL='MERGE_BAG'

                            select top 1 REASON from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR]
                            where BAG_NO=@BagNo and REASON in('A03','B6F')
                         ";

            string reason = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@Data", SqlDbType.NVarChar).Value = bagNo;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                reason = dt.Rows[0]["REASON"].ToString().Trim().ToUpper();
            }

            return reason;
        }

        /// <summary>
        /// 取得海運-上傳拆袋資料
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        public string GetB6F_SEA_UNPACKING_UPLOAD(string dataType, string trackingNo)
        {

            string pdtMessage = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select TRACKINGNO,PDTMESSAGE from [jetf].[dbo].[B6F_SEA_UNPACKING_UPLOAD] where TRACKINGNO=@TRACKINGNO and DATATYPE=@DATATYPE ", conn))
            {
                da.SelectCommand.Parameters.Add("@DATATYPE", SqlDbType.NVarChar).Value = dataType;
                da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = trackingNo;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                pdtMessage = dt.Rows[0]["PDTMESSAGE"].ToString().Trim();

                pdtMessage = string.IsNullOrEmpty(pdtMessage) ? $"{dataType}有資料，無上傳訊息" : pdtMessage;
            }
            return pdtMessage;
        }

        public void InsertPdtUnpacking(PdtUnpackingModel model)
        {
            using (SqlCommand cmd = new SqlCommand("insert [jetf].[dbo].[PdtUnpacking](API, DataType, BagNo, TrackingNo, UploadOpe, UploadTime) values(@API, @DataType, @BagNo, @TrackingNo, @UploadOpe, @UploadTime) ", conn))
            {
                cmd.Parameters.Add("@API", SqlDbType.NVarChar).Value = model.API;
                cmd.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = model.DataType;
                cmd.Parameters.Add("@BagNo", SqlDbType.NVarChar).Value = model.BagNo;
                cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = model.TrackingNo ?? (object)DBNull.Value;
                cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = model.UploadOpe;
                cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = model.UploadTime;
                cmd.ExecuteNonQuery();
            }

        }

        /// <summary>
        /// 新增掃貨上車
        /// </summary>
        /// <param name="body"></param>
        /// <param name="transNo"></param>
        UnpackingResponseModel InsertPdtScanCargoUpload(TrackingNoModel body, string transNo, string dataType)
        {
            UnpackingResponseModel response = new UnpackingResponseModel();

            using (SqlCommand cmd = new SqlCommand("[jetf].[dbo].[USP_Insert_PdtScanCargoUpload]", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Clear();
                cmd.Parameters.Add("@TransNo", SqlDbType.NVarChar).Value = transNo;
                cmd.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = dataType;
                cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = body.UploadOpe;
                cmd.Parameters.Add("@Data", SqlDbType.NVarChar).Value = body.TrackingNo;
                cmd.Parameters.Add("@CarNo", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = (object)DateTime.ParseExact(body.UploadTime, "yyyyMMddHHmmss", new System.Globalization.CultureInfo("zh-TW")).ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value;
                cmd.ExecuteNonQuery();

                response.Status = "Success";
                response.ResultCode = "Y";
            }
            return response;
        }

        /// <summary>
        /// 11.(GB321)進口簡易申報收單作業結果查詢
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public SwNatClearanceStatusModel PostGb321(Dictionary<string, string> parameters)
        {
            try
            {
                //關貿Api
                string url = "https://portal.sw.nat.gov.tw/APGQ/GB321!query";

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
                        return JsonConvert.DeserializeObject<SwNatClearanceStatusModel>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        return new SwNatClearanceStatusModel() { Msg = "(GB321)進口簡易申報收單作業結果查詢失敗，請重新查詢" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new SwNatClearanceStatusModel() { Msg = ex.Message };
            }
        }

        /// <summary>
        /// 1.(GB301)進口報單通關流程查詢
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Gb321Model PostGb301(Dictionary<string, string> parameters)
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
                        return JsonConvert.DeserializeObject<Gb321Model>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        return new Gb321Model() { Msg = "(GB301)進口報單通關流程查詢失敗，請重新查詢" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new Gb321Model() { Msg = ex.Message };
            }
        }

        /// <summary>
        /// 是否為溢卸
        /// </summary>
        /// <returns></returns>
        bool IsUnload(DataTable dt, string portCode = "TPCT")
        {
            var result = dt.AsEnumerable().Any(r => r.Field<string>("MAINNUMBER").Contains("溢卸"));

            if (result)
            {
                //如果筆數等於一筆
                if (dt.AsEnumerable().Count() == 1)
                {
                    //找不到指定港口代碼，就是高雄港
                    result = dt.AsEnumerable().Any(r => r.Field<string>("MODIFYBY") != null && !r.Field<string>("MODIFYBY").Contains(portCode));
                }
            }
            else
            {
                //找不到溢卸，再判斷MODIFYBY是不是有指定港口代碼，如果找不到，就是高雄港
                result = dt.AsEnumerable().Any(r => r.Field<string>("MODIFYBY") != null && !r.Field<string>("MODIFYBY").Contains(portCode));
            }
            return result;
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
            using (SqlDataAdapter da = new SqlDataAdapter("select POST_ENTRY,MAINNUMBER,MODIFYBY from DATA_CENTER.[dbo].[SEA_ORDER_ORIGINAL] where BL_NO=@BL_NO and GW > 0", conn))
            {
                da.SelectCommand.Parameters.Add("@BL_NO", SqlDbType.NVarChar).Value = blNo;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得空快主號
        /// </summary>
        /// <param name="blNo"></param>
        /// <returns></returns>
        string GetEtlMainNumber(string trackingNo)
        {
            var sql = @"
                        select top 1 MAINNUMBER from DATA_CENTER.[dbo].[ORIGINALLIST]
                        where TRACKINGNO=@TRACKINGNO
                        ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = trackingNo;
                da.Fill(dt);
            }
            if(dt.Rows.Count == 0)
                return string.Empty;

            return dt.Rows[0]["MAINNUMBER"].ToString();
        }

        /// <summary>
        /// 取得華儲放行未出倉
        /// </summary>
        /// <param name="blNo"></param>
        /// <returns></returns>
        bool GetTactRelnonout(string trackingNo)
        {
            var sql = @"
                        select top 1 TrackingNo from [jetf].[dbo].TactRelnonout
                        where TrackingNo=@TrackingNo
                        ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = trackingNo;
                da.Fill(dt);
            }

           return dt.Rows.Count > 0;
        }

        /// <summary>
        /// 取得遠雄放行未出倉
        /// </summary>
        /// <param name="blNo"></param>
        /// <returns></returns>
        bool GetFtzRelnonout(string trackingNo)
        {
            var sql = @"
                        select top 1 TrackingNo from [jetf].[dbo].FtzRelnonout
                        where TrackingNo=@TrackingNo
                        ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = trackingNo;
                da.Fill(dt);
            }

            return dt.Rows.Count > 0;
        }

        /// <summary>
        /// 取得併分提單號
        /// </summary>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        string GetEtlTargetTrackingNo(string trackingNo)
        {
            var sql = @"
                    select top 1 TARGET_CODE from [DATA_CENTER].[dbo].[CES_UPLOAD_RECORD]
                    where MODEL='MERGE_NUMBER' and (SOURCE_CODE = @TRACKINGNO or TARGET_CODE= @TRACKINGNO)
                        ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = trackingNo;
                da.Fill(dt);
            }
            if (dt.Rows.Count == 0)
                return string.Empty;

            return dt.Rows[0]["TARGET_CODE"].ToString();
        }

        /// <summary>
        /// 是否連線收單建檔
        /// </summary>
        /// <param name="blNo"></param>
        /// <returns></returns>
        Tuple<bool?, string> GetGb321ByDB(string blNo)
        {
            var sql = @"
                        SELECT [BagNumber],[IsReceiveOrder],[Gb321Status]
                        FROM [jetf].[dbo].[CptSeaMainNumberDetail]
                        where BagNumber=@BagNumber
                        ";
            DataTable dt = new DataTable();

            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@BagNumber", SqlDbType.NVarChar).Value = blNo;
                da.Fill(dt);
            }

            if (dt.Rows.Count == 0)
                return new Tuple<bool?, string>(null,string.Empty); // 查不到資料

            var isReceiveOrder = Convert.ToBoolean(dt.Rows[0]["IsReceiveOrder"]);
            var status = dt.Rows[0]["Gb321Status"].ToString();

            return new Tuple<bool?,string>(Convert.ToBoolean(isReceiveOrder), status);
        }

        /// <summary>
        /// 拆袋貨件查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        private UnpackingResponseModel UnbaggingCargoSearch(TrackingNoModel body)
        {
            var response = new UnpackingResponseModel();

            var trackingNo = body.TrackingNo.Trim();

            var result = GetProcess(trackingNo);

            if (result.Any() == false)
            {
                response.ResultCode = "Y";
                response.ResultMessage = "掃錯單號";
                return response;
            }

            //公司名義清出
            var isType3 = result.Where(r => r == "3").Any();
            if (isType3)
            {
                //轉入掃貨上車，74-空快回倉    
                InsertPdtScanCargoUpload(body, "74", "FTZ");

                response.ResultCode = "Y";
                response.ResultMessage = "公司名義清出";
                return response;
            }

            //現場轉出
            var isType4 = result.Where(r => r == "4").Any();
            if (isType4)
            {
                response.ResultCode = "N";

                //轉入掃貨上車，77-B6F現場轉出
                InsertPdtScanCargoUpload(body, "77", "FTZ");
                return response;
            }

            return response;
        }

        /// <summary>
        /// Gb301是否有收單建檔
        /// </summary>
        /// <param name="mawb"></param>
        /// <param name="hawb"></param>
        /// <returns></returns>
        private bool IsPassGb301(string mawb, string hawb)
        {
            var parameters = new Dictionary<string, string>
            {
                { "mawb", mawb },
                { "hawb", hawb }
            };

            var gb301Result = PostGb301(parameters);
            return gb301Result?.GridModel?.Any(r => r.ProcEventCodeStr == "E1 收單建檔") == true;
        }

        private bool IsPassGb321( string hawb)
        {
            var parameters = new Dictionary<string, string>
                    {
                        { "transType", "A" },
                        { "mawb", "" },
                        { "hawb", hawb }
                    };

            var result = PostGb321(parameters);

            if (result.Status == "ok")
            {
                var grid = result.GridModel?.FirstOrDefault();

                var message = grid?.ProType ?? "";
                if (result.GridModel.Select(r => r.ProType).Contains("連線收單建檔"))
                {
                   return true;
                }
            }

            if (result.Msg.Contains("分號資料重複，請同時輸入主號再重新查詢"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否有找到處置說明Type=3 公司名義收，Type=4 現場轉出
        /// </summary>
        /// <param name="dlvInv"></param>
        /// <returns></returns>
        List<string> GetProcess(string dlvInv)
        {
            string sql = @"
                select PROCESS_TYPE from jetf.[dbo].[Process]
                where DLV_INV=@DLV_INV and PROCESS_TYPE in ('3','4') and DEL = 0
                ";

            return conn.Query<string>(sql,
                new
                {
                    DLV_INV = dlvInv
                })
                .ToList();
        }

        /// <summary>
        /// 發送LINE
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        async Task<string> SendLineAsync(string message)
        {
            string result = "";

            try
            {
                //發送Telegram
                await _telegramBot.SendTextMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            return result;
        }

    }
}