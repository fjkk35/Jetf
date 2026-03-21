using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SendPhoneMessage
{
    public class OTPSend
    {
        public string UID { get; set; }

        public string Pwd { get; set; }

        /// <summary>
        /// 手機號碼
        /// </summary>
        public string DA { get; set; }

        /// <summary>
        /// 簡訊內容
        /// </summary>
        public string SM { get; set; }

        /// <summary>
        /// 預約時間 
        /// </summary>
        public string SCHEDULETIME { get; set; }

        /// <summary>
        /// 失效時間
        /// </summary>
        public string STOPTIME { get; set; }
    }
}
