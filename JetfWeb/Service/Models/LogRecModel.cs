using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class LogRecModel
    {
        /// <summary>
        /// 日期
        /// </summary>
        public string datadate { get; set; }
        /// <summary>
        /// 網頁操作功能
        /// </summary>
        public string fun_index { get; set; }
        /// <summary>
        /// 選取日期
        /// </summary>
        public string fun_datadate { get; set; } = "";
        /// <summary>
        /// 選項種類
        /// </summary>
        public string fun_type { get; set; } = "";
        /// <summary>
        /// 檔名
        /// </summary>
        public string fun_filename { get; set; } = "";
        /// <summary>
        /// 備註
        /// </summary>
        public string fun_memo { get; set; } = "";
        /// <summary>
        /// 使用者帳號
        /// </summary>
        public string user_id { get; set; }
        /// <summary>
        /// IP來源
        /// </summary>
        public string user_ip { get; set; }
        /// <summary>
        /// 更新時間
        /// </summary>
        public string log_time { get; set; }
    }
}
