using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class SeaClearanceFeeModel
    {
        public int Id { get; set; }

        public string CustCode { get; set; }

        public string CustName { get; set; }

        /// <summary>
        /// G1費用
        /// </summary>
        public int G1Fee { get; set; }

        /// <summary>
        /// 移倉費用
        /// </summary>
        public int MoveWarehouseFee { get; set; }

        /// <summary>
        /// 轉G1
        /// </summary>
        public int TransferG1Fee { get; set; }

        /// <summary>
        /// 轉移倉
        /// </summary>
        public int TransferWarehouseFee { get; set; }

        /// <summary>
        /// X2X3費用
        /// </summary>
        public int X2Fee { get; set; }

    }
}
