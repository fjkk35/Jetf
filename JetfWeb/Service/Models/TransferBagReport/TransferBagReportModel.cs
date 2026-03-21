using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.TransferBagReport
{
    public class TransferBagReportModel
    {
        /// <summary>
        /// 派件公司(掃讀)
        /// </summary>
        public string ScanTransNo { get; set; }

        /// <summary>
        /// 掃讀日期(21-24)算隔日
        /// </summary>
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string DespatchName { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        public int Total { get; set; }


    }
}
