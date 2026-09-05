using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Logistics
{
    /// <summary>
    /// 查詢託運單請求模型
    /// </summary>
    public class QueryRequest
    {
        /// <summary>
        /// 客戶訂單號
        /// </summary>
        public string CusOrder { get; set; }
    }

    /// <summary>
    /// 查詢託運單回應模型
    /// </summary>
    public class QueryResponse
    {
        /// <summary>
        /// 結果代碼
        /// </summary>
        public string ResultCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 託運單資料列表
        /// </summary>
        public QueryRow[] Rows { get; set; }
    }

    /// <summary>
    /// 託運單配送資料
    /// </summary>
    public class QueryRow
    {
        /// <summary>
        /// 系統訂單號
        /// </summary>
        public long SysOrder { get; set; }

        /// <summary>
        /// 文件批號
        /// </summary>
        public string DocLotNo { get; set; }

        /// <summary>
        /// 配送車次
        /// </summary>
        public string DcShip { get; set; }

        /// <summary>
        /// 車牌號碼
        /// </summary>
        public string CarId { get; set; }

        /// <summary>
        /// 配送中心名稱
        /// </summary>
        public string DcName { get; set; }

        /// <summary>
        /// 運輸公司名稱
        /// </summary>
        public string TranCompName { get; set; }

        /// <summary>
        /// 司機ID
        /// </summary>
        public string DriverId { get; set; }

        /// <summary>
        /// 司機姓名
        /// </summary>
        public string DriverName { get; set; }

        /// <summary>
        /// 門市名稱
        /// </summary>
        public string ShopName { get; set; }

        /// <summary>
        /// 客戶訂單號
        /// </summary>
        public string CusOrder { get; set; }

        /// <summary>
        /// 客戶訂單編號
        /// </summary>
        public string CusOrderNo { get; set; }

        /// <summary>
        /// 客戶擁有者ID
        /// </summary>
        public string CusOwnerId { get; set; }

        /// <summary>
        /// 客戶擁有者名稱
        /// </summary>
        public string CusOwnerName { get; set; }

        /// <summary>
        /// 序號
        /// </summary>
        public string SerialNo { get; set; }

        /// <summary>
        /// 到達順序
        /// </summary>
        public int? ArriveSeq { get; set; }

        /// <summary>
        /// 到達順序2
        /// </summary>
        public int? ArriveSeq2 { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Addr { get; set; }

        /// <summary>
        /// 標準化地址 (使用底線命名以符合 API 格式)
        /// </summary>
        public string Nor_Addr { get; set; }

        /// <summary>
        /// 是否有經緯度
        /// </summary>
        public bool IsLonAndLat { get; set; }

        /// <summary>
        /// 經度
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// 緯度
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// 聯絡人
        /// </summary>
        public string ContactPerson { get; set; }

        /// <summary>
        /// 聯絡電話
        /// </summary>
        public string ContactTel { get; set; }

        /// <summary>
        /// 品項名稱 (使用底線命名以符合 API 格式)
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 總金額
        /// </summary>
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// 應收帳款
        /// </summary>
        public decimal? AccountsReceivable { get; set; }

        /// <summary>
        /// 付款方式名稱
        /// </summary>
        public string PaymentMethodName { get; set; }

        /// <summary>
        /// 實際付款金額
        /// </summary>
        public decimal? ActualPayment { get; set; }

        /// <summary>
        /// 原因ID
        /// </summary>
        public string ReasonId { get; set; }

        /// <summary>
        /// 原因時間
        /// </summary>
        public string ReasonTime { get; set; }

        /// <summary>
        /// 原因名稱
        /// </summary>
        public string ReasonName { get; set; }

        /// <summary>
        /// 備註原因
        /// </summary>
        public string MemoReason { get; set; }

        /// <summary>
        /// 簽收人代碼
        /// </summary>
        public string Signer { get; set; }

        /// <summary>
        /// 簽收人名稱
        /// </summary>
        public string SignerName { get; set; }

        /// <summary>
        /// 簽收方式代碼
        /// </summary>
        public string SignMethod { get; set; }

        /// <summary>
        /// 簽收方式名稱
        /// </summary>
        public string SignMethodName { get; set; }

        /// <summary>
        /// 簽收狀態代碼
        /// </summary>
        public string SignStatus { get; set; }

        /// <summary>
        /// 簽收狀態名稱
        /// </summary>
        public string SignStatusName { get; set; }

        /// <summary>
        /// 文件流程狀態代碼
        /// </summary>
        public string DocFlowStatus { get; set; }

        /// <summary>
        /// 文件流程狀態名稱
        /// </summary>
        public string DocFlowStatusName { get; set; }

        /// <summary>
        /// 文件狀態代碼
        /// </summary>
        public string DocStatus { get; set; }

        /// <summary>
        /// 文件狀態名稱
        /// </summary>
        public string DocStatusName { get; set; }

        /// <summary>
        /// 實際到達日期 (格式: yyyy-MM-dd)
        /// </summary>
        public string RealArriveDate { get; set; }

        /// <summary>
        /// 實際到達時間 (格式: HH:mm)
        /// </summary>
        public string RealArriveTime { get; set; }

        /// <summary>
        /// 預計到達日期 (格式: yyyy-MM-dd)
        /// </summary>
        public string ArriveDate { get; set; }
    }
}