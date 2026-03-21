using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 查詢請求
    /// </summary>
    public class TactQueryRequest
    {
        /// <summary>
        /// 分號列表（多行）
        /// </summary>
        public string HwbNoList { get; set; }
    }
}
