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
        public TaxData GetTaxP(TaxCalculationInput input)
        {
            var taxData = new TaxData();
            var tax1 = input?.Tax1 ?? 0;
            var tax2 = input?.Tax2 ?? 0;
            var cod = input?.Cod ?? 0;
            var fee = input?.Fee ?? 0;

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
        /// 取得菜鳥P尊榮服務稅金
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxP(DataRow dr)
        {
            return GetTaxP(CreateTaxCalculationInput(dr));
        }

        /// <summary>
        /// 取得稅金N
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxN(TaxCalculationInput input)
        {
            var taxData = new TaxData();
            var tax1 = input?.Tax1 ?? 0;
            var tax2 = input?.Tax2 ?? 0;
            var cod = input?.Cod ?? 0;
            var fee = input?.Fee ?? 0;

            //跟派件收
            taxData.TransCod = tax1 + tax2;

            //跟客戶收
            taxData.CustomerCod = 0;

            //不包稅：代收貨款+稅金+手續費
            taxData.ToDlvCod = tax1 + tax2 + cod + fee;

            return taxData;
        }

        /// <summary>
        /// 取得稅金N
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxN(DataRow dr)
        {
            return GetTaxN(CreateTaxCalculationInput(dr));
        }

        /// <summary>
        /// 取得稅金C
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxC(TaxCalculationInput input)
        {
            var taxData = new TaxData();
            var tax1 = input?.Tax1 ?? 0;
            var tax2 = input?.Tax2 ?? 0;
            var cod = input?.Cod ?? 0;

            //跟派件收
            taxData.TransCod = 0;

            //跟客戶收
            taxData.CustomerCod = tax1 + tax2;

            taxData.ToDlvCod = cod;

            return taxData;
        }

        /// <summary>
        /// 取得稅金C
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxC(DataRow dr)
        {
            return GetTaxC(CreateTaxCalculationInput(dr));
        }

        /// <summary>
        /// 取得稅金D
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxD(TaxCalculationInput input)
        {
            var taxData = new TaxData();
            var tax1 = input?.Tax1 ?? 0;
            var tax2 = input?.Tax2 ?? 0;
            var cod = input?.Cod ?? 0;

            //跟派件收
            taxData.TransCod = tax1 + tax2;

            //跟客戶收
            taxData.CustomerCod = 0;

            taxData.ToDlvCod = cod;

            return taxData;
        }

        /// <summary>
        /// 取得稅金D
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxD(DataRow dr)
        {
            return GetTaxD(CreateTaxCalculationInput(dr));
        }

        /// <summary>
        /// 取得稅金Y
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxY(TaxCalculationInput input)
        {
            var taxData = new TaxData();
            var tax1 = input?.Tax1 ?? 0;
            var tax2 = input?.Tax2 ?? 0;
            var cod = input?.Cod ?? 0;

            //跟派件收
            taxData.TransCod = 0;

            //跟客戶收
            taxData.CustomerCod = tax1 + tax2;

            taxData.ToDlvCod = cod;

            return taxData;
        }

        /// <summary>
        /// 取得稅金Y
        /// </summary>
        /// <returns></returns>
        public TaxData GetTaxY(DataRow dr)
        {
            return GetTaxY(CreateTaxCalculationInput(dr));
        }

        /// <summary>
        /// 是否海運特殊客戶
        /// </summary>
        /// <returns></returns>
        public bool IsSeaSpecial(IEnumerable<string> customerSpecialPhones, string company, string phone)
        {
            var recphone = NormalizePhone(phone);
            if (string.IsNullOrEmpty(recphone))
            {
                return false;
            }

            var phones = customerSpecialPhones ?? Enumerable.Empty<string>();
            var hasMatchedPhone = phones.Any(x => !string.IsNullOrWhiteSpace(x) && x.IndexOf(recphone, StringComparison.OrdinalIgnoreCase) >= 0);
            return hasMatchedPhone && company == "新竹物流";
        }

        /// <summary>
        /// 是否海運特殊客戶
        /// </summary>
        /// <returns></returns>
        public bool IsSeaSpecial(DataTable dt_Customer_Special, string company, string phone)
        {
            var phones = dt_Customer_Special == null
                ? Enumerable.Empty<string>()
                : dt_Customer_Special.AsEnumerable().Select(row => row["PHONE"].ToString());

            return IsSeaSpecial(phones, company, phone);
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

        private static TaxCalculationInput CreateTaxCalculationInput(DataRow dr)
        {
            return new TaxCalculationInput
            {
                Tax1 = ToInt(dr, "tax1"),
                Tax2 = ToInt(dr, "tax2"),
                Cod = ToInt(dr, "cod"),
                Fee = ToInt(dr, "fee")
            };
        }

        private static int ToInt(DataRow dr, string columnName)
        {
            if (dr == null)
            {
                return 0;
            }

            Int32.TryParse(dr[columnName].ToString(), out var value);
            return value;
        }

        private static string NormalizePhone(string phone)
        {
            var recphone = Regex.Replace(phone ?? string.Empty, "[^0-9]", string.Empty);
            if (recphone.Length > 9)
            {
                recphone = recphone.Substring(recphone.Length - 9, 9);
            }

            return recphone;
        }
    }
}
