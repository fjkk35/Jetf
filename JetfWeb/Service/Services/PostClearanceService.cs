using Dapper;
using NPOI.POIFS.Storage;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using Service.Models.PostClearance;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class PostClearanceService
    {
        private SqlConnection conn;

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle, date2Style;

        /// <summary>
        /// 建構式
        /// </summary>
        public PostClearanceService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 上傳檔案 海快錯單袋號
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFile(string filePath, string dataDate, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
            //讀取Excel 
            List<PostClearanceUploadModel> modelList = ReadExcelPostClearance(filePath);
            //新增
            if (modelList.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dataDate = Convert.ToDateTime(dataDate).ToString("yyyyMMdd");

                resopnseModel = InsertPostClearanceUpload(modelList, dataDate, upload_time, userId);

                if (resopnseModel.status == Status.success)
                {
                    resopnseModel.msg = $"{upload_time}︿{userId}";
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
        /// 讀取Excel後段報關費用
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<PostClearanceUploadModel> ReadExcelPostClearance(string filePath)
        {
            List<PostClearanceUploadModel> modelList = new List<PostClearanceUploadModel>();
            PostClearanceUploadModel model;


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
                    //讀到表頭 下一行開始寫入資料
                    if ((sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "匯入日期") &&
                        (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "分提單號") &&
                        (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "傳輸日"))
                    {
                        read = true;
                        continue;
                    }

                    if (read)
                    {
                        model = new PostClearanceUploadModel();
                        //匯入日期
                        if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).CellType == CellType.Numeric)
                        {
                            model.ImportDate = Convert.ToDateTime(sheet.GetRow(i).GetCell(0).DateCellValue).ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            model.ImportDate = sheet.GetRow(i).GetCell(0) == null ? "" : Convert.ToDateTime(sheet.GetRow(i).GetCell(0)).ToString("yyyy/MM/dd");
                        }
                        //分號
                        model.BlNo = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                        //傳輸日
                        if (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).CellType == CellType.Numeric)
                        {
                            model.TransferDate = Convert.ToDateTime(sheet.GetRow(i).GetCell(2).DateCellValue).ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            model.TransferDate = sheet.GetRow(i).GetCell(2) == null ? "" : Convert.ToDateTime(sheet.GetRow(i).GetCell(2)).ToString("yyyy/MM/dd");
                        }
                        //出倉日
                        if (sheet.GetRow(i).GetCell(3) != null && sheet.GetRow(i).GetCell(3).CellType == CellType.Numeric)
                        {
                            model.SignOutDate = Convert.ToDateTime(sheet.GetRow(i).GetCell(3).DateCellValue).ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            model.SignOutDate = sheet.GetRow(i).GetCell(3) == null ? "" : Convert.ToDateTime(sheet.GetRow(i).GetCell(3)).ToString("yyyy/MM/dd");
                        }
                        //MAIL
                        model.Mail = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                        //報關類別
                        model.ClearanceType = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                        //倉儲
                        model.DataType = sheet.GetRow(i).GetCell(6) == null ? "" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                        //材積數
                        if (sheet.GetRow(i).GetCell(7) != null && sheet.GetRow(i).GetCell(7).CellType == CellType.Formula)
                        {
                            model.Volume = sheet.GetRow(i).GetCell(7).NumericCellValue.ToString();
                        }
                        else
                        {
                            model.Volume = sheet.GetRow(i).GetCell(7) == null ? "" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                        }

                        //X類稅金
                        model.XTax = sheet.GetRow(i).GetCell(8) == null ? "" : sheet.GetRow(i).GetCell(8).ToString().Trim();
                        //G類稅金
                        model.GTax = sheet.GetRow(i).GetCell(9) == null ? "" : sheet.GetRow(i).GetCell(9).ToString().Trim();
                        //滯報費減免
                        model.FeeReduction = sheet.GetRow(i).GetCell(10) == null ? "" : sheet.GetRow(i).GetCell(10).ToString().Trim();
                        //倉租天數減免
                        model.WarehouseRentDaysReduction = sheet.GetRow(i).GetCell(11) == null ? "" : sheet.GetRow(i).GetCell(11).ToString().Trim();
                        //報關費2
                        model.ClearanceFee2 = sheet.GetRow(i).GetCell(12) == null ? "" : sheet.GetRow(i).GetCell(12).ToString().Trim();
                        //報單收費方式
                        model.ClearanceFeeType = sheet.GetRow(i).GetCell(13) == null ? "" : sheet.GetRow(i).GetCell(13).ToString().Trim();
                        //稅金付款人
                        model.TaxPayer = sheet.GetRow(i).GetCell(14) == null ? "" : sheet.GetRow(i).GetCell(14).ToString().Trim();
                        //客戶
                        model.Customer = sheet.GetRow(i).GetCell(15) == null ? "" : sheet.GetRow(i).GetCell(15).ToString().Trim();
                        //實際交派日
                        if (sheet.GetRow(i).GetCell(16) != null && sheet.GetRow(i).GetCell(16).CellType == CellType.Numeric)
                        {
                            model.ActualDate = Convert.ToDateTime(sheet.GetRow(i).GetCell(16).DateCellValue).ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            if (DateTime.TryParse(sheet.GetRow(i).GetCell(16)?.StringCellValue, out DateTime dateValue) && dateValue != DateTime.MinValue)
                            {
                                model.ActualDate = dateValue.ToString("yyyy/MM/dd");
                            }
                            else
                            {
                                model.ActualDate = sheet.GetRow(i).GetCell(16) == null ? "" : sheet.GetRow(i).GetCell(16).ToString();
                            }
                        }
                        //備註
                        model.Remark = sheet.GetRow(i).GetCell(17) == null ? "" : sheet.GetRow(i).GetCell(17).ToString().Trim();
                        modelList.Add(model);
                    }
                }
            }
            return modelList;
        }

        /// <summary>
        /// 寫入上傳檔案 後段報關
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel InsertPostClearanceUpload(List<PostClearanceUploadModel> modelList, string dataDate, string upload_Time, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[PostClearanceUpload](ImportDate, BlNo, TransferDate, SignOutDate, Mail, ClearanceType, DataType, Volume, XTax, GTax, FeeReduction, ClearanceFee2, ClearanceFeeType, TaxPayer, Customer, ActualDate, Remark,WarehouseRentDaysReduction, Upload_Ope, UPLOAD_TIME) ");
            sb.Append("values(@ImportDate,@BlNo,@TransferDate,@SignOutDate,@Mail,@ClearanceType,@DataType,@Volume,@XTax,@GTax,@FeeReduction,@ClearanceFee2,@ClearanceFeeType,@TaxPayer,@Customer,@ActualDate,@Remark,@WarehouseRentDaysReduction,@Upload_Ope,@UPLOAD_TIME) ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        foreach (var item in modelList)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@ImportDate", SqlDbType.NVarChar).Value = item.ImportDate;
                            cmd.Parameters.Add("@BlNo", SqlDbType.NVarChar).Value = item.BlNo;
                            cmd.Parameters.Add("@TransferDate", SqlDbType.NVarChar).Value = item.TransferDate;
                            cmd.Parameters.Add("@SignOutDate", SqlDbType.NVarChar).Value = item.SignOutDate;
                            cmd.Parameters.Add("@Mail", SqlDbType.NVarChar).Value = item.Mail;
                            cmd.Parameters.Add("@ClearanceType", SqlDbType.NVarChar).Value = item.ClearanceType;
                            cmd.Parameters.Add("@DataType", SqlDbType.NVarChar).Value = item.DataType;
                            cmd.Parameters.Add("@Volume", SqlDbType.NVarChar).Value = item.Volume;
                            cmd.Parameters.Add("@XTax", SqlDbType.NVarChar).Value = item.XTax;
                            cmd.Parameters.Add("@GTax", SqlDbType.NVarChar).Value = item.GTax;
                            cmd.Parameters.Add("@FeeReduction", SqlDbType.NVarChar).Value = item.FeeReduction;
                            cmd.Parameters.Add("@ClearanceFee2", SqlDbType.NVarChar).Value = item.ClearanceFee2;
                            cmd.Parameters.Add("@ClearanceFeeType", SqlDbType.NVarChar).Value = item.ClearanceFeeType;
                            cmd.Parameters.Add("@TaxPayer", SqlDbType.NVarChar).Value = item.TaxPayer;
                            cmd.Parameters.Add("@Customer", SqlDbType.NVarChar).Value = item.Customer;
                            cmd.Parameters.Add("@ActualDate", SqlDbType.NVarChar).Value = item.ActualDate != "" ? item.ActualDate : (object)DBNull.Value;
                            cmd.Parameters.Add("@Remark", SqlDbType.NVarChar).Value = item.Remark;
                            cmd.Parameters.Add("@WarehouseRentDaysReduction", SqlDbType.NVarChar).Value = item.WarehouseRentDaysReduction;
                            cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                            cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = userId;
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

        /// <summary>
        /// 後段報關資料
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public DataTable GetPostClearance(string upload_time, string user_Id)
        {
            DataTable dt = new DataTable();

            StringBuilder sb = new StringBuilder();
            sb.Append("select a.Id,ImportDate,BlNo,b.MainNumber,b.DESPATCH_NAME as CUST_CODE,b.JETF_SERIAL,b.TRANS_NAME,a.DataType,a.ClearanceType,c.CUST_NAME,a.TransferDate,b.ETA,a.SignOutDate,a.ActualDate,a.Mail,a.Volume,a.ClearanceFee2,a.XTax,a.GTax,a.FeeReduction,a.ClearanceFeeType,a.TaxPayer,a.Customer,a.Remark,d.TAX_NUMBER,b.CC,a.WarehouseRentDaysReduction,b.GW,e.TaxNumber as TaxNumberFile,f.DataDate as UnboxingDate from [jetf].[dbo].[PostClearanceUpload] a ");
            sb.Append("left join DATA_CENTER.[dbo].[SEA_ORDER_ORIGINAL] b on a.BlNo=b.BL_NO ");
            sb.Append("left join [DATA_CENTER].[dbo].[SYS_CUST] c on b.DESPATCH_NAME =c.CUST_CODE ");
            sb.Append("left join [DATA_CENTER].[dbo].[CLEARANCE_TAX] d on a.BlNo = d.BAG_NUMBER ");
            sb.Append("left join [jetf].[dbo].[Clearance_Tax_Pdf] e on d.TAX_NUMBER = e.TaxNumber ");
            sb.Append("left join jetf.dbo.SeaUnboxingRecord f on b.MainNumber = f.MainNumber ");
            sb.Append("where a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                da.SelectCommand.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 後段報關Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        public IWorkbook GetPostClearanceWorkbook(string upload_time, string upload_ope)
        {
            IWorkbook workbook = new XSSFWorkbook();

            //取得後段報關明細上傳資料
            DataTable dt_Report = GetPostClearance(upload_time, upload_ope);

            //取得後段報關匯出Excel資料
            List<PostClearanceModel> list = GetPostClearanceList(dt_Report);

            //產生EXCEL頁籤
            GetPostClearanceSheet(workbook, list, "總表");

            //產生EXCEL頁籤
            GetPostClearanceSheet2(workbook, list, "代收檔");
            return workbook;
        }

        List<PostClearanceModel> GetPostClearanceList(DataTable dt_Report)
        {
            var group = dt_Report.AsEnumerable()
                         .GroupBy(g => new { Id = g.Field<int>("Id") })
                         .OrderBy(g => g.Key.Id)
                         .Select(g => new PostClearanceModel
                         {
                             ImportDate = g.Select(r => r.Field<DateTime?>("ImportDate")).FirstOrDefault(),
                             BlNo = g.Select(r => r.Field<string>("BlNo")).FirstOrDefault(),
                             UnboxingDate = g.Select(r => r.Field<string>("UnboxingDate"))
                                            .Where(s => !string.IsNullOrWhiteSpace(s))
                                            .Select(s => (DateTime?)DateTime.Parse(s))
                                            .FirstOrDefault(),
                             TaxNumber = g.Select(r => r.Field<string>("TAX_NUMBER")).FirstOrDefault(),
                             TaxNumberFile = g.Select(r => r.Field<string>("TaxNumberFile")).FirstOrDefault(),
                             JetfSerial = string.Join(",",
                                                         g.OrderByDescending(r => r.Field<decimal?>("GW"))
                                                         .Select(r => r.Field<string>("JETF_SERIAL"))
                                                         .Distinct()),
                             CC = g.Select(r => r.Field<double?>("CC")).FirstOrDefault() ?? 0,
                             TransName = g.Select(r => r.Field<string>("TRANS_NAME")).FirstOrDefault(),
                             DataType = g.Select(r => r.Field<string>("DataType")).FirstOrDefault(),
                             ClearanceType = g.Select(r => r.Field<string>("ClearanceType")).FirstOrDefault(),
                             CustCode = g.Select(r => r.Field<string>("CUST_CODE")).FirstOrDefault(),
                             CustName = g.Select(r => r.Field<string>("CUST_NAME")).FirstOrDefault(),
                             TransferDate = g.Select(r => r.Field<DateTime?>("TransferDate")).FirstOrDefault(),
                             Eta = g.Select(r => r.Field<DateTime?>("ETA")).FirstOrDefault(),
                             SignOutDate = g.Select(r => r.Field<DateTime?>("SignOutDate")).FirstOrDefault(),
                             ActualDate = g.Select(r => r.Field<DateTime?>("ActualDate")).FirstOrDefault(),
                             Mail = g.Select(r => r.Field<string>("Mail")).FirstOrDefault(),
                             Volume = g.Select(r => r.Field<int>("Volume")).FirstOrDefault(),
                             ClearanceFee2 = g.Select(r => r.Field<int>("ClearanceFee2")).FirstOrDefault(),
                             XTax = g.Select(r => r.Field<int>("XTax")).FirstOrDefault(),
                             GTax = g.Select(r => r.Field<int>("GTax")).FirstOrDefault(),
                             WarehouseRentDaysReduction = g.Select(r => r.Field<int>("WarehouseRentDaysReduction")).FirstOrDefault(),
                             FeeReduction = g.Select(r => r.Field<int>("FeeReduction")).FirstOrDefault(),
                             ClearanceFeeType = g.Select(r => r.Field<string>("ClearanceFeeType")).FirstOrDefault(),
                             TaxPayer = g.Select(r => r.Field<string>("TaxPayer")).FirstOrDefault(),
                             Customer = g.Select(r => r.Field<string>("Customer")).FirstOrDefault(),
                             Remark = g.Select(r => r.Field<string>("Remark")).FirstOrDefault(),
                         }).ToList();


            //如果是客戶是CN00165、 CN00132、 CN00145，就抓取速派新遞貨號內的單號
            var custCodes = new List<string> { "CN00165", "CN00132", "CN00145" };
            var bagNumbers = group.Where(r => custCodes.Contains(r.CustCode))
                .Select(r => r.BlNo)
                .Distinct()
                .ToList();

            var shenzhenCargoDic = GetShenzhenCargos(bagNumbers);

            foreach (var item in group)
            {
                #region 取得速派新遞貨號
                if (shenzhenCargoDic.ContainsKey(item.BlNo))
                {
                    item.JetfSerial = shenzhenCargoDic[item.BlNo];
                }
                #endregion

                #region 系統註記
                var memo = new List<string>();

                if (string.IsNullOrEmpty(item.JetfSerial))
                    memo.Add("系統無資料");

                if (!item.UnboxingDate.HasValue)
                    memo.Add("拆櫃日無日期");

                item.SystemMemo = string.Join("、", memo);
                #endregion


                #region 倉租天數 倉租數量
                if (item.ClearanceType == "移倉" || item.ClearanceType == "轉移倉")
                {
                    //if (item.DataType == "高雄郵聯(億興)" || item.DataType == "高雄郵聯(全旺)")
                    //{
                    //    //倉租天數
                    //    item.WarehouseRentDays = (int)(Convert.ToDateTime(item.SignOutDate) - Convert.ToDateTime(item.TransferDate)).TotalDays + 1 - item.WarehouseRentDaysReduction;
                    //    //倉租數量
                    //    item.WarehouseRentCount = item.WarehouseRentDays * item.Volume;
                    //}
                    if ((item.DataType == "TPCT" || item.ClearanceType == "移倉" || item.ClearanceType == "轉移倉") && item.UnboxingDate.HasValue)
                    {
                        //倉租天數
                        item.WarehouseRentDays = (int)(Convert.ToDateTime(item.SignOutDate) - item.UnboxingDate.Value).TotalDays - item.WarehouseRentDaysReduction + 1;
                        //倉租數量
                        item.WarehouseRentCount = item.WarehouseRentDays * item.Volume;
                    }
                    else
                    {
                        item.WarehouseRentDays = 0;
                        item.WarehouseRentCount = 0;
                    }
                }
                else
                {
                    item.WarehouseRentDays = 0;
                    item.WarehouseRentCount = 0;
                }
                #endregion

                #region 報關費1
                //報關費1
                if (item.CustName == "捷利" ||
                    item.CustName == "巧巧郎")
                {
                    switch (item.ClearanceType)
                    {
                        case "X2":
                            item.ClearanceFee = 100;
                            break;
                        case "X3":
                            item.ClearanceFee = 100;
                            break;
                        case "G1":
                            item.ClearanceFee = 300;
                            break;
                        case "轉G1":
                            item.ClearanceFee = 300;
                            break;
                        case "移倉":
                            item.ClearanceFee = 1000;
                            break;
                        case "轉移倉":
                            item.ClearanceFee = 1000;
                            break;
                    }
                }
                else if (item.CustName == "菜鳥網絡(自營外代)" ||
                         //item.CustName == "巧巧郎" ||
                         item.CustName == "菜鳥網絡(自營翔碩)" ||
                         item.CustName == "菜鳥網絡(自營海絲達)" ||
                         item.CustName == "淘寶(飛迅馳)" ||
                         item.CustName == "海克力斯" ||
                         item.CustName == "菜鳥網絡(中外運)" ||
                         item.CustName == "菜鳥網絡(自營中外運)" ||
                         item.CustName == "網訊" ||
                         item.CustName == "萬達" ||
                         item.CustName == "速派" ||
                         item.CustName == "牽禮馬" ||
                         item.CustName == "天馬" ||
                         item.CustName == "新遞" ||
                         item.CustName == "攜誠" ||
                         item.CustName == "騰揚" ||
                         item.CustName == "金祥富(海絲)" ||
                         item.CustName == "金祥富(海絲拼多多)"
                         )
                {
                    switch (item.ClearanceType)
                    {
                        case "X2":
                            item.ClearanceFee = 500;
                            break;
                        case "X3":
                            item.ClearanceFee = 500;
                            break;
                        case "G1":
                            item.ClearanceFee = 1500;
                            break;
                        case "轉G1":
                            item.ClearanceFee = 1500;
                            break;
                        case "移倉":
                            item.ClearanceFee = 1500;
                            break;
                        case "轉移倉":
                            item.ClearanceFee = 1500;
                            break;
                    }
                }
                else if (
                    item.CustName == "超峰" ||
                    item.CustName == "深圳超峰" ||
                    item.CustName == "台星")
                {
                    switch (item.ClearanceType)
                    {
                        case "X2":
                            item.ClearanceFee = 200;
                            break;
                        case "X3":
                            item.ClearanceFee = 200;
                            break;
                        case "G1":
                            item.ClearanceFee = 600;
                            break;
                        case "轉G1":
                            item.ClearanceFee = 600;
                            break;
                        case "移倉":
                            item.ClearanceFee = 1500;
                            break;
                        case "轉移倉":
                            item.ClearanceFee = 1500;
                            break;
                    }
                }
                else if (item.CustName == "RINCOS")
                {
                    switch (item.ClearanceType)
                    {
                        case "X2":
                            item.ClearanceFee = 100;
                            break;
                        case "X3":
                            item.ClearanceFee = 100;
                            break;
                        case "G1":
                            item.ClearanceFee = 1800;
                            break;
                        case "轉G1":
                            item.ClearanceFee = 1800;
                            break;
                        case "移倉":
                            item.ClearanceFee = 1800;
                            break;
                        case "轉移倉":
                            item.ClearanceFee = 1800;
                            break;
                    }
                }
                #endregion

                #region 機械使用費
                if (item.ClearanceType == "移倉" || item.ClearanceType == "轉移倉")
                {
                    //機械使用費
                    item.MachineryUsageFee = 70;
                    //倉租
                    item.WarehouseRent = 40;
                    //移倉費
                    item.RelocationFee = 265;
                    //數量(移倉)
                    item.RelocationCount = item.Volume;
                    //EDI傳輸費
                    item.EdiShippingFee = 100;
                    //數量(EDI傳輸)
                    item.EdiShippingCount = 1;
                    //處理費
                    item.HandlingFee = 600;
                    //數量(處理費)
                    item.HandlingCount = 1;
                }
                else
                {
                    //機械使用費
                    item.MachineryUsageFee = 0;
                    //倉租
                    item.WarehouseRent = 0;
                    //移倉費
                    item.RelocationFee = 0;
                    //數量(移倉)
                    item.RelocationCount = 0;
                    //EDI傳輸費
                    item.RelocationCount = 0;
                    //數量(EDI傳輸)
                    item.EdiShippingCount = 0;
                    //處理費
                    item.HandlingFee = 0;
                    //數量(處理費)
                    item.HandlingCount = 0;
                }
                #endregion

                #region 發票金額
                item.InvoiceAmount = (item.ClearanceFee.HasValue ? item.ClearanceFee.Value : 0) +
                                     (item.ClearanceFee2) +
                                     (item.MachineryUsageFee * item.Volume) +
                                     (item.WarehouseRent * item.WarehouseRentCount) +
                                     (item.RelocationFee * item.RelocationCount) +
                                     (item.EdiShippingFee * item.EdiShippingCount) +
                                     (item.HandlingFee * item.HandlingCount);
                #endregion

                #region 發票稅額
                item.InvoiceTax = Math.Ceiling(item.InvoiceAmount * 0.05);
                #endregion

                #region 收據金額
                item.ReceiptAmount = item.XTax + item.GTax + item.FeeReduction;
                #endregion

                #region 總計(未含代收手續費)
                item.Total = item.InvoiceAmount + item.InvoiceTax + item.ReceiptAmount;
                #endregion

                #region 派送手續費
                if (item.Total <= 1970)
                {
                    item.DeliveryFee = 30;
                }
                else if (item.Total >= 1971 && item.Total <= 4940)
                {
                    item.DeliveryFee = 60;
                }
                else if (item.Total >= 4941 && item.Total <= 9910)
                {
                    item.DeliveryFee = 90;
                }
                else if (item.Total >= 9911 && item.Total <= 19880)
                {
                    item.DeliveryFee = 120;
                }
                else if (item.Total >= 19881 && item.Total <= 49850)
                {
                    item.DeliveryFee = 150;
                }
                else
                {
                    item.DeliveryFee = 0;
                }
                #endregion

                #region 總計(含代收手續費)
                item.Total2 = item.Total + item.DeliveryFee;
                #endregion

                #region 發票金額+代收
                item.InvoiceAndCollectibleAmount = item.InvoiceAmount + item.DeliveryFee;
                #endregion

                #region 派送加值1%
                if (item.Total2 > 10000 && item.Total <= 49850)
                {
                    item.DeliverySurcharge = Math.Ceiling(item.Total2 * 0.01);
                }
                else
                {
                    item.DeliverySurcharge = 0;
                }
                #endregion

                #region 派送手續費總額
                if ((item.ClearanceFeeType == "客戶" && item.TaxPayer == "代收") ||
                    (item.ClearanceFeeType == "代收" && item.TaxPayer == "客戶") ||
                    (item.ClearanceFeeType == "代收" && item.TaxPayer == "代收"))
                {
                    item.TotalDeliveryAmount = item.DeliveryFee + item.DeliverySurcharge;
                }
                else
                {
                    item.TotalDeliveryAmount = 0;
                }
                #endregion

                #region 總計(含代收+加值)
                item.Total3 = item.Total2 + item.DeliverySurcharge;
                #endregion

                #region 稅金類別
                if (item.TaxPayer == "代收")
                {
                    item.TaxType = "N";
                }
                else if (item.TaxPayer == "匯款")
                {
                    item.TaxType = "D";
                }
                else if (item.TaxPayer == "客戶")
                {
                    item.TaxType = "C";
                }
                else if (item.TaxPayer == "捷豐")
                {
                    item.TaxType = "Y";
                }
                #endregion

                #region 稅金類別備註
                if (item.TaxType == "N")
                {
                    item.TaxTypeRemark = "代收不包稅";
                }
                else if (item.TaxType == "D")
                {
                    item.TaxTypeRemark = "收客匯款";
                }
                else if (item.TaxType == "C")
                {
                    item.TaxTypeRemark = "客戶付款";
                }
                else if (item.TaxType == "Y")
                {
                    item.TaxTypeRemark = "代收包稅";
                }
                #endregion

                #region 倉儲(代收檔)
                if (item.ClearanceType == "移倉" || item.ClearanceType == "轉移倉")
                {
                    item.CollectibleDataType = "G類";
                }
                else
                {
                    switch (item.DataType)
                    {
                        case "高雄郵聯(億興)":
                        case "高雄郵聯(全旺)":
                            item.CollectibleDataType = "CHWN";
                            break;
                        case "高雄郵聯(捷豐)":
                            item.CollectibleDataType = "JFKH";
                            break;
                        default:
                            item.CollectibleDataType = item.DataType;
                            break;
                    }
                }
                #endregion

                #region 稅金單編號
                if (item.ClearanceType == "轉G1" || item.ClearanceType == "移倉" || item.ClearanceType == "轉移倉")
                {
                    item.TaxNumber = "";
                }
                #endregion

            }

            return group;

        }

        /// <summary>
        /// 取得速派新遞貨號
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetShenzhenCargos(List<string> list)
        {
            var sql = @"select TrackingNo,DeliveryNo from jetf.dbo.ShenzhenCargo
                        where TrackingNo IN @TrackingNo";

            var result = conn.Query<dynamic>(sql, new { TrackingNo = list });

            return result
                .GroupBy(x => (string)x.TrackingNo)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(",", g.Select(x => (string)x.DeliveryNo))
                );
        }

        /// <summary>
        /// 後段報關明細表Sheet(總表)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetPostClearanceSheet(IWorkbook workbook, List<PostClearanceModel> list, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);

            #region 表頭
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("匯入日期");
            row.CreateCell(1).SetCellValue("匯入序號");
            row.CreateCell(2).SetCellValue("單號");
            row.CreateCell(3).SetCellValue("派件單號");
            row.CreateCell(4).SetCellValue("派件公司");
            row.CreateCell(5).SetCellValue("倉儲");
            row.CreateCell(6).SetCellValue("報關類別");
            row.CreateCell(7).SetCellValue("客戶");
            row.CreateCell(8).SetCellValue("傳輸日");
            row.CreateCell(9).SetCellValue("到港日");
            row.CreateCell(10).SetCellValue("出倉日");
            row.CreateCell(11).SetCellValue("實際交派日");
            row.CreateCell(12).SetCellValue("MAIL");
            row.CreateCell(13).SetCellValue("材積數");
            row.CreateCell(14).SetCellValue("倉租天數");

            row.CreateCell(15).SetCellValue("報關費用1");
            row.CreateCell(16).SetCellValue("數量");
            row.CreateCell(17).SetCellValue("報關費用2");
            row.CreateCell(18).SetCellValue("數量");
            row.CreateCell(19).SetCellValue("機械使用費");
            row.CreateCell(20).SetCellValue("數量");
            row.CreateCell(21).SetCellValue("倉租");
            row.CreateCell(22).SetCellValue("數量");
            row.CreateCell(23).SetCellValue("移倉費");
            row.CreateCell(24).SetCellValue("數量");
            row.CreateCell(25).SetCellValue("EDI傳輸費");
            row.CreateCell(26).SetCellValue("數量");
            row.CreateCell(27).SetCellValue("處理費");
            row.CreateCell(28).SetCellValue("數量");
            row.CreateCell(29).SetCellValue("派送手續費");
            row.CreateCell(30).SetCellValue("派送加值1%");
            row.CreateCell(31).SetCellValue("派送手續費總額");
            row.CreateCell(32).SetCellValue("數量");
            row.CreateCell(33).SetCellValue("三聯稅單");
            row.CreateCell(34).SetCellValue("四聯稅單");
            row.CreateCell(35).SetCellValue("滯報費減免");
            row.CreateCell(36).SetCellValue("稅金單編號");
            row.CreateCell(37).SetCellValue("發票金額");
            row.CreateCell(38).SetCellValue("發票金額+代收");
            row.CreateCell(39).SetCellValue("稅額5%");
            row.CreateCell(40).SetCellValue("收據金額");
            row.CreateCell(41).SetCellValue("總計(未含代收手續費)");
            row.CreateCell(42).SetCellValue("總計(含代收手續費)");
            row.CreateCell(43).SetCellValue("總計(含代收+加值)");
            row.CreateCell(44).SetCellValue("報單、稅金單比對");
            row.CreateCell(45).SetCellValue("報單收費方式");
            row.CreateCell(46).SetCellValue("稅金付款人");
            row.CreateCell(47).SetCellValue("客戶");
            row.CreateCell(48).SetCellValue("稅金");
            row.CreateCell(49).SetCellValue("報關費");
            row.CreateCell(50).SetCellValue("到付款");
            row.CreateCell(51).SetCellValue("備註");
            row.CreateCell(52).SetCellValue("系統註記");
            row.CreateCell(53).SetCellValue("比對稅金檔");
            row.CreateCell(54).SetCellValue("拆櫃日");


            int[] widthLarge = new[] { 7, 11, 41, 42, 43, 47, 52 };

            for (int i = 0; i < 55; i++)
            {
                //置中
                row.GetCell(i).CellStyle = cs_Center;
                if (Array.IndexOf(widthLarge, i) > -1)
                {
                    //寬度
                    sheet.SetColumnWidth(i, 7000);
                }
                else
                {
                    //寬度
                    sheet.SetColumnWidth(i, 5000);
                }
            }


            #endregion
            int irow = 1;
            foreach (var item in list)
            {
                row = sheet.CreateRow(irow);
                //匯入日期
                row.CreateCell(0).SetCellValue(item.ImportDate?.ToString("yyyy/MM/dd"));
                //匯入序號
                row.CreateCell(1).SetCellValue(irow);
                //單號
                row.CreateCell(2).SetCellValue(item.BlNo);
                //派件單號
                row.CreateCell(3).SetCellValue(item.JetfSerial);
                //派件公司
                row.CreateCell(4).SetCellValue(item.TransName);
                //倉儲
                row.CreateCell(5).SetCellValue(item.DataType);
                //報關類別
                row.CreateCell(6).SetCellValue(item.ClearanceType);
                //客戶
                row.CreateCell(7).SetCellValue(item.CustName);
                //傳輸日
                row.CreateCell(8).SetCellValue(item.TransferDate?.ToString("yyyy/MM/dd"));
                //到港日
                row.CreateCell(9).SetCellValue(item.Eta?.ToString("yyyy/MM/dd"));
                //出倉日
                row.CreateCell(10).SetCellValue(item.SignOutDate?.ToString("yyyy/MM/dd"));
                //實際交派日
                row.CreateCell(11).SetCellValue(item.ActualDate?.ToString("yyyy/MM/dd"));
                //MAIL
                row.CreateCell(12).SetCellValue(item.Mail);
                //材積數
                row.CreateCell(13).SetCellValue(item.Volume);
                //倉租天數
                row.CreateCell(14).SetCellValue(item.WarehouseRentDays);

                //報關費用1
                if (item.ClearanceFee.HasValue)
                    row.CreateCell(15).SetCellValue(item.ClearanceFee.Value);
                else
                    row.CreateCell(15).SetCellValue("錯誤");

                //數量 固定1
                row.CreateCell(16).SetCellValue(1);
                //報關費用2
                row.CreateCell(17).SetCellValue(item.ClearanceFee2);
                //數量 固定1
                row.CreateCell(18).SetCellValue(1);
                //機械使用費
                row.CreateCell(19).SetCellValue(item.MachineryUsageFee);
                //數量(材積數)
                row.CreateCell(20).SetCellValue(item.Volume);
                //倉租
                row.CreateCell(21).SetCellValue(item.WarehouseRent);
                //數量(倉租)
                row.CreateCell(22).SetCellValue(item.WarehouseRentCount);
                //移倉費
                row.CreateCell(23).SetCellValue(item.RelocationFee);
                //數量(移倉)
                row.CreateCell(24).SetCellValue(item.RelocationCount);
                //EDI傳輸費
                row.CreateCell(25).SetCellValue(item.EdiShippingFee);
                //數量(EDI傳輸)
                row.CreateCell(26).SetCellValue(item.EdiShippingCount);
                //處理費
                row.CreateCell(27).SetCellValue(item.HandlingFee);
                //數量(處理費)
                row.CreateCell(28).SetCellValue(item.HandlingCount);
                //派送手續費
                row.CreateCell(29).SetCellValue(item.DeliveryFee);
                //派送加值1%
                row.CreateCell(30).SetCellValue(item.DeliverySurcharge);
                //派送手續費總額
                row.CreateCell(31).SetCellValue(item.TotalDeliveryAmount);
                //數量
                row.CreateCell(32).SetCellValue(1);
                //X類稅金
                row.CreateCell(33).SetCellValue(item.XTax);
                //G類稅金
                row.CreateCell(34).SetCellValue(item.GTax);
                //滯報費減免
                row.CreateCell(35).SetCellValue(item.FeeReduction);
                //稅金單編號
                row.CreateCell(36).SetCellValue(item.TaxNumber);
                //發票金額
                row.CreateCell(37).SetCellValue(item.InvoiceAmount);
                //發票金額+代收
                row.CreateCell(38).SetCellValue(item.InvoiceAndCollectibleAmount);
                //稅額5%
                row.CreateCell(39).SetCellValue(item.InvoiceTax);
                //收據金額
                row.CreateCell(40).SetCellValue(item.ReceiptAmount);

                if (item.Total >= 49851 && !item.IsChangeToRemittance())
                {
                    //報單收費方式
                    row.CreateCell(45).SetCellValue("匯款");
                    //稅金付款人
                    row.CreateCell(46).SetCellValue("匯款");
                    //系統註記
                    string memo = string.IsNullOrWhiteSpace(item.SystemMemo)
                    ? "需改匯款"
                    : $"需改匯款、{item.SystemMemo}";

                    row.CreateCell(52).SetCellValue(memo);
                }
                else
                {
                    //報單收費方式
                    row.CreateCell(45).SetCellValue(item.ClearanceFeeType);
                    //稅金付款人
                    row.CreateCell(46).SetCellValue(item.TaxPayer);
                    //系統註記
                    row.CreateCell(52).SetCellValue(item.SystemMemo);
                }

                //總計(未含代收手續費)
                row.CreateCell(41).SetCellValue(item.Total);
                //總計(含代收手續費)
                row.CreateCell(42).SetCellValue(item.Total2);
                //總計(含代收+加值)
                row.CreateCell(43).SetCellValue(item.Total3);
                //報單、稅金單比對
                row.CreateCell(44).SetCellValue((item.ClearanceFeeType == item.TaxPayer) ? "TRUE" : "FALSE");
                //客戶
                row.CreateCell(47).SetCellValue(item.Customer);
                //稅金
                row.CreateCell(48).SetCellValue(item.ReceiptAmount);
                //報關費
                row.CreateCell(49).SetCellValue(item.InvoiceAmount + item.InvoiceTax);
                //到付款
                if (item.CC.HasValue)
                {
                    row.CreateCell(50).SetCellValue(item.CC.Value);
                }
                //備註
                row.CreateCell(51).SetCellValue(item.Remark);

                var isTaxNumberFile = !string.IsNullOrEmpty(item.TaxNumber) && !string.IsNullOrEmpty(item.TaxNumberFile) ? "有" : "無";

                //比對稅金檔
                row.CreateCell(53).SetCellValue(isTaxNumberFile);
                //拆櫃日
                row.CreateCell(54).SetCellValue(item.UnboxingDate?.ToString("yyyy-MM-dd"));

                irow++;
            }
        }

        /// <summary>
        /// 後段報關明細表Sheet(代收檔)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="list"></param>
        /// <param name="sheetName"></param>
        void GetPostClearanceSheet2(IWorkbook workbook, List<PostClearanceModel> list, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);

            #region 表頭
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("序號");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("分提單號");
            row.CreateCell(3).SetCellValue("派送單號");
            row.CreateCell(4).SetCellValue("稅金");
            row.CreateCell(5).SetCellValue("報關費");
            row.CreateCell(6).SetCellValue("到付款");
            row.CreateCell(7).SetCellValue("代收手續");
            row.CreateCell(8).SetCellValue("代收總金額");
            row.CreateCell(9).SetCellValue("稅金類別");
            row.CreateCell(10).SetCellValue("備註");
            row.CreateCell(11).SetCellValue("客戶名");
            row.CreateCell(12).SetCellValue("備註");
            row.CreateCell(13).SetCellValue("倉儲");
            row.CreateCell(14).SetCellValue("報關類別");
            row.CreateCell(15).SetCellValue("三聯稅單");
            row.CreateCell(16).SetCellValue("四聯稅單");
            row.CreateCell(17).SetCellValue("滯報費減免");
            row.CreateCell(18).SetCellValue("出倉日");
            row.CreateCell(19).SetCellValue("交派日");
            row.CreateCell(20).SetCellValue("報單收費方式");
            row.CreateCell(21).SetCellValue("稅金付款人");
            row.CreateCell(22).SetCellValue("客戶");
            int[] widthLarge = new[] { 22 };

            for (int i = 0; i < 23; i++)
            {
                //置中
                row.GetCell(i).CellStyle = cs_Center;

                sheet.SetColumnWidth(i, 5000);
                if (Array.IndexOf(widthLarge, i) > -1)
                {
                    //寬度
                    sheet.SetColumnWidth(i, 7000);
                }
                else
                {
                    //寬度
                    sheet.SetColumnWidth(i, 5000);
                }
            }


            #endregion
            int irow = 1;
            foreach (var item in list)
            {
                row = sheet.CreateRow(irow);
                //序號
                row.CreateCell(0).SetCellValue(irow);
                //倉儲
                row.CreateCell(1).SetCellValue(item.CollectibleDataType);
                //分提單號
                row.CreateCell(2).SetCellValue(item.BlNo);
                //派送單號
                if (item.JetfSerial.Split(',').Length > 0)
                {
                    row.CreateCell(3).SetCellValue(item.JetfSerial.Split(',')[0]);
                }
                //稅金
                row.CreateCell(4).SetCellValue(item.ReceiptAmount);
                //報關費
                row.CreateCell(5).SetCellValue(item.InvoiceAmount + item.InvoiceTax);
                //到付款
                if (item.CC.HasValue)
                {
                    row.CreateCell(6).SetCellValue(item.CC.Value);
                }
                //代收手續
                row.CreateCell(7).SetCellValue(item.TotalDeliveryAmount);
                //代收總金額
                row.CreateCell(8).SetCellValue(item.ReceiptAmount + item.InvoiceAmount + item.InvoiceTax + (item.CC.HasValue ? item.CC.Value : 0) + item.TotalDeliveryAmount);


                if (item.Total >= 49851 && !item.IsChangeToRemittance())
                {
                    //稅金類別
                    row.CreateCell(9).SetCellValue("D");
                    //備註
                    row.CreateCell(10).SetCellValue("收客匯款");
                }
                else
                {
                    //稅金類別
                    row.CreateCell(9).SetCellValue(item.TaxType);
                    //備註
                    row.CreateCell(10).SetCellValue(item.TaxTypeRemark);
                }

                //客戶名
                row.CreateCell(11).SetCellValue(item.Customer);
                //備註
                row.CreateCell(12).SetCellValue(item.Remark);
                //倉儲
                row.CreateCell(13).SetCellValue(item.DataType);
                //報關類別
                row.CreateCell(14).SetCellValue(item.ClearanceType);
                //X類稅金
                row.CreateCell(15).SetCellValue(item.XTax);
                //G類稅金
                row.CreateCell(16).SetCellValue(item.GTax);
                //滯報費減免
                row.CreateCell(17).SetCellValue(item.FeeReduction);
                //出倉日
                row.CreateCell(18).SetCellValue(item.SignOutDate?.ToString("yyyy/MM/dd"));
                //交派日
                row.CreateCell(19).SetCellValue(item.ActualDate?.ToString("yyyy/MM/dd"));
                //報單收費方式
                row.CreateCell(20).SetCellValue(item.ClearanceFeeType);
                //稅金付款人
                row.CreateCell(21).SetCellValue(item.TaxPayer);
                //客戶
                row.CreateCell(22).SetCellValue(item.CustName);
                irow++;
            }
        }

        /// <summary>
        /// Excel Style
        /// </summary>
        /// <param name="workbook"></param>
        void GetWorkbookStyle(IWorkbook workbook)
        {
            //藍色的Style
            fontB = workbook.CreateFont();
            fontB.Color = NPOI.SS.UserModel.IndexedColors.Blue.Index;

            font1 = (XSSFFont)workbook.CreateFont();
            //標題
            cs_Title = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //標題
            cs_Title_Left = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title_Left.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Title_Left.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Title.BorderTop = BorderStyle.Thin;
            //cs_Title.BorderBottom = BorderStyle.Thin;
            //cs_Title.BorderLeft = BorderStyle.Thin;
            //cs_Title.BorderRight = BorderStyle.Thin;
            //cs_Title.SetFont(font1);

            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center.BorderTop = BorderStyle.Thin;
            //cs_Center.BorderBottom = BorderStyle.Thin;
            //cs_Center.BorderLeft = BorderStyle.Thin;
            //cs_Center.BorderRight = BorderStyle.Thin;

            cs_Center_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center_Blue.BorderTop = BorderStyle.Thin;
            //cs_Center_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Center_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Center_Blue.BorderRight = BorderStyle.Thin;
            cs_Center_Blue.SetFont(fontB);

            format = (XSSFDataFormat)workbook.CreateDataFormat();
            cs_Int = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int.BorderTop = BorderStyle.Thin;
            //cs_Int.BorderBottom = BorderStyle.Thin;
            //cs_Int.BorderLeft = BorderStyle.Thin;
            //cs_Int.BorderRight = BorderStyle.Thin;
            cs_Int.DataFormat = format.GetFormat("#,##0");

            cs_Int_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int_Blue.BorderTop = BorderStyle.Thin;
            //cs_Int_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Int_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Int_Blue.BorderRight = BorderStyle.Thin;
            cs_Int_Blue.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue.SetFont(fontB);

            cs_Double = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Double.BorderTop = BorderStyle.Thin;
            //cs_Double.BorderBottom = BorderStyle.Thin;
            //cs_Double.BorderLeft = BorderStyle.Thin;
            //cs_Double.BorderRight = BorderStyle.Thin;
            cs_Double.DataFormat = format.GetFormat("#,##0.000");

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0.000%");
            cs_Percent2.SetFont(font1);


            dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            dateStyle.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm:ss");

            date2Style = (XSSFCellStyle)workbook.CreateCellStyle();
            date2Style.DataFormat = format.GetFormat("yyyy/mm/dd");

        }


    }
}
