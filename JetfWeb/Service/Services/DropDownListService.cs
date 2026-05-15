using Service.EnumTax;
using System;
using System.Collections;
using System.Web;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Dapper;
using Service.Extensions;
using System.Data.SqlClient;
using System.Data;

namespace Service.Services
{
    public class DropDownListService : _BaseService
    {
        public DropDownListService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }


        /// <summary>
        /// 取得空運稅金種類
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetEtlTaxTypeList()
        {
            var etlTaxTypeList = Enum.GetValues(typeof(EtlTaxType)).Cast<EtlTaxType>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = GetDescription(item)
                                });
            return etlTaxTypeList;
        }

        /// <summary>
        /// 取得海運稅金種類
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetSeaTaxTypeList()
        {
            var etlTaxTypeList = Enum.GetValues(typeof(SeaTaxType)).Cast<SeaTaxType>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = item.ToDescription()
                                });
            return etlTaxTypeList;
        }

        /// <summary>
        /// 海運倉別
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetSeaWarehouseTypeList()
        {
            return Enum.GetValues(typeof(SeaWarehouseType)).Cast<SeaWarehouseType>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = item.ToDescription()
                                });
        }

        /// <summary>
        /// 報關方式
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetPostEntryTypeList()
        {
            return Enum.GetValues(typeof(PostEntryType)).Cast<PostEntryType>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = item.ToDescription()
                                });
        }

        

        /// <summary>
        /// 取得物流公司
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetCompanyList()
        {
            var sql = @" select * from jetf.[dbo].[CompanyList] order by COMPANY_NO";

            var list = conn.Query(sql).Select(item => new SelectListItem
            {
                Value = item.COMPANY,
                Text = item.COMPANY
            });

            return list;
        }

        /// <summary>
        /// 取得罐頭簡訊
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetErrorOrderSmsMessages()
        {
            var sql = @"
                        select * from jetf.[dbo].[ErrorOrderSmsMessage]
                        ";

            var list = conn.Query(sql).Select(item => new SelectListItem
            {
                Value = item.Id.ToString(),
                Text = item.Name
            });

            return list;
        }

        /// <summary>
        /// 取得稅金收費方式
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetTaxPaymentTypeList()
        {
            var list = Enum.GetValues(typeof(TaxPaymentType)).Cast<TaxPaymentType>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = GetDescription(item)
                                });
            return list;
        }

        /// <summary>
        /// 取得Cpt單一入口網站查詢
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetCptTradeVanEnumList()
        {
            var list = Enum.GetValues(typeof(CptTradeVanEnum)).Cast<CptTradeVanEnum>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = GetDescription(item)
                                });
            return list;
        }

        /// <summary>
        /// 取得海運客戶
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetSeaCustomerList()
        {
            var sql = @"
                        select Cust_Code,Cust_Name from DATA_CENTER.dbo.SYS_CUST
                        where CUST_TYPE='SEA'
                        order by Cust_Code
                        ";

            var list = conn.Query(sql).Select(item => new SelectListItem
            {
                Value = item.Cust_Code,
                Text =$"{item.Cust_Code}-{item.Cust_Name}"
            });

            return list;
        }

        /// <summary>
        /// 取得空運客戶
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetAirCustomerList()
        {
            var sql = @"
                        select Cust_Code,Cust_Name from DATA_CENTER.dbo.SYS_CUST
                        where CUST_TYPE='AIR'
                        ORDER BY Cust_Code
                        ";

            var list = conn.Query(sql).Select(item => new SelectListItem
            {
                Value = item.Cust_Code,
                Text = item.Cust_Name
            });

            return list;
        }

        /// <summary>
        /// 取得空運客戶
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetEtlCustomerList()
        {
            var sql = @"
                       select distinct CUST_ID,CUSTOMER from jetf.dbo.customer_master
                       where TRAN_TYPE='空運'
                       order by CUST_ID
                        ";

            var list = conn.Query(sql).Select(item => new SelectListItem
            {
                Value = item.CUST_ID,
                Text = $"{item.CUST_ID}-{item.CUSTOMER}"
            });

            return list;
        }


        /// <summary>
        /// 取得Enum名稱
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }
}
