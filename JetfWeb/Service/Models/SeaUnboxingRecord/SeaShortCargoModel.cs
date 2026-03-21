using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaUnboxingRecord
{
    /// <summary>
    /// 短到
    /// </summary>
    public class SeaShortCargoModel
    {
        /// <summary>
        /// 倉儲
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分號
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 開立短到單日期
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 短到件數
        /// </summary>
        public string Piece { get; set; }
    }
}
