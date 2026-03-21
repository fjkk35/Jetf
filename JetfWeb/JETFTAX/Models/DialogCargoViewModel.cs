using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class DialogCargoViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Id")]
        public string Id { get; set; } = "";
        /// <summary>
        /// 到港日
        /// </summary>
        [Display(Name = "到港日")]
        public string ETA { get; set; } = "";
        /// <summary>
        /// 毛重
        /// </summary>
        [Display(Name = "毛重")]
        public string GW { get; set; }
        /// <summary>
        /// 件數
        /// </summary>
        [Display(Name = "件數")]
        public string PIECE { get; set; }
        /// <summary>
        /// 倉儲類型
        /// </summary>
        [Display(Name = "倉儲類型")]
        public string Source { get; set; }
        /// <summary>
        /// 報關類型
        /// </summary>
        [Display(Name = "報關類型")]
        public string Type { get; set; }
        /// <summary>
        /// 主提單號
        /// </summary>
        [Display(Name = "主提單號")]
        public string Main_Number { get; set; }
        /// <summary>
        /// 清關袋號
        /// </summary>
        [Display(Name = "清關袋號")]
        public string Bag_Number { get; set; }
        /// <summary>
        /// 稅單號碼
        /// </summary>
        [Display(Name = "稅單號碼")]
        public string Tax_Number { get; set; }
        /// <summary>
        /// 客戶代號
        /// </summary>
        [Display(Name = "客戶代號")]
        public string Cust_Id { get; set; }
        /// <summary>
        /// 客戶名稱
        /// </summary>
        [Display(Name = "客戶名稱")]
        public string Cust_Name { get; set; }
        /// <summary>
        /// 派件公司
        /// </summary>
        [Display(Name = "派件公司")]
        public string Trans_Name { get; set; }
        /// <summary>
        /// 派件公司(新)
        /// </summary>
        [Display(Name = "派件公司(新)")]
        public string Trans_Name_New { get; set; }
        /// <summary>
        /// 物流貨號
        /// </summary>
        [Display(Name = "物流貨號")]
        public string Dlv_Inv { get; set; }
        /// <summary>
        /// 物流貨號
        /// </summary>
        [Display(Name = "物流貨號")]
        public string Deliveryno { get; set; }
        /// <summary>
        /// 收件人名稱
        /// </summary>
        [Display(Name = "收件人名稱")]
        public string Recipient { get; set; }
        /// <summary>
        /// 收件人電話
        /// </summary>
        [Display(Name = "收件人電話")]
        public string Recphone { get; set; }
        /// <summary>
        /// 收件人地址
        /// </summary>
        [Display(Name = "收件人地址")]
        public string Recaddress { get; set; }
        /// <summary>
        /// 到付款
        /// </summary>
        [Display(Name = "到付款")]
        public string CC { get; set; }
        /// <summary>
        /// 客戶外箱號
        /// </summary>
        [Display(Name = "客戶外箱號")]
        public string Field_X { get; set; }
        /// <summary>
        /// 進倉時間
        /// </summary>
        [Display(Name = "進倉時間")]
        public string In_Datetime { get; set; }
        /// <summary>
        /// 出倉日期
        /// </summary>
        [Display(Name = "出倉日期")]
        public string Out_Date { get; set; }
        /// <summary>
        /// 出倉時間
        /// </summary>
        [Display(Name = "出倉時間")]
        public string Out_Datetime { get; set; }
        /// <summary>
        /// 稅金類別
        /// </summary>
        [Display(Name = "稅金類別")]
        public string Include_Tax { get; set; }
        /// <summary>
        /// 稅金1
        /// </summary>
        [Display(Name = "稅金1")]
        public string Tax1 { get; set; }
        /// <summary>
        /// 稅金2
        /// </summary>
        [Display(Name = "稅金2")]
        public string Tax2 { get; set; }
        /// <summary>
        /// 稅金合計
        /// </summary>
        [Display(Name = "稅金合計")]
        public string TotalTax { get; set; }
        /// <summary>
        /// 報關費
        /// </summary>
        [Display(Name = "報關費")]
        public string CCFee { get; set; }
        /// <summary>
        /// 代收手續費
        /// </summary>
        [Display(Name = "代收手續費")]
        public string Fee { get; set; }
        /// <summary>
        /// 到付款
        /// </summary>
        [Display(Name = "到付款")]
        public string Cod { get; set; }
        /// <summary>
        /// 物流代收款
        /// </summary>
        [Display(Name = "物流代收款")]
        public string To_Dlv_Cod { get; set; }

        /// <summary>
        /// 跟廠商收稅金
        /// </summary>
        [Display(Name = "跟廠商收稅金")]
        public string CustomerCod { get; set; }

        /// <summary>
        /// 跟派件收稅金
        /// </summary>
        [Display(Name = "跟派件收稅金")]
        public string TransCod { get; set; }

        /// <summary>
        /// 接駁公司
        /// </summary>
        [Display(Name = "接駁公司")]
        public string ScanCargoTransName { get; set; }
        /// <summary>
        /// 櫃號車號
        /// </summary>
        [Display(Name = "櫃號車號")]
        public string ScanCargoCarNo { get; set; }
        /// <summary>
        /// 掃貨上車
        /// </summary>
        [Display(Name = "掃貨上車")]
        public string ScanCargoUploadTime { get; set; }
        /// <summary>
        /// 掃讀工號
        /// </summary>
        [Display(Name = "掃讀工號")]
        public string ScanCargoUploadOpe { get; set; }

        /// <summary>
        /// 錯單類別
        /// </summary>
        [Display(Name = "錯單類別")]
        public string ErrorReason { get; set; }

        /// <summary>
        /// 客戶訂單號
        /// </summary>
        [Display(Name = "客戶訂單號")]
        public string Order_No { get; set; }

        /// <summary>
        /// 尾程單號
        /// </summary>
        [Display(Name = "尾程單號")]
        public string Express_No { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        [Display(Name = "分提單號")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        [Display(Name = "狀態")]
        public string Status { get; set; }

        public List<DialogCargo> DialogCargoList { get; set; }
        /// <summary>
        /// 稅金編號
        /// </summary>
        public List<TaxNumberItem> TaxNumberList { get; set; }
    }

    public class DialogCargo
    {
        [Display(Name = "作業時間")]
        public string tran_modify_time { get; set; }
        [Display(Name = "配送狀態")]
        public string tran_status { get; set; }
    }

    public class TaxNumberItem
    {
        public string TaxNumber { get; set; }
    }

}