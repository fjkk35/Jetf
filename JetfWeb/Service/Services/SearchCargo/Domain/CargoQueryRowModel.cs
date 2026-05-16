using System;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
    /// 貨況查詢整併後的內部資料模型。
    /// </summary>
    internal class CargoQueryRowModel
    {
        /// <summary>
        /// 資料主鍵。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 資料來源，僅使用 Air 或 Sea。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 原始資料類型。
        /// </summary>
        public string ORIGINAL { get; set; }

        /// <summary>
        /// 預計到港日。
        /// </summary>
        public DateTime? ETA { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        public string GW { get; set; }

        /// <summary>
        /// 件數。
        /// </summary>
        public string PIECE { get; set; }

        /// <summary>
        /// 稅金作業日。
        /// </summary>
        public string F_DataDate { get; set; }

        /// <summary>
        /// 倉儲類型。
        /// </summary>
        public string I_DATA_TYPE { get; set; }

        /// <summary>
        /// 通關類型。
        /// </summary>
        public string I_CLEARANCE_TYPE { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string DESPATCH_NAME { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CUSTOMER { get; set; }

        /// <summary>
        /// 進倉時間。
        /// </summary>
        public DateTime? I_SIGN_IN_TIME { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public DateTime? I_SIGN_OUT_TIME { get; set; }

        /// <summary>
        /// 主提單號。
        /// </summary>
        public string MAINNUMBER { get; set; }

        /// <summary>
        /// 清關袋號。
        /// </summary>
        public string BL_NO { get; set; }

        /// <summary>
        /// 分提單號或系統運單號。
        /// </summary>
        public string JETF_SERIAL { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string F_TAX_NUMBER { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        public string TRANS_NAME { get; set; }

        /// <summary>
        /// 收件人名稱。
        /// </summary>
        public string IMPORTER { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string IM_PHONENO { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        public string IM_ADD { get; set; }

        /// <summary>
        /// 稅金類別。
        /// </summary>
        public string F_INCLUDE_TAX { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public string F_CCFEE { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public string F_FEE { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public string F_COD { get; set; }

        /// <summary>
        /// 稅額一。
        /// </summary>
        public string F_TAX1 { get; set; }

        /// <summary>
        /// 稅額二。
        /// </summary>
        public string F_TAX2 { get; set; }

        /// <summary>
        /// 物流代收款。
        /// </summary>
        public string F_TO_DLV_COD { get; set; }

        /// <summary>
        /// 品名。
        /// </summary>
        public string ITEM_NAME { get; set; }

        /// <summary>
        /// 到付款或其他附加欄位。
        /// </summary>
        public string CC { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DELIVERYNO { get; set; }

        /// <summary>
        /// 客戶外箱號。
        /// </summary>
        public string FIELD_X { get; set; }

        /// <summary>
        /// 稅金付款對應代碼。
        /// </summary>
        public string TRANS_TAXPAYMENT { get; set; }

        /// <summary>
        /// 派件公司新名稱。
        /// </summary>
        public string TRANS_NAME_NEW { get; set; }

        /// <summary>
        /// 客戶訂單號。
        /// </summary>
        public string ORDER_NO { get; set; }

        /// <summary>
        /// 尾程單號。
        /// </summary>
        public string EXPRESS_NO { get; set; }

        /// <summary>
        /// 追蹤號。
        /// </summary>
        public string TRACKINGNO { get; set; }

        /// <summary>
        /// 格式化後的出倉日期。
        /// </summary>
        public string Format_OUT_DATETIME { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        public string STATUS { get; set; }

        /// <summary>
        /// 原始資料建立時間。
        /// </summary>
        public DateTime? SOURCE_CREATEDATE { get; set; }
    }
}