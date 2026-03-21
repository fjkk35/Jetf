using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 主號查詢請求
    /// </summary>
    public class TactMainQueryRequest
    {
        /// <summary>
        /// 主號（支援多行）
        /// </summary>
        public string Mwb { get; set; }
    }
}
