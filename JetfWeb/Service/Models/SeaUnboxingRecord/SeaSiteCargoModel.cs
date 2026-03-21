using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaUnboxingRecord
{
    /// <summary>
    /// 現場有貨
    /// </summary>
    public class SeaSiteCargoModel
    {
        /// <summary>
        /// 現場通知日期
        /// </summary>
        public string DataDate { get; set; }

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
        /// 派送號
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 分號件數
        /// </summary>
        public string Piece { get; set; }
    }
}
