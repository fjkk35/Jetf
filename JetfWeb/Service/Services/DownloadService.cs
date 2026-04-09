using Microsoft.VisualBasic;
using Service.EnumTax;
using Service.Models;
using Service.Services.Tax;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.Services
{
    public class DownloadService : _BaseService
    {
        private TaxService _taxService = new TaxService();
        private CustomerService customerService = new CustomerService();
      
        /// <summary>
        /// 更新菜鳥海運、空運，稅金方式P
        /// </summary>
        /// <returns></returns>
        public ResponseModel UpdateCainiaoTaxEdit() {

            ResponseModel resopnse = new ResponseModel();
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_Update_CainiaoTaxEdit]", conn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt);
            }

            resopnse.status = dt.Rows[0]["Status"].ToString();
            resopnse.msg = dt.Rows[0]["Message"].ToString();

            return resopnse;
        }

        public DataTableModel SeaReport(string date, string taxType, string include_Tax)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                if (include_Tax == "")
                {
                    sb.Append("select isnull(b.CUST_NAME,a.CUSTOMER) as CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TAX1,a.TAX2,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM from jetf.dbo.FEE_MASTER a ");
                    sb.Append("left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
                    sb.Append("left join jetf.dbo.customer_master c on a.CUSTOMER=c.CUST_ID and a.DLV_COM=c.TRANS_NAME and c.TRAN_TYPE='海運' ");
                    sb.Append("where DATADATE=@DATADATE and (a.INCLUDE_TAX is null or a.INCLUDE_TAX='') and ");
                    sb.Append("[SOURCE]=@SOURCE ");

                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = Convert.ToDateTime(date).ToString("yyyyMMdd");
                        da.SelectCommand.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = taxType;
                        da.Fill(dt);
                    }
                }
                else if (include_Tax == "D" || include_Tax == "C")
                {
                    sb.Append("select isnull(b.CUST_NAME,a.CUSTOMER) as CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TAX1,a.TAX2,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM from jetf.dbo.FEE_MASTER a ");
                    sb.Append("left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
                    sb.Append("left join jetf.dbo.customer_master c on a.CUSTOMER=c.CUST_ID and a.DLV_COM=c.TRANS_NAME and c.TRAN_TYPE='海運' ");
                    sb.Append("where DATADATE=@DATADATE and a.INCLUDE_TAX=@INCLUDE_TAX and ");
                    sb.Append("[SOURCE]=@SOURCE ");

                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = Convert.ToDateTime(date).ToString("yyyyMMdd");
                        da.SelectCommand.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = taxType;
                        da.SelectCommand.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = include_Tax;
                        da.Fill(dt);
                    }
                }
                else
                {
                    sb.Append(" select b.CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM from jetf.dbo.FEE_MASTER a ");
                    sb.Append(" left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
                    sb.Append(" left join jetf.dbo.customer_master c on a.CUSTOMER=c.CUST_ID and a.DLV_COM=c.TRANS_NAME and c.TRAN_TYPE='海運' ");
                    sb.Append(" where DATADATE=@DATADATE and a.INCLUDE_TAX = @INCLUDE_TAX and ");
                    sb.Append(" [SOURCE]=@SOURCE and c.COMPANY='新竹物流' and Download='1' ");
                    sb.Append(" union all ");
                    sb.Append(" select isnull(b.CUST_NAME,a.CUSTOMER) as CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM  from jetf.dbo.FEE_MASTER a ");
                    sb.Append(" left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
                    sb.Append(" where DATADATE=@DATADATE and a.INCLUDE_TAX = @INCLUDE_TAX and [SOURCE]=@SOURCE and SOURCE_TYPE='2' ");
                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = Convert.ToDateTime(date).ToString("yyyyMMdd");
                        da.SelectCommand.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = taxType;
                        da.SelectCommand.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = include_Tax;
                        da.Fill(dt);
                    }
                }

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
        /// G類稅金調整明細表
        /// </summary>
        /// <param name="sdate"></param>
        /// <param name="edate"></param>
        /// <returns></returns>
        public DataTableModel SeaModifyGReport(string sdate, string edate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("select MODIFY_DATADATE,DATADATE,a.SOURCE,isnull(b.CUST_NAME,a.CUSTOMER) as CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TAX1,a.TAX2,a.CCFEE,a.COD,a.FEE,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM,a.MEMO from jetf.dbo.FEE_MASTER_MODIFY_G a ");
                sb.Append("left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
                sb.Append("where MODIFY_DATADATE between @sdate and @edate ");
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate;
                    da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate;
                    da.Fill(dt);
                }

                //排序
                DataView dv = dt.DefaultView;
                dv.Sort = "MODIFY_DATADATE,DLV_INV,MEMO";
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
        /// 海快TPCT及TIPC稅金調整明細表
        /// </summary>
        /// <param name="sdate"></param>
        /// <param name="edate"></param>
        /// <returns></returns>
        public DataTableModel SeaModifyReport(string sdate, string edate)
        {
            DataTableModel dataTableModel = new DataTableModel();
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                sb.Append("select MODIFY_DATADATE, DATA_TYPE, MAIN_NUMBER, BAG_NUMBER, MERGE_NUMBER, TAX_NUMBER, TAX_BASE, TAX_AMOUNT, FREQ_SIGN, STATUS, MODIFY_SEQ, MODIFY_FILE, MODIFY_TIME,JETF_SERIAL from jetf.dbo.FEE_MASTER_MODIFY ");
                sb.Append("where MODIFY_DATADATE between @sdate and @edate ");
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate;
                    da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate;
                    da.Fill(dt);
                }

                //排序
                DataView dv = dt.DefaultView;
                dv.Sort = "MODIFY_DATADATE,DATA_TYPE";
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

        public DataTable GetFee_Master_Etl(string sdate, string edate, string timeBetween)
        {
            DataRow[] dr_Data,dr_Customer_Special;
            DateTime in_date, out_date;
            int tax1, tax2, fee, cod, tax_base;
            string recphone;
            StringBuilder sb = new StringBuilder();
            sb.Append("with cte as ");
            sb.Append("( ");
            sb.Append("  select ROW_NUMBER() OVER (PARTITION BY TAX_NUMBER ORDER BY TRACKINGNO ) as ROW_ID,* from ( ");
            sb.Append("	  select  ");
            sb.Append("	  a.DATA_TYPE,a.CLEARANCE_TYPE,a.CLEARANCE_NUMBER,a.SIGN_IN_TIME,a.SIGN_OUT_TIME,  ");
            sb.Append("	  isnull(b.TAX_NUMBER,c.TAX_NUMBER) as TAX_NUMBER,isnull(b.BAG_NUMBER,c.BAG_NUMBER) as BAG_NUMBER,isnull(b.MAIN_NUMBER,c.MAIN_NUMBER) as MAIN_NUMBER,isnull(b.TAX_AMOUNT,c.TAX_AMOUNT) as TAX_AMOUNT,isnull(b.TAX_BASE,c.TAX_BASE) as TAX_BASE,  ");
            sb.Append("	  isnull(isnull(d.BAGNO,e.BAGNO),a.BAG_NUMBER) as BAGNO,isnull(d.DESPATCHNO,e.DESPATCHNO) as DESPATCHNO,isnull(d.CC,e.CC) as CC,isnull(d.CLEARANCEWAREHOUSING,e.CLEARANCEWAREHOUSING) as CLEARANCEWAREHOUSING,isnull(d.RECIPIENT,e.RECIPIENT) as RECIPIENT,isnull(d.RECPHONE,e.RECPHONE) as RECPHONE,isnull(d.RECADDRESS,e.RECADDRESS) as RECADDRESS,isnull(d.TRACKINGUB,e.TRACKINGUB) as TRACKINGNO,isnull(d.DELIVERYNO,e.DELIVERYNO) as DELIVERYNO,isnull(d.TRANS_TAXPAYMENT,e.TRANS_TAXPAYMENT) as TRANS_TAXPAYMENT,isnull(d.ECM,e.ECM) as ECM  ");
            sb.Append("	  from DATA_CENTER.dbo.CLEARANCE_INFO a  ");
            sb.Append("	  left join DATA_CENTER.dbo.ETL_TACT_TAX b on a.MAIN_NUMBER=b.MAIN_NUMBER and a.BAG_NUMBER=b.BAG_NUMBER  ");
            sb.Append("	  left join DATA_CENTER.dbo.ETL_TACT_TAX c on a.MAIN_NUMBER=c.MAIN_NUMBER and a.MERGE_NUMBER=c.BAG_NUMBER  ");
            sb.Append("	  left join DATA_CENTER.dbo.ORIGINALLIST d on isnull(b.MAIN_NUMBER,c.MAIN_NUMBER)=d.MAINNUMBER and isnull(b.BAG_NUMBER,c.BAG_NUMBER)=d.BAGNO  ");
            sb.Append("	  left join DATA_CENTER.dbo.ORIGINALLIST e on isnull(b.MAIN_NUMBER,c.MAIN_NUMBER)=e.MAINNUMBER and isnull(b.BAG_NUMBER,c.BAG_NUMBER)=e.TRACKINGUB  ");
            sb.Append("	  where a.DATA_TYPE = 'tact' and a.SIGN_OUT_TIME between @sdate and @edate ");
            sb.Append("  ) a where TAX_NUMBER>'' ");
            sb.Append(") ");
            sb.Append("select ");
            sb.Append("DATA_TYPE,CLEARANCE_TYPE,CLEARANCE_NUMBER,SIGN_IN_TIME,SIGN_OUT_TIME, ");
            sb.Append("TAX_NUMBER,BAG_NUMBER,MAIN_NUMBER,TAX_AMOUNT,TAX_BASE,ECM, ");
            sb.Append("BAGNO,DESPATCHNO,CC,CLEARANCEWAREHOUSING,RECIPIENT,RECPHONE,RECADDRESS,isnull(TRACKINGNO,BAG_NUMBER) as TRACKINGNO,DELIVERYNO,TRANS_TAXPAYMENT, ");
            sb.Append("b.COD_FEE,b.INCLUDE_TAX,b.COMPANY,b.ISCAINIAOP ");
            sb.Append("from cte ");
            sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',cte.DESPATCHNO,5)=b.CUST_ID  and cte.TRANS_TAXPAYMENT=b.TRANS_NO and b.TRAN_TYPE='空運' ");
            sb.Append("where  ROW_ID='1' ");

            StringBuilder sb_Frz = new StringBuilder();
            sb_Frz.Append("with cte as ");
            sb_Frz.Append("( ");
            sb_Frz.Append("  select ROW_NUMBER() OVER (PARTITION BY TAX_NUMBER ORDER BY TRACKINGNO ) as ROW_ID,* from ( ");
            sb_Frz.Append("	  select  ");
            sb_Frz.Append("	  a.DATA_TYPE,a.CLEARANCE_TYPE,a.CLEARANCE_NUMBER,a.SIGN_IN_TIME,a.SIGN_OUT_TIME,  ");
            sb_Frz.Append("	  isnull(b.TAX_NUMBER,c.TAX_NUMBER) as TAX_NUMBER,isnull(b.BAG_NUMBER,c.BAG_NUMBER) as BAG_NUMBER,isnull(b.MAIN_NUMBER,c.MAIN_NUMBER) as MAIN_NUMBER,isnull(b.TAX_AMOUNT,c.TAX_AMOUNT) as TAX_AMOUNT,isnull(b.TAX_BASE,c.TAX_BASE) as TAX_BASE,  ");
            sb_Frz.Append("	  isnull(isnull(d.BAGNO,e.BAGNO),a.BAG_NUMBER) as BAGNO,isnull(d.DESPATCHNO,e.DESPATCHNO) as DESPATCHNO,isnull(d.CC,e.CC) as CC,isnull(d.CLEARANCEWAREHOUSING,e.CLEARANCEWAREHOUSING) as CLEARANCEWAREHOUSING,isnull(d.RECIPIENT,e.RECIPIENT) as RECIPIENT,isnull(d.RECPHONE,e.RECPHONE) as RECPHONE,isnull(d.RECADDRESS,e.RECADDRESS) as RECADDRESS,isnull(d.TRACKINGUB,e.TRACKINGUB) as TRACKINGNO,isnull(d.DELIVERYNO,e.DELIVERYNO) as DELIVERYNO,isnull(d.TRANS_TAXPAYMENT,e.TRANS_TAXPAYMENT) as TRANS_TAXPAYMENT,isnull(d.ECM,e.ECM) as ECM ");
            sb_Frz.Append("	  from DATA_CENTER.dbo.CLEARANCE_INFO a  ");
            sb_Frz.Append("	  left join DATA_CENTER.dbo.ETL_FTZ_TAX b on a.MAIN_NUMBER=b.MAIN_NUMBER and a.BAG_NUMBER=b.BAG_NUMBER  ");
            sb_Frz.Append("	  left join DATA_CENTER.dbo.ETL_FTZ_TAX c on a.MAIN_NUMBER=c.MAIN_NUMBER and a.MERGE_NUMBER=c.BAG_NUMBER  ");
            sb_Frz.Append("	  left join DATA_CENTER.dbo.ORIGINALLIST d on isnull(b.MAIN_NUMBER,c.MAIN_NUMBER)=d.MAINNUMBER and isnull(b.BAG_NUMBER,c.BAG_NUMBER)=d.BAGNO  ");
            sb_Frz.Append("	  left join DATA_CENTER.dbo.ORIGINALLIST e on isnull(b.MAIN_NUMBER,c.MAIN_NUMBER)=e.MAINNUMBER and isnull(b.BAG_NUMBER,c.BAG_NUMBER)=e.TRACKINGUB  ");
            sb_Frz.Append("	  where a.DATA_TYPE = 'ftz' and a.SIGN_OUT_TIME between @sdate and @edate ");
            sb_Frz.Append("  ) a where TAX_NUMBER>'' ");
            sb_Frz.Append(") ");
            sb_Frz.Append("select ");
            sb_Frz.Append("DATA_TYPE,CLEARANCE_TYPE,CLEARANCE_NUMBER,SIGN_IN_TIME,SIGN_OUT_TIME, ");
            sb_Frz.Append("TAX_NUMBER,BAG_NUMBER,MAIN_NUMBER,TAX_AMOUNT,TAX_BASE,ECM, ");
            sb_Frz.Append("BAGNO,DESPATCHNO,CC,CLEARANCEWAREHOUSING,RECIPIENT,RECPHONE,RECADDRESS,isnull(TRACKINGNO,BAG_NUMBER) as TRACKINGNO,DELIVERYNO,TRANS_TAXPAYMENT, ");
            sb_Frz.Append("b.COD_FEE,b.INCLUDE_TAX,b.COMPANY,b.ISCAINIAOP ");
            sb_Frz.Append("from cte ");
            sb_Frz.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',cte.DESPATCHNO,5)=b.CUST_ID  and cte.TRANS_TAXPAYMENT=b.TRANS_NO and b.TRAN_TYPE='空運' ");
            sb_Frz.Append("where  ROW_ID='1' ");

            DataTable dt_Data = new DataTable();
            //華儲
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate;
                da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate;
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt_Data);
            }

            //遠雄
            using (SqlDataAdapter da = new SqlDataAdapter(sb_Frz.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate;
                da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate;
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt_Data);
            }


            DataRow dr_Fee_Master;
            DataTable dt_Fee_Master = new DataTable();
            dt_Fee_Master.Columns.Add("source", typeof(string));//CLEARANCE_INFO.DATA_TYPE
            dt_Fee_Master.Columns.Add("type", typeof(string));//CLEARANCE_INFO.CLEARANCE_TYPE
            dt_Fee_Master.Columns.Add("customer", typeof(string));//ORIGINALLIST.DESPATCHNO
            dt_Fee_Master.Columns.Add("main_number", typeof(string));//ETL_TACT_TAX.MAIN_NUMBER
            dt_Fee_Master.Columns.Add("trackingno", typeof(string));//ORIGINALLIST.TRACKINGNO
            dt_Fee_Master.Columns.Add("clearance_number", typeof(string));//CLEARANCE_INFO.CLEARANCE_NUMBER
            dt_Fee_Master.Columns.Add("bag_number", typeof(string));//ETL_TACT_TAX.BAG_NUMBER
            dt_Fee_Master.Columns.Add("combine", typeof(string));//併單
            dt_Fee_Master.Columns.Add("in_date", typeof(string));//CLEARANCE_INFO.SIGN_IN_TIME
            dt_Fee_Master.Columns.Add("in_datetime", typeof(string));//CLEARANCE_INFO.SIGN_IN_TIME
            dt_Fee_Master.Columns.Add("out_datetime", typeof(string));//CLEARANCE_INFO.SIGN_OUT_TIME
            dt_Fee_Master.Columns.Add("tax_base", typeof(string));//ETL_TACT_TAX.TAX_BASE
            dt_Fee_Master.Columns.Add("tax1", typeof(string));//ETL_TACT_TAX.TAX_AMOUNT
            dt_Fee_Master.Columns.Add("tax2", typeof(string));
            dt_Fee_Master.Columns.Add("dlv_com", typeof(string));//ORIGINALLIST.TRANS_TAXPAYMENT
            dt_Fee_Master.Columns.Add("tax_number", typeof(string));//
            dt_Fee_Master.Columns.Add("cod", typeof(string));//ORIGINALLIST.CC
            dt_Fee_Master.Columns.Add("fee", typeof(string));
            dt_Fee_Master.Columns.Add("include_tax", typeof(string));//customer_master.INCLUDE_TAX
            dt_Fee_Master.Columns.Add("recipient", typeof(string));
            dt_Fee_Master.Columns.Add("recphone", typeof(string));
            dt_Fee_Master.Columns.Add("recaddress", typeof(string));
            dt_Fee_Master.Columns.Add("recid", typeof(string));
            dt_Fee_Master.Columns.Add("to_dlv_cod", typeof(string));
            dt_Fee_Master.Columns.Add("dlv_inv", typeof(string));
            dt_Fee_Master.Columns.Add("arrival", typeof(string)); //ECM 菜鳥LP單號
            dt_Fee_Master.Columns.Add("Trans_Cod", typeof(string));
            dt_Fee_Master.Columns.Add("Customer_Cod", typeof(string));


            //特殊客戶
            DataTable dt_Customer_Special = customerService.GetCustomer_Special("空運");

            if (dt_Data.Rows.Count > 0)
            {
                var dt_Group = from t in dt_Data.AsEnumerable()
                               group t by new { TRACKINGNO = t.Field<string>("TRACKINGNO") } into g
                               select new
                               {
                                   TRACKINGNO = g.Key.TRACKINGNO
                               };

                foreach (var item in dt_Group)
                {
                    dr_Fee_Master = dt_Fee_Master.NewRow();
                    //找最後出倉日的
                    dr_Data = dt_Data.Select($"TRACKINGNO='{item.TRACKINGNO}'", "SIGN_OUT_TIME desc");

                    //來源
                    dr_Fee_Master["source"] = dr_Data[0]["DATA_TYPE"].ToString();
                    //報關類型
                    dr_Fee_Master["type"] = dr_Data[0]["CLEARANCE_TYPE"].ToString();
                    //稅單號碼
                    dr_Fee_Master["tax_number"] = dr_Data[0]["TAX_NUMBER"].ToString();
                    //主提單號
                    dr_Fee_Master["main_number"] = dr_Data[0]["MAIN_NUMBER"].ToString();
                    //分提單號
                    dr_Fee_Master["trackingno"] = dr_Data[0]["TRACKINGNO"].ToString();
                    //物流單號
                    //dr_Fee_Master["dlv_inv"] = Regex.Replace(dr_Data[0]["DELIVERYNO"].ToString(), @"[^a-zA-Z0-9]", ""); //只留英文、數字
                    dr_Fee_Master["dlv_inv"] = dr_Data[0]["DELIVERYNO"].ToString();
                    //報單號碼
                    dr_Fee_Master["clearance_number"] = dr_Data[0]["CLEARANCE_NUMBER"].ToString();
                    //清關袋號
                    dr_Fee_Master["bag_number"] = dr_Data[0]["BAGNO"].ToString();
                    //清關袋號
                    if (DateTime.TryParse(dr_Data[0]["SIGN_IN_TIME"].ToString(), out in_date))
                    {
                        dr_Fee_Master["in_date"] = in_date.ToString("yyyyMMdd");
                        //進倉時間
                        dr_Fee_Master["in_datetime"] = in_date.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    //出倉時間
                    if (DateTime.TryParse(dr_Data[0]["SIGN_OUT_TIME"].ToString(), out out_date))
                    {
                        dr_Fee_Master["out_datetime"] = out_date.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    //是否包稅
                    dr_Fee_Master["include_tax"] = dr_Data[0]["INCLUDE_TAX"].ToString();

                    //納稅義務人
                    dr_Fee_Master["recipient"] = dr_Data[0]["RECIPIENT"].ToString();
                    //電話
                    dr_Fee_Master["recphone"] = Strings.StrConv(dr_Data[0]["RECPHONE"].ToString(), VbStrConv.Narrow, 1028);
                    //地址
                    dr_Fee_Master["RECADDRESS"] = dr_Data[0]["RECADDRESS"].ToString();
                    //客戶名稱
                    dr_Fee_Master["customer"] = dr_Data[0]["DESPATCHNO"].ToString();
                    //派件公司
                    dr_Fee_Master["dlv_com"] = dr_Data[0]["TRANS_TAXPAYMENT"].ToString();

                    //菜鳥LP單號
                    dr_Fee_Master["arrival"] = dr_Data[0]["ECM"].ToString();

                    tax_base = 0;
                    tax1 = 0;
                    tax2 = 0;
                    cod = 0;
                    fee = 0;
                    Int32.TryParse(dr_Data[0]["TAX_BASE"].ToString(), out tax_base);
                    Int32.TryParse(dr_Data[0]["TAX_AMOUNT"].ToString(), out tax1);
                    Int32.TryParse(dr_Data[0]["CC"].ToString(), out cod);
                    Int32.TryParse(dr_Data[0]["COD_FEE"].ToString(), out fee);

                    //稅基
                    dr_Fee_Master["tax_base"] = tax_base;
                    //稅金
                    dr_Fee_Master["tax1"] = tax1;
                    //代收款
                    dr_Fee_Master["cod"] = cod;
                    //手續費
                    dr_Fee_Master["fee"] = fee;

                    //兩筆稅金
                    if (dr_Data.Length > 1)
                    {
                        dr_Fee_Master["combine"] = "Y";
                        tax2 = 0;
                        for (int i = 1; i < dr_Data.Length; i++)
                        {
                            tax2 += Convert.ToInt32(dr_Data[i]["TAX_AMOUNT"]);
                        }
                        dr_Fee_Master["tax2"] = tax2.ToString();
                    }

                    //提供派件公司代收貨款金額
                    if (dr_Data[0]["INCLUDE_TAX"].ToString() == "Y")
                    {
                        var taxData = _taxService.GetTaxY(dr_Fee_Master);

                        dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                        dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                        dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                    }
                    //菜鳥尊榮服務
                    else if (!string.IsNullOrEmpty(dr_Data[0]["ISCAINIAOP"].ToString()) && Convert.ToBoolean(dr_Data[0]["ISCAINIAOP"]) == true)
                    {
                        var taxData = _taxService.GetTaxP(dr_Fee_Master);
                        //如果要跟派件公司收錢，稅金方式改成N(不包稅)
                        dr_Fee_Master["include_tax"] = taxData.TransCod > 0 ? "N" : dr_Fee_Master["include_tax"];
                        //手續費
                        dr_Fee_Master["fee"] = taxData.TransCod > 0 ? dr_Fee_Master["fee"] : 0;
                        dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                        dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                        dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                    }
                    //稅金D
                    else if (dr_Data[0]["INCLUDE_TAX"].ToString() == "D")
                    {
                        var taxData = _taxService.GetTaxD(dr_Fee_Master);
                        dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                        dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                        dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                    }
                    //特殊客戶
                    else if (_taxService.IsEtlSpecial(dt_Customer_Special, dr_Data[0]["company"].ToString(), dr_Fee_Master["recphone"].ToString().Trim()))
                    {
                        var taxData = _taxService.GetTaxD(dr_Fee_Master);
                        dr_Fee_Master["include_tax"] = "D";
                        dr_Fee_Master["fee"] = "0";
                        dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                        dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                        dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                    }
                    else if (dr_Data[0]["INCLUDE_TAX"].ToString() == "C")
                    {
                        var taxData = _taxService.GetTaxC(dr_Fee_Master);
                        //是否包稅-C客戶付款
                        dr_Fee_Master["fee"] = "0";
                        dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                        dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                        dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                    }
                    else
                    {
                        var taxData = _taxService.GetTaxN(dr_Fee_Master);
                        dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                        dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                        dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                    }
                    dt_Fee_Master.Rows.Add(dr_Fee_Master);
                }
            }
            return dt_Fee_Master;
        }

        ResponseModel InsertFee_Master_Etl(DataTable dt_Fee_Master, string dataDate, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                //新增FEE_MASTER
                StringBuilder sb = new StringBuilder();
                sb.Append("declare @Select_DLV_REMIT_CODE nvarchar(2) ");
                sb.Append("select @Select_DLV_REMIT_CODE=DLV_REMIT_CODE from jetf.dbo.FEE_MASTER where MAIN_NUMBER=@MAIN_NUMBER and TRACKINGNO=@TRACKINGNO and SOURCE_TYPE=@SOURCE_TYPE ");
                sb.Append("if @@ROWCOUNT>0 ");
                sb.Append("begin ");
                sb.Append("     if @Select_DLV_REMIT_CODE is null or @Select_DLV_REMIT_CODE<>'Y' ");
                sb.Append("     begin ");
                sb.Append("	            insert jetf.dbo.FEE_MASTER_LOG([ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD]) ");
                sb.Append("	            select [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER],getdate() as [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD] from jetf.dbo.FEE_MASTER where MAIN_NUMBER=@MAIN_NUMBER and TRACKINGNO=@TRACKINGNO ");
                sb.Append("	            update jetf.dbo.FEE_MASTER set DATADATE=@DATADATE,[SOURCE]=@SOURCE,[TYPE]=@TYPE,CUSTOMER=@CUSTOMER,MAIN_NUMBER=@MAIN_NUMBER,TRACKINGNO=@TRACKINGNO,CLEARANCE_NUMBER=@CLEARANCE_NUMBER,COMBINE=@COMBINE,IN_DATE=@IN_DATE,IN_DATETIME=@IN_DATETIME,OUT_DATETIME=@OUT_DATETIME,TAX_BASE=@TAX_BASE,TAX1=@TAX1,TAX2=@TAX2,DLV_COM=@DLV_COM,TAX_NUMBER=@TAX_NUMBER,FEE=@FEE,INCLUDE_TAX=@INCLUDE_TAX,RECIPIENT=@RECIPIENT,RECPHONE=@RECPHONE,RECADDRESS=@RECADDRESS,RECID=@RECID,COD=@COD,TO_DLV_COD=@TO_DLV_COD,DLV_INV=@DLV_INV,BAG_NUMBER=@BAG_NUMBER,ARRIVAL=@ARRIVAL,CUSTOMER_COD=@CUSTOMER_COD,TRANS_COD=@TRANS_COD,UPDATEDATE=getdate(),RECORD_FEE_MASTER='0' ");
                sb.Append("         	where MAIN_NUMBER=@MAIN_NUMBER and TRACKINGNO=@TRACKINGNO and SOURCE_TYPE=@SOURCE_TYPE ");
                sb.Append("     end ");
                sb.Append("end ");
                sb.Append("else ");
                sb.Append("begin ");
                sb.Append("insert [jetf].[dbo].[FEE_MASTER](DATADATE,SOURCE,SOURCE_TYPE,TYPE, CUSTOMER, MAIN_NUMBER, TRACKINGNO, CLEARANCE_NUMBER,COMBINE, IN_DATE, IN_DATETIME, OUT_DATETIME,TAX_BASE,TAX1, TAX2, DLV_COM,TAX_NUMBER,FEE,INCLUDE_TAX,RECIPIENT,RECPHONE,RECADDRESS,RECID,COD,TO_DLV_COD,DLV_INV,BAG_NUMBER,ARRIVAL,CUSTOMER_COD,TRANS_COD) ");
                sb.Append("values(@DATADATE,@SOURCE,@SOURCE_TYPE,@TYPE,@CUSTOMER,@MAIN_NUMBER,@TRACKINGNO,@CLEARANCE_NUMBER,@COMBINE,@IN_DATE,@IN_DATETIME,@OUT_DATETIME,@TAX_BASE,@TAX1,@TAX2,@DLV_COM,@TAX_NUMBER,@FEE,@INCLUDE_TAX,@RECIPIENT,@RECPHONE,@RECADDRESS,@RECID,@COD,@TO_DLV_COD,@DLV_INV,@BAG_NUMBER,@ARRIVAL,@CUSTOMER_COD,@TRANS_COD) ");
                sb.Append("end ");

                try
                {
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Transaction = tran;
                        for (int i = 0; i < dt_Fee_Master.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["source"] ?? DBNull.Value;
                            cmd.Parameters.Add("@SOURCE_TYPE", SqlDbType.NVarChar).Value = "3";
                            cmd.Parameters.Add("@TYPE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["type"] ?? DBNull.Value;
                            cmd.Parameters.Add("@CUSTOMER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["customer"] ?? DBNull.Value;
                            cmd.Parameters.Add("@MAIN_NUMBER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["main_number"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["trackingno"] ?? DBNull.Value;
                            cmd.Parameters.Add("@CLEARANCE_NUMBER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["clearance_number"] ?? DBNull.Value;
                            cmd.Parameters.Add("@BAG_NUMBER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["bag_number"] ?? DBNull.Value;
                            cmd.Parameters.Add("@COMBINE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["combine"] ?? DBNull.Value;
                            cmd.Parameters.Add("@IN_DATE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["in_date"] ?? DBNull.Value;
                            cmd.Parameters.Add("@IN_DATETIME", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["in_datetime"] ?? DBNull.Value;
                            cmd.Parameters.Add("@OUT_DATETIME", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["out_datetime"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX_BASE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax_base"].ToString() ?? "0";
                            cmd.Parameters.Add("@TAX1", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax1"].ToString() ?? "0";
                            cmd.Parameters.Add("@TAX2", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax2"].ToString() ?? "0";
                            cmd.Parameters.Add("@DLV_COM", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["dlv_com"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX_NUMBER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax_number"] ?? DBNull.Value;
                            cmd.Parameters.Add("@COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["cod"].ToString() ?? "0";
                            cmd.Parameters.Add("@FEE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["fee"].ToString() ?? "0";
                            cmd.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["include_tax"] ?? DBNull.Value;
                            cmd.Parameters.Add("@RECIPIENT", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recipient"] ?? DBNull.Value;
                            cmd.Parameters.Add("@RECPHONE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recphone"] ?? DBNull.Value;
                            cmd.Parameters.Add("@RECADDRESS", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recaddress"] ?? DBNull.Value;
                            cmd.Parameters.Add("@RECID", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recid"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TO_DLV_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["to_dlv_cod"] ?? DBNull.Value;
                            cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["dlv_inv"] ?? DBNull.Value;
                            cmd.Parameters.Add("@ARRIVAL", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["arrival"] ?? DBNull.Value;
                            cmd.Parameters.Add("@CUSTOMER_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["Customer_Cod"] ?? "0";
                            cmd.Parameters.Add("@TRANS_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["Trans_Cod"] ?? "0";
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
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

        public DataTableModel EtlReport(string date, string timeBetween, string sTime, string eTime, string company, string include_Tax, string userId)
        {
            DataTableModel dataTableModel = new DataTableModel();

            try
            {
                DateTime sdate, edate;
                sTime = sTime.Insert(2, ":");
                eTime = eTime.Insert(2, ":");
                string dataDate = Convert.ToDateTime(date).ToString("yyyyMMdd");
                DateTime.TryParse($"{date} {sTime}:00", out sdate);
                DateTime.TryParse($"{date} {eTime}:00", out edate);

                if (timeBetween == "1")
                {
                    //時間區間選1，開始AddDays-1
                    sdate = sdate.AddDays(-1);
                }

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                DataTable dt = new DataTable();
                StringBuilder sb = new StringBuilder();
                if (include_Tax == "")
                {
                    sb.Append("select BAG_NUMBER,TRACKINGNO,a.TAX1,a.TAX2,a.FEE,a.COD,TO_DLV_COD,RECIPIENT,RECPHONE,a.dlv_com as TRANS_NAME,OUT_DATETIME,a.INCLUDE_TAX,a.DLV_INV from jetf.dbo.FEE_MASTER a ");
                    sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO ");
                    sb.Append("where [SOURCE] in('tact','ftz') and OUT_DATETIME between @sdate and @edate and (a.INCLUDE_TAX is null or a.INCLUDE_TAX='') ");
                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.CommandTimeout = 300;
                        da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate.ToString("yyyy-MM-dd HH:mm:ss");
                        da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate.ToString("yyyy-MM-dd HH:mm:ss");
                        da.Fill(dt);
                    }
                }
                else if (include_Tax == "D" || include_Tax == "C")
                {
                    sb.Append("select BAG_NUMBER,TRACKINGNO,a.TAX1,a.TAX2,a.FEE,a.COD,TO_DLV_COD,RECIPIENT,RECPHONE,b.TRANS_NAME,OUT_DATETIME,a.INCLUDE_TAX,a.DLV_INV from jetf.dbo.FEE_MASTER a ");
                    sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO ");
                    sb.Append("where [SOURCE] in('tact','ftz') and OUT_DATETIME between @sdate and @edate and a.INCLUDE_TAX=@INCLUDE_TAX ");

                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.CommandTimeout = 300;
                        da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate.ToString("yyyy-MM-dd HH:mm:ss");
                        da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate.ToString("yyyy-MM-dd HH:mm:ss");
                        da.SelectCommand.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = include_Tax;
                        da.Fill(dt);
                    }
                }
                else
                {
                    sb.Append("select BAG_NUMBER,TRACKINGNO,TO_DLV_COD,RECIPIENT,RECPHONE,b.TRANS_NAME,OUT_DATETIME,a.INCLUDE_TAX,a.DLV_INV from jetf.dbo.FEE_MASTER a ");
                    sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO ");
                    sb.Append("where [SOURCE] in('tact','ftz') and b.COMPANY=@COMPANY and OUT_DATETIME between @sdate and @edate and a.INCLUDE_TAX=@INCLUDE_TAX ");

                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.CommandTimeout = 300;
                        da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate.ToString("yyyy-MM-dd HH:mm:ss");
                        da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate.ToString("yyyy-MM-dd HH:mm:ss");
                        da.SelectCommand.Parameters.Add("@COMPANY", SqlDbType.NVarChar).Value = company;
                        da.SelectCommand.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = include_Tax;
                        da.Fill(dt);
                    }

                    //同一個檔案多+新瑞宅配
                    if (timeBetween == "3")
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                        {
                            da.SelectCommand.CommandTimeout = 300;
                            da.SelectCommand.Parameters.Add("@sdate", SqlDbType.NVarChar).Value = sdate.ToString("yyyy-MM-dd HH:mm:ss");
                            da.SelectCommand.Parameters.Add("@edate", SqlDbType.NVarChar).Value = edate.ToString("yyyy-MM-dd HH:mm:ss");
                            da.SelectCommand.Parameters.Add("@COMPANY", SqlDbType.NVarChar).Value = "新瑞宅配";
                            da.SelectCommand.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = include_Tax;
                            da.Fill(dt);
                        }
                    }
                }
                dataTableModel.status = Status.success;
                dataTableModel.dt = dt;
                conn.Close();
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }

            return dataTableModel;
        }

        public ResponseModel UploadEtl(string date, string timeBetween, string sTime, string eTime, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            DateTime sdate, edate;
            sTime = sTime.Insert(2, ":");
            eTime = eTime.Insert(2, ":");
            string dataDate = Convert.ToDateTime(date).ToString("yyyyMMdd");
            if (DateTime.TryParse($"{date} {sTime}:00", out sdate) && DateTime.TryParse($"{date} {eTime}:00", out edate))
            {
                if (timeBetween == "1")
                {
                    //時間區間選1，開始AddDays-1
                    sdate = sdate.AddDays(-1);
                }
                if (edate > sdate)
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    //更新菜鳥海運、空運，稅金方式P
                    resopnseModel = UpdateCainiaoTaxEdit();
                    if (resopnseModel.status != Status.success)
                    {
                        return resopnseModel;
                    }

                    //取得空運寫入Fee_Master資料
                    DataTable dt_Fee_Master = GetFee_Master_Etl(sdate.ToString("yyyy-MM-dd HH:mm:ss"), edate.ToString("yyyy-MM-dd HH:mm:ss"), timeBetween);
                    //新增Fee_Master資料
                    resopnseModel = InsertFee_Master_Etl(dt_Fee_Master, dataDate, userId);
                    resopnseModel.status = Status.success;
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "結束時間大於開始時間，請確認";
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = "時間區間錯誤，請確認";
            }
            return resopnseModel;
        }


        /// <summary>
        /// 3-3空快稅金-回桃園倉庫明細表
        /// </summary>
        /// <returns></returns>
        public DataTableModel NoIncludeTaxReport(string source, string sDate, string eDate, string user_id)
        {
            DataTableModel dataTableModel = new DataTableModel();
            StringBuilder sb = new StringBuilder();
            //空運
            if (source == "1")
            {
                sb.Append("select DATADATE,SOURCE,TYPE,a.BAG_NUMBER,b.CUSTOMER,a.INCLUDE_TAX,a.TRACKINGNO,COMBINE,TAX1,TAX2,CCFEE,COD,FEE,TO_DLV_COD,b.TRANS_NAME,a.RECIPIENT,a.RECPHONE,IN_DATETIME,OUT_DATETIME,c.DELIVERYNO,c.WEIGHT,c.RECADDRESS from jetf.dbo.FEE_MASTER a ");
                sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO and b.TRAN_TYPE='空運' ");
                sb.Append("left join  DATA_CENTER.dbo.ORIGINALLIST c on a.TRACKINGNO=c.TRACKINGNO ");
                sb.Append("where DATADATE between @sDate and @eDate and (a.INCLUDE_TAX='N' or a.dlv_com='40' or a.dlv_com='41') and SOURCE_TYPE='3' and b.COMPANY not in ('新瑞宅配','新竹物流','圓通自取') ");
            }

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }

            //排序
            DataView dv = dt.DefaultView;
            dv.Sort = "SOURCE,CUSTOMER,DATADATE";
            dt = dv.ToTable();

            dataTableModel.status = Status.success;
            dataTableModel.dt = dt;
            return dataTableModel;
        }

        /// <summary>
        /// 3-4. 稅金總表及明細表
        /// </summary>
        /// <returns></returns>
        public DataTableModel IncludeTaxReport(string source, string sDate, string eDate, string user_id)
        {
            DataTableModel dataTableModel = new DataTableModel();
            StringBuilder sb = new StringBuilder();
            //海運
            if (source == "1")
            {
                sb.Append("select DATADATE,SOURCE,TYPE,a.customer as CUST_ID,a.dlv_com as TRANS_NO,a.ARRIVAL,b.CUST_NAME,CLEARANCE_NUMBER,TRACKINGNO as BAG_NUMBER,DLV_INV as TRACKINGNO,MAIN_NUMBER,TAX_NUMBER,IN_DATETIME,OUT_DATETIME,TAX_BASE,TAX1,TAX2,RECIPIENT,RECPHONE,c.TRANS_NAME,COD,a.INCLUDE_TAX,a.FEE,DLV_INV,a.TAX_PAYER,d.IMPORTER_ID,d.IMPORTER,a.CUSTOMER_COD,a.TRANS_COD,a.TAX_RECID from jetf.dbo.FEE_MASTER a ");
                sb.Append("left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
                sb.Append("left join jetf.dbo.customer_master c on a.CUSTOMER=c.CUST_ID and a.DLV_COM=c.TRANS_NAME and c.TRAN_TYPE='海運' ");
                sb.Append("left join DATA_CENTER.dbo.SEA_ORDER_EDIT d on a.TRACKINGNO = d.BL_NO  and a.MAIN_NUMBER=d.MAINNUMBER and ITEM_NO = '1' ");
                sb.Append("where DATADATE between @sDate and @eDate and SOURCE_TYPE='1' ");
            }
            //空運
            else if (source == "3")
            {
                sb.Append("select  DATADATE,SOURCE,TYPE,a.customer as CUST_ID,a.dlv_com as TRANS_NO,a.ARRIVAL,b.CUSTOMER as CUST_NAME,CLEARANCE_NUMBER,BAG_NUMBER,DLV_INV,MAIN_NUMBER,TAX_NUMBER,IN_DATETIME,OUT_DATETIME,TAX_BASE,TAX1,TAX2,a.RECIPIENT,a.RECPHONE,TRANS_NAME,COD,a.INCLUDE_TAX,a.FEE,a.TAX_PAYER,a.TRACKINGNO,c.RECID as IMPORTER_ID,c.RECIPIENT as IMPORTER,a.CUSTOMER_COD,a.TRANS_COD,a.TAX_RECID from jetf.dbo.FEE_MASTER a ");
                sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO and b.TRAN_TYPE='空運' ");
                sb.Append("left join (select distinct TRACKINGNO,RECID,RECIPIENT from DATA_CENTER.dbo.MAKELIST) c on a.TRACKINGNO = c.TRACKINGNO ");
                sb.Append("where DATADATE between @sDate and @eDate and SOURCE_TYPE='3' ");
            }

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }

            DataTable dt_New = new DataTable();
            dt_New = dt.Clone();
            foreach (DataColumn col in dt_New.Columns)
            {
                if (col.ColumnName == "TAX1" || col.ColumnName == "TAX2" || col.ColumnName == "COD")
                {
                    //修改列型別
                    col.DataType = typeof(Int64);
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                DataRow dr = dt_New.NewRow();
                dr["DATADATE"] = row["DATADATE"];
                dr["SOURCE"] = row["SOURCE"];
                dr["TYPE"] = row["TYPE"];
                dr["CUST_ID"] = row["CUST_ID"];
                dr["TRANS_NO"] = row["TRANS_NO"];
                dr["CUST_NAME"] = row["CUST_NAME"];
                dr["CLEARANCE_NUMBER"] = row["CLEARANCE_NUMBER"];
                dr["BAG_NUMBER"] = row["BAG_NUMBER"];
                dr["DLV_INV"] = row["DLV_INV"];
                dr["MAIN_NUMBER"] = row["MAIN_NUMBER"];
                dr["TAX_NUMBER"] = row["TAX_NUMBER"];
                dr["IN_DATETIME"] = row["IN_DATETIME"];
                dr["OUT_DATETIME"] = row["OUT_DATETIME"];
                dr["TAX_BASE"] = row["TAX_BASE"];
                dr["TAX1"] = row["TAX1"];
                dr["TAX2"] = row["TAX2"];
                if (row["TAX_PAYER"].ToString() == "")
                {
                    dr["RECIPIENT"] = row["RECIPIENT"];
                }
                else {
                    dr["RECIPIENT"] = row["TAX_PAYER"];
                }
                dr["RECPHONE"] = row["RECPHONE"];
                dr["TRANS_NAME"] = row["TRANS_NAME"];
                dr["COD"] = row["COD"];
                dr["INCLUDE_TAX"] = row["INCLUDE_TAX"];
                dr["FEE"] = row["FEE"];
                dr["TRACKINGNO"] = row["TRACKINGNO"];
                dr["IMPORTER_ID"] = row["IMPORTER_ID"];
                dr["IMPORTER"] = row["IMPORTER"];
                dr["ARRIVAL"] = row["ARRIVAL"];
                dr["CUSTOMER_COD"] = row["CUSTOMER_COD"];
                dr["TRANS_COD"] = row["TRANS_COD"];
                dr["TAX_RECID"] = row["TAX_RECID"];
                dt_New.Rows.Add(dr);
            }

            dataTableModel.status = Status.success;
            dataTableModel.dt = dt_New;
            return dataTableModel;
        }


        /// <summary>
        /// 物流代收金額差異表
        /// </summary>
        /// <returns></returns>
        public DataTableModel ReceiveReport(string sDate, string eDate, string user_id)
        {
            DataTableModel dataTableModel = new DataTableModel();
            StringBuilder sb = new StringBuilder();
            sb.Append("select DATADATE,SOURCE,TYPE,b.CUSTOMER,a.INCLUDE_TAX,a.DLV_INV,COMBINE,TAX1,TAX2,CCFEE,COD,FEE,TO_DLV_COD,b.TRANS_NAME,DLV_COD,DLV_COD_CODE,DLV_COD_TIME from jetf.dbo.FEE_MASTER a ");
            sb.Append("join jetf.dbo.customer_master b on a.CUSTOMER=b.CUST_ID and a.DLV_COM=b.TRANS_NAME and b.TRAN_TYPE='海運' ");
            sb.Append("where (DLV_COD_CODE is null or DLV_COD_CODE='N') and b.COMPANY='新竹物流' and a.INCLUDE_TAX='N' and a.DATADATE between @sDate and @eDate  ");
            sb.Append("union all ");
            sb.Append("select DATADATE,SOURCE,TYPE,b.CUSTOMER,a.INCLUDE_TAX,a.DLV_INV,COMBINE,TAX1,TAX2,CCFEE,COD,FEE,TO_DLV_COD,b.TRANS_NAME,DLV_COD,DLV_COD_CODE,DLV_COD_TIME from jetf.dbo.FEE_MASTER a ");
            sb.Append("join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.CUSTOMER,5)=b.CUST_ID and a.DLV_COM=b.TRANS_NO and b.TRAN_TYPE='空運' ");
            sb.Append("where SOURCE='tact' and (DLV_COD_CODE is null or DLV_COD_CODE='N') and b.COMPANY in ('新竹物流','新瑞宅配') and a.INCLUDE_TAX='N' and a.DATADATE between @sDate and @eDate ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            dataTableModel.status = Status.success;
            dataTableModel.dt = dt;
            return dataTableModel;
        }


        /// <summary>
        /// 物流代收匯款比對
        /// </summary>
        /// <returns></returns>
        public DataTableModel TransferReport(string date, string user_id)
        {
            DataTableModel dataTableModel = new DataTableModel();
            StringBuilder sb = new StringBuilder();
            sb.Append("select SOURCE,DATADATE,DLV_REMIT_DATE,ISNULL(b.CUSTOMER,c.CUSTOMER) as CUSTOMER,CLEARANCE_NUMBER,TRACKINGNO,DLV_INV,RECIPIENT,TAX1,TAX2,CCFEE,COD,FEE,TO_DLV_COD,DLV_REMIT_AMOUT,DLV_REMIT_AMOUT_FEE,DLV_REMIT_CODE,DLV_REMIT_TIME from [jetf].[dbo].[FEE_MASTER] a ");
            sb.Append("left join jetf.dbo.customer_master b on a.CUSTOMER=b.CUST_ID and a.DLV_COM=b.TRANS_NAME and b.TRAN_TYPE='海運' ");
            sb.Append("left join jetf.dbo.customer_master c on [jetf].[dbo].[PadLeft]('0',a.CUSTOMER,5)=b.CUST_ID and a.DLV_COM=b.TRANS_NO and b.TRAN_TYPE='空運' ");
            sb.Append("where DLV_REMIT_DATE=@DLV_REMIT_DATE ");

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@DLV_REMIT_DATE", SqlDbType.NVarChar).Value = date;
                da.Fill(dt);
            }
            dataTableModel.status = Status.success;
            dataTableModel.dt = dt;
            return dataTableModel;
        }

        /// <summary>
        /// 物流代收未匯款比對
        /// </summary>
        /// <returns></returns>
        public DataTableModel NoTransferReport(string sDate, string eDate, string user_id)
        {
            DataTableModel dataTableModel = new DataTableModel();
            StringBuilder sb = new StringBuilder();
            sb.Append("select DATADATE,SOURCE,TYPE,b.CUSTOMER,a.INCLUDE_TAX,a.DLV_INV,COMBINE,TAX1,TAX2,CCFEE,COD,FEE,TO_DLV_COD,b.TRANS_NAME from jetf.dbo.FEE_MASTER a ");
            sb.Append("join jetf.dbo.customer_master b on a.CUSTOMER=b.CUST_ID and a.DLV_COM=b.TRANS_NAME and b.TRAN_TYPE='海運' ");
            sb.Append("where (DLV_REMIT_DATE is null or DLV_REMIT_DATE='') and b.COMPANY='新竹物流' and DATADATE between @sDate and @eDate ");
            sb.Append("union all ");
            sb.Append("select DATADATE,SOURCE,TYPE,b.CUSTOMER,a.INCLUDE_TAX,a.DLV_INV,COMBINE,TAX1,TAX2,CCFEE,COD,FEE,TO_DLV_COD,b.TRANS_NAME from jetf.dbo.FEE_MASTER a ");
            sb.Append("join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.CUSTOMER,5)=b.CUST_ID and a.DLV_COM=b.TRANS_NO and b.TRAN_TYPE='空運'  ");
            sb.Append("where SOURCE='tact' and (DLV_REMIT_DATE is null or DLV_REMIT_DATE='') and b.COMPANY in ('新竹物流','新瑞宅配')  and DATADATE between @sDate and @eDate ");

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@sDate", SqlDbType.NVarChar).Value = sDate;
                da.SelectCommand.Parameters.Add("@eDate", SqlDbType.NVarChar).Value = eDate;
                da.Fill(dt);
            }
            dataTableModel.status = Status.success;
            dataTableModel.dt = dt;
            return dataTableModel;
        }
    }
}
