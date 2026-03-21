using JETFTAX.Models;
using Newtonsoft.Json;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CustomerController : Controller
    {
        CustomerService customerService = new CustomerService();

        /// <summary>
        /// 客戶查詢
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1","2")]
        [UserAuthorize(Authority.SearchCustomer)]
        public ActionResult SearchCustomer()
        {
            return View();
        }

        /// <summary>
        /// 客戶查詢-客戶資料
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.SearchCustomer)]
        public ActionResult GetCustomer() {
            DataTable dt = customerService.GetCustomer_Master();
            //int count = dt.Rows.Count;
            //JDataTableModel model = new JDataTableModel()
            //{
            //    recordsTotal = count,
            //    recordsFiltered = count,
            //    data = JsonConvert.SerializeObject(dt)

            //};
            string data = JsonConvert.SerializeObject(dt);
            return Json(data,JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 客戶查詢-客戶資料明細
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.SearchCustomer)]
        public ActionResult DialogCustomer(string id)
        {
            List<SelectListItem> tranTypeList = new List<SelectListItem>();
            tranTypeList.Add(new SelectListItem() { Text = "海運", Value = "海運" });
            tranTypeList.Add(new SelectListItem() { Text = "空運", Value = "空運" });

            //物流公司
            List<SelectListItem> customerList = new List<SelectListItem>();
            DataTable dt_CompanyList = customerService.GetCompanyList();
            for (int i = 0; i < dt_CompanyList.Rows.Count; i++)
            {
                //company = $"{ dt_CompanyList.Rows[i]["COMPANY_NO"].ToString()}-{dt_CompanyList.Rows[i]["COMPANY"].ToString()}";
                customerList.Add(new SelectListItem() { Text = dt_CompanyList.Rows[i]["COMPANY"].ToString(), Value = dt_CompanyList.Rows[i]["COMPANY_NO"].ToString() });
            }

            //是否包稅
            List<SelectListItem> includeTaxList = new List<SelectListItem>();
            includeTaxList.Add(new SelectListItem() { Text = "Y", Value = "Y" });
            includeTaxList.Add(new SelectListItem() { Text = "N", Value = "N" });
            includeTaxList.Add(new SelectListItem() { Text = "D", Value = "D" });
            includeTaxList.Add(new SelectListItem() { Text = "C", Value = "C" });

            CustomerViewModel vm = new CustomerViewModel() {
              ddlTranTypeList= tranTypeList,
              ddlCompanyList= customerList,
              ddlIncludeTaxList=includeTaxList,
            };

            if (id != "")
            {
                DataTable dt = customerService.GetCustomer_Master(id);
                vm.id = id;
                vm.tran_type = dt.Rows[0]["TRAN_TYPE"].ToString();
                vm.cust_id = dt.Rows[0]["CUST_ID"].ToString();
                vm.customer = dt.Rows[0]["CUSTOMER"].ToString();
                vm.trans_no = dt.Rows[0]["TRANS_NO"].ToString();
                vm.trans_name = dt.Rows[0]["TRANS_NAME"].ToString();
                vm.include_tax = dt.Rows[0]["INCLUDE_TAX"].ToString();
                vm.include_tax_name = dt.Rows[0]["INCLUDE_TAX_NAME"].ToString();
                vm.company_no = dt.Rows[0]["COMPANY_NO"].ToString();
                vm.company = dt.Rows[0]["COMPANY"].ToString();
                vm.cod_fee = dt.Rows[0]["COD_FEE"].ToString();
                vm.IsCainiaoP = Convert.ToBoolean(dt.Rows[0]["ISCAINIAOP"]);
            }
                

            return PartialView(vm);
        }

        /// <summary>
        /// 客戶查詢-客戶資料明細-新增或修改
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2")]
        [UserAuthorize(Authority.SearchCustomer)]
        public ActionResult EditCustomer(CustomerViewModel vm)
        {
            ResopnseModel resopnseModel;
            CustomerModel model = new CustomerModel() {
                id=vm.id,
                tran_type=vm.tran_type,
                cust_id=vm.cust_id,
                customer=vm.customer,
                trans_no=vm.trans_no,
                trans_name=vm.trans_name,
                include_tax=vm.include_tax,
                include_tax_name=vm.include_tax_name,
                company_no=vm.company_no,
                company=vm.company,
                cod_fee=vm.cod_fee,
                IsCainiaoP = vm.IsCainiaoP
            };

            if (vm.id == null)
            {
                resopnseModel= customerService.InsertCustomer_Master(model, Session["user_id"].ToString());
            }
            else {
                resopnseModel = customerService.EditCustomer_Master(model, Session["user_id"].ToString());
            }
           
            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }
    }
}