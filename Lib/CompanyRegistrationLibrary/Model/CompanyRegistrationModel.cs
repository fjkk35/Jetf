using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyRegistrationLibrary.Model
{
    public class CompanyRegistrationModel
    {
        /// <summary>
        /// 是否有登記
        /// </summary>
        public bool IsRegistration
        {
            get 
            {
                return string.IsNullOrEmpty(Company_Name) == false && string.IsNullOrEmpty(Revoke_App_Date?.Trim());
            }
        }

        /// <summary>
        /// 統編
        /// </summary>
        public string Business_Accounting_NO { get; set; }

        /// <summary>
        /// 公司名稱
        /// </summary>
        public string Company_Name { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        public string Company_Status_Desc { get; set; }

        /// <summary>
        /// 核准解散日期
        /// </summary>
        public string Revoke_App_Date { get; set; }
    }
}
