using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.WorkDayArea.Domain
{
    /// <summary>
    /// 工作天作業地區查詢請求
    /// </summary>
    public class WorkDayAreaQueryRequest
    {
        /// <summary>
        /// 作業地區Id
        /// </summary>
        public int WorkAreaId { get; set; }

        /// <summary>
        /// 開始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 結束日期
        /// </summary>
        public DateTime EndDate { get; set; }
    }
}
