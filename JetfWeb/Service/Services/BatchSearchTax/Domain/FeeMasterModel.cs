using System;

namespace Service.Services.BatchSearchTax.Domain
{
    /// <summary>
    /// 費用主檔資料模型
    /// </summary>
    public class FeeMasterModel
    {
        /// <summary>
        /// 1海運 2海運G類 3 空運
        /// </summary>
        public string Source_Type { get; set; }

        /// <summary>
        /// 物流貨號
        /// </summary>
        public string Dlv_Inv { get; set; }

        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 清關袋號
        /// </summary>
        public string Bag_Number { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string Main_Number { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string Clearance_Number { get; set; }

        /// <summary>
        /// 稅單號碼
        /// </summary>
        public string Tax_Number { get; set; }

        /// <summary>
        /// 稅基
        /// </summary>
        public decimal? Tax_Base { get; set; }

        /// <summary>
        /// 稅金1
        /// </summary>
        public decimal? Tax1 { get; set; }

        /// <summary>
        /// 稅金2
        /// </summary>
        public decimal? Tax2 { get; set; }

        /// <summary>
        /// 報關費
        /// </summary>
        public decimal? Ccfee { get; set; }

        /// <summary>
        /// 到付款
        /// </summary>
        public decimal? Cod { get; set; }

        /// <summary>
        /// 手續費
        /// </summary>
        public decimal? Fee { get; set; }

        /// <summary>
        /// 跟派件收稅金
        /// </summary>
        public decimal? Trans_Cod { get; set; }

        /// <summary>
        /// 跟廠商收稅金
        /// </summary>
        public decimal? Customer_Cod { get; set; }

        /// <summary>
        /// 是否包稅
        /// </summary>
        public string Include_Tax { get; set; }

        /// <summary>
        /// 派件物流公司代號
        /// </summary>
        public string Dlv_Com { get; set; }

        /// <summary>
        /// 物流代收貨款金額
        /// </summary>
        public decimal? To_Dlv_Cod { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string Trans_Name { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string Customer { get; set; }
    }
}
