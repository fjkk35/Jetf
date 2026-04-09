using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Models.Cargo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CargoService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public CargoService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 取得貨況
        /// </summary>
        /// <returns></returns>
        public DataTable GetFee_Master(string invoice)
        {
            DateTime date;
            GlobalService globalService = new GlobalService();
            StringBuilder sb = new StringBuilder();
            sb.Append("select DATADATE,SOURCE,TYPE,isnull(b.CUST_NAME,a.CUSTOMER) as CUST_NAME,IN_DATETIME,OUT_DATETIME,MAIN_NUMBER,TRACKINGNO as 'BAG_NUMBER',DLV_INV,TAX_NUMBER,DLV_COM as 'TRANS_NAME',RECIPIENT,RECPHONE,RECADDRESS,a.INCLUDE_TAX,CCFEE,a.FEE,a.COD,a.TAX1,a.TAX2,TO_DLV_COD from [jetf].[dbo].[FEE_MASTER] a ");
            sb.Append("left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
            sb.Append("where [SOURCE_TYPE] in('1','2') and (DLV_INV=@DLV_INV or TRACKINGNO=@DLV_INV) ");
            sb.Append("union all ");
            sb.Append("select DATADATE,SOURCE,TYPE,b.CUSTOMER,IN_DATETIME,OUT_DATETIME,MAIN_NUMBER,BAG_NUMBER,DLV_INV,TAX_NUMBER,b.TRANS_NAME,RECIPIENT,RECPHONE,RECADDRESS,a.INCLUDE_TAX,CCFEE,a.FEE,a.COD,a.TAX1,a.TAX2,TO_DLV_COD from [jetf].[dbo].[FEE_MASTER] a ");
            sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO and b.TRAN_TYPE='空運' ");
            sb.Append("where[SOURCE_TYPE]='3' and (DLV_INV=@DLV_INV or BAG_NUMBER=@DLV_INV) ");

            DataTable dt = new DataTable();
            if (invoice != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = invoice;
                    da.Fill(dt);
                }

                //if (dt.Rows.Count == 0)
                //{
                //    dt = GetSource_Fee_Master(invoice);
                //}
            }

            dt.Columns.Add("Format_OUT_DATETIME", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (DateTime.TryParse(dt.Rows[i]["OUT_DATETIME"].ToString(), out date))
                {
                    dt.Rows[i]["Format_OUT_DATETIME"] = date.ToString("yyyy-MM-dd");
                }
                dt.Rows[i]["INCLUDE_TAX"] = globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString());
            }

            return dt;
        }

        /// <summary>
        /// 取得貨況
        /// </summary>
        /// <returns></returns>
        //public DataTable GetMerge_Originallist(string id, string invoice,string searchType)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("select a.Id,ORIGINAL,ETA,GW,PIECE,F_DataDate,I_DATA_TYPE,I_CLEARANCE_TYPE,DESPATCH_NAME,a.CUSTOMER,I_SIGN_IN_TIME,I_SIGN_OUT_TIME,MAINNUMBER,BL_NO,JETF_SERIAL,F_TAX_NUMBER,a.TRANS_NAME,IMPORTER,IM_PHONENO,IM_ADD,F_INCLUDE_TAX,F_CCFEE,F_FEE,F_COD,F_TAX1,F_TAX2,F_TO_DLV_COD,ITEM_NAME,CC,DELIVERYNO,FIELD_X,TRANS_TAXPAYMENT,isnull(b.TRANS_NAME,TRANS_TAXPAYMENT) as 'TRANS_NAME_NEW',ORDER_NO,EXPRESS_NO,TRACKINGNO from [jetf].[dbo].[MERGE_ORIGINALLIST] (nolock) a ");
        //    sb.Append("left join [jetf].[dbo].[customer_master] b on a.DESPATCH_NAME=b.CUST_ID and a.TRANS_TAXPAYMENT=b.TRANS_NO ");
        //    if (id == "")
        //    {
        //        switch(searchType)
        //        {
        //            case "trackingNo":
        //            sb.Append("where JETF_SERIAL=@JETF_SERIAL or BL_NO=@JETF_SERIAL ");
        //                break;
        //            case "invoice":
        //                sb.Append("where DELIVERYNO=@JETF_SERIAL ");
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        sb.Append("where a.Id=@Id ");
        //    }

        //    DataTable dt = new DataTable();
        //    if (id != "" || invoice != "")
        //    {
        //        using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
        //        {
        //            da.SelectCommand.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
        //            da.SelectCommand.Parameters.Add("@JETF_SERIAL", SqlDbType.NVarChar).Value = invoice;
        //            da.Fill(dt);
        //        }
        //        //資料格式化
        //        FormatData(dt);
        //    }
        //    return dt;
        //}

        /// <summary>
        /// 取得貨況-電話
        /// </summary>
        /// <param name="phone">電話</param>
        /// <returns></returns>
        public DataTable GetMerge_Originallist_Phone(string phone)
        {
            string phone2, phone3, phone4, phone5, phone6, phone7, phone8, phone9, phone10, phone11, phone12;
            phone2 = phone.Substring(1);//911123456
            phone3 = "886" + phone; //8860911123456
            phone4 = "886" + phone.Substring(1); //886911123456
            phone5 = "+886" + phone; //+8860911123456
            phone6 = "+886" + phone.Substring(1); //+886911123456
            phone11 = "00886" + phone; //008860911123456
            phone12 = "00886" + phone.Substring(1); //00886911123456
            if (phone.Length > 7)
            {
                phone7 = "886-" + phone.Insert(6, "-").Insert(2, "-");
                phone8 = "886-" + phone.Substring(1).Insert(5, "-").Insert(1, "-");
                phone9 = "+886-" + phone.Insert(6, "-").Insert(2, "-");
                phone10 = "+886-" + phone.Substring(1).Insert(5, "-").Insert(1, "-");
            }
            else
            {
                phone7 = phone;
                phone8 = phone;
                phone9 = phone;
                phone10 = phone;
            }

            DateTime date;
            GlobalService globalService = new GlobalService();
            StringBuilder sb = new StringBuilder();
            sb.Append("select a.Id,ORIGINAL,ETA,GW,PIECE,F_DataDate,I_DATA_TYPE,I_CLEARANCE_TYPE,DESPATCH_NAME,a.CUSTOMER,I_SIGN_IN_TIME,I_SIGN_OUT_TIME,MAINNUMBER,BL_NO,JETF_SERIAL,F_TAX_NUMBER,a.TRANS_NAME,IMPORTER,IM_PHONENO,IM_ADD,F_INCLUDE_TAX,F_CCFEE,F_FEE,F_COD,F_TAX1,F_TAX2,F_TO_DLV_COD,ITEM_NAME,CC,DELIVERYNO,FIELD_X,TRANS_TAXPAYMENT,isnull(b.TRANS_NAME,TRANS_TAXPAYMENT) as 'TRANS_NAME_NEW',ORDER_NO,EXPRESS_NO,TRACKINGNO from [jetf].[dbo].[MERGE_ORIGINALLIST](nolock) a ");
            sb.Append("left join [jetf].[dbo].[customer_master] b on a.DESPATCH_NAME=b.CUST_ID and a.TRANS_TAXPAYMENT=b.TRANS_NO ");
            //sb.Append("where IM_PHONENO  LIKE @IM_PHONENO + '%'");
            sb.Append("where IM_PHONENO=@IM_PHONENO ");
            sb.Append("or IM_PHONENO=@IM_PHONENO2 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO3 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO4 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO5 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO6 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO7 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO8 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO9 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO10 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO11 ");
            sb.Append("or IM_PHONENO=@IM_PHONENO12 ");
            sb.Append("order by a.CREATEDATE desc ");

            DataTable dt = new DataTable();
            if (phone != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@IM_PHONENO", SqlDbType.NVarChar).Value = phone;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO2", SqlDbType.NVarChar).Value = phone2;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO3", SqlDbType.NVarChar).Value = phone3;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO4", SqlDbType.NVarChar).Value = phone4;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO5", SqlDbType.NVarChar).Value = phone5;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO6", SqlDbType.NVarChar).Value = phone6;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO7", SqlDbType.NVarChar).Value = phone7;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO8", SqlDbType.NVarChar).Value = phone8;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO9", SqlDbType.NVarChar).Value = phone9;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO10", SqlDbType.NVarChar).Value = phone10;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO11", SqlDbType.NVarChar).Value = phone11;
                    da.SelectCommand.Parameters.Add("@IM_PHONENO12", SqlDbType.NVarChar).Value = phone12;
                    da.Fill(dt);
                }

                //資料格式化
                FormatData(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得貨況-Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable GetMerge_Originallist_Id(string id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select a.Id,ORIGINAL,ETA,GW,PIECE,F_DataDate,I_DATA_TYPE,I_CLEARANCE_TYPE,DESPATCH_NAME,a.CUSTOMER,I_SIGN_IN_TIME,I_SIGN_OUT_TIME,MAINNUMBER,BL_NO,JETF_SERIAL,F_TAX_NUMBER,a.TRANS_NAME,IMPORTER,IM_PHONENO,IM_ADD,F_INCLUDE_TAX,F_CCFEE,F_FEE,F_COD,F_TAX1,F_TAX2,F_TO_DLV_COD,ITEM_NAME,CC,DELIVERYNO,FIELD_X,TRANS_TAXPAYMENT,isnull(b.TRANS_NAME,TRANS_TAXPAYMENT) as 'TRANS_NAME_NEW',ORDER_NO,EXPRESS_NO,TRACKINGNO,Status from [jetf].[dbo].[MERGE_ORIGINALLIST] (nolock) a ");
            sb.Append("left join [jetf].[dbo].[customer_master] b on a.DESPATCH_NAME=b.CUST_ID and a.TRANS_TAXPAYMENT=b.TRANS_NO ");
            sb.Append("where a.Id=@Id ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
                da.Fill(dt);
            }

            //資料格式化
            FormatData(dt);
            return dt;
        }


        /// <summary>
        /// 貨件狀態查詢
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public string GetEtlStatus(string trackingNo)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("", conn))
            {
                da.SelectCommand.CommandText = "select MODEL from DATA_CENTER.dbo.AIR_DETAIN where TRACKINGNO=@TRACKINGNO";
                da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = trackingNo;
                da.Fill(dt);
            }

            if (dt.Rows.Count > 0)
            {
                var model = dt.Rows[0]["MODEL"].ToString();

                model = model == "DU" ? "出口地扣留" :
                        model == "GF" ? "G類無ID" : model;

                return model;
            }

            return "";
        }

        /// <summary>
        /// 取得貨況-物流貨號
        /// </summary>
        /// <param name="deliveryno"></param>
        /// <returns></returns>
        public DataTable GetMerge_Originallist_Deliveryno(string deliveryno)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select a.Id,ORIGINAL,ETA,GW,PIECE,F_DataDate,I_DATA_TYPE,I_CLEARANCE_TYPE,DESPATCH_NAME,a.CUSTOMER,I_SIGN_IN_TIME,I_SIGN_OUT_TIME,MAINNUMBER,BL_NO,JETF_SERIAL,F_TAX_NUMBER,a.TRANS_NAME,IMPORTER,IM_PHONENO,IM_ADD,F_INCLUDE_TAX,F_CCFEE,F_FEE,F_COD,F_TAX1,F_TAX2,F_TO_DLV_COD,ITEM_NAME,CC,DELIVERYNO,FIELD_X,TRANS_TAXPAYMENT,isnull(b.TRANS_NAME,TRANS_TAXPAYMENT) as 'TRANS_NAME_NEW',ORDER_NO,EXPRESS_NO,TRACKINGNO from [jetf].[dbo].[MERGE_ORIGINALLIST] (nolock) a ");
            sb.Append("left join [jetf].[dbo].[customer_master] b on a.DESPATCH_NAME=b.CUST_ID and a.TRANS_TAXPAYMENT=b.TRANS_NO ");
            sb.Append("where DELIVERYNO=@DELIVERYNO ");

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@DELIVERYNO", SqlDbType.NVarChar).Value = deliveryno;
                da.Fill(dt);
            }

            //資料格式化
            FormatData(dt);
            return dt;
        }

        /// <summary>
        /// 取得貨況-分提單號
        /// </summary>
        /// <param name="deliveryno"></param>
        /// <returns></returns>
        public DataTable GetMerge_Originallist_Jetf_Serial(string jetf_Serial)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select a.Id,ORIGINAL,ETA,GW,PIECE,F_DataDate,I_DATA_TYPE,I_CLEARANCE_TYPE,DESPATCH_NAME,a.CUSTOMER,I_SIGN_IN_TIME,I_SIGN_OUT_TIME,MAINNUMBER,BL_NO,JETF_SERIAL,F_TAX_NUMBER,a.TRANS_NAME,IMPORTER,IM_PHONENO,IM_ADD,F_INCLUDE_TAX,F_CCFEE,F_FEE,F_COD,F_TAX1,F_TAX2,F_TO_DLV_COD,ITEM_NAME,CC,DELIVERYNO,FIELD_X,TRANS_TAXPAYMENT,isnull(b.TRANS_NAME,TRANS_TAXPAYMENT) as 'TRANS_NAME_NEW',ORDER_NO,EXPRESS_NO,TRACKINGNO from [jetf].[dbo].[MERGE_ORIGINALLIST] (nolock) a ");
            sb.Append("left join [jetf].[dbo].[customer_master] b on a.DESPATCH_NAME=b.CUST_ID and a.TRANS_TAXPAYMENT=b.TRANS_NO ");
            sb.Append("where JETF_SERIAL=@JETF_SERIAL ");

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@JETF_SERIAL", SqlDbType.NVarChar).Value = jetf_Serial;
                da.Fill(dt);
            }

            //資料格式化
            FormatData(dt);
            return dt;
        }

        /// <summary>
        /// 取得貨況-袋號
        /// </summary>
        /// <param name="deliveryno"></param>
        /// <returns></returns>
        public DataTable GetMerge_Originallist_Bl_No(string bl_No)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select a.Id,ORIGINAL,ETA,GW,PIECE,F_DataDate,I_DATA_TYPE,I_CLEARANCE_TYPE,DESPATCH_NAME,a.CUSTOMER,I_SIGN_IN_TIME,I_SIGN_OUT_TIME,MAINNUMBER,BL_NO,JETF_SERIAL,F_TAX_NUMBER,a.TRANS_NAME,IMPORTER,IM_PHONENO,IM_ADD,F_INCLUDE_TAX,F_CCFEE,F_FEE,F_COD,F_TAX1,F_TAX2,F_TO_DLV_COD,ITEM_NAME,CC,DELIVERYNO,FIELD_X,TRANS_TAXPAYMENT,isnull(b.TRANS_NAME,TRANS_TAXPAYMENT) as 'TRANS_NAME_NEW',ORDER_NO,EXPRESS_NO,TRACKINGNO from [jetf].[dbo].[MERGE_ORIGINALLIST] (nolock) a ");
            sb.Append("left join [jetf].[dbo].[customer_master] b on a.DESPATCH_NAME=b.CUST_ID and a.TRANS_TAXPAYMENT=b.TRANS_NO ");
            sb.Append("where BL_NO=@BL_NO ");

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@BL_NO", SqlDbType.NVarChar).Value = bl_No;
                da.Fill(dt);
            }

            //資料格式化
            FormatData(dt);
            return dt;
        }


        /// <summary>
        /// 取得使用速派物流貨號取得上傳分提單號
        /// </summary>
        /// <param name="deliveryNo"></param>
        /// <returns></returns>
        public string GetShenzhenCargoTrackingNo(string deliveryNo)
        {
            string sql = @"select TrackingNo from ShenzhenCargo
                           where DeliveryNo= @DeliveryNo
                          ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@DeliveryNo", SqlDbType.NVarChar).Value = deliveryNo;
                da.Fill(dt);
            }

            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["TrackingNo"].ToString();
            }

            return "";
        }

        /// <summary>
        /// 取得使用速派物流貨號取得上傳物流貨號
        /// </summary>
        /// <param name="deliveryNo"></param>
        /// <returns></returns>
        public DataTable GetShenzhenCargoDeliveryNo(string trackingNo)
        {
            string sql = @"select TrackingNo,DeliveryNo from ShenzhenCargo
                           where TrackingNo = @TrackingNo
                          ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = trackingNo;
                da.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// 使用客戶外箱號，回傳袋號
        /// </summary>
        /// <param name="field_X"></param>
        /// <returns></returns>
        public string GetOriginallist_BagNo(string field_X)
        {
            string bagNo = "";
            StringBuilder sb = new StringBuilder();
            sb.Append("select BAGNO from [DATA_CENTER].[dbo].[ORIGINALLIST] (nolock) ");
            sb.Append("where FIELD_X=@FIELD_X ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@FIELD_X", SqlDbType.NVarChar).Value = field_X;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                bagNo = dt.Rows[0]["BAGNO"].ToString().Trim();
            }

            return bagNo;
        }

        /// <summary>
        /// 使用客戶訂單號，回傳物流貨號
        /// </summary>
        /// <param name="field_X"></param>
        /// <returns></returns>
        public string GetOriginallist_Deliveryno(string Order_No)
        {
            string deliveryno = "";
            StringBuilder sb = new StringBuilder();
            sb.Append("select DELIVERYNO from [DATA_CENTER].[dbo].[ORIGINALLIST] (nolock) ");
            sb.Append("where ORDER_NO=@ORDER_NO ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@ORDER_NO", SqlDbType.NVarChar).Value = Order_No;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                deliveryno = dt.Rows[0]["DELIVERYNO"].ToString().Trim();
            }

            return deliveryno;
        }

        /// <summary>
        /// 資料格式化
        /// </summary>
        public void FormatData(DataTable dt)
        {
            DateTime date;
            GlobalService globalService = new GlobalService();
            dt.Columns.Add("Format_OUT_DATETIME", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (DateTime.TryParse(dt.Rows[i]["I_SIGN_OUT_TIME"].ToString(), out date))
                {
                    dt.Rows[i]["Format_OUT_DATETIME"] = date.ToString("yyyy-MM-dd");
                }
                dt.Rows[i]["F_INCLUDE_TAX"] = globalService.GetTaxType(dt.Rows[i]["F_INCLUDE_TAX"].ToString());
            }
        }


        /// <summary>
        /// 取得稅金資料
        /// </summary>
        public FeeMasterModel GetFeeMaster(string deliveryNo)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("select * from [jetf].[dbo].[FEE_MASTER] where DLV_INV=@DLV_INV", conn))
            {
                da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = deliveryNo;
                da.Fill(dt);
            }

            if(dt.Rows.Count == 0)
                 return null;

            var model = new FeeMasterModel() 
            {
                IncludeTax = dt.Rows[0]["INCLUDE_TAX"].ToString(),
                Tax1 = Int32.TryParse(dt.Rows[0]["TAX1"].ToString(), out var tax1) ? tax1 : 0,
                Tax2 = Int32.TryParse(dt.Rows[0]["TAX2"].ToString(), out var tax2) ? tax2 : 0,
                TotalTax = tax1 + tax2,
                CcFee = Int32.TryParse(dt.Rows[0]["CCFEE"].ToString(), out var ccFee) ? ccFee : 0,
                Fee = Int32.TryParse(dt.Rows[0]["FEE"].ToString(), out var fee) ? fee : 0,
                Cod = Int32.TryParse(dt.Rows[0]["COD"].ToString(), out var cod) ? cod : 0,
                ToDlvCod = Int32.TryParse(dt.Rows[0]["TO_DLV_COD"].ToString(), out var toDlvCod) ? toDlvCod : 0,
                CustomerCod = Int32.TryParse(dt.Rows[0]["CUSTOMER_COD"].ToString(), out var customerCod) ? customerCod : 0,
                TransCod = Int32.TryParse(dt.Rows[0]["TRANS_COD"].ToString(), out var transCod) ? transCod : 0,
            };

            return model;
        }


        /// <summary>
        /// 取得物流配送狀態
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        public DataTable GetCargo_Status_Detail(string trans_number)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from [DATA_CENTER].[dbo].[CARGO_STATUS_DETAIL] (nolock) where TRANS_NUMBER=@TRANS_NUMBER order by TRANS_MODIFY_TIME desc  ");
            DataTable dt = new DataTable();
            if (trans_number != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@TRANS_NUMBER", SqlDbType.NVarChar).Value = trans_number;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 新增貨況查詢記錄
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel InsertLog_Cargo_Status(LogCargoStatusModel model)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[LOG_CARGO_STATUS]([DLV_INV],[SEARCH_TIME],[REMARK],[USER_ID],[USER_IP]) ");
            sb.Append("values(@DLV_INV,@SEARCH_TIME,@REMARK,@USER_ID,@USER_IP) ");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
            {
                try
                {
                    conn.Open();
                    cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = model.Dlv_Inv;
                    cmd.Parameters.Add("@SEARCH_TIME", SqlDbType.NVarChar).Value = model.Search_Time;
                    cmd.Parameters.Add("@REMARK", SqlDbType.NVarChar).Value = model.Remark;
                    cmd.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = model.User_Id;
                    cmd.Parameters.Add("@USER_IP", SqlDbType.NVarChar).Value = model.User_Ip;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                }
                finally
                {
                    conn.Close();
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 取得貨況查詢記錄
        /// </summary>
        /// <param name="dlv_inv"></param>
        /// <returns></returns>
        public DataTable GetLog_Cargo_Status(string dlv_inv)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select  * from  [jetf].[dbo].[LOG_CARGO_STATUS] where DLV_INV=@DLV_INV and REMARK='貨況查詢' ");
            DataTable dt = new DataTable();
            if (dlv_inv != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 取得貨況查詢-通關袋號
        /// </summary>
        /// <param name="dlv_inv"></param>
        /// <returns></returns>
        public DataTable GetTargetBagNumber(string bagNumber)
        {
            DataTable dt = new DataTable();

            if (bagNumber != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_GetCargoTargetBagNumber]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@BagNumber", SqlDbType.NVarChar).Value = bagNumber;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 取得貨況查詢-併分提單號
        /// </summary>
        /// <param name="dlv_inv"></param>
        /// <returns></returns>
        public DataTable GetTargetTrackingNo(string bagNumber)
        {
            DataTable dt = new DataTable();

            if (bagNumber != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_GetCargoTargetTrackingNo]", conn))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@BagNumber", SqlDbType.NVarChar).Value = bagNumber;
                    da.Fill(dt);
                }
            }
            return dt;
        }


        /// <summary>
        /// 新增批量貨況查詢明細表
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel InsertBatchSearchCargo2(DataTable dt_Upload, string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";

            DateTime date = DateTime.Now;
            string dataDate = date.ToString("yyyyMMdd");
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[BatchSearchCargo2](TrackingNo, Upload_Time, Upload_Ope) ");
            sb.Append("values(@TrackingNo, @Upload_Time, @Upload_Ope) ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TrackingNo"].ToString();
                            cmd.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                            cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
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
            }

            return resopnseModel;
        }

        /// <summary>
        /// 取得批量貨況查詢明細表
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public DataTableModel GetBatchSearchCargo2(string upload_time, string user_Id)
        {
            DataTable dt = new DataTable();
            DataTableModel dataTableModel = new DataTableModel();
            dataTableModel.status = Status.success;
            dataTableModel.msg = "成功";
            try
            {
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_GetBatchSearchCargo]", conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                    da.SelectCommand.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                    da.Fill(dt);
                }
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }

            return dataTableModel;
        }


        /// <summary>
        /// 新增處置說明
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel InsertProcess(ProcessModel model)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";

            StringBuilder sb = new StringBuilder();
            //sb.Append("insert [jetf].[dbo].[Process]([CUST_ID],[CUSTOMER],[DATADATE],[MAINNUMBER],[BL_NO],[DLV_INV],[SIGN_IN_TIME],[RECIPIENT],[RECPHONE],[REMARK],[FILEPATH],[FILENAME],[USER_ID]) ");
            //sb.Append("values(@CUST_ID,@CUSTOMER,@DATADATE,@MAINNUMBER,@BL_NO,@DLV_INV,@SIGN_IN_TIME,@RECIPIENT,@RECPHONE,@REMARK,@FILEPATH,@FILENAME,@USER_ID) ");
            sb.Append("insert [jetf].[dbo].[Process]([MID],[DATADATE],[DLV_INV],[REMARK],[FILEPATH],[FILENAME],[USER_ID],[PROCESS_TYPE]) ");
            sb.Append("values(@MID,@DATADATE,@DLV_INV,@REMARK,@FILEPATH,@FILENAME,@USER_ID,@PROCESS_TYPE) ");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
            {
                try
                {
                    conn.Open();
                    cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = model.DataDate;
                    cmd.Parameters.Add("@MID", SqlDbType.NVarChar).Value = model.MId;
                    cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = model.Dlv_Inv;
                    cmd.Parameters.Add("@REMARK", SqlDbType.NVarChar).Value = model.Remark;
                    cmd.Parameters.Add("@FILEPATH", SqlDbType.NVarChar).Value = model.FilePath ?? "";
                    cmd.Parameters.Add("@FILENAME", SqlDbType.NVarChar).Value = model.FileName ?? "";
                    cmd.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = model.User_Id;
                    cmd.Parameters.Add("@PROCESS_TYPE", SqlDbType.NVarChar).Value = model.Process_Type ?? "1";
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                }
                finally
                {
                    conn.Close();
                }
            }
            return resopnseModel;

        }

        /// <summary>
        /// 取得處置說明資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetProcess(string dlv_Inv)
        {
            string sql = $@"
                            select a.*,b.[USER_NAME] as [USER_NAME],c.[USER_NAME] as FINISH_USER_NAME,PROCESS_TYPE from jetf.dbo.Process a
                            left join jetf.dbo.[USER_MASTER] b on a.[USER_ID]=b.[USER_ID]
                            left join jetf.dbo.[USER_MASTER] c on a.FINISH_USER_ID=c.[USER_ID]
                            where DLV_INV=@DLV_INV and DEL='0' 
                            order by CRTDATETIME desc
                         ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_Inv;
                da.Fill(dt);
            }
            dt.Columns.Add("FormatCrtDateTime", typeof(string));

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["FormatCrtDateTime"] = Convert.ToDateTime(dt.Rows[i]["CRTDATETIME"]).ToString("yyyy-MM-dd HH:mm:ss");
                dt.Rows[i]["PROCESS_TYPE"] = string.IsNullOrEmpty(dt.Rows[i]["PROCESS_TYPE"].ToString()) 
                    ? "貨況" 
                    : dt.Rows[i]["PROCESS_TYPE"].ToEnum<CargoProcessType>().ToDescription();
            }
            return dt;
        }

        /// <summary>
        /// 刪除處置說明
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel DeleteProcess(string id, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "刪除成功";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("update [jetf].[dbo].[Process] set DEL='1',DEL_USER_ID=@DEL_USER_ID,DELDATETIME=getdate() ");
                sb.Append("where ID=@ID ");
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.NVarChar).Value = id;
                    cmd.Parameters.Add("@DEL_USER_ID", SqlDbType.NVarChar).Value = user_Id;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }
            return resopnseModel;

        }

        /// <summary>
        /// 處置說明結案
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel FinishProcess(string dlv_inv, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "結案成功";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("update [jetf].[dbo].[Process] set FINISH='Y',FINISH_USER_ID=@FINISH_USER_ID,FINISH_DATETIME=getdate() ");
                sb.Append("where DLV_INV=@DLV_INV and FINISH='N' ");
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                    cmd.Parameters.Add("@FINISH_USER_ID", SqlDbType.NVarChar).Value = user_Id;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }
            return resopnseModel;

        }

        /// <summary>
        /// 處置說明下載
        /// </summary>
        /// <param name="sdate"></param>
        /// <param name="edate"></param>
        /// <returns></returns>
        public DataTableModel ProcessReport(string custId, string sdate, string edate, string processType,string finish)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                var sql = @"
                            with cte_Process as 
                            (
	                            SELECT  ROW_NUMBER() OVER (PARTITION BY a.ID ORDER BY a.ID ) as ROW_ID,a.DATADATE,b.ETA,b.DESPATCH_NAME as CUSD_ID,b.CUSTOMER,b.MAINNUMBER,b.BL_NO,a.DLV_INV,b.I_DATA_TYPE,b.I_SIGN_IN_TIME,b.I_SIGN_OUT_TIME,b.IMPORTER,b.IM_PHONENO,a.REMARK,c.[USER_NAME],a.[CRTDATETIME],a.DEL,a.FINISH,a.FINISH_USER_ID,d.[USER_NAME] as FINISH_USER_NAME,a.FINISH_DATETIME,a.PROCESS_TYPE FROM [jetf].[dbo].[Process] a (nolock) 
	                            left join [jetf].[dbo].MERGE_ORIGINALLIST b (nolock) on a.DLV_INV=b.JETF_SERIAL 
	                            left join [jetf].[dbo].[USER_MASTER] c on a.[USER_ID]=c.[USER_ID] 
	                            left join [jetf].[dbo].[USER_MASTER] d on a.[FINISH_USER_ID]=d.[USER_ID] 
	                            where DEL='0' and DATADATE between @sdate and @edate
                            )
                            select * from cte_Process
                            where ROW_ID='1' 
                            AND (@custId = 'All' OR CUSD_ID = @custId)
                            AND (@PROCESS_TYPE = 'All' OR PROCESS_TYPE = @PROCESS_TYPE)
                            AND (@FINISH = 'All' OR FINISH = @FINISH);
                          ";

            

                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate;
                    da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate;
                    da.SelectCommand.Parameters.Add("@custId", SqlDbType.NVarChar).Value = custId;
                    da.SelectCommand.Parameters.Add("@PROCESS_TYPE", SqlDbType.NVarChar).Value = processType;
                    da.SelectCommand.Parameters.Add("@FINISH", SqlDbType.NVarChar).Value = finish;
                    da.Fill(dt);
                }

                //排序
                DataView dv = dt.DefaultView;
                dv.Sort = "DATADATE,CRTDATETIME";
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

        /// <summary>
        /// 批量貨況查詢明細表上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel BatchSearchCargo2(string filePath, string fileName, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            //讀取檔案
            DataTable dt_Upload = ReadExcelBatchSearchCargo2(filePath);

            //新增
            if (dt_Upload.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                resopnseModel = InsertBatchSearchCargo2(dt_Upload, upload_time, userId);

                if (resopnseModel.status == Status.success)
                {
                    //resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                    resopnseModel.msg = $"{upload_time}︿{userId}";
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }

            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 讀取批量貨況查詢明細表上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelBatchSearchCargo2(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("TrackingNo", typeof(string));

            bool read = false;
            string trackingno;
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
                    //分提單號
                    trackingno = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "分提單號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && trackingno != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["TrackingNo"] = trackingno;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 取得轉檔紀錄
        /// </summary>
        /// <returns></returns>
        public DataTable GetLog_Work()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select top 500 * from [jetf].[dbo].[LOG_WORK] order by StartTime desc ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            dt.Columns.Add("Format_StartTime", typeof(string));
            dt.Columns.Add("Format_EndTime", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["Format_StartTime"] = Convert.ToDateTime(dt.Rows[i]["StartTime"]).ToString("yyyy-MM-dd HH:mm:ss");
                dt.Rows[i]["Format_EndTime"] = Convert.ToDateTime(dt.Rows[i]["EndTime"]).ToString("yyyy-MM-dd HH:mm:ss");
            }
            return dt;
        }

        /// <summary>
        /// 取得稅單PDF路徑
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <returns></returns>
        public string GetClearance_Tax_Pdf(string taxNumber)
        {
            string filePath = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Clearance_Tax_Pdf] where TaxNumber=@TaxNumber ", conn))
            {
                da.SelectCommand.Parameters.Add("@TaxNumber", SqlDbType.NVarChar).Value = taxNumber;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                filePath = dt.Rows[0]["FilePath"].ToString();
            }
            dt.Dispose();
            return filePath;
        }

        /// <summary>
        /// 查詢稅金編號
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <returns></returns>
        public DataTable GetTaxNumber(string original, string bagNumber, string dlv_Inv)
        {
            //用分提單號查稅金編號
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("", conn))
            {
                //空運用dlv_Inv查詢
                if (original.ToUpper() == "ETL")
                {
                    da.SelectCommand.CommandText = "SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where MERGE_NUMBER=@MERGE_NUMBER and BAG_NUMBER=@BAG_NUMBER ";
                    da.SelectCommand.Parameters.Add("@BAG_NUMBER", SqlDbType.NVarChar).Value = bagNumber;
                    da.SelectCommand.Parameters.Add("@MERGE_NUMBER", SqlDbType.NVarChar).Value = dlv_Inv;
                }
                //海運用bagNumber
                else
                {
                    da.SelectCommand.CommandText = "SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where MERGE_NUMBER=@MERGE_NUMBER ";
                    da.SelectCommand.Parameters.Add("@MERGE_NUMBER", SqlDbType.NVarChar).Value = bagNumber;
                }
                da.Fill(dt);
            }

            //空運用袋號查稅金編號
            if (dt.Rows.Count == 0 && original.ToUpper() == "ETL")
            {
                using (SqlDataAdapter da = new SqlDataAdapter("SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where MERGE_NUMBER=@BAG_NUMBER ", conn))
                {
                    da.SelectCommand.Parameters.Add("@BAG_NUMBER", SqlDbType.NVarChar).Value = bagNumber;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 查詢掃貨上車時間、人員
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <returns></returns>
        public DataTable GetPdtScanCargoUpload(string bagNumber, string dlv_Inv)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("select top 1 b.TransName,a.CarNo,a.UploadTime,a.UploadOpe from [jetf].[dbo].[PdtScanCargoUpload] a ");
            sb.Append("join [jetf].[dbo].[PdtTrans] b on a.TransNo=b.TransNo ");
            sb.Append("where a.Data=@bagNumber ");
            if (!string.IsNullOrEmpty(dlv_Inv))
            {
                sb.Append(" or Data=@dlv_Inv ");
            }


            sb.Append(" order by a.UploadTime desc ");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@bagNumber", SqlDbType.NVarChar).Value = bagNumber;
                da.SelectCommand.Parameters.Add("@dlv_Inv", SqlDbType.NVarChar).Value = dlv_Inv;
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 取得錯單類別
        /// </summary>
        /// <returns></returns>
        public DataTable GetErrorReason(string original, string mainNumber, string bagNumber, string dlv_Inv)
        {
            //[DATA_CENTER].[dbo].[ETL_PLINK_ERROR] 空運
            //[jetf].[dbo].[SEA_BAGNO_UPLOAD] 海運
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            if (original.ToUpper() == "ETL")
            {
                sb.Append("select REASON from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] ");
                sb.Append("where MAWB=@MainNumber and HAWB=@Dlv_Inv ");
                sb.Append("union ");
                sb.Append("select REASON from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] ");
                sb.Append("where MAWB=@MainNumber and BAG_NO=@BagNumber and HAWB='' ");
            }
            else
            {
                sb.Append("select MESSAGE as 'REASON' from [jetf].[dbo].[SEA_BAGNO_UPLOAD] ");
                sb.Append("where MAINNUMBER=@MainNumber and BL_NO=@BagNumber ");
                sb.Append("order by CRTDATETIME desc ");
            }
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@MainNumber", SqlDbType.NVarChar).Value = mainNumber;
                da.SelectCommand.Parameters.Add("@BagNumber", SqlDbType.NVarChar).Value = bagNumber;
                da.SelectCommand.Parameters.Add("@Dlv_Inv", SqlDbType.NVarChar).Value = dlv_Inv;
                da.Fill(dt);
            }
            return dt;

        }


        /// <summary>
        /// 取得簽收單路徑
        /// </summary>
        /// <param name="cargoNumber"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public DataTable GetCargo_Sign_Receipt(string cargoNumber)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Cargo_Sign_Receipt] where Jetf_Serial=@Jetf_Serial ", conn))
            {
                da.SelectCommand.Parameters.Add("@Jetf_Serial", SqlDbType.NVarChar).Value = cargoNumber;
                da.Fill(dt);
            }
            dt.Dispose();
            return dt;
        }
    }
}
