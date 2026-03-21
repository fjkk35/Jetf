using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models.PostClearance;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Service.EnumTax;
using Service.Extensions;
using Service.Models.ErrorOrderSend;
using System.Text.RegularExpressions;
using Service.Models.SeaClearanceCreate;
using Service.Services.CptTradeVan;
using System.Security.Cryptography;
using Service.Models.SeaClearanceCustTaxPayment;
using Service.Models.SeaClearanceSjlTaxPayment;

namespace Service.Services
{
    public class SeaClearanceCreateService: _BaseService
    {
        private readonly CptPortalApi _cptPortalApi;

        public SeaClearanceCreateService(CptPortalApi cptPortalApi)
        {
            _cptPortalApi = cptPortalApi;
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResopnseModel UploadFile(string filePath, string dataDate, string userId)
        {
            var resopnseModel = new ResopnseModel();

            dataDate = Convert.ToDateTime(dataDate).ToString("yyyyMMdd");

            //讀取Excel 
            List<SeaClearanceDetailQueryModel> list = ReadExcel(filePath, dataDate);

            //新增
            if (list.Count > 0)
            {
                //取得海運原單
                GetSeaOrderOriginal(list);

                //確認資料
                CheckList(list);

                //取得掛號
                GetMftNo(list);

                //取得G326
                GetGb326(list);

                //取得稅金
                GetTax(list);

                // 取得檔名
                string fileName = Path.GetFileName(filePath);

                //寫入資料
                resopnseModel = InsertData(list, fileName, userId);
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
        /// 讀取Excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        List<SeaClearanceDetailQueryModel> ReadExcel(string filePath, string dataDate)
        {
            var list = new List<SeaClearanceDetailQueryModel>();

            bool read = false;
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
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCellData(0) == "主提單號" &&
                        sheet.GetRow(i).GetCellData(1) == "分提單號")
                    {
                        read = true;
                        continue;
                    }

                    if (read &&
                       !string.IsNullOrEmpty(sheet.GetRow(i).GetCellData(0)) &&
                       !string.IsNullOrEmpty(sheet.GetRow(i).GetCellData(1)))
                    {
                        list.Add(new SeaClearanceDetailQueryModel()
                        {
                            DataDate = dataDate,
                            MainNumber = sheet.GetRow(i).GetCellData(0),
                            TrackingNo = sheet.GetRow(i).GetCellData(1)
                        });
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResopnseModel InsertData(List<SeaClearanceDetailQueryModel> list, string filePath, string userId)
        {
            var resopnseModel = new ResopnseModel();
            resopnseModel.msg = $"上傳檔案筆數：{list.Count}";

            var insertMainSql = @"insert [jetf].[dbo].[SeaClearance]([FileName],[UploadOpe])
                                    OUTPUT INSERTED.Id
                                    values(@FileName,@UploadOpe)";

            var insertDetailSql = @"insert [jetf].[dbo].[SeaClearanceDetail]([SeaClearanceId],[DataDate],[MainNumber],[TrackingNo],[MftNo],[IsSucess], [Memo],[ImportDate],[DeclNo],[ProDateTime],[IsSeaOrderOriginal],[Tax])
                                    OUTPUT INSERTED.Id
                                    values(@SeaClearanceId,@DataDate, @MainNumber,@TrackingNo, @MftNo,@IsSucess, @Memo,@ImportDate,@DeclNo,@ProDateTime,@IsSeaOrderOriginal,@Tax)";

            var insertOriginalMapping = @"insert [jetf].[dbo].[SeaClearanceDetailOriginalMapping](SeaClearanceDetailId, SeaOrderOriginalId, CreateDate, Modifyby, Post_Entry, Eta, Cust_Code,Piece, Importer, Im_Phoneno, Importer_Id, Tax_Payment, Item_Name,Jetf_Serial, Gw, CC)
                                    values(@SeaClearanceDetailId,@SeaOrderOriginalId, @CreateDate, @Modifyby, @Post_Entry, @Eta, @Cust_Code,@Piece, @Importer, @Im_Phoneno, @Importer_Id, @Tax_Payment, @Item_Name, @Jetf_Serial, @Gw, @CC)";

            if (conn.State != ConnectionState.Open)
                conn.Open();

            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // 插入 SeaClearance
                    var seaClearanceId = conn.QuerySingle<int>(insertMainSql, new
                    {
                        FileName = Path.GetFileName(filePath),
                        UploadOpe = userId
                    }, transaction: tran);

                    // 插入 SeaClearanceDetail
                    foreach (var item in list)
                    {
                       var detailId = conn.QuerySingle<int>(insertDetailSql, new
                        {
                            SeaClearanceId = seaClearanceId,
                            DataDate = item.DataDate,
                            MainNumber = item.MainNumber,
                            MftNo = item.MftNo,
                            TrackingNo = item.TrackingNo,
                            IsSucess = item.IsSucess,
                            Memo = item.Memo,
                            ImportDate = item.ImportDate,
                            DeclNo = item.DeclNo,
                            ProDateTime = item.ProDateTime,
                            IsSeaOrderOriginal = item.SeaOrderOriginals.Any(),
                            Tax = item.Tax,
                        }, transaction: tran);

                        // 插入 SeaClearanceDetailOriginalMapping
                        foreach (var seaOrderOriginal in item.SeaOrderOriginals)
                        {
                            conn.Execute(insertOriginalMapping, new
                            {
                                SeaClearanceDetailId = detailId,
                                SeaOrderOriginalId = seaOrderOriginal.SeaOrderOriginalId,
                                CreateDate = seaOrderOriginal.CreateDate,
                                Modifyby = seaOrderOriginal.Modifyby,
                                Post_Entry = seaOrderOriginal.Post_Entry,
                                Eta = seaOrderOriginal.Eta,
                                Cust_Code = seaOrderOriginal.Cust_Code,
                                Piece = seaOrderOriginal.Piece,
                                Importer = seaOrderOriginal.Importer,
                                Im_Phoneno = seaOrderOriginal.Im_Phoneno,
                                Importer_Id = seaOrderOriginal.Importer_Id,
                                Tax_Payment = seaOrderOriginal.Tax_Payment,
                                Item_Name = seaOrderOriginal.Item_Name,
                                Jetf_Serial = seaOrderOriginal.Jetf_Serial,
                                Gw = seaOrderOriginal.Gw,
                                CC = seaOrderOriginal.CC,
                            }, transaction: tran);
                        }
                    }

                    // 確認寫入
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    resopnseModel = new ResopnseModel(ex.Message);

                    // 取消寫入
                    tran.Rollback();
                }
                finally 
                {
                    conn.Close();
                }
            }

            return resopnseModel;
        }

        void GetGb326(List<SeaClearanceDetailQueryModel> list) 
        { 
            foreach(var item in list)
            {
                if (item.IsSucess == false)
                {
                    continue;
                }

               var parameters = new Dictionary<string, string>
               {
                   { "tab1.currentPage", "1" },
                   { "tab1.rowNum", "10" },
                   { "tab1.hideDeclNo", "" },
                   { "tab1.vslRegNo", item.MftNo },
                   { "tab1.mftNo", "" },
                   { "choice", "B" },
                   { "tab1.mawb", item.MainNumber },
                   { "tab1.hawb", item.TrackingNo }
               };
               
                var result = _cptPortalApi.GetGb326(parameters);

                item.ImportDate = result?.ImportDate;
            }
        }

       
        void CheckList(List<SeaClearanceDetailQueryModel> list) 
        {
            var sql = @"
            declare @Detail Table 
            ( 
               MainNumber nvarchar(100),
               TrackingNo nvarchar(100)
            )

            {0}
            
            select a.MainNumber,b.TrackingNo from [jetf].[dbo].[SeaClearanceDetail] a
            join @Detail b on a.MainNumber=b.MainNumber and a.TrackingNo=b.TrackingNo
            where a.IsSucess = 1";

            var sb = new StringBuilder();
            foreach (var item in list.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @Detail VALUES {string.Join(",",
                    item.Select(r => $"('{r.MainNumber}','{r.TrackingNo}')"))};");
            }

            sql = string.Format(sql, sb.ToString());

            var result = conn.Query<SeaClearanceDetailQueryModel>(sql)
             .ToList();

            foreach (var item in list)
            {
                //資料存在，重複上傳
                if (result.Any(r => r.MainNumber == item.MainNumber && r.TrackingNo == item.TrackingNo))
                {
                    item.IsSucess = false;
                    item.Memo = "重複上傳";
                    continue;
                }
              
                if (!item.SeaOrderOriginals.Any())
                {
                    item.IsSucess = false;
                    item.Memo = "找不到原單";
                    continue;
                }

                if (item.SeaOrderOriginals.Any(r => string.IsNullOrEmpty(r.Modifyby)))
                {
                    item.IsSucess = false;
                    item.Memo = "找不到倉別";
                    continue;
                }

                //成功
                item.IsSucess = true;

            }
        }

        /// <summary>
        /// 取得海運原單資料
        /// </summary>
        /// <param name="trackingNo"></param>
        /// <returns></returns>
        void GetSeaOrderOriginal(List<SeaClearanceDetailQueryModel> list) 
        {
           var sql = @"
                     declare @SeaOriginalMapping Table
                     ( 
                         MAINNUMBER nvarchar(100),
                         BL_NO nvarchar(100)
                     )

                    {0}

                    with cte_CES_MAIN_ORDER as
                    (
	                    select distinct MAIN_NUMBER,b.NAME as MODIFYBY from [DATA_CENTER].[dbo].[CES_MAIN_ORDER] a
	                    join [DATA_CENTER].[dbo].[SYS_PARAM] b on a.CLEARANCE_CP=b.CODE
                        where a.TYPE='ER'
                    )
 
                    select ROW_ID as SeaOrderOriginalId,a.MainNumber,a.BL_NO,CreateDate, Post_Entry, Eta, Despatch_Name as Cust_Code,Piece, Importer, Im_Phoneno, Importer_Id, Tax_Payment, Item_Name,Jetf_Serial, Gw, CC,
                    c.Modifyby
                    from @SeaOriginalMapping a
                    join [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] b on a.MainNumber =b.MAINNUMBER and a.BL_NO=b.BL_NO
                    left join cte_CES_MAIN_ORDER c on a.MainNumber = c.MAIN_NUMBER
                  ";

            var sb = new StringBuilder();
            foreach (var item in list.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @SeaOriginalMapping VALUES {string.Join(",",
                    item.Select(r => $"('{r.MainNumber}','{r.TrackingNo}')"))};");
            }

            sql = string.Format(sql, sb.ToString());

            var result = conn.Query<SeaOrderOriginalModel>(sql)
                .ToList();

            foreach (var item in list)
            {
                item.SeaOrderOriginals = result
                    .Where(r=> r.MainNumber == item.MainNumber && r.Bl_No == item.TrackingNo)
                    .ToList();
            }
        }

        /// <summary>
        /// 取得掛號
        /// </summary>
        /// <param name="list"></param>
        void GetMftNo(List<SeaClearanceDetailQueryModel> list)
        {
            var sql = @"
                        select top 1 FIELD_A as MftNo from DATA_CENTER.dbo.CES_MAIN_ORDER
                        where MAIN_NUMBER =@MainNumber
                      ";

            var mainNumbers = list.Where(x => x.IsSucess)
                .Select(x => x.MainNumber).Distinct()
                .ToList();

            foreach (var mainNumber in mainNumbers)
            {
                var mftNo = conn.Query<string>(sql, new 
                { 
                    MainNumber = mainNumber 
                }).FirstOrDefault();

                list.Where(x => x.MainNumber == mainNumber).ToList().ForEach(x => x.MftNo = mftNo);
            }
        }

        /// <summary>
        /// 取得稅金
        /// </summary>
        /// <param name="list"></param>
        //void GetTax(List<SeaClearanceDetailModel> list)
        //{
        //    var sql = @"
        //              select sum(TAX_AMOUNT) from DATA_CENTER.dbo.CLEARANCE_TAX
        //              where MERGE_NUMBER=@MERGE_NUMBER and TAX_AMOUNT > 0";

        //    foreach(var item in list)
        //    {
        //        var result = conn.Query<int?>(sql, new { MERGE_NUMBER = item.TrackingNo }).FirstOrDefault();
        //        item.Tax = result;
        //    }
        //}

        /// <summary>
        /// 取得稅金
        /// </summary>
        /// <param name="list"></param>
        void GetTax(List<SeaClearanceDetailQueryModel> list)
        {
            if (!list.Any())
                return;

            var sql = @"
                        declare @TrackingNumbers Table
                        (
                            TrackingNo nvarchar(100)
                        )

                        {0}

                        select a.TrackingNo, sum(b.TAX_AMOUNT) as Tax
                        from @TrackingNumbers a
                        left join DATA_CENTER.dbo.CLEARANCE_TAX b on a.TrackingNo = b.MERGE_NUMBER and b.TAX_AMOUNT > 0
                        group by a.TrackingNo
                        ";

            var sb = new StringBuilder();
            foreach (var item in list.Batch(1000))
            {
                sb.AppendLine($@"INSERT INTO @TrackingNumbers VALUES {string.Join(",",
                    item.Select(r => $"('{r.TrackingNo}')"))};");
            }

            sql = string.Format(sql, sb.ToString());

            var taxResults = conn.Query<TaxResult>(sql).ToList();

            // 將結果映射回原始列表
            foreach (var item in list)
            {
                var taxResult = taxResults.FirstOrDefault(r => r.TrackingNo == item.TrackingNo);
                item.Tax = taxResult?.Tax;
            }
        }

        public List<SeaClearanceModel> GetSeaClearance()
        {
            var sqlQuery = "SELECT * FROM jetf.dbo.SeaClearance order by Id desc";

            return conn.Query<SeaClearanceModel>(sqlQuery).ToList();
        }

        /// <summary>
        /// 上傳結果
        /// </summary>
        public List<UploadResultModel> GetUploadResult(int id) 
        {
            var sql = @"
                        select DataDate,MainNumber,TrackingNo,IsSucess,Memo from [jetf].[dbo].[SeaClearanceDetail]
                        where SeaClearanceId = @Id
                        ";
           return conn.Query<UploadResultModel>(sql,
                new
                {
                    Id = id
                }).ToList();
        }

    }
}
