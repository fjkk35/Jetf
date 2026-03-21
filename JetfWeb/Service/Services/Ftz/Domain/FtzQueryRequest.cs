using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// Ftz 查詢請求
    /// </summary>
    public class FtzQueryRequest
    {
        /// <summary>
        /// 查詢資料（多筆，換行分隔）
        /// </summary>
        public string HwbqList { get; set; }
    }
}
