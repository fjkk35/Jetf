using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
  /// 貨況查詢請求
    /// </summary>
    public class SearchCargoRequest
    {
        /// <summary>
 /// 查詢類型
        /// </summary>
        public string SearchType { get; set; }

/// <summary>
    /// 查詢值
        /// </summary>
        public string SearchValue { get; set; }
    }
}
