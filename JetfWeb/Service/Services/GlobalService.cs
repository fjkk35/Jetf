using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class GlobalService
    {
        /// <summary>
        /// 取得稅金類別名稱
        /// </summary>
        /// <returns></returns>
        public string GetTaxType(string include_tax)
        {
            switch (include_tax)
            {
                case "N":
                    include_tax = "不包稅";
                    break;
                case "Y":
                    include_tax = "包稅";
                    break;
                case "D":
                    include_tax = "收客匯款";
                    break;
                case "C":
                    include_tax = "客戶付款";
                    break;
            }
            return include_tax;
        }

        //取得IP
        public string GetIPAddress()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string sIPAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(sIPAddress))
            {
                return context.Request.ServerVariables["REMOTE_ADDR"];
            }
            else
            {
                string[] ipArray = sIPAddress.Split(new Char[] { ',' });
                return ipArray[0];
            }
        }
    }
}
