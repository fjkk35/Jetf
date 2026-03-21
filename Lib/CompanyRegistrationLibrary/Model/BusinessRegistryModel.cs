using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyRegistrationLibrary.Model
{
    public class BusinessRegistryModel
    {
        /// <summary>
        /// 是否有登記
        /// </summary>
        public bool IsRegistration
        {
            get
            {
                return string.IsNullOrEmpty(Business_Name) == false && 
                    (Business_Current_Status == "01" || Business_Current_Status == "05");
            }
        }

        /// <summary>
        /// 統編
        /// </summary>
        public string President_No { get; set; }

        /// <summary>
        /// 商業名稱
        /// </summary>
        public string Business_Name { get; set; }


        public string Business_Current_Status { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        public string Business_Current_Status_Desc { get; set; }

    }
}
