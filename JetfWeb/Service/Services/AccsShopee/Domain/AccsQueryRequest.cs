using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.AccsShopee.Domain
{
    /// <summary>
    /// Accs 查詢請求
    /// </summary>
    public class AccsQueryRequest
    {
        /// <summary>
        /// 主提單號（多筆用換行分隔）
        /// </summary>
        public string MawbNumbers { get; set; }

        /// <summary>
        /// Token
        /// </summary>
        public string Token { get; set; }
    }
}
