using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.BusinessRegistryNew
{
    public class BusinessRegistryNewModel
    {
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
