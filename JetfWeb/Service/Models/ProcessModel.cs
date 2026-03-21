using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class ProcessModel
    {
        /// <summary>
        /// MId
        /// </summary>
        public string MId { get; set; }
        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Id { get; set; } 
        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string Customer { get; set; } 
        /// <summary>
        /// 輸入日期 
        /// </summary>
        public string DataDate { get; set; }
        /// <summary>
        /// 入倉日期 
        /// </summary>
        public string Sign_In_Time { get; set; }
        /// <summary>
        /// 主提單號 
        /// </summary>
        public string MainNumber { get; set; }
        /// <summary>
        /// 清關袋號 
        /// </summary>
        public string Bl_No { get; set; } 
        /// <summary>
        /// 物流貨號 
        /// </summary>
        public string Dlv_Inv { get; set; } 
        /// <summary>
        /// 收件人名稱 
        /// </summary>
        public string Recipient { get; set; }
        /// <summary>
        /// 收件人電話 
        /// </summary>
        public string Recphone { get; set; } 
        /// <summary>
        /// 說明 
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 路徑
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// 檔名 
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 人員
        /// </summary>
        public string User_Id { get; set; }

        /// <summary>
        /// 處置說明分類
        /// </summary>
        public string Process_Type { get; set; }
    }
}
