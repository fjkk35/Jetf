using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
    /// 貨況明細回應
    /// </summary>
    public class CargoDetailResponse
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 預計到港日
        /// </summary>
        public DateTime? ETA { get; set; }

        /// <summary>
        /// 毛重
        /// </summary>
        public string GW { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        public string PIECE { get; set; }

        /// <summary>
        /// 倉儲類型
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 清關類型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string Main_Number { get; set; }

        /// <summary>
        /// 清關袋號
        /// </summary>
        public string Bag_Number { get; set; }

        /// <summary>
        /// 稅單編號
        /// </summary>
        public string Tax_Number { get; set; }

        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Id { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string Cust_Name { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string Trans_Name { get; set; }

        /// <summary>
        /// 派件公司(新)
        /// </summary>
        public string Trans_Name_New { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string Dlv_Inv { get; set; }

        /// <summary>
        /// 物流貨號
        /// </summary>
        public string Deliveryno { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話
        /// </summary>
        public string Recphone { get; set; }

        /// <summary>
        /// 收件人地址
        /// </summary>
        public string Recaddress { get; set; }

        /// <summary>
        /// CC
        /// </summary>
        public string CC { get; set; }

        /// <summary>
        /// 客戶外箱號
        /// </summary>
        public string Field_X { get; set; }

        /// <summary>
        /// 入倉時間
        /// </summary>
        public DateTime? In_Datetime { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? Out_Datetime { get; set; }

        /// <summary>
        /// 含稅價
        /// </summary>
        public string Include_Tax { get; set; }

        /// <summary>
        /// 關稅
        /// </summary>
        public string Tax1 { get; set; }

        /// <summary>
        /// 營業稅
        /// </summary>
        public string Tax2 { get; set; }

        /// <summary>
        /// 總稅金
        /// </summary>
        public string TotalTax { get; set; }

        /// <summary>
        /// 信用卡手續費
        /// </summary>
        public string CCFee { get; set; }

        /// <summary>
        /// 處理費
        /// </summary>
        public string Fee { get; set; }

        /// <summary>
        /// 代收貨款
        /// </summary>
        public string Cod { get; set; }

        /// <summary>
        /// 派件代收貨款
        /// </summary>
        public string To_Dlv_Cod { get; set; }

        /// <summary>
        /// 客戶代收貨款
        /// </summary>
        public string CustomerCod { get; set; }

        /// <summary>
        /// 派送代收貨款
        /// </summary>
        public string TransCod { get; set; }

        /// <summary>
        /// 客戶訂單號
        /// </summary>
        public string Order_No { get; set; }

        /// <summary>
        /// 尾程單號
        /// </summary>
        public string Express_No { get; set; }

        /// <summary>
        /// 分提單號(原始)
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 稅單編號列表
        /// </summary>
        public List<string> TaxNumberList { get; set; }

        /// <summary>
        /// 掃貨上車時間
        /// </summary>
        public string ScanCargoUploadTime { get; set; }

        /// <summary>
        /// 掃貨上車人員
        /// </summary>
        public string ScanCargoUploadOpe { get; set; }

        /// <summary>
        /// 掃貨派送公司
        /// </summary>
        public string ScanCargoTransName { get; set; }

        /// <summary>
        /// 車號
        /// </summary>
        public string ScanCargoCarNo { get; set; }

        /// <summary>
        /// 錯單原因
        /// </summary>
        public string ErrorReason { get; set; }

        public string Status { get; set; }

        /// <summary>
        /// 配送進度列表
        /// </summary>
        public List<CargoStatusItem> CargoStatusList { get; set; }

        /// <summary>
        /// 實際申報人
        /// </summary>
        public string ActualDeclarant { get; set; }

        /// <summary>
        /// 實際申報人電話
        /// </summary>
        public string ActualDeclarantPhone { get; set; }

        /// <summary>
        /// 實際申報品名列表
        /// </summary>
        public List<string> ActualItemNameList { get; set; }

        /// <summary>
        /// 實際申報金額
        /// </summary>
        public decimal ActualInvoiceAmount { get; set; }
    }

    /// <summary>
    /// 配送進度項目
    /// </summary>
    public class CargoStatusItem
    {
        /// <summary>
        /// 時間
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        public string Status { get; set; }
    }
}
