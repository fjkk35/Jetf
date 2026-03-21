using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// Ftz 併袋號查詢請求
    /// </summary>
    public class FtzBagQueryRequest
    {
        /// <summary>
        /// 查詢袋號（多筆，換行分隔）
        /// </summary>
        public string BagNoList { get; set; }
    }
}
