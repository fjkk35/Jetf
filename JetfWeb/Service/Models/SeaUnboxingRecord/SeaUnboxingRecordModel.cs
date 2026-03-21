using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaUnboxingRecord
{
    /// <summary>
    /// 主號拆櫃日
    /// </summary>
    public class SeaUnboxingRecordModel
    {
        /// <summary>
        /// 拆櫃日期
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string MainNumber { get; set; }
    }
}
