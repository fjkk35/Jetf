using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 併袋號查詢請求
    /// </summary>
    public class TactBagQueryRequest
    {
        /// <summary>
        /// 併袋號列表（支援換行分隔）
        /// </summary>
        public string BagNoList { get; set; }
    }
}
