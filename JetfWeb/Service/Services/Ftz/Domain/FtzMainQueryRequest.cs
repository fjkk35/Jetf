using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// Ftz 主號查詢請求
    /// </summary>
    public class FtzMainQueryRequest
    {
        /// <summary>
        /// 主號（多筆時用換行分隔）
        /// </summary>
        public string Mwb { get; set; }
    }
}
