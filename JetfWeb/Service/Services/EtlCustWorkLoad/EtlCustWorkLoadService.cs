using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.Models;
using Service.Models.EtlCustWorkLoad;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.EtlCustWorkLoad
{
    public class EtlCustWorkLoadService
    {
        private readonly CustomerService _customerService;

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent, cs_Percent2, dateStyle, date2Style;

        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public EtlCustWorkLoadService(CustomerService customerService)
        {
            _customerService = customerService;
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 空快客戶作業量報表-Excel-Workbook
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <returns></returns>
        public IWorkbook GetCustWorkLoadReportWorkbook(string custId, string custTypeId, string sDate, string eDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //明細
            DataTable dt_Details = CustWorkLoadDetails(custId, sDate, eDate).dt;
            string custName = "";
            //取得客戶名稱
            if (dt_Details.Rows.Count > 0)
            {
                custName = _customerService.GetCustomerName("空運", dt_Details.Rows[0]["DESPATCH_NAME"].ToString());
            }
            //班次到達時間
            DataTable dt_Arrive = GetArriveList(custId, sDate, eDate);

            //空快客戶作業量報表
            if (custTypeId == "1")
            {
                GetCustWorkLoadReportSheet(workbook, dt_Details, dt_Arrive, custName, "空快客戶作業量報表", sDate, eDate);
            }
            else if (custTypeId == "2")
            {
                GetCustWorkLoadReport2Sheet(workbook, dt_Details, dt_Arrive, custName, "空快客戶作業量報表", sDate, eDate);
            }

            //空快客戶作業量報表-袋號明細
            GetCustWorkLoadDetailsSheet(workbook, dt_Details, custName, "袋號明細", sDate, eDate);
            return workbook;
        }

        /// <summary>
        /// 空快客戶作業量報表(博豐)-Excel-Workbook-頁籤-空快客戶作業量報表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetCustWorkLoadReportSheet(IWorkbook workbook, DataTable dt_Details, DataTable dt_Arrive, string custName, string sheetName, string sDate, string eDate)
        {
            DataRow[] dr;
            string error_bl_no;
            int rowCount, colCount, total_bl_no, total_in_bl_no, total_out_bl_no, total_c3_bl_no, total_no_bl_no, total_a03_bl_no, total_b6f_bl_no, total_piece;
            double total_gw;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 10));
            row.CreateCell(0).SetCellValue($"{sDate} 13:00-{eDate}12:59{sheetName}");
            row.GetCell(0).CellStyle = cs_Title_Left;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("Fl");
            row.CreateCell(1).SetCellValue("Lm");//派件公司
            row.CreateCell(2).SetCellValue("CC Agent");//客戶
            row.CreateCell(3).SetCellValue("EC/SC");
            row.CreateCell(4).SetCellValue("P/T");
            row.CreateCell(5).SetCellValue("Pu date");
            row.CreateCell(6).SetCellValue("Mawbs");//主提單號
            row.CreateCell(7).SetCellValue("Service code");
            row.CreateCell(8).SetCellValue("原單袋數");//原單袋數
            row.CreateCell(9).SetCellValue("原單件數");//原單件數
            row.CreateCell(10).SetCellValue("GW");//GW
            row.CreateCell(11).SetCellValue("Ctns");
            row.CreateCell(12).SetCellValue("Pcs");
            row.CreateCell(13).SetCellValue("GW");
            row.CreateCell(14).SetCellValue("VW(RCL)");
            row.CreateCell(15).SetCellValue("Chargeble Weight");
            row.CreateCell(16).SetCellValue("Ori.");
            row.CreateCell(17).SetCellValue("Transit Airport");
            row.CreateCell(18).SetCellValue("Dest.");
            row.CreateCell(19).SetCellValue("ETA");
            row.CreateCell(20).SetCellValue("ATA");
            row.CreateCell(21).SetCellValue("Batch A");//航班代號
            row.CreateCell(22).SetCellValue("CTNS");//原單袋數
            row.CreateCell(23).SetCellValue("ATA");//到達時間
            row.CreateCell(24).SetCellValue("Batch B");
            row.CreateCell(25).SetCellValue("CTNS");
            row.CreateCell(26).SetCellValue("ATA");
            row.CreateCell(27).SetCellValue("Batch C");
            row.CreateCell(28).SetCellValue("CTNS");
            row.CreateCell(29).SetCellValue("ATA");
            row.CreateCell(30).SetCellValue("Batch D");
            row.CreateCell(31).SetCellValue("CTNS");
            row.CreateCell(32).SetCellValue("ATA");
            row.CreateCell(33).SetCellValue("Batch E");
            row.CreateCell(34).SetCellValue("CTNS");
            row.CreateCell(35).SetCellValue("ATA");
            row.CreateCell(36).SetCellValue("Batch F");
            row.CreateCell(37).SetCellValue("CTNS");
            row.CreateCell(38).SetCellValue("ATA");
            row.CreateCell(39).SetCellValue("Batch G");
            row.CreateCell(40).SetCellValue("CTNS");
            row.CreateCell(41).SetCellValue("ATA");
            row.CreateCell(42).SetCellValue("Batch B");
            row.CreateCell(43).SetCellValue("CTNS");
            row.CreateCell(44).SetCellValue("ATA");
            row.CreateCell(45).SetCellValue("Batch C");
            row.CreateCell(46).SetCellValue("CTNS");
            row.CreateCell(47).SetCellValue("ATA");
            row.CreateCell(48).SetCellValue("Batch D");
            row.CreateCell(49).SetCellValue("CTNS");
            row.CreateCell(50).SetCellValue("ATA");
            row.CreateCell(51).SetCellValue("Batch A");
            row.CreateCell(52).SetCellValue("CTNS");
            row.CreateCell(53).SetCellValue("ATA");
            row.CreateCell(54).SetCellValue("Batch B");
            row.CreateCell(55).SetCellValue("CTNS");
            row.CreateCell(56).SetCellValue("ATA");
            row.CreateCell(57).SetCellValue("Batch C");
            row.CreateCell(58).SetCellValue("CTNS");
            row.CreateCell(59).SetCellValue("ATA");
            row.CreateCell(60).SetCellValue("Batch D");
            row.CreateCell(61).SetCellValue("CTNS");
            row.CreateCell(62).SetCellValue("ATA");
            row.CreateCell(63).SetCellValue("1st release date");
            row.CreateCell(64).SetCellValue("1st releas");
            row.CreateCell(65).SetCellValue("2nd release date");
            row.CreateCell(66).SetCellValue("2nd releas");
            row.CreateCell(67).SetCellValue("3rd release date");
            row.CreateCell(68).SetCellValue("3rd release ctns");
            row.CreateCell(69).SetCellValue("4th release date");
            row.CreateCell(70).SetCellValue("4th releas");
            row.CreateCell(71).SetCellValue("5th release date");
            row.CreateCell(72).SetCellValue("5th relea");
            row.CreateCell(73).SetCellValue("6th release date");
            row.CreateCell(74).SetCellValue("6th releas");
            row.CreateCell(75).SetCellValue("7th release date");
            row.CreateCell(76).SetCellValue("7th release");
            row.CreateCell(77).SetCellValue("8th release date");
            row.CreateCell(78).SetCellValue("8th release");
            row.CreateCell(79).SetCellValue("9th release date");
            row.CreateCell(80).SetCellValue("9th release ctns");
            row.CreateCell(81).SetCellValue("10th release date");
            row.CreateCell(82).SetCellValue("10th release ctns");
            row.CreateCell(83).SetCellValue("10th release date");
            row.CreateCell(84).SetCellValue("10th release ctns");
            row.CreateCell(85).SetCellValue("11th release date");
            row.CreateCell(86).SetCellValue("11th release ctns");
            row.CreateCell(87).SetCellValue("1st DLV");
            row.CreateCell(88).SetCellValue("1st CTNS");
            row.CreateCell(89).SetCellValue("2nd DLV");
            row.CreateCell(90).SetCellValue("2nd CTNS");
            row.CreateCell(91).SetCellValue("3rd DLV");
            row.CreateCell(92).SetCellValue("3rd CTNS");
            row.CreateCell(93).SetCellValue("4th DLV");
            row.CreateCell(94).SetCellValue("4th CTNS");
            row.CreateCell(95).SetCellValue("5th DLV");
            row.CreateCell(96).SetCellValue("5th CTNS");
            row.CreateCell(97).SetCellValue("6th DLV");
            row.CreateCell(98).SetCellValue("6th CTNS");
            row.CreateCell(99).SetCellValue("7th DLV");
            row.CreateCell(100).SetCellValue("7th CTNS");
            row.CreateCell(101).SetCellValue("8th DLV");
            row.CreateCell(102).SetCellValue("8th CTNS");
            row.CreateCell(103).SetCellValue("9th DLV");
            row.CreateCell(104).SetCellValue("9th CTNS");
            row.CreateCell(105).SetCellValue("10th DLV");
            row.CreateCell(106).SetCellValue("10th CTNS");
            row.CreateCell(107).SetCellValue("10th DLV");
            row.CreateCell(108).SetCellValue("10th CTNS");
            row.CreateCell(109).SetCellValue("10th DLV");
            row.CreateCell(110).SetCellValue("10th CTNS");
            row.CreateCell(111).SetCellValue("C3");//C3袋數
            row.CreateCell(112).SetCellValue("未見");//入 - 出袋數
            row.CreateCell(113).SetCellValue("B6D/A03");//A03
            row.CreateCell(114).SetCellValue("B6E");//B6F
            row.CreateCell(115).SetCellValue("異常");
            row.CreateCell(116).SetCellValue("總件數");//原單袋數
            row.CreateCell(117).SetCellValue("落地"); //入倉袋數
            row.CreateCell(118).SetCellValue("清出");// 出倉袋數
            row.CreateCell(119).SetCellValue("交倉");
            row.CreateCell(120).SetCellValue("問題");//C3+未見

            //row.GetCell(0).CellStyle = cs_Center;
            //row.GetCell(1).CellStyle = cs_Center;
            //row.GetCell(2).CellStyle = cs_Center;
            //row.GetCell(3).CellStyle = cs_Center;
            //row.GetCell(4).CellStyle = cs_Center;
            //row.GetCell(5).CellStyle = cs_Center;
            //row.GetCell(6).CellStyle = cs_Center;
            //row.GetCell(7).CellStyle = cs_Center;
            //row.GetCell(8).CellStyle = cs_Center;
            //row.GetCell(9).CellStyle = cs_Center;
            //row.GetCell(10).CellStyle = cs_Center;

            sheet.SetColumnWidth(23, 5000);
            sheet.SetColumnWidth(26, 5000);
            sheet.SetColumnWidth(29, 5000);
            sheet.SetColumnWidth(32, 5000);
            sheet.SetColumnWidth(35, 5000);
            sheet.SetColumnWidth(38, 5000);
            sheet.SetColumnWidth(41, 5000);
            sheet.SetColumnWidth(44, 5000);
            sheet.SetColumnWidth(47, 5000);
            sheet.SetColumnWidth(50, 5000);
            sheet.SetColumnWidth(53, 5000);
            sheet.SetColumnWidth(56, 5000);
            sheet.SetColumnWidth(59, 5000);
            sheet.SetColumnWidth(62, 5000);

            sheet.SetColumnWidth(87, 5000);
            sheet.SetColumnWidth(89, 5000);
            sheet.SetColumnWidth(91, 5000);
            sheet.SetColumnWidth(93, 5000);
            sheet.SetColumnWidth(95, 5000);
            sheet.SetColumnWidth(97, 5000);
            sheet.SetColumnWidth(99, 5000);
            sheet.SetColumnWidth(101, 5000);
            sheet.SetColumnWidth(103, 5000);
            sheet.SetColumnWidth(105, 5000);
            sheet.SetColumnWidth(107, 5000);
            sheet.SetColumnWidth(109, 5000);

            sheet.SetColumnWidth(63, 5000);
            sheet.SetColumnWidth(65, 5000);
            sheet.SetColumnWidth(67, 5000);
            sheet.SetColumnWidth(69, 5000);
            sheet.SetColumnWidth(71, 5000);
            sheet.SetColumnWidth(73, 5000);
            sheet.SetColumnWidth(75, 5000);
            sheet.SetColumnWidth(77, 5000);
            sheet.SetColumnWidth(79, 5000);
            sheet.SetColumnWidth(81, 5000);

            //派件公司+主號
            var dt_Group = from t in dt_Details.AsEnumerable()
                           group t by new { trans_no = t.Field<int?>("TRANS_NO"), trans_name = t.Field<string>("TRANS_NAME"), mainnumber = t.Field<string>("MAINNUMBER") } into g
                           orderby g.Key.mainnumber, g.Key.trans_name
                           select new
                           {
                               trans_no = g.Key.trans_no,
                               trans_name = g.Key.trans_name,
                               mainnumber = g.Key.mainnumber,
                           };

            rowCount = 3;
            foreach (var item in dt_Group)
            {
                //原單袋數
                var dt_Bl_No = (from t in dt_Details.AsEnumerable()
                                where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString()
                                select t["BL_NO"]).Distinct().ToList();
                total_bl_no = dt_Bl_No.Count;

                //入倉袋數
                var dt_In_Bl_No = (from t in dt_Details.AsEnumerable()
                                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["I_SIGN_IN_TIME"].ToString() != ""
                                   select t["BL_NO"]).Distinct().ToList();
                total_in_bl_no = dt_In_Bl_No.Count;

                //出倉袋數
                var dt_Out_Bl_No = (from t in dt_Details.AsEnumerable()
                                    where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["I_SIGN_OUT_TIME"].ToString() != ""
                                    select t["BL_NO"]).Distinct().ToList();
                total_out_bl_no = dt_Out_Bl_No.Count;

                //原單件數
                total_piece = 0;
                int.TryParse(dt_Details.Compute("SUM(I_CARGO_PIECE)", $"MAINNUMBER='{item.mainnumber.ToString()}' and TRANS_NAME='{item.trans_name.ToString()}'").ToString(), out total_piece);
                //GW
                total_gw = 0;
                double.TryParse(dt_Details.Compute("SUM(I_CARGO_WEIGHT)", $"MAINNUMBER='{item.mainnumber.ToString()}'  and TRANS_NAME='{item.trans_name.ToString()}'").ToString(), out total_gw);
                //航班代號
                //flightnumber = "";
                //dr = dt_Details.Select($"MAINNUMBER='{item.mainnumber.ToString()}' and TRANS_NAME='{item.trans_name.ToString()}' and FLIGHTNUMBER > '' ");
                //if (dr.Length > 0)
                //{
                //    flightnumber = dr[0]["FLIGHTNUMBER"].ToString();
                //}

                //A03袋號
                var dt_A03_Bl_No = (from t in dt_Details.AsEnumerable()
                                    where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["REMARK"].ToString() == "A03"
                                    select t["FORMAT_BL_NO"]).Distinct().ToList();

                //B6F袋號
                var dt_B6F_Bl_No = (from t in dt_Details.AsEnumerable()
                                    where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["REMARK"].ToString() == "B6F"
                                    select t["BL_NO"]).Distinct().ToList();

                //異常
                error_bl_no = "";
                ////C3袋號
                //var dt_C3_Bl_No = (from t in dt_Details.AsEnumerable()
                //                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["I_SIGN_IN_TIME"].ToString() != "" && t["I_SIGN_OUT_TIME"].ToString() == ""
                //                   select t["BL_NO"]).Distinct().ToList();
                //C3袋號
                var dt_C3_Bl_No = (from t in dt_Details.AsEnumerable()
                                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["REMARK"].ToString() == "C3"
                                   select t["BL_NO"]).Distinct().ToList();
                if (dt_C3_Bl_No.Count > 0)
                {
                    error_bl_no += "C3：";
                    error_bl_no += string.Join(",", dt_C3_Bl_No);
                }
                //未見袋號
                //var dt_No_Bl_No = (from t in dt_Details.AsEnumerable()
                //                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["I_SIGN_IN_TIME"].ToString() == "" && t["I_SIGN_OUT_TIME"].ToString() == ""
                //                   select t["BL_NO"]).Distinct().ToList();
                //未見袋號
                var dt_No_Bl_No = (from t in dt_Details.AsEnumerable()
                                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["REMARK"].ToString() == "未見"
                                   select t["BL_NO"]).Distinct().ToList();
                if (dt_No_Bl_No.Count > 0)
                {
                    error_bl_no += " 未見：";
                    error_bl_no += string.Join(",", dt_No_Bl_No);
                }

                //A03代號
                if (dt_A03_Bl_No.Count > 0)
                {
                    error_bl_no += " A03：";
                    error_bl_no += string.Join(",", dt_A03_Bl_No);
                }

                //B6F代號
                if (dt_B6F_Bl_No.Count > 0)
                {
                    error_bl_no += " B6F：";
                    error_bl_no += string.Join(",", dt_B6F_Bl_No);
                }

                //C3袋數(已入倉 未出倉)
                total_c3_bl_no = dt_C3_Bl_No.Count;
                //未見(未入倉)
                total_no_bl_no = dt_No_Bl_No.Count;
                //A03
                total_a03_bl_no = dt_A03_Bl_No.Count;
                //B6F
                total_b6f_bl_no = dt_B6F_Bl_No.Count;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(1).SetCellValue(item.trans_name.ToString());//派件公司
                row.CreateCell(2).SetCellValue(custName);//客戶
                row.CreateCell(6).SetCellValue(item.mainnumber.ToString());//主提單號
                row.CreateCell(8).SetCellValue(total_bl_no);//原單袋數
                row.CreateCell(9).SetCellValue(total_piece);//原單件數
                row.CreateCell(10).SetCellValue(Math.Ceiling(total_gw));//GW
                //row.CreateCell(21).SetCellValue(flightnumber);//航班代號
                //row.CreateCell(22).SetCellValue(total_bl_no);//原單袋數
                row.CreateCell(111).SetCellValue(total_c3_bl_no);//C3袋數(已入倉 未出倉)
                row.CreateCell(112).SetCellValue(total_no_bl_no);//未見袋數(未入倉)
                row.CreateCell(113).SetCellValue(total_a03_bl_no);//A03
                row.CreateCell(114).SetCellValue(total_b6f_bl_no);//B6F
                row.CreateCell(115).SetCellValue(error_bl_no);//異常
                row.CreateCell(116).SetCellValue(total_bl_no);//原單袋數
                row.CreateCell(117).SetCellValue(total_in_bl_no); //落地(入倉袋數)
                row.CreateCell(118).SetCellValue(total_out_bl_no);//清出(出倉袋數)

                //班次到達時間
                DataRow[] dr_Arrive = dt_Arrive.Select($"TRANS_NO='{item.trans_no.ToString()}' and MAINNUMBER='{item.mainnumber.ToString() }'", "UPDATE_TIME desc");
                if (dr_Arrive.Length > 0)
                {
                    row.CreateCell(16).SetCellValue(dr_Arrive[0]["ORI"].ToString());
                    row.CreateCell(17).SetCellValue(dr_Arrive[0]["TRANSIT_AIRPORT"].ToString());
                    row.CreateCell(18).SetCellValue(dr_Arrive[0]["DEST"].ToString());
                    //班次到達時間
                    row.CreateCell(21).SetCellValue(dr_Arrive[0]["FLIGHTNUMBER"].ToString());//航班代號
                    row.CreateCell(22).SetCellValue(dr_Arrive[0]["FLIGHT_COUNT"].ToString()); //袋數
                    row.CreateCell(23).SetCellValue(dr_Arrive[0]["ARRIVE_DATE1"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["ARRIVE_DATE1"]).ToString("yyyy/M/d HH:mm") : ""); //到達時間1
                    row.CreateCell(26).SetCellValue(dr_Arrive[0]["ARRIVE_DATE2"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["ARRIVE_DATE2"]).ToString("yyyy/M/d HH:mm") : ""); //到達時間2
                    row.CreateCell(29).SetCellValue(dr_Arrive[0]["ARRIVE_DATE3"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["ARRIVE_DATE3"]).ToString("yyyy/M/d HH:mm") : ""); //到達時間3
                    row.CreateCell(32).SetCellValue(dr_Arrive[0]["ARRIVE_DATE4"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["ARRIVE_DATE4"]).ToString("yyyy/M/d HH:mm") : ""); //到達時間4
                    row.CreateCell(35).SetCellValue(dr_Arrive[0]["ARRIVE_DATE5"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["ARRIVE_DATE5"]).ToString("yyyy/M/d HH:mm") : ""); //到達時間5
                    //交倉時間
                    row.CreateCell(87).SetCellValue(dr_Arrive[0]["TRANS_DATE1"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE1"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(88).SetCellValue(dr_Arrive[0]["TRANS_COUNT1"].ToString()); //袋數
                    row.CreateCell(89).SetCellValue(dr_Arrive[0]["TRANS_DATE2"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE2"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(90).SetCellValue(dr_Arrive[0]["TRANS_COUNT2"].ToString()); //袋數
                    row.CreateCell(91).SetCellValue(dr_Arrive[0]["TRANS_DATE3"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE3"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(92).SetCellValue(dr_Arrive[0]["TRANS_COUNT3"].ToString()); //袋數
                    row.CreateCell(93).SetCellValue(dr_Arrive[0]["TRANS_DATE4"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE4"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(94).SetCellValue(dr_Arrive[0]["TRANS_COUNT4"].ToString()); //袋數
                    row.CreateCell(95).SetCellValue(dr_Arrive[0]["TRANS_DATE5"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE5"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(96).SetCellValue(dr_Arrive[0]["TRANS_COUNT5"].ToString()); //袋數
                    row.CreateCell(97).SetCellValue(dr_Arrive[0]["TRANS_DATE6"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE6"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(98).SetCellValue(dr_Arrive[0]["TRANS_COUNT6"].ToString()); //袋數
                    row.CreateCell(99).SetCellValue(dr_Arrive[0]["TRANS_DATE7"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE7"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(100).SetCellValue(dr_Arrive[0]["TRANS_COUNT7"].ToString()); //袋數
                    row.CreateCell(101).SetCellValue(dr_Arrive[0]["TRANS_DATE8"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE8"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(102).SetCellValue(dr_Arrive[0]["TRANS_COUNT8"].ToString()); //袋數
                    row.CreateCell(103).SetCellValue(dr_Arrive[0]["TRANS_DATE9"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE9"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(104).SetCellValue(dr_Arrive[0]["TRANS_COUNT9"].ToString()); //袋數
                    row.CreateCell(105).SetCellValue(dr_Arrive[0]["TRANS_DATE10"].ToString() != "" ? Convert.ToDateTime(dr_Arrive[0]["TRANS_DATE10"]).ToString("yyyy/M/d HH:mm") : ""); //派件公司送達時間
                    row.CreateCell(106).SetCellValue(dr_Arrive[0]["TRANS_COUNT10"].ToString()); //袋數

                }

                //出倉日期
                var dt_Sign_Out_Date = from t in dt_Details.AsEnumerable()
                                       where t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["I_SIGN_OUT_TIME"].ToString() != ""
                                       group t by new { trans_name = t.Field<string>("TRANS_NAME"), mainnumber = t.Field<string>("MAINNUMBER"), sign_out_date = t.Field<string>("I_SIGN_OUT_DATE") } into g
                                       orderby g.Min(t => t.Field<DateTime>("I_SIGN_OUT_TIME"))
                                       select new
                                       {
                                           sign_out_date = g.Key.sign_out_date,
                                           min_sign_out_time = g.Min(t => t.Field<DateTime>("I_SIGN_OUT_TIME")),
                                           trans_name = g.Key.trans_name,
                                           mainnumber = g.Key.mainnumber,
                                           total_bl_no = g.GroupBy(t => t.Field<string>("BL_NO")).Distinct().Count(),
                                       };

                colCount = 0;
                foreach (var item2 in dt_Sign_Out_Date)
                {
                    if (63 + colCount <= 81)
                    {
                        row.CreateCell(63 + colCount).SetCellValue(item2.min_sign_out_time.ToString("yyyy/M/d HH:mm")); //出倉時間
                        row.CreateCell(64 + colCount).SetCellValue(item2.total_bl_no); //出倉袋數
                    }
                    colCount = colCount + 2;
                }
                row.CreateCell(120).SetCellValue(total_c3_bl_no + total_no_bl_no + total_a03_bl_no + total_b6f_bl_no);//C3+未見+A03+B6F
                rowCount++;
            }
        }

        /// <summary>
        /// 空快客戶作業量報表-Excel-Workbook-頁籤-袋號明細
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetCustWorkLoadDetailsSheet(IWorkbook workbook, DataTable dt_Details, string custName, string sheetName, string sDate, string eDate)
        {
            string type;
            int rowCount;
            DateTime sign_in_time, sign_out_time;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);
            ISheet sheet = workbook.CreateSheet(sheetName);

            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("客戶");
            row.CreateCell(1).SetCellValue("主提單號");
            row.CreateCell(2).SetCellValue("袋號");
            row.CreateCell(3).SetCellValue("派件公司");
            row.CreateCell(4).SetCellValue("通關方式");
            row.CreateCell(5).SetCellValue("進倉時間");
            row.CreateCell(6).SetCellValue("出倉時間");
            row.CreateCell(7).SetCellValue("異常類別");
            row.CreateCell(8).SetCellValue("渠道代码");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 3500);
            sheet.SetColumnWidth(1, 3500);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 3500);
            sheet.SetColumnWidth(4, 3500);
            sheet.SetColumnWidth(3, 4500);
            sheet.SetColumnWidth(4, 4500);
            sheet.SetColumnWidth(5, 5500);
            sheet.SetColumnWidth(6, 5500);
            sheet.SetColumnWidth(7, 5500);
            sheet.SetColumnWidth(8, 5500);

            //派件公司+主號
            var dt_Group = from t in dt_Details.AsEnumerable()
                           group t by new { 
                               trans_name = t.Field<string>("TRANS_NAME"),
                               mainnumber = t.Field<string>("MAINNUMBER"),
                               bl_no = t.Field<string>("BL_NO"),
                               sign_in_time = t.Field<DateTime?>("I_SIGN_IN_TIME"),
                               sign_out_time = t.Field<DateTime?>("I_SIGN_OUT_TIME"),
                               remark = t.Field<string>("REMARK"),
                               line_code = t.Field<string>("LINE_CODE")
                           } into g
                           select new
                           {
                               trans_name = g.Key.trans_name,
                               mainnumber = g.Key.mainnumber,
                               bl_no = g.Key.bl_no,
                               line_code = g.Key.line_code,
                               sign_in_time = g.Key.sign_in_time,
                               sign_out_time = g.Key.sign_out_time,
                               remark = g.Key.remark
                           };

            rowCount = 1;
            foreach (var item in dt_Group)
            {
                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(custName);//客戶
                row.CreateCell(1).SetCellValue(item.mainnumber);//主提單號
                row.CreateCell(2).SetCellValue(item.bl_no);//袋號
                row.CreateCell(3).SetCellValue(item.trans_name);//派件公司
                row.CreateCell(4).SetCellValue("");//通關方式

                type = item.remark;
                //sign_in_time = DateTime.MinValue;
                //sign_out_time = DateTime.MinValue;
                if (DateTime.TryParse(item.sign_in_time.ToString(), out sign_in_time))
                {
                    row.CreateCell(5).SetCellValue(sign_in_time.ToString("yyyy/M/d HH:mm"));//進倉時間
                }

                if (DateTime.TryParse(item.sign_out_time.ToString(), out sign_out_time))
                {
                    row.CreateCell(6).SetCellValue(sign_out_time.ToString("yyyy/M/d HH:mm"));//出倉時間
                }

                //if (sign_in_time == DateTime.MinValue)
                //{
                //    type = "未見";
                //}
                //else if (sign_out_time == DateTime.MinValue)
                //{
                //    type = "未出";
                //}
                row.CreateCell(7).SetCellValue(type);//異常類別

                row.CreateCell(8).SetCellValue(item.line_code);//渠道代码
                rowCount++;
            }
        }

        /// <summary>
        /// 空快客戶作業量報表(蝦皮)-Excel-Workbook-頁籤-空快客戶作業量報表
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetCustWorkLoadReport2Sheet(IWorkbook workbook, DataTable dt_Details, DataTable dt_Arrive, string custName, string sheetName, string sDate, string eDate)
        {
            DateTime startDate = Convert.ToDateTime($"{sDate} 13:00");
            DateTime EndDate = Convert.ToDateTime($"{eDate} 12:59");
            string error_bl_no;
            int rowCount, colCount, total_bl_no, total_in_bl_no, total_out_bl_no, total_c3_bl_no, total_no_bl_no, total_a03_bl_no, total_b6f_bl_no, total_piece;
            double total_gw;

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //合併儲存格
            IRow row = sheet.CreateRow(0);
            sheet.AddMergedRegion(new CellRangeAddress(0, 1, 0, 10));
            row.CreateCell(0).SetCellValue($"{startDate.ToString("yyyy-MM-dd HH:mm")}-{EndDate.ToString("yyyy-MM-dd HH:mm")}{sheetName}");
            row.GetCell(0).CellStyle = cs_Title_Left;
            //表頭 
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("头程");
            row.CreateCell(1).SetCellValue("尾程");
            row.CreateCell(2).SetCellValue("清关行");
            row.CreateCell(3).SetCellValue("提单号");
            row.CreateCell(4).SetCellValue("渠道代码");
            row.CreateCell(5).SetCellValue("渠道箱数");
            row.CreateCell(6).SetCellValue("提单箱数");
            row.CreateCell(7).SetCellValue("提单重量");
            row.CreateCell(8).SetCellValue("第一批航班号");
            row.CreateCell(9).SetCellValue("箱数");
            row.CreateCell(10).SetCellValue("航班抵达时间");
            row.CreateCell(11).SetCellValue("第二批航班号");
            row.CreateCell(12).SetCellValue("箱数");
            row.CreateCell(13).SetCellValue("航班抵达时间");
            row.CreateCell(14).SetCellValue("Batch C");
            row.CreateCell(15).SetCellValue("CTNS");
            row.CreateCell(16).SetCellValue("ATA");
            row.CreateCell(17).SetCellValue("Batch D");
            row.CreateCell(18).SetCellValue("CTNS");
            row.CreateCell(19).SetCellValue("ATA");

            row.CreateCell(20).SetCellValue("第一批清关完成时间");
            row.CreateCell(21).SetCellValue("箱数");
            row.CreateCell(22).SetCellValue("第二批清关完成时间");
            row.CreateCell(23).SetCellValue("箱数");
            row.CreateCell(24).SetCellValue("第三批清关完成时间");
            row.CreateCell(25).SetCellValue("箱数");
            row.CreateCell(26).SetCellValue("第四批清关完成时间");
            row.CreateCell(27).SetCellValue("箱数");
            row.CreateCell(28).SetCellValue("第五批清关完成时间");
            row.CreateCell(29).SetCellValue("箱数");
            row.CreateCell(30).SetCellValue("第六批清关完成时间");
            row.CreateCell(31).SetCellValue("箱数");
            row.CreateCell(32).SetCellValue("第七批清关完成时间");
            row.CreateCell(33).SetCellValue("箱数");
            row.CreateCell(34).SetCellValue("第八批清关完成时间");
            row.CreateCell(35).SetCellValue("箱数");
            row.CreateCell(36).SetCellValue("第九批清关完成时间");
            row.CreateCell(37).SetCellValue("箱数");
            row.CreateCell(38).SetCellValue("第十批清关完成时间");
            row.CreateCell(39).SetCellValue("箱数");
            row.CreateCell(40).SetCellValue("第十一批清关完成时间");
            row.CreateCell(41).SetCellValue("箱数");
            row.CreateCell(42).SetCellValue("第十二批清关完成时间");
            row.CreateCell(43).SetCellValue("箱数");

            row.CreateCell(44).SetCellValue("第一批开始装车时间");
            row.CreateCell(45).SetCellValue("箱数");
            row.CreateCell(46).SetCellValue("第二批开始装车时间");
            row.CreateCell(47).SetCellValue("箱数");
            row.CreateCell(48).SetCellValue("第三批开始装车时间");
            row.CreateCell(49).SetCellValue("箱数");
            row.CreateCell(50).SetCellValue("第四批开始装车时间");
            row.CreateCell(51).SetCellValue("箱数");
            row.CreateCell(52).SetCellValue("第五批开始装车时间");
            row.CreateCell(53).SetCellValue("箱数");
            row.CreateCell(54).SetCellValue("第六批开始装车时间");
            row.CreateCell(55).SetCellValue("箱数");
            row.CreateCell(56).SetCellValue("第七批开始装车时间");
            row.CreateCell(57).SetCellValue("箱数");
            row.CreateCell(58).SetCellValue("第八批开始装车时间");
            row.CreateCell(59).SetCellValue("箱数");
            row.CreateCell(60).SetCellValue("第九批开始装车时间");
            row.CreateCell(61).SetCellValue("箱数");
            row.CreateCell(62).SetCellValue("第十批开始装车时间");
            row.CreateCell(63).SetCellValue("箱数");

            row.CreateCell(64).SetCellValue("第一批抵达尾程时间");
            row.CreateCell(65).SetCellValue("箱数");
            row.CreateCell(66).SetCellValue("第二批抵达尾程时间");
            row.CreateCell(67).SetCellValue("箱数");
            row.CreateCell(68).SetCellValue("第三批抵达尾程时间");
            row.CreateCell(69).SetCellValue("箱数");
            row.CreateCell(70).SetCellValue("第四批抵达尾程时间");
            row.CreateCell(71).SetCellValue("箱数");
            row.CreateCell(72).SetCellValue("第五批抵达尾程时间");
            row.CreateCell(73).SetCellValue("箱数");
            row.CreateCell(74).SetCellValue("第六批抵达尾程时间");
            row.CreateCell(75).SetCellValue("箱数");
            row.CreateCell(76).SetCellValue("第七批抵达尾程时间");
            row.CreateCell(77).SetCellValue("箱数");
            row.CreateCell(78).SetCellValue("第八批抵达尾程时间");
            row.CreateCell(79).SetCellValue("箱数");
            row.CreateCell(80).SetCellValue("第九批抵达尾程时间");
            row.CreateCell(81).SetCellValue("箱数");
            row.CreateCell(82).SetCellValue("第十批抵达尾程时间");
            row.CreateCell(83).SetCellValue("箱数");

            row.CreateCell(84).SetCellValue("第一批派送完成时间");
            row.CreateCell(88).SetCellValue("箱数");
            row.CreateCell(86).SetCellValue("第二批派送完成时间");
            row.CreateCell(87).SetCellValue("箱数");
            row.CreateCell(88).SetCellValue("第三批派送完成时间");
            row.CreateCell(89).SetCellValue("箱数");
            row.CreateCell(90).SetCellValue("第四批派送完成时间");
            row.CreateCell(91).SetCellValue("箱数");
            row.CreateCell(92).SetCellValue("第五批派送完成时间");
            row.CreateCell(93).SetCellValue("箱数");
            row.CreateCell(94).SetCellValue("第六批派送完成时间");
            row.CreateCell(95).SetCellValue("箱数");
            row.CreateCell(96).SetCellValue("第七批派送完成时间");
            row.CreateCell(97).SetCellValue("箱数");
            row.CreateCell(98).SetCellValue("第八批派送完成时间");
            row.CreateCell(99).SetCellValue("箱数");
            row.CreateCell(100).SetCellValue("第九批派送完成时间");
            row.CreateCell(101).SetCellValue("箱数");
            row.CreateCell(102).SetCellValue("第十批派送完成时间");
            row.CreateCell(103).SetCellValue("箱数");
            row.CreateCell(104).SetCellValue("第十一批派送完成时间");
            row.CreateCell(105).SetCellValue("箱数");
            row.CreateCell(106).SetCellValue("第十二批派送完成时间");
            row.CreateCell(107).SetCellValue("箱数");

            row.CreateCell(108).SetCellValue("第一批交倉完成时间");
            row.CreateCell(109).SetCellValue("箱数");
            row.CreateCell(110).SetCellValue("第二批交倉完成时间");
            row.CreateCell(111).SetCellValue("箱数");
            row.CreateCell(112).SetCellValue("第三批交倉完成时间");
            row.CreateCell(113).SetCellValue("箱数");
            row.CreateCell(114).SetCellValue("第四批交倉完成时间");
            row.CreateCell(115).SetCellValue("箱数");
            row.CreateCell(116).SetCellValue("第五批交倉完成时间");
            row.CreateCell(117).SetCellValue("箱数");
            row.CreateCell(118).SetCellValue("第六批交倉完成时间");
            row.CreateCell(119).SetCellValue("箱数");
            row.CreateCell(120).SetCellValue("第七批交倉完成时间");
            row.CreateCell(121).SetCellValue("箱数");
            row.CreateCell(122).SetCellValue("第八批交倉完成时间");
            row.CreateCell(123).SetCellValue("箱数");
            row.CreateCell(124).SetCellValue("第九批交倉完成时间");
            row.CreateCell(125).SetCellValue("箱数");
            row.CreateCell(126).SetCellValue("第十批交倉完成时间");
            row.CreateCell(127).SetCellValue("箱数");
            row.CreateCell(128).SetCellValue("第十一批交倉完成时间");
            row.CreateCell(129).SetCellValue("箱数");
            row.CreateCell(130).SetCellValue("第十二批交倉完成时间");
            row.CreateCell(131).SetCellValue("箱数");

            sheet.SetColumnWidth(0, 4000);
            sheet.SetColumnWidth(1, 4000);
            sheet.SetColumnWidth(2, 4000);
            sheet.SetColumnWidth(3, 5000);
            for (int i = 4; i < 132; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            //派件公司+主號
            var dt_Group = (from t in dt_Details.AsEnumerable()
                            group t by new { trans_no = t.Field<int?>("TRANS_NO"), trans_name = t.Field<string>("TRANS_NAME"), mainnumber = t.Field<string>("MAINNUMBER"), line_code = t.Field<string>("LINE_CODE"), bl_no = t.Field<string>("BL_NO") } into g
                            let minSignOutTime = g.Min(t => t.Field<DateTime?>("I_SIGN_OUT_TIME"))
                            where minSignOutTime >= startDate && minSignOutTime <= EndDate
                            //where g.Key.mainnumber == "695-43069036"
                            orderby g.Key.mainnumber, g.Key.trans_name
                            select new
                            {
                                trans_no = g.Key.trans_no,
                                trans_name = g.Key.trans_name,
                                mainnumber = g.Key.mainnumber,
                                line_code = g.Key.line_code
                            }).Distinct();

            rowCount = 3;
            foreach (var item in dt_Group)
            {
                //原單袋數
                var dt_Bl_No = (from t in dt_Details.AsEnumerable()
                                where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["LINE_CODE"].ToString() == item.line_code.ToString()
                                select t["BL_NO"]).Distinct().ToList();
                total_bl_no = dt_Bl_No.Count;

                //入倉袋數
                var dt_In_Bl_No = (from t in dt_Details.AsEnumerable()
                                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["LINE_CODE"].ToString() == item.line_code.ToString() && t["I_SIGN_IN_TIME"].ToString() != ""
                                   select t["BL_NO"]).Distinct().ToList();
                total_in_bl_no = dt_In_Bl_No.Count;

                //出倉袋數
                var dt_Out_Bl_No = (from t in dt_Details.AsEnumerable()
                                    where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["LINE_CODE"].ToString() == item.line_code.ToString() && t["I_SIGN_OUT_TIME"].ToString() != ""
                                    select t["BL_NO"]).Distinct().ToList();
                total_out_bl_no = dt_Out_Bl_No.Count;
                //原單件數
                total_piece = 0;
                int.TryParse(dt_Details.Compute("SUM(I_CARGO_PIECE)", $"MAINNUMBER='{item.mainnumber.ToString()}' and TRANS_NAME='{item.trans_name.ToString()}' and LINE_CODE='{item.line_code.ToString()}'").ToString(), out total_piece);
                //GW
                total_gw = 0;
                double.TryParse(dt_Details.Compute("SUM(I_CARGO_WEIGHT)", $"MAINNUMBER='{item.mainnumber.ToString()}'  and TRANS_NAME='{item.trans_name.ToString()}' and LINE_CODE='{item.line_code.ToString()}'").ToString(), out total_gw);
                //航班代號
                //flightnumber = "";
                //dr = dt_Details.Select($"MAINNUMBER='{item.mainnumber.ToString()}' and TRANS_NAME='{item.trans_name.ToString()}' and FLIGHTNUMBER > '' ");
                //if (dr.Length > 0)
                //{
                //    flightnumber = dr[0]["FLIGHTNUMBER"].ToString();
                //}

                //A03袋號
                var dt_A03_Bl_No = (from t in dt_Details.AsEnumerable()
                                    where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["LINE_CODE"].ToString() == item.line_code.ToString() && t["REMARK"].ToString() == "A03"
                                    select t["FORMAT_BL_NO"]).Distinct().ToList();

                //B6F袋號
                var dt_B6F_Bl_No = (from t in dt_Details.AsEnumerable()
                                    where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["LINE_CODE"].ToString() == item.line_code.ToString() && t["REMARK"].ToString() == "B6F"
                                    select t["BL_NO"]).Distinct().ToList();

                //異常
                error_bl_no = "";
                ////C3袋號
                //var dt_C3_Bl_No = (from t in dt_Details.AsEnumerable()
                //                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["I_SIGN_IN_TIME"].ToString() != "" && t["I_SIGN_OUT_TIME"].ToString() == ""
                //                   select t["BL_NO"]).Distinct().ToList();
                //C3袋號
                var dt_C3_Bl_No = (from t in dt_Details.AsEnumerable()
                                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["LINE_CODE"].ToString() == item.line_code.ToString() && t["REMARK"].ToString() == "C3"
                                   select t["BL_NO"]).Distinct().ToList();
                if (dt_C3_Bl_No.Count > 0)
                {
                    error_bl_no += "C3：";
                    error_bl_no += string.Join(",", dt_C3_Bl_No);
                }
                //未見袋號
                //var dt_No_Bl_No = (from t in dt_Details.AsEnumerable()
                //                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["I_SIGN_IN_TIME"].ToString() == "" && t["I_SIGN_OUT_TIME"].ToString() == ""
                //                   select t["BL_NO"]).Distinct().ToList();
                //未見袋號
                var dt_No_Bl_No = (from t in dt_Details.AsEnumerable()
                                   where t["MAINNUMBER"].ToString() == item.mainnumber.ToString() && t["TRANS_NAME"].ToString() == item.trans_name.ToString() && t["LINE_CODE"].ToString() == item.line_code.ToString() && t["REMARK"].ToString() == "未見"
                                   select t["BL_NO"]).Distinct().ToList();
                if (dt_No_Bl_No.Count > 0)
                {
                    error_bl_no += " 未見：";
                    error_bl_no += string.Join(",", dt_No_Bl_No);
                }

                //A03代號
                if (dt_A03_Bl_No.Count > 0)
                {
                    error_bl_no += " A03：";
                    error_bl_no += string.Join(",", dt_A03_Bl_No);
                }

                //B6F代號
                if (dt_B6F_Bl_No.Count > 0)
                {
                    error_bl_no += " B6F：";
                    error_bl_no += string.Join(",", dt_B6F_Bl_No);
                }

                //C3袋數(已入倉 未出倉)
                total_c3_bl_no = dt_C3_Bl_No.Count;
                //未見(未入倉)
                total_no_bl_no = dt_No_Bl_No.Count;
                //A03
                total_a03_bl_no = dt_A03_Bl_No.Count;
                //B6F
                total_b6f_bl_no = dt_B6F_Bl_No.Count;

                row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(custName);//客戶
                row.CreateCell(1).SetCellValue(item.trans_name.ToString());//派件公司
                row.CreateCell(2).SetCellValue("JIEFENG");//固定JIEFENG
                row.CreateCell(3).SetCellValue(item.mainnumber.ToString());//主提單號
                row.CreateCell(4).SetCellValue(item.line_code);
                row.CreateCell(5).SetCellValue(total_bl_no);//原單袋數
                row.CreateCell(6).SetCellValue("");//P/T

                //班次到達時間
                //DataRow[] dr_Arrive = dt_Arrive.Select($"TRANS_NO='{item.trans_no.ToString()}' and MAINNUMBER='{item.mainnumber.ToString() }'", "UPDATE_TIME desc");

                //班次到達時間
                var dr_Arrive = dt_Arrive.AsEnumerable()
                                  .Where(r => r.Field<string>("TRANS_NO") == item.trans_no.ToString() &&
                                              r.Field<string>("MAINNUMBER") == item.mainnumber.ToString() &&
                                              (r.Field<string>("LINE_CODE") == item.line_code.ToString() || string.IsNullOrEmpty(r.Field<string>("lINE_CODE"))))
                                  .OrderByDescending(r => r.Field<DateTime>("UPDATE_TIME"))
                                  .Select(r => new 
                                  {
                                      FLIGHTNUMBER = r.Field<string>("FLIGHTNUMBER"),
                                      ARRIVE_DATE1 = r.Field<DateTime?>("ARRIVE_DATE1")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE1 = r.Field<DateTime?>("TRANS_DATE1")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE2 = r.Field<DateTime?>("TRANS_DATE2")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE3 = r.Field<DateTime?>("TRANS_DATE3")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE4 = r.Field<DateTime?>("TRANS_DATE4")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE5 = r.Field<DateTime?>("TRANS_DATE5")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE6 = r.Field<DateTime?>("TRANS_DATE6")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE7 = r.Field<DateTime?>("TRANS_DATE7")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE8 = r.Field<DateTime?>("TRANS_DATE8")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE9 = r.Field<DateTime?>("TRANS_DATE9")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_DATE10 = r.Field<DateTime?>("TRANS_DATE10")?.ToString("yyyy/M/d HH:mm"),
                                      TRANS_COUNT1 = r.Field<int?>("TRANS_COUNT1"),
                                      TRANS_COUNT2 = r.Field<int?>("TRANS_COUNT2"),
                                      TRANS_COUNT3 = r.Field<int?>("TRANS_COUNT3"),
                                      TRANS_COUNT4 = r.Field<int?>("TRANS_COUNT4"),
                                      TRANS_COUNT5 = r.Field<int?>("TRANS_COUNT5"),
                                      TRANS_COUNT6 = r.Field<int?>("TRANS_COUNT6"),
                                      TRANS_COUNT7 = r.Field<int?>("TRANS_COUNT7"),
                                      TRANS_COUNT8 = r.Field<int?>("TRANS_COUNT8"),
                                      TRANS_COUNT9 = r.Field<int?>("TRANS_COUNT9"),
                                      TRANS_COUNT10 = r.Field<int?>("TRANS_COUNT10"),
                                  })
                                  .FirstOrDefault();

                if (dr_Arrive != null)
                {
                    row.CreateCell(8).SetCellValue(dr_Arrive.FLIGHTNUMBER);//航班代號
                    row.CreateCell(9).SetCellValue("");//Pu date
                    row.CreateCell(10).SetCellValue(dr_Arrive.ARRIVE_DATE1 ?? "");//到達時間1

                    //到達時間
                    row.CreateCell(84).SetCellValue(dr_Arrive.TRANS_DATE1 ?? ""); //派件公司送達時間
                    row.CreateCell(85).SetCellValue(dr_Arrive.TRANS_COUNT1.ToString()); //袋數
                    row.CreateCell(86).SetCellValue(dr_Arrive.TRANS_DATE2 ?? ""); //派件公司送達時間
                    row.CreateCell(87).SetCellValue(dr_Arrive.TRANS_COUNT2.ToString()); //袋數
                    row.CreateCell(88).SetCellValue(dr_Arrive.TRANS_DATE3 ?? ""); //派件公司送達時間
                    row.CreateCell(89).SetCellValue(dr_Arrive.TRANS_COUNT3.ToString()); //袋數
                    row.CreateCell(90).SetCellValue(dr_Arrive.TRANS_DATE4 ??""); //派件公司送達時間
                    row.CreateCell(91).SetCellValue(dr_Arrive.TRANS_COUNT4.ToString()); //袋數
                    row.CreateCell(92).SetCellValue(dr_Arrive.TRANS_DATE5 ?? ""); //派件公司送達時間
                    row.CreateCell(93).SetCellValue(dr_Arrive.TRANS_COUNT5.ToString()); //袋數
                    row.CreateCell(94).SetCellValue(dr_Arrive.TRANS_DATE6 ?? ""); //派件公司送達時間
                    row.CreateCell(95).SetCellValue(dr_Arrive.TRANS_COUNT6.ToString()); //袋數
                    row.CreateCell(96).SetCellValue(dr_Arrive.TRANS_DATE7 ?? ""); //派件公司送達時間
                    row.CreateCell(97).SetCellValue(dr_Arrive.TRANS_COUNT7.ToString()); //袋數
                    row.CreateCell(98).SetCellValue(dr_Arrive.TRANS_DATE8 ?? ""); //派件公司送達時間
                    row.CreateCell(99).SetCellValue(dr_Arrive.TRANS_COUNT8.ToString()); //袋數
                    row.CreateCell(100).SetCellValue(dr_Arrive.TRANS_DATE9 ?? ""); //派件公司送達時間
                    row.CreateCell(101).SetCellValue(dr_Arrive.TRANS_COUNT9.ToString()); //袋數
                    row.CreateCell(102).SetCellValue(dr_Arrive.TRANS_DATE10 ?? ""); //派件公司送達時間
                    row.CreateCell(103).SetCellValue(dr_Arrive.TRANS_COUNT10.ToString()); //袋數
                }

                //取得出倉時間區間
                var signOutTimeList = GetSignOutTimeList(dt_Details, item.trans_name, item.mainnumber, item.line_code);

                colCount = 0;
                foreach (var item2 in signOutTimeList)
                {
                    if (20 + colCount <= 43)
                    {
                        row.CreateCell(20 + colCount).SetCellValue(item2.SignOutTime.ToString("yyyy/M/d HH:mm"));//出倉時間
                        row.CreateCell(21 + colCount).SetCellValue(item2.TotalBlNo); //出倉袋數
                    }
                    colCount = colCount + 2;
                }

                //交倉時間
                var arrivalTimeData = GetArrivalTimeList(dt_Details, item.trans_name, item.mainnumber, item.line_code);

                colCount = 0;
                foreach (var item2 in arrivalTimeData)
                {
                    if (108 + colCount <= 131)
                    {
                        //清關沒有箱數，交倉時間就不要顯示
                        var sourceCell = row.GetCell(21 + colCount);
                        if (sourceCell != null)
                        {
                            row.CreateCell(108 + colCount).SetCellValue(item2.ToString("yyyy/MM/dd HH:mm"));//交倉時間
                            row.CreateCell(109 + colCount).SetCellValue(Convert.ToInt32(sourceCell.ToString())); //箱號
                        }
                    }
                    colCount = colCount + 2;
                }
                rowCount++;
            }
        }


        /// <summary>
        /// 取得班次到達時間
        /// </summary>
        /// <returns></returns>
        public DataTable GetArriveList(string custId, string sDate, string eDate)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_Arrive_Upload]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@CustId", SqlDbType.NVarChar).Value = custId;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} 13:00:00";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} 12:59:59";
                da.Fill(dt);
            }
            return dt;
        }

        public DataTableModel CustWorkLoadDetails(string custId, string sDate, string eDate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            DateTime sign_in_time, sign_out_time, maxTime;
            try
            {
                string mainnumber, bl_no;
                DataRow[] dr;
                DataTable dt_Upload = new DataTable();
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[SP_Select_CustWorkLoadDetails_New_V2]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.CommandTimeout = 1200;
                    da.SelectCommand.Parameters.Add("@CustId", SqlDbType.NVarChar).Value = custId;
                    da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} 13:00:00";
                    da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} 12:59:59";
                    da.Fill(dt);
                }

                using (SqlDataAdapter da = new SqlDataAdapter("select distinct MAINNUMBER,BL_NO,REMARK,BAGCOUNT from A03_B6F_UPLOAD", conn))
                {
                    da.Fill(dt_Upload);
                }

                dt.Columns.Add("I_SIGN_OUT_DATE", typeof(string));
                dt.Columns.Add("REMARK", typeof(string));
                dt.Columns.Add("FORMAT_BL_NO", typeof(string));


                maxTime = Convert.ToDateTime($"{eDate} 12:59:59");

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    //無派件公司
                    if (dt.Rows[i]["TRANS_NAME"].ToString() == "")
                    {
                        dt.Rows[i]["TRANS_NAME"] = "無派件公司";
                    }

                    //進倉日
                    if (DateTime.TryParse(dt.Rows[i]["I_SIGN_IN_TIME"].ToString(), out sign_in_time))
                    {
                        //進倉日大於最到時間清空資料
                        if (sign_in_time > maxTime)
                        {
                            dt.Rows[i]["I_SIGN_IN_TIME"] = DBNull.Value;
                        }
                    }

                    //出倉日
                    if (DateTime.TryParse(dt.Rows[i]["I_SIGN_OUT_TIME"].ToString(), out sign_out_time))
                    {
                        //出倉日大於最到時間清空資料
                        if (sign_out_time > maxTime)
                        {
                            dt.Rows[i]["I_SIGN_OUT_TIME"] = DBNull.Value;
                        }
                    }

                    //新增出倉日期
                    if (DateTime.TryParse(dt.Rows[i]["I_SIGN_OUT_TIME"].ToString(), out sign_out_time))
                    {
                        dt.Rows[i]["I_SIGN_OUT_DATE"] = sign_out_time.AddHours(-13).ToString("yyyyMMdd");
                    }

                    mainnumber = dt.Rows[i]["MAINNUMBER"].ToString();
                    bl_no = dt.Rows[i]["BL_NO"].ToString();

                    //未見
                    if (dt.Rows[i]["I_SIGN_IN_TIME"].ToString() == "" && dt.Rows[i]["I_SIGN_OUT_TIME"].ToString() == "")
                    {
                        //先判斷A03、B6F
                        dr = dt_Upload.Select($"MAINNUMBER='{mainnumber}' and BL_NO='{bl_no}'", "REMARK");
                        if (dr.Length > 0)
                        {
                            if (dr[0]["BAGCOUNT"].ToString() != "")
                            {
                                dt.Rows[i]["FORMAT_BL_NO"] = $"{dr[0]["BL_NO"].ToString()}*{dr[0]["BAGCOUNT"].ToString()}";
                            }
                            else
                            {
                                dt.Rows[i]["FORMAT_BL_NO"] = dr[0]["BL_NO"].ToString();
                            }

                            dt.Rows[i]["REMARK"] = dr[0]["REMARK"].ToString();
                        }
                        else
                        {
                            dt.Rows[i]["REMARK"] = "未見";
                        }
                    }

                    //C3
                    if (dt.Rows[i]["I_SIGN_IN_TIME"].ToString() != "" && dt.Rows[i]["I_SIGN_OUT_TIME"].ToString() == "")
                    {
                        //先判斷A03、B6F
                        dr = dt_Upload.Select($"MAINNUMBER='{mainnumber}' and BL_NO='{bl_no}'", "REMARK");
                        if (dr.Length > 0)
                        {
                            if (dr[0]["BAGCOUNT"].ToString() != "")
                            {
                                dt.Rows[i]["FORMAT_BL_NO"] = $"{dr[0]["BL_NO"].ToString()}*{dr[0]["BAGCOUNT"].ToString()}";
                            }
                            else
                            {
                                dt.Rows[i]["FORMAT_BL_NO"] = dr[0]["BL_NO"].ToString();
                            }

                            dt.Rows[i]["REMARK"] = dr[0]["REMARK"].ToString();
                        }
                        else
                        {
                            dt.Rows[i]["REMARK"] = "C3";
                        }
                    }
                }

                //排序
                DataView dv = dt.DefaultView;
                dv.Sort = "MAINNUMBER,TRANS_NAME";
                dt = dv.ToTable();

                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }
            return dataTableModel;
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

        /// <summary>
        /// 取得出艙時間區間
        /// </summary>
        /// <param name="dt_Details"></param>
        /// <param name="trans_name"></param>
        /// <param name="mainnumber"></param>
        /// <returns></returns>
        private List<SignOutTimeModel> GetSignOutTimeList(DataTable dt_Details,string trans_name,string mainnumber, string line_code)
        {
            //袋號重覆的Group，取第一次的出倉時間就好
            var signOutTimeGroup = dt_Details.AsEnumerable()
                                  .Where(t => t.Field<string>("TRANS_NAME") == trans_name.ToString() &&
                                              t.Field<string>("MAINNUMBER") == mainnumber.ToString() &&
                                              t.Field<string>("LINE_CODE") == line_code.ToString() &&
                                              t.Field<DateTime?>("I_SIGN_OUT_TIME") != null)
                                  .GroupBy(t => new
                                  {
                                      TransName = t.Field<string>("TRANS_NAME"),
                                      Mainnumber = t.Field<string>("MAINNUMBER"),
                                      BlNo = t.Field<string>("BL_NO"),
                                  })
                                  .Select(t => new
                                  {
                                      TransName = t.Key.TransName,
                                      Mainnumber = t.Key.Mainnumber,
                                      BlNo = t.Key.BlNo,
                                      SignOutTime = t.Min(m => m.Field<DateTime>("I_SIGN_OUT_TIME")),
                                  })
                                  .ToList();

            //出倉時間明細
            //SPX   第一個時段 9~12 第二個時段 12~21 第三個時段 21~隔天9
            //HL/FM 第一個時段 9~15 第二個時段 16~08
            var signOutTimeDetail = signOutTimeGroup.Select(t => new
            {
                DataDate = t.SignOutTime.AddHours(-9).ToString("yyyyMMdd"),
                TransName = t.TransName,
                Mainnumber = t.Mainnumber,
                BlNo = t.BlNo,
                SignOutTime = t.SignOutTime,
                TimeRange = t.TransName == "SPX" 
                                          ? (t.SignOutTime.Hour >= 9 && t.SignOutTime.Hour < 12 ? 1 
                                          : t.SignOutTime.Hour >= 12 && t.SignOutTime.Hour < 21 ? 2 : 3) 
                                          : (t.SignOutTime.Hour >= 9 && t.SignOutTime.Hour < 16 ? 1 : 2)
            }).ToList();

            //出倉時間結果
            return signOutTimeDetail.GroupBy(r => new
            {
                r.DataDate,
                r.TimeRange,
                r.TransName,
                r.Mainnumber,
            }).Select(r => new SignOutTimeModel()
            {
                Mainnumber = r.Key.Mainnumber,
                TransName = r.Key.TransName,
                SignOutTime = r.Min(m => m.SignOutTime),
                TotalBlNo = r.Select(m => m.BlNo).Distinct().Count()
            }).OrderBy(r => r.SignOutTime)
            .ToList();
        }

        /// <summary>
        /// 取得交倉時間區間
        /// </summary>
        /// <param name="dt_Details"></param>
        /// <param name="trans_name"></param>
        /// <param name="mainnumber"></param>
        /// <returns></returns>
        private List<DateTime> GetArrivalTimeList(DataTable dt_Details, string trans_name, string mainnumber,string line_code)
        {
            //交倉時間
            var arrivalTimeData = dt_Details.AsEnumerable()
                                  .Where(r => r.Field<string>("TRANS_NAME") == trans_name.ToString() &&
                                              r.Field<string>("MAINNUMBER") == mainnumber.ToString() &&
                                              r.Field<string>("lINE_CODE") == line_code.ToString() &&
                                              !string.IsNullOrEmpty(r.Field<string>("ArrivalTime")))
                                  .Select(r => new
                                  {
                                      TransName = r.Field<string>("TRANS_NAME"),
                                      ArrivalTime = Convert.ToDateTime(r.Field<string>("ArrivalTime"))
                                  }).Distinct().ToList();
                                 

            //出倉時間明細
            //SPX   第一個時段 9~12 第二個時段 12~21 第三個時段 21~隔天9
            //HL/FM 第一個時段 9~15 第二個時段 16~08
            var arrivalTimeDataDetail = arrivalTimeData.Select(r => new
            {
                DataDate = r.ArrivalTime.AddHours(-9).ToString("yyyyMMdd"),
                ArrivalTime = r.ArrivalTime,
                TimeRange = r.TransName == "SPX"
                                          ? (r.ArrivalTime.Hour >= 9 && r.ArrivalTime.Hour < 12 ? 1
                                          :  r.ArrivalTime.Hour >= 12 && r.ArrivalTime.Hour < 21 ? 2 : 3)
                                          : (r.ArrivalTime.Hour >= 9 && r.ArrivalTime.Hour < 16 ? 1 : 2)
            }).ToList();

           var result = arrivalTimeDataDetail.GroupBy(r => new
            {
                r.DataDate,
                r.TimeRange
            }).Select(r => 
                r.Min(it => it.ArrivalTime)
            )
            .OrderBy(r => r)
            .ToList();

            //出倉時間結果
            return result;
        }

    }

   
}
