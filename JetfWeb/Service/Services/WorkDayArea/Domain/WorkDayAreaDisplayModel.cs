using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.WorkDayArea.Domain
{
    /// <summary>
    /// 工作天作業地區顯示模型
    /// </summary>
    public class WorkDayAreaDisplayModel
    {
        /// <summary>
        /// 日期
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 星期幾
        /// </summary>
        public string DayOfWeek { get; set; }

        /// <summary>
        /// 日期類型：1工作天，2假日
        /// </summary>
        public int DateType { get; set; }

        /// <summary>
        /// 日期類型文字
        /// </summary>
        public string DateTypeName { get; set; }
    }
}
