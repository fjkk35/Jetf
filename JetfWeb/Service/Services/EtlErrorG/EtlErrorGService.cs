using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models.CptTradeVan;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.EtlErrorG
{
    public class EtlErrorGService : _BaseService
    {
        private readonly CptTradeVanService _cptTradeVanService;

        public EtlErrorGService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, CptTradeVanService cptTradeVanService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            this._cptTradeVanService = cptTradeVanService;
        }

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent, cs_Percent2, dateStyle, date2Style;



        /// <summary>
        /// 
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="custTypeId"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        public IWorkbook GetEtlErrorGWorkbook(string sDate, string eDate,bool isSearch)
        {
            IWorkbook workbook = new XSSFWorkbook();

            //更新GB321
            if(isSearch)
                UpdateGb321(sDate, eDate);

            //GB350-航班日期
            UpdateGb350(sDate, eDate);

            //明細
            DataTable dt_Details = EtlErrorGDetails(sDate, eDate);

            GetEtlErrorGSheet(workbook, dt_Details);
            return workbook;
        }

        void GetEtlErrorGSheet(IWorkbook workbook, DataTable dt)
        {
            var headCount = 0;
            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            ISheet sheet = workbook.CreateSheet("空快B6F錯單G報表");

            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(headCount++).SetCellValue("ACCS");
            row.CreateCell(headCount++).SetCellValue("日期差");
            row.CreateCell(headCount++).SetCellValue("航班");
            row.CreateCell(headCount++).SetCellValue("倉儲");
            row.CreateCell(headCount++).SetCellValue("客戶");
            row.CreateCell(headCount++).SetCellValue("報單號碼");
            row.CreateCell(headCount++).SetCellValue("主提單號碼");
            row.CreateCell(headCount++).SetCellValue("分提單號");
            row.CreateCell(headCount++).SetCellValue("預委任日期");
            row.CreateCell(headCount++).SetCellValue("統編/身分證字號");
            row.CreateCell(headCount++).SetCellValue("電話");
            row.CreateCell(headCount++).SetCellValue("申報金額");
            row.CreateCell(headCount++).SetCellValue("項次");
            row.CreateCell(headCount++).SetCellValue("貨物名稱");
            row.CreateCell(headCount++).SetCellValue("收件人名");
            row.CreateCell(headCount++).SetCellValue("錯單訊息");
            row.CreateCell(headCount++).SetCellValue("已委任 (Y/N=冒名)");
            row.CreateCell(headCount++).SetCellValue("原袋整袋 (收單 )");
            row.CreateCell(headCount++).SetCellValue("小號(收單+日期)");
            row.CreateCell(headCount++).SetCellValue("拆出小號進出倉日期");
            row.CreateCell(headCount++).SetCellValue("拆袋成功日期");
            row.CreateCell(headCount++).SetCellValue("已拆袋新建號碼");
            row.CreateCell(headCount++).SetCellValue("公司名義清出");
            row.CreateCell(headCount++).SetCellValue("現場 回報(備註");
            row.CreateCell(headCount++).SetCellValue("正確資料");
            row.CreateCell(headCount++).SetCellValue("滯報費");
            row.CreateCell(headCount++).SetCellValue("客服聯繫狀況");
            row.CreateCell(headCount++).SetCellValue("海關核准文號");

            //row.GetCell(0).CellStyle = cs_Center;

            for (int i = 0; i < 28; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);

                //倉儲
                var source = dt.Rows[i]["SOURCEFROM"].ToString().IndexOf("CE") > -1
                    ? "TACT" : dt.Rows[i]["SOURCEFROM"].ToString().IndexOf("CX") > -1
                    ? "FTZ" : "";

                if (DateTime.TryParseExact(dt.Rows[i]["DELIVERYDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    var today = DateTime.Now;
                    row.CreateCell(0).SetCellValue(date.ToString("yyyy/MM/dd"));//航班日期
                    row.CreateCell(1).SetCellValue((today.Date - date.Date).TotalDays);//日期差
                    row.CreateCell(8).SetCellValue(date.ToString("yyyyMMdd"));//預委任日期
                }

                row.CreateCell(2).SetCellValue(dt.Rows[i]["FLIGHTNUMBER"].ToString());//航班

                row.CreateCell(3).SetCellValue(source);//倉儲
                row.CreateCell(4).SetCellValue(dt.Rows[i]["CUST"].ToString());//客戶
                row.CreateCell(5).SetCellValue(dt.Rows[i]["BAG_NO"].ToString());//報單號碼
                row.CreateCell(6).SetCellValue(dt.Rows[i]["MAWB"].ToString());//主提單號碼
                row.CreateCell(7).SetCellValue(dt.Rows[i]["HAWB"].ToString());//分提單號
                row.CreateCell(9).SetCellValue(dt.Rows[i]["RECID"].ToString());//統編/身分證字號
                row.CreateCell(10).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());//電話

                if (double.TryParse(dt.Rows[i]["UNITPRICE"].ToString(), out var price))
                {
                    row.CreateCell(11).SetCellValue(price);//申報金額
                }

                row.CreateCell(12).SetCellValue(1);//項次
                row.CreateCell(13).SetCellValue(dt.Rows[i]["ITEMSMODIFY"].ToString());//貨物名稱
                row.CreateCell(14).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());//收件人名
                row.CreateCell(15).SetCellValue(dt.Rows[i]["REASON"].ToString());//錯單訊息
                row.CreateCell(16).SetCellValue(dt.Rows[i]["REPLY_NAME"].ToString());//已委任 (Y/N=冒名)
                row.CreateCell(17).SetCellValue(dt.Rows[i]["PRO_TYPE"].ToString());//原袋整袋 (收單 )
                if (DateTime.TryParseExact(dt.Rows[i]["PRO_DATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var proDate))
                {
                    row.CreateCell(18).SetCellValue($"收單{proDate.ToString("MM/dd")}");//小號(收單+日期)
                }
                row.CreateCell(19).SetCellValue(dt.Rows[i]["CLEARANCE_TYPE"].ToString());//拆出小號

                if (DateTime.TryParse(dt.Rows[i]["SCAN_UPLOAD_TIME2"].ToString(), out var dateTime))
                {
                    row.CreateCell(20).SetCellValue(dateTime.ToString("MM/dd"));//拆袋成功日期
                    //row.CreateCell(21).SetCellValue(dt.Rows[i]["SCAN_BAGNO"].ToString());//拆袋袋號
                }

                row.CreateCell(27).SetCellValue(dt.Rows[i]["CUSTOMS_APPROVAL_NUMBER"].ToString());//海關核准文號

            }
        }

        public DataTable EtlErrorGDetails(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_Select_EtlB6FErrorG_Report]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@SDataDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@EDataDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }

            //排序
            DataView dv = dt.DefaultView;
            dv.Sort = "DELIVERYDATE,MAWB,BAG_NO,HAWB";

            return dv.ToTable();
        }

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

            cs_Percent = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent.DataFormat = format.GetFormat("0.00%");
            cs_Percent.SetFont(font1);

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



        void UpdateGb321(string sDate, string eDate)
        {
            var data = GetEtlPlinkError(sDate, eDate).AsEnumerable()
                .Select(r => new
                {
                    Id = r.Field<int>("ROW_ID"),
                    MainNumber = r.Field<string>("MAWB"),
                    TrackingNo = r.Field<string>("HAWB")
                }).ToList();

            conn.Open();

            data.ForEach(r =>
            {
                var result = GetGb321(r.MainNumber, r.TrackingNo);
                var model = result.GridModel?.FirstOrDefault(x => x.ProType.Contains("連線收單建檔"));
                var clearanceModel = result.GridModel?.FirstOrDefault(x => x.ProType.Contains("通關方式"));

                var proDate = model == null || string.IsNullOrEmpty(model.ProDate) ? "" : model.ProDate;

                //通關方式
                var clearanceType = clearanceModel == null || string.IsNullOrEmpty(clearanceModel.ProType) ? "" : clearanceModel?.ProType.Replace("通關方式", "");

                var proType = string.IsNullOrEmpty(proDate) ? "" : "收單";

                if(!string.IsNullOrEmpty(proDate) || !string.IsNullOrEmpty(clearanceType))
                    //更新
                    UpdateEtlPlinkError(r.Id, proType, proDate, clearanceType);
            });

            conn.Close();

        }

        void UpdateGb350(string sDate, string eDate)
        {
            var data = GetEtlPlinkErrorMawb(sDate, eDate).AsEnumerable()
                .Select(r => new
                {
                    MainNumber = r.Field<string>("MAWB")
                }).ToList();

            conn.Open();

            data.ForEach(r =>
            {
                var result = GetGb350(r.MainNumber);
                var importDate = result.GridModel?.FirstOrDefault()?.IMPORT_DATE ?? "";

                if(!string.IsNullOrEmpty(importDate))
                    InsertEtlDeliveryDate(r.MainNumber, importDate);
            });

            conn.Close();

        }



        Gb321Model GetGb321(string mainNumber, string trackingNo)
        {
            var parameters = new Dictionary<string, string>
                    {
                        { "transType", "A" },
                        { "mawb", string.IsNullOrEmpty(mainNumber) ? "" :mainNumber },
                        { "hawb", trackingNo }
                    };

            return _cptTradeVanService.GetGb321(parameters);
        }


        Gb350Model GetGb350(string mainNumber)
        {
            var parameters = new Dictionary<string, string>
                    {
                        { "finalChoice", "A" },
                        { "choice", "A" },
                        { "tab4.mawb", string.IsNullOrEmpty(mainNumber) ? "" :mainNumber },
                        { "tab4.mode", "5" },
                        { "tab4.currentGridPage", "1" },
                        { "tab4.currentGridPageRows", "10" }
                    };

            return _cptTradeVanService.GetGb350(parameters);
        }


        /// <summary>
        /// 取得空快錯單
        /// </summary>
        /// <returns></returns>
        DataTable GetEtlPlinkError(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            string sql = @"
                            select min(ROW_ID) ROW_ID,MAWB,HAWB from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR]
                            where CREATE_TIME between @SDataDate and @EDataDate and REASON in ('B6F','A03') and (PRO_TYPE is null or CLEARANCE_TYPE is null) 
                            group by MAWB,HAWB
                        ";

            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@SDataDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@EDataDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            return dt;
        }

        DataTable GetEtlPlinkErrorMawb(string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            string sql = @"
                            select distinct MAWB from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR]
                            where CREATE_TIME between @SDataDate and @EDataDate
                            and not exists (
                                select 1 from [jetf].[dbo].[EtlDeliveryDate]
                                where Mawb = ETL_PLINK_ERROR.MAWB
                            )
                        ";

            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@SDataDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@EDataDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 更新空快錯單
        /// </summary>
        /// <param name="id"></param>
        /// <param name="proTime"></param>
        void UpdateEtlPlinkError(int id, string proType, string proDate, string clearanceType)
        {
            using (SqlCommand cmd = new SqlCommand("update [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] set PRO_TYPE=@PRO_TYPE,PRO_DATE=@PRO_DATE,CLEARANCE_TYPE=@CLEARANCE_TYPE where ROW_ID=@ROW_ID", conn))
            {
                cmd.Parameters.Add("@ROW_ID", SqlDbType.NVarChar).Value = id;
                cmd.Parameters.Add("@PRO_TYPE", SqlDbType.NVarChar).Value = proType;
                cmd.Parameters.Add("@PRO_DATE", SqlDbType.NVarChar).Value = proDate;
                cmd.Parameters.Add("@CLEARANCE_TYPE", SqlDbType.NVarChar).Value = clearanceType;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 新增空快錯單航班日期
        /// </summary>
        /// <param name="id"></param>
        /// <param name="proTime"></param>
        void InsertEtlDeliveryDate (string mawb, string deliveryDate)
        {
            using (SqlCommand cmd = new SqlCommand("insert [jetf].[dbo].[EtlDeliveryDate](Mawb,DeliveryDate) values(@Mawb,@DeliveryDate)", conn))
            {
                cmd.Parameters.Add("@Mawb", SqlDbType.NVarChar).Value = mawb;
                cmd.Parameters.Add("@DeliveryDate", SqlDbType.NVarChar).Value = deliveryDate;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
