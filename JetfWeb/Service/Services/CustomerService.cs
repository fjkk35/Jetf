using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.Customer.Domain;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CustomerService : _BaseService
    {
        /// <summary>
        /// 建構式
        /// </summary>
        public CustomerService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public CustomerQueryResult QueryCustomers(CustomerQueryRequest request)
        {
            request = request ?? new CustomerQueryRequest();

            int page = request.Page <= 0 ? 1 : request.Page;
            int pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
            pageSize = Math.Min(pageSize, 200);

            var query = BuildCustomerQuery(request);

            int totalCount = query.Count();

            var data = query
                .OrderBy(x => x.TranType)
                .ThenBy(x => x.CustId)
                .ThenBy(x => x.TransNo)
                .ThenBy(x => x.TransName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CustomerListItem
                {
                    Id = x.Id,
                    TranType = x.TranType,
                    CustId = x.CustId,
                    Customer = x.Customer,
                    TransNo = x.TransNo,
                    TransName = x.TransName,
                    IncludeTax = x.IncludeTax,
                    IncludeTaxName = x.IncludeTaxName,
                    CompanyNo = x.CompanyNo,
                    Company = x.Company,
                    CodFee = x.CodFee,
                    IsCainiaoP = x.IsCainiaoP == true
                })
                .ToList();

            return new CustomerQueryResult
            {
                TotalCount = totalCount,
                Data = data
            };
        }

        public byte[] ExportExcel(CustomerQueryRequest request)
        {
            request = request ?? new CustomerQueryRequest();

            var data = BuildCustomerQuery(request)
                .OrderBy(x => x.TranType)
                .ThenBy(x => x.CustId)
                .ThenBy(x => x.TransNo)
                .ThenBy(x => x.TransName)
                .Select(x => new CustomerListItem
                {
                    Id = x.Id,
                    TranType = x.TranType,
                    CustId = x.CustId,
                    Customer = x.Customer,
                    TransNo = x.TransNo,
                    TransName = x.TransName,
                    IncludeTax = x.IncludeTax,
                    IncludeTaxName = x.IncludeTaxName,
                    CompanyNo = x.CompanyNo,
                    Company = x.Company,
                    CodFee = x.CodFee,
                    IsCainiaoP = x.IsCainiaoP == true
                })
                .ToList();

            var workbook = CreateExcelWorkbook(data);

            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        public CustomerFormOptions GetFormOptions()
        {
            return new CustomerFormOptions
            {
                TranTypes = new List<CustomerPageOption>
                {
                    new CustomerPageOption { Value = "海運", Text = "海運" },
                    new CustomerPageOption { Value = "空運", Text = "空運" }
                },
                IncludeTaxes = new List<CustomerPageOption>
                {
                    new CustomerPageOption { Value = "Y", Text = "Y" },
                    new CustomerPageOption { Value = "N", Text = "N" },
                    new CustomerPageOption { Value = "D", Text = "D" },
                    new CustomerPageOption { Value = "C", Text = "C" }
                },
                Companies = GetCompanyOptions()
            };
        }

        public List<CustomerPageOption> GetCustomerOptions(string tranType)
        {
            if (tranType == "海運")
            {
                return DataCenterDb.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == "SEA")
                    .Select(x => new CustomerPageOption
                    {
                        Value = x.CustCode,
                        Text = x.CustName
                    })
                    .ToList()
                    .GroupBy(x => x.Value)
                    .Select(g => g.FirstOrDefault())
                    .OrderBy(x => x.Value)
                    .ToList();
            }

            if (tranType == "空運")
            {
                return DataCenterDb.SysCusts
                    .AsNoTracking()
                    .Where(x => x.CustType == "AIR" && !string.IsNullOrEmpty(x.OldCode))
                    .Select(x => new CustomerPageOption
                    {
                        Value = x.OldCode,
                        Text = x.CustName
                    })
                    .ToList()
                    .GroupBy(x => x.Value)
                    .Select(g => g.FirstOrDefault())
                    .OrderBy(x => x.Value)
                    .ToList();
            }

            return new List<CustomerPageOption>();
        }

        public CustomerUpsertModel GetCustomerDetail(int id)
        {
            return JetfDb.CustomerMasters
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new CustomerUpsertModel
                {
                    Id = x.Id,
                    TranType = x.TranType,
                    CustId = x.CustId,
                    Customer = x.Customer,
                    TransNo = x.TransNo,
                    TransName = x.TransName,
                    IncludeTax = x.IncludeTax,
                    IncludeTaxName = x.IncludeTaxName,
                    CompanyNo = x.CompanyNo,
                    Company = x.Company,
                    CodFee = x.CodFee.HasValue ? x.CodFee.Value.ToString() : string.Empty,
                    IsCainiaoP = x.IsCainiaoP == true
                })
                .FirstOrDefault();
        }

        public ResponseModel SaveCustomer(CustomerUpsertModel model, string userId)
        {
            model = model ?? new CustomerUpsertModel();

            string validationMessage = ValidateCustomer(model, out int codFee, out CustomerPageOption customerOption, out CustomerPageOption companyOption);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return new ResponseModel(validationMessage);
            }

            bool isCreate = !model.Id.HasValue;
            CustomerMasterEntity entity;

            if (isCreate)
            {
                entity = new CustomerMasterEntity();
                JetfDb.CustomerMasters.Add(entity);
            }
            else
            {
                entity = JetfDb.CustomerMasters.FirstOrDefault(x => x.Id == model.Id.Value);
                if (entity == null)
                {
                    return new ResponseModel("查無客戶資料");
                }
            }

            entity.TranType = model.TranType.Trim();
            entity.CustId = model.CustId.Trim();
            entity.Customer = customerOption.Text;
            entity.TransNo = model.TranType == "空運" ? (model.TransNo ?? string.Empty).Trim() : string.Empty;
            entity.TransName = (model.TransName ?? string.Empty).Trim();
            entity.IncludeTax = model.IncludeTax.Trim();
            entity.IncludeTaxName = (model.IncludeTaxName ?? string.Empty).Trim();
            entity.CompanyNo = companyOption.Value;
            entity.Company = companyOption.Text;
            entity.CodFee = codFee;
            entity.IsCainiaoP = model.IsCainiaoP;
            entity.UpdateTime = DateTime.Now;
            entity.UpdateOpe = string.IsNullOrWhiteSpace(userId) ? "system" : userId;

            JetfDb.SaveChanges();

            return new ResponseModel
            {
                status = Status.success,
                msg = isCreate ? "新增成功" : "更新成功"
            };
        }

        private List<CustomerPageOption> GetCompanyOptions()
        {
            const string sql = @"
SELECT DISTINCT
    COMPANY_NO AS Value,
    COMPANY AS Text
FROM [jetf].[dbo].[CompanyList]
ORDER BY COMPANY_NO";

            return JetfDb.Database.SqlQuery<CustomerPageOption>(sql).ToList();
        }

        private IQueryable<CustomerMasterEntity> BuildCustomerQuery(CustomerQueryRequest request)
        {
            var custCodes = (request.CustCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            var query = JetfDb.CustomerMasters.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.TranType))
            {
                string tranType = request.TranType.Trim();
                query = query.Where(x => x.TranType == tranType);
            }

            if (custCodes.Any())
            {
                query = query.Where(x => custCodes.Contains(x.CustId));
            }

            if (!string.IsNullOrWhiteSpace(request.TransKeyword))
            {
                string keyword = request.TransKeyword.Trim();
                query = query.Where(x =>
                    (x.TransNo ?? string.Empty).Contains(keyword) ||
                    (x.TransName ?? string.Empty).Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(request.IncludeTax))
            {
                string includeTax = request.IncludeTax.Trim();
                query = query.Where(x => x.IncludeTax == includeTax);
            }

            if (!string.IsNullOrWhiteSpace(request.CompanyNo))
            {
                string companyNo = request.CompanyNo.Trim();
                query = query.Where(x => x.CompanyNo == companyNo);
            }

            if (request.IsCainiaoP.HasValue)
            {
                if (request.IsCainiaoP.Value)
                {
                    query = query.Where(x => x.IsCainiaoP == true);
                }
                else
                {
                    query = query.Where(x => x.IsCainiaoP != true);
                }
            }

            return query;
        }

        private IWorkbook CreateExcelWorkbook(List<CustomerListItem> data)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("客戶查詢");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);

            string[] headers =
            {
                "序號",
                "ID",
                "運送類型",
                "客戶編號",
                "客戶",
                "派件公司編號",
                "派件公司",
                "是否包稅",
                "是否包稅中文",
                "物流公司",
                "手續費",
                "菜鳥尊榮服務"
            };

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            for (int index = 0; index < data.Count; index++)
            {
                var item = data[index];
                var row = sheet.CreateRow(index + 1);

                NpoiCell.CreateIntCell(row, 0, index + 1, dataStyle);
                NpoiCell.CreateIntCell(row, 1, item.Id, dataStyle);
                NpoiCell.CreateCell(row, 2, item.TranType, dataStyle);
                NpoiCell.CreateCell(row, 3, item.CustId, dataStyle);
                NpoiCell.CreateCell(row, 4, item.Customer, dataStyle);
                NpoiCell.CreateCell(row, 5, item.TransNo, dataStyle);
                NpoiCell.CreateCell(row, 6, item.TransName, dataStyle);
                NpoiCell.CreateCell(row, 7, item.IncludeTax, dataStyle);
                NpoiCell.CreateCell(row, 8, item.IncludeTaxName, dataStyle);
                NpoiCell.CreateCell(row, 9, item.Company, dataStyle);
                NpoiCell.CreateIntCell(row, 10, item.CodFee, numberStyle);
                NpoiCell.CreateCell(row, 11, item.IsCainiaoPText, dataStyle);
            }

            sheet.AutoSizeColumns(headers.Length, scale: 1.2, minWidth: 12);
            return workbook;
        }

        private string ValidateCustomer(
            CustomerUpsertModel model,
            out int codFee,
            out CustomerPageOption customerOption,
            out CustomerPageOption companyOption)
        {
            codFee = 0;
            customerOption = null;
            companyOption = null;

            if (string.IsNullOrWhiteSpace(model.TranType))
            {
                return "請選擇運送類型";
            }

            if (model.TranType != "海運" && model.TranType != "空運")
            {
                return "運送類型不正確";
            }

            if (string.IsNullOrWhiteSpace(model.CustId))
            {
                return "請選擇客戶";
            }

            customerOption = GetCustomerOptions(model.TranType)
                .FirstOrDefault(x => x.Value == model.CustId.Trim());

            if (customerOption == null)
            {
                return "查無對應客戶資料";
            }

            if (model.TranType == "空運" && string.IsNullOrWhiteSpace(model.TransNo))
            {
                return "空運需輸入派件公司編號";
            }

            if (string.IsNullOrWhiteSpace(model.TransName))
            {
                return "請輸入派件公司";
            }

            if (string.IsNullOrWhiteSpace(model.IncludeTax))
            {
                return "請選擇是否包稅";
            }

            var includeTaxOptions = new HashSet<string>(new[] { "Y", "N", "D", "C" });
            if (!includeTaxOptions.Contains(model.IncludeTax.Trim()))
            {
                return "是否包稅不正確";
            }

            if (string.IsNullOrWhiteSpace(model.CompanyNo))
            {
                return "請選擇物流公司";
            }

            companyOption = GetCompanyOptions()
                .FirstOrDefault(x => x.Value == model.CompanyNo.Trim());

            if (companyOption == null)
            {
                return "查無對應物流公司";
            }

            if (string.IsNullOrWhiteSpace(model.CodFee))
            {
                return "請輸入手續費";
            }

            if (!int.TryParse(model.CodFee.Trim(), out codFee))
            {
                return "手續費格式不正確";
            }

            string normalizedCustId = model.CustId.Trim();
            string normalizedTransName = (model.TransName ?? string.Empty).Trim();
            string normalizedTransNo = (model.TransNo ?? string.Empty).Trim();

            bool exists;
            if (model.TranType == "海運")
            {
                exists = JetfDb.CustomerMasters.AsNoTracking().Any(x =>
                    x.TranType == model.TranType &&
                    x.CustId == normalizedCustId &&
                    x.TransName == normalizedTransName &&
                    (!model.Id.HasValue || x.Id != model.Id.Value));

                if (exists)
                {
                    return $"{normalizedCustId} 和 {normalizedTransName} 已存在此客戶";
                }
            }
            else
            {
                exists = JetfDb.CustomerMasters.AsNoTracking().Any(x =>
                    x.TranType == model.TranType &&
                    x.CustId == normalizedCustId &&
                    x.TransNo == normalizedTransNo &&
                    (!model.Id.HasValue || x.Id != model.Id.Value));

                if (exists)
                {
                    return $"{normalizedCustId} 和 {normalizedTransNo} 已存在此客戶";
                }
            }

            return string.Empty;
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
        public ResponseModel EditCustomer_Master(CustomerModel model,string user_id)
        {
            ResponseModel resopnseModel = new ResponseModel();
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
        public ResponseModel InsertCustomer_Master(CustomerModel model, string user_id)
        {
            ResponseModel resopnseModel = new ResponseModel();
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
