using Service.Models.Tax;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.Services.Tax
{
    public class TaxService
    {
        /// <summary>
        /// 取得菜鳥P尊榮服務稅金
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxP(DataRow dr)
        {
            var taxData = new TaxData();

            Int32.TryParse(dr["tax1"].ToString(), out var tax1);
            Int32.TryParse(dr["tax2"].ToString(), out var tax2);
            Int32.TryParse(dr["cod"].ToString(), out var cod);
            Int32.TryParse(dr["fee"].ToString(), out var fee);

            //菜鳥只付1000，超過的請派件公司收
            if (tax1 + tax2 > 1000)
            {
                //跟派件收
                taxData.TransCod = (tax1 + tax2) - 1000;

                //跟客戶收
                taxData.CustomerCod = 1000;

                taxData.ToDlvCod = taxData.TransCod + cod + fee;
            }
            else
            {
                //跟派件收
                taxData.TransCod = 0;

                //跟客戶收
                taxData.CustomerCod = tax1 + tax2;

                taxData.ToDlvCod = cod;
            }
            return taxData;
        }

        /// <summary>
        /// 取得稅金N
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxN(DataRow dr)
        {
            var taxData = new TaxData();

            Int32.TryParse(dr["tax1"].ToString(), out var tax1);
            Int32.TryParse(dr["tax2"].ToString(), out var tax2);
            Int32.TryParse(dr["cod"].ToString(), out var cod);
            Int32.TryParse(dr["fee"].ToString(), out var fee);

            //跟派件收
            taxData.TransCod = tax1 + tax2;

            //跟客戶收
            taxData.CustomerCod = 0;

            //不包稅：代收貨款+稅金+手續費
            taxData.ToDlvCod = tax1 + tax2 + cod + fee;

            return taxData;
        }

        /// <summary>
        /// 取得稅金C
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxC(DataRow dr)
        {
            var taxData = new TaxData();

            Int32.TryParse(dr["tax1"].ToString(), out var tax1);
            Int32.TryParse(dr["tax2"].ToString(), out var tax2);
            Int32.TryParse(dr["cod"].ToString(), out var cod);

            //跟派件收
            taxData.TransCod = 0;

            //跟客戶收
            taxData.CustomerCod = tax1 + tax2;

            taxData.ToDlvCod = cod;

            return taxData;
        }

        /// <summary>
        /// 取得稅金D
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxD(DataRow dr)
        {
            var taxData = new TaxData();

            Int32.TryParse(dr["tax1"].ToString(), out var tax1);
            Int32.TryParse(dr["tax2"].ToString(), out var tax2);
            Int32.TryParse(dr["cod"].ToString(), out var cod);

            //跟派件收
            taxData.TransCod = tax1 + tax2;

            //跟客戶收
            taxData.CustomerCod = 0;

            taxData.ToDlvCod = cod;

            return taxData;
        }

        /// <summary>
        /// 取得稅金Y
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxY(DataRow dr)
        {
            var taxData = new TaxData();

            Int32.TryParse(dr["tax1"].ToString(), out var tax1);
            Int32.TryParse(dr["tax2"].ToString(), out var tax2);
            Int32.TryParse(dr["cod"].ToString(), out var cod);

            //跟派件收
            taxData.TransCod = 0;

            //跟客戶收
            taxData.CustomerCod = tax1 + tax2;

            taxData.ToDlvCod = cod;

            return taxData;
        }

        /// <summary>
        /// 是否海運特殊客戶
        /// </summary>
        /// <returns></returns>
        public bool IsSeaSpecial(DataTable dt_Customer_Special, string company, string phone)
        {
            //特殊客戶電話判斷 取右邊9碼
            var recphone = Regex.Replace(phone, "[^0-9]", "");
            if (recphone.Length > 9)
            {
                recphone = recphone.Substring(recphone.Length - 9, 9);
            }
            var dr_Customer_Special = dt_Customer_Special.Select($"PHONE like '%{recphone}%'");

            return recphone != "" && dr_Customer_Special.Length > 0 && company == "新竹物流";
        }

        /// <summary>
        /// 是否空運特殊客戶
        /// </summary>
        /// <returns></returns>
        public bool IsEtlSpecial(DataTable dt_Customer_Special, string company, string phone)
        {
            //特殊客戶電話判斷 取右邊9碼
            var recphone = Regex.Replace(phone, "[^0-9]", "");
            if (recphone.Length > 9)
            {
                recphone = recphone.Substring(recphone.Length - 9, 9);
            }
            var dr_Customer_Special = dt_Customer_Special.Select($"PHONE like '%{recphone}%'");

            return recphone !="" && 
                dr_Customer_Special.Length > 0 && 
                company == "新竹物流" &&
                company == "新瑞宅配" &&
                company == "捷豐"
                ;
        }
    }
}
