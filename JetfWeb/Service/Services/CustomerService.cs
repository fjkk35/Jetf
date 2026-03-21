using Newtonsoft.Json;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CustomerService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public CustomerService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 取得客戶資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetCustomer_Master()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from dbo.customer_master ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            return dt;
        }


        /// <summary>
        /// 更新客戶資料
        /// </summary>
        /// <returns></returns>
        public ResopnseModel EditCustomer_Master(CustomerModel model,string user_id)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "更新成功";
            try
            {
                string check = CheckCustomer_Master(model);
                if (check != "")
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = check;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("update  jetf.dbo.customer_master set TRAN_TYPE=@TRAN_TYPE,CUST_ID=@CUST_ID,CUSTOMER=@CUSTOMER,TRANS_NO=@TRANS_NO,TRANS_NAME=@TRANS_NAME,INCLUDE_TAX=@INCLUDE_TAX,INCLUDE_TAX_NAME=@INCLUDE_TAX_NAME,COMPANY_NO=@COMPANY_NO,COMPANY=@COMPANY,COD_FEE=@COD_FEE,ISCAINIAOP=@ISCAINIAOP,UPDATE_TIME=@UPDATE_TIME,UPDATE_OPE=@UPDATE_OPE ");
                    sb.Append("where ID=@ID ");
                    DataTable dt = new DataTable();
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Parameters.Add("@TRAN_TYPE", SqlDbType.NVarChar).Value = model.tran_type;
                        cmd.Parameters.Add("@CUST_ID", SqlDbType.NVarChar).Value = model.cust_id;
                        cmd.Parameters.Add("@CUSTOMER", SqlDbType.NVarChar).Value = model.customer;
                        cmd.Parameters.Add("@TRANS_NO", SqlDbType.NVarChar).Value = model.trans_no ?? "";
                        cmd.Parameters.Add("@TRANS_NAME", SqlDbType.NVarChar).Value = model.trans_name;
                        cmd.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = model.include_tax;
                        cmd.Parameters.Add("@INCLUDE_TAX_NAME", SqlDbType.NVarChar).Value = model.include_tax_name ?? "";
                        cmd.Parameters.Add("@COMPANY_NO", SqlDbType.NVarChar).Value = model.company_no;
                        cmd.Parameters.Add("@COMPANY", SqlDbType.NVarChar).Value = model.company;
                        cmd.Parameters.Add("@COD_FEE", SqlDbType.NVarChar).Value = model.cod_fee;
                        cmd.Parameters.Add("@ISCAINIAOP", SqlDbType.NVarChar).Value = model.IsCainiaoP;
                        cmd.Parameters.Add("@ID", SqlDbType.NVarChar).Value = model.id;
                        cmd.Parameters.Add("@UPDATE_TIME", SqlDbType.NVarChar).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        cmd.Parameters.Add("@UPDATE_OPE", SqlDbType.NVarChar).Value = user_id;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
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
        /// 新增客戶資料
        /// </summary>
        /// <returns></returns>
        public ResopnseModel InsertCustomer_Master(CustomerModel model, string user_id)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";
            try
            {
                string check = CheckCustomer_Master(model);
                if (check != "")
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = check;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("insert [jetf].[dbo].[customer_master](TRAN_TYPE,CUST_ID,CUSTOMER,TRANS_NO,TRANS_NAME,INCLUDE_TAX,INCLUDE_TAX_NAME,COMPANY_NO,COMPANY,COD_FEE,ISCAINIAOP,UPDATE_TIME,UPDATE_OPE) ");
                    sb.Append("values(@TRAN_TYPE,@CUST_ID,@CUSTOMER,@TRANS_NO,@TRANS_NAME,@INCLUDE_TAX,@INCLUDE_TAX_NAME,@COMPANY_NO,@COMPANY,@COD_FEE,@ISCAINIAOP,@UPDATE_TIME,@UPDATE_OPE) ");
                    DataTable dt = new DataTable();
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Parameters.Add("@TRAN_TYPE", SqlDbType.NVarChar).Value = model.tran_type;
                        cmd.Parameters.Add("@CUST_ID", SqlDbType.NVarChar).Value = model.cust_id;
                        cmd.Parameters.Add("@CUSTOMER", SqlDbType.NVarChar).Value = model.customer;
                        cmd.Parameters.Add("@TRANS_NO", SqlDbType.NVarChar).Value = model.trans_no ?? "";
                        cmd.Parameters.Add("@TRANS_NAME", SqlDbType.NVarChar).Value = model.trans_name;
                        cmd.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = model.include_tax;
                        cmd.Parameters.Add("@INCLUDE_TAX_NAME", SqlDbType.NVarChar).Value = model.include_tax_name ?? "";
                        cmd.Parameters.Add("@COMPANY_NO", SqlDbType.NVarChar).Value = model.company_no;
                        cmd.Parameters.Add("@COMPANY", SqlDbType.NVarChar).Value = model.company;
                        cmd.Parameters.Add("@COD_FEE", SqlDbType.NVarChar).Value = model.cod_fee;
                        cmd.Parameters.Add("@ISCAINIAOP", SqlDbType.NVarChar).Value = model.IsCainiaoP;
                        cmd.Parameters.Add("@UPDATE_TIME", SqlDbType.NVarChar).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        cmd.Parameters.Add("@UPDATE_OPE", SqlDbType.NVarChar).Value = user_id;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
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
        /// 檢查客戶是否重複
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string CheckCustomer_Master(CustomerModel model)
        {
            string result = "";
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            if (model.tran_type == "海運")
            {
                if (model.cust_id == null || model.trans_name == null || model.customer == null || model.cod_fee == null)
                {
                    result = $"[CUST_ID]和[CUSTOMER]和[TRANS_NAME]和[手續費]為必填欄位";
                }
                else
                {
                    sb.Append("select * from [jetf].[dbo].[customer_master] ");
                    sb.Append("where TRAN_TYPE=@TRAN_TYPE and CUST_ID=@CUST_ID and TRANS_NAME=@TRANS_NAME");
                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.Parameters.Add("@TRAN_TYPE", SqlDbType.NVarChar).Value = model.tran_type;
                        da.SelectCommand.Parameters.Add("@CUST_ID", SqlDbType.NVarChar).Value = model.cust_id;
                        da.SelectCommand.Parameters.Add("@TRANS_NAME", SqlDbType.NVarChar).Value = model.trans_name;
                        da.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            if (dt.Rows[0]["ID"].ToString() != model.id)
                            {
                                result = $"{model.cust_id}和{model.trans_name}已存在此客戶";
                            }
                        }
                    }
                }
            }
            else if (model.tran_type == "空運")
            {
                if (model.cust_id == null || model.trans_no == null || model.trans_name == null || model.customer == null || model.cod_fee == null)
                {
                    result = $"[CUST_ID]和[CUSTOMER]和[TRANS_NO]和[TRANS_NAME]和[手續費]為必填欄位";
                }
                else
                {
                    sb.Append("select * from [jetf].[dbo].[customer_master] ");
                    sb.Append("where TRAN_TYPE=@TRAN_TYPE and CUST_ID=@CUST_ID and TRANS_NO =@TRANS_NO ");
                    using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                    {
                        da.SelectCommand.Parameters.Add("@TRAN_TYPE", SqlDbType.NVarChar).Value = model.tran_type;
                        da.SelectCommand.Parameters.Add("@CUST_ID", SqlDbType.NVarChar).Value = model.cust_id;
                        da.SelectCommand.Parameters.Add("@TRANS_NO", SqlDbType.NVarChar).Value = model.trans_no;
                        da.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            if (dt.Rows[0]["ID"].ToString() != model.id)
                            {
                                result = $"{model.cust_id}和{model.trans_no}已存在此客戶";
                            }
                        }
                    }
                }
            }
            return result;
        }



        /// <summary>
        /// 取得客戶資料
        /// </summary>
        /// <returns></returns>
        public DataTable GetCustomer_Master(string id)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from dbo.customer_master where ID=@ID");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@ID", SqlDbType.NVarChar).Value = id;
                da.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// 取得客戶名稱
        /// </summary>
        /// <returns></returns>
        public string GetCustomerName(string tranType,string custId)
        {
            string custName = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[customer_master] where TRAN_TYPE=@TRAN_TYPE and CUST_ID=@CUST_ID ", conn))
            {
                da.SelectCommand.Parameters.Add("@TRAN_TYPE", SqlDbType.NVarChar).Value = tranType;
                da.SelectCommand.Parameters.Add("@CUST_ID", SqlDbType.NVarChar).Value = custId;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                custName = dt.Rows[0]["CUSTOMER"].ToString();
            }
            return custName;
        }

        /// <summary>
        /// 取得物流公司
        /// </summary>
        /// <returns></returns>
        public DataTable GetCompanyList()
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[CompanyList] ", conn))
            {
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 特殊客戶，用電話號碼和客戶收錢
        /// </summary>
        /// <returns></returns>
        public DataTable GetCustomer_Special(string tran_type)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM [jetf].[dbo].[customer_special] where TRAN_TYPE=@TRAN_TYPE", conn))
            {
                da.SelectCommand.Parameters.Add("@TRAN_TYPE", SqlDbType.NVarChar).Value = tran_type;
                da.Fill(dt);
            }
            return dt;
        }

        //取得客戶
        public DataTable GetCustomerList()
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT [TRAN_TYPE],[CUST_ID],[CUSTOMER] FROM [jetf].[dbo].[customer_master] ");
            sb.Append("group by [TRAN_TYPE],[CUST_ID],[CUSTOMER] ");
            sb.Append("order by [TRAN_TYPE],[CUST_ID] ");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            return dt;
        }

        //取得派件公司
        public DataTable GetTransNameList()
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT TRAN_TYPE,TRANS_NO,TRANS_NAME FROM [jetf].[dbo].[customer_master] ");
            sb.Append("group by TRAN_TYPE,TRANS_NO,TRANS_NAME ");
            sb.Append("order by TRAN_TYPE,TRANS_NO ");
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            return dt;
        }
    }
}
