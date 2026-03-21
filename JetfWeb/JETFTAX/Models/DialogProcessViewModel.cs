using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class DialogProcessViewModel
    {

        /// <summary>
        /// MID
        /// </summary>
        [Display(Name = "MID")]
        public string P_MId { get; set; } = "";
        /// <summary>
        /// 客戶代號
        /// </summary>
        [Display(Name = "客戶代號")]
        public string P_Cust_Id { get; set; } = "";
        /// <summary>
        /// 客戶名稱
        /// </summary>
        [Display(Name = "客戶名稱")]
        public string P_Customer { get; set; } = "";
        /// <summary>
        /// 輸入日期 
        /// </summary>
        [Display(Name = "輸入日期")]
        public string P_DataDate { get; set; } = "";
        /// <summary>
        /// 入倉日期 
        /// </summary>
        [Display(Name = "入倉日期")]
        public string P_Sign_In_Time { get; set; } = "";
        /// <summary>
        /// 主提單號 
        /// </summary>
        [Display(Name = "主提單號")]
        public string P_MainNumber { get; set; } = "";
        /// <summary>
        /// 清關袋號 
        /// </summary>
        [Display(Name = "清關袋號")]
        public string P_Bl_No { get; set; } = "";
        /// <summary>
        /// 分提單號 
        /// </summary>
        //[Display(Name = "分提單號")]
        //public string P_TrackingNo { get; set; } = "";
        /// <summary>
        /// 物流貨號 
        /// </summary>
        [Display(Name = "物流貨號")]
        public string P_Dlv_Inv { get; set; } = "";
        /// <summary>
        /// 收件人名稱 
        /// </summary>
        [Display(Name = "收件人名稱")]
        public string P_Recipient { get; set; } = "";
        /// <summary>
        /// 收件人電話 
        /// </summary>
        [Display(Name = "收件人電話")]
        public string P_Recphone { get; set; } = "";
        /// <summary>
        /// 說明 
        /// </summary>
        [Display(Name = "處置說明")]
        public string P_Remark { get; set; } = "";

        /// <summary>
        /// 分類
        /// </summary>
        [Display(Name = "分　　類")]
        public string P_Type { get; set; } = "";
    }
}