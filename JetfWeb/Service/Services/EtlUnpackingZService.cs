using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using Service.Models.EtlUnpackingZ;
using Service.Models.ExportClearance;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;

namespace Service.Services
{
    public class EtlUnpackingZService
    {
        private string mawbUrl = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryMawb";

        private string detailUrl = "https://portal.sw.nat.gov.tw/APGQ/GB350!queryDetail";

        private SqlConnection conn;
        public EtlUnpackingZService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel Upload(List<string> filePaths, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            List<EtlUnpackingZModel> list = new List<EtlUnpackingZModel>();

            filePaths.ForEach(r =>
            {
                //讀取Excel
                list.AddRange(ReadCsv(r));
            });

            //新增
            if (list.Count > 0)
            {
                string uploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                //寫入資料
                resopnseModel = InsertEtlUnpackingZ(list, uploadTime, userId);

                if (resopnseModel.status == Status.success)
                {
                    resopnseModel.msg = $"上傳檔案筆數：{list.Count }";
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
        /// 新增資料
        /// </summary>
        /// <param name="list"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        ResopnseModel InsertEtlUnpackingZ(List<EtlUnpackingZModel> list, string uploadTime, string userId)
        {
            ResopnseModel resopnseModel = new ResopnseModel();

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                string sql = @"
                                insert jetf.dbo.EtlUnpackingZ(TrackingNo, Memo, UploadOpe, UploadTime) 
                                values(@TrackingNo, @Memo, @UploadOpe, @UploadTime)";

                //更新ZZZA時間
                string sql2 = @"
                                update [jetf].[dbo].[B6F_UNPACKING_UPLOAD] set ZZZA_UPLOAD_TIME=b.UploadTime
                                from jetf.dbo.B6F_UNPACKING_UPLOAD a,jetf.dbo.EtlUnpackingZ b
                                where a.TRACKINGNO=b.TrackingNo and b.UploadOpe=@UploadOpe and b.UploadTime=@UploadTime
                                and a.ZZZA_UPLOAD_TIME is null
                              ";
                try
                {
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Transaction = tran;
                        list.ForEach(r =>
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = r.TrackingNo;
                            cmd.Parameters.Add("@Memo", SqlDbType.NVarChar).Value = r.Memo;
                            cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = userId;
                            cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = uploadTime;
                            cmd.ExecuteNonQuery();
                        });
                    }

                    //更新ZZZA上傳時間
                    using (SqlCommand cmd = new SqlCommand(sql2, conn))
                    {
                        cmd.Transaction = tran;
                        cmd.Parameters.Add("@UploadOpe", SqlDbType.NVarChar).Value = userId;
                        cmd.Parameters.Add("@UploadTime", SqlDbType.NVarChar).Value = uploadTime;
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
            return resopnseModel;
        }

        /// <summary>
        /// 讀取Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<EtlUnpackingZModel> ReadCsv(string filePath)
        {
            List<EtlUnpackingZModel> list = new List<EtlUnpackingZModel>();
            EtlUnpackingZModel item;

            // 讀取CSV檔案
            var lines = File.ReadLines(filePath).Skip(1);

            // 處理CSV檔案的每一行
            foreach (var line in lines)
            {
                var data = line.Split(',');
                if (data.Length > 4)
                {
                    item = new EtlUnpackingZModel();
                    item.TrackingNo = data[0].Trim();
                    item.Memo = data[4].Trim();
                    if (item.Memo.Contains("ZZZA"))
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 查詢資料
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public IWorkbook Download(List<string> search)
        {
            //主號查詢
            List<MawbModel> mawbList = SearchMawb(search);

            //明細查詢
            SearchDetail(mawbList);

            IWorkbook workbook = new XSSFWorkbook();
            //主號查詢結果
            GetMawbSheet(workbook, mawbList);
            //主號2查詢結果
            GetMawb2Sheet(workbook, mawbList);
            //明細查詢結果
            GetDetailSheet(workbook, mawbList);

            return workbook;
        }

        /// <summary>
        /// 主號Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="mawbList"></param>
        /// <returns></returns>
        IWorkbook GetMawbSheet(IWorkbook workbook, List<MawbModel> mawbList)
        {
            ISheet sheet = workbook.CreateSheet("主號");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("主號");
            row.CreateCell(2).SetCellValue("明細");
            row.CreateCell(3).SetCellValue("結果");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);

            int iRow = 1;
            foreach (var item in mawbList)
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(iRow);
                row.CreateCell(1).SetCellValue(item.searchMawb);
                row.CreateCell(2).SetCellValue(item.gridModel.Count);
                row.CreateCell(3).SetCellValue(item.msg);
                iRow++;
            }

            return workbook;
        }

        /// <summary>
        /// 主號2Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="mawbList"></param>
        /// <returns></returns>
        void GetMawb2Sheet(IWorkbook workbook, List<MawbModel> mawbList)
        {
            ISheet sheet = workbook.CreateSheet("主號2");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("主號");
            row.CreateCell(2).SetCellValue("傳輸時間");
            row.CreateCell(3).SetCellValue("總件數");
            row.CreateCell(4).SetCellValue("狀態");
            row.CreateCell(5).SetCellValue("結果");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);


            int iRow = 1;
            foreach (var mawb in mawbList)
            {
                foreach (var item in mawb.gridModel)
                {
                    row = sheet.CreateRow(iRow);
                    row.CreateCell(0).SetCellValue(iRow);
                    row.CreateCell(1).SetCellValue(item.MAWB);
                    if (DateTime.TryParseExact(item.TRANS_DATE, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var transDate))
                    {
                        row.CreateCell(2).SetCellValue(transDate.ToString("yyyy/MM/dd HH:mm:ss"));
                    }
                    row.CreateCell(3).SetCellValue(item.TOT_PACK_QTY);
                    row.CreateCell(4).SetCellValue(item.STATUS);
                    row.CreateCell(5).SetCellValue(item.Detail.msg);
                    iRow++;
                }
            }
        }

        /// <summary>
        /// 明細Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="mawbList"></param>
        /// <returns></returns>
        void GetDetailSheet(IWorkbook workbook, List<MawbModel> mawbList)
        {
            ISheet sheet = workbook.CreateSheet("明細");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("袋號");
            row.CreateCell(1).SetCellValue("袋數");
            row.CreateCell(2).SetCellValue("重量");
            row.CreateCell(3).SetCellValue("1分號多件之分號");
            row.CreateCell(4).SetCellValue("備註");
            row.CreateCell(5).SetCellValue("分艙單收單註記");
            row.CreateCell(6).SetCellValue("主號");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);

            int iRow = 1;
            foreach (var mawb in mawbList)
            {
                mawb.gridModel.ForEach(r =>
                {
                    r.Detail.gridModel?.ForEach(item =>
                    {
                        row = sheet.CreateRow(iRow);
                        row.CreateCell(0).SetCellValue(item.POUCH_NO);
                        row.CreateCell(1).SetCellValue(item.QTY);
                        row.CreateCell(2).SetCellValue(item.WEIGHT);
                        row.CreateCell(4).SetCellValue(item.REMARK);
                        row.CreateCell(5).SetCellValue(item.SOURCE_NOTE);
                        row.CreateCell(6).SetCellValue(r.MAWB);
                        iRow++;
                    });
                });
            }
        }


        public void Excel()
        {

        }

        /// <summary>
        /// 查詢主號
        /// </summary>
        /// <returns></returns>
        List<MawbModel> SearchMawb(List<string> search)
        {
            List<MawbModel> mawbList = new List<MawbModel>();

            foreach (var item in search)
            {
                var parameters = new Dictionary<string, string>
                {
                    { "finalChoice", "A" },
                    { "choice", "A" },
                    { "tab4.mawb", item.ToString().Trim() },
                    { "transDateSearch", "" },
                    { "transIdSearch", "" },
                    { "importDateSearch", "" },
                    { "pouchNo", "" },
                    { "year", "" },
                    { "tab4.mode", "5" },
                    { "tab4.currentGridPage", "1" },
                    { "tab4.currentGridPageRows", "10" }
                };
                mawbList.Add(PostMawb(parameters));
            }

            return mawbList;
        }

        /// <summary>
        /// 查詢明細
        /// </summary>
        /// <param name="mawbList"></param>
        /// <returns></returns>
        void SearchDetail(List<MawbModel> mawbList)
        {
            List<DetailModel> detailList = new List<DetailModel>();
            foreach (var mawb in mawbList)
            {
                if (mawb.msg.Contains("[查詢成功]"))
                {
                    foreach (var item in mawb.gridModel)
                    {
                        if (DateTime.TryParseExact(item.TRANS_DATE, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var transDate))
                        {
                            var parameters = new Dictionary<string, string>
                            {
                                { "tab4.mawb", item.MAWB },
                                { "transDate", transDate.ToString("yyyy/MM/dd HH:mm:ss") },
                                { "tab4.currentGridPage", "1" },
                                { "tab4.currentGridPageRows", "10" }
                            };
                            //查詢明細
                            item.Detail = PostDetail(parameters);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 主號
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        MawbModel PostMawb(Dictionary<string, string> parameters)
        {
            MawbModel result = new MawbModel();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    var formData = new FormUrlEncodedContent(parameters).ReadAsStringAsync().Result;
                    var content = new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded");

                    //發送 POST 請求
                    HttpResponseMessage response = client.PostAsync(mawbUrl, content).Result;

                    // 檢查請求是否成功
                    if (response.IsSuccessStatusCode)
                    {
                        result = JsonConvert.DeserializeObject<MawbModel>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        result.msg = "查詢失敗";
                    }
                }
            }
            catch (Exception ex)
            {
                result.msg = ex.Message;
            }

            result.searchMawb = parameters["tab4.mawb"];

            return result;
        }

        /// <summary>
        /// 明細
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        DetailModel PostDetail(Dictionary<string, string> parameters)
        {
            DetailModel result = new DetailModel();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    var content = new FormUrlEncodedContent(parameters);
                    //發送 POST 請求
                    HttpResponseMessage response = client.PostAsync(detailUrl, content).Result;

                    // 檢查請求是否成功
                    if (response.IsSuccessStatusCode)
                    {
                        result = JsonConvert.DeserializeObject<DetailModel>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        result.msg = "查詢失敗";
                    }
                }
            }
            catch (Exception ex)
            {
                result.msg = ex.Message;
            }
            return result;
        }
    }
}
