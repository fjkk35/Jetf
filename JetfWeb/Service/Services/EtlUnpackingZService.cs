using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using Service.Models.CptTradeVan;
using Service.Models.EtlUnpackingZ;
using Service.Services.CptTradeVan;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services
{
    public class EtlUnpackingZService
    {
        private readonly CptPortalApi _cptPortalApi;
        private SqlConnection conn;

        public EtlUnpackingZService() : this(new CptPortalApi())
        {
        }

        public EtlUnpackingZService(CptPortalApi cptPortalApi)
        {
            _cptPortalApi = cptPortalApi ?? new CptPortalApi();
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel Upload(List<string> filePaths, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            try
            {
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
            }
            finally
            {
                conn.Close();
            }
            return resopnseModel;
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="list"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        ResponseModel InsertEtlUnpackingZ(List<EtlUnpackingZModel> list, string uploadTime, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

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
            List<Gb350Model> mawbList = SearchMawb(search);

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
        IWorkbook GetMawbSheet(IWorkbook workbook, List<Gb350Model> mawbList)
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
                row.CreateCell(1).SetCellValue(item.SearchMawb);
                row.CreateCell(2).SetCellValue(item.GridModel?.Count ?? 0);
                row.CreateCell(3).SetCellValue(item.Msg);
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
        void GetMawb2Sheet(IWorkbook workbook, List<Gb350Model> mawbList)
        {
            ISheet sheet = workbook.CreateSheet("主號2");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("主號");
            row.CreateCell(2).SetCellValue("傳輸時間");
            row.CreateCell(3).SetCellValue("總件數");
            row.CreateCell(4).SetCellValue("進口日期");
            row.CreateCell(5).SetCellValue("航機班次");
            row.CreateCell(6).SetCellValue("狀態");
            row.CreateCell(7).SetCellValue("結果");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);


            int iRow = 1;
            foreach (var mawb in mawbList)
            {
                foreach (var item in mawb.GridModel ?? new List<Gb350GridModel>())
                {
                    row = sheet.CreateRow(iRow);
                    row.CreateCell(0).SetCellValue(iRow);
                    row.CreateCell(1).SetCellValue(item.MAWB);
                    if (DateTime.TryParseExact(item.TRANS_DATE, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var transDate))
                    {
                        row.CreateCell(2).SetCellValue(transDate.ToString("yyyy/MM/dd HH:mm:ss"));
                    }
                    row.CreateCell(3).SetCellValue(item.TOT_PACK_QTY);
                    var importDateText = string.Empty;
                    if (DateTime.TryParseExact(item.IMPORT_DATE, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var importDate))
                    {
                        importDateText = importDate.ToString("yyyy/MM/dd");
                    }
                    row.CreateCell(4).SetCellValue(importDateText);
                    row.CreateCell(5).SetCellValue(item.VOYAGE_FLIGHT_NO);
                    row.CreateCell(6).SetCellValue(item.STATUS);
                    row.CreateCell(7).SetCellValue(item.Detail?.Msg);
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
        void GetDetailSheet(IWorkbook workbook, List<Gb350Model> mawbList)
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

            // 先彙整所有明細，讓同一主號即使分散在不同筆資料，也能一起判斷收單註記。
            var detailRows = mawbList
                .SelectMany(mawb => mawb.GridModel ?? new List<Gb350GridModel>())
                .SelectMany(r => (r.Detail?.GridModel ?? new List<Gb350DetailGridModel>())
                    .Select(item => new
                    {
                        MainNumber = r.MAWB ?? string.Empty,
                        Detail = item
                    }))
                .ToList();

            // 若同一主號的分艙單收單註記全部空白，明細頁籤的該欄位全部補上 X。
            var mainNumbersWithSourceNote = detailRows
                .GroupBy(r => r.MainNumber)
                .ToDictionary(
                    group => group.Key,
                    group => group.Any(r => !string.IsNullOrWhiteSpace(r.Detail.SOURCE_NOTE)));

            int iRow = 1;
            foreach (var item in detailRows)
            {
                row = sheet.CreateRow(iRow);
                row.CreateCell(0).SetCellValue(item.Detail.POUCH_NO);
                row.CreateCell(1).SetCellValue(item.Detail.QTY);
                var weightCell = row.CreateCell(2);
                if (item.Detail.WEIGHT.HasValue)
                {
                    weightCell.SetCellValue(item.Detail.WEIGHT.Value);
                }
                else
                {
                    weightCell.SetCellValue(string.Empty);
                }
                row.CreateCell(3).SetCellValue(item.Detail.HAWB);
                row.CreateCell(4).SetCellValue(item.Detail.REMARK);
                // 已有任一註記時保留原值；同一主號全空白時統一標示 X。
                row.CreateCell(5).SetCellValue(
                    mainNumbersWithSourceNote[item.MainNumber] ? item.Detail.SOURCE_NOTE : "X");
                row.CreateCell(6).SetCellValue(item.MainNumber);
                iRow++;
            }
        }


        public void Excel()
        {

        }

        /// <summary>
        /// 查詢主號
        /// </summary>
        /// <returns></returns>
        List<Gb350Model> SearchMawb(List<string> search)
        {
            List<Gb350Model> mawbList = new List<Gb350Model>();

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
        void SearchDetail(List<Gb350Model> mawbList)
        {
            foreach (var mawb in mawbList)
            {
                if ((mawb.Msg ?? "").Contains("[查詢成功]"))
                {
                    foreach (var item in mawb.GridModel ?? new List<Gb350GridModel>())
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
        Gb350Model PostMawb(Dictionary<string, string> parameters)
        {
            Gb350Model result = _cptPortalApi.GetGb350Mawb(parameters);
            result.SearchMawb = parameters["tab4.mawb"];

            return result;
        }

        /// <summary>
        /// 明細
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Gb350DetailModel PostDetail(Dictionary<string, string> parameters)
        {
            return _cptPortalApi.GetGb350Detail(parameters);
        }
    }
}
