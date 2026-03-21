using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.WorkDayArea.Domain
{
    /// <summary>
    /// 工作天作業地區更新請求
    /// </summary>
    public class WorkDayAreaUpdateRequest
    {
        /// <summary>
        /// 作業地區Id
        /// </summary>
        public int WorkAreaId { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 日期類型：1工作天，2假日
        /// </summary>
        public int DateType { get; set; }
    }
}
