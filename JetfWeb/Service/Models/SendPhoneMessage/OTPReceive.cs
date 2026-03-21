using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SendPhoneMessage
{
    public class OTPReceive
    {
        /// <summary>
        /// 簡訊流水號
        /// </summary>
        public string RowId { get; set; }

        /// <summary>
        /// 則數
        /// </summary>
        public string Cnt { get; set; }

        /// <summary>
        /// 發送結果代碼 
        /// </summary>
        public string ErrorCode { get; set; }
    }
}
